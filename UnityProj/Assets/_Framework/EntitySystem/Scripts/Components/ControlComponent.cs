using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 玩家控制组件——将外部输入转化为 DecisionCommand。
    /// BC-07.1: 实现 IDecisionMaker 接口。
    /// BC-07.2: 与 AIComponent 互斥挂载。
    /// 
    /// 使用方式：外部输入系统（InputManager/触屏/摇杆）每帧调用
    /// SetMoveInput() 和 SetAttackInput() 写入意图，
    /// ControlComponent 在 Tick 时将意图转化为 DecisionCommand。
    /// 
    /// Tick 时序：TickOrder=100（Decision 阶段），与 AIComponent 同级。
    /// </summary>
    public sealed class ControlComponent : IEntityComponent, ITickable, IDecisionMaker
    {
        // ──────────────── IEntityComponent ────────────────
        public ComponentType Type => ComponentType.Control;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ──────────────── ITickable ────────────────
        public int TickOrder => 100; // TickOrders.Decision

        // ──────────────── 外部输入缓冲 ────────────────
        private Vector2 _moveInput;
        private bool _attackInput;
        private Vector2 _aimInput;

        /// <summary>
        /// 抑制移动转发。设为 true 时，Tick 不再将 _moveInput 写入 MovementComponent。
        /// 用于 1:1 跟手模式——外部直接调 SetPosition，避免速度系统干扰。
        /// </summary>
        public bool SuppressMovement { get; set; }

        // ──────────────── 内部状态 ────────────────
        private Entity _owner;
        private DecisionCommand _lastCommand;

        // ──────────────── 生命周期 ────────────────

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;
            _moveInput = Vector2.zero;
            _attackInput = false;
            _aimInput = Vector2.right;
            _lastCommand = DecisionCommand.Idle;
        }

        public void Reset()
        {
            _moveInput = Vector2.zero;
            _attackInput = false;
            _aimInput = Vector2.right;
            _lastCommand = DecisionCommand.Idle;
            _owner = null;
            IsActive = false;
            SuppressMovement = false;
        }

        // ──────────────── 外部输入 API ────────────────

        /// <summary>设置移动输入方向（由 InputManager 每帧调用）</summary>
        public void SetMoveInput(Vector2 direction)
        {
            _moveInput = direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        /// <summary>设置攻击输入（按下=true，松开=false）</summary>
        public void SetAttackInput(bool attack)
        {
            _attackInput = attack;
        }

        /// <summary>设置瞄准方向（摇杆/触屏/鼠标方向）</summary>
        public void SetAimInput(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
                _aimInput = direction.normalized;
        }

        // ──────────────── Tick ────────────────

        public void Tick(float dt)
        {
            if (_owner == null) return;

            _lastCommand = new DecisionCommand
            {
                MoveDirection = _moveInput,
                WantsAttack = _attackInput,
                AimDirection = _aimInput
            };

            // 将决策应用到 MovementComponent（SuppressMovement 时跳过，避免与直接位置模式冲突）
            if (!SuppressMovement)
            {
                var movement = _owner.GetComponent(ComponentType.Movement) as MovementComponent;
                if (movement != null)
                {
                    movement.SetMoveDirection(_lastCommand.MoveDirection);
                }
            }
        }

        // ──────────────── IDecisionMaker ────────────────

        public DecisionCommand GetDecision() => _lastCommand;
    }
}
