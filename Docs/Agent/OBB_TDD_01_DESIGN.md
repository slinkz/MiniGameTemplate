---
system: obb-collision
scope: design-math
last_verified: 2026-05-02
related_code: Assets/_Framework/OBB/ObstacleCollisionMath.cs, ObstaclePool.cs
---

## 1. 背景与动机

### 1.1 现状

当前弹幕系统的障碍物碰撞完全基于 **AABB（轴对齐包围盒）**：

- `ObstacleData` 结构体存储 `Min` / `Max` 两个 `Vector2`
- `CollisionSolver` 中圆 vs AABB（Phase 2）、扇形 vs AABB（Phase 6）
- `LaserSegmentSolver` 中射线 vs AABB（Slab 算法）
- 法线计算 `GetAABBNormal` 基于分离轴穿透深度
- 碰撞区域由 `SpriteRenderer.bounds` 定义（ObstacleRegistrar）

**限制**：障碍物只能是水平/垂直的矩形，不支持任何角度的旋转。设计师在场景中布局斜墙、旋转掩体等需求无法满足。

### 1.2 目标

1. 将障碍物碰撞从 AABB 升级为 **OBB（有向包围盒）**，支持任意 2D Z 轴旋转
2. 碰撞区域定义从 `SpriteRenderer.bounds` 改为 `BoxCollider2D`，Scene View 中所见即所得
3. 消除 `GetAABBNormal` / `ClampToAABB` 在两个 Solver 中的代码重复

### 1.3 非目标

- ❌ 不做圆形、多边形等非矩形障碍物
- ❌ 不做 3D 碰撞（仅 2D Z 轴旋转）
- ❌ 不做 DOTS/Burst 加速（保持现有 MonoBehaviour 架构）
- ❌ 不改变现有的阵营过滤、碰撞响应（Die/Reflect/Pierce 等）逻辑
- ❌ 不做空间分区优化（64 个障碍物上限，暴力遍历足够）

---

## 2. 行为契约（稳定层）

> 以下契约定义系统"对外承诺的行为"，独立于具体实现方案。

### BC-01: 旋转 0° 向后兼容

当障碍物旋转角为 0 时，所有碰撞行为（弹丸反射方向、激光截断位置、喷雾遮挡区域）必须与升级前 AABB 实现**数值一致**。

### BC-02: 圆 vs OBB 碰撞

给定圆心 P、半径 R、OBB（Center, HalfExtents, Rotation），碰撞判定等价于：
1. 将 P 变换到 OBB 局部坐标系
2. 在局部空间做标准圆 vs AABB 碰撞
3. 法线在局部空间计算后旋转回世界空间

### BC-03: 射线 vs OBB 碰撞

给定射线 (Origin, Direction)、膨胀宽度 halfWidth、OBB，碰撞判定等价于：
1. 将射线变换到 OBB 局部坐标系
2. 在局部空间做标准 Slab 算法
3. 返回的 t 值（碰撞距离）在世界空间中有效（因为旋转保持距离不变）

**前提假设**：Direction 为**单位向量**。只有 dir 为单位向量时，Slab 的 t 值才等于实际欧氏距离。多次反射后浮点漂移可能导致 dir 略偏离单位长度，这是现有 AABB 代码的已知限制，本次升级不引入新的偏差。(v1.2 补充)

### BC-04: 扇形 vs OBB 碰撞

距离检查用 BC-02 同款局部空间 clamp 做最近点计算；角度检查仍使用 OBB **中心点**相对于 Spray Origin 的方向（与现有 AABB 行为一致的近似）。

### BC-05: AddRect API 兼容

不传旋转参数时默认 0°，行为等同于旧 AABB。`AddCircle` 内部调用 `AddRect` 旋转固定为 0°。

### BC-06: UpdatePosition 兼容

保留 `UpdatePosition(index, center)` 重载——只更新位置，保持原有旋转角不变。

### BC-07: 障碍物生命周期不变

HitPoints / Phase / Faction / 被摧毁流程不受 OBB 升级影响。

---

## 3. 技术方案（易变层）

### 3.1 核心数学原理

OBB 碰撞检测的核心思想：**将碰撞对象变换到 OBB 的局部坐标系，在局部空间做标准 AABB 碰撞，结果再变换回世界坐标系。**

2D 旋转变换：
- **逆旋转（世界→局部）**：`localX = dx * cos + dy * sin`，`localY = -dx * sin + dy * cos`
- **正旋转（局部→世界）**：`worldX = lx * cos - ly * sin`，`worldY = lx * sin + ly * cos`

预计算 `sin` / `cos` 一次，所有碰撞检测复用。

### 3.2 数据结构变更

#### ObstacleData（Framework 核心）

```csharp
// BEFORE（24 bytes）
public struct ObstacleData
{
    public Vector2 Min;       // AABB 最小点
    public Vector2 Max;       // AABB 最大点
    public int HitPoints;
    public byte Faction;
    public byte Phase;
    public byte _pad1;
    public byte _pad2;
}

// AFTER（36 bytes）
public struct ObstacleData
{
    public Vector2 Center;       // OBB 中心（世界坐标）
    public Vector2 HalfExtents;  // 半尺寸（局部空间宽高的一半）
    public float RotationRad;    // 旋转角度（弧度，逆时针为正）
    public float Sin;            // 预计算 sin(RotationRad)
    public float Cos;            // 预计算 cos(RotationRad)
    public int HitPoints;
    public byte Faction;
    public byte Phase;
    public byte _pad1;
    public byte _pad2;
}
```

**设计决策——预计算 Sin/Cos vs 实时计算**：

| 方案 | 优点 | 缺点 |
|------|------|------|
| **A: 存 RotationRad + Sin/Cos** ✅ | 碰撞时零三角函数调用；UpdatePosition 可只更新 Center | 多 12 字节内存/obstacle |
| B: 只存 RotationRad | 内存紧凑 | 每次碰撞都要 `Mathf.Sin/Cos`；2000 弹丸 × 64 障碍物 = 128K 次三角函数调用/帧 |

**选择 A**：弹幕是高频碰撞场景，+768 bytes 总内存开销可忽略。

#### 内存布局分析

```
BEFORE: 2×8(Vector2 Min/Max) + 4(HP) + 4×1(Faction+Phase+pad×2) = 24 bytes × 64 = 1,536 bytes
AFTER:  2×8(Center/HalfExtents) + 3×4(Rot+Sin+Cos) + 4(HP) + 4×1(Faction+Phase+pad×2) = 36 bytes × 64 = 2,304 bytes
增量:   +768 bytes total（可忽略）
```

> **对齐说明**：struct 最大字段对齐为 4 bytes（float），36 是 4 的整数倍，无额外 padding。不同运行时/平台若有 8 字节对齐要求，每元素可能占 40 bytes（+256 bytes total），仍可忽略。(v1.2 补充)

### 3.3 ObstaclePool API 变更

```csharp
// AddRect：rotationRad 放在末尾（默认值 0f），避免破坏现有调用顺序
public int AddRect(Vector2 center, Vector2 size,
    int hitPoints = 0, BulletFaction faction = BulletFaction.Neutral,
    float rotationRad = 0f)

// 新增：同时更新位置和旋转
public void UpdateTransform(int index, Vector2 center, float rotationRad)

// 保留：只更新位置，旋转不变（BC-06）
public void UpdatePosition(int index, Vector2 center)

// AddCircle：不变（内部传 rotationRad = 0f）
public int AddCircle(Vector2 center, float radius,
    int hitPoints = 0, BulletFaction faction = BulletFaction.Neutral)
```

#### 关键实现片段 (v1.2 补充)

```csharp
// AddRect 核心赋值：
ref var obs = ref Data[slot];
obs.Center = center;
obs.HalfExtents = size * 0.5f;
obs.RotationRad = rotationRad;
obs.Sin = Mathf.Sin(rotationRad);
obs.Cos = Mathf.Cos(rotationRad);
obs.HitPoints = hitPoints;
obs.Faction = (byte)faction;
obs.Phase = (byte)ObstaclePhase.Active;

// UpdateTransform：同时更新位置和旋转
public void UpdateTransform(int index, Vector2 center, float rotationRad)
{
    if (index < 0 || index >= MAX_OBSTACLES) return;
    ref var obs = ref Data[index];
    if (obs.Phase == (byte)ObstaclePhase.Inactive) return;
    obs.Center = center;
    obs.RotationRad = rotationRad;
    obs.Sin = Mathf.Sin(rotationRad);   // 必须重算
    obs.Cos = Mathf.Cos(rotationRad);   // 必须重算
}

// UpdatePosition：只更新位置，Sin/Cos/RotationRad 不变（BC-06）
public void UpdatePosition(int index, Vector2 center)
{
    if (index < 0 || index >= MAX_OBSTACLES) return;
    ref var obs = ref Data[index];
    if (obs.Phase == (byte)ObstaclePhase.Inactive) return;
    obs.Center = center;  // 一行搞定
}
```

**关键设计决策——`rotationRad` 参数位置**：

| 方案 | 优点 | 缺点 |
|------|------|------|
| A: 插在 size 后面 `AddRect(center, size, rotationRad, hp, faction)` | 语义上连贯（几何参数在一起） | ⚠️ **静默 bug 风险**：现有调用 `AddRect(c, s, hp, fac)` 中 `hp(int)` 会隐式转为 `float rotationRad`，编译不报错但行为错误 |
| **B: 放在末尾 `AddRect(center, size, hp, faction, rotationRad)`** ✅ | 现有调用 `AddRect(c, s, hp, fac)` 完全不受影响，新参数有默认值 0f | 几何参数不连续 |

**选择 B**：API 安全优先。避免隐式类型转换导致的静默 bug，这类问题在运行时极难排查。

**受影响调用点**：

| 调用位置 | 当前调用 | 迁移方式 |
|----------|----------|----------|
| `ObstacleRegistrar.Register()` | `AddRect(center, size, _hitPoints, _faction)` | 追加 `rotation` 参数 |
| `ObstacleSpawner.SpawnAll()` | `AddRect(def.Center, def.Size, def.HitPoints, def.Faction)` | 追加 `def.Rotation * Deg2Rad` |
| `ObstaclePool.AddCircle()` | 内部调用 `AddRect(center, size2d, hitPoints, faction)` | 不变（默认 0f） |

### 3.4 碰撞数学——共享工具类 `ObstacleCollisionMath`

当前 `ClampToAABB`、`GetAABBNormal` 在 `CollisionSolver` 和 `LaserSegmentSolver` 中各有一份完全相同的副本（违反 DRY）。本次升级统一提取为共享 static 工具类：

```csharp
/// <summary>
/// OBB 碰撞数学工具——零分配、静态方法、AggressiveInlining。
/// 位于 _Framework/DanmakuSystem/Scripts/Core/ObstacleCollisionMath.cs
/// 注意：internal 是刻意设计——只对 Framework 内部暴露，Example 层通过 ObstaclePool API 间接使用。(v1.2 说明)
/// </summary>
internal static class ObstacleCollisionMath
{
    // ── 坐标变换 ──

    /// <summary>世界坐标点 → OBB 局部空间。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2 WorldToLocal(Vector2 worldPoint, in ObstacleData obs)
    {
        Vector2 d = worldPoint - obs.Center;
        return new Vector2(
            d.x * obs.Cos + d.y * obs.Sin,    // 逆旋转
           -d.x * obs.Sin + d.y * obs.Cos);
    }

    /// <summary>局部空间方向向量 → 世界空间方向。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2 LocalDirToWorld(Vector2 localDir, in ObstacleData obs)
    {
        return new Vector2(
            localDir.x * obs.Cos - localDir.y * obs.Sin,   // 正旋转
            localDir.x * obs.Sin + localDir.y * obs.Cos);
    }

    // ── 碰撞原语 ──

    /// <summary>圆 vs OBB。返回 true=碰撞，outNormal=世界空间法线。</summary>
    /// <remarks>v1.2 修正：法线计算内联，消除重复 WorldToLocal 调用。</remarks>
    internal static bool CircleVsOBB(
        Vector2 circleCenter, float radius,
        in ObstacleData obs, out Vector2 normal)
    {
        Vector2 local = WorldToLocal(circleCenter, in obs);
        Vector2 closest = ClampLocal(local, obs.HalfExtents);
        float dx = local.x - closest.x;
        float dy = local.y - closest.y;
        if (dx * dx + dy * dy >= radius * radius)
        {
            normal = default;
            return false;
        }
        // 法线直接在局部空间计算，复用已有的 local（避免重复 WorldToLocal）
        float ox = obs.HalfExtents.x - Mathf.Abs(local.x);
        float oy = obs.HalfExtents.y - Mathf.Abs(local.y);
        Vector2 ln = ox < oy
            ? new Vector2(local.x > 0 ? 1 : -1, 0)
            : new Vector2(0, local.y > 0 ? 1 : -1);
        normal = LocalDirToWorld(ln, in obs);
        return true;
    }

    /// <summary>射线 vs OBB（膨胀 halfWidth）。Slab 算法，返回 t 值。</summary>
    internal static float RayVsOBB(
        Vector2 origin, Vector2 dir, float maxDist,
        in ObstacleData obs, float halfWidth)
    {
        // 射线变换到局部空间
        Vector2 lo = WorldToLocal(origin, in obs);
        Vector2 ld = new Vector2(
            dir.x * obs.Cos + dir.y * obs.Sin,
           -dir.x * obs.Sin + dir.y * obs.Cos);
        // 膨胀
        float hex = obs.HalfExtents.x + halfWidth;
        float hey = obs.HalfExtents.y + halfWidth;
        // Slab
        float tMin = 0f, tMax = maxDist;
        // X slab
        if (Mathf.Abs(ld.x) < 1e-4f)
        { if (lo.x < -hex || lo.x > hex) return float.MaxValue; }
        else
        {
            float inv = 1f / ld.x;
            float t1 = (-hex - lo.x) * inv, t2 = (hex - lo.x) * inv;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Mathf.Max(tMin, t1); tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) return float.MaxValue;
        }
        // Y slab
        if (Mathf.Abs(ld.y) < 1e-4f)
        { if (lo.y < -hey || lo.y > hey) return float.MaxValue; }
        else
        {
            float inv = 1f / ld.y;
            float t1 = (-hey - lo.y) * inv, t2 = (hey - lo.y) * inv;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Mathf.Max(tMin, t1); tMax = Mathf.Min(tMax, t2);
            if (tMin > tMax) return float.MaxValue;
        }
        return tMin >= 0 ? tMin : 0f;
    }

    /// <summary>OBB 法线——局部空间分离轴法，旋转回世界空间。供 LaserSegmentSolver 等外部调用。</summary>
    internal static Vector2 GetOBBNormal(Vector2 worldPoint, in ObstacleData obs)
    {
        Vector2 local = WorldToLocal(worldPoint, in obs);
        float ox = obs.HalfExtents.x - Mathf.Abs(local.x);
        float oy = obs.HalfExtents.y - Mathf.Abs(local.y);
        Vector2 ln = ox < oy
            ? new Vector2(local.x > 0 ? 1 : -1, 0)
            : new Vector2(0, local.y > 0 ? 1 : -1);
        return LocalDirToWorld(ln, in obs);
    }

    // ── 封装原语（供外部调用） ──

    /// <summary>世界点到 OBB 的最近距离平方。(v1.3 新增，供 Phase 6 使用)</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float DistanceSqToOBB(Vector2 worldPoint, in ObstacleData obs)
    {
        Vector2 local = WorldToLocal(worldPoint, in obs);
        Vector2 closest = ClampLocal(local, obs.HalfExtents);
        float dx = local.x - closest.x;
        float dy = local.y - closest.y;
        return dx * dx + dy * dy;
    }

    // ── 内部辅助 ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 ClampLocal(Vector2 p, Vector2 he)
    {
        return new Vector2(
            Mathf.Clamp(p.x, -he.x, he.x),
            Mathf.Clamp(p.y, -he.y, he.y));
    }
}
```

### 3.5 CollisionSolver 变更清单

| 方法 | 变更内容 |
|------|----------|
| `SolveBulletVsObstacle` (Phase 2) | inline AABB 代码 → `ObstacleCollisionMath.CircleVsOBB()` |
| `SolveSprayVsObstacle` (Phase 6) | 见下方 Phase 6 OBB 适配伪代码 |
| `ClampToAABB` (private) | **删除**——移入共享工具类 |
| `GetAABBNormal` (private) | **删除**——移入共享工具类 |

注释 Phase 2 更新为"圆 vs OBB"。

#### Phase 6 OBB 适配伪代码 (v1.2 补充)

```csharp
// 距离检查——封装在 DistanceSqToOBB 中（v1.3 修正：不再直接调 private ClampLocal）
float distSq = ObstacleCollisionMath.DistanceSqToOBB(spray.Origin, in obs);
if (distSq > spray.Range * spray.Range) continue;

// 角度检查——仍使用世界空间 OBB 中心点（与旧 AABB 行为一致的近似）
Vector2 toObs = obs.Center - spray.Origin;
float angle = Mathf.Atan2(toObs.y, toObs.x);
float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
    angle * Mathf.Rad2Deg,
    spray.Direction * Mathf.Rad2Deg));
if (angleDiff > spray.ConeAngle * Mathf.Rad2Deg) continue;  // ConeAngle 是半角（弧度）
```

### 3.6 LaserSegmentSolver 变更清单

| 方法 | 变更内容 |
|------|----------|
| `RayVsAABB` (private) | **删除**——替换为 `ObstacleCollisionMath.RayVsOBB()` |
| `RaycastObstacles` | 改调 `RayVsOBB(origin, dir, maxDist, in obs, halfWidth)` |
| `RaycastObstaclesWithNormal` | 法线改用 `GetOBBNormal(hitPoint, in obs)` |
| `DamageAllObstaclesOnRay` | 同 `RaycastObstacles` |
| `GetAABBNormal` (private) | **删除**——移入共享工具类 |

### 3.7 ObstacleRegistrar 变更

```csharp
// BEFORE
[RequireComponent(typeof(SpriteRenderer))]
public class ObstacleRegistrar : MonoBehaviour { ... }

// AFTER
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class ObstacleRegistrar : MonoBehaviour
{
    private BoxCollider2D _collider;
    private float _lastRotZ;  // 变化检测（v1.2 补充）

    // ── RotateVector 辅助（内联 2D 旋转） (v1.2 补充) ──
    // 将 2D 向量绕原点旋转 angleDeg 度
    static Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Register():
    float rotZ = transform.eulerAngles.z;
    Vector2 worldCenter = (Vector2)transform.position
                        + RotateVector(_collider.offset, rotZ);
    // lossyScale 取 Abs 防负 scale（R6 缓解）
    // 注意：不支持有旋转的父级 Transform，lossyScale 在此场景下不准确。
    Vector2 size = new Vector2(
        _collider.size.x * Mathf.Abs(transform.lossyScale.x),
        _collider.size.y * Mathf.Abs(transform.lossyScale.y));
    float rotRad = rotZ * Mathf.Deg2Rad;
    _poolIndex = _pool.AddRect(worldCenter, size, _hitPoints, _faction, rotRad);
    _lastRotZ = rotZ;

    // Update()（v1.3 完善：补充 worldCenter 完整计算逻辑）:
    float curRotZ = transform.eulerAngles.z;
    Vector2 pos = (Vector2)transform.position;
    // worldCenter 计算：offset 非零时需旋转，为零时直接用 position（零三角函数）
    Vector2 curCenter = (_collider.offset == Vector2.zero)
        ? pos
        : pos + RotateVector(_collider.offset, curRotZ);

    if (Mathf.Approximately(curRotZ, _lastRotZ))
    {
        // 旋转没变——只更新位置，避免 Pool 侧三角函数调用
        // 注意：offset 非零时 RotateVector 仍有一次 sin/cos 开销（上方已计算）
        _pool.UpdatePosition(_poolIndex, curCenter);
    }
    else
    {
        _pool.UpdateTransform(_poolIndex, curCenter, curRotZ * Mathf.Deg2Rad);
        _lastRotZ = curRotZ;
    }

    // Reset()（编辑器自动配置）:
    void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        // 如果已有 SpriteRenderer，从其尺寸初始化 collider size
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            col.size = sr.sprite.bounds.size;
    }
}
```

**设计决策——保留 SpriteRenderer 还是只用 BoxCollider2D**：

| 方案 | 优点 | 缺点 |
|------|------|------|
| **A: 同时 RequireComponent(SR + BC2D)** ✅ | SR 负责视觉渲染、BC2D 负责碰撞定义，职责清晰 | 两个组件 |
| B: 只要 BoxCollider2D，自定义 Gizmo 画碰撞区域 | 只一个组件 | 障碍物没有视觉表现 |

**选择 A**：障碍物在 Game View 中必须可见，SpriteRenderer 不可少。

### 3.8 ObstacleSpawner 变更

`ObstacleDefinition` 增加 `Rotation` 字段：

```csharp
[System.Serializable]
public struct ObstacleDefinition
{
    public Vector2 Center;
    public Vector2 Size;
    [Tooltip("旋转角度（度，逆时针为正）")]
    public float Rotation;  // 新增
    public int HitPoints;
    public BulletFaction Faction;
}
```

Gizmo 旋转绘制：
```csharp
Gizmos.matrix = Matrix4x4.TRS(center3, Quaternion.Euler(0, 0, def.Rotation), Vector3.one);
Gizmos.DrawCube(Vector3.zero, size3);
Gizmos.matrix = Matrix4x4.identity;
```

---

