using MiniGameTemplate.Utils;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸发射器——将 PatternSO 配置翻译为 BulletCore + BulletTrail + BulletModifier 写入。
    /// 无状态 static 类，所有方法接收外部依赖作为参数。
    /// </summary>
    public static class BulletSpawner
    {
        /// <summary>
        /// 发射一组弹丸（单次，不含 Burst 连射）。
        /// PatternScheduler 的 Burst 连射通过多次调用本方法实现。
        /// </summary>
        public static void Fire(
            BulletPatternSO pattern,
            Vector2 origin,
            float baseAngleDeg,
            BulletWorld world,
            DanmakuTypeRegistry registry,
            DifficultyProfileSO difficulty = null)
        {
            var type = pattern.BulletType;
            if (type == null) return;

            int count = pattern.Count;
            float speed = pattern.Speed;

            // 难度乘数
            if (difficulty != null)
            {
                count = Mathf.RoundToInt(count * difficulty.CountMultiplier);
                speed *= difficulty.SpeedMultiplier;
            }

            float spreadAngle = pattern.SpreadAngle;
            float startAngle = baseAngleDeg + pattern.StartAngle;
            float step = count > 1 ? spreadAngle / count : 0f;
            float halfSpread = spreadAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int slot = world.Allocate();
                if (slot == -1)
                {
                    GameLog.LogWarning("[Danmaku] BulletWorld full, discarding remaining bullets");
                    return;
                }

                float angleDeg = startAngle - halfSpread + step * i + step * 0.5f;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector2 velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * speed;

                // 写入 BulletCore（热数据）
                ref var core = ref world.Cores[slot];
                core.Position = origin;
                core.Velocity = velocity;
                core.Lifetime = pattern.Lifetime * (difficulty?.LifetimeMultiplier ?? 1f);
                core.Elapsed = 0;
                core.Radius = type.CollisionRadius;
                core.TypeIndex = type.RuntimeIndex;
                core.Phase = (byte)BulletPhase.Active;
                core.HitPoints = type.InitialHitPoints;
                core.Flags = BulletCore.FLAG_ACTIVE;
                core.Faction = (byte)type.Faction;
                core.LastHitId = 0;

                // 条件标记
                if (type.RotateToDirection)
                    core.Flags |= BulletCore.FLAG_ROTATE_TO_DIR;
                if (pattern.IsHoming)
                    core.Flags |= BulletCore.FLAG_HOMING;
                if (type.Trail == TrailMode.Trail || type.Trail == TrailMode.Both)
                    core.Flags |= BulletCore.FLAG_HEAVY_TRAIL;
                if (type.ChildPattern != null)
                    core.Flags |= BulletCore.FLAG_HAS_CHILD;

                // 写入 BulletTrail（冷数据）
                ref var trail = ref world.Trails[slot];
                trail.TrailLength = (type.Trail == TrailMode.Ghost || type.Trail == TrailMode.Both)
                    ? type.GhostCount : (byte)0;
                trail.PrevPos1 = trail.PrevPos2 = trail.PrevPos3 = origin;

                // 写入 BulletModifier（如果有延迟变速/追踪延迟）
                bool hasModifier = pattern.DelayBeforeAccel > 0 || pattern.HomingDelay > 0;
                if (hasModifier)
                {
                    core.Flags |= BulletCore.FLAG_HAS_MODIFIER;
                    ref var mod = ref world.Modifiers[slot];
                    mod.DelayEndTime = pattern.DelayBeforeAccel;
                    mod.DelaySpeedScale = pattern.DelaySpeedScale;
                    mod.AccelEndTime = pattern.DelayBeforeAccel + pattern.AccelDuration;
                    mod.HomingStartTime = pattern.HomingDelay;
                }

                // SpeedOverLifetime 曲线（与延迟变速互斥）
                if (!hasModifier && type.SpeedOverLifetime != null && type.SpeedOverLifetime.keys.Length > 1)
                    core.Flags |= BulletCore.FLAG_SPEED_CURVE;
            }
        }
    }
}
