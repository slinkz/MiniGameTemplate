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

穿透弹命中后继续飞行，`LastHitId`（1 byte）记录上次命中目标，防止多帧重复碰撞：

```csharp
// 碰撞检测时
if ((c.Flags & FLAG_PIERCE_COOLDOWN) != 0 && c.LastHitId == targetId) continue;

// BulletMover 每帧：不再重叠时清除冷却
c.Flags &= unchecked((byte)~FLAG_PIERCE_COOLDOWN);
c.LastHitId = 0;
```

> **为什么 1 byte 够用**：弹幕游戏中穿透弹同时穿越两个目标的概率极低。如确需同时穿越多目标，可升级为 `ushort LastHitId1, LastHitId2`。

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
float dt = _timeScale.DeltaTime;  // 循环外缓存
BulletMover.UpdateAll(_bulletWorld, ..., dt);
LaserUpdater.UpdateAll(_laserPool, dt);
_damageNumbers.Update(dt);
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

```csharp
public class DanmakuSystem : MonoBehaviour
{
    [SerializeField] private DanmakuWorldConfig _worldConfig;
    [SerializeField] private DanmakuRenderConfig _renderConfig;
    [SerializeField] private DanmakuTypeRegistry _typeRegistry;
    [SerializeField] private DanmakuTimeScaleSO _timeScale;
    [SerializeField] private GameEvent _onPlayerHit;
    [SerializeField] private IntGameEvent _onDamageDealt;

    private BulletWorld _bulletWorld;
    private LaserPool _laserPool;
    private SprayPool _sprayPool;
    private ObstaclePool _obstaclePool;
    private CollisionSolver _collision;
    private BulletRenderer _bulletRenderer;
    private DamageNumberSystem _damageNumbers;
    private TrailPool _trailPool;
    private PatternScheduler _patternScheduler;

    private void Awake()
    {
        _typeRegistry.AssignRuntimeIndices();   // + DFS 环引用检测
        _bulletWorld = new BulletWorld(_worldConfig.MaxBullets);
        _bulletRenderer = new BulletRenderer();
        _bulletRenderer.Initialize(_worldConfig.MaxBullets * 4, _renderConfig);
        _patternScheduler = new PatternScheduler();
    }

    private void Update()
    {
        float dt = _timeScale.DeltaTime;
        // 0. 同步玩家碰撞体
        // 1. 弹幕组合调度
        _patternScheduler.Update(dt, this);
        // 2. 运动更新
        BulletMover.UpdateAll(_bulletWorld, _typeRegistry, _worldConfig.WorldBounds, dt);
        LaserUpdater.UpdateAll(_laserPool, dt);
        SprayUpdater.UpdateAll(_sprayPool, dt);
        // 3. 碰撞检测
        var hitResult = _collision.SolveAll(...);
        // 4. 无敌帧伤害控制
        // 5. 渲染
        _bulletRenderer.Rebuild(_bulletWorld, _typeRegistry);
        _damageNumbers.Update(dt);
        _trailPool.Update(dt);
    }

    // —— 公开 API ——
    public void FireBullets(BulletPatternSO pattern, Vector2 origin, float angle) { }
    public void FirePatternGroup(PatternGroupSO group, Vector2 origin, float angle, Transform aimTarget = null) { }
    public int FireLaser(LaserTypeSO type, Vector2 origin, float angle) { }
    public int FireSpray(SprayTypeSO type, Vector2 origin, float direction) { }
    public int AddObstacle(ObstacleTypeSO type, Vector2 center) { }
    public void RemoveObstacle(int index) { }
    public void ClearAllBullets() { }
    public void ClearAllObstacles() { }
    public void SetPlayer(Transform player, float radius) { }
    public int ActiveBulletCount => _bulletWorld.ActiveCount;
}
```

### 生命周期管理

**DontDestroyOnLoad + FreeAll 清场**：
- **Awake**：一次性预分配所有数组/Mesh/池。后续零 GC
- **场景切换**：`FreeAll()` 重置状态（清零 + 空闲栈回满），不释放内存
- **OnDestroy**：销毁 Mesh、释放 Trail 等 Unity 对象

> 保持内存常驻，避免场景切换时 128 KB+ 的 GC spike。

---

**相关文档**：
- [弹幕系统总览](DANMAKU_SYSTEM.md) — 系统架构、武器类型、框架集成
- [弹幕系统 — 数据结构](DANMAKU_DATA.md) — 所有运行时 struct 和枚举
- [弹幕系统 — SO 配置体系](DANMAKU_CONFIG.md) — 所有 ScriptableObject 定义
- [弹幕系统 — 渲染架构](DANMAKU_RENDERING.md) — Mesh 合批、拖尾、爆炸特效
