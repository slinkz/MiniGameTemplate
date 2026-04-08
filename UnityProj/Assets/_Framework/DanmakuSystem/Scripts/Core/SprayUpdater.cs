namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 喷雾更新——纯 static 工具类。
    /// 负责：挂载源同步、Elapsed 推进、TickTimer 推进、生命周期回收。
    /// 喷雾的视觉效果由对象池 ParticleSystem 驱动，Updater 只管逻辑。
    /// </summary>
    public static class SprayUpdater
    {
        public static void UpdateAll(SprayPool pool, AttachSourceRegistry attachRegistry, float dt)
        {
            for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
            {
                ref var spray = ref pool.Data[i];
                if (spray.Phase == 0) continue;  // 未激活

                // ── 挂载源同步：每帧回写 Origin + Direction ──
                if (spray.AttachId != 0)
                {
                    spray.Origin = attachRegistry.GetWorldPosition(spray.AttachId, spray.Origin);
                    spray.Direction = attachRegistry.GetWorldAngle(spray.AttachId, spray.Direction);
                }

                spray.Elapsed += dt;

                if (spray.Elapsed >= spray.Lifetime)
                {
                    FreeSpray(pool, attachRegistry, i);
                    continue;
                }

                // 伤害 tick（碰撞检测在 CollisionSolver 中统一处理）
                spray.TickTimer += dt;
                if (spray.TickTimer >= spray.TickInterval)
                    spray.TickTimer -= spray.TickInterval;
            }
        }

        /// <summary>
        /// 回收喷雾——先释放挂载源引用，再归还池槽位。
        /// </summary>
        public static void FreeSpray(SprayPool pool, AttachSourceRegistry attachRegistry, int index)
        {
            byte attachId = pool.Data[index].AttachId;
            if (attachId != 0)
                attachRegistry.Release(attachId);
            pool.Free(index);
        }
    }
}
