using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Phase 1 最小攻击组件——定时发射弹幕。
    /// v2.4 新增（GD-R4-003/009）。
    /// 
    /// 使用 ComponentType.Attack 槽位（独立于 Phase 3 SkillComponent）。
    /// 攻击决策来自 IDecisionMaker（Control/AI），当 WantsAttack=true 且 CD 就绪时发射。
    /// 
    /// Tick 时序：TickOrder=150（Attack 阶段，Decision 之后 AutoAim 之前）。
    /// 
    /// 近战攻击说明（GD-R4-009）：
    /// Phase 1 所有攻击统一走弹幕系统。近战=射程极短的瞬发弹幕。
    /// </summary>
    public sealed class AttackComponent : IEntityComponent, ITickable
    {
        // ──────────────── IEntityComponent ────────────────
        public ComponentType Type => ComponentType.Attack;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ──────────────── ITickable ────────────────
        public int TickOrder => 150; // TickOrders.Attack

        // ──────────────── 配置 ────────────────
        private Entity _owner;
        private float _attackInterval;
        private float _timer;
        private BulletPatternSO _bulletPattern;
        private Vector2 _fireOffset;

        // ──────────────── 生命周期 ────────────────

        public void Init(Entity owner)
        {
            _owner = owner;
            _attackInterval = owner.ConfigSO.AttackInterval;
            _bulletPattern = owner.ConfigSO.AttackBulletPattern;
            _fireOffset = owner.ConfigSO.AttackFireOffset;
            _timer = 0f;
            IsActive = true;
        }

        public void Reset()
        {
            _timer = 0f;
            _owner = null;
            _bulletPattern = null;
            IsActive = false;
        }

        // ──────────────── Tick ────────────────

        public void Tick(float dt)
        {
            if (_owner == null || _bulletPattern == null) return;

            // 累积计时器
            _timer += dt;
            if (_timer < _attackInterval) return;

            // 检查决策是否要攻击
            var decisionMaker = GetDecisionMaker();
            if (decisionMaker == null) return;

            var command = decisionMaker.GetDecision();
            if (!command.WantsAttack) return;

            // CD 就绪 + 决策要求攻击 → 发射
            _timer -= _attackInterval;

            var ds = DanmakuSystem.Instance;
            if (ds == null) return;

            Vector2 firePos = _owner.Position + _fireOffset;
            float fireAngle = GetFireAngle(command.AimDirection);
            ds.FireBullets(_bulletPattern, firePos, fireAngle);
        }

        // ──────────────── 内部工具 ────────────────

        private IDecisionMaker GetDecisionMaker()
        {
            // 优先 Control，其次 AI（互斥挂载下只会有一个）
            var ctrl = _owner.GetComponent(ComponentType.Control);
            if (ctrl is IDecisionMaker dm1) return dm1;

            var ai = _owner.GetComponent(ComponentType.AI);
            if (ai is IDecisionMaker dm2) return dm2;

            return null;
        }

        private float GetFireAngle(Vector2 aimDir)
        {
            if (aimDir.sqrMagnitude < 0.01f)
            {
                // 无瞄准方向时使用 Entity 朝向
                return _owner.Rotation;
            }
            return Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        }

        // ──────────────── 测试支持 ────────────────

        /// <summary>获取当前 CD 计时器值（测试用）</summary>
        internal float Timer => _timer;

        /// <summary>强制重置 CD（测试用）</summary>
        internal void ForceResetTimer() => _timer = 0f;

        /// <summary>是否已配置攻击弹幕</summary>
        public bool HasAttackConfig => _bulletPattern != null;
    }
}
