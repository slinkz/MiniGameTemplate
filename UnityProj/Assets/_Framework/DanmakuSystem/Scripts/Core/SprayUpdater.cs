using MiniGameTemplate.VFX;
using UnityEngine;


namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 喷雾更新——纯 static 工具类。
    /// 负责：挂载源同步、Elapsed 推进、TickTimer 推进、生命周期回收。
    /// Phase 3：集成 VFX 附着模式，喷雾可用 Sprite Sheet VFX 替代 ParticleSystem。
    /// </summary>
    public static class SprayUpdater
    {
        public static void UpdateAll(
            SprayPool pool,
            AttachSourceRegistry attachRegistry,
            DanmakuTypeRegistry typeRegistry,
            SpriteSheetVFXSystem vfxSystem,
            float dt)
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
                    FreeSpray(pool, attachRegistry, vfxSystem, i);
                    continue;
                }

                // ── VFX 启动（首帧，VfxSlot == -1 时尝试启动） ──
                if (spray.VfxSlot < 0 && vfxSystem != null && typeRegistry != null)
                {
                    var sprayType = typeRegistry.SprayTypes[spray.SprayTypeIndex];
                    if (sprayType != null && sprayType.SprayVFXType != null)
                    {
                        bool followTarget = sprayType.SprayVFXType.AttachMode == VFXAttachMode.FollowTarget && spray.AttachId != 0;
                        spray.VfxSlot = followTarget
                            ? vfxSystem.PlayAttached(sprayType.SprayVFXType, spray.AttachId, 1f)
                            : vfxSystem.Play(sprayType.SprayVFXType, spray.Origin, 1f, spray.Direction * Mathf.Rad2Deg);
                    }
                }


                // 注意：TickTimer 推进在 CollisionSolver.SolveSprays 中完成，
                // 避免与 tick 判断分处两地导致时序 bug。
            }
        }

        /// <summary>
        /// 回收喷雾——先停止附着 VFX，再释放挂载源引用，最后归还池槽位。
        /// </summary>
        public static void FreeSpray(
            SprayPool pool,
            AttachSourceRegistry attachRegistry,
            SpriteSheetVFXSystem vfxSystem,
            int index)
        {
            ref var spray = ref pool.Data[index];

            // 停止附着 VFX
            if (spray.VfxSlot >= 0 && vfxSystem != null)
            {
                vfxSystem.StopAttached(spray.VfxSlot);
                spray.VfxSlot = -1;
            }

            byte attachId = spray.AttachId;
            if (attachId != 0)
                attachRegistry.Release(attachId);
            pool.Free(index);
        }
    }
}
