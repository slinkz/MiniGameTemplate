# 弹幕系统 — 碰撞与运行时

> **预计阅读**：25 分钟 &nbsp;|&nbsp; **前置**：先读 [弹幕系统总览](DANMAKU_SYSTEM.md) 了解整体架构
>
> 本文档覆盖：障碍物子系统、碰撞检测（7 阶段）、碰撞响应系统、Pierce 冷却、网格分区、伤害模型、速度安全上限、无敌帧、时间缩放、延迟变速、DanmakuSystem 入口与生命周期。

---

## 障碍物子系统

障碍物是弹幕场景的静态/准静态元素（柱子、墙壁、可破坏屏障等），与弹丸碰撞产生响应。

| 特性 | 说明 |
|------|------|
| 数量上限 | 64 个 |
| 碰撞体 | 仅 AABB（轴对齐矩形） |
| 生命值 | 0=不可摧毁，1-65535=可摧毁 |
| 阵营 | 可配 `IgnoreFaction` 让特定阵营穿透 |
| 法线 | AABB 四面法线固定，取弹丸碰撞点最近面 |

### ObstacleData — 运行时数据

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct ObstacleData
{
    public Vector2 Center;
    public Vector2 HalfSize;
    public ushort  HitPoints;
    public ushort  MaxHitPoints;
    public byte    Phase;          // Active / Destroyed
    public byte    Flags;
    public byte    TypeIndex;
    public byte    _pad;

    public const byte FLAG_ACTIVE        = 1 << 0;
    public const byte FLAG_IGNORE_PLAYER = 1 << 1;
    public const byte FLAG_IGNORE_ENEMY  = 1 << 2;
}
// sizeof = 20 bytes

public enum ObstaclePhase : byte { Active = 0, Destroyed = 1 }
```

### ObstaclePool — 数据容器

```csharp
public class ObstaclePool
{
    public const int MAX_OBSTACLES = 64;
    public readonly ObstacleData[] Data = new ObstacleData[MAX_OBSTACLES];
    public int ActiveCount { get; private set; }
    // ... Allocate / Free / FreeAll（同 BulletWorld 模式）
}
```

### ObstacleTypeSO — 类型配置

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Obstacle Type")]
public class ObstacleTypeSO : ScriptableObject
{
    public Vector2 HalfSize = new(0.5f, 0.5f);
    public ushort HitPoints = 0;
    public bool IgnorePlayerBullets;
    public bool IgnoreEnemyBullets;
    public GameObject Prefab;
    public PoolDefinition DestroyEffect;
    public Color HitFlashColor = Color.white;
    [HideInInspector] public byte RuntimeIndex;
}
```

> 障碍物数量少（≤64），渲染走 SpriteRenderer（不合批），不影响 DC 预算。策划在场景编辑器中摆放预制件，`ObstacleSpawner` 在 `OnEnable` 时注册到 `ObstaclePool`。

---

## 碰撞检测

### CollisionSolver — 统一碰撞调度

三种武器类型的碰撞在一个 Solver 里统一处理。碰撞检测**始终执行**，无敌帧仅控制伤害是否生效。

> **阵营过滤**是最外层 early-out：`Enemy` 弹丸只与 `Player` 对象碰撞，`Player` 弹丸只与 `Enemy` 对象碰撞，`Neutral` 与所有对象碰撞。

```csharp
public CollisionResult SolveAll(
    BulletWorld bulletWorld, LaserPool laserPool, SprayPool sprayPool,
    ObstaclePool obstaclePool, DanmakuTypeRegistry registry,
    in CircleHitbox player, BulletFaction playerFaction, float dt)
{
    // Phase 1: 弹丸 vs 目标对象（网格分区加速）
    // Phase 2: 弹丸 vs 障碍物（圆 vs AABB，遍历）
    // Phase 3: 弹丸 vs 屏幕边缘
    // Phase 4: 激光 vs 玩家（线段距离）
    // Phase 5: 喷雾 vs 玩家（扇形判定）
    return result;
}
```

### 弹丸 vs AABB（障碍物碰撞）

```csharp
// 圆 vs AABB 碰撞
Vector2 closest = ClampToAABB(c.Position, obs.Center, obs.HalfSize);
float distSq = (c.Position - closest).sqrMagnitude;
if (distSq < c.Radius * c.Radius) { /* 命中 */ }
```

### AABB 法线计算

```csharp
static Vector2 GetAABBNormal(Vector2 point, Vector2 center, Vector2 halfSize)
{
    Vector2 d = point - center;
    float overlapX = halfSize.x - Mathf.Abs(d.x);
    float overlapY = halfSize.y - Mathf.Abs(d.y);
    return overlapX < overlapY
        ? new Vector2(d.x > 0 ? 1 : -1, 0)
        : new Vector2(0, d.y > 0 ? 1 : -1);
}
```

### 屏幕边缘法线

固定四方向：碰左边→`(1,0)`，碰右边→`(-1,0)`，碰下边→`(0,1)`，碰上边→`(0,-1)`。

---

## 碰撞响应系统

弹丸碰撞到不同目标类型时，根据 `BulletTypeSO` 配置执行不同响应：

| 目标类型 | 可用响应 | 默认 |
|---------|---------|------|
| **对象** | Die / ReduceHP / Pierce | Die |
| **障碍物** | Die / ReduceHP / Pierce / BounceBack / Reflect | Die |
| **屏幕边缘** | Die / ReduceHP / BounceBack / Reflect / RecycleOnDistance | Die |

### 响应执行

```csharp
void ApplyCollisionResponse(ref BulletCore core, BulletTypeSO type,
    CollisionTarget target, byte targetId, Vector2 normal)
{
    // 1. 按 target 类型选择 response 和 hpCost
    // 2. 执行响应：
    //    Die          → HitPoints = 0
    //    ReduceHP     → HitPoints -= hpCost
    //    Pierce       → 记录 LastHitId，设 FLAG_PIERCE_COOLDOWN
    //    BounceBack   → Velocity 取反 + 扣 HP
    //    Reflect      → Vector2.Reflect(Velocity, normal) + 扣 HP
    //    RecycleOnDistance → 由 BulletMover 出界检查处理
    // 3. HitPoints == 0 → Phase = Exploding
}
```

> **反弹 + 扣减 HP**：BounceBack/Reflect 在反弹同时也扣 HP。需要"无限反弹"则设 `InitialHitPoints = 255`。

### Pierce 碰撞冷却

穿透弹命中后继续飞行，`PierceHitMask`（ushort，16 bits）位掩码记录已命中的目标槽位，防止多帧重复碰撞：

```csharp
// 碰撞检测时——按目标在 TargetRegistry 中的槽位号检查对应 bit
ushort targetBit = (ushort)(1 << targetSlotIndex);
if ((c.Flags & FLAG_PIERCE_COOLDOWN) != 0 && (c.PierceHitMask & targetBit) != 0) continue;

// 命中时设置对应 bit
c.PierceHitMask |= targetBit;
c.Flags |= FLAG_PIERCE_COOLDOWN;

// BulletMover 每帧：不再重叠时清除冷却
c.Flags &= unchecked((byte)~FLAG_PIERCE_COOLDOWN);
c.PierceHitMask = 0;
```

> **为什么升级为 ushort 位掩码**：弹丸可能同帧穿越多个目标（TargetRegistry 支持多目标注册），单字节 `LastHitId` 无法同时跟踪多个已命中目标。16 bits 支持最多 16 个碰撞目标同时追踪。

---

## 碰撞检测算法

### 弹丸碰撞：均匀网格空间分区

屏幕分 12×20 格子，每帧按位置入格。碰撞时只查玩家所在格 + 8 邻格。2048 颗 → 平均检测 ~50 颗 → < 0.5ms。

### 激光碰撞：线段 vs 圆

```
距离 = |叉积| / 线段长度
距离 < laserHalfWidth + playerRadius → 命中
```

### 喷雾碰撞：扇形 vs 圆

```
1. dist(origin, player) < range ?
2. angleBetween(direction, toPlayer) < coneAngle ?
两者都满足 → 在喷雾范围内
```

---

## 伤害模型

| 武器类型 | 伤害来源 | 触发方式 |
|---------|---------|---------|
| 弹丸 | `BulletTypeSO.Damage` | 碰撞时立即（非无敌时） |
| 激光 | `LaserTypeSO.DamagePerTick` | TickTimer 间隔（非无敌时） |
| 喷雾 | `SprayTypeSO.DamagePerTick` | TickTimer 间隔（非无敌时） |

> 同帧多颗弹丸命中，伤害**逐弹累加**。弹丸命中可摧毁障碍物时，对障碍物造成 `Damage` 点伤害。

### 速度安全上限

```
安全速度 ≤ 12 单位/秒（留 50% 余量应对掉帧）
```

| Speed | 安全？ | 说明 |
|-------|:----:|------|
| 5 | ✅ | 单帧移动 0.083u，远小于玩家半径 |
| 12 | ✅ | 临界值 |
| 20 | ⚠️ | 30fps 时可能穿透 |

`BulletPatternSO.Speed` 已加 `[Range(0.1, 20)]`。超速弹配合 `IsHoming = true` 可缓解。

---

## 无敌帧

```csharp
// DanmakuSystem.Update()
var result = _collision.SolveAll(...);
_invincibleTimer -= Time.deltaTime;  // 真实时间，不受弹幕 TimeScale 影响
if (result.HasPlayerHit && _invincibleTimer <= 0f)
{
    _onPlayerHit.Raise();
    _onDamageDealt.Raise(result.TotalDamage);
    _invincibleTimer = _worldConfig.InvincibleDuration;
}
```

- 碰撞**始终执行**——弹丸该消失就消失
- 无敌只控制"是否发伤害事件"
- `InvincibleDuration` 默认 0（关闭）

---

## 时间缩放（子弹时间）

所有时间计算用 `DanmakuTimeScaleSO.DeltaTime`：

```csharp
// RunUpdatePipeline（Update 阶段——逻辑 + 碰撞）
float dt = _timeScale.DeltaTime;
BulletMover.UpdateAll(_bulletWorld, ..., dt);
LaserUpdater.UpdateAll(_laserPool, dt);
_damageNumbers.Rebuild(dt);   // 注：Rebuild 在 LateUpdate 管线中执行

// RunLateUpdatePipeline（LateUpdate 阶段——渲染提交）
// 见 DANMAKU_RENDERING.md 管线详解
```

---

## 延迟变速系统

由 `BulletMover` 内置，`BulletPatternSO` 的延迟参数驱动：

> **P0-2**：三字段 if/else 优先，`SpeedOverLifetime` 曲线仅在三字段全为默认值时生效（互斥）。
>
> **P0-3**：空洞遍历 `0→Capacity` + `FLAG_ACTIVE` 跳过。
>
> **P1-2**：`BulletMover` 顺便写 Trail 历史位置。

```csharp
// BulletMover.UpdateAll 伪代码
for (int i = 0; i < world.Capacity; i++)
{
    if ((c.Flags & FLAG_ACTIVE) == 0) continue;
    c.Elapsed += dt;

    // 延迟变速（FLAG_HAS_MODIFIER）
    float speedMul = 1f;
    if (hasModifier)
    {
        if (c.Elapsed < mod.DelayEndTime) speedMul = mod.DelaySpeedScale;
        else if (c.Elapsed < mod.AccelEndTime) speedMul = Lerp(mod.DelaySpeedScale, 1f, t);
    }
    else if (hasSpeedCurve)
    {
        speedMul = type.SpeedOverLifetime.Evaluate(c.Elapsed / c.Lifetime);
    }

    // 追踪（HomingStartTime 从冷数据）
    // 运动
    c.Position += c.Velocity * speedMul * dt;
    // Trail 写入
    // 生命周期检查 + 子弹幕触发
}
```

---

## DanmakuSystem — 唯一的 MonoBehaviour

DanmakuSystem 采用 `partial class` 拆分为 4 个文件，职责清晰：

| 文件 | 职责 |
|------|------|
| `DanmakuSystem.cs` | Facade：Awake / Update / LateUpdate / 单例 |
| `DanmakuSystem.Runtime.cs` | 持有所有子系统引用、InitializeSubsystems / DisposeSubsystems |
| `DanmakuSystem.API.cs` | Fire / Register / Clear 等公开 API + RuntimeAtlas 统计 |
| `DanmakuSystem.UpdatePipeline.cs` | RunUpdatePipeline / RunLateUpdatePipeline 管线驱动 |

```csharp
public partial class DanmakuSystem : MonoBehaviour
{
    // ── 配置（DanmakuSystem.cs） ──
    [SerializeField] private DanmakuWorldConfig _worldConfig;
    [SerializeField] private DanmakuRenderConfig _renderConfig;
    [SerializeField] private DanmakuTypeRegistry _typeRegistry;
    [SerializeField] private DanmakuTimeScaleSO _timeScale;
    [SerializeField] private DifficultyProfileSO _difficulty;
    [SerializeField] private GameEvent _onPlayerHit;
    [SerializeField] private IntGameEvent _onDamageDealt;

    public static DanmakuSystem Instance { get; private set; }

    // ── 子系统（Runtime.cs） ──
    private BulletWorld _bulletWorld;
    private LaserPool _laserPool;
    private SprayPool _sprayPool;
    private ObstaclePool _obstaclePool;
    private AttachSourceRegistry _attachRegistry;   // 激光/喷雾挂载源
    private TargetRegistry _targetRegistry;          // 多目标碰撞
    private CollisionSolver _collisionSolver;
    private CollisionEventBuffer _collisionEventBuffer;
    private PatternScheduler _scheduler;
    private SpawnerDriver _spawnerDriver;
    private BulletRenderer _bulletRenderer;
    private LaserRenderer _laserRenderer;
    private LaserWarningRenderer _laserWarningRenderer;
    private DamageNumberSystem _damageNumbers;
    private TrailPool _trailPool;
    private IDanmakuEffectsBridge _effectsBridge;    // 碰撞特效桥接
    private IDanmakuVFXRuntime _vfxRuntime;          // R4.0 VFX 管线桥接

    // ── 生命周期（DanmakuSystem.cs） ──
    void Awake()  → InitializeSubsystems()
    void Update() → RunUpdatePipeline()        // 逻辑 + 碰撞
    void LateUpdate() → RunLateUpdatePipeline() // 渲染提交

    // ── 公开 API（API.cs） ──
    public void FireBullets(BulletPatternSO pattern, Vector2 origin, float baseAngle);
    public void FireGroup(PatternGroupSO group, Vector2 origin, float baseAngle);
    public int  FireLaser(byte typeIndex, Vector2 origin, float angle, float length, float lifetime = 0f);
    public int  FireLaser(byte typeIndex, Transform source, float length, ...);  // Attached 模式
    public int  FireSpray(byte typeIndex, Vector2 origin, float direction, ...);
    public int  FireSpray(byte typeIndex, Transform source, ...);                // Attached 模式
    public void SetPlayer(Transform playerTransform, float radius);
    public bool RegisterTarget(ICollisionTarget target);
    public void UnregisterTarget(ICollisionTarget target);
    public void ClearAll();
    public void ClearAllBulletsWithEffect();
    public (string Label, RuntimeAtlasStats? Stats)[] GetAllAtlasStats();  // R4.3 Debug HUD
}
```

### 生命周期管理

**DontDestroyOnLoad + ClearAll 清场**：
- **Awake**：一次性预分配所有数组/Mesh/池（含 MotionRegistry.Initialize）。后续零 GC
- **场景切换**：`ClearAll()` 重置状态（先停所有喷雾附着 VFX，再逐池 FreeAll），不释放内存
- **OnDestroy**：`DisposeSubsystems()` 销毁 Mesh、释放 RBM 等 Unity 对象

> 保持内存常驻，避免场景切换时 128 KB+ 的 GC spike。

---

**相关文档**：
- [弹幕系统总览](DANMAKU_SYSTEM.md) — 系统架构、武器类型、框架集成
- [弹幕系统 — 数据结构](DANMAKU_DATA.md) — 所有运行时 struct 和枚举
- [弹幕系统 — SO 配置体系](DANMAKU_CONFIG.md) — 所有 ScriptableObject 定义
- [弹幕系统 — 渲染架构](DANMAKU_RENDERING.md) — Mesh 合批、拖尾、爆炸特效
