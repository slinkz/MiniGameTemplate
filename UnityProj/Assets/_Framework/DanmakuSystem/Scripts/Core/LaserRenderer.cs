using UnityEngine;
using MiniGameTemplate.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光 Mesh 渲染器——通过 RenderBatchManager 按 (RenderLayer, LaserTexture) 分桶渲染。
    /// <para>
    /// 每条激光的每段 (LaserSegment) 生成一个 Quad（4 顶点 / 6 索引）：
    /// - 宽度由 LaserTypeSO.WidthProfile 沿线段长度驱动
    /// - UV.x 0->1 横跨宽度（中心=0.5，Shader 用于 Core/Glow 渐变）
    /// - UV.y 沿长度方向映射（Shader 用于纹理滚动）
    /// - 顶点 Color = 插值(CoreColor, EdgeColor) * Phase alpha
    /// </para>
    /// </summary>
    public class LaserRenderer
    {
        private RenderBatchManager _batchManager;
        private int _quadCount;

        /// <summary>上帧绘制的 Quad 数（调试用）</summary>
        public int DrawCount => _quadCount;

        /// <summary>
        /// 初始化渲染器——从 TypeRegistry 收集所有激光贴图预热桶。
        /// </summary>
        public void Initialize(DanmakuRenderConfig renderConfig, DanmakuTypeRegistry registry, int maxQuadsPerBucket)
        {
            _batchManager = new RenderBatchManager();

            var registrations = new System.Collections.Generic.List<RenderBatchManager.BucketRegistration>();

            if (registry.LaserTypes != null)
            {
                for (int i = 0; i < registry.LaserTypes.Length; i++)
                {
                    var lt = registry.LaserTypes[i];
                    if (lt == null || lt.LaserTexture == null) continue;

                    // 激光统一使用 Normal 层（激光自身材质处理混合模式）
                    var key = new RenderBatchManager.BucketKey(RenderLayer.Normal, lt.LaserTexture);
                    bool exists = false;
                    for (int j = 0; j < registrations.Count; j++)
                    {
                        if (registrations[j].Key.Equals(key))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                        registrations.Add(new RenderBatchManager.BucketRegistration(key, renderConfig.LaserMaterial, RenderSortingOrder.LaserDefault));
                }
            }

            _batchManager.Initialize(registrations, maxQuadsPerBucket);
        }

        /// <summary>
        /// 每帧由 DanmakuSystem.LateUpdate 调用——重建激光 Mesh 并 DrawMesh。
        /// </summary>
        public void Rebuild(LaserPool pool, DanmakuTypeRegistry registry)
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

                var type = registry.LaserTypes[laser.LaserTypeIndex];
                if (type.LaserTexture == null) continue;

                var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, type.LaserTexture);
                if (!_batchManager.TryGetBucket(bucketKey, out var bucket)) continue;

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
                        ref uvYAccum, ref lengthAccum, totalLength);
                }
            }

            _batchManager.UploadAndDrawAll();
        }

        /// <summary>释放 BatchManager 资源。</summary>
        public void Dispose()
        {
            _batchManager?.Dispose();
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

        private void WriteSegmentQuad(
            RenderBatchManager.RenderBucket bucket,
            ref LaserSegment seg,
            LaserTypeSO type,
            float width,
            float alpha,
            ref float uvYAccum,
            ref float lengthAccum,
            float totalLength)
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
                UV = new Vector2(0f, uvYAccum),
            };

            verts[baseV + 1] = new RenderVertex
            {
                Position = new Vector3(startRight.x, startRight.y, 0f),
                Color = color,
                UV = new Vector2(1f, uvYAccum),
            };

            verts[baseV + 2] = new RenderVertex
            {
                Position = new Vector3(endRight.x, endRight.y, 0f),
                Color = color,
                UV = new Vector2(1f, uvYEnd),
            };

            verts[baseV + 3] = new RenderVertex
            {
                Position = new Vector3(endLeft.x, endLeft.y, 0f),
                Color = color,
                UV = new Vector2(0f, uvYEnd),
            };

            uvYAccum = uvYEnd;
            lengthAccum += seg.Length;
        }
    }
}
