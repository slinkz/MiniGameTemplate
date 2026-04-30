using NUnit.Framework;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;
using UnityEngine;
using CollisionEventType = MiniGameTemplate.Entity.CollisionEventType;

/// <summary>
/// Phase 1.5 验证测试：CollisionComponent
/// 
/// AC 覆盖：
/// - AC-1: 编译通过（lint=0）
/// - AC-2: 弹丸命中 Entity 触发 OnCollisionHit（通过 EntityEventBus）
/// - AC-3: 注册/注销不泄漏槽位
/// 
/// 测试策略：
/// - Editor 测试无 DanmakuSystem.Instance，通过 ForceEnableCollision() 绕过
/// - TargetRegistry 注册/注销逻辑通过直接操作 TargetRegistry 实例验证
/// </summary>
[TestFixture]
public class CollisionComponentTests
{
    private Entity _entity;
    private CollisionComponent _collisionComp;
    private EntityConfigSO _config;

    [SetUp]
    public void SetUp()
    {
        _config = ScriptableObject.CreateInstance<EntityConfigSO>();
        _config.MaxHp = 100;
        _config.CollisionRadius = 0.5f;
        _config.Camp = EnumCamp.Enemy;

        _entity = new Entity();
        _entity.ConfigSO = _config;
        _entity.Camp = _config.Camp;

        _collisionComp = new CollisionComponent();
        _entity.RegisterComponent(_collisionComp);
        _entity.InitAll(new Vector2(3f, 4f), 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_config);
    }

    // ══════════════════════════════════════════════════════════════
    // AC-1: 基础初始化
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void Init_SetsCorrectType()
    {
        Assert.AreEqual(ComponentType.Collision, _collisionComp.Type);
    }

    [Test]
    public void Init_IsActiveByDefault()
    {
        Assert.IsTrue(_collisionComp.IsActive);
    }

    [Test]
    public void Init_WithoutDanmakuSystem_CollisionDisabled()
    {
        // 无 DanmakuSystem.Instance 时，碰撞标记为不可用
        Assert.IsFalse(_collisionComp.IsCollisionEnabled);
        Assert.AreEqual(-1, _collisionComp.TargetSlot);
    }

    // ══════════════════════════════════════════════════════════════
    // AC-2: 弹丸命中触发 OnCollisionHit
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void OnBulletHit_PublishesOnCollisionHit_WithCorrectContext()
    {
        // 强制启用碰撞以绕过 DanmakuSystem 依赖
        _collisionComp.ForceEnableCollision();

        OnCollisionHit receivedEvent = default;
        bool eventReceived = false;
        _entity.EventBus.Subscribe<OnCollisionHit>(evt =>
        {
            eventReceived = true;
            receivedEvent = evt;
        });

        _collisionComp.OnBulletHit(25, 0);

        Assert.IsTrue(eventReceived, "OnCollisionHit should be published on bullet hit");
        Assert.AreEqual(25, receivedEvent.Context.BaseDamage);
        Assert.AreEqual(CollisionEventType.BulletHit, receivedEvent.Context.HitType);
        Assert.AreEqual(EntityId.Invalid, receivedEvent.Context.AttackerId);
    }

    [Test]
    public void OnLaserHit_PublishesOnCollisionHit_WithLaserType()
    {
        _collisionComp.ForceEnableCollision();

        OnCollisionHit receivedEvent = default;
        bool eventReceived = false;
        _entity.EventBus.Subscribe<OnCollisionHit>(evt =>
        {
            eventReceived = true;
            receivedEvent = evt;
        });

        _collisionComp.OnLaserHit(15, 0);

        Assert.IsTrue(eventReceived);
        Assert.AreEqual(15, receivedEvent.Context.BaseDamage);
        Assert.AreEqual(CollisionEventType.LaserHit, receivedEvent.Context.HitType);
    }

    [Test]
    public void OnSprayHit_PublishesOnCollisionHit_WithSprayType()
    {
        _collisionComp.ForceEnableCollision();

        OnCollisionHit receivedEvent = default;
        bool eventReceived = false;
        _entity.EventBus.Subscribe<OnCollisionHit>(evt =>
        {
            eventReceived = true;
            receivedEvent = evt;
        });

        _collisionComp.OnSprayHit(10, 0);

        Assert.IsTrue(eventReceived);
        Assert.AreEqual(10, receivedEvent.Context.BaseDamage);
        Assert.AreEqual(CollisionEventType.SprayHit, receivedEvent.Context.HitType);
    }

    [Test]
    public void OnBulletHit_WhenInactive_DoesNotPublishEvent()
    {
        _collisionComp.ForceEnableCollision();
        _collisionComp.SetActive(false);

        int eventCount = 0;
        _entity.EventBus.Subscribe<OnCollisionHit>(evt => eventCount++);

        _collisionComp.OnBulletHit(50, 0);

        Assert.AreEqual(0, eventCount, "Should not publish event when component is inactive");
    }

    [Test]
    public void OnBulletHit_WhenCollisionDisabled_DoesNotPublishEvent()
    {
        // 默认就是 CollisionDisabled（无 DanmakuSystem），无需额外操作
        int eventCount = 0;
        _entity.EventBus.Subscribe<OnCollisionHit>(evt => eventCount++);

        _collisionComp.OnBulletHit(50, 0);

        Assert.AreEqual(0, eventCount, "Should not publish event when collision is disabled");
    }

    // ══════════════════════════════════════════════════════════════
    // AC-3: 注册/注销不泄漏槽位
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void TargetRegistry_RegisterAndUnregister_NoLeak()
    {
        var registry = new TargetRegistry();

        // 注册
        int slot = registry.Register(_collisionComp);
        Assert.IsTrue(slot >= 0, "Should get a valid slot");
        Assert.AreEqual(1, registry.Count);

        // 注销
        registry.Unregister(_collisionComp);
        Assert.AreEqual(0, registry.Count);
    }

    [Test]
    public void TargetRegistry_MultipleRegisterUnregister_NoSlotLeak()
    {
        var registry = new TargetRegistry();

        // 循环注册/注销 50 次，验证槽位不泄漏
        for (int i = 0; i < 50; i++)
        {
            var entity = new Entity();
            entity.ConfigSO = _config;
            entity.Camp = EnumCamp.Enemy;

            var comp = new CollisionComponent();
            entity.RegisterComponent(comp);
            entity.InitAll(Vector2.zero, 0f);

            int slot = registry.Register(comp);
            Assert.IsTrue(slot >= 0, $"Iteration {i}: Should get valid slot");

            registry.Unregister(comp);
        }

        Assert.AreEqual(0, registry.Count, "All slots should be freed");
    }

    [Test]
    public void TargetRegistry_FullCapacity_ReturnsNegative()
    {
        var registry = new TargetRegistry();
        var components = new CollisionComponent[TargetRegistry.MAX_TARGETS];

        // 填满所有槽位
        for (int i = 0; i < TargetRegistry.MAX_TARGETS; i++)
        {
            var entity = new Entity();
            entity.ConfigSO = _config;
            entity.Camp = EnumCamp.Enemy;

            components[i] = new CollisionComponent();
            entity.RegisterComponent(components[i]);
            entity.InitAll(Vector2.zero, 0f);

            int slot = registry.Register(components[i]);
            Assert.IsTrue(slot >= 0, $"Slot {i} should be valid");
        }

        Assert.AreEqual(TargetRegistry.MAX_TARGETS, registry.Count);

        // 超出上限注册应失败
        var overflowComp = new CollisionComponent();
        int overflowSlot = registry.Register(overflowComp);
        Assert.AreEqual(-1, overflowSlot, "Should return -1 when registry is full");
    }

    [Test]
    public void TargetRegistry_UnregisterFreesSlot_ForNextRegister()
    {
        var registry = new TargetRegistry();
        var components = new CollisionComponent[TargetRegistry.MAX_TARGETS];

        // 填满
        for (int i = 0; i < TargetRegistry.MAX_TARGETS; i++)
        {
            var entity = new Entity();
            entity.ConfigSO = _config;
            entity.Camp = EnumCamp.Enemy;

            components[i] = new CollisionComponent();
            entity.RegisterComponent(components[i]);
            entity.InitAll(Vector2.zero, 0f);

            registry.Register(components[i]);
        }

        // 释放第 3 个槽位
        registry.Unregister(components[2]);
        Assert.AreEqual(TargetRegistry.MAX_TARGETS - 1, registry.Count);

        // 新注册应该成功（复用释放的槽位）
        var newComp = new CollisionComponent();
        int newSlot = registry.Register(newComp);
        Assert.AreEqual(2, newSlot, "Should reuse freed slot index 2");
        Assert.AreEqual(TargetRegistry.MAX_TARGETS, registry.Count);
    }

    // ══════════════════════════════════════════════════════════════
    // Hitbox + Faction
    // ══════════════════════════════════════════════════════════════

    [Test]
    public void Hitbox_ReflectsEntityPosition()
    {
        var hitbox = _collisionComp.Hitbox;
        Assert.AreEqual(3f, hitbox.Center.x, 0.001f);
        Assert.AreEqual(4f, hitbox.Center.y, 0.001f);
        Assert.AreEqual(0.5f, hitbox.Radius, 0.001f);
    }

    [Test]
    public void Hitbox_UpdatesWhenEntityMoves()
    {
        _entity.Position = new Vector2(10f, 20f);
        var hitbox = _collisionComp.Hitbox;
        Assert.AreEqual(10f, hitbox.Center.x, 0.001f);
        Assert.AreEqual(20f, hitbox.Center.y, 0.001f);
    }

    [Test]
    public void Faction_MatchesEntityCamp()
    {
        Assert.AreEqual(EnumCamp.Enemy, _collisionComp.Faction);
    }

    [Test]
    public void IsAlive_TrueWhenOwnerAlive()
    {
        Assert.IsTrue(_collisionComp.IsAlive);
    }

    [Test]
    public void Reset_ClearsState()
    {
        _collisionComp.Reset();
        Assert.AreEqual(-1, _collisionComp.TargetSlot);
        Assert.IsFalse(_collisionComp.IsAlive);
    }
}
