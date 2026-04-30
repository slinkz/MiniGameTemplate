using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 全局管理器——管理所有 EntityPool，统一驱动 Tick。
    /// 非 MonoBehaviour，由游戏层（EntitySystemBootstrap）在 Update 中调用。
    /// Phase 1 以 EntityConfigSO 为主键；Phase 2 可增加 Spawn(int configId,...) 重载。
    /// 
    /// BC-06 契约实现：
    /// - BC-06.1：统一驱动所有活跃 Entity 的 Tick，不依赖 MonoBehaviour.Update
    /// - BC-06.2：Tickable 组件按 TickOrder 升序执行（Entity 内部排序）
    /// - BC-06.4：由外部 MonoBehaviour 控制调用时机
    /// 
    /// v2.1 EC-005/EC-013：延迟销毁 + swap-remove。
    /// v2.3 SA-006：CountAliveByConfig API。
    /// </summary>
    public class EntityManager
    {
        private readonly Dictionary<EntityConfigSO, EntityPool> _pools = new();
        private readonly List<Entity> _activeEntities = new();
        private readonly List<Entity> _pendingDespawn = new();
        private bool _isTicking;

        // ──────────── ID 分配器 ────────────

        private uint _nextId = 1;

        /// <summary>分配唯一 EntityId（从 1 开始递增）</summary>
        private EntityId AllocateId() => new EntityId(_nextId++);

        // ──────────── 公共 API ────────────

        /// <summary>当前活跃 Entity 数量</summary>
        public int ActiveCount => _activeEntities.Count;

        /// <summary>
        /// 每帧驱动所有活跃 Entity。
        /// Phase A：Tick 所有活跃 Entity。
        /// Phase B：统一处理延迟销毁。
        /// </summary>
        public void Tick(float dt)
        {
            _isTicking = true;

            // Phase A: Tick 所有活跃 Entity
            for (int i = 0; i < _activeEntities.Count; i++)
            {
                var entity = _activeEntities[i];
                if (entity.IsPaused)
                {
                    entity.DecrementPauseFrames();
                    continue;
                }
                entity.Tick(dt);
            }

            _isTicking = false;

            // Phase B: 统一处理延迟销毁（Tick 期间 Despawn 只标记不执行）
            if (_pendingDespawn.Count > 0)
            {
                for (int i = 0; i < _pendingDespawn.Count; i++)
                {
                    ExecuteDespawn(_pendingDespawn[i]);
                }
                _pendingDespawn.Clear();
            }
        }

        /// <summary>
        /// 从指定配置的池取出 Entity（Phase 1 主 API）。
        /// </summary>
        /// <param name="config">配置 SO</param>
        /// <param name="position">初始位置</param>
        /// <param name="rotation">初始朝向角度</param>
        /// <returns>已初始化的 Entity，或 null（池满）</returns>
        public Entity Spawn(EntityConfigSO config, Vector2 position, float rotation)
        {
            var pool = GetOrCreatePool(config);
            var entity = pool.Acquire(position, rotation);
            if (entity != null)
            {
                entity.Id = AllocateId();
                entity.ActiveListIndex = _activeEntities.Count;
                _activeEntities.Add(entity);
            }
            return entity;
        }

        /// <summary>
        /// 回收 Entity（延迟模式：Tick 期间调用只加入待销毁队列，帧尾统一执行）。
        /// Tick 外调用则立即执行。
        /// </summary>
        public void Despawn(Entity entity)
        {
            if (entity == null || !entity.IsAlive) return;

            if (_isTicking)
            {
                entity.MarkPendingDespawn();
                _pendingDespawn.Add(entity);
            }
            else
            {
                ExecuteDespawn(entity);
            }
        }

        /// <summary>
        /// 查询指定配置类型的存活 Entity 数量（排除 PendingDespawn）。
        /// v2.3（SA-006）：供 EntitySpawner 的 AllCleared 触发模式使用。
        /// </summary>
        public int CountAliveByConfig(EntityConfigSO config)
        {
            int count = 0;
            for (int i = 0; i < _activeEntities.Count; i++)
            {
                var e = _activeEntities[i];
                if (e.ConfigSO == config && !e.IsPendingDespawn)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Phase 3 预留：按半径搜索指定阵营的 Entity（零 GC，使用预分配结果缓冲区）。
        /// </summary>
        public int FindEntitiesInRadius(
            Vector2 center, float radius, Danmaku.EnumCamp camp,
            Entity[] resultBuffer, int maxResults)
        {
            // Phase 3 实现：线性扫描 _activeEntities，如需优化改为空间分区
            throw new System.NotImplementedException("Phase 3");
        }

        // ──────────── 内部方法 ────────────

        /// <summary>实际销毁：swap-remove O(1) + 归还池</summary>
        private void ExecuteDespawn(Entity entity)
        {
            // swap-remove: 将最后一个 Entity 移到被删位置
            int idx = entity.ActiveListIndex;
            int last = _activeEntities.Count - 1;
            if (idx != last)
            {
                _activeEntities[idx] = _activeEntities[last];
                _activeEntities[idx].ActiveListIndex = idx;
            }
            _activeEntities.RemoveAt(last);

            // 归还池
            if (_pools.TryGetValue(entity.ConfigSO, out var pool))
            {
                pool.Release(entity);
            }
            else
            {
                Debug.LogError($"[EntityManager] 找不到 Entity 对应的池：{entity.ConfigSO?.name}");
            }
        }

        /// <summary>获取或按需创建池</summary>
        private EntityPool GetOrCreatePool(EntityConfigSO config)
        {
            if (!_pools.TryGetValue(config, out var pool))
            {
                pool = new EntityPool(config);
                _pools[config] = pool;
            }
            return pool;
        }
    }
}
