# ScriptableObject 类型清单

所有模板内置的 ScriptableObject 类型。通过 `[CreateAssetMenu]` 在 Inspector 中创建。

## Variables（数据变量）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `FloatVariable` | MiniGameTemplate/Variables/Float | float 运行时值，含变更事件 |
| `IntVariable` | MiniGameTemplate/Variables/Int | int 运行时值，含变更事件 |
| `StringVariable` | MiniGameTemplate/Variables/String | string 运行时值，含变更事件 |
| `BoolVariable` | MiniGameTemplate/Variables/Bool | bool 运行时值，含变更事件 |

**共同 API**：
- `Value` — 读写值（set 自动触发事件）
- `OnValueChanged` — C# event，值变更时触发
- `ResetToInitial()` — 重置为 Inspector 中设定的初始值
- `[ContextMenu] Reset to Initial Value` — 编辑器右键重置

## Events（事件通道）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `GameEvent` | MiniGameTemplate/Events/Game Event | 无参事件 |
| `IntGameEvent` | MiniGameTemplate/Events/Int Event | int 参数事件 |
| `FloatGameEvent` | MiniGameTemplate/Events/Float Event | float 参数事件 |
| `StringGameEvent` | MiniGameTemplate/Events/String Event | string 参数事件 |

**API**：
- `Raise()` / `Raise(T value)` — 触发事件
- 配合 `GameEventListener` / `GameEventListener<T>` 组件在 Inspector 中配置响应

## Runtime Sets（运行时集合）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `TransformRuntimeSet` | MiniGameTemplate/Runtime Sets/Transform Set | 跟踪场景中活跃的 Transform |

**API**：
- `Items` — 只读列表
- `GetFirst()` — 获取第一个元素（替代 FindObjectOfType）
- 配合 `RuntimeSetRegistrar` 组件自动注册/注销

## Core（核心配置）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `SceneDefinition` | MiniGameTemplate/Core/Scene Definition | 场景定义（消除硬编码场景名） |
| `GameConfig` | MiniGameTemplate/Core/Game Config | 全局游戏配置 |
| `AssetConfig` | MiniGameTemplate/Core/Asset Config | YooAsset 资源管理配置（包名、运行模式、CDN 地址） |

## Audio（音频）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `AudioClipSO` | MiniGameTemplate/Audio/Audio Clip | 单个音效配置 |
| `AudioLibrary` | MiniGameTemplate/Audio/Audio Library | 音效库（按 key 索引） |

## Pool（对象池）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `PoolDefinition` | MiniGameTemplate/Pool/Pool Definition | 池配置（预制件 + 大小） |

## FSM（状态机）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `State` | MiniGameTemplate/FSM/State | 状态定义，可配置 OnEnter/OnExit 事件 |
| `StateTransition` | MiniGameTemplate/FSM/State Transition | 状态转换规则 |

## Danmaku（弹幕系统）

| 类 | 菜单路径 | 用途 |
|----|---------|------|
| `BulletTypeSO` | MiniGameTemplate/Danmaku/Bullet Type | 弹丸视觉类型（UV、碰撞、伤害、拖尾、爆炸、碰撞响应） |
| `LaserTypeSO` | MiniGameTemplate/Danmaku/Laser Type | 激光类型（宽度曲线、阶段时长、伤害、折射配置） |
| `SprayTypeSO` | MiniGameTemplate/Danmaku/Spray Type | 喷雾类型（锥角、射程、伤害、碰撞响应） |
| `ObstacleTypeSO` | MiniGameTemplate/Danmaku/Obstacle Type | 障碍物类型 |
| `BulletPatternSO` | MiniGameTemplate/Danmaku/Bullet Pattern | 弹幕发射模式（数量、散布角、速度、延迟变速、追踪） |
| `PatternGroupSO` | MiniGameTemplate/Danmaku/Pattern Group | 弹幕组合编排（多层/延迟/重复/旋转） |
| `SpawnerProfileSO` | MiniGameTemplate/Danmaku/Spawner Profile | 发射器配置（Boss/敌人用） |
| `DifficultyProfileSO` | MiniGameTemplate/Danmaku/Difficulty Profile | 难度乘数（数量/速度/生命周期） |
| `DanmakuWorldConfig` | MiniGameTemplate/Danmaku/World Config | 世界配置（容量、边界、碰撞网格、无敌帧） |
| `DanmakuRenderConfig` | MiniGameTemplate/Danmaku/Render Config | 渲染配置（材质、贴图） |
| `DanmakuTypeRegistry` | MiniGameTemplate/Danmaku/Type Registry | 类型注册表（集中管理所有类型 SO） |
| `DanmakuTimeScaleSO` | MiniGameTemplate/Danmaku/Time Scale | 时间缩放（子弹时间） |

**LaserTypeSO 碰撞响应配置**：
- `OnHitObstacle` — `LaserObstacleResponse` 枚举：`Block`（截断+反射）/ `Pierce`（穿透）/ `BlockAndDamage` / `PierceAndDamage`
- `OnScreenEdge` — `LaserScreenEdgeResponse` 枚举：`Clip`（截断）/ `Reflect`（反射）
- `MaxReflections` — 最大折射次数（含障碍物 Block 和屏幕边缘 Reflect）

**SprayTypeSO 碰撞响应配置**：
- `OnHitObstacle` — `SprayObstacleResponse` 枚举：`Ignore` / `ReduceRange`
