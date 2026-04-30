using UnityEngine;

namespace MiniGameTemplate.Entity
{
    // ──────────────── 碰撞事件类型 ────────────────

    /// <summary>碰撞事件来源类型</summary>
    public enum CollisionEventType : byte
    {
        BulletHit = 0,
        LaserHit = 1,
        SprayHit = 2,
        ContactHit = 3, // P2.2: Entity vs Entity 接触碰撞
    }

    // ──────────────── 伤害上下文（v2.4 GD-R4-001）────────────────

    /// <summary>
    /// 伤害上下文 struct，替代裸 int damage。
    /// 携带攻击者信息 + 命中类型，供伤害管线扩展。
    /// Phase 1：HealthComponent 直接读 BaseDamage 扣血。
    /// Phase 2：游戏层可订阅 OnCollisionHit 在 TakeDamage 前拦截处理（护甲/暴击等）。
    /// </summary>
    public struct DamageContext
    {
        /// <summary>弹幕配置的原始伤害（TypeSO.Damage）</summary>
        public int BaseDamage;

        /// <summary>发射者 EntityId（无发射者时 = Invalid）</summary>
        public EntityId AttackerId;

        /// <summary>命中来源类型（Bullet / Laser / Spray）</summary>
        public CollisionEventType HitType;

        // Phase 2 扩展预留：DamageType (Physical/Magical)、CritMultiplier 等
    }

    // ──────────────── Entity 内部事件 struct 定义 ────────────────
    // 所有事件均为 struct（零 GC），通过 EntityEventBus 在 Entity 内部分发。

    /// <summary>状态切换事件</summary>
    public struct OnStateChanged
    {
        public int OldState;
        public int NewState;
    }

    /// <summary>受伤事件</summary>
    public struct OnDamaged
    {
        public int Damage;
        public int RemainingHp;
        public EntityId Source;
    }

    /// <summary>死亡事件</summary>
    public struct OnDeath
    {
        public EntityId Killer;
    }

    /// <summary>位置变化事件</summary>
    public struct OnPositionChanged
    {
        public Vector2 OldPos;
        public Vector2 NewPos;
    }

    /// <summary>锁定目标事件</summary>
    public struct OnTargetAcquired
    {
        public EntityId Target;
    }

    /// <summary>丢失目标事件</summary>
    public struct OnTargetLost { }

    /// <summary>技能释放事件</summary>
    public struct OnSkillCast
    {
        public int SkillId;
        public EntityId Target;
    }

    /// <summary>动画事件</summary>
    public struct OnAnimEvent
    {
        public int EventId;
    }

    /// <summary>碰撞命中事件（v2.4 携带完整 DamageContext）</summary>
    public struct OnCollisionHit
    {
        public DamageContext Context;
    }
}
