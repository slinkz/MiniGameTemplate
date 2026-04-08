# 弹幕系统 — SO 配置体系

> **预计阅读**：20 分钟 &nbsp;|&nbsp; **前置**：先读 [弹幕系统总览](DANMAKU_SYSTEM.md) 了解整体架构
>
> 本文档覆盖弹幕系统的所有 ScriptableObject 定义：弹丸/激光/喷雾/障碍物类型、弹幕模式、组合引擎、发射器/难度配置、系统配置拆分。

---

## 资产目录结构

```
Assets/_Game/ScriptableObjects/Danmaku/
├── BulletTypes/              # 弹丸视觉+行为
│   ├── SmallOrb.asset
│   ├── Needle.asset          # 米粒弹（rotateToDirection=true）
│   └── BigOrb_Glow.asset    # 大玉发光弹（renderLayer=Additive）
├── LaserTypes/               # 激光视觉+行为
├── SprayTypes/               # 喷雾视觉+行为
├── ObstacleTypes/            # 障碍物类型
├── Patterns/                 # 弹幕发射模式
├── PatternGroups/            # 弹幕组合
├── Config/
│   ├── WorldConfig.asset     # 容量、世界边界、碰撞网格
│   ├── RenderConfig.asset    # 材质、贴图、图集
│   ├── TypeRegistry.asset    # 弹丸/激光/喷雾类型注册表
│   └── TimeScale.asset       # 时间缩放
├── Difficulty/               # Easy / Normal / Hard
└── Atlas/                    # 弹丸图集 + 数字精灵图集 + 激光纹理
```

---

## 弹丸类型

### BulletTypeSO

控制弹丸的全部视觉效果和碰撞行为。美术在 Inspector 里配，不碰代码。

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Type")]
public class BulletTypeSO : ScriptableObject
{
    [Header("视觉")]
    public Rect AtlasUV;                  // 图集 UV 矩形
    public Color Tint = Color.white;
    public Vector2 Size = new(0.2f, 0.2f);
    public bool RotateToDirection;        // 朝飞行方向旋转（米粒弹等）

    [Header("碰撞")]
    public float CollisionRadius = 0.1f;

    [Header("伤害")]
    [Min(0)] public int Damage = 1;

    [Header("生命值")]
    [Range(1, 255)] public byte InitialHitPoints = 1;

    [Header("阵营")]
    public BulletFaction Faction = BulletFaction.Enemy;

    [Header("碰撞响应 — 碰到对象（玩家/敌人）")]
    public CollisionResponse OnHitTarget = CollisionResponse.Die;
    [Range(1, 255)] public byte HitTargetHPCost = 1;

    [Header("碰撞响应 — 碰到障碍物")]
    public CollisionResponse OnHitObstacle = CollisionResponse.Die;
    [Range(1, 255)] public byte HitObstacleHPCost = 1;

    [Header("碰撞响应 — 碰到屏幕边缘")]
    public CollisionResponse OnHitScreenEdge = CollisionResponse.Die;
    [Range(1, 255)] public byte HitScreenEdgeHPCost = 1;
    public float ScreenEdgeRecycleDistance = 1f;

    [Header("碰撞反馈")]
    public PoolDefinition BounceEffect;
    public AudioClipSO BounceSFX;
    public AudioClipSO PierceSFX;
    public Color DamageFlashTint = new Color(1, 0.3f, 0.3f, 1);
    public byte DamageFlashFrames = 3;

    [Header("拖尾")]
    public TrailMode Trail = TrailMode.None;
    public byte GhostCount = 3;
    public int TrailPointCount = 20;
    public float TrailWidth = 0.3f;
    public AnimationCurve TrailWidthCurve;
    public Gradient TrailColor;

    [Header("爆炸")]
    public ExplosionMode Explosion = ExplosionMode.MeshFrame;
    public int ExplosionFrameCount = 4;
    public PoolDefinition HeavyExplosionPrefab;

    [Header("子弹幕")]
    public BulletPatternSO ChildPattern;

    [Header("渲染层")]
    public RenderLayer Layer = RenderLayer.Normal;

    [HideInInspector] public ushort RuntimeIndex;
}

public enum TrailMode { None, Ghost, Trail, Both }
public enum ExplosionMode { None, MeshFrame, PooledPrefab }
public enum RenderLayer { Normal, Additive }
```

---

## 激光与喷雾类型

### LaserTypeSO

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Laser Type")]
public class LaserTypeSO : ScriptableObject
{
    [Header("视觉")]
    public Texture2D LaserTexture;
    public float UVScrollSpeed = 2f;
    public Color CoreColor = Color.white;
    public Color EdgeColor = Color.cyan;
    public AnimationCurve WidthProfile;

    [Header("宽度生命周期曲线")]
    public AnimationCurve WidthOverLifetime = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("阶段时长")]
    public float ChargeDuration = 0.5f;
    public float FiringDuration = 2f;
    public float FadeDuration = 0.3f;

    [Header("伤害")]
    public float DamagePerTick = 10f;
    public float TickInterval = 0.1f;

    [Header("碰撞")]
    public float MaxWidth = 0.8f;

    [HideInInspector] public byte RuntimeIndex;

    public float TotalDuration => ChargeDuration + FiringDuration + FadeDuration;
}
```

> **宽度驱动方式**：`LaserUpdater` 根据 `elapsed / TotalDuration` 采样 `WidthOverLifetime` 曲线，乘以 `MaxWidth` 得到当前宽度。Phase 仅控制碰撞开关。

### SprayTypeSO

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spray Type")]
public class SprayTypeSO : ScriptableObject
{
    [Header("视觉")]
    public PoolDefinition ParticleEffectPrefab;

    [Header("判定")]
    public float ConeAngle = 30f;
    public float Range = 5f;

    [Header("伤害")]
    public float DamagePerTick = 5f;
    public float TickInterval = 0.5f;

    [HideInInspector] public byte RuntimeIndex;
}
```

> **校验机制**：CustomEditor 在 Scene View 绘制判定扇形 Gizmo，和 ParticleSystem 视觉范围叠加对比，偏差 > 5° 弹出黄色警告。

---

## 弹幕模式与组合

### BulletPatternSO — 弹幕发射模式

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Pattern")]
public class BulletPatternSO : ScriptableObject
{
    [Header("弹幕类型")]
    public BulletTypeSO BulletType;

    [Header("发射参数")]
    public int Count = 12;
    public float SpreadAngle = 360f;
    public float StartAngle = 0f;
    public float AnglePerShot = 0f;

    [Header("运动")]
    [Range(0.1f, 20f)] public float Speed = 5f;
    public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);
    public float Lifetime = 5f;

    [Header("延迟变速")]
    public float DelayBeforeAccel = 0f;
    [Range(0f, 1f)] public float DelaySpeedScale = 0f;
    public float AccelDuration = 0.3f;

    [Header("追踪")]
    public bool IsHoming;
    public float HomingStrength = 2f;
    public float HomingDelay = 0f;

    [Header("连射")]
    public int BurstCount = 1;
    public float BurstInterval = 0.05f;

    [Header("音效")]
    public AudioClipSO FireSFX;
}
```

### PatternGroupSO — 弹幕组合

一个 `PatternGroupSO` 将多个 `BulletPatternSO` 编排在一起：

| 需求 | 实现方式 |
|------|---------|
| **多层弹幕**（外圈快内圈慢） | 两个 PatternEntry，不同 Speed |
| **延迟变速** | PatternSO 的 `DelayBeforeAccel` + `AccelDuration` |
| **弹幕嵌套** | BulletTypeSO 的 `ChildPattern` 引用 |
| **时序编排** | PatternEntry 的 `Delay` 字段 |
| **重复组合** | `RepeatCount` + `RepeatInterval` |

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Pattern Group")]
public class PatternGroupSO : ScriptableObject
{
    public PatternEntry[] Entries;
    public int RepeatCount = 1;
    public float RepeatInterval = 0.5f;
    public float AngleIncrementPerRepeat = 0f;
}

[System.Serializable]
public struct PatternEntry
{
    public BulletPatternSO Pattern;
    public float Delay;
    public float AngleOverride;
    public bool AimAtPlayer;
}
```

#### 用法示例

**示例 1：双层环弹** — 两个 Entry（SlowInnerRing Speed=2 + FastOuterRing Speed=6），同时发射。

**示例 2：静止后追踪弹** — `DelayBeforeAccel=0.5`, `DelaySpeedScale=0`, `AccelDuration=0.3`, `IsHoming=true`, `HomingDelay=0.5`。

**示例 3：母弹爆炸散射** — BulletTypeSO `MotherOrb` 设 `ChildPattern = ScatterShot`，消亡时以当前位置为 origin 发射子弹幕。

**示例 4：Boss 三轮花弹幕** — `RepeatCount=3`, `RepeatInterval=0.4`, `AngleIncrementPerRepeat=15`。

### PatternScheduler — 组合执行引擎

> **设计决策 P0-4**：`ScheduleTask` 用 `byte PatternIndex` 索引查表，不直接持有 SO 引用（避免值类型 struct 中嵌入引用类型）。
>
> **设计决策 P1-5**：硬上限 **64 槽**。溢出时静默丢弃 + `GameLog.LogWarning`。
>
> **设计决策 P1-6**：`AimAtPlayer`（发射时快照 position）和 `FLAG_HOMING`（飞行中实时追踪）是两个独立需求。

```csharp
public class PatternScheduler
{
    private const int MAX_TASKS = 64;
    private readonly BulletPatternSO[] _patterns = new BulletPatternSO[MAX_TASKS];
    private readonly ScheduleTask[] _tasks = new ScheduleTask[MAX_TASKS];
    private int _activeCount;

    public void Schedule(PatternGroupSO group, Vector2 origin, float baseAngle, Transform aimTarget) { /* ... */ }
    public void Update(float dt, DanmakuSystem system) { /* ... */ }
}

private struct ScheduleTask
{
    public byte PatternIndex;
    public Vector2 Origin;
    public float Angle;
    public float Timer;
    public int RemainingRepeats;
    public float RepeatInterval;
    public float AngleIncrement;
    public Vector2 AimSnapshot;
}
```

### BulletSpawner — 弹丸发射器

无状态 static 类，将 `BulletPatternSO` 配置翻译为 BulletCore + BulletTrail + BulletModifier 写入 `BulletWorld`。

```csharp
public static class BulletSpawner
{
    public static void Fire(
        BulletPatternSO pattern, Vector2 origin, float baseAngleDeg,
        BulletWorld world, DanmakuTypeRegistry registry,
        DifficultyProfileSO difficulty = null)
    {
        // 1. 应用难度乘数（count × CountMultiplier, speed × SpeedMultiplier）
        // 2. 角度展开（SpreadAngle / Count）
        // 3. 写入 Core（Position/Velocity/Lifetime/Radius/TypeIndex/Phase/HP/Flags/Faction）
        // 4. 条件标记（RotateToDirection/Homing/Trail/ChildPattern）
        // 5. 写入 Trail（GhostCount/PrevPos）
        // 6. 写入 Modifier（DelayEndTime/AccelEndTime/HomingStartTime，仅 FLAG_HAS_MODIFIER）
        // 7. SpeedOverLifetime 曲线（与延迟变速互斥）
    }
}
```

> **Burst 连射**：`PatternScheduler` 负责 Burst 时序——每次 BurstInterval 调用一次 `BulletSpawner.Fire()`，共 `BurstCount` 次。

---

## 配置 SO 拆分

原 `DanmakuConfigSO` 拆为三个独立 SO，避免策划改难度和美术改材质互相冲突。

### DanmakuWorldConfig — 容量 + 世界规则

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/World")]
public class DanmakuWorldConfig : ScriptableObject
{
    [Header("容量")]
    public int MaxBullets = 2048;
    public int MaxLasers = 16;
    public int MaxSprays = 8;

    [Header("世界边界")]
    public Rect WorldBounds = new(-6, -10, 12, 20);

    [Header("碰撞网格")]
    public int GridCellsX = 12;
    public int GridCellsY = 20;

    [Header("无敌帧")]
    public float InvincibleDuration = 0f;
}
```

### DanmakuRenderConfig — 渲染资产

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Render")]
public class DanmakuRenderConfig : ScriptableObject
{
    public Material BulletMaterial;
    public Material BulletAdditiveMaterial;
    public Material LaserMaterial;
    public Texture2D BulletAtlas;
    public Texture2D NumberAtlas;
}
```

### DanmakuTypeRegistry — 类型注册表

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Type Registry")]
public class DanmakuTypeRegistry : ScriptableObject
{
    public BulletTypeSO[] BulletTypes;
    public LaserTypeSO[] LaserTypes;
    public SprayTypeSO[] SprayTypes;

    public void AssignRuntimeIndices() { /* Awake 时给每个 TypeSO 分配索引 */ }
}
```

---

## 时间缩放

### DanmakuTimeScaleSO

弹幕系统有自己的时间源，和 `Time.timeScale` 独立：

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Time Scale")]
public class DanmakuTimeScaleSO : ScriptableObject
{
    [Range(0f, 2f)] public float TimeScale = 1f;
    public float DeltaTime => Time.deltaTime * TimeScale;
    public void SetSlowMotion(float scale) => TimeScale = scale;
    public void ResetSpeed() => TimeScale = 1f;
}
```

> **⚠️ 热路径编码规范**：遍历弹丸数组的循环**必须在循环外缓存 `float dt = _timeScale.DeltaTime`**，然后以参数传入。禁止在热循环内部访问 SO 属性。

用途：
- `TimeScale = 0.3` → 弹幕慢放，玩家正常操作（子弹时间）
- 不同 TimeScaleSO → 分层减速（Boss 弹幕减速但杂兵正常）

---

## 发射器与难度

### SpawnerProfileSO — 发射器配置

> **设计决策 P1-7**：和 PatternGroupSO 是不同层级——PatternGroupSO 描述"怎么发"，SpawnerProfileSO 描述"一个 Boss/敌人挂哪些组、怎么切换"。

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spawner Profile")]
public class SpawnerProfileSO : ScriptableObject
{
    public PatternGroupSO[] PatternGroups;
    public float CooldownBetweenGroups = 2f;
    public SpawnerSwitchMode SwitchMode = SpawnerSwitchMode.Sequential;
}

public enum SpawnerSwitchMode { Sequential, Random, External }
```

### DifficultyProfileSO — 难度配置

> **设计决策 P1-8**：混合模式——基础乘数 + 少量 Pattern 替换。

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Difficulty Profile")]
public class DifficultyProfileSO : ScriptableObject
{
    public float SpeedMultiplier = 1f;
    public float CountMultiplier = 1f;
    public float LifetimeMultiplier = 1f;
    public PatternOverride[] PatternOverrides;
}

[System.Serializable]
public struct PatternOverride
{
    public PatternGroupSO Original;
    public PatternGroupSO Replacement;
}
```

### PoolDefinition — 对象池配置（复用框架）

> **设计决策 P1-10**：弹幕系统**直接复用**框架的 `MiniGameTemplate.Pool.PoolDefinition`（SO），不自定义同名类型。BulletTypeSO 的 `BounceEffect`、`HeavyExplosionPrefab`，ObstacleTypeSO 的 `DestroyEffect`，SprayTypeSO 的 `ParticleEffectPrefab` 均为此类型。

---

**相关文档**：
- [弹幕系统总览](DANMAKU_SYSTEM.md) — 系统架构、武器类型、框架集成
- [弹幕系统 — 数据结构](DANMAKU_DATA.md) — 所有运行时 struct 和枚举
- [弹幕系统 — 渲染架构](DANMAKU_RENDERING.md) — Mesh 合批、拖尾、爆炸特效
- [弹幕系统 — 碰撞与运行时](DANMAKU_COLLISION.md) — 碰撞系统、延迟变速、DanmakuSystem 入口
