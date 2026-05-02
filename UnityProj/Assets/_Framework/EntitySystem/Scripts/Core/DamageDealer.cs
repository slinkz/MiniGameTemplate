using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 直接伤害工具类——不走弹幕系统的伤害路径。
    /// 用途：AOE 技能、光环伤害、陷阱、环境伤害、治疗等。
    /// 
    /// 所有方法均为静态，无状态，零 GC（使用预分配 buffer）。
    /// (v0.4 SA-001) 模仿 Physics2D API 风格，静态工具类设计。
    /// </summary>
    public static class DamageDealer
    {
        // ATK-003：buffer 开 64 的理由：
        // - maxTargets=16 是默认值，调用方可传更大值
        // - 64 = EntityPool 默认最大容量的一半（预留裕量）
        // - 只有 Entity 引用（64 × 8 bytes = 512B），内存开销可忽略
        private static readonly Entity[] _buffer = new Entity[64];

        // UA-003：重入保护。
        // SA-009：Unity 协程是协作式调度（非抢占），同帧内不会真正并行，
        // 重入保护对协程场景有效。
        private static bool _isProcessingArea;

        /// <summary>
        /// 对单个 Entity 直接造伤。走完整 TakeDamage 管线。
        /// </summary>
        public static void DealDamageToEntity(Entity target, DamageContext context)
        {
            if (target == null || !target.IsAlive || target.IsPendingDespawn) return;

            var health = target.GetComponent(ComponentType.Health) as HealthComponent;
            if (health == null) return;

            health.TakeDamage(ref context);
        }

        /// <summary>
        /// 对范围内指定阵营的 Entity 造伤。返回实际命中数。
        /// 注意：不支持嵌套调用（UA-003 行为约束）。
        /// </summary>
        public static int DealAreaDamage(
            Vector2 center, float radius, EnumCamp targetCamp,
            DamageContext baseContext, int maxTargets = 16)
        {
            // UA-003：重入检测
            Debug.Assert(!_isProcessingArea,
                "[DamageDealer] DealAreaDamage 不支持嵌套调用！请检查 OnDeath 回调链。");
            if (_isProcessingArea) return 0;

            var mgr = EntityManagerAccessor.Instance;
            Debug.Assert(mgr != null, "[DamageDealer] EntityManager not initialized!");
            if (mgr == null) return 0;

            _isProcessingArea = true;
            int hitCount = 0;
            try
            {
                int count = mgr.FindEntitiesInRadius(center, radius, targetCamp, _buffer,
                    Mathf.Min(maxTargets, _buffer.Length));

                for (int i = 0; i < count; i++)
                {
                    // v0.4 SA-006：循环中检查——前序目标的 OnDeath 可能导致后序目标被标记回收
                    if (_buffer[i].IsPendingDespawn || !_buffer[i].IsAlive) continue;

                    var ctx = baseContext; // struct 值拷贝，每个目标独立 context
                    var health = _buffer[i].GetComponent(ComponentType.Health) as HealthComponent;
                    if (health != null)
                    {
                        health.TakeDamage(ref ctx);
                        hitCount++;
                    }
                }
            }
            finally
            {
                _isProcessingArea = false;
            }

            return hitCount;
        }
    }
}
