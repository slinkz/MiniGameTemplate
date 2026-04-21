using MiniGameTemplate.Rendering;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光预警线渲染器——在激光 Charging 阶段渲染一条闪烁细线，提示玩家即将有激光。
    /// 复用 RenderBatchManager 架构，使用激光材质但排序在激光本体下方。
    /// PI-001: 接收共享 RuntimeAtlasManager。预警线天然适合入 Atlas（无 UV 滚动）。
    /// </summary>
    public class LaserWarningRenderer
    {
        private RenderBatchManager _batchManager;
        private Material _laserMaterial;
        private RuntimeAtlasManager _runtimeAtlas;
        private int _quadCount;

        /// <summary>预警线固定宽度（世界单位）</summary>
        private const float WARNING_WIDTH = 0.02f;

        /// <summary>闪烁频率（Hz）</summary>
        private const float BLINK_FREQUENCY = 12f;

        /// <summary>上帧绘制的 Quad 数（调试用）</summary>
        public int DrawCount => _quadCount;

        /// <summary>
        /// 初始化渲染器。PI-001: 接收共享 RuntimeAtlasManager。
        /// </summary>
        internal void Initialize(DanmakuRenderConfig renderConfig, DanmakuTypeRegistry registry,
            int maxQuads, RuntimeAtlasManager sharedAtlas = null)
        {
            _batchManager = new RenderBatchManager();
            _laserMaterial = renderConfig.LaserMaterial;
            _runtimeAtlas = sharedAtlas;

            // ADR-030：预警线桶允许在首次遇到激光贴图时按需创建。
            _batchManager.Initialize(System.Array.Empty<RenderBatchManager.BucketRegistration>(), maxQuads);
        }

        /// <summary>
        /// 每帧重建预警线 Mesh——仅渲染 Charging 阶段的激光。
        /// </summary>
        internal void Rebuild(LaserPool pool, DanmakuTypeRegistry registry)
        {
            _batchManager.ResetAll();
            _quadCount = 0;

            for (int i = 0; i < pool.Capacity; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase != 1) continue;  // 仅 Charging 阶段

                var type = registry.GetLaserType(laser.LaserTypeIndex);
                if (type.LaserTexture == null) continue;

                // 预警线默认走 Atlas（天然无 UV 滚动需求）
                var binding = RuntimeAtlasBindingResolver.ResolveLaser(_runtimeAtlas, type);
                if (!binding.IsValid) continue;

                var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, binding.Texture);
                if (!_batchManager.TryGetOrCreateBucket(bucketKey, _laserMaterial, RenderSortingOrder.LaserDefault - 1, out var bucket))
                    continue;

                // 闪烁 alpha：0.3 ~ 1.0 正弦波
                float blinkAlpha = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(laser.Elapsed * BLINK_FREQUENCY));

                WriteWarningLine(bucket, ref laser, type, blinkAlpha, binding.UsesRuntimeAtlas ? binding.UVRect : new Rect(0, 0, 1, 1));
            }

            _batchManager.UploadAndDrawAll();
        }

        /// <summary>释放 BatchManager 资源。PI-001: 共享 Atlas 由 DanmakuSystem 统一 Dispose。</summary>
        public void Dispose()
        {
            _batchManager?.Dispose();
            _runtimeAtlas = null;
        }

        private void WriteWarningLine(
            RenderBatchManager.RenderBucket bucket,
            ref LaserData laser,
            LaserTypeSO type,
            float alpha,
            Rect uvRect)
        {
            int baseV = bucket.AllocateQuad();
            if (baseV < 0) return;

            _quadCount++;

            float halfW = WARNING_WIDTH * 0.5f;
            float angle = laser.Angle;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // 预警线长度——使用激光的完整长度
            Vector2 start = laser.Origin;
            Vector2 end = laser.Origin + dir * laser.Length;

            Color32 color = new Color32(
                (byte)(type.CoreColor.r * 255),
                (byte)(type.CoreColor.g * 255),
                (byte)(type.CoreColor.b * 255),
                (byte)(alpha * 255));

            var verts = bucket.Vertices;

            verts[baseV + 0] = new RenderVertex
            {
                Position = new Vector3(start.x + perp.x * halfW, start.y + perp.y * halfW, 0f),
                Color = color,
                UV = new Vector2(uvRect.x, uvRect.y),
            };

            verts[baseV + 1] = new RenderVertex
            {
                Position = new Vector3(start.x - perp.x * halfW, start.y - perp.y * halfW, 0f),
                Color = color,
                UV = new Vector2(uvRect.x + uvRect.width, uvRect.y),
            };

            verts[baseV + 2] = new RenderVertex
            {
                Position = new Vector3(end.x - perp.x * halfW, end.y - perp.y * halfW, 0f),
                Color = color,
                UV = new Vector2(uvRect.x + uvRect.width, uvRect.y + uvRect.height),
            };

            verts[baseV + 3] = new RenderVertex
            {
                Position = new Vector3(end.x + perp.x * halfW, end.y + perp.y * halfW, 0f),
                Color = color,
                UV = new Vector2(uvRect.x, uvRect.y + uvRect.height),
            };
        }
    }
}
