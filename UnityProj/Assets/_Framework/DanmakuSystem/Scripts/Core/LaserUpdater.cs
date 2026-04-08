namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光更新——纯 static 工具类，每帧由 DanmakuSystem.Update 调用。
    /// 负责：Phase 推进（Charging → Firing → Fading）、宽度曲线驱动、TickTimer 推进。
    /// </summary>
    public static class LaserUpdater
    {
        public static void UpdateAll(LaserPool pool, DanmakuTypeRegistry registry, float dt)
        {
            for (int i = 0; i < LaserPool.MAX_LASERS; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase == 0) continue;  // 未激活

                laser.Elapsed += dt;
                var type = registry.LaserTypes[laser.LaserTypeIndex];

                // Phase 推进
                if (laser.Elapsed < type.ChargeDuration)
                {
                    // Charging: 细线闪烁，不造成伤害
                    laser.Width = type.MaxWidth * 0.05f;
                }
                else if (laser.Elapsed < type.ChargeDuration + type.FiringDuration)
                {
                    // Firing: 宽度曲线驱动 + 伤害 tick
                    float normalizedTime = laser.Elapsed / type.TotalDuration;
                    laser.Width = type.WidthOverLifetime.Evaluate(normalizedTime) * type.MaxWidth;

                    // 伤害 tick（TickTimer 始终推进，伤害由碰撞检测统一处理）
                    laser.TickTimer += dt;
                    if (laser.TickTimer >= laser.TickInterval)
                    {
                        laser.TickTimer -= laser.TickInterval;
                    }
                }
                else if (laser.Elapsed < type.TotalDuration)
                {
                    // Fading: 宽度递减，不造成伤害
                    float normalizedTime = laser.Elapsed / type.TotalDuration;
                    laser.Width = type.WidthOverLifetime.Evaluate(normalizedTime) * type.MaxWidth;
                }
                else
                {
                    // 生命周期结束，回收
                    pool.Free(i);
                }
            }
        }
    }
}
