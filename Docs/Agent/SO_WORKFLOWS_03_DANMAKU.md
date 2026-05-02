---
system: danmaku
scope: so-danmaku-configs
last_verified: 2026-05-02
related_code: Assets/_Framework/DanmakuSystem/Scripts/Config/*.cs
---

# SO 配置流程 — 03 弹幕系统

> 12 个 SO 类型。创建弹幕的典型路径：`BulletTypeSO → BulletPatternSO → PatternGroupSO → SpawnerProfileSO`。

## BulletTypeSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Bullet Type`
**命名空间**：`MiniGameTemplate.Danmaku`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/BulletTypeSO.cs`

### 字段清单（分组）

**资源描述**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SourceTexture` | `Texture2D` | null | 弹丸贴图 |
| `UVRect` | `Rect` | (0,0,1,1) | 静态 UV 区域 |
| `SamplingMode` | enum | `Static` | Static / SpriteSheet |
| `SheetColumns/Rows/TotalFrames` | `int` | 1/1/1 | 序列帧配置 |
| `PlaybackMode` | enum | `StretchToLifetime` | StretchToLifetime / FixedFps |
| `FixedFps` | `float` | 12 | 固定帧率 |
| `AtlasBinding` | `AtlasMappingSO` | null | Atlas 优化绑定 |

**视觉**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Tint` | `Color` | white | 颜色叠加 |
| `Size` | `float` | 0.5 | 世界单位尺寸 |
| `RotateToDirection` | `bool` | false | 朝飞行方向旋转 |
| `UseVisualAnimation` | `bool` | false | 启用缩放/透明度曲线 |
| `ScaleOverLifetime` | `AnimationCurve` | — | 缩放曲线 |
| `AlphaOverLifetime` | `AnimationCurve` | — | 透明度曲线 |
| `ColorOverLifetime` | `Gradient` | — | 颜色渐变 |

**运动**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MotionType` | enum | `Default` | Default / SineWave / Spiral |
| `SineAmplitude` | `float` | 0.5 | 正弦振幅 |
| `SineFrequency` | `float` | 2 | 正弦频率 |
| `SpiralAngularVelocity` | `float` | 180 | 螺旋角速度 |
| `SpeedOverLifetime` | `AnimationCurve` | — | 速度曲线 |

**碰撞**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CollisionRadius` | `float` | 0.1 | 碰撞半径 |
| `Damage` | `int` | 10 | 基础伤害 |
| `InitialHitPoints` | `byte` | 1 | 生命值（1~255） |
| `Faction` | `EnumCamp` | Enemy | 阵营 |
| `OnHitTarget/Obstacle/ScreenEdge` | `CollisionResponse` | — | 三路碰撞响应 |

**拖尾**

| 字段 | 类型 | 说明 |
|------|------|------|
| `Trail` | `TrailMode` | None(0)/Ghost(1)/Trail(2)/Both(3) |
| `GhostCount` | `int` | 残影数 |
| `GhostInterval` | `int` | 采样间隔帧 (1~15) |
| `TrailPointCount/Width/WidthCurve/Color` | — | Trail 模式参数 |

**爆炸 & 子弹幕**

| 字段 | 类型 | 说明 |
|------|------|------|
| `Explosion` | enum | MeshFrame / PooledPrefab |
| `ExplosionFrameCount/AtlasUV/HeavyExplosionPrefab` | — | 爆炸效果配置 |
| `ChildPattern` | `BulletPatternSO` | 消亡触发子弹幕 |

---

## BulletPatternSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Bullet Pattern`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/BulletPatternSO.cs`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `BulletType` | `BulletTypeSO` | null | 弹丸类型 |
| `Count` | `int` | 12 | 单次数量 |
| `SpreadAngle` | `float` | 360 | 散布角（360=全方位） |
| `StartAngle` | `float` | 0 | 起始角偏移 |
| `AnglePerShot` | `float` | 0 | 每次发射角度递增 |
| `Speed` | `float` | 5 | 弹速 [0.1,20] |
| `SpeedOverLifetime` | `AnimationCurve` | 1.0 | 速度倍率曲线 |
| `Lifetime` | `float` | 5 | 存活时间（秒） |
| `DelayBeforeAccel` | `float` | 0 | 延迟变速等待时长 |
| `DelaySpeedScale` | `float` | 0 | 等待期速度倍率 [0,1] |
| `AccelDuration` | `float` | 0.3 | 加速持续时间 |
| `IsHoming` | `bool` | false | 是否追踪 |
| `HomingStrength` | `float` | 2 | 追踪转向速度 |
| `HomingDelay` | `float` | 0 | 追踪激活延迟（秒） |
| `BurstCount` | `int` | 1 | 连射次数 |
| `BurstInterval` | `float` | 0.05 | 连射间隔 |
| `FireSFX` | `AudioClipSO` | null | 发射音效 |

---

## PatternGroupSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Pattern Group`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/PatternGroupSO.cs`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Entries` | `PatternEntry[]` | `[]` | 弹幕编排条目 |
| `RepeatCount` | `int` | 1 | 整组重复次数 |
| `RepeatInterval` | `float` | 0.5 | 轮间间隔（秒） |
| `AngleIncrementPerRepeat` | `float` | 0 | 每轮角度偏移 |

### PatternEntry

| 字段 | 类型 | 说明 |
|------|------|------|
| `Pattern` | `BulletPatternSO` | 弹幕模式 |
| `Delay` | `float` | 组内延迟（秒） |
| `AngleOverride` | `float` | 覆盖起始角（-1=不覆盖） |
| `AimAtPlayer` | `bool` | 指向玩家位置 |

---

## SpawnerProfileSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Spawner Profile`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/SpawnerProfileSO.cs`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `PatternGroups` | `PatternGroupSO[]` | `[]` | 弹幕组列表 |
| `CooldownBetweenGroups` | `float` | 2.0 | 组间冷却（秒） |
| `SwitchMode` | `SpawnerSwitchMode` | Sequential | Sequential/Random/External |

---

## DifficultyProfileSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Difficulty Profile`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/DifficultyProfileSO.cs`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SpeedMultiplier` | `float` | 1.0 | 弹速乘数 |
| `CountMultiplier` | `float` | 1.0 | 数量乘数（四舍五入） |
| `LifetimeMultiplier` | `float` | 1.0 | 存活时间乘数 |
| `PatternOverrides` | `PatternOverride[]` | `[]` | 难度替换条目 |

### PatternOverride

| 字段 | 类型 | 说明 |
|------|------|------|
| `Original` | `PatternGroupSO` | 原始弹幕组 |
| `Replacement` | `PatternGroupSO` | 替换目标 |

---

## LaserTypeSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Laser Type`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/LaserTypeSO.cs`

### 关键字段

| 字段 | 类型 | 说明 |
|------|------|------|
| 宽度曲线、阶段时长、伤害 | — | 激光形态 |
| `OnHitObstacle` | `LaserObstacleResponse` | Block/Pierce/BlockAndDamage/PierceAndDamage |
| `OnScreenEdge` | `LaserScreenEdgeResponse` | Clip/Reflect |
| `MaxReflections` | `int` | 最大折射次数 |

---

## SprayTypeSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Spray Type`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/SprayTypeSO.cs`

### 关键字段

| 字段 | 类型 | 说明 |
|------|------|------|
| 锥角、射程、伤害 | — | 喷雾形态 |
| `OnHitObstacle` | `SprayObstacleResponse` | Ignore/ReduceRange |

---

## ObstacleTypeSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Obstacle Type`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/ObstacleTypeSO.cs`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Size` | `Vector2` | (1,1) | AABB 尺寸 |
| `HitPoints` | `int` | 0 | 生命值（0=不可摧毁） |
| `Faction` | `EnumCamp` | Enemy | 阵营（己方弹穿透） |
| `DestroyEffect` | `PoolDefinition` | null | 摧毁特效 |
| `Visual` | `Sprite` | null | 渲染精灵 |

---

## DanmakuWorldConfig

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Config/World`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/DanmakuWorldConfig.cs`
**实例数量**：项目唯一（1 个）

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `MaxBullets` | `int` | 2048 | 弹丸容量 |
| `MaxLasers` | `int` | 16 | 激光容量 |
| `MaxSprays` | `int` | 8 | 喷雾容量 |
| `MaxTrails` | `int` | 64 | 拖尾容量 |
| `WorldBounds` | `Rect` | (-6,-10,12,20) | 弹幕活动边界 |
| `CollisionEventBufferCapacity` | `int` | 256 | 碰撞事件缓冲 |
| `InvincibleDuration` | `float` | 0 | 受击无敌时长（秒） |

---

## DanmakuRenderConfig

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Config/Render`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/DanmakuRenderConfig.cs`
**实例数量**：项目唯一（1 个）

### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `BulletMaterial` | `Material` | 弹丸 Alpha Blend 材质 |
| `LaserMaterial` | `Material` | 激光材质 |
| `BulletAtlas` | `Texture2D` | 弹丸图集（Fallback） |
| `NumberAtlas` | `Texture2D` | 数字精灵图集（飘字） |
| `RuntimeAtlasConfig` | `RuntimeAtlasConfig` | 运行时图集（空=旧路径） |

---

## DanmakuTimeScaleSO

**菜单路径**：`Create → MiniGameTemplate/Danmaku/Time Scale`
**源码**：`Assets/_Framework/DanmakuSystem/Scripts/Config/DanmakuTimeScaleSO.cs`

### 字段清单

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TimeScale` | `float` | 1.0 | 时间倍率 [0,2]，独立于 Time.timeScale |

### API

| 方法 | 说明 |
|------|------|
| `DeltaTime` | `Time.deltaTime * TimeScale` |
| `SetSlowMotion(scale)` | 设置慢动作 |
| `ResetSpeed()` | 恢复 1.0 |
