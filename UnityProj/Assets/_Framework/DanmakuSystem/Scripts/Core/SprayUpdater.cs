namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 喷雾更新——纯 static 工具类。
    /// 负责：Elapsed 推进、TickTimer 推进、生命周期回收。
    /// 喷雾的视觉效果由对象池 ParticleSystem 驱动，Updater 只管逻辑。
    /// </summary>
    public static class SprayUpdater
    {
        public static void UpdateAll(SprayPool pool, float dt)
        {
            for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
            {
                ref var spray = ref pool.Data[i];
                if (spray.Phase == 0) continue;  // 未激活

                spray.Elapsed += dt;

                if (spray.Elapsed >= spray.Lifetime)
                {
                    pool.Free(i);
                    continue;
                }

                // 伤害 tick（碰撞检测在 CollisionSolver 中统一处理）
                spray.TickTimer += dt;
                if (spray.TickTimer >= spray.TickInterval)
                    spray.TickTimer -= spray.TickInterval;
            }
        }
    }
}
