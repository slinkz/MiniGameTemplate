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

    // ──────────────── 伤害类型（P2.4 新增）────────────────

    /// <summary>
    /// 伤害属性类型。用于抗性计算（P2.4 GD-R4-001）。
    /// Phase 2：HealthComponent 的 IDamageModifier 链根据此类型应用不同减伤公式。
    /// </summary>
    public enum DamageType : byte
    {
        Physical = 0,   // 物理伤害
        Magical = 1,    // 魔法伤害
        Pure = 2,       // 纯粹伤害（无视抗性）
    }

    // ──────────────── 伤害上下文（v2.4 GD-R4-001 / P2.4 扩展）────────────────

    /// <summary>
    /// 伤害上下文 struct，替代裸 int damage。
    /// 携带攻击者信息 + 命中类型 + 伤害属性，供伤害管线扩展。
    /// P2.4 扩展：新增 DamageType / CritMultiplier / IsCritical / FinalDamage。
    /// 
    /// 伤害流程：
    ///   BaseDamage → IDamageModifier 链处理 → 写入 FinalDamage → HealthComponent 读 FinalDamage 扣血
    /// </summary>
    public struct DamageContext
    {
        /// <summary>弹幕配置的原始伤害（TypeSO.Damage）</summary>
        public int BaseDamage;

        /// <summary>发射者 EntityId（无发射者时 = Invalid）</summary>
        public EntityId AttackerId;

        /// <summary>命中来源类型（Bullet / Laser / Spray / Contact）</summary>
        public CollisionEventType HitType;

        /// <summary>伤害属性类型（P2.4 新增）</summary>
        public DamageType Type;

        /// <summary>暴击倍率（1.0 = 无暴击，>1.0 = 暴击）（P2.4 新增）</summary>
        public float CritMultiplier;

        /// <summary>是否暴击（P2.4 新增）</summary>
        public bool IsCritical;

        /// <summary>
        /// 伤害来源位置（子弹/激光/喷雾命中点）。
        /// 用于计算击退方向——从 SourcePosition 指向被击者位置。
        /// 当 HasSourcePosition=false 时忽略此字段。
        /// </summary>
        public Vector2 SourcePosition;

        /// <summary>SourcePosition 是否有效（避免 Vector2.zero 歧义）</summary>
        public bool HasSourcePosition;

        /// <summary>
        /// 经 IDamageModifier 链修正后的最终伤害。
        /// HealthComponent 实际扣血使用此值（而非 BaseDamage）。
        /// 未经修正时默认 = 0，HealthComponent 内部 fallback 到 BaseDamage。
        /// </summary>
        public int FinalDamage;
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
