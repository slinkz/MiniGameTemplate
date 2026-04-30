using NUnit.Framework;
using MiniGameTemplate.Entity;
using UnityEngine;

/// <summary>
/// Phase 1.4 验证测试：StateComponent + HealthComponent
/// 
/// AC 覆盖：
/// - AC-1: 编译通过（lint=0）
/// - AC-2: 互斥状态冲突时正确阻止
/// - AC-3: OnDamaged 事件携带正确来源
/// - AC-4: HP=0 触发 OnDeath
/// </summary>
[TestFixture]
public class StateHealthComponentTests
{
    private Entity _entity;
    private StateComponent _stateComp;
    private HealthComponent _healthComp;
    private EntityConfigSO _config;

    [SetUp]
    public void SetUp()
    {
        _config = ScriptableObject.CreateInstance<EntityConfigSO>();
        _config.MaxHp = 100;
        _config.Camp = MiniGameTemplate.Danmaku.EnumCamp.Enemy;

        _entity = new Entity();
        _entity.ConfigSO = _config;
        _entity.Camp = _config.Camp;

        _stateComp = new StateComponent();
        _healthComp = new HealthComponent();

        _entity.RegisterComponent(_stateComp);
        _entity.RegisterComponent(_healthComp);

        _entity.InitAll(Vector2.zero, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_config);
    }

    // ══════════════════════════════════════════════════════════════
    // AC-2: 互斥状态冲突时正确阻止
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void StateComponent_TryAddState_SuccessWhenNoConflict()
    {
        bool result = _stateComp.TryAddState(EntityState.Moving);
        Assert.IsTrue(result);
        Assert.IsTrue(_stateComp.HasState(EntityState.Moving));
    }

    [Test]
    public void StateComponent_TryAddState_BlockedByExclusion()
    {
        // Stunned 与 Moving 互斥
        _stateComp.TryAddState(EntityState.Stunned);
        bool result = _stateComp.TryAddState(EntityState.Moving);

        Assert.IsFalse(result, "Moving should be blocked by Stunned (mutual exclusion)");
        Assert.IsFalse(_stateComp.HasState(EntityState.Moving));
        Assert.IsTrue(_stateComp.HasState(EntityState.Stunned));
    }

    [Test]
    public void StateComponent_TryAddState_StunnedBlocksAttacking()
    {
        _stateComp.TryAddState(EntityState.Stunned);
        bool result = _stateComp.TryAddState(EntityState.Attacking);

        Assert.IsFalse(result, "Attacking should be blocked by Stunned");
        Assert.IsFalse(_stateComp.HasState(EntityState.Attacking));
    }

    [Test]
    public void StateComponent_TryAddState_DeadBlocksOtherStates()
    {
        // ForceAdd Dead（Dead 通常通过 ForceAdd 添加）
        _stateComp.ForceAddState(EntityState.Dead);

        // 尝试添加 Moving → 应被 Dead 阻止
        bool result = _stateComp.TryAddState(EntityState.Moving);
        Assert.IsFalse(result, "Moving should be blocked when Dead");
    }

    [Test]
    public void StateComponent_TryAddState_IdempotentIfAlreadyHas()
    {
        _stateComp.TryAddState(EntityState.Moving);
        bool result = _stateComp.TryAddState(EntityState.Moving);

        Assert.IsTrue(result, "Adding already-existing state should return true (idempotent)");
    }

    [Test]
    public void StateComponent_RemoveState_AllowsPreviouslyBlockedState()
    {
        _stateComp.TryAddState(EntityState.Stunned);
        Assert.IsFalse(_stateComp.TryAddState(EntityState.Moving));

        // 移除 Stunned
        _stateComp.RemoveState(EntityState.Stunned);

        // 现在 Moving 应该可以添加了
        bool result = _stateComp.TryAddState(EntityState.Moving);
        Assert.IsTrue(result);
        Assert.IsTrue(_stateComp.HasState(EntityState.Moving));
    }

    [Test]
    public void StateComponent_ForceAddState_BypassesExclusion()
    {
        _stateComp.TryAddState(EntityState.Moving);

        // ForceAdd Dead 即使有 Moving 也能成功（BypassExclusion）
        _stateComp.ForceAddState(EntityState.Dead);
        Assert.IsTrue(_stateComp.HasState(EntityState.Dead));
    }

    [Test]
    public void StateComponent_OnStateChanged_Published()
    {
        int eventCount = 0;
        OnStateChanged lastEvent = default;

        _entity.EventBus.Subscribe<OnStateChanged>(evt =>
        {
            eventCount++;
            lastEvent = evt;
        });

        _stateComp.TryAddState(EntityState.Moving);

        Assert.AreEqual(1, eventCount);
        Assert.AreEqual(EntityState.Moving, lastEvent.NewState);
    }

    // ══════════════════════════════════════════════════════════════
    // AC-3: OnDamaged 事件携带正确来源
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void HealthComponent_TakeDamage_PublishesOnDamaged_WithCorrectSource()
    {
        OnDamaged receivedEvent = default;
        bool eventReceived = false;

        _entity.EventBus.Subscribe<OnDamaged>(evt =>
        {
            eventReceived = true;
            receivedEvent = evt;
        });

        var attackerId = new EntityId(42);
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 30,
            AttackerId = attackerId,
            HitType = CollisionEventType.BulletHit
        });

        Assert.IsTrue(eventReceived);
        Assert.AreEqual(30, receivedEvent.Damage);
        Assert.AreEqual(70, receivedEvent.RemainingHp);
        Assert.AreEqual(attackerId, receivedEvent.Source);
    }

    [Test]
    public void HealthComponent_TakeDamage_ReducesHp()
    {
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 25,
            AttackerId = EntityId.Invalid,
            HitType = CollisionEventType.BulletHit
        });

        Assert.AreEqual(75, _healthComp.CurrentHp);
    }

    [Test]
    public void HealthComponent_TakeDamage_ZeroDamage_NoEvent()
    {
        int eventCount = 0;
        _entity.EventBus.Subscribe<OnDamaged>(evt => eventCount++);

        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 0,
            AttackerId = EntityId.Invalid,
            HitType = CollisionEventType.BulletHit
        });

        Assert.AreEqual(0, eventCount, "Zero damage should not trigger OnDamaged");
        Assert.AreEqual(100, _healthComp.CurrentHp);
    }

    // ══════════════════════════════════════════════════════════════
    // AC-4: HP=0 触发 OnDeath
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void HealthComponent_HpReachesZero_PublishesOnDeath()
    {
        OnDeath receivedEvent = default;
        bool eventReceived = false;

        _entity.EventBus.Subscribe<OnDeath>(evt =>
        {
            eventReceived = true;
            receivedEvent = evt;
        });

        var killerId = new EntityId(99);
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 100, // 满伤害致死
            AttackerId = killerId,
            HitType = CollisionEventType.BulletHit
        });

        Assert.IsTrue(eventReceived, "OnDeath should be published when HP reaches 0");
        Assert.AreEqual(killerId, receivedEvent.Killer);
        Assert.AreEqual(0, _healthComp.CurrentHp);
        Assert.IsTrue(_healthComp.IsDead);
    }

    [Test]
    public void HealthComponent_HpReachesZero_SetsDeadState()
    {
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 200, // 超杀
            AttackerId = EntityId.Invalid,
            HitType = CollisionEventType.BulletHit
        });

        Assert.IsTrue(_stateComp.HasState(EntityState.Dead),
            "StateComponent should have Dead state after lethal damage");
    }

    [Test]
    public void HealthComponent_AlreadyDead_NoDuplicateDeath()
    {
        int deathCount = 0;
        _entity.EventBus.Subscribe<OnDeath>(evt => deathCount++);

        // 第一次致死
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 100,
            AttackerId = EntityId.Invalid,
            HitType = CollisionEventType.BulletHit
        });

        // 第二次伤害（已死亡）
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 50,
            AttackerId = EntityId.Invalid,
            HitType = CollisionEventType.BulletHit
        });

        Assert.AreEqual(1, deathCount, "OnDeath should only fire once");
    }

    [Test]
    public void HealthComponent_OverkillDamage_HpClampsToZero()
    {
        _healthComp.TakeDamage(new DamageContext
        {
            BaseDamage = 999,
            AttackerId = EntityId.Invalid,
            HitType = CollisionEventType.BulletHit
        });

        Assert.AreEqual(0, _healthComp.CurrentHp, "HP should clamp to 0, not go negative");
    }

    // ══════════════════════════════════════════════════════════════
    // 边界测试
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void StateComponent_Reset_ClearsAllStates()
    {
        _stateComp.TryAddState(EntityState.Moving);
        _stateComp.TryAddState(EntityState.Invincible);

        _stateComp.Reset();

        Assert.IsFalse(_stateComp.HasState(EntityState.Moving));
        Assert.IsFalse(_stateComp.HasState(EntityState.Invincible));
    }

    [Test]
    public void HealthComponent_InitFromConfig_CorrectMaxHp()
    {
        Assert.AreEqual(100, _healthComp.MaxHp);
        Assert.AreEqual(100, _healthComp.CurrentHp);
        Assert.AreEqual(1f, _healthComp.HpRatio, 0.001f);
    }
}
