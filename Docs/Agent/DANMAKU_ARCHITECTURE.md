# 弹幕系统架构设计（Danmaku System）

## 概述

本文档定义在 MiniGameTemplate 框架上构建弹幕（Danmaku / Bullet Hell）游戏系统的架构方案。
核心设计原则：**纯数据驱动、零 GC、单 Mesh 合批渲染、自写碰撞**。

> 技术瓶颈分析见 → [DANMAKU_TECH_ANALYSIS.md](./DANMAKU_TECH_ANALYSIS.md)

---

## 一、系统总览

```
┌────────────────────────────────────────────────────────────────────┐
│                         Game Layer                                  │
│  BossAI / StageController / PlayerController                       │
│  （MonoBehaviour 薄壳，每个 < 150 行）                               │
└─────────┬──────────────────────┬───────────────────────────────────┘
          │ 引用 SO               │ 调用 API
┌─────────┴──────────────────────┴───────────────────────────────────┐
│                  ScriptableObject 资产层                             │
│  BulletPatternSO / BulletTypeSO / DanmakuConfigSO                  │
│  SpawnerProfileSO / DifficultyProfileSO                             │
│  FloatVariable(score,hp) / GameEvent(onHit,onBossPhase)            │
└─────────┬──────────────────────────────────────────────────────────┘
          │ 驱动
┌─────────┴──────────────────────────────────────────────────────────┐
│               Danmaku Module（高性能核心）                           │
│                                                                     │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐          │
│  │ BulletWorld  │→│ BulletMover   │→│ CollisionSolver   │          │
│  │ (数据容器)    │  │ (运动更新)    │  │ (碰撞检测)        │          │
│  └──────┬──────┘  └──────────────┘  └────────┬─────────┘          │
│         │                                      │                    │
│  ┌──────┴──────┐                      ┌───────┴────────┐          │
│  │ BulletSpawner│                      │ BulletRenderer  │          │
│  │ (发射器)     │                      │ (单Mesh合批渲染) │          │
│  └─────────────┘                      └────────────────┘          │
│                                                                     │
│  单个 DanmakuSystem MonoBehaviour 驱动整个模块                       │
└────────────────────────────────────────────────────────────────────┘
```

### 关键约束

| 约束 | 说明 |
|------|------|
| **零 GameObject** | 弹幕不是 GameObject，是 struct 数组中的一行数据 |
| **零 GC** | 全部预分配，运行时无 new / List 扩容 / 装箱 |
| **1-3 Draw Call** | 所有弹幕共享一个 Material + Atlas，单 Mesh 渲染 |
| **自写碰撞** | 圆 vs 圆/点，不用 Physics2D |
| **SO 驱动** | 弹幕模式、弹幕类型、难度曲线全部是 SO 资产 |
| **单一 Update** | 整个弹幕系统只有一个 MonoBehaviour.Update() |

---

## 二、数据结构

### 2.1 BulletData（运行时弹幕实例，struct）

```csharp
/// <summary>
/// 单颗弹幕的运行时数据。纯值类型，存储在预分配数组中。
/// 32 bytes，缓存友好。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BulletData
{
    public Vector2 Position;     // 8B — 当前位置
    public Vector2 Velocity;     // 8B — 当前速度（方向 × 速率）
    public float Lifetime;       // 4B — 剩余存活时间
    public float Radius;         // 4B — 碰撞半径
    public ushort TypeIndex;     // 2B — 索引 BulletTypeSO 配置（颜色/UV）
    public ushort Flags;         // 2B — 位标记（Active, Homing, SpeedCurve...）
    public float SpawnTime;      // 4B — 生成时的 Time.time（用于速度曲线采样）

    // Flags 位定义
    public const ushort FLAG_ACTIVE      = 1 << 0;
    public const ushort FLAG_HOMING      = 1 << 1;
    public const ushort FLAG_SPEED_CURVE = 1 << 2;
    public const ushort FLAG_ROTATABLE   = 1 << 3;

    public bool IsActive
    {
        get => (Flags & FLAG_ACTIVE) != 0;
        set => Flags = value
            ? (ushort)(Flags | FLAG_ACTIVE)
            : (ushort)(Flags & ~FLAG_ACTIVE);
    }
}
```

### 2.2 BulletWorld（数据容器）

```csharp
/// <summary>
/// 弹幕世界：持有所有弹幕数据的预分配容器。
/// 不继承 MonoBehaviour，纯 C# 类。
/// </summary>
public class BulletWorld
{
    public const int MAX_BULLETS = 2048;

    public readonly BulletData[] Bullets = new BulletData[MAX_BULLETS];
    public int ActiveCount { get; private set; }

    // 空闲槽位栈（预分配，避免 GC）
    private readonly int[] _freeSlots = new int[MAX_BULLETS];
    private int _freeTop;

    public BulletWorld()
    {
        // 初始化时所有槽位都空闲
        for (int i = MAX_BULLETS - 1; i >= 0; i--)
            _freeSlots[_freeTop++] = i;
    }

    /// <summary>分配一个弹幕槽位。返回索引，-1 表示已满。</summary>
    public int Allocate()
    {
        if (_freeTop == 0) return -1;
        int idx = _freeSlots[--_freeTop];
        ActiveCount++;
        return idx;
    }

    /// <summary>释放弹幕槽位。</summary>
    public void Free(int index)
    {
        Bullets[index].IsActive = false;
        _freeSlots[_freeTop++] = index;
        ActiveCount--;
    }

    /// <summary>释放全部弹幕。</summary>
    public void FreeAll()
    {
        _freeTop = 0;
        ActiveCount = 0;
        for (int i = MAX_BULLETS - 1; i >= 0; i--)
        {
            Bullets[i].IsActive = false;
            _freeSlots[_freeTop++] = i;
        }
    }
}
```

---

## 三、ScriptableObject 配置资产

### 3.1 BulletTypeSO — 弹幕视觉类型

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Type")]
public class BulletTypeSO : ScriptableObject
{
    [Header("视觉")]
    public Rect AtlasUV;           // 在图集上的 UV 矩形
    public Color Tint = Color.white;
    public Vector2 Size = new Vector2(0.2f, 0.2f);

    [Header("碰撞")]
    public float CollisionRadius = 0.1f;

    [Header("运行时索引（由系统自动分配）")]
    [HideInInspector] public ushort RuntimeIndex;
}
```

### 3.2 BulletPatternSO — 弹幕模式（策划核心配置）

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Pattern")]
public class BulletPatternSO : ScriptableObject
{
    [Header("弹幕类型")]
    public BulletTypeSO BulletType;

    [Header("发射参数")]
    public int Count = 12;              // 单次发射弹幕数量
    public float SpreadAngle = 360f;    // 散布角度（360 = 全方位）
    public float StartAngle = 0f;       // 起始角度偏移
    public float AnglePerShot = 0f;     // 每次发射的角度递增（旋转弹幕用）

    [Header("运动参数")]
    public float Speed = 5f;
    public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);
    public float Lifetime = 5f;

    [Header("追踪")]
    public bool IsHoming;
    public float HomingStrength = 2f;   // 追踪转向速度（度/秒）

    [Header("连射")]
    public int BurstCount = 1;          // 连射组数
    public float BurstInterval = 0.05f; // 组间间隔（秒）

    [Header("音效")]
    public AudioClipSO FireSFX;
}
```

### 3.3 SpawnerProfileSO — 发射器行为配置

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spawner Profile")]
public class SpawnerProfileSO : ScriptableObject
{
    [Tooltip("按顺序循环执行的弹幕模式列表")]
    public BulletPatternSO[] Patterns;

    [Tooltip("每个模式的发射间隔（秒）")]
    public float FireInterval = 0.5f;

    [Tooltip("模式切换间歇（秒）")]
    public float PatternSwitchDelay = 1f;

    [Tooltip("循环次数，0 = 无限")]
    public int LoopCount = 0;
}
```

### 3.4 DanmakuConfigSO — 全局弹幕系统配置

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config")]
public class DanmakuConfigSO : ScriptableObject
{
    [Header("容量")]
    public int MaxBullets = 2048;

    [Header("世界边界（超出自动回收）")]
    public Rect WorldBounds = new Rect(-6, -10, 12, 20);

    [Header("碰撞网格")]
    public int GridCellsX = 12;
    public int GridCellsY = 20;

    [Header("弹幕类型注册表")]
    public BulletTypeSO[] BulletTypes;

    [Header("渲染")]
    public Material BulletMaterial;
    public Texture2D BulletAtlas;

    [Header("难度")]
    public DifficultyProfileSO DefaultDifficulty;
}
```

### 3.5 DifficultyProfileSO — 难度配置

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Difficulty Profile")]
public class DifficultyProfileSO : ScriptableObject
{
    [Tooltip("全局弹速倍率")]
    [Range(0.5f, 3f)] public float SpeedMultiplier = 1f;

    [Tooltip("全局弹幕密度倍率（影响 Pattern.Count）")]
    [Range(0.5f, 3f)] public float DensityMultiplier = 1f;

    [Tooltip("全局弹幕存活时间倍率")]
    [Range(0.5f, 2f)] public float LifetimeMultiplier = 1f;

    [Tooltip("随游戏时长递增的难度曲线（X=分钟, Y=总倍率）")]
    public AnimationCurve DifficultyOverTime = AnimationCurve.Linear(0, 1, 10, 2);
}
```

---

## 四、核心系统

### 4.1 BulletMover — 运动更新

```csharp
/// <summary>
/// 纯静态工具类，每帧更新所有活跃弹幕的位置。
/// 无状态，无 MonoBehaviour，无 GC。
/// </summary>
public static class BulletMover
{
    public static void UpdateAll(BulletWorld world, Rect bounds, float dt)
    {
        var bullets = world.Bullets;
        for (int i = 0; i < BulletWorld.MAX_BULLETS; i++)
        {
            if (!bullets[i].IsActive) continue;

            ref var b = ref bullets[i];

            // 速度曲线
            if ((b.Flags & BulletData.FLAG_SPEED_CURVE) != 0)
            {
                // 由调用方在 Spawn 时注入曲线采样结果到 Velocity 的 magnitude
                // 这里省略（见 BulletSpawner）
            }

            // 运动
            b.Position += b.Velocity * dt;

            // 生命周期
            b.Lifetime -= dt;

            // 回收条件：超时 或 出界
            if (b.Lifetime <= 0f || !bounds.Contains(b.Position))
            {
                world.Free(i);
            }
        }
    }
}
```

### 4.2 CollisionSolver — 碰撞检测

```csharp
/// <summary>
/// 简单几何碰撞：玩家判定点（圆）vs 弹幕（圆）。
/// 使用均匀网格空间分区加速。
/// </summary>
public class CollisionSolver
{
    private readonly int _cellsX, _cellsY;
    private readonly Rect _bounds;
    private readonly float _cellWidth, _cellHeight;

    // 网格桶：每个格子存储弹幕索引列表
    // 预分配，避免 GC
    private readonly int[] _grid;          // 扁平化 [cellsX * cellsY * bucketCapacity]
    private readonly int[] _gridCounts;    // 每格当前数量
    private const int BUCKET_CAPACITY = 32;

    public CollisionSolver(DanmakuConfigSO config)
    {
        _cellsX = config.GridCellsX;
        _cellsY = config.GridCellsY;
        _bounds = config.WorldBounds;
        _cellWidth = _bounds.width / _cellsX;
        _cellHeight = _bounds.height / _cellsY;
        _grid = new int[_cellsX * _cellsY * BUCKET_CAPACITY];
        _gridCounts = new int[_cellsX * _cellsY];
    }

    /// <summary>
    /// 构建空间网格（每帧调用一次）。
    /// </summary>
    public void BuildGrid(BulletWorld world)
    {
        // 清空计数
        System.Array.Clear(_gridCounts, 0, _gridCounts.Length);

        var bullets = world.Bullets;
        for (int i = 0; i < BulletWorld.MAX_BULLETS; i++)
        {
            if (!bullets[i].IsActive) continue;

            int cx = (int)((bullets[i].Position.x - _bounds.x) / _cellWidth);
            int cy = (int)((bullets[i].Position.y - _bounds.y) / _cellHeight);
            cx = Mathf.Clamp(cx, 0, _cellsX - 1);
            cy = Mathf.Clamp(cy, 0, _cellsY - 1);

            int cell = cy * _cellsX + cx;
            int count = _gridCounts[cell];
            if (count < BUCKET_CAPACITY)
            {
                _grid[cell * BUCKET_CAPACITY + count] = i;
                _gridCounts[cell] = count + 1;
            }
        }
    }

    /// <summary>
    /// 检测玩家碰撞。返回命中的弹幕索引列表（通过回调，避免 alloc）。
    /// </summary>
    public void CheckCollision(
        Vector2 playerPos, float playerRadius,
        BulletWorld world,
        System.Action<int> onHit)
    {
        int cx = (int)((playerPos.x - _bounds.x) / _cellWidth);
        int cy = (int)((playerPos.y - _bounds.y) / _cellHeight);

        // 检查玩家所在格及 8 邻格
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int nx = cx + dx;
            int ny = cy + dy;
            if (nx < 0 || nx >= _cellsX || ny < 0 || ny >= _cellsY) continue;

            int cell = ny * _cellsX + nx;
            int count = _gridCounts[cell];
            int baseIdx = cell * BUCKET_CAPACITY;

            for (int j = 0; j < count; j++)
            {
                int bulletIdx = _grid[baseIdx + j];
                ref var b = ref world.Bullets[bulletIdx];
                if (!b.IsActive) continue;

                float dx2 = b.Position.x - playerPos.x;
                float dy2 = b.Position.y - playerPos.y;
                float distSq = dx2 * dx2 + dy2 * dy2;
                float radSum = b.Radius + playerRadius;

                if (distSq < radSum * radSum)
                {
                    onHit(bulletIdx);
                }
            }
        }
    }
}
```

### 4.3 BulletSpawner — 发射器

```csharp
/// <summary>
/// 根据 BulletPatternSO 配置向 BulletWorld 发射弹幕。
/// 纯方法调用，无状态，无 MonoBehaviour。
/// </summary>
public static class BulletSpawner
{
    public static void SpawnPattern(
        BulletWorld world,
        BulletPatternSO pattern,
        Vector2 origin,
        float baseAngleDeg,
        DifficultyProfileSO difficulty,
        float gameTime)
    {
        float speedMul = difficulty?.SpeedMultiplier ?? 1f;
        float densityMul = difficulty?.DensityMultiplier ?? 1f;
        int count = Mathf.RoundToInt(pattern.Count * densityMul);
        float angleStep = count > 1 ? pattern.SpreadAngle / count : 0f;
        float startAngle = baseAngleDeg + pattern.StartAngle
                         - (count > 1 ? pattern.SpreadAngle * 0.5f : 0f);

        bool hasSpeedCurve = pattern.SpeedOverLifetime.keys.Length > 2;

        for (int i = 0; i < count; i++)
        {
            int idx = world.Allocate();
            if (idx < 0) return; // 弹幕池已满

            float angleDeg = startAngle + angleStep * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            ref var b = ref world.Bullets[idx];
            b.Position = origin;
            b.Velocity = dir * pattern.Speed * speedMul;
            b.Lifetime = pattern.Lifetime * (difficulty?.LifetimeMultiplier ?? 1f);
            b.Radius = pattern.BulletType.CollisionRadius;
            b.TypeIndex = pattern.BulletType.RuntimeIndex;
            b.SpawnTime = gameTime;
            b.Flags = BulletData.FLAG_ACTIVE;

            if (pattern.IsHoming)
                b.Flags |= BulletData.FLAG_HOMING;
            if (hasSpeedCurve)
                b.Flags |= BulletData.FLAG_SPEED_CURVE;
        }
    }
}
```

### 4.4 BulletRenderer — 单 Mesh 合批渲染

```csharp
/// <summary>
/// 将所有活跃弹幕渲染为一个合并 Mesh。
/// 1 个 MeshFilter + 1 个 MeshRenderer = 1 Draw Call。
/// </summary>
public class BulletRenderer
{
    private readonly Mesh _mesh;
    private readonly Vector3[] _vertices;
    private readonly Vector2[] _uvs;
    private readonly Color32[] _colors;
    private readonly int[] _triangles;
    private readonly BulletTypeSO[] _types;

    private const int VERTS_PER_BULLET = 4;
    private const int TRIS_PER_BULLET = 6;

    public BulletRenderer(int maxBullets, BulletTypeSO[] types)
    {
        _types = types;
        _mesh = new Mesh { name = "DanmakuMesh" };
        _mesh.MarkDynamic(); // 提示 Unity 这是每帧更新的 Mesh

        int maxVerts = maxBullets * VERTS_PER_BULLET;
        int maxTris = maxBullets * TRIS_PER_BULLET;

        _vertices = new Vector3[maxVerts];
        _uvs = new Vector2[maxVerts];
        _colors = new Color32[maxVerts];
        _triangles = new int[maxTris];
    }

    /// <summary>
    /// 每帧调用：从 BulletWorld 数据重建 Mesh。
    /// </summary>
    public void Rebuild(BulletWorld world)
    {
        int quadIdx = 0;
        var bullets = world.Bullets;

        for (int i = 0; i < BulletWorld.MAX_BULLETS; i++)
        {
            if (!bullets[i].IsActive) continue;

            ref var b = ref bullets[i];
            var type = _types[b.TypeIndex];
            var pos = b.Position;
            float hw = type.Size.x * 0.5f;
            float hh = type.Size.y * 0.5f;

            int vi = quadIdx * VERTS_PER_BULLET;

            // 四个顶点（Billboard，面向摄像机）
            _vertices[vi + 0] = new Vector3(pos.x - hw, pos.y - hh, 0);
            _vertices[vi + 1] = new Vector3(pos.x + hw, pos.y - hh, 0);
            _vertices[vi + 2] = new Vector3(pos.x + hw, pos.y + hh, 0);
            _vertices[vi + 3] = new Vector3(pos.x - hw, pos.y + hh, 0);

            // UV（从图集 Rect）
            _uvs[vi + 0] = new Vector2(type.AtlasUV.xMin, type.AtlasUV.yMin);
            _uvs[vi + 1] = new Vector2(type.AtlasUV.xMax, type.AtlasUV.yMin);
            _uvs[vi + 2] = new Vector2(type.AtlasUV.xMax, type.AtlasUV.yMax);
            _uvs[vi + 3] = new Vector2(type.AtlasUV.xMin, type.AtlasUV.yMax);

            // 颜色
            Color32 c = type.Tint;
            _colors[vi + 0] = c;
            _colors[vi + 1] = c;
            _colors[vi + 2] = c;
            _colors[vi + 3] = c;

            // 三角形索引
            int ti = quadIdx * TRIS_PER_BULLET;
            _triangles[ti + 0] = vi;
            _triangles[ti + 1] = vi + 2;
            _triangles[ti + 2] = vi + 1;
            _triangles[ti + 3] = vi;
            _triangles[ti + 4] = vi + 3;
            _triangles[ti + 5] = vi + 2;

            quadIdx++;
        }

        // 清理未使用的三角形索引（避免渲染残留）
        int usedTris = quadIdx * TRIS_PER_BULLET;
        for (int t = usedTris; t < _triangles.Length; t++)
            _triangles[t] = 0;

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.colors32 = _colors;
        _mesh.triangles = _triangles;
    }

    public Mesh GetMesh() => _mesh;
}
```

---

## 五、DanmakuSystem — 唯一的 MonoBehaviour

```csharp
/// <summary>
/// 弹幕系统入口。场景中唯一的弹幕 MonoBehaviour。
/// 每帧驱动：运动 → 碰撞 → 渲染。
/// </summary>
public class DanmakuSystem : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private DanmakuConfigSO _config;

    [Header("事件通道")]
    [SerializeField] private GameEvent _onPlayerHit;
    [SerializeField] private IntGameEvent _onBulletCountChanged;

    [Header("运行时数据（调试用，只读）")]
    [SerializeField, HideInInspector] private int _debugActiveBullets;

    // 核心子系统
    private BulletWorld _world;
    private CollisionSolver _collision;
    private BulletRenderer _renderer;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    // 玩家引用（通过 SO RuntimeSet 或直接注入）
    private Transform _playerTransform;
    private float _playerRadius = 0.05f; // 判定点极小

    private void Awake()
    {
        _world = new BulletWorld();
        _collision = new CollisionSolver(_config);

        // 注册弹幕类型运行时索引
        for (ushort i = 0; i < _config.BulletTypes.Length; i++)
            _config.BulletTypes[i].RuntimeIndex = i;

        _renderer = new BulletRenderer(_config.MaxBullets, _config.BulletTypes);

        // 渲染组件
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = _config.BulletMaterial;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. 运动更新
        BulletMover.UpdateAll(_world, _config.WorldBounds, dt);

        // 2. 碰撞检测
        if (_playerTransform != null)
        {
            _collision.BuildGrid(_world);
            _collision.CheckCollision(
                (Vector2)_playerTransform.position,
                _playerRadius,
                _world,
                OnBulletHitPlayer);
        }

        // 3. 渲染
        _renderer.Rebuild(_world);
        _meshFilter.mesh = _renderer.GetMesh();

        // 4. 调试 / 事件
        _debugActiveBullets = _world.ActiveCount;
        _onBulletCountChanged?.Raise(_world.ActiveCount);
    }

    private void OnBulletHitPlayer(int bulletIndex)
    {
        _world.Free(bulletIndex);
        _onPlayerHit?.Raise();
    }

    // —— 公开 API ——

    /// <summary>注入玩家引用（由 PlayerController 在初始化时调用）。</summary>
    public void SetPlayer(Transform player, float radius = 0.05f)
    {
        _playerTransform = player;
        _playerRadius = radius;
    }

    /// <summary>按模式发射弹幕。</summary>
    public void Fire(BulletPatternSO pattern, Vector2 origin, float angleDeg)
    {
        BulletSpawner.SpawnPattern(
            _world, pattern, origin, angleDeg,
            _config.DefaultDifficulty, Time.time);

        if (pattern.FireSFX != null)
            AudioManager.Instance.PlaySFX(pattern.FireSFX);
    }

    /// <summary>清除全部弹幕（Boss 转阶段 / 玩家使用炸弹）。</summary>
    public void ClearAll()
    {
        _world.FreeAll();
    }

    /// <summary>当前活跃弹幕数量。</summary>
    public int ActiveBulletCount => _world.ActiveCount;
}
```

---

## 六、与框架集成

### 6.1 事件通道连接

| SO 事件 | 发布者 | 监听者 |
|---------|--------|--------|
| `OnPlayerHit : GameEvent` | DanmakuSystem | PlayerHealth（扣血）、VFXController（闪白）、AudioManager（被击音效） |
| `OnBulletCountChanged : IntGameEvent` | DanmakuSystem | DebugHUD（显示弹幕数） |
| `OnBossPhaseChanged : IntGameEvent` | BossAI | DanmakuSystem.ClearAll()（清屏）+ SpawnerController（切换弹幕模式） |
| `OnBombUsed : GameEvent` | PlayerController | DanmakuSystem.ClearAll() + VFXController（全屏特效） |

### 6.2 与现有框架模块的关系

| 框架模块 | 弹幕系统使用方式 |
|---------|----------------|
| **EventSystem** | GameEvent / IntGameEvent 做跨系统通信 |
| **AudioSystem** | AudioManager.PlaySFX(pattern.FireSFX) 播发射音效 |
| **TimerService** | Boss 弹幕间隔调度：`TimerService.Instance.Repeat(interval, FireNext)` |
| **FSM** | Boss 阶段状态机，State SO 切换触发不同 SpawnerProfile |
| **ObjectPool** | **不使用**。弹幕系统内部自带 BulletWorld 预分配池 |
| **FloatVariable** | 玩家血量、分数等共享变量 |

### 6.3 推荐目录结构

```
Assets/_Framework/DanmakuSystem/
├── MODULE_README.md
├── Scripts/
│   ├── Data/
│   │   ├── BulletData.cs
│   │   ├── BulletWorld.cs
│   │   ├── BulletTypeSO.cs
│   │   ├── BulletPatternSO.cs
│   │   ├── SpawnerProfileSO.cs
│   │   ├── DanmakuConfigSO.cs
│   │   └── DifficultyProfileSO.cs
│   ├── Core/
│   │   ├── BulletMover.cs
│   │   ├── BulletSpawner.cs
│   │   ├── CollisionSolver.cs
│   │   └── BulletRenderer.cs
│   └── DanmakuSystem.cs
└── Shaders/
    └── Danmaku-Unlit-Atlas.shader    ← 极简 Unlit shader，支持顶点色 + 图集 UV
```

---

## 七、性能预算

基于微信小游戏 WebGL 环境（中端 Android 手机）：

| 系统 | 操作 | 预算 |
|------|------|------|
| BulletMover | 2048 颗遍历 + 运动 + 回收 | ≤ 1.0ms |
| CollisionSolver | 构建网格 + 查询 9 格 | ≤ 0.5ms |
| BulletRenderer | 重建 2048 quad Mesh | ≤ 1.5ms |
| GPU 渲染 | 1 Draw Call, 2048 半透明 quad | ≤ 2.0ms |
| **总计** | | **≤ 5.0ms（60fps 下 33% 帧预算）** |

### GC 预算

| 来源 | 预期 |
|------|------|
| 每帧 | **0 bytes**（全部预分配 + struct） |
| 弹幕发射 | 0（写入预分配数组） |
| 弹幕回收 | 0（标记 + 归还槽位） |
| Mesh 更新 | 0（预分配 Vector3[]/int[] 数组） |

---

## 八、扩展点

### 8.1 追踪弹（Homing）

在 `BulletMover.UpdateAll` 中增加 homing 逻辑：

```csharp
if ((b.Flags & BulletData.FLAG_HOMING) != 0)
{
    Vector2 toTarget = playerPos - b.Position;
    float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x);
    float currentAngle = Mathf.Atan2(b.Velocity.y, b.Velocity.x);
    float newAngle = Mathf.MoveTowardsAngle(
        currentAngle * Mathf.Rad2Deg,
        targetAngle * Mathf.Rad2Deg,
        homingStrength * dt) * Mathf.Deg2Rad;
    float speed = b.Velocity.magnitude;
    b.Velocity = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * speed;
}
```

### 8.2 激光（Laser Beam）

激光不走弹幕系统，单独用 `LineRenderer` 或自定义 Mesh 绘制。碰撞用线段 vs 圆检测。

### 8.3 弹幕录像回放

由于弹幕数据全部是确定性的 struct 数组（位置 + 速度 + 时间），可以直接序列化每帧的发射指令（Pattern + origin + angle），回放时重新模拟即可。

### 8.4 子弹时间（Bullet Time）

修改传入 `BulletMover.UpdateAll` 的 `dt` 即可全局慢动作。不需要 `Time.timeScale`（避免影响其他系统）。

---

## 九、暂不实现（后续迭代）

| 功能 | 原因 |
|------|------|
| 弹幕编辑器 | 当前用 SO Inspector + AnimationCurve 已够用，后续视策划需求决定 |
| GPU 粒子弹幕 | WebGL 无 Compute Shader，等微信小游戏支持 WebGL 2.0 后再评估 |
| 弹幕脚本语言 | BulletML 等 DSL 增加学习成本，先用 SO 组合覆盖 80% 弹幕模式 |
| 多玩家碰撞 | 当前假设单玩家，扩展时在 `CollisionSolver.CheckCollision` 循环多个玩家即可 |

---

## 十、结论

本架构通过 **纯数据数组 + 单 Mesh 合批 + 网格碰撞** 解决了微信小游戏平台弹幕游戏的三大 P0 瓶颈
（Draw Call / MonoBehaviour 桥接 / Physics2D），同时保持了与 MiniGameTemplate 框架的 SO 驱动理念一致：

- 策划通过 `BulletPatternSO` / `DifficultyProfileSO` 配置弹幕，**不碰代码**
- 系统间通过 `GameEvent` SO 通道通信，**零硬引用**
- 整个模块自包含在 `_Framework/DanmakuSystem/`，**可独立测试**
