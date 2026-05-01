using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 移动组件——管理 Entity 逻辑坐标的位移 + 击退。
    /// 
    /// 设计要点（TDD §4.4 + BC-02.2）：
    /// - Tickable（TickOrder=300），每帧根据速度+方向更新 Entity.Position
    /// - 速度修正器：固定数组（最多 4 个 Modifier），避免 List 扩容 GC
    /// - 击退（v2.4 GD-R4-004）：额外位移叠加在正常移动之上
    /// - 发布 OnPositionChanged 事件供 ViewBridge 消费
    /// - 从 EntityConfigSO.MoveSpeed / TurnSpeed 读取基础属性
    /// </summary>
    public class MovementComponent : IEntityComponent, ITickable
    {
        // ── IEntityComponent 实现 ──
        public bool IsActive { get; private set; }
        public ComponentType Type => ComponentType.Movement;

        // ── ITickable 实现 ──
        public int TickOrder => TickOrders.Movement;

        // ── 引用 ──
        private Entity _owner;

        // ── 基础属性（从 EntityConfigSO 读取）──
        private float _baseSpeed;
        private float _turnSpeed;

        // ── 运动状态 ──
        private Vector2 _moveDirection;
        private bool _isMoving;

        /// <summary>当前移动方向（归一化）</summary>
        public Vector2 MoveDirection => _moveDirection;

        /// <summary>是否正在移动</summary>
        public bool IsMoving => _isMoving;

        // ── 速度修正器（固定数组，最多 4 个，避免 GC）──
        private const int MAX_MODIFIERS = 4;
        private readonly float[] _speedModifiers = new float[MAX_MODIFIERS];
        private int _modifierCount;

        // ── 击退状态（v2.4 GD-R4-004 + P2.4 曲线支持）──
        private Vector2 _knockbackDir;
        private float _knockbackSpeed;       // 线性模式下的恒定速度
        private float _knockbackRemaining;   // 剩余击退时间
        private float _knockbackDuration;    // 总击退时间（用于曲线采样）
        private float _knockbackDistance;    // 总击退距离（曲线模式使用）
        private UnityEngine.AnimationCurve _knockbackCurve; // 击退速度曲线（null=线性）

        /// <summary>是否正在被击退</summary>
        public bool IsKnockedBack => _knockbackRemaining > 0f;

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;

            // 从配置读取基础属性
            _baseSpeed = owner.ConfigSO != null ? owner.ConfigSO.MoveSpeed : 3f;
            _turnSpeed = owner.ConfigSO != null ? owner.ConfigSO.TurnSpeed : 360f;

            _moveDirection = Vector2.zero;
            _isMoving = false;
            _modifierCount = 0;
            _knockbackDir = Vector2.zero;
            _knockbackSpeed = 0f;
            _knockbackRemaining = 0f;
            _knockbackDuration = 0f;
            _knockbackDistance = 0f;
            _knockbackCurve = null;
        }

        public void Reset()
        {
            _moveDirection = Vector2.zero;
            _isMoving = false;
            _modifierCount = 0;
            _knockbackDir = Vector2.zero;
            _knockbackSpeed = 0f;
            _knockbackRemaining = 0f;
            _knockbackDuration = 0f;
            _knockbackDistance = 0f;
            _knockbackCurve = null;
            _owner = null;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        // ── 外部控制接口 ──

        /// <summary>
        /// 设置移动方向。由 ControlComponent / AIComponent 在 Decision Tick 中调用。
        /// </summary>
        /// <param name="direction">移动方向（会被归一化），零向量 = 停止</param>
        public void SetMoveDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                _moveDirection = Vector2.zero;
                _isMoving = false;
            }
            else
            {
                _moveDirection = direction.normalized;
                _isMoving = true;
            }
        }

        /// <summary>
        /// 直接设置朝向角度（度，0=右，逆时针正）。
        /// </summary>
        public void SetRotation(float angle)
        {
            if (_owner != null)
                _owner.Rotation = angle;
        }

        /// <summary>
        /// 直接设置位置（P2.2 碰撞分离用，绕过速度系统）。
        /// </summary>
        public void SetPosition(Vector2 pos)
        {
            if (_owner != null)
                _owner.Position = pos;
        }

        /// <summary>
        /// 面向目标位置（立即转向，Phase 2 扩展平滑转向）。
        /// </summary>
        public void LookAt(Vector2 targetPos)
        {
            if (_owner == null) return;
            Vector2 dir = targetPos - _owner.Position;
            if (dir.sqrMagnitude < 0.0001f) return;
            _owner.Rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        // ── 速度修正器 ──

        /// <summary>
        /// 添加速度倍率修正器（如减速/加速 Buff）。
        /// 返回槽位索引（-1 = 已满）。
        /// </summary>
        public int AddSpeedModifier(float multiplier)
        {
            if (_modifierCount >= MAX_MODIFIERS) return -1;
            _speedModifiers[_modifierCount] = multiplier;
            return _modifierCount++;
        }

        /// <summary>
        /// 移除指定槽位的速度修正器。
        /// </summary>
        public void RemoveSpeedModifier(int slot)
        {
            if (slot < 0 || slot >= _modifierCount) return;
            // swap-remove
            _modifierCount--;
            if (slot < _modifierCount)
            {
                _speedModifiers[slot] = _speedModifiers[_modifierCount];
            }
        }

        /// <summary>
        /// 清除所有速度修正器。
        /// </summary>
        public void ClearSpeedModifiers()
        {
            _modifierCount = 0;
        }

        /// <summary>
        /// 计算最终速度（基础速度 × 所有修正器乘积）。
        /// </summary>
        public float GetFinalSpeed()
        {
            float speed = _baseSpeed;
            for (int i = 0; i < _modifierCount; i++)
            {
                speed *= _speedModifiers[i];
            }
            return speed;
        }

        // ── 击退（v2.4 GD-R4-004）──

        /// <summary>
        /// 施加击退效果。被调用后在 duration 时间内沿 direction 位移 distance 距离。
        /// 击退期间正常移速叠加（击退是额外位移，不替代原始运动）。
        /// 支持曲线模式：curve != null 时，瞬时速度 = (distance/duration) * curve.Evaluate(t)，t=0~1。
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float distance, float duration, AnimationCurve curve = null)
        {
            if (duration <= 0f) return;
            _knockbackDir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.zero;
            _knockbackSpeed = distance / duration;
            _knockbackRemaining = duration;
            _knockbackDuration = duration;
            _knockbackDistance = distance;
            _knockbackCurve = curve;
        }

        // ── Tick ──

        public void Tick(float dt)
        {
            if (_owner == null) return;

            Vector2 oldPos = _owner.Position;
            Vector2 displacement = Vector2.zero;

            // 1. 正常移动
            if (_isMoving)
            {
                float speed = GetFinalSpeed();
                displacement += _moveDirection * (speed * dt);
            }

            // 2. 击退位移（叠加在正常移动之上，支持曲线衰减）
            if (_knockbackRemaining > 0f)
            {
                float knockDt = Mathf.Min(dt, _knockbackRemaining);
                float speed;
                if (_knockbackCurve != null && _knockbackCurve.length > 0 && _knockbackDuration > 0f)
                {
                    // 曲线模式：t = 已经过时间比例（0→1）
                    float elapsed = _knockbackDuration - _knockbackRemaining;
                    float t = elapsed / _knockbackDuration;
                    speed = _knockbackSpeed * _knockbackCurve.Evaluate(t);
                }
                else
                {
                    // 线性模式
                    speed = _knockbackSpeed;
                }
                displacement += _knockbackDir * (speed * knockDt);
                _knockbackRemaining -= dt;
                if (_knockbackRemaining < 0f) _knockbackRemaining = 0f;
            }

            // 3. 应用位移
            if (displacement.sqrMagnitude > 0.00001f)
            {
                _owner.Position += displacement;

                // 发布位置变化事件
                _owner.EventBus.Publish(new OnPositionChanged
                {
                    OldPos = oldPos,
                    NewPos = _owner.Position
                });
            }
        }
    }
}
