namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 动画状态组件——纯逻辑层，管理动画状态 ID 映射。
    /// 
    /// 设计要点（TDD §4.3 + BC-02.2）：
    /// - Tickable（TickOrder=400），在 Movement 之后执行
    /// - Phase 1 **不操作任何渲染组件**（Spine/SpriteRenderer）
    /// - 只提供 CurrentAnimId 只读属性，由游戏层 ViewBridge 读取并驱动实际渲染
    /// - 通过监听 OnStateChanged / MovementComponent.IsMoving 自动切换动画 ID
    /// - Entity 层保持纯逻辑，渲染表现完全解耦
    /// 
    /// 动画 ID 约定：
    /// - 0 = Idle
    /// - 1 = Move / Walk
    /// - 2 = Attack
    /// - 3 = Hit
    /// - 4 = Death
    /// - 100+ = 自定义（由游戏层定义）
    /// </summary>
    public class AnimationComponent : IEntityComponent, ITickable
    {
        // ── IEntityComponent 实现 ──
        public bool IsActive { get; private set; }
        public ComponentType Type => ComponentType.Animation;

        // ── ITickable 实现 ──
        public int TickOrder => TickOrders.Animation;

        // ── 引用 ──
        private Entity _owner;
        private MovementComponent _movement;
        private StateComponent _state;

        // ── 动画状态 ──
        private int _currentAnimId;
        private int _overrideAnimId;
        private float _overrideTimer;

        /// <summary>
        /// 当前生效的动画 ID。
        /// 优先级：Override（临时） > State 驱动 > Movement 驱动 > Idle
        /// </summary>
        public int CurrentAnimId => _overrideTimer > 0f ? _overrideAnimId : _currentAnimId;

        /// <summary>动画是否被临时覆盖中</summary>
        public bool IsOverriding => _overrideTimer > 0f;

        // ── 动画 ID 常量 ──
        public static class AnimId
        {
            public const int Idle = 0;
            public const int Move = 1;
            public const int Attack = 2;
            public const int Hit = 3;
            public const int Death = 4;
        }

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;
            _currentAnimId = AnimId.Idle;
            _overrideAnimId = 0;
            _overrideTimer = 0f;

            // 缓存同 Entity 上的 Movement 和 State 组件引用
            _movement = owner.GetComponent(ComponentType.Movement) as MovementComponent;
            _state = owner.GetComponent(ComponentType.State) as StateComponent;
        }

        public void Reset()
        {
            _currentAnimId = AnimId.Idle;
            _overrideAnimId = 0;
            _overrideTimer = 0f;
            _movement = null;
            _state = null;
            _owner = null;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        // ── 外部控制接口 ──

        /// <summary>
        /// 播放临时动画覆盖（如攻击、受击闪白），持续 duration 秒后自动恢复。
        /// </summary>
        /// <param name="animId">动画 ID</param>
        /// <param name="duration">持续时间（秒），≤0 则立即清除覆盖</param>
        public void PlayOverride(int animId, float duration)
        {
            if (duration <= 0f)
            {
                _overrideTimer = 0f;
                return;
            }
            _overrideAnimId = animId;
            _overrideTimer = duration;
        }

        /// <summary>
        /// 强制设置当前动画 ID（不使用自动推导，直到下一次 Tick 重新推导）。
        /// </summary>
        public void ForceSetAnimId(int animId)
        {
            _currentAnimId = animId;
        }

        // ── Tick ──

        public void Tick(float dt)
        {
            if (_owner == null) return;

            // 1. Override 倒计时
            if (_overrideTimer > 0f)
            {
                _overrideTimer -= dt;
                if (_overrideTimer < 0f) _overrideTimer = 0f;
                // Override 期间不推导 _currentAnimId，保持上次自动推导结果
                return;
            }

            // 2. 根据 State 和 Movement 自动推导动画 ID
            _currentAnimId = DeriveAnimId();
        }

        // ── 内部推导 ──

        /// <summary>
        /// 根据当前 Entity 状态自动推导动画 ID。
        /// 优先级：Dead > Hit > Moving > Idle
        /// </summary>
        private int DeriveAnimId()
        {
            // 死亡状态
            if (_state != null && _state.HasState(EntityState.Dead))
                return AnimId.Death;

            // 受击状态（由 StateComponent 管理，短暂停留后自动移除）
            if (_state != null && _state.HasState(EntityState.Hit))
                return AnimId.Hit;

            // 移动中
            if (_movement != null && _movement.IsMoving)
                return AnimId.Move;

            // 默认：Idle
            return AnimId.Idle;
        }
    }
}
