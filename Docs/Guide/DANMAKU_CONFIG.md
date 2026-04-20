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
│   └── SineWave.asset        # 正弦波弹丸（MotionType=SineWave）
├── LaserTypes/               # 激光视觉+行为
├── SprayTypes/               # 喷雾视觉+行为
├── ObstacleTypes/            # 障碍物类型
├── Patterns/                 # 弹幕发射模式
├── PatternGroups/            # 弹幕组合
├── Config/
│   ├── WorldConfig.asset     # 容量、世界边界、碰撞网格
│   ├── RenderConfig.asset    # 材质、贴图、图集、RuntimeAtlasConfig
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
    // ── 统一资源描述 ──
    [Header("资源描述（统一）")]
    public Texture2D SourceTexture;              // 源贴图（每个弹丸类型可引用独立贴图）
    [FormerlySerializedAs("AtlasUV")]
    public Rect UVRect = new Rect(0, 0, 1, 1);  // 静态弹丸的 UV 区域
    public BulletSamplingMode SamplingMode;      // Static / SpriteSheet

    [Header("序列帧配置（SpriteSheet 时有效）")]
    public int SheetColumns = 1;
    public int SheetRows = 1;
    public int SheetTotalFrames = 1;
    public BulletPlaybackMode PlaybackMode;      // StretchToLifetime / FixedFps
    public float FixedFps = 12f;

    [Header("Atlas 绑定（可选优化）")]
    public AtlasMappingSO AtlasBinding;          // null = 独立模式，非 null = Atlas 派生
    public int SchemaVersion = 1;                // 迁移版本号

    // ── 视觉 ──
    [Header("视觉")]
    public Color Tint = Color.white;
    public Vector2 Size = new(0.2f, 0.2f);
    public bool RotateToDirection;               // 朝飞行方向旋转

    [Header("碰撞")]
    public float CollisionRadius = 0.1f;

    // ── 运动策略 ──
    [Header("运动")]
    public MotionType MotionType;                // Default / SineWave / Spiral

    [Header("正弦波参数（SineWave）")]
    public float SineAmplitude = 1.0f;
    public float SineFrequency = 3.0f;

    [Header("螺旋参数（Spiral）")]
    public float SpiralAngularVelocity = 180f;

    public AnimationCurve SpeedOverLifetime;     // 速度曲线

    [Header("视觉动画")]
    public bool UseVisualAnimation;
    public AnimationCurve ScaleOverLifetime;
    public AnimationCurve AlphaOverLifetime;
    public Gradient ColorOverLifetime;

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
    [Range(1, 15)] public byte GhostInterval = 5; // 残影采样间隔（帧数）
    public int TrailPointCount = 20;
    public float TrailWidth = 0.3f;
    public AnimationCurve TrailWidthCurve;
    public Gradient TrailColor;

    [Header("爆炸")]
    public ExplosionMode Explosion = ExplosionMode.MeshFrame;
    public int ExplosionFrameCount = 4;
    public Rect ExplosionAtlasUV;                // 爆炸帧第一帧 UV
    public PoolDefinition HeavyExplosionPrefab;

    [Header("子弹幕")]
    public BulletPatternSO ChildPattern;

    [HideInInspector] public ushort RuntimeIndex;

    // ── 辅助方法 ──
    public int MaxFrameCount => ...;             // 序列帧总有效帧数
    public Texture2D GetResolvedTexture();       // AtlasBinding > SourceTexture
    public Rect GetResolvedBaseUV();             // Atlas 映射 > UVRect
    public Rect GetFrameUV(int frameIndex, Rect baseUV); // 序列帧子区域计算
}

public enum TrailMode { None, Ghost, Trail, Both }
public enum ExplosionMode { None, MeshFrame, PooledPrefab }
```

> **注意**：ADR-029 v2 已移除 Additive Blend，不再有 `RenderLayer.Additive`。所有弹丸统一走 `RenderLayer.Normal` + Alpha Blend。
>
> **MotionType 参数可见性**：`BulletTypeSOEditor` (Custom Editor) 根据 MotionType 条件显示 SineWave/Spiral 参数，根据 TrailMode 条件显示 Ghost/Trail 参数。

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
    public AnimationCurve WidthProfile;          // 沿长度的宽度曲线（中间粗两头细）

    [Header("宽度生命周期曲线")]
    public AnimationCurve WidthOverLifetime = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("阶段时长")]
    public float ChargeDuration = 0.5f;          // 蓄力（细线闪烁，不造成伤害）
    public float FiringDuration = 2f;            // 发射（全宽光柱，造成伤害）
    public float FadeDuration = 0.3f;            // 消散

    [Header("伤害")]
    public float DamagePerTick = 10f;
    public float TickInterval = 0.1f;

    [Header("碰撞")]
    public BulletFaction Faction = BulletFaction.Enemy;
    public float MaxWidth = 0.8f;

    [Header("碰撞响应 — 障碍物")]
    public LaserObstacleResponse OnHitObstacle = LaserObstacleResponse.Ignore;

    [Header("碰撞响应 — 屏幕边缘")]
    public LaserScreenEdgeResponse OnHitScreenEdge = LaserScreenEdgeResponse.Clip;
    public float ScreenEdgeRecycleMargin = 1f;   // Origin 越界回收的边缘余量

    [Header("折射")]
    [Range(0, 8)]
    public byte MaxReflections = 0;              // 最大折射次数（0=直线不折射）

    [HideInInspector] public byte RuntimeIndex;

    public float TotalDuration => ChargeDuration + FiringDuration + FadeDuration;
}
```

> **字段语义补充**：
> - `Faction`：决定激光与哪些目标碰撞（同弹丸阵营过滤逻辑）
> - `OnHitObstacle`：`Ignore`=穿透障碍物、`Reflect`=折射（消耗 MaxReflections）
> - `OnHitScreenEdge`：`Clip`=裁剪到屏幕边缘、`Reflect`=边缘折射、`Recycle`=Origin 出界时回收
> - `WidthOverLifetime`：LaserUpdater 按 `elapsed/TotalDuration` 采样，乘以 `MaxWidth` 得到当前宽度
> - `WidthProfile`：沿激光长度方向的局部粗细分布
> - Phase 仅控制碰撞开关


### SprayTypeSO

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spray Type")]
public class SprayTypeSO : ScriptableObject
{
    [Header("视觉")]
    public PoolDefinition ParticleEffectPrefab;
    public VFXTypeSO SprayVFXType;

    [Header("判定")]
    public float ConeAngle = 30f;
    public float Range = 5f;

    [Header("伤害")]
    public float DamagePerTick = 5f;
    public float TickInterval = 0.5f;

    [HideInInspector] public byte RuntimeIndex;
}
```

> **AttachMode 语义**：`SprayTypeSO` 只决定喷雾判定范围与伤害；喷雾 VFX 是否持续跟随目标，由 `SprayVFXType.AttachMode` 决定。`World` = 仅在生成瞬间取一次 `spray.Origin` 播放 world-space VFX，后续不跟随；`FollowTarget` = 使用 `AttachSourceId` 持续跟随喷雾源位置移动。
> **运行时前置规则**：如果 `SprayVFXType` 已赋值，但未注册到当前 SpriteSheetVFXSystem 使用的 `VFXTypeRegistrySO`，运行时 `SpriteSheetVFXSystem.CanPlay()` 会返回 false，喷雾特效不会播放，并输出 **Error** 日志：`[SpriteSheetVFXSystem] Type not found in registry: ...`。
> **修复步骤**：把对应 `VFXTypeSO` 加入当前 SpriteSheetVFXSystem 引用的 `VFXTypeRegistrySO._types` 列表，或改回已注册的 VFXType。


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
    public int MaxTrails = 64;

    [Header("世界边界")]
    public Rect WorldBounds = new(-6, -10, 12, 20);

    [Header("碰撞事件缓冲")]
    public int CollisionEventBufferCapacity = 256;  // 旁路表现通道，溢出不影响主逻辑

    [Header("无敌帧")]
    public float InvincibleDuration = 0f;  // 真实时间，不受弹幕 TimeScale 影响
}
```

### DanmakuRenderConfig — 渲染资产

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Render")]
public class DanmakuRenderConfig : ScriptableObject
{
    [Header("材质")]
    public Material BulletMaterial;       // Alpha Blend（ADR-029 v2：Additive 已移除）
    public Material LaserMaterial;

    [Header("贴图")]
    public Texture2D BulletAtlas;         // 弹丸图集（规则网格布局）
    public Texture2D NumberAtlas;         // 数字精灵图集（0-9 飘字用）

    [Header("Runtime Atlas")]
    public RuntimeAtlasConfig RuntimeAtlasConfig;  // 为空时保持旧渲染路径
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
