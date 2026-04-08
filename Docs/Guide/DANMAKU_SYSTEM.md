# 弹幕系统架构设计

> **预计阅读**：25 分钟 &nbsp;|&nbsp; **目标**：理解弹幕系统的完整架构、数据结构、渲染方案和扩展机制

## 为什么需要专门的弹幕系统？

微信小游戏运行在手机 WebGL 上，有三个致命约束：

| 约束 | 影响 |
|------|------|
| **Draw Call 预算极低**（50-80） | 几百颗弹幕不能每颗一个 Draw Call |
| **单线程**（无 Job System / Burst） | 所有逻辑跑一个线程，不能暴力计算 |
| **Physics2D 极慢** | WebGL 上物理引擎是单线程的，大量 Collider 会卡死 |

如果用传统方式——每颗弹幕一个 GameObject + SpriteRenderer + CircleCollider2D——500 颗弹幕就能把中端手机干到 15fps 以下。

本系统的解法：**弹幕不是 GameObject，是 struct 数组里的一行数据**。

---

## 系统总览

```
┌─────────────────────────────────────────────────────────────────────┐
│                          游戏逻辑层                                  │
│  BossAI / StageController / PlayerController                        │
│  （MonoBehaviour 薄壳，每个 < 150 行）                                │
└──────────┬──────────────────────┬────────────────────────────────────┘
           │ 引用 SO               │ 调用 API
┌──────────┴──────────────────────┴────────────────────────────────────┐
│                   ScriptableObject 资产层                             │
│  BulletTypeSO / LaserTypeSO / SprayTypeSO                            │
│  BulletPatternSO / SpawnerProfileSO                                  │
│  DanmakuConfigSO / DanmakuTimeScaleSO / DifficultyProfileSO         │
└──────────┬───────────────────────────────────────────────────────────┘
           │ 驱动
┌──────────┴───────────────────────────────────────────────────────────┐
│                    DanmakuSystem（唯一 MonoBehaviour）                │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │ 弹丸子系统                                                 │       │
│  │  BulletWorld → BulletMover → CollisionSolver              │       │
│  │  BulletSpawner ↗                  BulletRenderer ↙        │       │
│  └──────────────────────────────────────────────────────────┘       │
│  ┌──────────────────┐  ┌──────────────────┐                         │
│  │ 激光子系统         │  │ 喷雾子系统         │                         │
│  │ LaserPool         │  │ SprayPool          │                        │
│  └──────────────────┘  └──────────────────┘                         │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐     │
│  │ 伤害飘字           │  │ 特效池            │  │ 拖尾池          │     │
│  │ DamageNumberSystem│  │ EffectPool        │  │ TrailPool       │    │
│  └──────────────────┘  └──────────────────┘  └────────────────┘     │
└──────────────────────────────────────────────────────────────────────┘
```

### 关键设计约束

| 约束 | 说明 |
|------|------|
| **弹幕 = struct** | 不是 GameObject，是预分配数组中的值类型数据 |
| **零 GC** | 运行时无 new / List 扩容 / 装箱 |
| **7-11 Draw Call** | 弹幕+发光+飘字+拖尾+特效，远低于 50 DC 预算 |
| **自写碰撞** | 圆 vs 圆 / 线段 vs 圆 / 扇形 vs 圆，不用 Physics2D |
| **SO 驱动** | 弹幕类型、发射模式、难度曲线全部是 Inspector 可配的 SO 资产 |
| **单一 Update** | 整个模块只有 `DanmakuSystem` 一个 MonoBehaviour |

---

## 三种武器类型

弹幕游戏的武器不止"弹丸"一种。系统支持三种武器类型，各有独立的数据池和碰撞逻辑：

| 类型 | 数据结构 | 池容量 | 碰撞方式 | 伤害模型 | 渲染方式 |
|------|---------|--------|---------|---------|---------|
| **弹丸 Bullet** | `BulletData[]` | 2048 | 圆 vs 圆（网格分区加速） | 单次命中 | Mesh 合批 |
| **激光 Laser** | `LaserData[]` | 16 | 线段 vs 圆 | 固定间隔 DPS | LaserPool（拉伸四边形） |
| **喷雾 Spray** | `SprayData[]` | 8 | 扇形 vs 圆 | 固定间隔 DPS | ParticleSystem 对象池 |

激光和喷雾数量少，不需要空间分区，直接遍历即可。

---

## 数据结构

### BulletData — 弹丸运行时数据

每颗弹幕在内存中就是一个 struct：

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct BulletData
{
    public Vector2 Position;       // 当前位置
    public Vector2 Velocity;       // 速度向量
    public Vector2 PrevPos1;       // 上1帧位置（残影拖尾用）
    public Vector2 PrevPos2;       // 上2帧位置
    public Vector2 PrevPos3;       // 上3帧位置
    public float   Lifetime;       // 剩余存活时间
    public float   Elapsed;        // 已过时间（速度曲线采样用）
    public float   Radius;         // 碰撞半径
    public ushort  TypeIndex;      // BulletTypeSO 索引
    public byte    TrailLength;    // 残影数量：0=无, 1-3
    public byte    Phase;          // 生命阶段：Active / Exploding / Dead
    public ushort  Flags;          // 位标记

    // Flags 位定义
    public const ushort FLAG_ACTIVE           = 1 << 0;
    public const ushort FLAG_HOMING           = 1 << 1;
    public const ushort FLAG_SPEED_CURVE      = 1 << 2;
    public const ushort FLAG_ROTATE_TO_DIR    = 1 << 3;  // 朝飞行方向旋转
    public const ushort FLAG_HEAVY_TRAIL      = 1 << 4;  // 使用 TrailPool 重量拖尾
}
```

### LaserData — 激光运行时数据

```csharp
public struct LaserData
{
    public Vector2 Origin;         // 发射点
    public float   Angle;          // 角度（弧度）
    public float   Length;          // 长度
    public float   Width;          // 当前宽度（蓄力时窄→发射时宽）
    public float   MaxWidth;       // 最大宽度
    public float   Elapsed;
    public float   Lifetime;
    public float   TickTimer;      // DPS 计时器
    public float   TickInterval;   // 伤害间隔
    public float   DamagePerTick;  // 每次伤害量
    public byte    Phase;          // Charging / Firing / Fading
    public byte    LaserTypeIndex;
}
```

### SprayData — 喷雾运行时数据

```csharp
public struct SprayData
{
    public Vector2 Origin;         // 喷射源
    public float   Direction;      // 朝向角度（弧度）
    public float   ConeAngle;     // 扇形半角（弧度）
    public float   Range;          // 射程
    public float   Elapsed;
    public float   Lifetime;
    public float   TickTimer;
    public float   TickInterval;
    public float   DamagePerTick;
    public byte    Phase;          // Active / Fading
    public byte    SprayTypeIndex;
}
```

### DamageNumberData — 伤害飘字

```csharp
public struct DamageNumberData
{
    public Vector2 Position;
    public Vector2 Velocity;       // 向上飘动
    public float   Lifetime;
    public float   Elapsed;
    public int     Damage;         // 伤害数值
    public byte    DigitCount;     // 位数（预计算，运行时不 ToString）
    public byte    Flags;          // 暴击/元素/治疗标记
    public float   Scale;          // 暴击放大
    public Color32 Color;
}
```

### BulletWorld — 数据容器

所有弹丸的"世界"就是一个预分配数组 + 空闲槽位栈：

```csharp
public class BulletWorld
{
    public const int MAX_BULLETS = 2048;

    public readonly BulletData[] Bullets = new BulletData[MAX_BULLETS];
    public int ActiveCount { get; private set; }

    private readonly int[] _freeSlots = new int[MAX_BULLETS];
    private int _freeTop;

    public BulletWorld()
    {
        for (int i = MAX_BULLETS - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
    }

    public int Allocate()  { /* 从栈顶弹出空闲索引 */ }
    public void Free(int index) { /* 标记失活，索引压回栈 */ }
    public void FreeAll()  { /* 全部重置 */ }
}
```

激光和喷雾有类似的 `LaserPool`（16 slots）和 `SprayPool`（8 slots），结构一致但容量小得多。

---

## ScriptableObject 配置体系

所有弹幕的"长什么样、怎么飞、多难"都是 SO 资产，策划在 Inspector 里配，不碰代码。

### 资产目录结构

```
Assets/_Game/ScriptableObjects/Danmaku/
├── BulletTypes/              # 弹丸视觉+行为
│   ├── SmallOrb.asset        # 圆弹
│   ├── Needle.asset          # 米粒弹（rotateToDirection=true）
│   └── BigOrb_Glow.asset    # 大玉发光弹（renderLayer=Additive）
│
├── LaserTypes/               # 激光视觉+行为
│   ├── ThinBeam.asset
│   └── FatBeam.asset
│
├── SprayTypes/               # 喷雾视觉+行为
│   ├── Flame.asset
│   └── Poison.asset
│
├── Patterns/                 # 发射模式（所有武器类型通用）
│   ├── CircleSpread.asset
│   ├── AimedBurst.asset
│   └── SweepLaser.asset
│
├── Config/
│   ├── DanmakuConfig.asset   # 全局配置
│   └── TimeScale.asset       # 时间缩放
│
├── Difficulty/
│   ├── Easy.asset
│   ├── Normal.asset
│   └── Hard.asset
│
└── Atlas/
    ├── BulletAtlas.png           # 弹丸图集（规则网格布局）
    ├── BulletAtlasConfig.asset   # UV 映射
    ├── NumberAtlas.png           # 数字精灵图集（0-9）
    └── LaserTexture.png         # 激光纹理（UV 滚动用）
```

### BulletTypeSO — 弹丸视觉类型

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

    [Header("拖尾")]
    public TrailMode Trail = TrailMode.None;
    public byte GhostCount = 3;           // Mesh 残影数量（Ghost 模式）
    public int TrailPointCount = 20;       // 轨迹点数（Trail 模式）
    public float TrailWidth = 0.3f;
    public AnimationCurve TrailWidthCurve;
    public Gradient TrailColor;

    [Header("爆炸")]
    public ExplosionMode Explosion = ExplosionMode.MeshFrame;
    public int ExplosionFrameCount = 4;    // Mesh 内爆炸帧数
    public PoolDefinition HeavyExplosionPrefab;  // 重特效预制件

    [Header("渲染层")]
    public RenderLayer Layer = RenderLayer.Normal;

    [HideInInspector] public ushort RuntimeIndex;
}

public enum TrailMode { None, Ghost, Trail, Both }
public enum ExplosionMode { None, MeshFrame, PooledPrefab }
public enum RenderLayer { Normal, Additive }
```

> 💡 美术通过这个 SO 控制弹丸的全部视觉效果——外观、拖尾、爆炸、混合模式——不需要懂代码。

### LaserTypeSO — 激光类型

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Laser Type")]
public class LaserTypeSO : ScriptableObject
{
    [Header("视觉")]
    public Texture2D LaserTexture;        // 激光纹理（UV 横向滚动）
    public float UVScrollSpeed = 2f;      // UV 滚动速度
    public Color CoreColor = Color.white;
    public Color EdgeColor = Color.cyan;
    public AnimationCurve WidthProfile;   // 沿长度的宽度曲线（中间粗两头细）

    [Header("阶段时长")]
    public float ChargeDuration = 0.5f;   // 蓄力阶段（细线闪烁）
    public float FiringDuration = 2f;     // 发射阶段（全宽光柱）
    public float FadeDuration = 0.3f;     // 消散阶段

    [Header("伤害")]
    public float DamagePerTick = 10f;
    public float TickInterval = 0.1f;     // 0.1s = 每秒 10 次

    [Header("碰撞")]
    public float MaxWidth = 0.8f;

    [HideInInspector] public byte RuntimeIndex;
}
```

### SprayTypeSO — 喷雾类型

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spray Type")]
public class SprayTypeSO : ScriptableObject
{
    [Header("视觉")]
    public PoolDefinition ParticleEffectPrefab;  // 对象池 ParticleSystem 预制件

    [Header("判定")]
    public float ConeAngle = 30f;         // 扇形半角（度）
    public float Range = 5f;

    [Header("伤害")]
    public float DamagePerTick = 5f;
    public float TickInterval = 0.5f;

    [HideInInspector] public byte RuntimeIndex;
}
```

### BulletPatternSO — 弹幕发射模式（策划核心配置）

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Pattern")]
public class BulletPatternSO : ScriptableObject
{
    [Header("弹幕类型")]
    public BulletTypeSO BulletType;

    [Header("发射参数")]
    public int Count = 12;                // 单次弹幕数量
    public float SpreadAngle = 360f;      // 散布角（360=全方位）
    public float StartAngle = 0f;         // 起始角偏移
    public float AnglePerShot = 0f;       // 每次发射的角度递增（旋转弹幕用）

    [Header("运动")]
    public float Speed = 5f;
    public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);
    public float Lifetime = 5f;

    [Header("追踪")]
    public bool IsHoming;
    public float HomingStrength = 2f;     // 追踪转向速度（度/秒）

    [Header("连射")]
    public int BurstCount = 1;
    public float BurstInterval = 0.05f;

    [Header("音效")]
    public AudioClipSO FireSFX;
}
```

### DanmakuConfigSO — 全局配置

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config")]
public class DanmakuConfigSO : ScriptableObject
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

    [Header("弹幕类型注册表")]
    public BulletTypeSO[] BulletTypes;
    public LaserTypeSO[] LaserTypes;
    public SprayTypeSO[] SprayTypes;

    [Header("渲染")]
    public Material BulletMaterial;          // 弹丸 Alpha Blend
    public Material BulletAdditiveMaterial;  // 发光弹丸 Additive
    public Material LaserMaterial;
    public Texture2D BulletAtlas;
    public Texture2D NumberAtlas;            // 数字精灵图集

    [Header("难度")]
    public DifficultyProfileSO DefaultDifficulty;

    [Header("玩家受击")]
    [Tooltip("受击后无敌时间（秒），0 = 无无敌帧")]
    public float InvincibleDuration = 0f;
}
```

### DanmakuTimeScaleSO — 时间缩放

弹幕系统有自己的时间源，和 `Time.timeScale` 独立：

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Time Scale")]
public class DanmakuTimeScaleSO : ScriptableObject
{
    [Range(0f, 2f)]
    public float TimeScale = 1f;

    /// <summary>弹幕系统的 deltaTime，所有时间计算用这个。</summary>
    public float DeltaTime => Time.deltaTime * TimeScale;

    public void SetSlowMotion(float scale) => TimeScale = scale;
    public void ResetSpeed() => TimeScale = 1f;
}
```

这让你可以：
- **只减速弹幕**，玩家操作正常（子弹时间效果）
- 不同弹幕组挂不同 TimeScaleSO（Boss 弹幕减速但杂兵弹幕正常）
- 策划在 Inspector 里拖 slider 实时调试

---

## 渲染架构

### 分层 Mesh 合批

不是所有弹幕都能合进一个 Mesh——不同混合模式、不同类型的视觉元素需要分层：

```
┌─────────────────────────────────────────────────┐
│                   每帧渲染                        │
│                                                  │
│  Mesh Layer 0: 弹丸主体 + 残影     ── 1 DC      │  ← Alpha Blend Material
│               （爆炸帧也在这里）                    │
│  Mesh Layer 1: 发光弹丸 + 残影     ── 1 DC      │  ← Additive Material
│  Mesh Layer 2: 伤害飘字（数字精灵）── 1 DC       │  ← Number Atlas
│  LaserPool:    激光                ── 1-2 DC     │  ← 拉伸四边形 + UV 滚动
│  TrailPool:    重量级拖尾          ── 1-3 DC     │  ← Dynamic Batching
│  EffectPool:   中/重特效           ── 3-5 DC     │  ← 对象池预制件
│  SprayVFX:     喷雾 ParticleSystem ── 1-3 DC     │
│  FairyGUI:     低频文本/UI         ── 已合批      │
│                                                  │
│  总计: 7-11 Draw Call                            │
└─────────────────────────────────────────────────┘
```

### 弹丸旋转

米粒弹、箭矢弹等非圆形弹丸需要朝飞行方向旋转。通过 `BulletTypeSO.RotateToDirection` 标记：

- **圆弹**（`RotateToDirection = false`）：直接用轴对齐四边形，4 次加法
- **米粒弹**（`RotateToDirection = true`）：算 `Atan2` + `Sin/Cos`，旋转四个顶点

2048 颗弹幕全旋转额外开销约 0.3-0.5ms。圆弹走快速路径跳过旋转，自动优化。

### 弹丸不排序

所有弹丸同一深度，渲染顺序随意。Additive 弹幕天然不需要排序，Alpha Blend 弹幕密集重叠时略有视觉差异但弹幕游戏不在意。

### 图集方案

弹幕系统使用**自定义二进制图集**（规则网格布局），不用 Unity Sprite Atlas。

**为什么不用 Sprite Atlas？**

| | Sprite Atlas | 自定义图集 |
|---|-------------|-----------|
| 和自定义 Mesh 配合 | 需查 `Sprite.uv`，有开销 | 整数除法算 UV，极快 |
| WebGL 兼容性 | Late Binding 坑 | 直接加载 Texture2D |
| YooAsset 依赖 | 需对齐 Bundle 策略 | 独立，无依赖问题 |
| 序列帧 UV 计算 | 布局随机，需查表 | 规则网格，`row × col` |
| 美术工作流 | 拖入即可 | 需跑打包工具 |

弹幕系统的图集布局规则性极强（N 种弹丸 × M 帧），天然适合固定网格。其他系统（UI、场景装饰）继续用 Sprite Atlas。

图集打包工具是一个 Editor 菜单项：美术把散图丢进指定目录，一键打成规则网格图集 + 生成 `BulletAtlasConfig` SO（记录每种弹丸的 UV 区域）。

---

## 拖尾系统

支持两种拖尾，通过 `BulletTypeSO.Trail` 配置：

### Ghost 模式（Mesh 内残影）——大部分弹丸用

原理：每颗弹幕存储最近 3 帧的历史位置，合批 Mesh 时额外画 2-3 个缩小 + 降低 alpha 的四边形。

```
弹幕飞行方向 →

  [残影3]   [残影2]   [残影1]   [弹幕本体]
  α=0.15    α=0.3     α=0.6     α=1.0
  scale=0.5  scale=0.7  scale=0.85  scale=1.0
```

2048 颗弹丸 × 4 个四边形 = 8192 四边形 = 32K 顶点，单 Mesh 扛得住，仍然 1 Draw Call。

### Trail 模式（独立曲线拖尾）——特殊弹丸用

连续曲线拖尾（激光蛇形弹道、Boss 大招等），需要沿历史轨迹生成三角带 Mesh。

通过 `TrailPool`管理：
- 预分配 16-32 条 Trail 实例
- 每条 = 1 个 MeshFilter + MeshRenderer，共享 Trail Material
- 同 Material 自动 Dynamic Batching
- 16 条 Trail ≈ 1-3 Draw Call

策划在 `BulletTypeSO` 里选 `None / Ghost / Trail / Both`，按需分配。

---

## 爆炸特效

弹丸消失时的视觉反馈分两档：

### 轻量：Mesh 内爆炸帧（零额外开销）

弹丸命中后不立即移除，而是切换到"爆炸阶段"：`Phase = Exploding`。

渲染时按 `ExplosionFrameCount`（通常 3-4 帧）偏移 UV 到爆炸帧序列，播完后才真正回收。

500 颗弹丸同时消失 → 500 个 Mesh 内爆炸帧，零额外 Draw Call，零 GC。

### 重量：对象池特效

Boss 大招爆炸、全屏清弹等重表现需求走 `EffectPool`，通过框架的 `PoolManager` 取预制件播放。同屏控制在 3-5 个以内。

策划通过 `BulletTypeSO.Explosion` 选择：`None / MeshFrame / PooledPrefab`。

---

## 伤害飘字

### 高频飘字：数字精灵 Mesh 合批

弹幕命中产生的伤害数字走合批 Mesh，和弹丸共享渲染管线思路：

- 预制好 0-9 的数字贴图放在 `NumberAtlas`
- `DamageNumberData[]` 预分配数组
- 每帧和弹丸一起合批渲染
- 按 digit 索引 UV：`uv.x = (digitIndex % cols) * cellWidth`
- 通过 `Flags` 区分颜色/缩放（暴击=红色放大，普通=白色，治疗=绿色）

同屏 100 个飘字 ≈ 0 额外开销，1 Draw Call。

### 低频飘字：FairyGUI 文本对象池

Boss 名字、技能名、系统提示等走 FairyGUI 文本，富文本/描边/动画支持完整。同屏不超过 5-10 个。

---

## 碰撞检测

### CollisionSolver — 统一碰撞调度

三种武器类型的碰撞在一个 Solver 里统一处理：

```csharp
public void SolveAll(
    BulletData[] bullets, int bulletCount,
    LaserData[] lasers, int laserCount,
    SprayData[] sprays, int sprayCount,
    in CircleHitbox player,
    float dt)
{
    // Phase 1: 弹丸 vs 玩家（网格分区加速）
    SolveBullets(bullets, bulletCount, player);

    // Phase 2: 激光 vs 玩家（线段距离）
    SolveLasers(lasers, laserCount, player, dt);

    // Phase 3: 喷雾 vs 玩家（扇形判定）
    SolveSprays(sprays, sprayCount, player, dt);
}
```

### 弹丸碰撞：均匀网格空间分区

屏幕分成 12×20 格子，每帧把弹丸按位置塞进格子。碰撞检测时只查玩家所在格 + 8 邻格：

```
碰撞 = (dx*dx + dy*dy) < (r1+r2)*(r1+r2)
```

2048 颗弹丸 → 平均只需检测 ~50 颗 → < 0.5ms。

### 激光碰撞：线段 vs 圆

```
点到线段距离 = |叉积| / 线段长度
距离 < laserHalfWidth + playerRadius → 命中
```

同屏 16 条激光，直接遍历即可。

### 喷雾碰撞：扇形 vs 圆

```
1. 距离检测：dist(origin, player) < range ?
2. 角度检测：angleBetween(direction, toPlayer) < coneAngle ?
两个都满足 → 在喷雾范围内
```

同屏 8 个喷雾，直接遍历。

### 伤害模型

| 武器类型 | 伤害触发方式 | 伤害间隔 |
|---------|------------|---------|
| 弹丸 | 碰撞即触发，弹丸消失 | — |
| 激光 | 固定间隔 tick | 可配，如 0.1s |
| 喷雾 | 固定间隔 tick | 可配，如 0.5s |

激光和喷雾的伤害不按帧算，按固定 `TickInterval` 计时器触发。这保证了 30fps 和 60fps 下 DPS 一致。

### 无敌帧

预留，默认关闭：

```
DanmakuConfigSO.InvincibleDuration = 0f  // 默认无无敌帧
```

所有伤害源（弹丸/激光/喷雾）共享同一个无敌计时器。被弹丸命中后，无敌期间激光也不造成伤害。

---

## 时间缩放（子弹时间）

弹幕系统所有涉及时间的计算都用 `DanmakuTimeScaleSO.DeltaTime`，不用 `Time.deltaTime`：

```csharp
float dt = _timeScale.DeltaTime;  // = Time.deltaTime * timeScale

// 弹丸运动
bullet.Position += bullet.Velocity * dt;
bullet.Elapsed += dt;

// 激光 tick
laser.TickTimer += dt;

// 飘字飘动
number.Position += number.Velocity * dt;
```

效果：
- `TimeScale = 0.3` → 弹幕慢放，玩家正常操作（经典子弹时间）
- `TimeScale = 0` → 弹幕冻结
- 不同 TimeScaleSO → 分层减速（Boss 弹幕减速但杂兵弹幕正常）

通过 GameEvent 触发：`OnSlowMotion.Raise()` → `DanmakuTimeScaleSO.SetSlowMotion(0.3f)` + TimerService 延时恢复。

---

## DanmakuSystem — 唯一的 MonoBehaviour

整个弹幕模块只有一个 MonoBehaviour 作为入口：

```csharp
public class DanmakuSystem : MonoBehaviour
{
    [SerializeField] private DanmakuConfigSO _config;
    [SerializeField] private DanmakuTimeScaleSO _timeScale;

    [Header("事件通道")]
    [SerializeField] private GameEvent _onPlayerHit;
    [SerializeField] private IntGameEvent _onDamageDealt;

    // 子系统（全部在 Awake 中初始化）
    private BulletWorld _bulletWorld;
    private LaserPool _laserPool;
    private SprayPool _sprayPool;
    private CollisionSolver _collision;
    private BulletRenderer _bulletRenderer;
    private DamageNumberSystem _damageNumbers;
    private TrailPool _trailPool;

    // 无敌帧
    private float _invincibleTimer;

    private void Update()
    {
        float dt = _timeScale.DeltaTime;

        // 1. 运动更新
        BulletMover.UpdateAll(_bulletWorld, _config.WorldBounds, dt);
        LaserUpdater.UpdateAll(_laserPool, dt);
        SprayUpdater.UpdateAll(_sprayPool, dt);

        // 2. 碰撞检测
        _invincibleTimer -= Time.deltaTime;  // 无敌用真实时间
        if (_invincibleTimer <= 0f)
        {
            _collision.SolveAll(
                _bulletWorld, _laserPool, _sprayPool,
                playerHitbox, dt);
        }

        // 3. 渲染
        _bulletRenderer.Rebuild(_bulletWorld);
        _damageNumbers.Update(dt);
        _trailPool.Update(dt);
    }

    // —— 公开 API ——
    public void FireBullets(BulletPatternSO pattern, Vector2 origin, float angle) { }
    public int FireLaser(LaserTypeSO type, Vector2 origin, float angle) { }
    public int FireSpray(SprayTypeSO type, Vector2 origin, float direction) { }
    public void ClearAllBullets() { }
    public void SetPlayer(Transform player, float radius) { }
    public int ActiveBulletCount => _bulletWorld.ActiveCount;
}
```

---

## 与框架集成

### 事件通道

| SO 事件 | 发布者 | 监听者 |
|---------|--------|--------|
| `OnPlayerHit` | DanmakuSystem | PlayerHealth（扣血）、VFXController（闪白）、AudioManager（音效） |
| `OnDamageDealt : IntGameEvent` | DanmakuSystem | DamageNumberSystem（生成飘字） |
| `OnBossPhaseChanged` | BossAI | DanmakuSystem.ClearAllBullets() + SpawnerController（切模式） |

### 框架模块使用

| 模块 | 使用方式 |
|------|---------|
| **EventSystem** | 跨系统通信（命中、Boss 阶段） |
| **AudioSystem** | `AudioManager.PlaySFX(pattern.FireSFX)` |
| **TimerService** | Boss 弹幕间隔调度 |
| **FSM** | Boss 阶段状态机 |
| **ObjectPool** | 重特效、TrailPool 的底层（弹丸本身不用对象池） |
| **FloatVariable** | 玩家血量等共享变量 |

---

## 推荐目录结构

```
Assets/_Framework/DanmakuSystem/
├── MODULE_README.md
├── Scripts/
│   ├── Data/
│   │   ├── BulletData.cs
│   │   ├── LaserData.cs
│   │   ├── SprayData.cs
│   │   ├── DamageNumberData.cs
│   │   ├── BulletWorld.cs
│   │   ├── LaserPool.cs
│   │   └── SprayPool.cs
│   ├── Config/
│   │   ├── BulletTypeSO.cs
│   │   ├── LaserTypeSO.cs
│   │   ├── SprayTypeSO.cs
│   │   ├── BulletPatternSO.cs
│   │   ├── SpawnerProfileSO.cs
│   │   ├── DanmakuConfigSO.cs
│   │   ├── DanmakuTimeScaleSO.cs
│   │   └── DifficultyProfileSO.cs
│   ├── Core/
│   │   ├── BulletMover.cs
│   │   ├── BulletSpawner.cs
│   │   ├── LaserUpdater.cs
│   │   ├── SprayUpdater.cs
│   │   ├── CollisionSolver.cs
│   │   ├── BulletRenderer.cs
│   │   ├── DamageNumberSystem.cs
│   │   └── TrailPool.cs
│   └── DanmakuSystem.cs
├── Shaders/
│   ├── Danmaku-Unlit-Atlas.shader         # 弹丸（Alpha Blend + 顶点色）
│   ├── Danmaku-Unlit-Additive.shader      # 发光弹丸
│   └── Danmaku-Laser.shader               # 激光（UV 滚动 + 边缘发光）
└── Editor/
    ├── BulletPreviewEditor.cs             # 弹丸预览器（P0 美术工具）
    ├── PatternTesterWindow.cs             # 弹幕模式测试器（P1）
    └── AtlasPackerWindow.cs               # 图集打包工具（P1）
```

---

## 性能预算

基于微信小游戏 WebGL（中端 Android 手机，60fps 目标）：

| 系统 | 操作 | 预算 |
|------|------|------|
| BulletMover | 2048 颗遍历 + 运动 + 回收 | ≤ 1.0ms |
| LaserUpdater + SprayUpdater | 16 + 8 次更新 | ≤ 0.1ms |
| CollisionSolver | 构建网格 + 弹丸/激光/喷雾碰撞 | ≤ 0.6ms |
| BulletRenderer | 重建 2048×4 quad + 残影 + 飘字 Mesh | ≤ 1.8ms |
| GPU 渲染 | 7-11 Draw Call | ≤ 2.5ms |
| **总计** | | **≤ 6.0ms（60fps 下 36% 帧预算）** |

### GC 预算

| 来源 | 每帧分配 |
|------|---------|
| 弹丸运动/发射/回收 | 0 bytes |
| Mesh 更新 | 0 bytes（预分配数组） |
| 碰撞检测 | 0 bytes（预分配网格） |
| 伤害飘字 | 0 bytes（预分配数组） |
| 激光/喷雾 | 0 bytes |

---

## 美术工具

### 弹丸预览器（P0）

- **形态**：BulletTypeSO 的 CustomEditor + Scene View 绘制
- **功能**：选中一个 BulletTypeSO，在 Scene View 实时预览弹丸外观 + 拖尾效果 + 爆炸帧动画
- **价值**：美术必须能看到自己画的弹丸在游戏里的实际效果，否则只能反复打包验证

### 弹幕模式测试器（P1）

- **形态**：EditorWindow
- **功能**：选一个 BulletPatternSO，点"播放"在 Scene View 里发射一轮弹幕，可调速/暂停
- **价值**：策划调弹幕模式时的即时反馈

### 图集打包工具（P1）

- **形态**：EditorWindow / 菜单项
- **功能**：美术把弹丸散图放进指定目录，一键打成规则网格图集 + 生成 UV 映射 SO
- **价值**：消除手动排图集的痛苦

---

## 扩展预留

以下功能当前不实现，但数据结构和接口已预留扩展口：

| 功能 | 扩展方式 | 难度 |
|------|---------|------|
| **擦弹判定**（Grazing） | BulletTypeSO 加 `GrazeRadius`，碰撞时加一个 `distance > hitRadius && distance < grazeRadius` 分支 | 低 |
| **清弹特效**（Bomb） | 遍历 BulletData 数组，批量切 `Phase = Dissolving`，播消散帧后回收 | 低 |
| **弹幕录像回放** | 序列化每帧的发射指令（Pattern + origin + angle），回放时重新模拟 | 中 |
| **多玩家碰撞** | CollisionSolver.CheckCollision 循环多个玩家 | 低 |
| **GPU 粒子弹幕** | 等微信小游戏支持 WebGL 2.0 + Compute Shader | 高 |
| **弹幕脚本 DSL** | 当前 SO 组合可覆盖 80% 弹幕模式，DSL 增加学习成本 | 高 |

---

## 已确认的全部设计决策

| 维度 | 决策 |
|------|------|
| 弹幕数据 | struct 预分配数组，零 GC |
| 渲染 | 分层 Mesh 合批，7-11 DC |
| 图集 | 弹幕用自定义图集（规则网格），其他系统用 Sprite Atlas |
| 弹丸旋转 | 支持，BulletTypeSO 配置 |
| 弹丸排序 | 不排序，统一深度 |
| 拖尾 | 双模式：Ghost（Mesh 残影）+ Trail（独立曲线），SO 配置 |
| 爆炸特效 | 轻量走 Mesh 内帧动画，重量走对象池 |
| 伤害飘字 | 高频走数字精灵合批，低频走 FairyGUI |
| 武器类型 | 弹丸(2048) + 激光(16) + 喷雾(8) |
| 碰撞 | 圆vs圆 + 线段vs圆 + 扇形vs圆，弹丸用网格分区 |
| 伤害模型 | 弹丸=单次命中，激光/喷雾=固定间隔 tick |
| 无敌帧 | 预留，默认 0，全伤害源共享 |
| 时间缩放 | DanmakuTimeScaleSO，独立时间源 |
| 擦弹/清弹 | 预留扩展口，先不做 |
| 美术工具 | 预览器(P0) + 模式测试器(P1) + 图集打包(P1) |
