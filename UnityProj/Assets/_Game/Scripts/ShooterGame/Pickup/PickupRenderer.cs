using System;
using System.Collections.Generic;
using MiniGameTemplate.Rendering;
using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// 道具批量 Mesh 渲染器（零 GC、零 Instantiate）。
    /// 每帧从 PickupSystem 读取活跃道具数据，通过 RenderBatchManager 绘制。
    /// 支持闪烁（即将消失警告）和 Sprite 纹理。
    /// </summary>
    public sealed class PickupRenderer : IDisposable
    {
        private const int MAX_QUADS = 16; // 与 PickupSystem.MAX_PICKUPS 一致

        private RenderBatchManager _batchManager;
        private Material _material;
        private bool _initialized;

        /// <summary>
        /// 初始化渲染器。
        /// </summary>
        /// <param name="material">Alpha Blend 材质模板（需支持 _MainTex + Vertex Color）</param>
        public void Initialize(Material material)
        {
            if (_initialized || material == null) return;

            _material = material;
            _batchManager = new RenderBatchManager();

            // 不预建桶——第一次遇到新 Sprite 纹理时按需动态建桶
            var emptyRegistrations = new List<RenderBatchManager.BucketRegistration>();
            _batchManager.Initialize(emptyRegistrations, MAX_QUADS);
            _initialized = true;
        }

        /// <summary>
        /// 每帧由 BattleController 在 LateUpdate 时调用——收集道具数据 → 填充顶点 → DrawMesh。
        /// </summary>
        public void Render(PickupSystem pickupSystem)
        {
            if (!_initialized || pickupSystem == null) return;

            _batchManager.ResetAll();

            int count = pickupSystem.ActiveCount;
            if (count == 0)
            {
                // 无道具也要 Upload（清空上帧残影）
                _batchManager.UploadAndDrawAll();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                ref readonly var pickup = ref pickupSystem.GetPickup(i);
                if (!pickup.IsActive) continue;

                var config = pickup.Config;
                if (config == null) continue;

                // 获取 Sprite 纹理
                Sprite icon = config.Icon;
                if (icon == null) continue;

                Texture texture = icon.texture;
                if (texture == null) continue;

                // 动态建桶（按纹理分桶）
                var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, texture);
                if (!_batchManager.TryGetOrCreateBucket(bucketKey, _material, RenderSortingOrder.Pickup, out var bucket))
                    continue;

                // 计算 UV（从 Sprite 的 Rect）
                Rect spriteRect = icon.textureRect;
                float texW = texture.width;
                float texH = texture.height;
                Rect uv = new Rect(
                    spriteRect.x / texW,
                    spriteRect.y / texH,
                    spriteRect.width / texW,
                    spriteRect.height / texH
                );

                // 计算 Alpha（闪烁）
                float alpha = 1f;
                if (PickupSystem.ShouldBlink(pickup.RemainingTime))
                {
                    alpha = PickupSystem.GetBlinkAlpha(pickup.RemainingTime);
                }

                // 写 Quad
                WriteQuad(bucket, pickup.Position, config.IconSize, uv, alpha);
            }

            _batchManager.UploadAndDrawAll();
        }

        public void Dispose()
        {
            _batchManager?.Dispose();
            _batchManager = null;
            _initialized = false;
        }

        // ── Quad 写入 ──

        private static void WriteQuad(
            RenderBatchManager.RenderBucket bucket,
            Vector2 position,
            Vector2 size,
            Rect uv,
            float alpha)
        {
            int baseVertex = bucket.AllocateQuad();
            if (baseVertex < 0) return;

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            Color32 color = new Color32(255, 255, 255, (byte)(alpha * 255));

            var verts = bucket.Vertices;

            // 左下
            verts[baseVertex + 0].Position = new Vector3(position.x - halfW, position.y - halfH, 0f);
            verts[baseVertex + 0].UV = new Vector2(uv.xMin, uv.yMin);
            verts[baseVertex + 0].Color = color;

            // 右下
            verts[baseVertex + 1].Position = new Vector3(position.x + halfW, position.y - halfH, 0f);
            verts[baseVertex + 1].UV = new Vector2(uv.xMax, uv.yMin);
            verts[baseVertex + 1].Color = color;

            // 右上
            verts[baseVertex + 2].Position = new Vector3(position.x + halfW, position.y + halfH, 0f);
            verts[baseVertex + 2].UV = new Vector2(uv.xMax, uv.yMax);
            verts[baseVertex + 2].Color = color;

            // 左上
            verts[baseVertex + 3].Position = new Vector3(position.x - halfW, position.y + halfH, 0f);
            verts[baseVertex + 3].UV = new Vector2(uv.xMin, uv.yMax);
            verts[baseVertex + 3].Color = color;
        }
    }
}
