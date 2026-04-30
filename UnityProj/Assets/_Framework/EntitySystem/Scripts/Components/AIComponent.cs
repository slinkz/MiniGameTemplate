namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI 决策组件——通过 IDecisionStrategy 产生每帧 DecisionCommand。
    /// BC-07.1: 实现 IDecisionMaker 接口。
    /// BC-07.2: 与 ControlComponent 互斥挂载（EntityPool 创建时校验）。
    /// BC-07.3: 内部策略可替换（默认 ConditionActionTableStrategy）。
    /// 
    /// Tick 时序：TickOrder=100（Decision 阶段），在 Movement/Attack 之前。
    /// </summary>
    public sealed class AIComponent : IEntityComponent, ITickable, IDecisionMaker
    {
        // ──────────────── IEntityComponent ────────────────
        public ComponentType Type => ComponentType.AI;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ──────────────── ITickable ────────────────
        public int TickOrder => 100; // TickOrders.Decision

        // ──────────────── 内部状态 ────────────────
        private Entity _owner;
        private IDecisionStrategy _strategy;
        private DecisionCommand _lastCommand;

        // ──────────────── 生命周期 ────────────────

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;

            // 默认策略：ConditionActionTableStrategy
            if (_strategy == null)
                _strategy = new ConditionActionTableStrategy();

            _strategy.Init(owner);
            _lastCommand = DecisionCommand.Idle;
        }

        public void Reset()
        {
            _strategy?.Reset();
            _lastCommand = DecisionCommand.Idle;
            _owner = null;
            IsActive = false;
        }

        // ──────────────── Tick ────────────────

        public void Tick(float dt)
        {
            if (_owner == null || _strategy == null) return;

            _lastCommand = _strategy.Evaluate(dt);

            // 将决策应用到 MovementComponent
            var movement = _owner.GetComponent(ComponentType.Movement) as MovementComponent;
            if (movement != null)
            {
                movement.SetMoveDirection(_lastCommand.MoveDirection);
            }
        }

        // ──────────────── IDecisionMaker ────────────────

        public DecisionCommand GetDecision() => _lastCommand;

        // ──────────────── 策略替换（BC-07.3）────────────────

        /// <summary>替换 AI 策略（运行时可切换行为模式）</summary>
        public void SetStrategy(IDecisionStrategy strategy)
        {
            _strategy?.Reset();
            _strategy = strategy;
            if (_owner != null)
                _strategy?.Init(_owner);
        }
    }
}
