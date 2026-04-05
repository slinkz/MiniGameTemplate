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
