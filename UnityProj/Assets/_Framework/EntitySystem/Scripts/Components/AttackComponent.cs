using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// [OBSOLETE] 已被 SkillComponent Slot[0] 替代（TDD-06）。
    /// 保留文件用于迁移参考。新代码请使用 SkillComponent + SkillConfigSO(IsNormalAttack=true)。
    /// </summary>
    [System.Obsolete("Use SkillComponent Slot[0] with IsNormalAttack=true. See TDD-06.")]
    public sealed class AttackComponent : IEntityComponent, ITickable
    {
        // ──────────────── IEntityComponent ────────────────
        public ComponentType Type => ComponentType.Attack;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ──────────────── ITickable ────────────────
        public int TickOrder => TickOrders.Attack; // 150

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

            // P3.4: Buff 攻速修正（pull 模式）
            float effectiveInterval = _attackInterval;
            var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
            if (buff != null)
                effectiveInterval *= buff.AttackIntervalModifier;

            if (_timer < effectiveInterval) return;

            // 检查决策是否要攻击
            var decisionMaker = GetDecisionMaker();
            if (decisionMaker == null) return;

            var command = decisionMaker.GetDecision();
            if (!command.WantsAttack) return;

            // CD 就绪 + 决策要求攻击 → 发射
            _timer -= effectiveInterval;

            var ds = DanmakuSystem.Instance;
            if (ds == null) return;

            Vector2 firePos = _owner.Position + _fireOffset;
            float fireAngle = GetFireAngle(command.AimDirection);
            ds.FireBullets(_bulletPattern, firePos, fireAngle, _owner.Id.Value);
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
            // 普攻永远朝实体正前方射击，不跟随 AutoAim 目标
            // 优先级 1：DecisionCommand 瞄准方向（手动操控时可覆盖）
            if (aimDir.sqrMagnitude > 0.01f)
            {
                return Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            }

            // 优先级 2：Entity 朝向
            return _owner.Rotation;
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
