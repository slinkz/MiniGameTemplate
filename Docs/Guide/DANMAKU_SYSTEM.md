# 弹幕系统架构设计

> **预计阅读**：40 分钟 &nbsp;|&nbsp; **目标**：理解弹幕系统的完整架构、数据结构、渲染方案、碰撞系统、障碍物子系统、弹幕组合系统和扩展机制

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
│  BulletTypeSO / LaserTypeSO / SprayTypeSO / ObstacleTypeSO             │
│  BulletPatternSO / PatternGroupSO / SpawnerProfileSO                 │
│  DanmakuWorldConfig / DanmakuRenderConfig / DanmakuTypeRegistry      │
│  DanmakuTimeScaleSO / DifficultyProfileSO                            │
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
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐     │
│  │ 激光子系统         │  │ 喷雾子系统         │  │ 障碍物子系统    │     │
│  │ LaserPool         │  │ SprayPool          │  │ ObstaclePool   │    │
│  └──────────────────┘  └──────────────────┘  └────────────────┘     │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐     │
│  │ 弹幕组合引擎       │  │ 特效池            │  │ 拖尾池          │     │
│  │ PatternScheduler  │  │ EffectPool        │  │ TrailPool       │    │
│  └──────────────────┘  └──────────────────┘  └────────────────┘     │
│  ┌──────────────────┐                                               │
│  │ 伤害飘字           │                                               │
│  │ DamageNumberSystem│                                               │
│  └──────────────────┘                                               │
└──────────────────────────────────────────────────────────────────────┘
```

### 关键设计约束

| 约束 | 说明 |
|------|------|
| **弹幕 = struct** | 不是 GameObject，是预分配数组中的值类型数据 |
| **零 GC** | 运行时无 new / List 扩容 / 装箱 |
| **7-11 Draw Call** | 弹幕+发光+飘字+拖尾+特效，远低于 50 DC 预算 |
| **自写碰撞** | 圆 vs 圆 / 线段 vs 圆 / 扇形 vs 圆 / 圆 vs AABB（障碍物），不用 Physics2D |
| **SO 驱动** | 弹幕类型、发射模式、碰撞响应、难度曲线全部是 Inspector 可配的 SO 资产 |
| **单一 Update** | 整个模块只有 `DanmakuSystem` 一个 MonoBehaviour |
| **阵营过滤** | Enemy/Player/Neutral 三阵营，碰撞检测最外层 early-out |
| **碰撞响应分离** | 碰到对象/障碍物/屏幕边缘的行为分别配置（Die/ReduceHP/Pierce/Bounce/Reflect） |

### 命名空间约定

弹幕系统所有类型统一使用命名空间 **`MiniGameTemplate.Danmaku`**。与框架其他模块的依赖关系：

| 引用的框架类型 | 命名空间 | 说明 |
|---|---|---|
| `PoolDefinition` | `MiniGameTemplate.Pool` | 对象池配置 SO（特效/重爆炸预制件） |
| `GameEvent` / `IntGameEvent` | `MiniGameTemplate.Events` | SO 事件通道 |
| `AudioClipSO` | `MiniGameTemplate.Audio` | 音效 SO 包装 |

> **注意**：本文档中的代码块为减少视觉噪音**省略了 namespace 声明和 using 语句**。实现时所有 `.cs` 文件必须包裹在 `namespace MiniGameTemplate.Danmaku { ... }` 中，并按需 `using` 框架命名空间。

---

## 三种武器类型

弹幕游戏的武器不止"弹丸"一种。系统支持三种武器类型，各有独立的数据池和碰撞逻辑：

| 类型 | 数据结构 | 池容量 | 碰撞方式 | 伤害模型 | 渲染方式 | 生命值 |
|------|---------|--------|---------|---------|---------|--------|
| **弹丸 Bullet** | `BulletCore[]` + `BulletTrail[]` + `BulletModifier[]` | 2048 | 圆 vs 圆（网格分区加速） | 按碰撞响应配置 | Mesh 合批 | 可配（1-255） |
| **激光 Laser** | `LaserData[]` | 16 | 线段 vs 圆 | 固定间隔 DPS | LaserPool（拉伸四边形） | — |
| **喷雾 Spray** | `SprayData[]` | 8 | 扇形 vs 圆 | 固定间隔 DPS | ParticleSystem 对象池 | — |

激光和喷雾数量少，不需要空间分区，直接遍历即可。

---

## 数据结构

### 弹丸数据：热/冷分离（SoA 模式）

弹丸数据采用 **Structure-of-Arrays（SoA）** 设计，将数据分为三层独立数组：

1. **BulletCore**（热数据）：运动/碰撞/生命周期——每帧必遍历
2. **BulletTrail**（冷数据）：残影拖尾——仅渲染时和有拖尾的弹丸读取
3. **BulletModifier**（修饰数据）：延迟变速/追踪延迟——仅带 `FLAG_HAS_MODIFIER` 的弹丸读取

> **设计决策**：2048 颗弹丸的 BulletCore（36 bytes/颗 = 72 KB）单独遍历时可完整放进 L2 缓存。如果热冷不分离，每颗 80 bytes → 160 KB，缓存效率大幅下降。运动更新和碰撞检测只遍历 `BulletCore[]`，渲染时才按需合并 `BulletTrail[]`，带延迟变速的弹丸才额外读 `BulletModifier[]`。

#### BulletCore — 热数据（运动 + 碰撞 + 生命周期）

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct BulletCore
{
    public Vector2 Position;       // 当前位置                         offset  0, size 8
    public Vector2 Velocity;       // 速度向量                         offset  8, size 8
    public float   Lifetime;       // 最大存活时间（超时即死）            offset 16, size 4
    public float   Elapsed;        // 已过时间（速度曲线采样用）          offset 20, size 4
    public float   Radius;         // 碰撞半径                         offset 24, size 4
    public ushort  TypeIndex;      // BulletTypeSO 索引                offset 28, size 2
    public byte    Phase;          // 生命阶段：Active/Exploding/Dead   offset 30, size 1
    public byte    HitPoints;      // 剩余生命值（0=死亡，1=单次即死）    offset 31, size 1
    public byte    Flags;          // 位标记（8 bits，见下方）           offset 32, size 1
    public byte    Faction;        // 阵营：0=Enemy, 1=Player, 2=Neutral offset 33, size 1
    public byte    LastHitId;      // Pierce 碰撞冷却：上次命中目标 ID    offset 34, size 1
    public byte    _pad;           // 对齐填充                          offset 35, size 1

    // Flags 位定义（byte，8 bits——当前用了 8 个，刚好满）
    public const byte FLAG_ACTIVE           = 1 << 0;
    public const byte FLAG_HOMING           = 1 << 1;
    public const byte FLAG_SPEED_CURVE      = 1 << 2;
    public const byte FLAG_ROTATE_TO_DIR    = 1 << 3;  // 朝飞行方向旋转
    public const byte FLAG_HEAVY_TRAIL      = 1 << 4;  // 使用 TrailPool 重量拖尾
    public const byte FLAG_HAS_CHILD        = 1 << 5;  // 消亡时触发子弹幕
    public const byte FLAG_HAS_MODIFIER     = 1 << 6;  // 有冷数据 BulletModifier
    public const byte FLAG_PIERCE_COOLDOWN  = 1 << 7;  // 正在穿透冷却中
}
// sizeof = 36 bytes
```

> **sizeof 修正说明**：`BulletCore` 实际 36 bytes（不是此前声称的 32）。2048 颗 × 36 = **72 KB**。典型中端手机 L1 数据缓存 32-48 KB，L2 缓存 256-512 KB，因此 BulletCore 数组可完整放入 L2。L1 未命中率相比 32 bytes 方案约上升 ~12%，但仍远优于混合布局（热冷不分离 76 bytes/颗 = 152 KB）。如果后续 profiling 发现瓶颈，可将 `Flags` 压缩或把 `LastHitId` 移入 `BulletModifier`。

> **生命值系统**：`HitPoints` 控制弹丸在碰撞多少次后死亡。默认值 1 = 单次碰撞即死（传统弹幕行为）。设为 255 可实现"不可摧毁"弹丸。每次碰撞扣减量由 `BulletTypeSO` 的碰撞响应配置决定。
>
> **存活时间**：`Lifetime` 是最大存活时间。`Elapsed >= Lifetime` 时弹丸强制死亡，无论剩余生命值。
>
> **阵营系统**：`Faction` 决定碰撞检测时与哪些对象交互。`Enemy` 阵营弹丸只与玩家阵营碰撞，`Player` 阵营弹丸只与敌方阵营碰撞，`Neutral` 与所有对象碰撞。

#### Phase 状态转换图

弹丸的生命周期分为四个阶段，状态机是严格单向的（不可回退）：

```
  ┌─────────┐        HitPoints=0         ┌──────────────┐     爆炸帧播完      ┌────────┐     归还槽位     ┌────────┐
  │  Active  │ ──── 或 Lifetime 到期 ───→ │  Exploding   │ ──────────────→ │  Dead  │ ────────────→ │  Free  │
  │          │                            │              │                  │        │               │(回池)  │
  │ 参与碰撞 │                            │ 不参与碰撞    │                  │ 不渲染  │               │        │
  │ 参与运动 │                            │ 渲染爆炸帧    │                  │        │               │        │
  │ 渲染弹丸 │                            │ 不运动        │                  │        │               │        │
  └─────────┘                            └──────────────┘                  └────────┘               └────────┘
```

| 阶段 | 碰撞 | 运动 | 渲染 | 持续时间 | 退出条件 |
|------|:----:|:----:|:----:|---------|---------|
| **Active** | ✅ | ✅ | 弹丸本体 + 残影 | 直到 HP=0 或 Lifetime | HP 归零 / Elapsed ≥ Lifetime |
| **Exploding** | ❌ | ❌ | 爆炸帧序列 | `ExplosionFrameCount / 60` 秒 | 帧计数器归零 |
| **Dead** | ❌ | ❌ | ❌ | 0（同帧回收） | 立即 → Free |
| **Free** | — | — | — | — | 等待 `BulletWorld.Allocate()` 重新激活 |

```csharp
/// <summary>弹丸生命阶段</summary>
public enum BulletPhase : byte
{
    Active    = 0,   // 正常飞行中
    Exploding = 1,   // 播放爆炸帧动画（碰撞关闭）
    Dead      = 2,   // 等待回收
}
```

> **Exploding 阶段**的弹丸**不参与碰撞检测**——这是刻意的设计。如果爆炸帧还能碰撞，500 颗弹丸同时爆炸会在 3-4 帧内重复触发伤害/响应。
>
> **Dead → Free** 在同一帧完成：`BulletMover.UpdateAll` 检测到 `Phase == Dead` 时立即调用 `BulletWorld.Free(i)` 归还槽位。

#### BulletTrail — 冷数据（残影拖尾）

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct BulletTrail
{
    public Vector2 PrevPos1;       // 上1帧位置（残影拖尾用）
    public Vector2 PrevPos2;       // 上2帧位置
    public Vector2 PrevPos3;       // 上3帧位置
    public byte    TrailLength;    // 残影数量：0=无, 1-3
    public byte    _pad1;
    public ushort  _pad2;
}
// sizeof = 28 bytes
```

两个数组按索引一一对齐：`_cores[i]` 和 `_trails[i]` 描述同一颗弹丸。只有 `TrailLength > 0` 的弹丸在渲染时才读取 trail 数据。

#### BulletModifier — 冷数据（延迟变速 + 追踪延迟）

> **设计决策 P0-1**：延迟变速和追踪延迟参数拆到独立冷数组 `BulletModifier[]`，与 Core/Trail 同索引。只有设置了 `FLAG_HAS_MODIFIER` 的弹丸才读取此数据。大部分弹丸（无延迟变速）不需要 Modifier，运动更新时跳过读取，保持 BulletCore 热数据紧凑。

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct BulletModifier
{
    public float DelayEndTime;     // 延迟变速结束时刻 = DelayBeforeAccel
    public float DelaySpeedScale;  // 延迟期间速度倍率（0=完全静止）
    public float AccelEndTime;     // 加速结束时刻 = DelayBeforeAccel + AccelDuration
    public float HomingStartTime;  // 追踪开始时刻（0=立即追踪）
}
// sizeof = 16 bytes
```

三个数组按索引一一对齐：`_cores[i]`、`_trails[i]`、`_modifiers[i]` 描述同一颗弹丸。

#### 碰撞响应枚举

弹丸碰撞到不同目标类型时的行为分别配置：

```csharp
/// <summary>弹丸碰撞到某类目标时的响应行为</summary>
public enum CollisionResponse : byte
{
    /// <summary>立即死亡（HitPoints 归零）</summary>
    Die            = 0,
    /// <summary>削减生命值（扣减量可配置）</summary>
    ReduceHP       = 1,
    /// <summary>穿透（不消耗生命值，继续飞行）</summary>
    Pierce         = 2,
    /// <summary>原路反弹（速度完全取反）</summary>
    BounceBack     = 3,
    /// <summary>镜像反弹（速度沿碰撞法线镜像）</summary>
    Reflect        = 4,
    /// <summary>超出距离后回收（仅屏幕边缘有效）</summary>
    RecycleOnDistance = 5,
}

/// <summary>弹丸阵营</summary>
public enum BulletFaction : byte
{
    /// <summary>敌方弹丸，与玩家阵营碰撞</summary>
    Enemy   = 0,
    /// <summary>玩家弹丸，与敌方阵营碰撞</summary>
    Player  = 1,
    /// <summary>中立弹丸，与所有对象碰撞</summary>
    Neutral = 2,
}
```

### CircleHitbox — 圆形碰撞体

碰撞系统的基础输入类型，表示一个圆形碰撞区域（玩家/敌人/碰撞目标）：

```csharp
/// <summary>
/// 圆形碰撞体。只读值类型，传参用 in 避免拷贝。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CircleHitbox
{
    public readonly Vector2 Center;
    public readonly float Radius;

    public CircleHitbox(Vector2 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>便捷构造——从 Vector3 取 XY 分量</summary>
    public CircleHitbox(Vector3 position, float radius)
    {
        Center = new Vector2(position.x, position.y);
        Radius = radius;
    }
}
// sizeof = 12 bytes（Vector2=8 + float=4，无填充）
```

> **传参约定**：`CircleHitbox` 是只读值类型，所有碰撞函数签名使用 `in CircleHitbox` 传参（避免 12 bytes 拷贝）。

### CollisionTarget — 碰撞目标类型枚举

`ApplyCollisionResponse` 根据此枚举决定读取 `BulletTypeSO` 的哪组碰撞响应配置：

```csharp
/// <summary>弹丸碰撞的目标类型（决定读取哪组碰撞响应配置）</summary>
public enum CollisionTarget : byte
{
    /// <summary>玩家/敌人等碰撞对象</summary>
    Target     = 0,
    /// <summary>场景障碍物（AABB）</summary>
    Obstacle   = 1,
    /// <summary>屏幕边缘</summary>
    ScreenEdge = 2,
}
```

### LaserData — 激光运行时数据

```csharp
public struct LaserData
{
    public Vector2 Origin;         // 发射点
    public float   Angle;          // 角度（弧度）
    public float   Length;         // 长度
    public float   Width;          // 当前宽度（由曲线驱动）
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

> **设计决策 P1-3**：伤害飘字使用**环形缓冲区**——写指针单调递增 `(writeIndex++) % capacity`，旧条目自动被覆盖，无需显式 Free。容量 128 条（同屏最多 128 个飘字）。

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

> **编码规范**：`DigitCount` 在写入时用除法链预计算（`damage / 10` 循环），严禁运行时 `ToString().Length`。

#### DamageNumberSystem — 伤害飘字管理器

独立的 Mesh 合批渲染器（和 BulletRenderer 分开，使用 NumberAtlas 材质，1 DC）。

```csharp
/// <summary>
/// 伤害飘字系统——环形缓冲区 + 独立 Mesh 合批渲染。
/// 写入时自动覆盖最旧条目，无需显式 Free。
/// </summary>
public class DamageNumberSystem
{
    private const int CAPACITY = 128;

    private readonly DamageNumberData[] _data = new DamageNumberData[CAPACITY];
    private int _writeIndex;   // 单调递增写指针（% CAPACITY 取模）

    // 独立 Mesh（和弹丸 Mesh 分开，使用 NumberAtlas 材质）
    private Mesh _mesh;
    private DanmakuVertex[] _vertices;  // CAPACITY × maxDigits × 4 顶点
    private Material _numberMaterial;

    public void Initialize(DanmakuRenderConfig renderConfig)
    {
        _numberMaterial = new Material(renderConfig.BulletMaterial);
        _numberMaterial.mainTexture = renderConfig.NumberAtlas;
        _mesh = new Mesh();
        _mesh.MarkDynamic();
        _vertices = new DanmakuVertex[CAPACITY * 6 * 4];  // 最多 6 位数 × 4 顶点
        // ... 索引缓冲初始化（同 BulletRenderer 模式）
    }

    /// <summary>写入一条伤害飘字。环形覆盖，零 GC。</summary>
    public void Spawn(Vector2 position, int damage, byte flags)
    {
        ref var d = ref _data[_writeIndex % CAPACITY];
        d.Position = position;
        d.Velocity = new Vector2(0, 1.5f);  // 向上飘动
        d.Damage = damage;
        d.Elapsed = 0;
        d.Lifetime = 0.8f;
        d.Scale = (flags & 0x01) != 0 ? 1.5f : 1f;  // 暴击放大
        d.Color = (flags & 0x01) != 0
            ? new Color32(255, 60, 60, 255)   // 暴击红
            : new Color32(255, 255, 255, 255); // 普通白
        d.DigitCount = CountDigits(damage);
        d.Flags = flags;
        _writeIndex++;
    }

    /// <summary>每帧更新飘字运动 + 重建 Mesh。</summary>
    public void Update(float dt)
    {
        int quadCount = 0;
        for (int i = 0; i < CAPACITY; i++)
        {
            ref var d = ref _data[i];
            if (d.Elapsed >= d.Lifetime) continue;
            d.Elapsed += dt;
            d.Position += d.Velocity * dt;
            d.Velocity.y += 3f * dt;  // 向上加速（抛物线感）

            // 按 digit 组装四边形到 _vertices，UV 从 NumberAtlas 取
            AssembleDigitQuads(ref d, _vertices, ref quadCount);
        }
        // 上传 + DrawMesh
        if (quadCount > 0)
        {
            _mesh.SetVertexBufferData(_vertices, 0, 0, quadCount * 4);
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, quadCount * 6));
            Graphics.DrawMesh(_mesh, Matrix4x4.identity, _numberMaterial, 0);
        }
    }

    private static byte CountDigits(int value)
    {
        if (value < 10) return 1;
        if (value < 100) return 2;
        if (value < 1000) return 3;
        if (value < 10000) return 4;
        if (value < 100000) return 5;
        return 6;
    }
}
```

### BulletWorld — 数据容器

所有弹丸的"世界"就是三个预分配数组 + 空闲槽位栈：

```csharp
public class BulletWorld
{
    public const int DEFAULT_MAX_BULLETS = 2048;

    // 热/冷/修饰 三层分离（SoA）
    public readonly BulletCore[]     Cores;
    public readonly BulletTrail[]    Trails;
    public readonly BulletModifier[] Modifiers;

    /// <summary>精确活跃弹丸数（Allocate +1 / Free -1）</summary>
    public int ActiveCount { get; private set; }

    /// <summary>数组容量（遍历上限）</summary>
    public int Capacity { get; }

    private readonly int[] _freeSlots;
    private int _freeTop;

    public BulletWorld(int capacity = DEFAULT_MAX_BULLETS)
    {
        Capacity  = capacity;
        Cores     = new BulletCore[capacity];
        Trails    = new BulletTrail[capacity];
        Modifiers = new BulletModifier[capacity];
        _freeSlots = new int[capacity];

        for (int i = capacity - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
    }

    public int Allocate()
    {
        if (_freeTop == 0) return -1;  // 池满
        ActiveCount++;
        return _freeSlots[--_freeTop];
    }

    public void Free(int index)
    {
        Cores[index].Flags = 0;  // 清除 FLAG_ACTIVE
        _freeSlots[_freeTop++] = index;
        ActiveCount--;
    }

    public void FreeAll()
    {
        _freeTop = 0;
        for (int i = Capacity - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
        ActiveCount = 0;
    }
}
```

激光和喷雾有类似的 `LaserPool`（16 slots）和 `SprayPool`（8 slots），结构一致但容量小得多：

### LaserPool / SprayPool — 数据容器

```csharp
/// <summary>激光数据容器。结构与 BulletWorld 一致（空闲栈 + 容量常量）。</summary>
public class LaserPool
{
    public const int MAX_LASERS = 16;

    public readonly LaserData[] Data = new LaserData[MAX_LASERS];
    public int ActiveCount { get; private set; }

    private readonly int[] _freeSlots = new int[MAX_LASERS];
    private int _freeTop;

    public LaserPool()
    {
        for (int i = MAX_LASERS - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
    }

    public int Allocate()
    {
        if (_freeTop == 0) return -1;
        ActiveCount++;
        return _freeSlots[--_freeTop];
    }

    public void Free(int index)
    {
        Data[index] = default;
        _freeSlots[_freeTop++] = index;
        ActiveCount--;
    }

    public void FreeAll()
    {
        _freeTop = 0;
        for (int i = MAX_LASERS - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
        ActiveCount = 0;
    }
}

/// <summary>喷雾数据容器。结构同 LaserPool。</summary>
public class SprayPool
{
    public const int MAX_SPRAYS = 8;

    public readonly SprayData[] Data = new SprayData[MAX_SPRAYS];
    public int ActiveCount { get; private set; }

    private readonly int[] _freeSlots = new int[MAX_SPRAYS];
    private int _freeTop;

    public SprayPool()
    {
        for (int i = MAX_SPRAYS - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
    }

    public int Allocate() { /* 同 LaserPool */ return -1; }
    public void Free(int index) { /* 同 LaserPool */ }
    public void FreeAll() { /* 同 LaserPool */ }
}
```

### LaserUpdater — 激光更新器

```csharp
/// <summary>
/// 激光更新——纯 static 工具类，每帧由 DanmakuSystem.Update 调用。
/// 负责：Phase 推进（Charging → Firing → Fading）、宽度曲线驱动、TickTimer 推进。
/// </summary>
public static class LaserUpdater
{
    public static void UpdateAll(LaserPool pool, DanmakuTypeRegistry registry, float dt)
    {
        for (int i = 0; i < LaserPool.MAX_LASERS; i++)
        {
            ref var laser = ref pool.Data[i];
            if (laser.Phase == 0) continue;  // 未激活

            laser.Elapsed += dt;
            var type = registry.LaserTypes[laser.LaserTypeIndex];

            // Phase 推进
            if (laser.Elapsed < type.ChargeDuration)
            {
                // Charging: 细线闪烁，不造成伤害
                laser.Width = type.MaxWidth * 0.05f;  // 蓄力细线
            }
            else if (laser.Elapsed < type.ChargeDuration + type.FiringDuration)
            {
                // Firing: 宽度曲线驱动 + 伤害 tick
                float normalizedTime = laser.Elapsed / type.TotalDuration;
                laser.Width = type.WidthOverLifetime.Evaluate(normalizedTime) * type.MaxWidth;

                // 伤害 tick（TickTimer 始终推进，伤害由调用者的无敌帧控制）
                laser.TickTimer += dt;
                if (laser.TickTimer >= laser.TickInterval)
                {
                    laser.TickTimer -= laser.TickInterval;
                    // 碰撞检测在 CollisionSolver 中统一处理
                }
            }
            else if (laser.Elapsed < type.TotalDuration)
            {
                // Fading: 宽度递减，不造成伤害
                float normalizedTime = laser.Elapsed / type.TotalDuration;
                laser.Width = type.WidthOverLifetime.Evaluate(normalizedTime) * type.MaxWidth;
            }
            else
            {
                // 生命周期结束，回收
                pool.Free(i);
            }
        }
    }
}
```

### SprayUpdater — 喷雾更新器

```csharp
/// <summary>
/// 喷雾更新——纯 static 工具类。
/// 负责：Elapsed 推进、TickTimer 推进、生命周期回收。
/// 喷雾的视觉效果由对象池 ParticleSystem 驱动，Updater 只管逻辑。
/// </summary>
public static class SprayUpdater
{
    public static void UpdateAll(SprayPool pool, float dt)
    {
        for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
        {
            ref var spray = ref pool.Data[i];
            if (spray.Phase == 0) continue;  // 未激活

            spray.Elapsed += dt;

            if (spray.Elapsed >= spray.Lifetime)
            {
                pool.Free(i);
                continue;
            }

            // 伤害 tick（碰撞检测在 CollisionSolver 中统一处理）
            spray.TickTimer += dt;
            if (spray.TickTimer >= spray.TickInterval)
                spray.TickTimer -= spray.TickInterval;
        }
    }
}
```

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
├── ObstacleTypes/            # 障碍物类型
│   ├── Wall.asset            # 不可摧毁墙壁
│   ├── Pillar.asset          # 柱子（可被玩家弹丸摧毁）
│   └── Shield.asset          # 敌方护盾（仅阻挡玩家弹丸）
│
├── Patterns/                 # 弹幕发射模式
│   ├── CircleSpread.asset
│   ├── AimedBurst.asset
│   └── SweepLaser.asset
│
├── PatternGroups/            # 弹幕组合（多层/嵌套/延迟变速）
│   ├── Boss1_Phase1.asset
│   └── DoubleRing.asset
│
├── Config/
│   ├── WorldConfig.asset     # 容量、世界边界、碰撞网格
│   ├── RenderConfig.asset    # 材质、贴图、图集
│   ├── TypeRegistry.asset    # 弹丸/激光/喷雾类型注册表
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

    [Header("伤害")]
    [Tooltip("弹丸命中目标时造成的基础伤害值")]
    [Min(0)]
    public int Damage = 1;

    [Header("生命值")]
    [Tooltip("初始生命值。1=单次碰撞即死（默认），255=几乎不可摧毁")]
    [Range(1, 255)]
    public byte InitialHitPoints = 1;

    [Header("阵营")]
    public BulletFaction Faction = BulletFaction.Enemy;

    [Header("碰撞响应 — 碰到对象（玩家/敌人）")]
    public CollisionResponse OnHitTarget = CollisionResponse.Die;
    [Tooltip("OnHitTarget=ReduceHP 时每次扣减的生命值")]
    [Range(1, 255)]
    public byte HitTargetHPCost = 1;

    [Header("碰撞响应 — 碰到障碍物")]
    public CollisionResponse OnHitObstacle = CollisionResponse.Die;
    [Tooltip("OnHitObstacle=ReduceHP 时每次扣减的生命值")]
    [Range(1, 255)]
    public byte HitObstacleHPCost = 1;

    [Header("碰撞响应 — 碰到屏幕边缘")]
    public CollisionResponse OnHitScreenEdge = CollisionResponse.Die;
    [Tooltip("OnHitScreenEdge=ReduceHP 时每次扣减的生命值")]
    [Range(1, 255)]
    public byte HitScreenEdgeHPCost = 1;
    [Tooltip("OnHitScreenEdge=RecycleOnDistance 时，超出屏幕边缘多远后回收（世界单位）")]
    public float ScreenEdgeRecycleDistance = 1f;

    [Header("碰撞反馈（视觉 + 音频）")]
    [Tooltip("反弹/反射时播放的特效（从 EffectPool 取，null=无特效）")]
    public PoolDefinition BounceEffect;
    [Tooltip("反弹/反射时播放的音效")]
    public AudioClipSO BounceSFX;
    [Tooltip("穿透目标时播放的音效")]
    public AudioClipSO PierceSFX;
    [Tooltip("HP 扣减（但未死亡）时的闪烁色调")]
    public Color DamageFlashTint = new Color(1, 0.3f, 0.3f, 1);
    [Tooltip("HP 扣减时闪烁的帧数（在渲染时叠加色调）")]
    public byte DamageFlashFrames = 3;

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

    [Header("子弹幕（消亡时触发）")]
    [Tooltip("弹丸消亡时触发的子弹幕模式。设 null = 无子弹幕")]
    public BulletPatternSO ChildPattern;

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

    [Header("宽度生命周期曲线")]
    [Tooltip("横轴=归一化时间(0→1, 覆盖 charge+fire+fade 全过程), 纵轴=宽度比例(0→1, 1=MaxWidth)")]
    public AnimationCurve WidthOverLifetime = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

    /// <summary>总时长（charge + fire + fade）</summary>
    public float TotalDuration => ChargeDuration + FiringDuration + FadeDuration;
}
```

> **宽度驱动方式**：`LaserUpdater` 根据 `elapsed / TotalDuration` 采样 `WidthOverLifetime` 曲线，乘以 `MaxWidth` 得到当前宽度。Phase 标记仅控制碰撞开关（Charging 期间不造成伤害）。后续扩展"瞬发激光"只需把 `ChargeDuration = 0` + 调整曲线即可，无需改代码。

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

> **校验机制**：`SprayTypeSO` 的 CustomEditor 在 Scene View 绘制判定扇形 Gizmo（使用 `Handles.DrawSolidArc`），和 ParticleSystem 的视觉范围叠加对比。Inspector 面板显示 `PS.Shape.angle` vs `ConeAngle` 的偏差值，超过 5° 时弹出黄色警告。

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
    [Tooltip("弹丸速度（世界单位/秒）。安全上限约 12，超过可能穿透小碰撞体。详见'速度安全上限'章节")]
    [Range(0.1f, 20f)]
    public float Speed = 5f;
    public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);
    public float Lifetime = 5f;

    [Header("延迟变速")]
    [Tooltip("发射后静止/减速的等待时长（秒），0=无延迟")]
    public float DelayBeforeAccel = 0f;
    [Tooltip("等待期间的速度倍率（0=完全静止, 0.5=半速）")]
    [Range(0f, 1f)]
    public float DelaySpeedScale = 0f;
    [Tooltip("等待结束后的加速持续时间（秒），0=瞬间变速")]
    public float AccelDuration = 0.3f;

    [Header("追踪")]
    public bool IsHoming;
    public float HomingStrength = 2f;     // 追踪转向速度（度/秒）
    [Tooltip("追踪延迟：发射后多久才开始追踪（秒）。配合 DelayBeforeAccel 实现'静止后追踪'")]
    public float HomingDelay = 0f;

    [Header("连射")]
    public int BurstCount = 1;
    public float BurstInterval = 0.05f;

    [Header("音效")]
    public AudioClipSO FireSFX;
}
```

### PatternGroupSO — 弹幕组合（多层 / 延迟 / 嵌套）

这是弹幕系统的组合引擎核心。一个 `PatternGroupSO` 将多个 `BulletPatternSO` 编排在一起，覆盖以下关键玩法需求：

| 需求 | PatternGroupSO 的实现方式 |
|------|--------------------------|
| **多层弹幕**（外圈快内圈慢） | 两个 PatternEntry，不同 Speed，同一发射时机 |
| **延迟变速**（发射后静止→加速） | PatternSO 的 `DelayBeforeAccel` + `AccelDuration` |
| **弹幕嵌套**（母弹爆炸→散射子弹） | BulletTypeSO 的 `ChildPattern` 引用 |
| **时序编排**（先发内圈，0.3s 后发外圈） | PatternEntry 的 `Delay` 字段 |
| **重复组合**（每 0.5s 发一轮，共 5 轮） | `RepeatCount` + `RepeatInterval` |

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Pattern Group")]
public class PatternGroupSO : ScriptableObject
{
    [Header("弹幕编排")]
    public PatternEntry[] Entries;

    [Header("重复")]
    [Tooltip("整组重复次数（1 = 只发一轮）")]
    public int RepeatCount = 1;
    [Tooltip("每轮之间的间隔（秒）")]
    public float RepeatInterval = 0.5f;

    [Header("角度递增")]
    [Tooltip("每轮整组的角度偏移（旋转花弹幕用）")]
    public float AngleIncrementPerRepeat = 0f;
}

[System.Serializable]
public struct PatternEntry
{
    [Tooltip("使用的弹幕模式")]
    public BulletPatternSO Pattern;

    [Tooltip("相对于组开始时间的发射延迟（秒）")]
    public float Delay;

    [Tooltip("覆盖 Pattern 的起始角度（-1 = 不覆盖，使用 Pattern 自己的值）")]
    public float AngleOverride;

    [Tooltip("瞄准玩家：发射时初始角度指向玩家（快照 position）。和 IsHoming（飞行中追踪）是独立需求")]
    public bool AimAtPlayer;
}
```

#### 用法示例

**示例 1：双层环弹（外快内慢）**

```
PatternGroupSO "DoubleRing":
  Entries:
    [0] Pattern = SlowInnerRing (Count=8, Speed=2, SpreadAngle=360)    Delay=0
    [1] Pattern = FastOuterRing (Count=16, Speed=6, SpreadAngle=360)   Delay=0
  RepeatCount = 1
```

**示例 2：静止后追踪弹**

```
BulletPatternSO "DelayedHoming":
  Speed = 8
  DelayBeforeAccel = 0.5    ← 发射后静止 0.5 秒
  DelaySpeedScale = 0       ← 完全静止
  AccelDuration = 0.3       ← 0.3 秒线性加速到 Speed
  IsHoming = true
  HomingDelay = 0.5         ← 和静止等待同步，静止结束同时开始追踪
```

**示例 3：母弹爆炸散射**

```
BulletTypeSO "MotherOrb":
  ChildPattern = "ScatterShot" (Count=8, SpreadAngle=360, Speed=4)
  Explosion = MeshFrame

→ MotherOrb 消亡时，BulletMover 检测 FLAG_HAS_CHILD：
  1. 以弹丸当前位置为 origin
  2. 发射 ChildPattern
  3. 然后进入爆炸帧
```

**示例 4：Boss 三轮花弹幕**

```
PatternGroupSO "Boss1_FlowerAttack":
  Entries:
    [0] Pattern = PetalBurst (Count=5, SpreadAngle=72, Speed=3)  Delay=0  AimAtPlayer=true
  RepeatCount = 3
  RepeatInterval = 0.4
  AngleIncrementPerRepeat = 15   ← 每轮旋转 15°，形成花瓣展开效果
```

#### PatternScheduler — 组合执行引擎

`PatternScheduler` 是一个纯逻辑类（无 MonoBehaviour），由 `DanmakuSystem.Update` 每帧驱动：

> **设计决策 P0-4**：`ScheduleTask` 不直接持有 SO 引用（避免值类型 struct 中嵌入引用类型导致的装箱/拷贝问题），改为 `byte PatternIndex` 索引查 `PatternScheduler._patterns[]` 查找表。SO 引用仅存在于查找表数组中（64 个引用的 GC 标记开销可忽略）。
>
> **设计决策 P1-5**：硬上限 **64 槽**。溢出时静默丢弃新提交的 Task + `GameLog.LogWarning` 警告。
>
> **设计决策 P1-6**：`PatternEntry.AimAtPlayer` 和 `BulletCore.FLAG_HOMING` 是两个独立需求：
> - **AimAtPlayer**（发射时初始朝向）：Schedule 时 **快照** 目标 position 计算初始角度，后续 repeat 如果 Transform 仍存活则更新，否则沿用上次快照
> - **FLAG_HOMING**（飞行中追踪）：每帧实时读 player position 进行转向，需要 null-check

```csharp
public class PatternScheduler
{
    private const int MAX_TASKS = 64;

    // SO 引用查找表——struct 中只存索引，GC 友好
    private readonly BulletPatternSO[] _patterns = new BulletPatternSO[MAX_TASKS];
    private readonly ScheduleTask[] _tasks = new ScheduleTask[MAX_TASKS];
    private int _activeCount;

    /// <summary>提交一个弹幕组合</summary>
    public void Schedule(PatternGroupSO group, Vector2 origin, float baseAngle, Transform aimTarget)
    {
        // 把 group 展开成多个 ScheduleTask（每个 entry × repeatCount）
        // AimAtPlayer=true 时快照 aimTarget.position 作为初始朝向
        // aimTarget Transform 缓存以便后续 repeat 更新
    }

    /// <summary>每帧调用，检查时间到了就发射对应 Pattern</summary>
    public void Update(float dt, DanmakuSystem system)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            ref var task = ref _tasks[i];
            task.Timer -= dt;
            if (task.Timer <= 0f)
            {
                system.FireBullets(_patterns[task.PatternIndex], task.Origin, task.Angle);
                // 标记完成或推进到下一轮
                // AimAtPlayer: 如果缓存的 Transform 仍存活，更新快照角度
            }
        }
        // 压缩已完成任务（swap-remove）
    }
}

private struct ScheduleTask
{
    public byte PatternIndex;       // 索引查 _patterns[]，不持有 SO 引用
    public Vector2 Origin;
    public float Angle;
    public float Timer;             // 倒计时
    public int RemainingRepeats;
    public float RepeatInterval;
    public float AngleIncrement;
    public Vector2 AimSnapshot;     // AimAtPlayer 快照 position
}
```

> **性能**：64 个预分配槽位，`Update` 只是遍历 + 倒计时比较，开销可忽略（< 0.01ms）。溢出时 `GameLog.LogWarning("[Danmaku] PatternScheduler overflow: {count}/64")`。

#### BulletSpawner — 弹丸发射器

`BulletSpawner` 是纯工具类（无状态），负责将 `BulletPatternSO` 的配置翻译为实际的弹丸数据写入 `BulletWorld`。由 `DanmakuSystem.FireBullets` 和 `PatternScheduler` 调用。

```csharp
/// <summary>
/// 弹丸发射器——将 PatternSO 配置翻译为 BulletCore + BulletTrail + BulletModifier 写入。
/// 无状态 static 类，所有方法接收外部依赖作为参数。
/// </summary>
public static class BulletSpawner
{
    /// <summary>
    /// 发射一组弹丸（单次，不含 Burst 连射）。
    /// PatternScheduler 的 Burst 连射通过多次调用本方法实现。
    /// </summary>
    public static void Fire(
        BulletPatternSO pattern,
        Vector2 origin,
        float baseAngleDeg,
        BulletWorld world,
        DanmakuTypeRegistry registry,
        DifficultyProfileSO difficulty = null)
    {
        var type = pattern.BulletType;
        int count = pattern.Count;
        float speed = pattern.Speed;

        // 难度乘数
        if (difficulty != null)
        {
            count = Mathf.RoundToInt(count * difficulty.CountMultiplier);
            speed *= difficulty.SpeedMultiplier;
        }

        float spreadAngle = pattern.SpreadAngle;
        float startAngle = baseAngleDeg + pattern.StartAngle;
        float step = count > 1 ? spreadAngle / count : 0f;
        float halfSpread = spreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            int slot = world.Allocate();
            if (slot == -1) return;  // 池满，静默丢弃

            float angleDeg = startAngle - halfSpread + step * i + step * 0.5f;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * speed;

            // 写入 BulletCore（热数据）
            ref var core = ref world.Cores[slot];
            core.Position = origin;
            core.Velocity = velocity;
            core.Lifetime = pattern.Lifetime * (difficulty?.LifetimeMultiplier ?? 1f);
            core.Elapsed = 0;
            core.Radius = type.CollisionRadius;
            core.TypeIndex = type.RuntimeIndex;
            core.Phase = (byte)BulletPhase.Active;
            core.HitPoints = type.InitialHitPoints;
            core.Flags = BulletCore.FLAG_ACTIVE;
            core.Faction = (byte)type.Faction;
            core.LastHitId = 0;

            // 条件标记
            if (type.RotateToDirection) core.Flags |= BulletCore.FLAG_ROTATE_TO_DIR;
            if (pattern.IsHoming) core.Flags |= BulletCore.FLAG_HOMING;
            if (type.Trail == TrailMode.Trail || type.Trail == TrailMode.Both)
                core.Flags |= BulletCore.FLAG_HEAVY_TRAIL;
            if (type.ChildPattern != null) core.Flags |= BulletCore.FLAG_HAS_CHILD;

            // 写入 BulletTrail（冷数据）
            ref var trail = ref world.Trails[slot];
            trail.TrailLength = (type.Trail == TrailMode.Ghost || type.Trail == TrailMode.Both)
                ? type.GhostCount : (byte)0;
            trail.PrevPos1 = trail.PrevPos2 = trail.PrevPos3 = origin;

            // 写入 BulletModifier（如果有延迟变速/追踪延迟）
            bool hasModifier = pattern.DelayBeforeAccel > 0 || pattern.HomingDelay > 0;
            if (hasModifier)
            {
                core.Flags |= BulletCore.FLAG_HAS_MODIFIER;
                ref var mod = ref world.Modifiers[slot];
                mod.DelayEndTime = pattern.DelayBeforeAccel;
                mod.DelaySpeedScale = pattern.DelaySpeedScale;
                mod.AccelEndTime = pattern.DelayBeforeAccel + pattern.AccelDuration;
                mod.HomingStartTime = pattern.HomingDelay;
            }

            // SpeedOverLifetime 曲线（与延迟变速互斥）
            if (!hasModifier && pattern.SpeedOverLifetime.keys.Length > 1)
                core.Flags |= BulletCore.FLAG_SPEED_CURVE;
        }
    }
}
```

> **Burst 连射**：`PatternScheduler` 负责 Burst 的时序编排——每次 BurstInterval 调用一次 `BulletSpawner.Fire()`，共调 `BurstCount` 次。Spawner 本身无状态，只处理单次发射。

### 配置 SO 拆分

原先的 `DanmakuConfigSO` 承载了容量、渲染、类型注册、难度、玩家受击等过多职责。拆为三个独立 SO，各自独立版本管理，避免策划改难度和美术改材质互相冲突：

#### DanmakuWorldConfig — 容量 + 世界规则

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
    [Tooltip("受击后无敌时长（秒）。0=关闭无敌帧。无敌用真实时间，不受弹幕 TimeScale 影响")]
    public float InvincibleDuration = 0f;
}
```

#### DanmakuRenderConfig — 渲染资产

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Render")]
public class DanmakuRenderConfig : ScriptableObject
{
    [Header("材质")]
    public Material BulletMaterial;          // 弹丸 Alpha Blend
    public Material BulletAdditiveMaterial;  // 发光弹丸 Additive
    public Material LaserMaterial;

    [Header("贴图")]
    public Texture2D BulletAtlas;
    public Texture2D NumberAtlas;            // 数字精灵图集
}
```

#### DanmakuTypeRegistry — 类型注册表

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Type Registry")]
public class DanmakuTypeRegistry : ScriptableObject
{
    public BulletTypeSO[] BulletTypes;
    public LaserTypeSO[] LaserTypes;
    public SprayTypeSO[] SprayTypes;

    /// <summary>Awake 时调用，给每个 TypeSO 分配运行时索引</summary>
    public void AssignRuntimeIndices()
    {
        for (ushort i = 0; i < BulletTypes.Length; i++)
            BulletTypes[i].RuntimeIndex = i;
        for (byte i = 0; i < LaserTypes.Length; i++)
            LaserTypes[i].RuntimeIndex = i;
        for (byte i = 0; i < SprayTypes.Length; i++)
            SprayTypes[i].RuntimeIndex = i;
    }
}
```

> **无敌帧和难度** 分别属于 Combat 和 Difficulty 域，不再放在弹幕系统配置里。无敌帧由调用者（PlayerHealth 或 CombatSystem）管理。

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

> **⚠️ 热路径编码规范**：任何遍历弹丸数组的循环（`BulletMover.UpdateAll`、`CollisionSolver.SolveBullets` 等）**必须在循环外缓存 `float dt = _timeScale.DeltaTime`**，然后以参数形式传入。禁止在热循环内部访问 SO 属性。这是故意为之的性能决策——2048 次属性访问 × 2（SO + `Time.deltaTime`）= 4096 次不必要开销。所有内部更新函数的签名已设计为接收 `float dt` 参数而非 SO 引用。

这让你可以：
- **只减速弹幕**，玩家操作正常（子弹时间效果）
- 不同弹幕组挂不同 TimeScaleSO（Boss 弹幕减速但杂兵弹幕正常）
- 策划在 Inspector 里拖 slider 实时调试

### SpawnerProfileSO — 发射器配置

> **设计决策 P1-7**：SpawnerProfileSO 和 PatternGroupSO 是不同层级——PatternGroupSO 描述"一组弹幕怎么发"，SpawnerProfileSO 描述"一个 Boss/敌人挂哪些弹幕组、怎么切换"。

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spawner Profile")]
public class SpawnerProfileSO : ScriptableObject
{
    [Header("弹幕组列表")]
    [Tooltip("该发射器可使用的弹幕组（按顺序或按条件切换）")]
    public PatternGroupSO[] PatternGroups;

    [Header("发射间隔")]
    [Tooltip("两次弹幕组发射之间的冷却时间（秒）")]
    public float CooldownBetweenGroups = 2f;

    [Header("切换条件")]
    [Tooltip("弹幕组切换模式")]
    public SpawnerSwitchMode SwitchMode = SpawnerSwitchMode.Sequential;
}

public enum SpawnerSwitchMode
{
    /// <summary>按顺序循环</summary>
    Sequential,
    /// <summary>随机选择</summary>
    Random,
    /// <summary>由外部逻辑（如 Boss 阶段状态机）控制</summary>
    External,
}
```

### DifficultyProfileSO — 难度配置

> **设计决策 P1-8**：混合模式——基础乘数 + 少量 Pattern 替换。弹幕系统在发射时查询当前 DifficultyProfileSO，应用乘数并查替换表。

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Difficulty Profile")]
public class DifficultyProfileSO : ScriptableObject
{
    [Header("全局乘数")]
    [Tooltip("弹丸速度乘数")]
    public float SpeedMultiplier = 1f;
    [Tooltip("弹幕数量乘数（四舍五入取整）")]
    public float CountMultiplier = 1f;
    [Tooltip("弹丸存活时间乘数")]
    public float LifetimeMultiplier = 1f;

    [Header("Pattern 替换（可选）")]
    [Tooltip("在特定难度下替换整个 PatternGroupSO")]
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

> **设计决策 P1-10**：弹幕系统**直接复用**框架的 `MiniGameTemplate.Pool.PoolDefinition`（ScriptableObject），不自定义同名类型。理由：弹幕系统的特效池/重爆炸预制件本质就是普通对象池需求，框架的 `PoolManager` API 可直接匹配；BulletTypeSO 本身已是 SO（GC 根），引用其他 SO 不产生额外 GC 开销。
>
> 弹幕系统中所有 `PoolDefinition` 字段（`BulletTypeSO.BounceEffect`、`BulletTypeSO.HeavyExplosionPrefab`、`ObstacleTypeSO.DestroyEffect`、`SprayTypeSO.ParticleEffectPrefab`）的类型均为 `MiniGameTemplate.Pool.PoolDefinition`。

---

## 渲染架构

### Mesh 上传优化

弹丸渲染的核心瓶颈是 CPU 到 GPU 的数据上传。我们采用**交错顶点格式（Interleaved Vertex Layout）+ 单次上传**策略，而非分散的 `SetVertices/SetUVs/SetColors/SetTriangles` 多次拷贝。

#### 顶点格式

```csharp
[StructLayout(LayoutKind.Sequential)]
struct DanmakuVertex
{
    public Vector3 Position;   // 12 bytes
    public Vector2 UV;         // 8 bytes
    public Color32 Color;      // 4 bytes
}
// sizeof = 24 bytes
```

#### 上传策略

```csharp
/// <summary>
/// 弹丸渲染器。管理两个独立 Mesh——Normal（Alpha Blend）和 Additive（发光弹丸）。
/// 每帧遍历 BulletWorld，按 RenderLayer 分拣四边形到对应 Mesh，各自 1 DC。
/// </summary>
public class BulletRenderer
{
    // 双 Mesh 分层——不同混合模式不能合进同一 Mesh
    private Mesh _meshNormal;                   // Alpha Blend 层
    private Mesh _meshAdditive;                 // Additive 层
    private DanmakuVertex[] _verticesNormal;    // Normal 层顶点数组
    private DanmakuVertex[] _verticesAdditive;  // Additive 层顶点数组
    private int[] _triangles;                   // 共享索引模板（固定拓扑）

    private Material _materialNormal;
    private Material _materialAdditive;

    private VertexAttributeDescriptor[] _layout = new[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
    };

    public void Initialize(int maxQuads, DanmakuRenderConfig renderConfig)
    {
        _materialNormal = renderConfig.BulletMaterial;
        _materialAdditive = renderConfig.BulletAdditiveMaterial;

        _meshNormal = CreateDynamicMesh(maxQuads);
        _meshAdditive = CreateDynamicMesh(maxQuads);
        _verticesNormal = new DanmakuVertex[maxQuads * 4];
        _verticesAdditive = new DanmakuVertex[maxQuads * 4];

        // 索引缓冲是固定拓扑（四边形），两个 Mesh 共享同一份模板
        _triangles = new int[maxQuads * 6];
        for (int i = 0, vi = 0; i < maxQuads; i++, vi += 4)
        {
            int ti = i * 6;
            _triangles[ti]     = vi;
            _triangles[ti + 1] = vi + 1;
            _triangles[ti + 2] = vi + 2;
            _triangles[ti + 3] = vi + 2;
            _triangles[ti + 4] = vi + 3;
            _triangles[ti + 5] = vi;
        }
    }

    private Mesh CreateDynamicMesh(int maxQuads)
    {
        var mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.SetVertexBufferParams(maxQuads * 4, _layout);
        mesh.SetIndexBufferParams(maxQuads * 6, IndexFormat.UInt32);
        mesh.SetIndexBufferData(_triangles, 0, 0, maxQuads * 6);
        mesh.subMeshCount = 1;
        return mesh;
    }

    public void Rebuild(BulletWorld world, DanmakuTypeRegistry registry)
    {
        // 遍历活跃弹丸，按 RenderLayer 分拣到对应顶点数组
        int normalCount = 0, additiveCount = 0;
        AssembleQuads(world, registry, _verticesNormal, ref normalCount,
                      _verticesAdditive, ref additiveCount);

        // 各自 1 次上传 + 1 次 DrawMesh
        UploadAndDraw(_meshNormal, _verticesNormal, normalCount, _materialNormal);
        UploadAndDraw(_meshAdditive, _verticesAdditive, additiveCount, _materialAdditive);
    }

    private void UploadAndDraw(Mesh mesh, DanmakuVertex[] verts, int quadCount, Material mat)
    {
        if (quadCount == 0) return;
        mesh.SetVertexBufferData(verts, 0, 0, quadCount * 4);
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, quadCount * 6));
        Graphics.DrawMesh(mesh, Matrix4x4.identity, mat, 0);
    }
}
```

**性能对比**：

| 方案 | 每帧跨层拷贝次数 | 数据量（8192 quads） |
|------|:---:|---:|
| 旧方案：`SetVertices` + `SetUVs` + `SetColors` + `SetTriangles` | 4 次 | ~960 KB |
| 新方案：交错顶点 + `SetVertexBufferData` | **1 次** | ~768 KB |
| 新方案（索引缓冲初始化一次不每帧上传） | — | **节省 192 KB/帧** |

> **WebGL 兼容性说明**：Unity 2022 WebGL 不支持 `NativeArray` 版 `SetVertexBufferData`，但 `T[]` 版本可用。`Mesh.MarkDynamic()` 在 WebGL 上对应 `GL.DYNAMIC_DRAW`，驱动会为频繁更新的 VBO 选择更合适的内存策略。

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
│  EffectPool:   中/重特效           ── 3-5 DC     │
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

原理：每颗弹幕的 `BulletTrail` 存储最近 3 帧的历史位置，合批 Mesh 时额外画 2-3 个缩小 + 降低 alpha 的四边形。

```
弹幕飞行方向 →

  [残影3]   [残影2]   [残影1]   [弹幕本体]
  α=0.15    α=0.3     α=0.6     α=1.0
  scale=0.5  scale=0.7  scale=0.85  scale=1.0
```

2048 颗弹丸 × 4 个四边形 = 8192 四边形 = 32K 顶点，单 Mesh 扛得住，仍然 1 Draw Call。

### Trail 模式（独立曲线拖尾）——特殊弹丸用

连续曲线拖尾（激光蛇形弹道、Boss 大招等），需要沿历史轨迹生成三角带 Mesh。

通过 `TrailPool` 管理：
- 预分配 16-32 条 Trail 实例
- 每条 = 1 个 MeshFilter + MeshRenderer，共享 Trail Material
- 同 Material 自动 Dynamic Batching
- 16 条 Trail ≈ 1-3 Draw Call

策划在 `BulletTypeSO` 里选 `None / Ghost / Trail / Both`，按需分配。

---

## 爆炸特效与子弹幕触发

弹丸消失时的视觉反馈分两档，子弹幕触发也在此时执行：

### 轻量：Mesh 内爆炸帧（零额外开销）

弹丸命中后不立即移除，而是切换到"爆炸阶段"：`Phase = Exploding`。

渲染时按 `ExplosionFrameCount`（通常 3-4 帧）偏移 UV 到爆炸帧序列，播完后才真正回收。

500 颗弹丸同时消失 → 500 个 Mesh 内爆炸帧，零额外 Draw Call，零 GC。

### 重量：对象池特效

Boss 大招爆炸、全屏清弹等重表现需求走 `EffectPool`，通过框架的 `PoolManager` 取预制件播放。同屏控制在 3-5 个以内。

策划通过 `BulletTypeSO.Explosion` 选择：`None / MeshFrame / PooledPrefab`。

### 子弹幕触发（弹幕嵌套）

当弹丸消亡（HitPoints 归零 / 生命周期结束）且设置了 `FLAG_HAS_CHILD` 时，`BulletMover` 在回收前执行：

```csharp
// BulletMover.UpdateAll 伪代码（在回收循环中）
if ((core.Flags & BulletCore.FLAG_HAS_CHILD) != 0)
{
    var childPattern = typeRegistry.BulletTypes[core.TypeIndex].ChildPattern;
    if (childPattern != null)
    {
        // P2-5 决策：子 Pattern 配 AimAtPlayer 时用"母弹→玩家"方向，
        //            否则用母弹飞行方向
        float angle = childPattern.AimAtPlayer
            ? AngleToPlayer(core.Position)
            : Mathf.Atan2(core.Velocity.y, core.Velocity.x) * Mathf.Rad2Deg;
        spawner.Fire(childPattern, core.Position, angle);
    }
}
```

> **深度限制**：子弹幕的 `BulletTypeSO.ChildPattern` **不允许自引用或形成环**。`DanmakuTypeRegistry.AssignRuntimeIndices()` 初始化时做一遍 DFS 检测，环引用直接报错。**P1-1 决策：仅 Awake 时检测**——Play Mode 热改 SO 不重复检测（策划自负），文档警告。运行时零开销。
>
> **容量保护**：子弹幕发射仍然走 `BulletWorld.Allocate()`，池满时静默丢弃（日志警告），不会越界。

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

## 障碍物子系统

碰撞响应表里"碰到障碍物"是核心功能——BounceBack / Reflect / Die 等行为都依赖障碍物的碰撞检测和法线计算。

### 设计定位

障碍物是**弹幕场景的静态/准静态元素**：柱子、墙壁、可破坏屏障等。它们与弹丸交互产生碰撞响应，但自身**不是弹丸**（不需要运动更新、不需要渲染合批）。

| 特性 | 说明 |
|------|------|
| **数量** | 上限 64 个（弹幕游戏的场景障碍物不会太多） |
| **碰撞体** | 仅 AABB（轴对齐矩形），不支持任意多边形（WebGL 性能约束 + 弹幕游戏不需要） |
| **生命值** | 可配：0 = 不可摧毁，1-65535 = 可被弹丸/激光/喷雾摧毁 |
| **阵营** | 对**所有弹丸**生效（无阵营过滤），但可配 `IgnoreFaction` 让特定阵营穿透 |
| **法线** | AABB 四面法线固定（上/下/左/右），取弹丸碰撞点最近面 |

### ObstacleData — 运行时数据

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct ObstacleData
{
    public Vector2 Center;          // AABB 中心
    public Vector2 HalfSize;        // AABB 半尺寸（宽/2, 高/2）
    public ushort  HitPoints;       // 剩余生命值（0=不可摧毁）
    public ushort  MaxHitPoints;    // 初始生命值（用于血条显示比例）
    public byte    Phase;           // Active / Destroyed
    public byte    Flags;           // 位标记
    public byte    TypeIndex;       // ObstacleTypeSO 索引
    public byte    _pad;            // 对齐填充

    // Flags 位定义
    public const byte FLAG_ACTIVE             = 1 << 0;
    public const byte FLAG_IGNORE_PLAYER      = 1 << 1;  // 玩家阵营弹丸穿透
    public const byte FLAG_IGNORE_ENEMY       = 1 << 2;  // 敌方阵营弹丸穿透
}
// sizeof = 20 bytes
```

```csharp
/// <summary>障碍物生命阶段</summary>
public enum ObstaclePhase : byte
{
    Active    = 0,   // 正常存在
    Destroyed = 1,   // 已被摧毁（等待回收/播放摧毁特效）
}
```

### ObstaclePool — 数据容器

```csharp
public class ObstaclePool
{
    public const int MAX_OBSTACLES = 64;

    public readonly ObstacleData[] Data = new ObstacleData[MAX_OBSTACLES];
    public int ActiveCount { get; private set; }

    private readonly int[] _freeSlots = new int[MAX_OBSTACLES];
    private int _freeTop;

    public ObstaclePool()
    {
        for (int i = MAX_OBSTACLES - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
    }

    public int Allocate() { /* 同 BulletWorld 逻辑 */ }
    public void Free(int index) { /* 同 BulletWorld 逻辑 */ }
    public void FreeAll() { /* 同 BulletWorld 逻辑 */ }
}
```

### ObstacleTypeSO — 障碍物类型配置

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Obstacle Type")]
public class ObstacleTypeSO : ScriptableObject
{
    [Header("碰撞体")]
    [Tooltip("AABB 半尺寸（世界单位）")]
    public Vector2 HalfSize = new(0.5f, 0.5f);

    [Header("生命值")]
    [Tooltip("0=不可摧毁，>0=可被弹丸摧毁")]
    public ushort HitPoints = 0;

    [Header("阵营穿透")]
    [Tooltip("勾选：玩家弹丸穿透此障碍物（不碰撞）")]
    public bool IgnorePlayerBullets;
    [Tooltip("勾选：敌方弹丸穿透此障碍物（不碰撞）")]
    public bool IgnoreEnemyBullets;

    [Header("视觉")]
    [Tooltip("场景中的 SpriteRenderer 或预制件（非 Mesh 合批，数量少不影响性能）")]
    public GameObject Prefab;
    [Tooltip("被摧毁时的特效")]
    public PoolDefinition DestroyEffect;
    [Tooltip("被命中时的闪烁颜色")]
    public Color HitFlashColor = Color.white;

    [HideInInspector] public byte RuntimeIndex;
}
```

> 💡 策划在场景编辑器中摆放障碍物预制件，`ObstacleSpawner` MonoBehaviour 在 `OnEnable` 时注册到 `ObstaclePool`，`OnDisable` 时注销。障碍物数量少（≤64），渲染走 SpriteRenderer（不合批），不影响 Draw Call 预算。

### 碰撞检测：弹丸 vs AABB

```csharp
// CollisionSolver.SolveBulletVsObstacle 伪代码
static void SolveBulletVsObstacle(
    BulletCore[] cores, int capacity,
    ObstacleData[] obstacles, int obstacleCount,
    DanmakuTypeRegistry registry,
    ref CollisionResult result)
{
    for (int i = 0; i < capacity; i++)
    {
        ref var c = ref cores[i];
        if ((c.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;
        if (c.Phase != (byte)BulletPhase.Active) continue;

        for (int j = 0; j < obstacleCount; j++)
        {
            ref var obs = ref obstacles[j];
            if ((obs.Flags & ObstacleData.FLAG_ACTIVE) == 0) continue;

            // 阵营穿透检查
            if (c.Faction == (byte)BulletFaction.Player && (obs.Flags & ObstacleData.FLAG_IGNORE_PLAYER) != 0) continue;
            if (c.Faction == (byte)BulletFaction.Enemy && (obs.Flags & ObstacleData.FLAG_IGNORE_ENEMY) != 0) continue;

            // 圆 vs AABB 碰撞检测
            Vector2 closest = ClampToAABB(c.Position, obs.Center, obs.HalfSize);
            float distSq = (c.Position - closest).sqrMagnitude;
            if (distSq >= c.Radius * c.Radius) continue;

            // — 命中 —

            // 计算 AABB 碰撞法线（取最近面）
            Vector2 normal = GetAABBNormal(c.Position, obs.Center, obs.HalfSize);

            // 对障碍物造成伤害（如果可摧毁）
            if (obs.HitPoints > 0)
            {
                var bulletType = registry.BulletTypes[c.TypeIndex];
                obs.HitPoints = (ushort)Mathf.Max(0, obs.HitPoints - bulletType.Damage);
                if (obs.HitPoints == 0)
                    obs.Phase = (byte)ObstaclePhase.Destroyed;
            }

            // 对弹丸执行碰撞响应（障碍物 targetId 用索引 j）
            ApplyCollisionResponse(ref c, registry.BulletTypes[c.TypeIndex],
                                   CollisionTarget.Obstacle, (byte)j, normal);
        }
    }
}
```

### AABB 法线计算

```csharp
/// <summary>
/// 从 AABB 外部一点求最近面法线。
/// 取弹丸中心到 AABB 各面的距离，返回距离最近的面法线。
/// </summary>
static Vector2 GetAABBNormal(Vector2 point, Vector2 center, Vector2 halfSize)
{
    Vector2 d = point - center;
    Vector2 abs = new Vector2(Mathf.Abs(d.x), Mathf.Abs(d.y));

    // 比较到左右面 vs 上下面的穿透深度
    float overlapX = halfSize.x - abs.x;
    float overlapY = halfSize.y - abs.y;

    if (overlapX < overlapY)
        return new Vector2(d.x > 0 ? 1 : -1, 0);  // 左面或右面
    else
        return new Vector2(0, d.y > 0 ? 1 : -1);  // 上面或下面
}
```

### 屏幕边缘法线

屏幕边缘的碰撞法线是固定四个方向：

```csharp
// CollisionSolver.SolveBulletVsScreenEdge 内
Vector2 GetScreenEdgeNormal(Vector2 position, Rect worldBounds)
{
    // 取距离最近的边缘
    float dLeft   = position.x - worldBounds.xMin;
    float dRight  = worldBounds.xMax - position.x;
    float dBottom = position.y - worldBounds.yMin;
    float dTop    = worldBounds.yMax - position.y;

    float minDist = Mathf.Min(dLeft, dRight, dBottom, dTop);

    if (minDist == dLeft)   return Vector2.right;   // (1, 0) — 碰左边，法线向右
    if (minDist == dRight)  return Vector2.left;    // (-1, 0)
    if (minDist == dBottom) return Vector2.up;      // (0, 1)
    return Vector2.down;                             // (0, -1)
}
```

> **性能**：64 个障碍物 × 2048 颗弹丸 = 最坏情况 131,072 次 AABB 检测。但实际上活跃弹丸通常 200-800 颗、障碍物 5-20 个 = 4,000-16,000 次，每次仅 clamp + sqrMagnitude，**预估 < 0.15ms**。如果成为瓶颈，可对障碍物也做网格分区（但 64 个 AABB 不太可能成为瓶颈）。

---

## 碰撞检测

### CollisionSolver — 统一碰撞调度

三种武器类型的碰撞在一个 Solver 里统一处理。碰撞检测**始终执行**，无敌帧仅控制伤害是否生效。

> **阵营过滤**：碰撞检测前先判断阵营。`Enemy` 弹丸只与 `Player` 阵营对象碰撞，`Player` 弹丸只与 `Enemy` 阵营对象碰撞，`Neutral` 与所有对象碰撞。阵营过滤是最外层 early-out，避免不必要的距离计算。

```csharp
public struct CollisionResult
{
    public bool HasPlayerHit;    // 任意伤害源命中玩家
    public int TotalDamage;      // 本帧总伤害（用于飘字）
}

/// <summary>
/// 统一碰撞调度。接受 Pool 引用（不展开数组），内部按需取 .Data/.ActiveCount。
/// </summary>
public CollisionResult SolveAll(
    BulletWorld bulletWorld,
    LaserPool laserPool,
    SprayPool sprayPool,
    ObstaclePool obstaclePool,
    DanmakuTypeRegistry registry,
    in CircleHitbox player, BulletFaction playerFaction,
    float dt)
{
    var result = default(CollisionResult);

    var cores = bulletWorld.Cores;
    int capacity = bulletWorld.Capacity;

    // Phase 1: 弹丸 vs 目标对象（网格分区加速）
    SolveBulletVsTarget(cores, capacity, registry, player, playerFaction, ref result);

    // Phase 2: 弹丸 vs 障碍物（圆 vs AABB，遍历）
    SolveBulletVsObstacle(cores, capacity, obstaclePool.Data, obstaclePool.ActiveCount, registry, ref result);

    // Phase 3: 弹丸 vs 屏幕边缘
    SolveBulletVsScreenEdge(cores, capacity, registry, ref result);

    // Phase 4: 激光 vs 玩家（线段距离）
    SolveLasers(laserPool.Data, laserPool.ActiveCount, player, dt, ref result);

    // Phase 5: 喷雾 vs 玩家（扇形判定）
    SolveSprays(sprayPool.Data, sprayPool.ActiveCount, player, dt, ref result);

    return result;
}
```

> **设计决策**：碰撞检测与伤害判定分离。即使玩家处于无敌状态，弹丸命中仍然触发碰撞响应（消失/反弹/扣减 HP），激光/喷雾的 TickTimer 照常推进但不累积伤害。无敌帧的控制权上移到调用者（DanmakuSystem 或 CombatSystem），不在 CollisionSolver 内部。

### 碰撞响应系统

弹丸碰撞到不同目标类型时，根据 `BulletTypeSO` 的配置执行不同响应：

#### 碰撞响应对照表

| 目标类型 | 可用响应 | 默认 | 说明 |
|---------|---------|------|------|
| **对象**（玩家/敌人） | `Die` / `ReduceHP` / `Pierce` | `Die` | 穿透弹：命中后继续飞行不消耗 HP |
| **障碍物** | `Die` / `ReduceHP` / `Pierce` / `BounceBack` / `Reflect` | `Die` | 反弹弹：速度取反或沿法线镜像 |
| **屏幕边缘** | `Die` / `ReduceHP` / `BounceBack` / `Reflect` / `RecycleOnDistance` | `Die` | 可配超距回收（出屏 N 单位后才回收） |

#### 碰撞响应伪代码

```csharp
// CollisionSolver 内——处理弹丸碰到对象的响应
// targetId：碰撞目标 ID（Pierce 冷却用），屏幕边缘传 0
// normal：碰撞法线（Reflect 用），Die/Pierce 等不需要法线时传 default
void ApplyCollisionResponse(ref BulletCore core, BulletTypeSO type,
    CollisionTarget target, byte targetId, Vector2 normal)
{
    CollisionResponse response;
    byte hpCost;

    switch (target)
    {
        case CollisionTarget.Target:
            response = type.OnHitTarget;
            hpCost = type.HitTargetHPCost;
            break;
        case CollisionTarget.Obstacle:
            response = type.OnHitObstacle;
            hpCost = type.HitObstacleHPCost;
            break;
        case CollisionTarget.ScreenEdge:
            response = type.OnHitScreenEdge;
            hpCost = type.HitScreenEdgeHPCost;
            break;
    }

    switch (response)
    {
        case CollisionResponse.Die:
            core.HitPoints = 0;  // 立即死亡
            break;

        case CollisionResponse.ReduceHP:
            core.HitPoints = (byte)Mathf.Max(0, core.HitPoints - hpCost);
            break;

        case CollisionResponse.Pierce:
            // 穿透：不消耗生命值，继续飞行
            // 记录目标 ID 防止下一帧重复碰撞（per-target 冷却）
            core.LastHitId = targetId;
            core.Flags |= BulletCore.FLAG_PIERCE_COOLDOWN;
            break;

        case CollisionResponse.BounceBack:
            core.Velocity = -core.Velocity;  // 速度完全取反
            core.HitPoints = (byte)Mathf.Max(0, core.HitPoints - hpCost);
            break;

        case CollisionResponse.Reflect:
            // 沿碰撞法线镜像：v' = v - 2(v·n)n
            core.Velocity = Vector2.Reflect(core.Velocity, normal);
            core.HitPoints = (byte)Mathf.Max(0, core.HitPoints - hpCost);
            break;

        case CollisionResponse.RecycleOnDistance:
            // 由 BulletMover 在出界检查时处理
            // 超出 ScreenEdgeRecycleDistance 后才回收
            break;
    }

    // HitPoints 归零 → 进入爆炸阶段
    if (core.HitPoints == 0)
        core.Phase = (byte)BulletPhase.Exploding;
}
```

> **反弹 + 扣减 HP**：`BounceBack` 和 `Reflect` 响应在反弹的同时也扣减 HP。这意味着弹丸每次碰墙都会损耗，HP 耗尽后最终消失。如果需要"无限反弹"，将 `InitialHitPoints` 设为 255。

#### Pierce 碰撞冷却机制

穿透弹（`OnHitTarget = Pierce`）在命中目标后继续飞行，但**下一帧可能仍与同一目标重叠**。如果不做处理，一颗穿透弹穿过半径 0.3 的玩家时会在 3-4 帧内连续触发碰撞，造成 3-4 次伤害——玩家体验："明明只碰了一下，怎么扣了 4 次血？"

**解法**：`BulletCore.LastHitId`（1 byte）记录上次命中的目标 ID。碰撞检测时：

```csharp
// CollisionSolver.SolveBulletVsTarget 内——Pierce 冷却检查
if ((c.Flags & BulletCore.FLAG_PIERCE_COOLDOWN) != 0 && c.LastHitId == targetId)
    continue;  // 跳过——仍在穿透同一目标

// 碰撞检测通过后...
if (type.OnHitTarget == CollisionResponse.Pierce)
{
    core.LastHitId = targetId;
    core.Flags |= BulletCore.FLAG_PIERCE_COOLDOWN;
}
```

```csharp
// BulletMover.UpdateAll 内——每帧清除冷却标记
// 当弹丸不再与任何目标重叠时，BulletMover 在运动更新后清除 FLAG_PIERCE_COOLDOWN
// 这样弹丸穿过目标后可以再次命中其他目标
if ((c.Flags & BulletCore.FLAG_PIERCE_COOLDOWN) != 0)
{
    // 检查是否仍与 LastHitId 目标重叠
    // 如果不再重叠，清除冷却：
    c.Flags &= unchecked((byte)~BulletCore.FLAG_PIERCE_COOLDOWN);
    c.LastHitId = 0;
}
```

> **为什么用 1 byte 而不是 HashSet**：弹幕游戏中一颗穿透弹同时穿越两个目标的概率极低（目标间距 >> 弹丸半径）。1 byte 记录"上一个"目标就够了。如果确实需要同时穿越多个目标（如密集敌阵），可以升级为 `ushort LastHitId1, LastHitId2`（BulletCore 增加 2 bytes）。

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

| 武器类型 | 碰撞行为 | 伤害来源 | 伤害触发 | 伤害间隔 |
|---------|---------|---------|---------|---------|
| 弹丸 | 按 `BulletTypeSO` 碰撞响应配置（Die/ReduceHP/Pierce/Bounce/Reflect） | `BulletTypeSO.Damage` | 碰撞时立即触发（非无敌时） | — |
| 激光 | TickTimer 始终推进 | `LaserTypeSO.DamagePerTick` | 非无敌时触发伤害 | 可配，如 0.1s |
| 喷雾 | TickTimer 始终推进 | `SprayTypeSO.DamagePerTick` | 非无敌时触发伤害 | 可配，如 0.5s |

> **同帧多命中策略**：如果同一帧有多颗弹丸命中玩家，伤害**逐弹累加**（`TotalDamage += bullet.Damage`）。这是弹幕游戏的标准行为——高密度弹幕比低密度弹幕更危险，给策划精确控制弹幕压力的杠杆。
>
> **弹丸 vs 障碍物伤害**：弹丸命中可摧毁障碍物时，对障碍物造成 `BulletTypeSO.Damage` 点伤害（从 `ObstacleData.HitPoints` 扣减）。

### 速度安全上限与隧穿风险

弹丸碰撞是**离散检测**（每帧检查当前位置是否与目标重叠），不做连续碰撞（Swept Circle）。如果弹丸一帧移动距离 > 目标碰撞体直径，弹丸可能直接穿过目标。

```
安全速度上限 = 目标碰撞体直径 / deltaTime
               = (PlayerRadius × 2) / (1/60)
               = 0.2 × 2 × 60 = 24 单位/秒（理论值）

实际安全上限建议：12 单位/秒（留 50% 余量应对掉帧）
```

| 场景 | 安全吗？ | 说明 |
|------|:------:|------|
| Speed=5（常规弹幕） | ✅ | 单帧移动 0.083u，远小于玩家半径 0.2u |
| Speed=12（快速弹） | ✅ | 单帧移动 0.2u，等于玩家半径，临界值 |
| Speed=20（高速追踪弹） | ⚠️ | 单帧移动 0.33u > 玩家半径，30fps 时可能穿透 |

**策划指引**：
- `BulletPatternSO.Speed` 字段已加 `[Range(0.1, 20)]` 限制
- 常规弹幕保持 Speed ≤ 12
- 如果确实需要高速弹（Speed > 12），应配合 `IsHoming = true`（追踪弹不需要精确碰撞，接近时减速锁定）
- 后续可扩展 Swept Circle 检测用于特殊高速弹丸（见扩展预留）

---

## 无敌帧

无敌帧由 `DanmakuSystem` 统一管理，和碰撞检测解耦：

```csharp
// DanmakuSystem.Update() 内
var result = _collision.SolveAll(...);

_invincibleTimer -= Time.deltaTime;  // 无敌用真实时间，不受弹幕 TimeScale 影响
if (result.HasPlayerHit && _invincibleTimer <= 0f)
{
    _onPlayerHit.Raise();
    _onDamageDealt.Raise(result.TotalDamage);
    _invincibleTimer = _worldConfig.InvincibleDuration;
}
```

- 碰撞检测**始终执行**——弹丸命中后该消失就消失，不因无敌而穿透
- 无敌只控制"是否发伤害事件"
- 无敌计时器用真实时间（`Time.deltaTime`），不受弹幕 TimeScale 影响
- `InvincibleDuration` 在 `DanmakuWorldConfig` 中配置，默认 0（关闭）

---

## 时间缩放（子弹时间）

弹幕系统所有涉及时间的计算都用 `DanmakuTimeScaleSO.DeltaTime`，不用 `Time.deltaTime`：

```csharp
// DanmakuSystem.Update() 内——循环外缓存，绝不在热循环内访问 SO
float dt = _timeScale.DeltaTime;

// 弹丸运动（dt 以参数传入）
BulletMover.UpdateAll(_bulletWorld, _worldConfig.WorldBounds, dt);

// 激光 tick
LaserUpdater.UpdateAll(_laserPool, dt);

// 飘字飘动
_damageNumbers.Update(dt);
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
    [SerializeField] private DanmakuWorldConfig _worldConfig;
    [SerializeField] private DanmakuRenderConfig _renderConfig;
    [SerializeField] private DanmakuTypeRegistry _typeRegistry;
    [SerializeField] private DanmakuTimeScaleSO _timeScale;

    [Header("事件通道")]
    [SerializeField] private GameEvent _onPlayerHit;
    [SerializeField] private IntGameEvent _onDamageDealt;

    // 子系统（全部在 Awake 中初始化）
    private BulletWorld _bulletWorld;
    private LaserPool _laserPool;
    private SprayPool _sprayPool;
    private ObstaclePool _obstaclePool;
    private CollisionSolver _collision;
    private BulletRenderer _bulletRenderer;
    private DamageNumberSystem _damageNumbers;
    private TrailPool _trailPool;
    private PatternScheduler _patternScheduler;

    // 玩家碰撞体（P0-5 决策：缓存 Transform + radius，Update 开头同步 position）
    private Transform _playerTransform;
    private float _playerRadius;
    private CircleHitbox _playerHitbox;

    // 无敌帧
    private float _invincibleTimer;

    private void Awake()
    {
        // 分配运行时索引 + DFS 检测子弹幕环引用（P1-1 决策：仅 Awake 检测）
        _typeRegistry.AssignRuntimeIndices();
        _bulletWorld = new BulletWorld(_worldConfig.MaxBullets);
        _bulletRenderer = new BulletRenderer();
        _bulletRenderer.Initialize(_worldConfig.MaxBullets * 4, _renderConfig);  // 含残影
        _patternScheduler = new PatternScheduler();
        // ... 其余子系统初始化
    }

    private void Update()
    {
        // 缓存 dt——热路径编码规范
        float dt = _timeScale.DeltaTime;

        // 0. 同步玩家碰撞体（P0-5 决策）
        if (_playerTransform != null)
            _playerHitbox = new CircleHitbox(_playerTransform.position, _playerRadius);

        // 1. 弹幕组合调度
        _patternScheduler.Update(dt, this);

        // 2. 运动更新（P0-3 决策：空洞遍历 0→Capacity + FLAG_ACTIVE 跳过）
        BulletMover.UpdateAll(_bulletWorld, _typeRegistry, _worldConfig.WorldBounds, dt);
        LaserUpdater.UpdateAll(_laserPool, dt);
        SprayUpdater.UpdateAll(_sprayPool, dt);

        // 3. 碰撞检测（始终执行，不受无敌帧影响）
        var hitResult = _collision.SolveAll(
            _bulletWorld, _laserPool, _sprayPool, _obstaclePool,
            _typeRegistry, _playerHitbox, BulletFaction.Player, dt);

        // 4. 无敌帧伤害控制
        _invincibleTimer -= Time.deltaTime;
        if (hitResult.HasPlayerHit && _invincibleTimer <= 0f)
        {
            _onPlayerHit.Raise();
            _onDamageDealt.Raise(hitResult.TotalDamage);
            _invincibleTimer = _worldConfig.InvincibleDuration;
        }

        // 5. 渲染（合并热/冷数据组装交错顶点，按 RenderLayer 分拣，双 Mesh 上传）
        _bulletRenderer.Rebuild(_bulletWorld, _typeRegistry);
        _damageNumbers.Update(dt);
        _trailPool.Update(dt);
    }

    // —— 公开 API ——
    public void FireBullets(BulletPatternSO pattern, Vector2 origin, float angle) { }
    public void FirePatternGroup(PatternGroupSO group, Vector2 origin, float angle, Transform aimTarget = null)
    {
        _patternScheduler.Schedule(group, origin, angle, aimTarget);
    }
    public int FireLaser(LaserTypeSO type, Vector2 origin, float angle) { }
    public int FireSpray(SprayTypeSO type, Vector2 origin, float direction) { }
    public int AddObstacle(ObstacleTypeSO type, Vector2 center) { }
    public void RemoveObstacle(int index) { _obstaclePool.Free(index); }
    public void ClearAllBullets() { _bulletWorld.FreeAll(); }
    public void ClearAllObstacles() { _obstaclePool.FreeAll(); }
    public void SetPlayer(Transform player, float radius)
    {
        _playerTransform = player;
        _playerRadius = radius;
    }
    public int ActiveBulletCount => _bulletWorld.ActiveCount;
}
```

### 生命周期管理

`DanmakuSystem` 使用 **DontDestroyOnLoad + FreeAll 清场** 策略：

- **Awake**：预分配所有数组、Mesh、对象池。一次性内存分配，后续运行时零 GC
- **场景切换**：调用 `FreeAll()` 重置所有弹丸/激光/喷雾/飘字/调度任务的状态（数组内容清零 + 空闲栈回满），**不释放内存**
- **OnDestroy**：销毁 Mesh、释放 Trail Pool 的 MeshFilter/Renderer 等 Unity 对象

> **为什么不每关重建**：128 KB 弹丸数组 + Mesh + 各种池的重新分配会在场景切换时产生一次性 GC spike。保持内存常驻，换来的是场景切换零卡顿。

---

## 延迟变速系统

延迟变速是 `BulletMover` 的内置能力，由 `BulletPatternSO` 的延迟参数驱动：

> **设计决策 P0-2**：三字段（`DelayBeforeAccel` / `DelaySpeedScale` / `AccelDuration`）优先，运行时 if/else 判断。`SpeedOverLifetime` 曲线仅在三字段全为默认值时生效（两者互斥）。
>
> **设计决策 P0-3**：空洞遍历——`for (i = 0; i < Capacity; i++)` + `FLAG_ACTIVE` 跳过。Allocate/Free O(1)，实现简单。WebGL 单线程下分支预测成本低于 swap-back 的索引重映射开销。
>
> **设计决策 P1-2**：`BulletMover` 顺便写 Trail 历史位置——运动更新时，如果 `TrailLength > 0` 则 shift 历史位置。等于 Mover 既读 Core 又写 Trail（缓存不完美但逻辑简单，避免第二遍遍历）。

```csharp
// BulletMover.UpdateAll 伪代码
static void UpdateAll(BulletWorld world, DanmakuTypeRegistry registry, Rect bounds, float dt)
{
    var cores = world.Cores;
    var trails = world.Trails;
    var modifiers = world.Modifiers;

    for (int i = 0; i < world.Capacity; i++)
    {
        ref var c = ref cores[i];
        if ((c.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;

        c.Elapsed += dt;

        // —— 延迟变速（P0-2：三字段 if/else 优先） ——
        float speedMul = 1f;
        if ((c.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
        {
            ref var mod = ref modifiers[i];
            if (c.Elapsed < mod.DelayEndTime)
            {
                // 延迟期间：使用 DelaySpeedScale
                speedMul = mod.DelaySpeedScale;
            }
            else if (c.Elapsed < mod.AccelEndTime)
            {
                // 加速期间：从 DelaySpeedScale 线性插值到 1.0
                float t = (c.Elapsed - mod.DelayEndTime) / (mod.AccelEndTime - mod.DelayEndTime);
                speedMul = Mathf.Lerp(mod.DelaySpeedScale, 1f, t);
            }
            // else: 加速结束，speedMul = 1.0
        }
        else if ((c.Flags & BulletCore.FLAG_SPEED_CURVE) != 0)
        {
            // 无 Modifier 但有速度曲线：走 SpeedOverLifetime（互斥）
            speedMul = registry.BulletTypes[c.TypeIndex].SpeedOverLifetime.Evaluate(c.Elapsed / c.Lifetime);
        }

        // —— 追踪（P0-1：HomingStartTime 从冷数据读取） ——
        if ((c.Flags & BulletCore.FLAG_HOMING) != 0)
        {
            float homingStart = (c.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0
                ? modifiers[i].HomingStartTime : 0f;
            if (c.Elapsed >= homingStart)
            {
                // 转向玩家...
            }
        }

        c.Position += c.Velocity * speedMul * dt;

        // —— 拖尾写入（P1-2：Mover 顺便写 Trail） ——
        ref var trail = ref trails[i];
        if (trail.TrailLength > 0)
        {
            trail.PrevPos3 = trail.PrevPos2;
            trail.PrevPos2 = trail.PrevPos1;
            trail.PrevPos1 = c.Position;
        }

        // —— 碰撞响应处理（碰到对象/障碍物/屏幕边缘） ——
        // 详见碰撞检测章节

        // —— 出界/生命周期/生命值检查 ——
        if (c.Elapsed >= c.Lifetime || c.HitPoints == 0)
        {
            // 子弹幕触发（P2-5 决策）
            if ((c.Flags & BulletCore.FLAG_HAS_CHILD) != 0)
            {
                var childPattern = registry.BulletTypes[c.TypeIndex].ChildPattern;
                if (childPattern != null)
                {
                    // 基准角：子 Pattern 配 AimAtPlayer 时用"母弹→玩家"方向，
                    //         否则用母弹飞行方向
                    float angle = childPattern.AimAtPlayer
                        ? AngleToPlayer(c.Position)
                        : Mathf.Atan2(c.Velocity.y, c.Velocity.x) * Mathf.Rad2Deg;
                    spawner.Fire(childPattern, c.Position, angle);
                }
            }
            // 进入爆炸阶段或直接回收
        }
    }
}
```

> **策划友好**：`BulletPatternSO` 的 `DelayBeforeAccel` + `DelaySpeedScale` + `AccelDuration` 三个字段足以表达"静止→加速"、"慢速→快速"、"匀速→减速→停止"等变速模式。更复杂的速度曲线直接用 `SpeedOverLifetime` AnimationCurve。

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
│   │   ├── BulletCore.cs
│   │   ├── BulletTrail.cs
│   │   ├── BulletModifier.cs
│   │   ├── BulletPhase.cs             # BulletPhase 枚举
│   │   ├── CollisionResponse.cs       # 碰撞响应枚举 + BulletFaction 枚举 + CollisionTarget 枚举
│   │   ├── CircleHitbox.cs            # 圆形碰撞体 struct（readonly）
│   │   ├── LaserData.cs
│   │   ├── SprayData.cs
│   │   ├── ObstacleData.cs            # 障碍物运行时数据 + ObstaclePhase 枚举
│   │   ├── DamageNumberData.cs
│   │   ├── BulletWorld.cs
│   │   ├── LaserPool.cs
│   │   ├── SprayPool.cs
│   │   └── ObstaclePool.cs            # 障碍物数据容器
│   ├── Config/
│   │   ├── BulletTypeSO.cs
│   │   ├── LaserTypeSO.cs
│   │   ├── SprayTypeSO.cs
│   │   ├── ObstacleTypeSO.cs          # 障碍物类型配置
│   │   ├── BulletPatternSO.cs
│   │   ├── PatternGroupSO.cs
│   │   ├── SpawnerProfileSO.cs
│   │   ├── DifficultyProfileSO.cs
│   │   ├── DanmakuWorldConfig.cs
│   │   ├── DanmakuRenderConfig.cs
│   │   ├── DanmakuTypeRegistry.cs
│   │   └── DanmakuTimeScaleSO.cs
│   ├── Core/
│   │   ├── BulletMover.cs
│   │   ├── BulletSpawner.cs
│   │   ├── PatternScheduler.cs
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
    ├── SprayTypeEditor.cs                 # 喷雾判定范围 Gizmo + 校验
    ├── TypeRegistryValidator.cs           # 子弹幕环引用检测
    └── AtlasPackerWindow.cs               # 图集打包工具（P1）
```

---

## Shader 与 WebGL 兼容性

> **设计决策 P1-4**：目标 **WebGL 2.0（GLES 3.0）**。可用 `discard`、`highp float`，但不用 Compute Shader。

### 三个 Shader 的设计约束

| Shader | 功能 | WebGL 2.0 注意事项 |
|--------|------|-------------------|
| `Danmaku-Unlit-Atlas.shader` | 弹丸主体 + 残影（Alpha Blend + 顶点色） | `discard` 用于 alpha cutoff，highp position/UV |
| `Danmaku-Unlit-Additive.shader` | 发光弹丸（Additive 混合） | 同上，`Blend One One` |
| `Danmaku-Laser.shader` | 激光（UV 滚动 + 边缘发光） | UV 滚动使用 `fract(_Time.y * _ScrollSpeed)` 防精度溢出 |

### WebGL 2.0 兼容规则

- **精度**：vertex shader 全用 `highp`，fragment shader 的 UV 和 position 用 `highp`，其余可 `mediump`
- **`discard`**：允许使用（WebGL 2.0 支持），用于 alpha < 0.01 的像素裁剪
- **不用 MRT**：单 render target 即可
- **不用 Compute Shader**：微信小游戏 WebGL 不支持
- **激光 UV 防溢出**：`fract(x)` 重映射避免 `float` 精度在长时间运行后抖动

---

## 性能预算

基于微信小游戏 WebGL（中端 Android 手机，60fps 目标）：

| 系统 | 操作 | 预算 | 备注 |
|------|------|------|------|
| BulletMover | 2048 颗遍历 + 运动 + 延迟变速 + 子弹幕触发 | ≤ 0.8ms | 仅遍历 BulletCore[]（72KB），L2 缓存友好 |
| PatternScheduler | 组合调度（≤64 任务） | ≤ 0.01ms | 简单倒计时 |
| LaserUpdater + SprayUpdater | 16 + 8 次更新 | ≤ 0.1ms | |
| CollisionSolver | 构建网格 + 弹丸/激光/喷雾碰撞 + 弹丸 vs 障碍物 AABB | ≤ 0.75ms | 障碍物 AABB 碰撞新增 ~0.15ms |
| BulletRenderer | 组装交错顶点 + 单次 Mesh 上传 | ≤ 1.5ms | 比分散上传节省 ~30% |
| GPU 渲染 | 7-11 Draw Call | ≤ 2.5ms | |
| **总计** | | **≤ 5.7ms（60fps 下 34% 帧预算）** | |

> **Profiling 优先级**：如果总帧耗超预算，首先检查 `BulletRenderer.Rebuild` 的 Mesh 上传耗时。如果 BulletMover 超过 0.8ms，首先考虑 SoA 进一步拆分（把 Elapsed/Lifetime 再单独拎出去）。

### GC 预算

| 来源 | 每帧分配 |
|------|---------|
| 弹丸运动/发射/回收/子弹幕触发 | 0 bytes |
| Mesh 更新 | 0 bytes（预分配数组） |
| 碰撞检测 | 0 bytes（预分配网格） |
| 伤害飘字 | 0 bytes（预分配数组） |
| 激光/喷雾 | 0 bytes |
| PatternScheduler | 0 bytes（预分配 64 槽） |

---

## 美术工具

### 弹丸预览器（P0）

- **形态**：BulletTypeSO 的 CustomEditor + Scene View 绘制
- **功能**：选中一个 BulletTypeSO，在 Scene View 实时预览弹丸外观 + 拖尾效果 + 爆炸帧动画
- **价值**：美术必须能看到自己画的弹丸在游戏里的实际效果，否则只能反复打包验证

### 弹幕模式测试器（P1）

- **形态**：EditorWindow
- **功能**：选一个 BulletPatternSO 或 PatternGroupSO，点"播放"在 Scene View 里发射一轮弹幕，可调速/暂停
- **价值**：策划调弹幕模式时的即时反馈

### 喷雾判定校验器（P1）

- **形态**：SprayTypeSO 的 CustomEditor
- **功能**：在 Scene View 绘制判定扇形（`Handles.DrawSolidArc`），和关联的 ParticleSystem 视觉范围叠加。Inspector 面板显示 PS Shape 角度 vs SO 配置角度偏差值，超过 5° 弹出黄色警告
- **价值**：消除"明明没碰到火焰但掉血了"的策划 / QA 困惑

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
| **清弹特效**（Bomb） | 遍历 BulletCore 数组，批量切 `Phase = Dissolving`，播消散帧后回收 | 低 |
| **弹幕录像回放** | 序列化每帧的发射指令（Pattern + origin + angle），回放时重新模拟 | 中 |
| **多玩家碰撞** | CollisionSolver.CheckCollision 循环多个玩家 | 低 |
| **弹丸 vs 弹丸碰撞** | 双层网格分区查询，碰撞时根据配置执行合成/对消/弹反。**注意**：2048² 的全量检测不可行，必须靠空间分区 + 阵营过滤裁剪到实际 O(N) 范围 | 中 |
| **Swept Circle 高速碰撞** | 对 Speed > 12 的弹丸启用射线 vs 圆检测（从上帧位置到当前位置的线段），避免隧穿 | 中 |
| **手机振动反馈** | 在 `OnPlayerHit` 事件监听器中调用 `wx.vibrateShort({ type: 'heavy' })`，通过 IWeChatBridge 抽象层适配 | 低 |
| **新手引导弹幕** | 用 `DifficultyProfileSO.CountMultiplier=0.3` + `TimeScale=0.5` 实现"慢动作教学关卡" | 低 |
| **GPU 粒子弹幕** | 等微信小游戏支持 WebGL 2.0 + Compute Shader | 高 |
| **弹幕脚本 DSL** | PatternGroupSO 可覆盖 90% 弹幕模式，DSL 仅在需要程序化弹幕生成时考虑 | 高 |

---

## 已确认的全部设计决策

| 维度 | 决策 |
|------|------|
| 弹幕数据 | struct 预分配数组，热/冷/修饰 三层 SoA 分离（Core 36B + Trail 28B + Modifier 16B），零 GC |
| 弹丸生命值 | BulletCore.HitPoints 控制碰撞次数，默认 1=单次碰撞即死，255=几乎不可摧毁 |
| 弹丸存活时间 | BulletCore.Lifetime 为最大存活时间，超时强制死亡（无论剩余 HP） |
| 弹丸伤害值 | BulletTypeSO.Damage（int），命中目标/障碍物时的基础伤害。同帧多颗命中逐弹累加 |
| 阵营系统 | BulletCore.Faction（Enemy/Player/Neutral），碰撞检测最外层 early-out |
| 碰撞响应 | 对象/障碍物/屏幕边缘三类目标分别配置响应（Die/ReduceHP/Pierce/BounceBack/Reflect/RecycleOnDistance） |
| 碰撞响应反馈 | BulletTypeSO 配置 BounceEffect/BounceSFX/PierceSFX/DamageFlashTint/DamageFlashFrames |
| Pierce 碰撞冷却 | BulletCore.LastHitId（1 byte）记录上次命中目标，FLAG_PIERCE_COOLDOWN 防多帧重复伤害 |
| Phase 状态机 | Active → Exploding（HP=0/超时）→ Dead → Free，Exploding 不参与碰撞 |
| 障碍物子系统 | ObstacleData（AABB 碰撞体）+ ObstaclePool（64 上限）+ ObstacleTypeSO，支持可摧毁 + 阵营穿透 |
| 障碍物法线 | AABB 四面法线（取最近面），屏幕边缘法线（固定四方向） |
| 速度安全上限 | 安全速度 ≤ 12 单位/秒，超过可能隧穿。BulletPatternSO.Speed 加 [Range(0.1, 20)] |
| 延迟变速数据 (P0-1) | 拆到冷数据 `BulletModifier[]`，FLAG_HAS_MODIFIER 控制是否读取 |
| 延迟变速执行 (P0-2) | 三字段 if/else 优先，SpeedOverLifetime 曲线仅在三字段全为默认值时生效（互斥） |
| 遍历策略 (P0-3) | 空洞遍历 0→Capacity + FLAG_ACTIVE 跳过，Allocate/Free O(1) |
| ScheduleTask (P0-4) | byte PatternIndex 索引查表，不直接持有 SO 引用 |
| Player hitbox (P0-5) | SetPlayer 缓存 Transform+radius，Update 开头同步 position |
| 环引用检测 (P1-1) | 仅 Awake 时 DFS 检测，Play Mode 热改不检测（文档警告） |
| Trail 写入 (P1-2) | BulletMover 顺便写历史位置（TrailLength > 0 时 shift） |
| 伤害飘字池 (P1-3) | 环形缓冲区，写指针单调递增，容量 128 |
| Shader/WebGL (P1-4) | 目标 WebGL 2.0（GLES 3.0），可用 discard/highp，不用 Compute |
| Scheduler 槽位 (P1-5) | 硬上限 64，溢出静默丢弃 + 日志警告 |
| AimAtPlayer (P1-6) | 发射时快照 position（初始朝向）与 FLAG_HOMING（飞行中追踪）是两个独立需求 |
| SpawnerProfileSO (P1-7) | 保留，补定义——Boss/敌人发射器配置，和 PatternGroupSO 不同层级 |
| DifficultyProfileSO (P1-8) | 混合模式——全局乘数 + 少量 Pattern 替换 |
| ActiveCount (P1-9) | Allocate +1 / Free -1 精确计数 |
| PoolDefinition (P1-10) | 复用框架 `MiniGameTemplate.Pool.PoolDefinition`（SO），不自定义同名类型 |
| 子弹幕基准角 (P2-5) | 子 Pattern 配 AimAtPlayer 时用"母弹→玩家"方向，否则用母弹飞行方向 |
| 渲染 | 交错顶点格式 + 单次 Mesh 上传，分层合批 7-11 DC |
| 图集 | 弹幕用自定义图集（规则网格），其他系统用 Sprite Atlas |
| 弹丸旋转 | 支持，BulletTypeSO 配置 |
| 弹丸排序 | 不排序，统一深度 |
| 拖尾 | 双模式：Ghost（Mesh 残影）+ Trail（独立曲线），SO 配置 |
| 爆炸特效 | 轻量走 Mesh 内帧动画，重量走对象池 |
| 伤害飘字 | 高频走数字精灵合批，低频走 FairyGUI |
| 武器类型 | 弹丸(2048) + 激光(16) + 喷雾(8) + 障碍物(64) |
| 碰撞 | 圆vs圆 + 线段vs圆 + 扇形vs圆 + 圆vsAABB（障碍物），弹丸用网格分区 |
| 碰撞与伤害分离 | 碰撞始终执行，无敌帧只控制伤害事件触发 |
| 无敌帧 | 移到战斗域，默认 0，全伤害源共享 |
| 时间缩放 | DanmakuTimeScaleSO，热路径必须循环外缓存 dt |
| 弹幕组合 | PatternGroupSO 多层编排 + BulletTypeSO.ChildPattern 嵌套触发 |
| 延迟变速 | BulletPatternSO 内置 DelayBeforeAccel / AccelDuration |
| 配置拆分 | WorldConfig / RenderConfig / TypeRegistry 三个独立 SO |
| 生命周期 | DontDestroyOnLoad + FreeAll 清场，避免反复分配 |
| 激光宽度 | WidthOverLifetime 曲线驱动，Phase 仅控制碰撞开关 |
| 子弹幕深度控制 | 初始化 DFS 检测环引用（仅 Awake），运行时零开销 |
| 擦弹/清弹 | 预留扩展口，先不做 |
| 美术工具 | 预览器(P0) + 模式测试器(P1) + 喷雾校验器(P1) + 图集打包(P1) |
