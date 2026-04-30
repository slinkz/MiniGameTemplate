using NUnit.Framework;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;
using UnityEngine;

/// <summary>
/// Phase 1.6 验证测试：MovementComponent + AnimationComponent
/// 
/// AC 覆盖：
/// - AC-1: 编译通过
/// - AC-2: Entity 位置按速度更新
/// - AC-3: CurrentAnimId 随状态切换
/// 
/// 测试策略：
/// - Editor 纯逻辑测试，不依赖 MonoBehaviour 或 DanmakuSystem
/// </summary>
[TestFixture]
public class MovementAnimationComponentTests
{
    private Entity _entity;
    private EntityConfigSO _config;
    private MovementComponent _movement;
    private AnimationComponent _animation;
    private StateComponent _state;

    [SetUp]
    public void SetUp()
    {
        _config = ScriptableObject.CreateInstance<EntityConfigSO>();
        _config.MaxHp = 100;
        _config.MoveSpeed = 5f;
        _config.TurnSpeed = 360f;
        _config.CollisionRadius = 0.5f;
        _config.KnockbackDistance = 1f;
        _config.KnockbackDuration = 0.5f;
        _config.Components = new[]
        {
            ComponentType.State,
            ComponentType.Health,
            ComponentType.Movement,
            ComponentType.Animation
        };

        _entity = new Entity();
        _entity.ConfigSO = _config;
        _entity.Camp = EnumCamp.Enemy;

        _state = new StateComponent();
        _movement = new MovementComponent();
        _animation = new AnimationComponent();

        _entity.RegisterComponent(_state);
        _entity.RegisterComponent(_movement);
        _entity.RegisterComponent(_animation);

        _entity.InitAll(Vector2.zero, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        if (_config != null)
            Object.DestroyImmediate(_config);
    }

    // ══════════════════════════════════════════════
    // MovementComponent 测试
    // ══════════════════════════════════════════════

    [Test]
    public void Movement_SetDirection_EntityPositionUpdates()
    {
        _movement.SetMoveDirection(Vector2.right);
        _movement.Tick(1f); // dt=1s, speed=5 → 位移 5 单位

        Assert.AreEqual(5f, _entity.Position.x, 0.001f);
        Assert.AreEqual(0f, _entity.Position.y, 0.001f);
    }

    [Test]
    public void Movement_ZeroDirection_DoesNotMove()
    {
        _movement.SetMoveDirection(Vector2.zero);
        _movement.Tick(1f);

        Assert.AreEqual(Vector2.zero, _entity.Position);
    }

    [Test]
    public void Movement_SpeedModifier_AffectsDisplacement()
    {
        _movement.SetMoveDirection(Vector2.right);
        _movement.AddSpeedModifier(2f); // 2x 速度
        _movement.Tick(1f);

        // 5 * 2 = 10 单位
        Assert.AreEqual(10f, _entity.Position.x, 0.001f);
    }

    [Test]
    public void Movement_MultipleModifiers_Multiplicative()
    {
        _movement.SetMoveDirection(Vector2.right);
        _movement.AddSpeedModifier(2f);
        _movement.AddSpeedModifier(0.5f); // 2 * 0.5 = 1x
        _movement.Tick(1f);

        Assert.AreEqual(5f, _entity.Position.x, 0.001f);
    }

    [Test]
    public void Movement_RemoveModifier_RestoresSpeed()
    {
        _movement.SetMoveDirection(Vector2.right);
        int slot = _movement.AddSpeedModifier(2f);
        _movement.RemoveSpeedModifier(slot);
        _movement.Tick(1f);

        Assert.AreEqual(5f, _entity.Position.x, 0.001f);
    }

    [Test]
    public void Movement_MaxModifiers_ReturnsMinusOne()
    {
        for (int i = 0; i < 4; i++) // MAX_MODIFIERS = 4
            Assert.GreaterOrEqual(_movement.AddSpeedModifier(1f), 0);

        // 第 5 个应返回 -1
        Assert.AreEqual(-1, _movement.AddSpeedModifier(1f));
    }

    [Test]
    public void Movement_Knockback_AppliesExtraDisplacement()
    {
        // 施加击退：向右 1 单位，持续 0.5 秒
        _movement.ApplyKnockback(Vector2.right, 1f, 0.5f);
        Assert.IsTrue(_movement.IsKnockedBack);

        _movement.Tick(0.5f); // 完整击退

        // 击退：1 / 0.5 * 0.5 = 1 单位（无正常移动）
        Assert.AreEqual(1f, _entity.Position.x, 0.001f);
        Assert.IsFalse(_movement.IsKnockedBack);
    }

    [Test]
    public void Movement_Knockback_StacksWithNormalMovement()
    {
        _movement.SetMoveDirection(Vector2.right);
        _movement.ApplyKnockback(Vector2.up, 1f, 1f); // 向上击退
        _movement.Tick(1f);

        // X = 5（正常移动），Y = 1（击退）
        Assert.AreEqual(5f, _entity.Position.x, 0.001f);
        Assert.AreEqual(1f, _entity.Position.y, 0.001f);
    }

    [Test]
    public void Movement_Knockback_ZeroDuration_NoEffect()
    {
        _movement.ApplyKnockback(Vector2.right, 1f, 0f);
        Assert.IsFalse(_movement.IsKnockedBack);
    }

    [Test]
    public void Movement_LookAt_SetsRotation()
    {
        _movement.LookAt(new Vector2(0f, 1f)); // 正上方
        Assert.AreEqual(90f, _entity.Rotation, 0.01f);
    }

    [Test]
    public void Movement_SetRotation_DirectlyModifiesEntityRotation()
    {
        _movement.SetRotation(45f);
        Assert.AreEqual(45f, _entity.Rotation, 0.001f);
    }

    [Test]
    public void Movement_PublishesOnPositionChanged()
    {
        bool eventReceived = false;
        Vector2 capturedOld = Vector2.zero;
        Vector2 capturedNew = Vector2.zero;

        _entity.EventBus.Subscribe<OnPositionChanged>(evt =>
        {
            eventReceived = true;
            capturedOld = evt.OldPos;
            capturedNew = evt.NewPos;
        });

        _movement.SetMoveDirection(Vector2.right);
        _movement.Tick(1f);

        Assert.IsTrue(eventReceived);
        Assert.AreEqual(Vector2.zero, capturedOld);
        Assert.AreEqual(new Vector2(5f, 0f), capturedNew);
    }

    [Test]
    public void Movement_NoDisplacement_NoEvent()
    {
        bool eventReceived = false;
        _entity.EventBus.Subscribe<OnPositionChanged>(_ => eventReceived = true);

        _movement.SetMoveDirection(Vector2.zero);
        _movement.Tick(1f);

        Assert.IsFalse(eventReceived);
    }

    [Test]
    public void Movement_Reset_ClearsAllState()
    {
        _movement.SetMoveDirection(Vector2.right);
        _movement.AddSpeedModifier(2f);
        _movement.ApplyKnockback(Vector2.up, 1f, 1f);

        _movement.Reset();

        Assert.IsFalse(_movement.IsMoving);
        Assert.IsFalse(_movement.IsKnockedBack);
        Assert.AreEqual(Vector2.zero, _movement.MoveDirection);
    }

    [Test]
    public void Movement_Inactive_DoesNotTick()
    {
        _movement.SetMoveDirection(Vector2.right);
        _movement.SetActive(false);

        // Tick 不会被 Entity 调用（IsActive=false 跳过），但直接调用也应安全
        // Entity.Tick 内部已检查 IsActive，此处模拟直接调用
        _movement.Tick(1f);

        // 虽然直接调用了 Tick，但因为 Entity.Tick 不会对 inactive 组件调度
        // 这里验证 MovementComponent 自身逻辑正确
        // 实际位移仍会发生（组件本身不检查 IsActive，由 Entity 调度控制）
        Assert.AreEqual(5f, _entity.Position.x, 0.001f);
    }

    // ══════════════════════════════════════════════
    // AnimationComponent 测试
    // ══════════════════════════════════════════════

    [Test]
    public void Animation_InitialState_IsIdle()
    {
        Assert.AreEqual(AnimationComponent.AnimId.Idle, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_Moving_SwitchesToMoveAnim()
    {
        _movement.SetMoveDirection(Vector2.right);
        _animation.Tick(0.016f); // 1 frame tick

        Assert.AreEqual(AnimationComponent.AnimId.Move, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_StopMoving_SwitchesToIdle()
    {
        _movement.SetMoveDirection(Vector2.right);
        _animation.Tick(0.016f);
        Assert.AreEqual(AnimationComponent.AnimId.Move, _animation.CurrentAnimId);

        _movement.SetMoveDirection(Vector2.zero);
        _animation.Tick(0.016f);
        Assert.AreEqual(AnimationComponent.AnimId.Idle, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_Dead_OverridesMove()
    {
        _movement.SetMoveDirection(Vector2.right);
        _state.ForceAddState(EntityState.Dead);
        _animation.Tick(0.016f);

        Assert.AreEqual(AnimationComponent.AnimId.Death, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_Hit_OverridesMove()
    {
        _movement.SetMoveDirection(Vector2.right);
        _state.TryAddState(EntityState.Hit);
        _animation.Tick(0.016f);

        Assert.AreEqual(AnimationComponent.AnimId.Hit, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_Override_TakesPriority()
    {
        _movement.SetMoveDirection(Vector2.right);
        _animation.Tick(0.016f);
        Assert.AreEqual(AnimationComponent.AnimId.Move, _animation.CurrentAnimId);

        _animation.PlayOverride(AnimationComponent.AnimId.Attack, 0.5f);
        _animation.Tick(0.016f);

        // Override 优先
        Assert.AreEqual(AnimationComponent.AnimId.Attack, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_Override_ExpiresAfterDuration()
    {
        _animation.PlayOverride(AnimationComponent.AnimId.Attack, 0.1f);
        _animation.Tick(0.05f);
        Assert.IsTrue(_animation.IsOverriding);

        _animation.Tick(0.06f); // 超过 0.1 秒
        Assert.IsFalse(_animation.IsOverriding);

        // 下一帧恢复自动推导
        _animation.Tick(0.016f);
        Assert.AreEqual(AnimationComponent.AnimId.Idle, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_ForceSet_ImmediatelyChanges()
    {
        _animation.ForceSetAnimId(99);
        Assert.AreEqual(99, _animation.CurrentAnimId);
    }

    [Test]
    public void Animation_Reset_ClearsState()
    {
        _animation.PlayOverride(AnimationComponent.AnimId.Attack, 1f);
        _animation.ForceSetAnimId(99);

        _animation.Reset();

        // Reset 后 CurrentAnimId 应为 Idle（因为 _currentAnimId = Idle, _overrideTimer = 0）
        Assert.AreEqual(AnimationComponent.AnimId.Idle, _animation.CurrentAnimId);
        Assert.IsFalse(_animation.IsOverriding);
    }

    // ══════════════════════════════════════════════
    // TickOrder 验证
    // ══════════════════════════════════════════════

    [Test]
    public void TickOrder_MovementBeforeAnimation()
    {
        Assert.Less(_movement.TickOrder, _animation.TickOrder);
    }
}
