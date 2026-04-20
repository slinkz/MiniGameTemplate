using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 螺旋运动策略——持续转向 + 速度曲线。
    /// 使用 BulletModifier 空闲字段：
    /// - HomingStrength → AngularVelocity（角速度，度/秒）
    /// </summary>
    public static class SpiralMotionStrategy
    {
        /// <summary>默认角速度（度/秒）</summary>
        public const float DEFAULT_ANGULAR_VELOCITY = 180f;

        public static void Execute(
            ref BulletCore core,
            ref BulletModifier modifier,
            BulletTypeSO type,
            Vector2 playerPos,
            float dt)
        {
            // 从 Modifier 空闲字段读取角速度
            float angularVelocity = modifier.HomingStrength != 0f
                ? modifier.HomingStrength
                : DEFAULT_ANGULAR_VELOCITY;

            // 速度倍率
            float speedMultiplier = 1f;
            if ((core.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
            {
                speedMultiplier = MotionUtility.CalculateModifierSpeed(core.Elapsed, ref modifier);
            }
            else if ((core.Flags & BulletCore.FLAG_SPEED_CURVE) != 0)
            {
                float normalizedTime = core.Elapsed / core.Lifetime;
                speedMultiplier = type.SpeedOverLifetime.Evaluate(normalizedTime);
            }

            // 持续转向
            float speed = core.Velocity.magnitude;
            if (speed < 0.001f)
            {
                core.Position += core.Velocity * speedMultiplier * dt;
                return;
            }

            float currentAngle = Mathf.Atan2(core.Velocity.y, core.Velocity.x);
            float turnAmount = angularVelocity * Mathf.Deg2Rad * dt;
            float newAngle = currentAngle + turnAmount;

            // 更新速度方向（保持速度大小不变）
            core.Velocity = new Vector2(
                Mathf.Cos(newAngle),
                Mathf.Sin(newAngle)) * speed;

            // 位置更新
            core.Position += core.Velocity * speedMultiplier * dt;
        }
    }
}
