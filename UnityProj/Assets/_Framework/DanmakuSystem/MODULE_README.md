# DanmakuSystem（弹幕系统）

## 概述

纯数据驱动弹幕系统，专为微信小游戏（WebGL）优化。支持弹丸/激光/喷雾三种武器类型 + 障碍物交互，零 GC 分配，2048 弹丸 ≤ 5.7ms/帧。

## 目录结构

```
DanmakuSystem/
├── MODULE_README.md          # 本文件
├── Scripts/
│   ├── DanmakuSystem.cs      # 唯一 MonoBehaviour 入口
│   ├── Data/                  # 纯数据结构 + 容器
│   │   ├── BulletCore.cs      # 弹丸热数据 (36B)
│   │   ├── BulletTrail.cs     # 弹丸冷数据 (28B)
│   │   ├── BulletModifier.cs  # 修饰数据 (16B)
│   │   ├── DanmakuEnums.cs    # 所有枚举（含 LaserObstacleResponse, LaserScreenEdgeResponse, SprayObstacleResponse）
│   │   ├── CircleHitbox.cs    # 圆形碰撞体
│   │   ├── DanmakuVertex.cs   # 渲染顶点 (24B)
│   │   ├── LaserData.cs       # 激光运行时数据（含 Segments[], AttachId）
│   │   ├── LaserSegment.cs    # 折射线段数据结构 (Start/End/Normal)
│   │   ├── SprayData.cs       # 喷雾运行时数据（含 AttachId）
│   │   ├── ObstacleData.cs    # 障碍物运行时数据
│   │   ├── DamageNumberData.cs # 伤害飘字数据
│   │   ├── AttachSourceRegistry.cs # 挂载源注册表 (24 槽, 引用计数)
│   │   ├── BulletWorld.cs     # 弹丸 SoA 容器 (2048)
│   │   ├── LaserPool.cs       # 激光容器 (16)
│   │   ├── SprayPool.cs       # 喷雾容器 (8)
│   │   └── ObstaclePool.cs    # 障碍物容器 (64)
│   ├── Config/                # ScriptableObject 配置
│   │   ├── BulletTypeSO.cs    # 弹丸视觉类型
│   │   ├── LaserTypeSO.cs     # 激光类型（含折射/穿透响应）
│   │   ├── SprayTypeSO.cs     # 喷雾类型（含碰撞响应）
│   │   ├── ObstacleTypeSO.cs  # 障碍物类型
│   │   ├── BulletPatternSO.cs # 弹幕发射模式
│   │   ├── PatternGroupSO.cs  # 弹幕组合编排
│   │   ├── SpawnerProfileSO.cs # 发射器配置
│   │   ├── DifficultyProfileSO.cs # 难度乘数
│   │   ├── DanmakuWorldConfig.cs  # 世界配置
│   │   ├── DanmakuRenderConfig.cs # 渲染配置
│   │   ├── DanmakuTypeRegistry.cs # 类型注册表
│   │   └── DanmakuTimeScaleSO.cs  # 时间缩放
│   └── Core/                  # 系统逻辑
│       ├── BulletMover.cs     # 弹丸运动
│       ├── BulletSpawner.cs   # 弹丸发射
│       ├── CollisionSolver.cs # 7 阶段碰撞检测
│       ├── BulletRenderer.cs  # Mesh 渲染
│       ├── PatternScheduler.cs # 弹幕调度
│       ├── LaserUpdater.cs    # 激光更新（含 FreeLaser 统一回收）
│       ├── LaserSegmentSolver.cs # 激光折射段解算（射线 vs AABB/屏幕边缘）
│       ├── SprayUpdater.cs    # 喷雾更新（含 FreeSpray 统一回收）
│       ├── DamageNumberSystem.cs # 伤害飘字
│       └── TrailPool.cs       # 拖尾曲线池
└── Shaders/
    ├── DanmakuBullet.shader         # Alpha Blend
    ├── DanmakuBulletAdditive.shader # Additive
    └── DanmakuLaser.shader          # 激光
```

## 架构要点

- **SoA 布局**: BulletCore(热) + BulletTrail(冷) + BulletModifier(修饰) 三层分离
- **预分配池**: 所有容器启动时预分配，运行时零 new
- **双 Mesh 渲染**: Normal + Additive 各一个 Mesh，每帧单次 SetVertexBufferData
- **7 阶段碰撞**: 弹丸→目标 / 弹丸→障碍物 / 弹丸→屏幕边缘 / 激光→玩家 / 喷雾→玩家 / 激光→障碍物(折射/穿透) / 喷雾→屏幕边缘
- **碰撞响应**: Die / ReduceHP / Pierce / BounceBack / Reflect / RecycleOnDistance
- **激光折射**: LaserSegmentSolver 解算反射/穿透路径，MAX_ITERATIONS=32 安全网
- **挂载跟踪**: AttachSourceRegistry（24 槽，引用计数+空闲栈，注册即持有 refCount=1）
- **PatternScheduler**: 64 槽调度器，驱动 Burst 连射 + PatternGroup 组合
- **DontDestroyOnLoad**: 关卡切换 ClearAll() 清场而非销毁

## 快速接入

```csharp
// 1. 设置玩家
DanmakuSystem.Instance.SetPlayer(playerTransform, 0.2f);

// 2. 发射弹幕
DanmakuSystem.Instance.FireBullets(patternSO, spawnPosition, angleDeg);

// 3. 发射弹幕组合
DanmakuSystem.Instance.FireGroup(groupSO, spawnPosition, angleDeg);

// 4. 发射激光（Detached 固定）
DanmakuSystem.Instance.FireLaser(typeIndex, origin, angle, length: 10f);

// 5. 发射激光（Attached 跟随 Transform）
DanmakuSystem.Instance.FireLaser(typeIndex, bossGunTransform, length: 10f, lifetime: 5f);

// 6. 发射喷雾（Detached 固定）
DanmakuSystem.Instance.FireSpray(typeIndex, origin, direction, coneAngle, range, lifetime: 3f);

// 7. 发射喷雾（Attached 跟随 Transform）
DanmakuSystem.Instance.FireSpray(typeIndex, source, coneAngle, range, lifetime: 3f);

// 8. 清场
DanmakuSystem.Instance.ClearAll();
```

## 依赖模块

- **EventSystem**: GameEvent / IntGameEvent（玩家命中/伤害事件）
- **ObjectPool**: PoolManager / PoolDefinition（重特效预制件）
- **AudioSystem**: AudioClipSO（音效配置）

## 性能预算

| 子系统 | 预算 | 说明 |
|--------|------|------|
| BulletMover | ≤ 1.5ms | 2048 弹丸运动 |
| CollisionSolver | ≤ 1.5ms | 5 阶段碰撞 |
| BulletRenderer | ≤ 1.5ms | 双 Mesh 上传 |
| PatternScheduler | ≤ 0.2ms | 64 任务遍历 |
| LaserUpdater + SprayUpdater | ≤ 0.5ms | 24 实体 |
| DamageNumberSystem | ≤ 0.3ms | 128 飘字 |
| TrailPool | ≤ 0.2ms | 64 条拖尾 |
| **总计** | **≤ 5.7ms** | 60fps 帧预算 34% |

## WebGL 约束

- 无 Physics2D、无 Compute Shader
- 单线程，不可使用 Jobs/Burst
- Gamma 色彩空间
- Draw Call 预算：7-11 DC（弹丸2 + 激光1 + 喷雾1 + 飘字1 + 拖尾1 + UI若干）
