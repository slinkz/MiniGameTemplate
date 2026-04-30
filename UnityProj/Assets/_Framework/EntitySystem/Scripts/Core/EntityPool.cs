using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 按 Entity 配置类型分池。
    /// 采用预分配数组 + 空闲槽位栈（参考 BulletWorld），零 GC。
    /// Phase 1 以 EntityConfigSO 为键；Phase 2 可选 Luban configId 桥接。
    /// 
    /// BC-04 契约实现：
    /// - BC-04.1：按配置类型分池（EntityConfigSO → 独立池）
    /// - BC-04.2：Entity + 组件整体预分配，取出/归还零 GC
    /// - BC-04.3：取出时调用 InitAll()，归还时调用 ResetAll()
    /// - BC-04.4：MaxCapacity 硬上限，超限 LogWarning 不崩溃
    /// </summary>
    public class EntityPool
    {
        private readonly Entity[] _entities;
        private readonly int[] _freeSlots;
        private int _freeTop;
        private readonly EntityConfigSO _config;

        /// <summary>当前活跃（已取出）的 Entity 数量</summary>
        public int ActiveCount { get; private set; }

        /// <summary>池容量（预分配数量）</summary>
        public int Capacity { get; }

        /// <summary>关联的配置 SO</summary>
        public EntityConfigSO Config => _config;

        /// <summary>
        /// 创建对象池并预分配所有 Entity。
        /// </summary>
        /// <param name="config">配置 SO，决定容量和组件列表</param>
        public EntityPool(EntityConfigSO config)
        {
            _config = config;
            Capacity = config.PoolMax;
            _entities = new Entity[config.PoolMax];
            _freeSlots = new int[config.PoolMax];

            // 预创建所有 Entity + 组件
            for (int i = 0; i < config.PoolMax; i++)
            {
                _entities[i] = CreateEntityFromConfig(config, i);
                _freeSlots[_freeTop++] = i;
            }
        }

        /// <summary>
        /// 从池中取出一个 Entity。池满时返回 null + LogWarning。
        /// </summary>
        /// <param name="position">初始位置</param>
        /// <param name="rotation">初始朝向角度</param>
        /// <returns>已初始化的 Entity，或 null（池满）</returns>
        public Entity Acquire(Vector2 position, float rotation)
        {
            if (_freeTop == 0)
            {
                Debug.LogWarning($"[EntityPool] 池满：{_config.name}（容量={Capacity}）");
                return null;
            }

            int slot = _freeSlots[--_freeTop];
            var entity = _entities[slot];
            entity.InitAll(position, rotation);
            ActiveCount++;
            return entity;
        }

        /// <summary>
        /// 归还 Entity 到池中。
        /// </summary>
        public void Release(Entity entity)
        {
            entity.ResetAll();
            _freeSlots[_freeTop++] = entity.PoolSlot;
            ActiveCount--;
        }

        /// <summary>
        /// 根据配置创建 Entity 并注册组件。
        /// Phase 1：根据 config.Components 数组实例化对应组件占位（具体组件类在后续 Phase 实现）。
        /// </summary>
        private static Entity CreateEntityFromConfig(EntityConfigSO config, int slotIndex)
        {
            var entity = new Entity();
            entity.PoolSlot = slotIndex;
            entity.ConfigSO = config;
            entity.Camp = config.Camp;

            // Phase 1.4~1.7 会在此处创建具体组件实例
            // 目前 P1.3 只创建空 Entity 容器（组件在后续 Phase 注册）
            // 当具体组件类实现后，这里会根据 config.Components 数组实例化

            return entity;
        }
    }
}
