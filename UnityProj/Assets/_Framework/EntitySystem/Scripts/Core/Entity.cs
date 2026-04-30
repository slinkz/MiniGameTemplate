using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 容器——纯 C# 对象，不继承 MonoBehaviour，不持有 GameObject。
    /// 
    /// 核心职责：
    /// - 持有组件数组（按 ComponentType 枚举索引，O(1) 查找）
    /// - 持有本地事件总线（EntityEventBus）
    /// - 持有唯一 ID（EntityId）和阵营（EnumCamp）
    /// - 驱动所有 Tickable 组件的 Tick 调用（按 TickOrder 升序）
    /// 
    /// 生命周期由 EntityPool / EntityManager 管理：
    /// - Acquire：分配 ID → 注入组件 → Init 所有组件
    /// - Tick：驱动 Tickable 组件
    /// - Release：Reset 所有组件 → 清空事件总线 → 归还池
    /// </summary>
    public sealed class Entity
    {
        // ──────────────── 身份 ────────────────

        /// <summary>全局唯一标识</summary>
        public EntityId Id { get; internal set; }

        /// <summary>阵营（与弹幕系统共享 EnumCamp）</summary>
        public Danmaku.EnumCamp Camp { get; internal set; }

        /// <summary>是否存活（已 Acquire 且未 Release）</summary>
        public bool IsAlive { get; internal set; }

        // ──────────────── 位置（逻辑坐标）────────────────

        /// <summary>当前逻辑位置（由 MovementComponent 更新）</summary>
        public Vector2 Position;

        /// <summary>当前朝向角度（度，0=右，逆时针正）</summary>
        public float Rotation;

        // ──────────────── 组件系统 ────────────────

        /// <summary>
        /// 组件数组，按 ComponentType 枚举作为索引。
        /// 长度固定为 ComponentType.MAX（16），未挂载的槽位为 null。
        /// </summary>
        private readonly IEntityComponent[] _components = new IEntityComponent[(int)ComponentType.MAX];

        /// <summary>
        /// Tickable 组件排序缓存（按 TickOrder 升序）。
        /// 在 Init 阶段构建，避免每帧排序。
        /// </summary>
        private ITickable[] _tickables;
        private int _tickableCount;

        // ──────────────── Pause 支持（v2.4 预留）────────────────

        private int _pauseFrames;

        /// <summary>是否处于暂停状态（Phase 1 始终 false）</summary>
        public bool IsPaused => _pauseFrames > 0;

        /// <summary>暂停指定帧数（Phase 2 用于 HitStop 顿帧）</summary>
        public void PauseFor(int frames) => _pauseFrames = frames;

        internal void DecrementPauseFrames()
        {
            if (_pauseFrames > 0) _pauseFrames--;
        }

        // ──────────────── 事件总线 ────────────────

        /// <summary>Entity 本地事件总线（组件间解耦通信）</summary>
        public EntityEventBus EventBus { get; } = new EntityEventBus();

        // ──────────────── 池化管理 ────────────────

        /// <summary>在池数组中的槽位索引（Release 时归还用）</summary>
        public int PoolSlot { get; internal set; }

        /// <summary>在 EntityManager._activeEntities 中的索引（swap-remove 用）</summary>
        public int ActiveListIndex { get; internal set; }

        /// <summary>所属配置 SO（由 EntityPool 创建时设置）</summary>
        public EntityConfigSO ConfigSO { get; internal set; }

        /// <summary>是否处于待销毁状态（Tick 期间标记，帧尾统一执行）</summary>
        public bool IsPendingDespawn { get; private set; }

        /// <summary>标记为待销毁（由 EntityManager.Despawn 在 Tick 期间调用）</summary>
        internal void MarkPendingDespawn() => IsPendingDespawn = true;

        // ──────────────── 组件管理 ────────────────

        /// <summary>
        /// 注册组件到指定槽位。由 EntityPool 在预分配阶段调用。
        /// </summary>
        internal void RegisterComponent(IEntityComponent component)
        {
            int slot = (int)component.Type;
            _components[slot] = component;
        }

        /// <summary>
        /// 枚举版 GetComponent：O(1) 直接数组索引，零类型检查。热路径首选。
        /// </summary>
        public IEntityComponent GetComponent(ComponentType type)
        {
            return _components[(int)type];
        }

        /// <summary>
        /// 泛型版 GetComponent：O(N) 线性扫描 + is T 类型检查。
        /// N ≤ 16，非热路径使用（如初始化阶段）。
        /// </summary>
        public T GetComponent<T>() where T : class, IEntityComponent
        {
            for (int i = 0; i < (int)ComponentType.MAX; i++)
            {
                if (_components[i] is T result) return result;
            }
            return null;
        }

        // ──────────────── 生命周期 ────────────────

        /// <summary>
        /// 从池取出时初始化：设置位置/朝向 → 标记存活 → Init 所有组件 → 构建 Tickable 缓存。
        /// 由 EntityPool.Acquire() 调用。
        /// </summary>
        internal void InitAll(Vector2 position, float rotation)
        {
            Position = position;
            Rotation = rotation;
            IsAlive = true;
            IsPendingDespawn = false;
            _pauseFrames = 0;

            // Init 所有非空组件
            for (int i = 0; i < (int)ComponentType.MAX; i++)
            {
                _components[i]?.Init(this);
            }

            // 构建 Tickable 排序缓存
            RebuildTickableCache();
        }

        /// <summary>
        /// 归还池时重置：Reset 所有组件 → 清空事件总线 → 标记非存活。
        /// 由 EntityPool.Release() 调用。
        /// </summary>
        internal void ResetAll()
        {
            for (int i = 0; i < (int)ComponentType.MAX; i++)
            {
                _components[i]?.Reset();
            }
            EventBus.ClearAll();
            _pauseFrames = 0;
            IsAlive = false;
            IsPendingDespawn = false;
        }

        /// <summary>
        /// 每帧 Tick：按 TickOrder 升序驱动所有激活的 Tickable 组件。
        /// 由 EntityManager.Tick() 调用。
        /// </summary>
        internal void Tick(float dt)
        {
            if (!IsAlive) return;

            // Pause 支持：暂停期间跳过 Tick
            if (IsPaused)
            {
                DecrementPauseFrames();
                return;
            }

            for (int i = 0; i < _tickableCount; i++)
            {
                var tickable = _tickables[i];
                // _tickables 全部来自 _components 筛选，必然同时是 IEntityComponent
                if (((IEntityComponent)tickable).IsActive)
                {
                    tickable.Tick(dt);
                }
            }
        }

        // ──────────────── 内部工具 ────────────────

        /// <summary>
        /// 重建 Tickable 缓存：收集所有实现 ITickable 的组件，按 TickOrder 升序排列。
        /// 使用插入排序（N≤16，简单高效）。
        /// </summary>
        private void RebuildTickableCache()
        {
            // 懒初始化数组（最多 16 个组件都可能 Tickable）
            if (_tickables == null)
                _tickables = new ITickable[(int)ComponentType.MAX];

            _tickableCount = 0;

            for (int i = 0; i < (int)ComponentType.MAX; i++)
            {
                if (_components[i] is ITickable tickable)
                {
                    _tickables[_tickableCount++] = tickable;
                }
            }

            // 插入排序（N≤16，简单高效，零 GC）
            for (int i = 1; i < _tickableCount; i++)
            {
                var key = _tickables[i];
                int keyOrder = key.TickOrder;
                int j = i - 1;
                while (j >= 0 && _tickables[j].TickOrder > keyOrder)
                {
                    _tickables[j + 1] = _tickables[j];
                    j--;
                }
                _tickables[j + 1] = key;
            }
        }

        /// <summary>
        /// 清空所有组件槽位（仅在 Entity 对象被彻底销毁时使用，正常流程用 Reset）。
        /// </summary>
        internal void ClearComponents()
        {
            for (int i = 0; i < (int)ComponentType.MAX; i++)
            {
                _components[i] = null;
            }
            _tickableCount = 0;
        }
    }
}
