using UnityEngine;
using MiniGameTemplate.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光 Mesh 渲染器——通过 RenderBatchManager 按 (RenderLayer, Texture) 分桶渲染。
    /// <para>
    /// PI-001: 接收 DanmakuSystem 共享的 RuntimeAtlasManager。
    /// Laser Phase L2: UseRuntimeAtlas=true 时走 Atlas，UV 归一化到子区域；
    ///                  UseRuntimeAtlas=false 时保持独立贴图 + 世界空间 UV。
    /// </para>
    /// </summary>
    public class LaserRenderer
    {
        private RenderBatchManager _batchManager;
        private Material _laserMaterial;
        private Material _laserMaterialAtlas;  // Atlas 模式材质克隆（启用 _ATLASMODE_ON keyword）
        private RuntimeAtlasManager _runtimeAtlas;
        private int _quadCount;

        /// <summary>上帧绘制的 Quad 数（调试用）</summary>
        public int DrawCount => _quadCount;

        /// <summary>
        /// 初始化渲染器。PI-001: 接收共享 RuntimeAtlasManager。
        /// </summary>
        internal void Initialize(DanmakuRenderConfig renderConfig, DanmakuTypeRegistry registry,
            int maxQuadsPerBucket, RuntimeAtlasManager sharedAtlas = null)
        {
            _batchManager = new RenderBatchManager();
            _laserMaterial = renderConfig.LaserMaterial;
            _runtimeAtlas = sharedAtlas;

            // Atlas 模式材质克隆：启用 _ATLASMODE_ON keyword，
            // 让 Shader 在 frag 中分离渐变 UV.x 和纹理采样 UV。
            if (_laserMaterial != null)
            {
                _laserMaterialAtlas = new Material(_laserMaterial);
                _laserMaterialAtlas.name = _laserMaterial.name + " (Atlas)";
                // ADR-032: new Material() 不可靠保留 shaderKeywords，必须显式复制后再追加。
                _laserMaterialAtlas.shaderKeywords = _laserMaterial.shaderKeywords;
                _laserMaterialAtlas.EnableKeyword("_ATLASMODE_ON");
            }

            // ADR-030：激光桶允许在首次发射对应类型时按需创建。
            _batchManager.Initialize(System.Array.Empty<RenderBatchManager.BucketRegistration>(), maxQuadsPerBucket);
        }

        /// <summary>
        /// 每帧由 DanmakuSystem.LateUpdate 调用——重建激光 Mesh 并 DrawMesh。
        /// </summary>
        internal void Rebuild(LaserPool pool, DanmakuTypeRegistry registry)
        {
            _batchManager.ResetAll();
            _quadCount = 0;

            if (pool.ActiveCount == 0)
            {
                _batchManager.UploadAndDrawAll();
                return;
            }

            for (int i = 0; i < pool.Capacity; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase == 0) continue;
                if (laser.SegmentCount == 0) continue;

                var type = registry.GetLaserType(laser.LaserTypeIndex);
                if (type.LaserTexture == null) continue;

                // L2.3: 通过 Resolver 解算纹理（PI-005: 简化签名）
                var binding = RuntimeAtlasBindingResolver.ResolveLaser(_runtimeAtlas, type);
                if (!binding.IsValid) continue;

                // L2.4: Atlas 模式用 binding.UVRect，非 Atlas 模式用 full rect
                bool usesAtlas = binding.UsesRuntimeAtlas;
                Rect atlasUVRect = usesAtlas ? binding.UVRect : new Rect(0, 0, 1, 1);

                // Atlas 桶用启用 _ATLASMODE_ON 的材质克隆，非 Atlas 桶用原始材质
                var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, binding.Texture);
                Material mat = usesAtlas ? _laserMaterialAtlas : _laserMaterial;
                if (!_batchManager.TryGetOrCreateBucket(bucketKey, mat, RenderSortingOrder.LaserDefault, out var bucket))
                    continue;

                // Phase alpha
                float alpha = GetPhaseAlpha(ref laser, type);
                if (alpha <= 0f) continue;

                // 累计 UV.y 偏移和长度比例
                float uvYAccum = 0f;
                float lengthAccum = 0f;
                float totalLength = laser.VisualLength > 0f ? laser.VisualLength : laser.Length;

                for (int s = 0; s < laser.SegmentCount; s++)
                {
                    ref var seg = ref laser.Segments[s];
                    if (seg.Length <= 0.0001f) continue;

                    WriteSegmentQuad(bucket, ref seg, type, laser.Width, alpha,
                        ref uvYAccum, ref lengthAccum, totalLength, atlasUVRect, usesAtlas);
                }
            }

            _batchManager.UploadAndDrawAll();
        }

        /// <summary>释放 BatchManager 资源。PI-001: 共享 Atlas 由 DanmakuSystem 统一 Dispose。</summary>
        public void Dispose()
        {
            _batchManager?.Dispose();
            if (_laserMaterialAtlas != null)
            {
                UnityObjectDestroyUtility.Destroy(_laserMaterialAtlas);
                _laserMaterialAtlas = null;
            }
            _runtimeAtlas = null;
        }

        // ──── 内部方法 ────

        private static float GetPhaseAlpha(ref LaserData laser, LaserTypeSO type)
        {
            switch (laser.Phase)
            {
                case 1: // Charging
                    return 0.3f + 0.5f * (0.5f + 0.5f * Mathf.Sin(laser.Elapsed * 20f));
                case 2: // Firing
                    return 1f;
                case 3: // Fading
                    float fadeStart = type.ChargeDuration + type.FiringDuration;
                    float fadeProgress = (laser.Elapsed - fadeStart) / type.FadeDuration;
                    return Mathf.Clamp01(1f - fadeProgress);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 写入一段激光 Quad。
        /// PI-002: Atlas 模式下 UV.x 保持 [0,1]（给 Shader 做横向渐变），
        ///         UV.y 归一化到 Atlas 子区域。Shader 通过 _ATLASMODE_ON keyword
        ///         分离渐变计算和纹理采样。
        ///         非 Atlas 模式保留原始世界空间 UV（wrapMode=Repeat 环绕）。
        /// CR-03: 使用显式 bool 标志替代 width < 1f 浮点比较。
        /// </summary>
        private void WriteSegmentQuad(
            RenderBatchManager.RenderBucket bucket,
            ref LaserSegment seg,
            LaserTypeSO type,
            float width,
            float alpha,
            ref float uvYAccum,
            ref float lengthAccum,
            float totalLength,
            Rect atlasUVRect,
            bool usesAtlas)
        {
            int baseV = bucket.AllocateQuad();
            if (baseV < 0) return;

            _quadCount++;

            float halfW = width * 0.5f;

            Vector2 perp = new Vector2(-seg.Direction.y, seg.Direction.x);

            float startWidthScale = 1f;
            float endWidthScale = 1f;
            if (type.WidthProfile != null && type.WidthProfile.length > 0 && totalLength > 0f)
            {
                float startNorm = Mathf.Clamp01(lengthAccum / totalLength);
                float endNorm = Mathf.Clamp01((lengthAccum + seg.Length) / totalLength);
                startWidthScale = type.WidthProfile.Evaluate(startNorm);
                endWidthScale = type.WidthProfile.Evaluate(endNorm);
            }

            float startHalfW = halfW * startWidthScale;
            float endHalfW = halfW * endWidthScale;

            Vector2 startLeft = seg.Start + perp * startHalfW;
            Vector2 startRight = seg.Start - perp * startHalfW;
            Vector2 endLeft = seg.End + perp * endHalfW;
            Vector2 endRight = seg.End - perp * endHalfW;

            float uvYEnd = uvYAccum + seg.Length;

            // PI-002: UV 映射语义分支（CR-03: 使用显式 bool 替代浮点比较）
            float u0, u1, v0, v1;
            if (usesAtlas)
            {
                // Atlas 模式：UV.x = [0, 1]（渐变参数，给 Shader distFromCenter 用）
                // UV.y = Atlas 子区域归一化（纹理纵向采样）
                u0 = 0f;
                u1 = 1f;
                v0 = atlasUVRect.y + (totalLength > 0f ? uvYAccum / totalLength : 0f) * atlasUVRect.height;
                v1 = atlasUVRect.y + (totalLength > 0f ? uvYEnd / totalLength : 0f) * atlasUVRect.height;
            }
            else // 非 Atlas 模式：保留原始世界空间 UV
            {
                u0 = 0f;
                u1 = 1f;
                v0 = uvYAccum;
                v1 = uvYEnd;
            }

            Color32 color = new Color32(
                (byte)(type.CoreColor.r * 255),
                (byte)(type.CoreColor.g * 255),
                (byte)(type.CoreColor.b * 255),
                (byte)(alpha * 255));

            var verts = bucket.Vertices;

            verts[baseV + 0] = new RenderVertex
            {
                Position = new Vector3(startLeft.x, startLeft.y, 0f),
                Color = color,
                UV = new Vector2(u0, v0),
            };

            verts[baseV + 1] = new RenderVertex
            {
                Position = new Vector3(startRight.x, startRight.y, 0f),
                Color = color,
                UV = new Vector2(u1, v0),
            };

            verts[baseV + 2] = new RenderVertex
            {
                Position = new Vector3(endRight.x, endRight.y, 0f),
                Color = color,
                UV = new Vector2(u1, v1),
            };

            verts[baseV + 3] = new RenderVertex
            {
                Position = new Vector3(endLeft.x, endLeft.y, 0f),
                Color = color,
                UV = new Vector2(u0, v1),
            };

            uvYAccum = uvYEnd;
            lengthAccum += seg.Length;
        }
    }
}
