---
system: entity-component
scope: core-architecture
last_verified: 2026-05-02
depends_on: [EC_TDD_01_OVERVIEW]
related_code: Assets/_Framework/EntitySystem/Core/*.cs, Assets/_Framework/EntitySystem/Components/CollisionComponent.cs
---

### 3.2 核心接口定义

> **v2.1 变更（EC-001/EC-004）**：Init 统一为单参数；新增 ComponentType 枚举实现 O(1) GetComponent。

```csharp
namespace MiniGameTemplate.Entity
{
    /// <summary>Entity 唯一标识</summary>
    public readonly struct EntityId : System.IEquatable<EntityId>
    {
        public readonly uint Value;
        public EntityId(uint value) => Value = value;
        public bool Equals(EntityId other) => Value == other.Value;
        public override int GetHashCode() => (int)Value;
        public static readonly EntityId Invalid = new(0);
    }

    /// <summary>
    /// 组件类型枚举——Entity 内部以此为数组索引实现 O(1) GetComponent。
    /// 新增组件类型时在此枚举追加（最大 16 种，预留扩展）。
    /// </summary>
    public enum ComponentType : byte
    {
        State = 0,
        Health = 1,
        Animation = 2,
        Movement = 3,
        Collision = 4,
        AutoAim = 5,
        Skill = 6,
        Control = 7,
        AI = 8,
        Attack = 9,  // v2.4: Phase 1 最小攻击组件
        // 预留 10~15
        MAX = 16
    }

    /// <summary>组件基接口</summary>
    public interface IEntityComponent
    {
        /// <summary>组件是否激活</summary>
        bool IsActive { get; }

        /// <summary>组件类型枚举（用于 Entity 内部数组索引）</summary>
        ComponentType Type { get; }

        /// <summary>
        /// 初始化（从池取出时调用）。
        /// 组件通过 owner 间接获取配置：owner.Config 提供配置数据（SO 或 Luban）。
        /// </summary>
        void Init(Entity owner);

        /// <summary>重置（归还池时调用，清运行时数据保留对象）</summary>
        void Reset();

        /// <summary>激活/休眠切换</summary>
        void SetActive(bool active);
    }

    /// <summary>需要每帧驱动的组件</summary>
    public interface ITickable
    {
        /// <summary>Tick 排序优先级（升序执行）</summary>
        int TickOrder { get; }

        /// <summary>每帧更新</summary>
        void Tick(float dt);
    }
}
```

**Entity.GetComponent 实现方案**：
```csharp
public class Entity
{
    // 固定长度数组，按 ComponentType 枚举索引
    private readonly IEntityComponent[] _components = new IEntityComponent[(int)ComponentType.MAX];

    // ── v2.4 新增：Pause 支持（GD-R4-011）──
    // Phase 1 预留，Phase 2 用于 HitStop 顿帧。
    // Phase 1 不调用 PauseFor()，IsPaused 永远 false——分支预测器零开销。
    private int _pauseFrames;
    public bool IsPaused => _pauseFrames > 0;
    public void PauseFor(int frames) => _pauseFrames = frames;
    internal void DecrementPauseFrames() { if (_pauseFrames > 0) _pauseFrames--; }

    /// <summary>
    /// 泛型版：O(N) 线性扫描 + is T 类型检查（N≤16，热路径建议用枚举版）。
    /// </summary>
    public T GetComponent<T>() where T : class, IEntityComponent
    {
        for (int i = 0; i < (int)ComponentType.MAX; i++)
        {
            if (_components[i] is T result) return result;
        }
        return null;
    }

    /// <summary>
    /// 枚举版：O(1) 直接数组索引，零类型检查。热路径首选。
    /// </summary>
    public IEntityComponent GetComponent(ComponentType type) => _components[(int)type];
}
```

### 3.3 Tick 优先级常量

```csharp
public static class TickOrders
{
    public const int Decision   = 100;  // ControlComponent / AIComponent
    public const int Attack     = 150;  // AttackComponent（v2.4）
    public const int AutoAim    = 200;  // AutoAimComponent
    public const int Movement   = 300;  // MovementComponent
    public const int Animation  = 400;  // AnimationComponent
}
```

### 3.4 EntityEventBus 设计

> **v2.1 变更（EC-003）**：改为预分配固定长度 Handler 列表，彻底消除 Delegate.Combine GC；补充 TypeId<T> 实现方案。

```csharp
/// <summary>
/// 零 GC 实体本地事件总线。
/// TypeId<T> 通过泛型静态字段递增分配（编译期确定），O(1) 类型分发。
/// Handler 存储用预分配固定数组替代 Delegate.Combine，避免委托链 GC。
/// </summary>
public sealed class EntityEventBus
{
    private const int MAX_EVENT_TYPES = 16;   // 预留 16 种事件类型
    private const int MAX_HANDLERS_PER_TYPE = 4; // 每种事件最多 4 个订阅者

    // 二维预分配数组：[eventTypeId][handlerSlot]
    private readonly System.Delegate[,] _handlers = new System.Delegate[MAX_EVENT_TYPES, MAX_HANDLERS_PER_TYPE];
    private readonly int[] _handlerCounts = new int[MAX_EVENT_TYPES];

    public void Publish<T>(T evt) where T : struct
    {
        int typeId = TypeId<T>.Get(); // v2.3: 懒初始化，Domain Reload 安全
        if (typeId >= MAX_EVENT_TYPES) return;
        int count = _handlerCounts[typeId];
        for (int i = 0; i < count; i++)
        {
            ((System.Action<T>)_handlers[typeId, i])?.Invoke(evt);
        }
    }

    public void Subscribe<T>(System.Action<T> handler) where T : struct
    {
        int typeId = TypeId<T>.Get(); // v2.3: 懒初始化，Domain Reload 安全
        if (typeId >= MAX_EVENT_TYPES) return;
        int count = _handlerCounts[typeId];
        if (count >= MAX_HANDLERS_PER_TYPE) return; // 静默丢弃，开发期 LogWarning
        _handlers[typeId, count] = handler;
        _handlerCounts[typeId] = count + 1;
    }

    public void Unsubscribe<T>(System.Action<T> handler) where T : struct
    {
        int typeId = TypeId<T>.Get(); // v2.3: 懒初始化，Domain Reload 安全
        if (typeId >= MAX_EVENT_TYPES) return;
        int count = _handlerCounts[typeId];
        for (int i = 0; i < count; i++)
        {
            if (_handlers[typeId, i] == (System.Delegate)handler)
            {
                // swap-remove
                _handlers[typeId, i] = _handlers[typeId, count - 1];
                _handlers[typeId, count - 1] = null;
                _handlerCounts[typeId] = count - 1;
                return;
            }
        }
    }

    public void ClearAll()
    {
        System.Array.Clear(_handlers, 0, _handlers.Length);
        System.Array.Clear(_handlerCounts, 0, _handlerCounts.Length);
    }
}

/// <summary>
/// 泛型事件类型 ID 分配器。利用泛型静态字段实现自动递增。
/// IL2CPP/AOT 安全——每个 T 的静态字段在首次访问时初始化。
/// 
/// v2.3 变更（SA-004）：从 static readonly 改为 static int + 懒初始化，
/// 解决 Domain Reload 后 TypeId 乱序导致 EventBus 事件分发错误的问题。
/// static readonly 字段在 Domain Reload 后不会被重新赋值（CLR 语义），
/// 而 static int + 懒初始化可以在 Reset 后重新分配正确的 ID。
/// </summary>
private static class TypeId<T> where T : struct
{
    public static int Value = -1; // -1 = 未分配

    public static int Get()
    {
        if (Value < 0) Value = TypeIdCounter.Next();
        return Value;
    }
}
private static class TypeIdCounter
{
    private static int _next;
    private static readonly System.Collections.Generic.List<System.Action> _resetCallbacks = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _next = 0;
        // 重置所有已分配的 TypeId（通过回调列表）
        for (int i = 0; i < _resetCallbacks.Count; i++)
            _resetCallbacks[i]?.Invoke();
        _resetCallbacks.Clear();
    }

    public static int Next()
    {
        int id = System.Threading.Interlocked.Increment(ref _next) - 1;
        // 注册重置回调（利用闭包无法捕获泛型类型参数，故在调用侧注册）
        return id;
    }

    /// <summary>注册 Domain Reload 时的 TypeId 重置回调</summary>
    public static void RegisterResetCallback(System.Action callback) => _resetCallbacks.Add(callback);
}

// ── 伤害上下文（v2.4 新增，GD-R4-001）──
// 替代裸 int damage，携带攻击者信息 + 命中类型，供伤害管线扩展。
// Phase 1 HealthComponent 直接读 BaseDamage 扣血；
// Phase 2 游戏层可订阅 OnCollisionHit 在 TakeDamage 前拦截处理（护甲/暴击等）。
public struct DamageContext
{
    public int BaseDamage;              // 弹幕配置的原始伤害（TypeSO.Damage）
    public EntityId AttackerId;         // 发射者 EntityId（无发射者时 = Invalid）
    public CollisionEventType HitType;  // Bullet / Laser / Spray
    // Phase 2 扩展预留：DamageType (Physical/Magical)、CritMultiplier 等
}

// 事件 struct 定义
public struct OnStateChanged { public int OldState; public int NewState; }
public struct OnDamaged { public int Damage; public int RemainingHp; public EntityId Source; }
public struct OnDeath { public EntityId Killer; }
public struct OnPositionChanged { public Vector2 OldPos; public Vector2 NewPos; }
public struct OnTargetAcquired { public EntityId Target; }
public struct OnTargetLost { }
public struct OnSkillCast { public int SkillId; public EntityId Target; }
public struct OnAnimEvent { public int EventId; }
// v2.4 变更（GD-R4-001）：OnCollisionHit 改为携带完整 DamageContext
public struct OnCollisionHit { public DamageContext Context; }
```

### 3.5 CollisionComponent → ICollisionTarget 桥接

> **v2.4 变更（GD-R4-001）**：OnBulletHit 等回调改为构造 DamageContext（携带攻击者信息），替代裸 int damage。

```csharp
/// <summary>
/// 将 Entity 桥接到弹幕碰撞系统。
/// 实现 ICollisionTarget，复用 TargetRegistry 的 64 槽位（v2.2 扩容）。
/// v2.4：碰撞回调构造 DamageContext 发布到 EntityEventBus。
/// </summary>
public class CollisionComponent : IEntityComponent, ICollisionTarget
{
    private Entity _owner;
    private float _radius;
    private int _targetSlot = -1;

    // ── ICollisionTarget 实现 ──
    public CircleHitbox Hitbox => new(_owner.Position, _radius);
    public EnumCamp Faction => _owner.Camp;

    // v2.4: 构造 DamageContext，携带 AttackerId（从 BulletCore.OwnerEntityId 获取）
    public void OnBulletHit(int damage, int bulletIndex)
    {
        // CollisionSolver 需将 BulletCore.OwnerEntityId 传入（v2.4 新增参数）
        // Phase 1 暂用 EntityId.Invalid 作为 fallback
        _owner.EventBus.Publish(new OnCollisionHit
        {
            Context = new DamageContext
            {
                BaseDamage = damage,
                AttackerId = EntityId.Invalid, // Phase 1: 由 CollisionSolver 填充
                HitType = CollisionEventType.BulletHit
            }
        });
    }

    public void OnLaserHit(int damage, int laserIndex) { /* 同上模式，HitType=LaserHit */ }
    public void OnSprayHit(int damage, int sprayIndex) { /* 同上模式，HitType=SprayHit */ }

    // ── IEntityComponent 实现 ──
    public void Init(Entity owner)
    {
        _owner = owner;
        _radius = owner.ConfigSO.CollisionRadius;
        // 注册到弹幕碰撞系统（直接调用 TargetRegistry 以获取槽位索引）
        var ds = DanmakuSystem.Instance;
        if (ds != null)
        {
            _targetSlot = ds.TargetRegistry.Register(this);
            if (_targetSlot < 0)
            {
                Debug.LogError($"[CollisionComponent] Entity {_owner.Id} 注册碰撞目标失败：TargetRegistry 已满（64/64），需扩容");
                _isCollisionEnabled = false;
            }
        }
    }

    public void Reset()
    {
        if (_targetSlot >= 0)
        {
            var ds = DanmakuSystem.Instance;
            if (ds != null) ds.TargetRegistry.Unregister(this);
        }
        _targetSlot = -1;
        _isCollisionEnabled = true;
    }
}
```

**集成约束**：
- TargetRegistry 硬上限 64 个目标（v2.2 从 16 扩容，天命人决策 D-01）。超出后 LogError 提示需扩容。
- Entity vs Entity 的碰撞不走 TargetRegistry（那是弹丸 vs 目标），走独立的 EntityCollisionSolver（Phase 2 实现）。

