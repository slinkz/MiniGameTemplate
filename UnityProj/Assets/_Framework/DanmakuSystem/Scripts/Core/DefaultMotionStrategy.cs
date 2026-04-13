using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 默认运动策略——封装现有 BulletMover 的延迟变速 + 速度曲线 + 追踪逻辑。
    /// </summary>
    public static class DefaultMotionStrategy
    {
        public static void Execute(
            ref BulletCore core,
            ref BulletModifier modifier,
            BulletTypeSO type,
            Vector2 playerPos,
            float dt)
        {
            // 延迟变速（Modifier 冷数据）
            float speedMultiplier = 1f;
            if ((core.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
            {
                speedMultiplier = CalculateModifierSpeed(core.Elapsed, ref modifier);
            }
            // 速度曲线（与延迟变速互斥）
            else if ((core.Flags & BulletCore.FLAG_SPEED_CURVE) != 0)
            {
                float normalizedTime = core.Elapsed / core.Lifetime;
                speedMultiplier = type.SpeedOverLifetime.Evaluate(normalizedTime);
            }

            // 追踪（实时转向）
            if ((core.Flags & BulletCore.FLAG_HOMING) != 0)
            {
                bool shouldTrack = true;
                if ((core.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
                {
                    if (core.Elapsed < modifier.HomingStartTime)
                        shouldTrack = false;
                }

                if (shouldTrack)
                {
                    Vector2 toPlayer = playerPos - core.Position;
                    if (toPlayer.sqrMagnitude > 0.001f)
                    {
                        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
                        float currentAngle = Mathf.Atan2(core.Velocity.y, core.Velocity.x);
                        float homingDegPerSec = modifier.HomingStrength;
                        float maxTurn = homingDegPerSec * Mathf.Deg2Rad * dt;
                        float angleDiff = Mathf.DeltaAngle(
                            currentAngle * Mathf.Rad2Deg,
                            targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        float turn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);
                        float newAngle = currentAngle + turn;
                        float speedSq = core.Velocity.sqrMagnitude;
                        float speed = speedSq > 0.001f ? Mathf.Sqrt(speedSq) : 0f;
                        core.Velocity = new Vector2(
                            Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * speed;
                    }
                }
            }

            // 位置更新
            core.Position += core.Velocity * speedMultiplier * dt;
        }

        private static float CalculateModifierSpeed(float elapsed, ref BulletModifier mod)
        {
            if (elapsed < mod.DelayEndTime)
            {
                return mod.DelaySpeedScale;
            }
            else if (elapsed < mod.AccelEndTime)
            {
                float t = (elapsed - mod.DelayEndTime) / (mod.AccelEndTime - mod.DelayEndTime);
                return Mathf.Lerp(mod.DelaySpeedScale, 1f, t);
            }
            else
            {
                return 1f;
            }
        }
    }
}
