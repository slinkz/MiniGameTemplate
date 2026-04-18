using MiniGameTemplate.Rendering;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕渲染器——通过 RenderBatchManager 按 Texture 分桶渲染（ADR-029 v2：统一 Normal）。
    /// 支持多贴图弹丸和 Static/SpriteSheet 两种采样模式。
    /// </summary>
    public class BulletRenderer
    {
        private RenderBatchManager _batchManager;
        private Texture2D _fallbackAtlas; // 旧资产 SourceTexture 为空时的 fallback
        private int _totalQuadCount;

        /// <summary>上帧绘制的弹丸总 Quad 数（含残影）</summary>
        public int TotalDrawCount => _totalQuadCount;

        /// <summary>
        /// 初始化渲染器——从 TypeRegistry 收集所有 Texture 预热桶。
        /// </summary>
        public void Initialize(DanmakuRenderConfig renderConfig, DanmakuTypeRegistry registry, int maxQuadsPerBucket)
        {
            _batchManager = new RenderBatchManager();
            _fallbackAtlas = renderConfig.BulletAtlas;

            // 收集所有唯一的 Texture 组合（ADR-029 v2：Layer 统一 Normal）
            var registrations = new System.Collections.Generic.List<RenderBatchManager.BucketRegistration>();

            if (registry.BulletTypes != null)
            {
                for (int i = 0; i < registry.BulletTypes.Length; i++)
                {
                    var bt = registry.BulletTypes[i];
                    if (bt == null) continue;

                    // 优先 AtlasBinding.AtlasTexture > SourceTexture > fallback
                    var tex = bt.GetResolvedTexture();
                    if (tex == null) tex = _fallbackAtlas;
                    if (tex == null) continue;

                    var key = new RenderBatchManager.BucketKey(RenderLayer.Normal, tex);
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
                        registrations.Add(new RenderBatchManager.BucketRegistration(key, renderConfig.BulletMaterial, RenderSortingOrder.Bullet));
                }
            }

            // 兼容：如果没有任何桶被收集但全局 Atlas 存在，至少建一个 fallback 桶
            if (registrations.Count == 0 && _fallbackAtlas != null)
            {
                registrations.Add(new RenderBatchManager.BucketRegistration(
                    new RenderBatchManager.BucketKey(RenderLayer.Normal, _fallbackAtlas),
                    renderConfig.BulletMaterial,
                    RenderSortingOrder.Bullet));
            }

            _batchManager.Initialize(registrations, maxQuadsPerBucket);
        }

        /// <summary>
        /// 每帧由 DanmakuSystem.LateUpdate 调用——收集弹丸 -> 填充顶点 -> 上传 -> DrawMesh。
        /// </summary>
        public void Rebuild(BulletWorld world, BulletTrail[] trails, DanmakuTypeRegistry registry)
        {
            _batchManager.ResetAll();
            _totalQuadCount = 0;

            var cores = world.Cores;
            int capacity = world.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref var core = ref cores[i];
                if ((core.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;

                var bulletType = registry.BulletTypes[core.TypeIndex];

                // 确定贴图——优先 AtlasBinding > SourceTexture > fallback（ADR-017）
                // 注意：Unity Object 的 ?? 运算符不走 Unity 的 == null 重载，必须用显式 != null
                var resolvedTex = bulletType.GetResolvedTexture();
                var texture = (resolvedTex != null) ? resolvedTex : _fallbackAtlas;
                if (texture == null) continue;

                // 解析基础 UV——Atlas 绑定时从 AtlasMappingSO 查子区域
                Rect baseUV = bulletType.GetResolvedBaseUV();

                var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, texture);
                if (!_batchManager.TryGetBucket(bucketKey, out var bucket)) continue;

                // 受伤闪烁
                ref var trail = ref trails[i];
                Color tint = bulletType.Tint;
                if (trail.FlashTimer > 0)
                {
                    tint = bulletType.DamageFlashTint;
                    trail.FlashTimer--;
                }

                // 爆炸帧动画
                if (core.Phase == (byte)BulletPhase.Exploding
                    && bulletType.Explosion == ExplosionMode.MeshFrame
                    && bulletType.ExplosionFrameCount > 0)
                {
                    float frameDuration = 1f / 60f;
                    int frame = Mathf.Clamp(
                        (int)(core.Elapsed / frameDuration),
                        0, bulletType.ExplosionFrameCount - 1);

                    Rect uv = bulletType.ExplosionAtlasUV;
                    float frameWidth = uv.width;
                    Rect frameUV = new Rect(
                        uv.x + frame * frameWidth, uv.y,
                        frameWidth, uv.height);

                    float explosionAlpha = 1f - (float)frame / bulletType.ExplosionFrameCount;

                    WriteQuadUV(bucket, ref core, bulletType, explosionAlpha, tint, frameUV);
                }
                else
                {
                    // 解析 UV：Static 或 SpriteSheet（基于 Atlas 解析后的 baseUV）
                    Rect uv = ResolveUV(bulletType, ref core, baseUV);

                    WriteQuadUV(bucket, ref core, bulletType, 1f, tint, uv);
                }

                // 残影 Quad
                if (core.Phase == (byte)BulletPhase.Active)
                {
                    Rect ghostUV = bulletType.SamplingMode == BulletSamplingMode.Static
                        ? baseUV
                        : bulletType.GetFrameUV(0, baseUV); // 残影用第一帧

                    if (trail.TrailLength >= 1)
                        WriteGhostQuad(bucket, trail.PrevPos1, bulletType, 0.6f, ghostUV);
                    if (trail.TrailLength >= 2)
                        WriteGhostQuad(bucket, trail.PrevPos2, bulletType, 0.3f, ghostUV);
                    if (trail.TrailLength >= 3)
                        WriteGhostQuad(bucket, trail.PrevPos3, bulletType, 0.15f, ghostUV);
                }
            }

            _batchManager.UploadAndDrawAll();
        }

        /// <summary>释放 BatchManager 资源。</summary>
        public void Dispose()
        {
            _batchManager?.Dispose();
        }

        // ──── 序列帧 UV 解析 ────

        /// <summary>
        /// 解析弹丸当前帧的 UV。
        /// Static 模式返回 baseUV，SpriteSheet 模式按生命周期或固定 FPS 计算帧索引。
        /// baseUV 已经过 Atlas 解析，可能是 Atlas 子区域而非 TypeSO.UVRect。
        /// </summary>
        private static Rect ResolveUV(BulletTypeSO type, ref BulletCore core, Rect baseUV)
        {
            if (type.SamplingMode == BulletSamplingMode.Static)
                return baseUV;

            // SpriteSheet 模式：计算帧索引
            int frameIndex = 0;
            int maxFrame = type.MaxFrameCount;

            switch (type.PlaybackMode)
            {
                case BulletPlaybackMode.StretchToLifetime:
                {
                    // 弹丸生命周期归一化时间 -> 帧
                    float t = core.Lifetime > 0f
                        ? Mathf.Clamp01(core.Elapsed / core.Lifetime)
                        : 0f;
                    frameIndex = Mathf.Min((int)(t * maxFrame), maxFrame - 1);
                    break;
                }

                case BulletPlaybackMode.FixedFpsLoop:
                {
                    float fps = Mathf.Max(0.001f, type.FixedFps);
                    frameIndex = (int)(core.Elapsed * fps) % maxFrame;
                    break;
                }

                case BulletPlaybackMode.FixedFpsOnce:
                {
                    float fps = Mathf.Max(0.001f, type.FixedFps);
                    frameIndex = Mathf.Min((int)(core.Elapsed * fps), maxFrame - 1);
                    break;
                }
            }

            return type.GetFrameUV(frameIndex, baseUV);
        }

        // ──── Quad 写入 ────

        private void WriteQuadUV(RenderBatchManager.RenderBucket bucket,
            ref BulletCore core, BulletTypeSO type, float alpha, Color tint, Rect uv)
        {
            int baseVertex = bucket.AllocateQuad();
            if (baseVertex < 0) return;

            _totalQuadCount++;

            // DEC-005=C：从 Core 读取动画值（Mover 每帧写入，Allocate 初始化默认值）
            float halfW = type.Size.x * 0.5f * core.AnimScale;
            float halfH = type.Size.y * 0.5f * core.AnimScale;

            // 动画透明度叠加
            float finalAlpha = alpha * core.AnimAlpha;

            // 动画颜色叠加
            Color finalTint = new Color(
                tint.r * (core.AnimColor.r / 255f),
                tint.g * (core.AnimColor.g / 255f),
                tint.b * (core.AnimColor.b / 255f),
                tint.a * (core.AnimColor.a / 255f));

            float cos = 1f, sin = 0f;
            if ((core.Flags & BulletCore.FLAG_ROTATE_TO_DIR) != 0)
            {
                float angle = Mathf.Atan2(core.Velocity.y, core.Velocity.x);
                cos = Mathf.Cos(angle);
                sin = Mathf.Sin(angle);
            }

            var verts = bucket.Vertices;
            WriteVertex(ref verts[baseVertex + 0], core.Position, -halfW, -halfH, cos, sin,
                uv.xMin, uv.yMin, finalTint, finalAlpha);
            WriteVertex(ref verts[baseVertex + 1], core.Position, halfW, -halfH, cos, sin,
                uv.xMax, uv.yMin, finalTint, finalAlpha);
            WriteVertex(ref verts[baseVertex + 2], core.Position, halfW, halfH, cos, sin,
                uv.xMax, uv.yMax, finalTint, finalAlpha);
            WriteVertex(ref verts[baseVertex + 3], core.Position, -halfW, halfH, cos, sin,
                uv.xMin, uv.yMax, finalTint, finalAlpha);
        }

        private void WriteGhostQuad(RenderBatchManager.RenderBucket bucket,
            Vector2 position, BulletTypeSO type, float alpha, Rect uv)
        {
            int baseVertex = bucket.AllocateQuad();
            if (baseVertex < 0) return;

            _totalQuadCount++;

            float halfW = type.Size.x * 0.5f;
            float halfH = type.Size.y * 0.5f;

            var verts = bucket.Vertices;
            WriteVertex(ref verts[baseVertex + 0], position, -halfW, -halfH, 1, 0,
                uv.xMin, uv.yMin, type.Tint, alpha);
            WriteVertex(ref verts[baseVertex + 1], position, halfW, -halfH, 1, 0,
                uv.xMax, uv.yMin, type.Tint, alpha);
            WriteVertex(ref verts[baseVertex + 2], position, halfW, halfH, 1, 0,
                uv.xMax, uv.yMax, type.Tint, alpha);
            WriteVertex(ref verts[baseVertex + 3], position, -halfW, halfH, 1, 0,
                uv.xMin, uv.yMax, type.Tint, alpha);
        }

        private static void WriteVertex(
            ref RenderVertex v,
            Vector2 center, float offsetX, float offsetY,
            float cos, float sin,
            float uvX, float uvY,
            Color tint, float alpha)
        {
            float rx = offsetX * cos - offsetY * sin;
            float ry = offsetX * sin + offsetY * cos;

            v.Position = new Vector3(center.x + rx, center.y + ry, 0f);
            v.UV = new Vector2(uvX, uvY);
            v.Color = new Color32(
                (byte)(tint.r * 255),
                (byte)(tint.g * 255),
                (byte)(tint.b * 255),
                (byte)(alpha * tint.a * 255));
        }

    }
}
