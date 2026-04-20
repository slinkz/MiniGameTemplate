# DanmakuSystem（弹幕系统）

## 概述

纯数据驱动弹幕系统，专为微信小游戏（WebGL）优化。支持弹丸/激光/喷雾三种武器类型 + 障碍物交互，零 GC 分配，2048 弹丸 ≤ 5.7ms/帧。

## 目录结构

```
DanmakuSystem/
├── MODULE_README.md          # 本文件
├── Scripts/
│   ├── DanmakuSystem.cs            # Facade MonoBehaviour 入口（partial class 主文件：单例/Awake/Update/LateUpdate）
│   ├── DanmakuSystem.Runtime.cs    # [Phase 2] 子系统引用持有、InitializeSubsystems/DisposeSubsystems
│   ├── DanmakuSystem.API.cs        # [Phase 2] 公开 API：Fire/Register/Clear/ClearAllBulletsWithEffect
│   ├── DanmakuSystem.UpdatePipeline.cs # [Phase 2] 逐帧驱动管线（9步 Update + LateUpdate 渲染）
│   ├── Data/                  # 纯数据结构 + 容器
│   │   ├── BulletCore.cs      # 弹丸热数据 (36B)
│   │   ├── BulletTrail.cs     # 弹丸冷数据 (28B)
│   │   ├── BulletModifier.cs  # 修饰数据 (20B)
│   │   ├── DanmakuEnums.cs    # 所有枚举（含 MotionType, CollisionEventType, BulletSamplingMode, BulletPlaybackMode 等）
│   │   ├── CircleHitbox.cs    # 圆形碰撞体
│   │   ├── ICollisionTarget.cs # 碰撞目标接口（玩家/Boss/敌人等）
│   │   ├── TargetRegistry.cs  # 碰撞目标注册表 (16 槽)
│   │   ├── CollisionEventBuffer.cs # [Phase 2] 零 GC 碰撞事件旁路缓冲（CollisionEvent struct + CollisionEventBuffer class）
│   │   ├── (已迁移)           # 渲染顶点已迁移至 _Framework/Rendering/RenderVertex.cs
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
│   │   ├── BulletTypeSO.cs    # 弹丸类型（含 MotionType, SourceTexture, SamplingMode, SchemaVersion）
│   │   ├── LaserTypeSO.cs     # 激光类型（含折射/穿透响应）
│   │   ├── SprayTypeSO.cs     # 喷雾类型（含碰撞响应）
│   │   ├── ObstacleTypeSO.cs  # 障碍物类型
│   │   ├── BulletPatternSO.cs # 弹幕发射模式
│   │   ├── PatternGroupSO.cs  # 弹幕组合编排
│   │   ├── SpawnerProfileSO.cs # 发射器配置
│   │   ├── DifficultyProfileSO.cs # 难度乘数
│   │   ├── DanmakuWorldConfig.cs  # 世界配置（含 CollisionEventBufferCapacity = 256）
│   │   ├── DanmakuRenderConfig.cs # 渲染配置
│   │   ├── DanmakuTypeRegistry.cs # 类型注册表
│   │   └── DanmakuTimeScaleSO.cs  # 时间缩放
│   └── Core/                  # 系统逻辑
│       ├── BulletMover.cs     # 弹丸运动（通过 MotionRegistry 策略委托驱动，不含运动分支逻辑）
│       ├── BulletSpawner.cs   # 弹丸发射
│       ├── CollisionSolver.cs # 7 阶段碰撞检测（含 CollisionEventBuffer 旁路写入）
│       ├── MotionRegistry.cs  # [Phase 2] 运动策略受控注册表（MotionStrategy 委托 + 枚举索引）
│       ├── DefaultMotionStrategy.cs  # [Phase 2] 默认运动策略（延迟变速 + 速度曲线 + 追踪）
│       ├── SineWaveMotionStrategy.cs # [Phase 2] 正弦波运动策略
│       ├── SpiralMotionStrategy.cs   # [Phase 2] 螺旋运动策略
│       ├── IDanmakuEffectsBridge.cs  # [Phase 2] 特效桥接接口（解耦 Danmaku ↔ VFX）
│       ├── DefaultDanmakuEffectsBridge.cs # [Phase 2] 默认桥接实现（消费事件 → 触发 VFX）
│       ├── BulletRenderer.cs  # 弹丸 Mesh 渲染（BatchManager 分桶）
│       ├── LaserRenderer.cs   # 激光 Mesh 渲染（Quad 条带 + WidthProfile + BatchManager）
│       ├── LaserWarningRenderer.cs # 激光预警线渲染（RBM 独立贴图）
│       ├── PatternScheduler.cs # 弹幕调度（含调试统计）
│       ├── SpawnerDriver.cs   # 发射器驱动（驱动 SpawnerProfileSO 自动发射）
│       ├── LaserUpdater.cs    # 激光更新（含 FreeLaser 统一回收）
│       ├── LaserSegmentSolver.cs # 激光折射段解算（射线 vs AABB/屏幕边缘）
│       ├── SprayUpdater.cs    # 喷雾更新（含 FreeSpray 统一回收）
│       ├── MotionUtility.cs   # [2026-04-20] 运动策略共享工具（CalculateModifierSpeed 去重）
│       ├── IDanmakuVFXRuntime.cs  # [R4.0] VFX 管线驱动接口（TickVFX/RenderVFX/Play/PlayAttached/StopAttached）
│       ├── DanmakuVFXRuntimeBridge.cs # [R4.0] 桥接实现（转发到 SpriteSheetVFXSystem）
│       ├── DanmakuAttachSourceResolver.cs # IVFXPositionResolver 实现（将 VFX attachId 解析为 DanmakuSystem 世界坐标）
│       ├── DanmakuEffectsBridgeConfig.cs # 碰撞特效桥接配置组件（序列化 VFX 类型引用 + 音效引用）
│       ├── DamageNumberSystem.cs # 伤害飘字（R3 迁移到 RBM + RuntimeAtlas）
│       └── TrailPool.cs       # 拖尾曲线池（方案 A：独立 Mesh + 接入统计）
└── Shaders/
    ├── DanmakuBullet.shader         # Alpha Blend（子弹/VFX/飘字通用）
    └── DanmakuLaser.shader          # 激光（Additive Blend，硬编码 CoreColor/GlowColor）
```

## 架构要点

### 核心数据架构
- **SoA 布局**: BulletCore(热) + BulletTrail(冷) + BulletModifier(修饰) 三层分离
- **预分配池**: 所有容器启动时预分配，运行时零 new
- **统一渲染管线**: 所有 Renderer 通过 `RenderBatchManager`（共享 `_Framework/Rendering/`）提交，按 `BucketRegistration(BucketKey, templateMat, sortingOrder)` 分桶，`material.renderQueue` 控制 GPU 级层序
- **RuntimeAtlas 纹理管理**: 子弹/VFX/飘字优先通过 `RuntimeAtlasBindingResolver` 走 RuntimeAtlas 动态图集，激光保持独立贴图
- **Debug HUD Atlas 可观测性**: `DanmakuDebugHUD` 通过 `DanmakuSystem.GetAllAtlasStats()` 聚合读取 Bullet/VFX/DamageNumber 的 RuntimeAtlas 统计；HUD 在 `Start()` 首帧主动刷新缓存，且仅在存在有效 stats 时显示 RuntimeAtlas section，避免首屏空窗与高度计算分叉
- **激光渲染**: LaserRenderer 使用统一 RBM，WidthProfile 曲线驱动宽度，Phase alpha 闪烁/渐隐

### Facade 拆分 [Phase 2]

`DanmakuSystem` 保留为单一 `MonoBehaviour` Facade 入口（DontDestroyOnLoad 单例），内部通过 `partial class` 拆分为 4 个职责文件：

| 文件 | 行数 | 职责 |
|------|------|------|
| `DanmakuSystem.cs` | 112 | Facade 主文件：单例、Awake/Update/LateUpdate、序列化字段、`PlayerCollisionTarget` 内部适配器 |
| `DanmakuSystem.Runtime.cs` | 94 | 子系统引用持有、`InitializeSubsystems()` / `DisposeSubsystems()`、`MotionRegistry.Initialize()` |
| `DanmakuSystem.API.cs` | 261 | 公开 API：SetPlayer / RegisterTarget / Fire* / ClearAll / `ClearAllBulletsWithEffect` |
| `DanmakuSystem.UpdatePipeline.cs` | 78 | 逐帧管线：9步 Update（SpawnerDriver→Scheduler→BulletMover→LaserUpdater→SprayUpdater→CollisionSolver→PlayerHit→EffectsBridge→BufferReset）+ LateUpdate 渲染 |

### 碰撞系统
- **多目标碰撞**: 通过 ICollisionTarget + TargetRegistry（16 槽）支持任意数量碰撞目标，自动阵营过滤
- **7 阶段碰撞**: 弹丸→目标 / 弹丸→障碍物 / 弹丸→屏幕边缘 / 激光→目标 / 喷雾→目标 / 喷雾→障碍物 / 喷雾→屏幕边缘
- **碰撞响应**: Die / ReduceHP / Pierce / BounceBack / Reflect / RecycleOnDistance
- **碰撞事件 Buffer [Phase 2]**: `CollisionEventBuffer`（预分配 256 条，零 GC）作为旁路表现通道，溢出仅影响 VFX/飘字等表现，不影响伤害/击退/死亡主逻辑
- **激光折射**: LaserSegmentSolver 解算反射/穿透路径，MAX_ITERATIONS=32 安全网

### 运动策略 [Phase 2]

通过 `MotionRegistry` 受控静态注册表 + `MotionStrategy` 委托实现可扩展运动：

| MotionType | 策略类 | 行为 |
|-----------|--------|------|
| `Default` (0) | `DefaultMotionStrategy` | 延迟变速 + 速度曲线 + 追踪 |
| `SineWave` (1) | `SineWaveMotionStrategy` | 垂直于飞行方向叠加正弦偏移 |
| `Spiral` (2) | `SpiralMotionStrategy` | 持续角速度转向 + 速度曲线 |

**扩展方式**：在 `MotionType` 枚举新增值 → 实现新策略类（static Execute 方法）→ 在 `MotionRegistry.Initialize()` 注册。无需修改 `BulletMover` 核心热路径。

### 特效桥接 [Phase 2]

- **`IDanmakuEffectsBridge`** 接口解耦 Danmaku ↔ VFX 命名空间依赖
  - `OnCollisionEventsReady(CollisionEventBuffer)` — 碰撞后、Buffer Reset 前调用
  - `OnBulletCleared(int, Vector2, BulletTypeSO)` — 清屏 API 逐弹丸调用
- **`DefaultDanmakuEffectsBridge`** 是唯一包含 `using MiniGameTemplate.VFX` 的运行时类
- DanmakuSystem Facade 仍持有 `_hitVfxSystem` / `_hitVfxType` 序列化字段（Phase 3 待迁移到桥接组件）

### 其他架构要点
- **挂载跟踪**: AttachSourceRegistry（24 槽，引用计数+空闲栈，注册即持有 refCount=1）
- **PatternScheduler**: 64 槽调度器，驱动 Burst 连射 + PatternGroup 组合（含 PeakTasks/TotalScheduled 统计）
- **SpawnerDriver**: 8 槽发射器驱动器，自动驱动 SpawnerProfileSO 的 Sequential/Random/External 模式
- **DontDestroyOnLoad**: 关卡切换 ClearAll() 清场而非销毁

## 快速接入

```csharp
// 1. 设置玩家（便捷方法，内部自动注册到 TargetRegistry）
DanmakuSystem.Instance.SetPlayer(playerTransform, 0.2f);

// 2. 注册自定义碰撞目标（如 Boss）
DanmakuSystem.Instance.RegisterTarget(myBossCollisionTarget);

// 3. 发射弹幕
DanmakuSystem.Instance.FireBullets(patternSO, spawnPosition, angleDeg);

// 4. 发射弹幕组合
DanmakuSystem.Instance.FireGroup(groupSO, spawnPosition, angleDeg);

// 5. 发射激光（Detached 固定）
DanmakuSystem.Instance.FireLaser(typeIndex, origin, angle, length: 10f);

// 6. 发射激光（Attached 跟随 Transform）
DanmakuSystem.Instance.FireLaser(typeIndex, bossGunTransform, length: 10f, lifetime: 5f);

// 7. 发射喷雾（Detached 固定）
DanmakuSystem.Instance.FireSpray(typeIndex, origin, direction, coneAngle, range, lifetime: 3f);

// 8. 发射喷雾（Attached 跟随 Transform）
DanmakuSystem.Instance.FireSpray(typeIndex, source, coneAngle, range, lifetime: 3f);

// 9. 启动发射器驱动（自动按 SpawnerProfile 发射）
int spawnerId = DanmakuSystem.Instance.SpawnerDriver.Start(
    spawnerProfileSO, () => bossTransform.position);

// 10. 外部控制切换弹幕组（External 模式）
DanmakuSystem.Instance.SpawnerDriver.SetGroupIndex(spawnerId, 2);

// 11. 运行时切换难度
DanmakuSystem.Instance.Difficulty = harderDifficultySO;

// 12. 清场（回收全部弹丸/激光/喷雾/障碍物/挂载源/调度任务）
DanmakuSystem.Instance.ClearAll();

// 13. [Phase 2] 清屏转化（遍历每颗弹丸 → 通知 EffectsBridge 生成特效/得分 → 回收）
DanmakuSystem.Instance.ClearAllBulletsWithEffect();
```

### 使用 BulletTypeSO 的 MotionType

```csharp
// 在 BulletTypeSO Inspector 中选择 MotionType：
// Default  — 普通弹丸（延迟变速/速度曲线/追踪）
// SineWave — 蛇形弹丸（振幅/频率通过 Modifier 空闲字段配置）
// Spiral   — 螺旋弹丸（角速度通过 Modifier 空闲字段配置）
```

### 消费碰撞事件 Buffer

```csharp
// 自定义桥接——实现 IDanmakuEffectsBridge 消费碰撞事件
public class MyEffectsBridge : IDanmakuEffectsBridge
{
    public void OnCollisionEventsReady(CollisionEventBuffer buffer)
    {
        var span = buffer.AsReadOnlySpan();
        for (int i = 0; i < span.Length; i++)
        {
            ref readonly var evt = ref span[i];
            // evt.Position / evt.Damage / evt.EventType / evt.SourceFaction ...
        }
    }

    public void OnBulletCleared(int bulletIndex, Vector2 position, BulletTypeSO type)
    {
        // 清屏特效/得分
    }
}
```

## 依赖模块

- **Rendering**: `_Framework/Rendering/`（RenderVertex / RenderLayer / RenderSortingOrder / RenderBatchManager / RenderBatchManagerRuntimeStats / RuntimeAtlasSystem）
- **VFXSystem**: `_Framework/VFXSystem/`（通过 `IDanmakuVFXRuntime` + `IDanmakuEffectsBridge` 桥接，主命名空间不直接引用 VFX）
- **EventSystem**: GameEvent / IntGameEvent（玩家命中/伤害事件）
- **ObjectPool**: PoolManager / PoolDefinition（重特效预制件）
- **AudioSystem**: AudioClipSO（音效配置）

## 每帧更新管线 [Phase 2 → R4.0 统一]

```
Update() → RunUpdatePipeline()
  1. SpawnerDriver.Tick          — 发射器驱动 Tick
  2. PatternScheduler.Tick       — 调度器执行到期任务
  3. BulletMover.UpdateAll       — 弹丸运动（MotionRegistry 策略委托）
  4. LaserUpdater.UpdateAll      — 激光更新（挂载同步 + 折射段解算）
  5. SprayUpdater.UpdateAll      — 喷雾更新（挂载同步 + VFX 启动）
  6. IDanmakuVFXRuntime.TickVFX  — VFX 逻辑帧推进（R4.0 收编）
  7. CollisionSolver.SolveAll    — 7 阶段碰撞（写入 CollisionEventBuffer）
  8. PlayerHit 事件 + 飘字       — 无敌帧 + GameEvent
  9. EffectsBridge.OnCollisionEventsReady — 桥接层消费事件 Buffer
 10. CollisionEventBuffer.Reset  — 帧末清零

LateUpdate() → RunLateUpdatePipeline()
  RenderBatchManagerRuntimeStats.BeginFrame()
  ├── TrailPool.Render()                  ← 独立 Mesh + Graphics.DrawMesh（renderQueue=3090）
  ├── BulletRenderer.Rebuild + UploadAndDrawAll    → 独立 RBM（RuntimeAtlas 纹理）
  ├── LaserRenderer.Rebuild + UploadAndDrawAll     → 独立 RBM（独立贴图）
  ├── LaserWarningRenderer.Rebuild + UploadAndDrawAll → 独立 RBM（独立贴图）
  ├── IDanmakuVFXRuntime.RenderVFX()      ← R4.0 收编（VFXBatchRenderer → 独立 RBM）
  └── DamageNumberSystem.Rebuild(dt) + UploadAndDrawAll → 独立 RBM（RuntimeAtlas DamageText）
  RenderBatchManagerRuntimeStats.EndFrame()

  注意：每个 Renderer 内部持有独立的 RBM 实例，各自在 Rebuild 末尾
  调用 UploadAndDrawAll()。渲染层序由 material.renderQueue 值控制（GPU 级排序），
  不依赖代码调用顺序。
```

## 性能预算

| 子系统 | 预算 | 说明 |
|--------|------|------|
| BulletMover | ≤ 1.5ms | 2048 弹丸运动 |
| CollisionSolver | ≤ 1.5ms | 7 阶段碰撞 |
| BulletRenderer | ≤ 1.5ms | 分桶 Mesh 上传 |
| PatternScheduler | ≤ 0.2ms | 64 任务遍历 |
| LaserUpdater + SprayUpdater | ≤ 0.5ms | 24 实体 |
| DamageNumberSystem | ≤ 0.3ms | 128 飘字 |
| TrailPool | ≤ 0.2ms | 64 条拖尾 |
| **总计** | **≤ 5.7ms** | 60fps 帧预算 34% |

## WebGL 约束

- 无 Physics2D、无 Compute Shader
- 单线程，不可使用 Jobs/Burst
- Gamma 色彩空间
- Draw Call 预算：按贴图分桶后随贴图数线性增长，基线约 7-15 DC

## 重构进度

| Phase | 目标 | 状态 |
|-------|------|------|
| Phase 0 | 基础设施层（共享渲染、容量配置化） | ✅ 已完成 |
| Phase 1 | 渲染管线重构（多贴图分桶、序列帧子弹） | ✅ 已完成 |
| Phase 2 | 事件与扩展性（碰撞事件 Buffer、运动策略、Facade 拆分、VFX 桥接、清屏 API） | ✅ 已完成 |
| Phase 3 | 视觉增强（Ghost 残影、Trail 拖尾、SineWave/Spiral 运动、DamageNumber） | ✅ 已完成 |
| Phase 4 | 工具与优化（Atlas 打包工具 4.1/4.2、ADR-029 Additive 移除） | ✅ 已完成 |
| **R0** | RuntimeAtlasSystem 基础设施 | ✅ 已完成 |
| **R1** | RuntimeAtlasManager 配置驱动管理层 | ✅ 已完成 |
| **R2** | BulletRenderer / VFXBatchRenderer / Laser 迁移到统一 RBM + RuntimeAtlas | ✅ 已完成 |
| **R3** | DamageNumberSystem / TrailPool 迁移 + 管线统一调度 | ✅ 已完成 |
| **R4** | VFX 编排层统一（SpriteSheetVFXSystem 收编到 DanmakuSystem 管线）+ Detached Spray VFX 回归修复 | ✅ 已完成 |
