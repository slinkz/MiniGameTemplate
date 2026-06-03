using System.Collections.Generic;
using MiniGameTemplate.Danmaku;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 通用飘字系统——环形缓冲区 (128) + RenderBatchManager 纯 GPU 渲染。
    /// 独立于 Entity/Danmaku 业务逻辑，任何系统均可通过 Spawn 显示飘字。
    /// 
    /// 生命周期由 DanmakuSystem 管理：
    ///   Initialize → 每帧 Rebuild → ClearAll → Dispose
    /// </summary>
    public class FloatingTextSystem
    {
        public const int MAX_NUMBERS = 128;
        private const int MAX_DIGITS_PER_NUMBER = 5;
        private const int DIGIT_COUNT = 10;
        private const float DIGIT_UV_WIDTH = 1f / DIGIT_COUNT;
        private const float DIGIT_SIZE = 0.3f;
        private const float DIGIT_SPACING = 0.2f;
        private const float FLOAT_SPEED = 1.5f;
        private const float FADE_START = 0.6f;

        private readonly FloatingTextData[] _buffer = new FloatingTextData[MAX_NUMBERS];
        private int _head;
        private int _count;

        private RenderBatchManager _batchManager;
        private RuntimeAtlasManager _runtimeAtlas;
        private Texture2D _fallbackAtlas;
        private int _totalQuadCount;

        /// <summary>当前活跃飘字产生的 Quad 总数（Debug HUD 用）</summary>
        public int TotalDrawCount => _totalQuadCount;

        /// <summary>
        /// 初始化渲染资源。由 DanmakuSystem.InitializeSubsystems() 调用。
        /// </summary>
        /// <param name="renderConfig">弹幕渲染配置（提供 NumberAtlas + BulletMaterial）</param>
        /// <param name="sharedAtlas">共享 RuntimeAtlasManager（可选，null 则 fallback 原始贴图）</param>
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
        /// 生成一个伤害飘字。线程安全：否（仅主线程调用）。
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <param name="damage">伤害数值（0~99999，超出截断为 5 位）</param>
        /// <param name="color">飘字颜色</param>
        /// <param name="isCritical">暴击标记（true → 1.5x 缩放 + 放大动画）</param>
        public void Spawn(Vector2 position, int damage, Color32 color, bool isCritical = false)
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
            data.Color = color;

            _head = (_head + 1) % MAX_NUMBERS;
            if (_count < MAX_NUMBERS)
                _count++;
        }

        /// <summary>
        /// 每帧更新位置/透明度 + 重建 GPU 批次。
        /// 由 DanmakuSystem.RunLateUpdatePipeline() 调用，传入 unscaledDeltaTime。
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

        /// <summary>清除所有活跃飘字（战斗退场/关卡切换）。</summary>
        public void ClearAll()
        {
            for (int i = 0; i < MAX_NUMBERS; i++)
                _buffer[i].Lifetime = 0f;

            _head = 0;
            _count = 0;
        }

        /// <summary>释放 GPU 资源。</summary>
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

        private void WriteNumber(RenderBatchManager.RenderBucket bucket, in FloatingTextData data, float alpha, Rect atlasUv)
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

    /// <summary>
    /// 飘字预定义颜色常量——避免颜色魔法数字散落各处。
    /// </summary>
    public static class FloatingTextColors
    {
        public static readonly Color32 Normal   = new Color32(255, 255, 255, 255); // 白色
        public static readonly Color32 Critical = new Color32(255, 200, 50, 255);  // 暴击金
        public static readonly Color32 Dot      = new Color32(153, 51, 255, 255);  // DOT 紫
        public static readonly Color32 Heal     = new Color32(50, 255, 100, 255);  // 治疗绿（预留）
    }
}
