# ScriptableObject 类型清单

> 最后更新：2026-04-20 | 所有模板内置的 ScriptableObject 类型索引

所有 SO 均通过 `[CreateAssetMenu]` 在 Inspector 中创建。本清单同时服务于人类开发者和 Agent——Agent 可按"菜单路径"快速定位类型。

---

## Variables（数据变量）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `FloatVariable` | MiniGameTemplate/Variables/Float | `MiniGameTemplate.Data` | float 运行时值，含变更事件 |
| `IntVariable` | MiniGameTemplate/Variables/Int | `MiniGameTemplate.Data` | int 运行时值，含变更事件 |
| `StringVariable` | MiniGameTemplate/Variables/String | `MiniGameTemplate.Data` | string 运行时值，含变更事件 |
| `BoolVariable` | MiniGameTemplate/Variables/Bool | `MiniGameTemplate.Data` | bool 运行时值，含变更事件 |

**共同 API**：
- `Value` — 读写值（set 自动触发事件）
- `OnValueChanged` — C# event，值变更时触发
- `ResetToInitial()` — 重置为 Inspector 中设定的初始值
- `[ContextMenu] Reset to Initial Value` — 编辑器右键重置

## Events（事件通道）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `GameEvent` | MiniGameTemplate/Events/Game Event | `MiniGameTemplate.Events` | 无参事件 |
| `IntGameEvent` | MiniGameTemplate/Events/Int Event | `MiniGameTemplate.Events` | int 参数事件 |
| `FloatGameEvent` | MiniGameTemplate/Events/Float Event | `MiniGameTemplate.Events` | float 参数事件 |
| `StringGameEvent` | MiniGameTemplate/Events/String Event | `MiniGameTemplate.Events` | string 参数事件 |

**API**：
- `Raise()` / `Raise(T value)` — 触发事件
- 配合 `GameEventListener` / `GameEventListener<T>` 组件在 Inspector 中配置响应

## Runtime Sets（运行时集合）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `TransformRuntimeSet` | MiniGameTemplate/Runtime Sets/Transform Set | `MiniGameTemplate.Data` | 跟踪场景中活跃的 Transform |

**API**：
- `Items` — 只读列表
- `GetFirst()` — 获取第一个元素（替代 FindObjectOfType）
- 配合 `RuntimeSetRegistrar` 组件自动注册/注销

## Core（核心配置）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `SceneDefinition` | MiniGameTemplate/Core/Scene Definition | `MiniGameTemplate.Lifecycle` | 场景定义（消除硬编码场景名） |
| `GameConfig` | MiniGameTemplate/Core/Game Config | `MiniGameTemplate.Lifecycle` | 全局游戏配置 |
| `AssetConfig` | MiniGameTemplate/Core/Asset Config | `MiniGameTemplate.Asset` | YooAsset 资源管理配置（包名、运行模式、CDN 地址） |

## Audio（音频）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `AudioClipSO` | MiniGameTemplate/Audio/Audio Clip | `MiniGameTemplate.Audio` | 单个音效配置 |
| `AudioLibrary` | MiniGameTemplate/Audio/Audio Library | `MiniGameTemplate.Audio` | 音效库（按 key 索引） |

## Pool（对象池）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `PoolDefinition` | MiniGameTemplate/Pool/Pool Definition | `MiniGameTemplate.Pool` | 池配置（预制件 + 大小） |

## FSM（状态机）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `State` | MiniGameTemplate/FSM/State | `MiniGameTemplate.FSM` | 状态定义，可配置 OnEnter/OnExit 事件 |
| `StateTransition` | MiniGameTemplate/FSM/State Transition | `MiniGameTemplate.FSM` | 状态转换规则 |

## Rendering（渲染基础设施）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `AtlasMappingSO` | MiniGameTemplate/Rendering/Atlas Mapping | `MiniGameTemplate.Rendering` | Atlas 映射——记录打包后图集与源贴图的 UV 对应关系 |
| `RuntimeAtlasConfig` | MiniGameTemplate/Rendering/Runtime Atlas Config | `MiniGameTemplate.Rendering` | RuntimeAtlas 全局配置——6 通道独立参数 |

**AtlasMappingSO 字段**：
- `AtlasTexture` — 打包生成的图集贴图
- `Padding` — 像素间距
- `Entries[]` — 子图映射条目（`AtlasEntry`：SourceTexture + SourceGUID + UVRect + PixelRect）
- `TryFindEntry(source, out entry)` — 按引用或 GUID 查找映射
- `GetUVRectForSource(source)` — 快速获取 UV

**RuntimeAtlasConfig 通道配置**（每通道各一个 `AtlasChannelConfig`）：
- `Bullet` — 弹丸通道（Default）
- `VFX` — 特效通道（Default）
- `DamageText` — 飘字通道（Small）
- `Laser` — 激光通道（Small）
- `Trail` — 拖尾通道（Small）
- `Character` — 角色通道（Default）
- `GetChannelConfig(channel)` — 按枚举获取对应通道配置
- `Validate()` — 校验所有通道参数合法

## Danmaku（弹幕系统）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `BulletTypeSO` | MiniGameTemplate/Danmaku/Bullet Type | `MiniGameTemplate.Danmaku` | 弹丸完整配置（见下方详情） |
| `LaserTypeSO` | MiniGameTemplate/Danmaku/Laser Type | `MiniGameTemplate.Danmaku` | 激光类型（宽度曲线、阶段时长、伤害、折射配置） |
| `SprayTypeSO` | MiniGameTemplate/Danmaku/Spray Type | `MiniGameTemplate.Danmaku` | 喷雾类型（锥角、射程、伤害、碰撞响应） |
| `ObstacleTypeSO` | MiniGameTemplate/Danmaku/Obstacle Type | `MiniGameTemplate.Danmaku` | 障碍物类型 |
| `BulletPatternSO` | MiniGameTemplate/Danmaku/Bullet Pattern | `MiniGameTemplate.Danmaku` | 弹幕发射模式（数量、散布角、速度、延迟变速、追踪） |
| `PatternGroupSO` | MiniGameTemplate/Danmaku/Pattern Group | `MiniGameTemplate.Danmaku` | 弹幕组合编排（多层/延迟/重复/旋转） |
| `SpawnerProfileSO` | MiniGameTemplate/Danmaku/Spawner Profile | `MiniGameTemplate.Danmaku` | 发射器配置（Boss/敌人用） |
| `DifficultyProfileSO` | MiniGameTemplate/Danmaku/Difficulty Profile | `MiniGameTemplate.Danmaku` | 难度乘数（数量/速度/生命周期） |
| `DanmakuWorldConfig` | MiniGameTemplate/Danmaku/World Config | `MiniGameTemplate.Danmaku` | 世界配置（容量、边界、碰撞网格、无敌帧） |
| `DanmakuRenderConfig` | MiniGameTemplate/Danmaku/Config/Render | `MiniGameTemplate.Danmaku` | 渲染配置（材质、贴图、RuntimeAtlas） |
| `DanmakuTypeRegistry` | MiniGameTemplate/Danmaku/Type Registry | `MiniGameTemplate.Danmaku` | 类型注册表（集中管理所有类型 SO） |
| `DanmakuTimeScaleSO` | MiniGameTemplate/Danmaku/Time Scale | `MiniGameTemplate.Danmaku` | 时间缩放（子弹时间） |

### BulletTypeSO 字段详情

**资源描述**：
- `SourceTexture` — 源贴图引用（每个弹丸类型可有独立贴图）
- `UVRect` — 静态弹丸的 UV 区域（归一化到 SourceTexture）
- `SamplingMode` — `Static`（静态 UV） / `SpriteSheet`（序列帧）
- `SheetColumns/SheetRows/SheetTotalFrames` — 序列帧配置
- `PlaybackMode` — `StretchToLifetime` / `FixedFps`
- `FixedFps` — 固定帧率（PlaybackMode=FixedFps 时有效）
- `AtlasBinding` — 可选 `AtlasMappingSO` 引用（Atlas 优化模式）

**视觉**：
- `Tint` — 颜色叠加
- `Size` — 弹丸尺寸（世界单位）
- `RotateToDirection` — 是否朝飞行方向旋转

**运动**：
- `MotionType` — `Default` / `SineWave` / `Spiral`
- `SineAmplitude` / `SineFrequency` — 正弦波参数（MotionType=SineWave 时有效）
- `SpiralAngularVelocity` — 螺旋角速度（MotionType=Spiral 时有效）
- `SpeedOverLifetime` — 速度曲线（AnimationCurve）

**视觉动画**（DEC-005=C）：
- `UseVisualAnimation` — 开启后每帧采样动画曲线
- `ScaleOverLifetime` / `AlphaOverLifetime` — 缩放/透明度曲线
- `ColorOverLifetime` — 颜色渐变

**碰撞**：
- `CollisionRadius` — 碰撞半径
- `Damage` — 基础伤害
- `InitialHitPoints` — 初始生命值（1~255）
- `Faction` — 阵营（`Enemy` / `Player`）

**碰撞响应**（三路独立配置）：
- `OnHitTarget` / `OnHitObstacle` / `OnHitScreenEdge` — `CollisionResponse` 枚举
- 各自的 `HPCost` 和 `ScreenEdgeRecycleDistance`

**碰撞反馈**：
- `BounceEffect` / `BounceSFX` / `PierceSFX` / `DamageFlashTint` / `DamageFlashFrames`

**拖尾**：
- `Trail` — `TrailMode` 枚举：`None(0)` / `Ghost(1)` / `Trail(2)` / `Both(3)`
- Ghost 模式：`GhostCount`（残影数）、`GhostInterval`（采样间隔帧数，1~15）
- Trail 模式：`TrailPointCount`、`TrailWidth`、`TrailWidthCurve`（AnimationCurve）、`TrailColor`（Gradient）

**爆炸**：
- `Explosion` — `MeshFrame` / `PooledPrefab`
- `ExplosionFrameCount` / `ExplosionAtlasUV` / `HeavyExplosionPrefab`

**子弹幕**：
- `ChildPattern` — 消亡时触发的 `BulletPatternSO`

**运行时辅助方法**：
- `GetResolvedTexture()` — 解析实际贴图（AtlasBinding > SourceTexture）
- `GetResolvedBaseUV()` — 解析基础 UV（Atlas 映射 > UVRect）
- `GetFrameUV(frameIndex, baseUV)` — 序列帧帧 UV 计算

### DanmakuRenderConfig 字段详情

- `BulletMaterial` — 弹丸 Alpha Blend 材质
- `LaserMaterial` — 激光材质
- `BulletAtlas` — 弹丸图集（fallback 全局 Atlas）
- `NumberAtlas` — 数字精灵图集（飘字用）
- `RuntimeAtlasConfig` — RuntimeAtlas 配置引用（空=旧渲染路径）

### LaserTypeSO 碰撞响应配置

- `OnHitObstacle` — `LaserObstacleResponse` 枚举：`Block` / `Pierce` / `BlockAndDamage` / `PierceAndDamage`
- `OnScreenEdge` — `LaserScreenEdgeResponse` 枚举：`Clip` / `Reflect`
- `MaxReflections` — 最大折射次数

### SprayTypeSO 碰撞响应配置

- `OnHitObstacle` — `SprayObstacleResponse` 枚举：`Ignore` / `ReduceRange`

## VFX（特效系统）

| 类 | 菜单路径 | 命名空间 | 用途 |
|----|---------|----------|------|
| `VFXTypeSO` | MiniGameTemplate/VFX/VFX Type | `MiniGameTemplate.VFX` | Sprite Sheet 特效类型完整配置 |
| ~~`VFXTypeRegistrySO`~~ | _(ADR-030：已降级为 `internal class VFXTypeRegistry`，不再是 SO)_ | `MiniGameTemplate.VFX` | 运行时 VFX 类型注册表（懒注册） |
| `VFXRenderConfig` | MiniGameTemplate/VFX/Render Config | `MiniGameTemplate.VFX` | VFX 渲染配置（材质、贴图、RuntimeAtlas） |

### VFXTypeSO 字段详情

**资源描述**：
- `SourceTexture` — 源贴图引用
- `UVRect` — 在 SourceTexture 上的 UV 区域
- `AtlasBinding` — 可选 `AtlasMappingSO` 引用

**Sprite Sheet**：
- `Columns` / `Rows` / `TotalFrames` / `FramesPerSecond`

**播放**：
- `Loop` — 是否循环（关闭=播完回收）
- `RotateWithInstance` — 是否随实例旋转

**渲染**：
- `Size` — 世界单位尺寸
- `Tint` — 默认颜色

**附着模式**（ADR-013）：
- `AttachMode` — `VFXAttachMode`：`World`（世界空间固定） / `FollowTarget`（跟随附着源）

**运行时辅助方法**：
- `MaxFrameCount` — 有效帧数
- `Duration` — 播放时长
- `GetResolvedTexture()` / `GetResolvedBaseUV()` / `GetFrameUV()`

### VFXRenderConfig 字段详情

- `NormalMaterial` — 基础混合材质
- `AtlasTexture` — 共用图集（fallback）
- `RuntimeAtlasConfig` — RuntimeAtlas 配置引用（空=旧渲染路径）
