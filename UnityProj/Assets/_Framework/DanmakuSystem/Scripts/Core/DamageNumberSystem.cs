using System.Collections.Generic;
using MiniGameTemplate.Rendering;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 伤害飘字系统——环形缓冲区 (128) + RenderBatchManager 提交。
    /// R3.1：迁移到 RuntimeAtlas / RBM，数字 UV 基于 DamageText Channel 的分配结果重映射。
    /// </summary>
    public class DamageNumberSystem
    {
        public const int MAX_NUMBERS = 128;
        private const int MAX_DIGITS_PER_NUMBER = 5;
        private const int DIGIT_COUNT = 10;
        private const float DIGIT_UV_WIDTH = 1f / DIGIT_COUNT;
        private const float DIGIT_SIZE = 0.3f;
        private const float DIGIT_SPACING = 0.2f;
        private const float FLOAT_SPEED = 1.5f;
        private const float FADE_START = 0.6f;

        private readonly DamageNumberData[] _buffer = new DamageNumberData[MAX_NUMBERS];
        private int _head;
        private int _count;

        private RenderBatchManager _batchManager;
        private RuntimeAtlasManager _runtimeAtlas;
        private Texture2D _fallbackAtlas;
        private int _totalQuadCount;

        public int TotalDrawCount => _totalQuadCount;

        /// <summary>
        /// PI-001: 接收 DanmakuSystem 持有的共享 RuntimeAtlasManager。
        /// </summary>
        public void Initialize(DanmakuRenderConfig renderConfig, RuntimeAtlasManager sharedAtlas = null)
        {
            _batchManager = new RenderBatchManager();
            _fallbackAtlas = renderConfig != null ? renderConfig.NumberAtlas : null;

            // PI-001: 使用共享 Atlas 实例
            _runtimeAtlas = sharedAtlas;

            var registrations = new List<RenderBatchManager.BucketRegistration>();
            var binding = ResolveBinding();
            if (binding.IsValid && renderConfig != null && renderConfig.BulletMaterial != null)
            {
                registrations.Add(new RenderBatchManager.BucketRegistration(
                    new RenderBatchManager.BucketKey(RenderLayer.Normal, binding.Texture),
                    renderConfig.BulletMaterial,
                    RenderSortingOrder.DamageNumber));
            }

            _batchManager.Initialize(registrations, MAX_NUMBERS * MAX_DIGITS_PER_NUMBER);
        }

        /// <summary>
        /// 生成一个伤害飘字。
        /// </summary>
        public void Spawn(Vector2 position, int damage, bool isCritical = false)
        {
            ref var data = ref _buffer[_head];
            data.Position = position;
            data.Velocity = new Vector2(Random.Range(-0.3f, 0.3f), FLOAT_SPEED);
            data.Lifetime = 0.8f;
            data.Elapsed = 0f;
            data.Damage = damage;
            data.DigitCount = CountDigits(damage);
            data.Flags = isCritical ? (byte)1 : (byte)0;
            data.Scale = isCritical ? 1.5f : 1f;
            data.Color = isCritical
                ? new Color32(255, 200, 50, 255)
                : new Color32(255, 255, 255, 255);

            _head = (_head + 1) % MAX_NUMBERS;
            if (_count < MAX_NUMBERS)
                _count++;
        }

        /// <summary>
        /// 每帧更新 + 重建批次。由 DanmakuSystem.LateUpdate 调用。
        /// </summary>
        public void Rebuild(float dt)
        {
            _batchManager.ResetAll();
            _totalQuadCount = 0;

            var binding = ResolveBinding();
            if (!binding.IsValid)
            {
                _batchManager.UploadAndDrawAll();
                return;
            }

            var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, binding.Texture);
            if (!_batchManager.TryGetBucket(bucketKey, out var bucket))
            {
                _batchManager.UploadAndDrawAll();
                return;
            }

            for (int i = 0; i < MAX_NUMBERS; i++)
            {
                ref var data = ref _buffer[i];
                if (data.Lifetime <= 0f)
                    continue;

                data.Elapsed += dt;
                if (data.Elapsed >= data.Lifetime)
                {
                    data.Lifetime = 0f;
                    _count--;
                    continue;
                }

                float t = data.Elapsed / data.Lifetime;
                float speedFactor = 1f - t * 0.5f;
                data.Position += data.Velocity * speedFactor * dt;

                float alpha = t > FADE_START
                    ? 1f - (t - FADE_START) / (1f - FADE_START)
                    : 1f;

                WriteNumber(bucket, data, alpha, binding.UVRect);
            }

            _batchManager.UploadAndDrawAll();
        }

        /// <summary>获取 RuntimeAtlas 统计快照（无 Atlas 时返回 null）。
        /// CR-06: 共享 Atlas 后此方法返回全局统计，建议使用 DanmakuSystem.GetAllAtlasStats() 代替。</summary>
        [System.Obsolete("Use DanmakuSystem.GetAllAtlasStats() for shared atlas stats.")]
        public RuntimeAtlasStats? GetAtlasStats()
        {
            return _runtimeAtlas != null && _runtimeAtlas.IsInitialized
                ? _runtimeAtlas.GetStats()
                : (RuntimeAtlasStats?)null;
        }

        public void Dispose()
        {
            _batchManager?.Dispose();
            // PI-001: 共享 Atlas 由 DanmakuSystem 统一 Dispose
            _runtimeAtlas = null;
        }

        private RuntimeAtlasBindingResolver.ResolvedTextureBinding ResolveBinding()
        {
            if (_runtimeAtlas != null && _runtimeAtlas.IsInitialized && _fallbackAtlas != null)
            {
                AtlasAllocation allocation = _runtimeAtlas.Allocate(AtlasChannel.DamageText, _fallbackAtlas);
                if (allocation.Valid)
                {
                    RenderTexture atlasTexture = _runtimeAtlas.GetAtlasTexture(AtlasChannel.DamageText, allocation.PageIndex);
                    if (atlasTexture != null)
                        return new RuntimeAtlasBindingResolver.ResolvedTextureBinding(atlasTexture, allocation.UVRect, true);
                }
            }

            if (_fallbackAtlas != null)
                return new RuntimeAtlasBindingResolver.ResolvedTextureBinding(_fallbackAtlas, new Rect(0f, 0f, 1f, 1f), false);

            return default;
        }

        private void WriteNumber(RenderBatchManager.RenderBucket bucket, in DamageNumberData data, float alpha, Rect atlasUv)
        {
            int damage = data.Damage;
            int digits = data.DigitCount;
            float totalWidth = digits * DIGIT_SPACING * data.Scale;
            float startX = data.Position.x - totalWidth * 0.5f;

            int divisor = 1;
            for (int d = 1; d < digits; d++)
                divisor *= 10;

            for (int d = 0; d < digits; d++)
            {
                int digit = (damage / divisor) % 10;
                divisor /= 10;

                float x = startX + d * DIGIT_SPACING * data.Scale;
                float halfSize = DIGIT_SIZE * 0.5f * data.Scale;

                float pixelWidth = _fallbackAtlas != null ? _fallbackAtlas.width : 0f;
                float digitPixelWidth = pixelWidth / DIGIT_COUNT;
                float localUvLeft = digitPixelWidth > 0f ? digit * digitPixelWidth / pixelWidth : digit * DIGIT_UV_WIDTH;
                float localUvRight = digitPixelWidth > 0f ? (digit + 1) * digitPixelWidth / pixelWidth : localUvLeft + DIGIT_UV_WIDTH;
                float uvLeft = atlasUv.x + localUvLeft * atlasUv.width;
                float uvRight = atlasUv.x + localUvRight * atlasUv.width;
                float uvBottom = atlasUv.y;
                float uvTop = atlasUv.y + atlasUv.height;

                int baseVertex = bucket.AllocateQuad();
                if (baseVertex < 0)
                    return;

                _totalQuadCount++;

                byte a = (byte)(alpha * data.Color.a);
                var color = new Color32(data.Color.r, data.Color.g, data.Color.b, a);
                var verts = bucket.Vertices;

                verts[baseVertex + 0] = new RenderVertex
                {
                    Position = new Vector3(x - halfSize, data.Position.y - halfSize, 0f),
                    Color = color,
                    UV = new Vector2(uvLeft, uvBottom),
                };
                verts[baseVertex + 1] = new RenderVertex
                {
                    Position = new Vector3(x + halfSize, data.Position.y - halfSize, 0f),
                    Color = color,
                    UV = new Vector2(uvRight, uvBottom),
                };
                verts[baseVertex + 2] = new RenderVertex
                {
                    Position = new Vector3(x + halfSize, data.Position.y + halfSize, 0f),
                    Color = color,
                    UV = new Vector2(uvRight, uvTop),
                };
                verts[baseVertex + 3] = new RenderVertex
                {
                    Position = new Vector3(x - halfSize, data.Position.y + halfSize, 0f),
                    Color = color,
                    UV = new Vector2(uvLeft, uvTop),
                };
            }
        }

        private static byte CountDigits(int value)
        {
            if (value < 0)
                value = -value;
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            return 5;
        }
    }
}
