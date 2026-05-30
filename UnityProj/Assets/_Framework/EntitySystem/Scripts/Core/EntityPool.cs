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
        /// 按 config.Components 数组实例化对应组件并 RegisterComponent。
        /// </summary>
        private static Entity CreateEntityFromConfig(EntityConfigSO config, int slotIndex)
        {
            var entity = new Entity();
            entity.PoolSlot = slotIndex;
            entity.ConfigSO = config;
            entity.Camp = config.Camp;

            // 根据配置的组件列表实例化并注册
            if (config.Components != null)
            {
                for (int i = 0; i < config.Components.Length; i++)
                {
                    var comp = CreateComponent(config.Components[i]);
                    if (comp != null)
                        entity.RegisterComponent(comp);
                }
            }

            return entity;
        }

        /// <summary>
        /// 工厂方法：根据 ComponentType 创建对应组件实例。
        /// Phase 1~3A 支持：State, Health, Movement, Collision, Control, AI, Attack, Animation, AutoAim, Skill, Buff。
        /// </summary>
        private static IEntityComponent CreateComponent(ComponentType type)
        {
            switch (type)
            {
                case ComponentType.State:     return new StateComponent();
                case ComponentType.Health:    return new HealthComponent();
                case ComponentType.Movement:  return new MovementComponent();
                case ComponentType.Collision: return new CollisionComponent();
                case ComponentType.Control:   return new ControlComponent();
                case ComponentType.AI:        return new AIComponent();
                case ComponentType.Animation: return new AnimationComponent();
                case ComponentType.AutoAim:   return new AutoAimComponent();
                case ComponentType.Skill:     return new SkillComponent();
                case ComponentType.Buff:      return new BuffComponent();
                case ComponentType.EnemyShoot: return new EnemyShootComponent();
                case ComponentType.Passive:   return new PassiveComponent();
                default:
                    Debug.LogWarning($"[EntityPool] 未知组件类型：{type}，跳过创建。");
                    return null;
            }
        }
    }
}
