# 弹幕系统 — 数据结构

> **预计阅读**：15 分钟 &nbsp;|&nbsp; **前置**：先读 [弹幕系统总览](DANMAKU_SYSTEM.md) 了解整体架构
>
> 本文档覆盖弹幕系统的所有运行时数据结构：弹丸三层 SoA、激光/喷雾/障碍物数据、碰撞枚举、数据容器和更新器。

---

## 弹丸数据：热/冷分离（SoA 模式）

弹丸数据采用 **Structure-of-Arrays（SoA）** 设计，将数据分为三层独立数组：

1. **BulletCore**（热数据）：运动/碰撞/生命周期——每帧必遍历
2. **BulletTrail**（冷数据）：残影拖尾——仅渲染时和有拖尾的弹丸读取
3. **BulletModifier**（修饰数据）：延迟变速/追踪延迟——仅带 `FLAG_HAS_MODIFIER` 的弹丸读取

> **设计决策**：2048 颗弹丸的 BulletCore（36 bytes/颗 = 72 KB）单独遍历时可完整放进 L2 缓存。如果热冷不分离，每颗 80 bytes → 160 KB，缓存效率大幅下降。运动更新和碰撞检测只遍历 `BulletCore[]`，渲染时才按需合并 `BulletTrail[]`，带延迟变速的弹丸才额外读 `BulletModifier[]`。

### BulletCore — 热数据（运动 + 碰撞 + 生命周期）

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

### Phase 状态转换图

弹丸的生命周期分为四个阶段，状态机是严格单向的（不可回退）：

```
  ┌─────────┐        HitPoints=0         ┌──────────────┐     爆炸帧播完      ┌────────┐     归还槽位     ┌────────┐
  │  Active  │ ──── 或 Lifetime 到期 ───→ │  Exploding   │ ──────────────→ │  Dead  │ ────────────→ │(回池)  │
  │          │                            │              │                  │        │               │  Free  │
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

### BulletTrail — 冷数据（残影拖尾）

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

### BulletModifier — 冷数据（延迟变速 + 追踪延迟）

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

---

## 碰撞相关枚举

### CollisionResponse — 碰撞响应

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

---

## 激光与喷雾数据

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

---

## 伤害飘字数据

### DamageNumberData

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

### DamageNumberSystem — 伤害飘字管理器

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

    public void Initialize(DanmakuRenderConfig renderConfig) { /* ... */ }

    /// <summary>写入一条伤害飘字。环形覆盖，零 GC。</summary>
    public void Spawn(Vector2 position, int damage, byte flags) { /* ... */ }

    /// <summary>每帧更新飘字运动 + 重建 Mesh。</summary>
    public void Update(float dt) { /* ... */ }

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

---

## 数据容器

### BulletWorld — 弹丸数据容器

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

    public BulletWorld(int capacity = DEFAULT_MAX_BULLETS) { /* ... */ }
    public int Allocate() { /* ... */ }
    public void Free(int index) { /* ... */ }
    public void FreeAll() { /* ... */ }
}
```

### LaserPool / SprayPool — 数据容器

激光和喷雾有类似的容器，结构一致但容量小得多：

```csharp
/// <summary>激光数据容器。结构与 BulletWorld 一致（空闲栈 + 容量常量）。</summary>
public class LaserPool
{
    public const int MAX_LASERS = 16;
    public readonly LaserData[] Data = new LaserData[MAX_LASERS];
    public int ActiveCount { get; private set; }
    // ... Allocate / Free / FreeAll（同 BulletWorld 模式）
}

/// <summary>喷雾数据容器。结构同 LaserPool。</summary>
public class SprayPool
{
    public const int MAX_SPRAYS = 8;
    public readonly SprayData[] Data = new SprayData[MAX_SPRAYS];
    public int ActiveCount { get; private set; }
    // ... Allocate / Free / FreeAll（同 LaserPool）
}
```

### ObstacleData 与 ObstaclePool

详见 [弹幕系统 — 碰撞与运行时](DANMAKU_COLLISION.md#障碍物子系统)。

---

## 更新器

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
        // 遍历所有激光槽位
        // Phase 推进：Charging → Firing → Fading → 回收
        // Firing 阶段：WidthOverLifetime 曲线驱动宽度 + TickTimer 推进
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
        // 遍历所有喷雾槽位
        // Elapsed 推进、TickTimer 推进、超时回收
    }
}
```

---

**相关文档**：
- [弹幕系统总览](DANMAKU_SYSTEM.md) — 系统架构、武器类型、框架集成
- [弹幕系统 — SO 配置体系](DANMAKU_CONFIG.md) — 所有 ScriptableObject 定义
- [弹幕系统 — 渲染架构](DANMAKU_RENDERING.md) — Mesh 合批、拖尾、爆炸特效
- [弹幕系统 — 碰撞与运行时](DANMAKU_COLLISION.md) — 碰撞系统、延迟变速、DanmakuSystem 入口
