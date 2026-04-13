using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 正弦波运动策略——垂直于飞行方向叠加正弦偏移。
    /// 使用 BulletModifier 空闲字段：
    /// - HomingStartTime → SineAmplitude（振幅，世界单位）
    /// - HomingStrength → SineFrequency（频率，Hz）
    /// </summary>
    public static class SineWaveMotionStrategy
    {
        /// <summary>振幅存储字段（复用 BulletModifier.HomingStartTime）</summary>
        public const float DEFAULT_AMPLITUDE = 1.0f;

        /// <summary>频率存储字段（复用 BulletModifier.HomingStrength）</summary>
        public const float DEFAULT_FREQUENCY = 3.0f;

        public static void Execute(
            ref BulletCore core,
            ref BulletModifier modifier,
            BulletTypeSO type,
            Vector2 playerPos,
            float dt)
        {
            // 从 Modifier 空闲字段读取参数
            float amplitude = modifier.HomingStartTime > 0f ? modifier.HomingStartTime : DEFAULT_AMPLITUDE;
            float frequency = modifier.HomingStrength > 0f ? modifier.HomingStrength : DEFAULT_FREQUENCY;

            // 速度倍率（支持延迟变速/速度曲线）
            float speedMultiplier = 1f;
            if ((core.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
            {
                speedMultiplier = CalculateModifierSpeed(core.Elapsed, ref modifier);
            }
            else if ((core.Flags & BulletCore.FLAG_SPEED_CURVE) != 0)
            {
                float normalizedTime = core.Elapsed / core.Lifetime;
                speedMultiplier = type.SpeedOverLifetime.Evaluate(normalizedTime);
            }

            // 飞行方向（单位向量）
            float speed = core.Velocity.magnitude;
            if (speed < 0.001f)
            {
                core.Position += core.Velocity * speedMultiplier * dt;
                return;
            }

            Vector2 forward = core.Velocity / speed;
            // 垂直方向（逆时针旋转 90 度）
            Vector2 perpendicular = new Vector2(-forward.y, forward.x);

            // 正弦偏移量（基于 Elapsed 累积时间）
            float prevPhase = (core.Elapsed - dt) * frequency * Mathf.PI * 2f;
            float currPhase = core.Elapsed * frequency * Mathf.PI * 2f;
            float sineOffset = (Mathf.Sin(currPhase) - Mathf.Sin(prevPhase)) * amplitude;

            // 位置更新 = 直线运动 + 正弦横向偏移
            core.Position += forward * (speed * speedMultiplier * dt) + perpendicular * sineOffset;
        }

        private static float CalculateModifierSpeed(float elapsed, ref BulletModifier mod)
        {
            if (elapsed < mod.DelayEndTime)
                return mod.DelaySpeedScale;
            else if (elapsed < mod.AccelEndTime)
            {
                float t = (elapsed - mod.DelayEndTime) / (mod.AccelEndTime - mod.DelayEndTime);
                return Mathf.Lerp(mod.DelaySpeedScale, 1f, t);
            }
            return 1f;
        }
    }
}
