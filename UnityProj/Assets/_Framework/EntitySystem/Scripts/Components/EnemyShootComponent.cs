using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 敌机射击组件——V2 Sprint 1 新增。
    /// 
    /// 与 AttackComponent 的区别：
    /// - 不依赖 IDecisionMaker（敌机无条件射击）
    /// - 内置 FirstAttackDelay（每只敌机独立计时器，从 Spawn 时刻开始）
    /// - 固定向下射击（baseAngle = 270°）
    /// - TickOrder = 155（在 AttackComponent=150 之后，避免冲突）
    /// 
    /// 数据来源（TDD S1.2）：
    /// - _pattern = EntityConfigSO.AttackBulletPattern（复用已有字段）
    /// - _cooldown = EntityConfigSO.AttackInterval（复用已有字段）
    /// - _firstFireDelay = EntityConfigSO.FirstAttackDelay（V2 新增）
    /// </summary>
    public sealed class EnemyShootComponent : IEntityComponent, ITickable
    {
        // ──────────────── IEntityComponent ────────────────
        public ComponentType Type => ComponentType.EnemyShoot;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ──────────────── ITickable ────────────────
        public int TickOrder => 155; // 在 Attack(150) 之后

        // ──────────────── 配置 ────────────────
        private Entity _owner;
        private BulletPatternSO _pattern;
        private float _cooldown;
        private float _cooldownTimer;
        private float _firstFireDelay;
        private float _firstFireTimer;
        private bool _hasFirstFired;
        private Vector2 _fireOffset;

        // ──────────────── 生命周期 ────────────────

        public void Init(Entity owner)
        {
            _owner = owner;
            var config = owner.ConfigSO;

            _pattern = config.AttackBulletPattern;
            _cooldown = config.AttackInterval;
            _firstFireDelay = config.FirstAttackDelay;
            _fireOffset = config.AttackFireOffset;

            _firstFireTimer = 0f;
            _cooldownTimer = 0f;
            _hasFirstFired = false;
            IsActive = _pattern != null; // 无弹幕配置则自动休眠
        }

        public void Reset()
        {
            _owner = null;
            _pattern = null;
            _firstFireTimer = 0f;
            _cooldownTimer = 0f;
            _hasFirstFired = false;
            IsActive = false;
        }

        // ──────────────── Tick ────────────────

        public void Tick(float dt)
        {
            if (_owner == null || _pattern == null) return;

            // Phase 1: 首次开火延迟
            if (!_hasFirstFired)
            {
                _firstFireTimer += dt;
                if (_firstFireTimer >= _firstFireDelay)
                {
                    _hasFirstFired = true;
                    Fire();
                    _cooldownTimer = _cooldown;
                }
                return;
            }

            // Phase 2: 正常射击循环
            _cooldownTimer -= dt;
            if (_cooldownTimer <= 0f)
            {
                Fire();
                _cooldownTimer += _cooldown;
            }
        }

        // ──────────────── 发射 ────────────────

        private void Fire()
        {
            var ds = DanmakuSystem.Instance;
            if (ds == null) return;

            Vector2 firePos = _owner.Position + _fireOffset;
            // 270° = 向下（弹幕系统角度制：0=右，90=上，270=下）
            ds.FireBullets(_pattern, firePos, 270f, _owner.Id.Value);
        }

        // ──────────────── 测试支持 ────────────────

        /// <summary>是否已完成首次开火</summary>
        internal bool HasFirstFired => _hasFirstFired;

        /// <summary>首次开火计时器当前值</summary>
        internal float FirstFireTimer => _firstFireTimer;
    }
}
