---
system: general
scope: so-infrastructure
last_verified: 2026-05-02
related_code: Assets/_Framework/DataSystem/Scripts/Variables/*.cs, Assets/_Framework/EventSystem/Scripts/*.cs, Assets/_Framework/ObjectPool/Scripts/*.cs, Assets/_Framework/FSM/Scripts/*.cs, Assets/_Framework/AudioSystem/Scripts/*.cs
---

# SO 配置流程 — 05 基础设施

> Variables / Events / Pool / FSM / Audio — 8 个 SO 类型。

## Variables（运行时数据变量）

### 统一模式

4 个类型完全同构：`FloatVariable`、`IntVariable`、`StringVariable`、`BoolVariable`。

**命名空间**：`MiniGameTemplate.Data`
**源码目录**：`Assets/_Framework/DataSystem/Scripts/Variables/`
**实例目录**：`Assets/_Game/Configs/Variables/`

| 类 | 菜单路径 | 值类型 |
|----|---------|--------|
| `FloatVariable` | `Create → MiniGameTemplate/Variables/Float` | `float` |
| `IntVariable` | `Create → MiniGameTemplate/Variables/Int` | `int` |
| `StringVariable` | `Create → MiniGameTemplate/Variables/String` | `string` |
| `BoolVariable` | `Create → MiniGameTemplate/Variables/Bool` | `bool` |

### 字段清单（以 IntVariable 为例）

| 字段 | C# 属性 | 类型 | 说明 |
|------|---------|------|------|
| `_initialValue` | — | `int` | Inspector 设定的初始值 |
| `_value` | `Value` | `int` | 当前运行时值 |

### 公共 API

| API | 说明 |
|-----|------|
| `Value` { get; set; } | 读写值（set 触发 OnValueChanged） |
| `OnValueChanged` | `event Action<T>` |
| `SetValue(T)` | 等价于 Value = x |
| `ApplyChange(T)` | 累加（Float/Int 专有） |
| `ResetToInitial()` | 重置为 _initialValue |
| `[ContextMenu] Reset to Initial Value` | 编辑器右键 |

### 设计理念

- `OnEnable()` 自动重置 → 每次进入 Play Mode 值干净
- 解耦组件间通信：A 组件写 Variable，B 组件监听 OnValueChanged
- 替代全局静态变量，Inspector 可调试

### Agent 创建代码

```csharp
var hp = ScriptableObject.CreateInstance<IntVariable>();
var so = new SerializedObject(hp);
so.FindProperty("_initialValue").intValue = 100;
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.CreateAsset(hp, "Assets/_Game/Configs/Variables/PlayerHP.asset");
AssetDatabase.SaveAssets();
```

---

## Events（事件通道）

### GameEvent（无参）

**菜单路径**：`Create → MiniGameTemplate/Events/Game Event`
**命名空间**：`MiniGameTemplate.Events`
**源码**：`Assets/_Framework/EventSystem/Scripts/GameEvent.cs`
**实例目录**：`Assets/_Game/Configs/Events/`

#### 字段

无序列化字段——纯通道。

#### API

| 方法 | 说明 |
|------|------|
| `Raise()` | 触发事件 |
| 配合 `GameEventListener` MonoBehaviour | Inspector 配置响应 |

### 泛型事件

| 类 | 菜单路径 | 参数类型 |
|----|---------|---------|
| `IntGameEvent` | `Create → MiniGameTemplate/Events/Int Event` | `int` |
| `FloatGameEvent` | `Create → MiniGameTemplate/Events/Float Event` | `float` |
| `StringGameEvent` | `Create → MiniGameTemplate/Events/String Event` | `string` |

#### API

| 方法 | 说明 |
|------|------|
| `Raise(T value)` | 触发带参事件 |

### 使用模式

1. 创建 `GameEvent` 资产（如 `OnPlayerDeath`）
2. 触发方：代码中 `onPlayerDeathEvent.Raise()`
3. 响应方：GameObject 挂 `GameEventListener`，Inspector 绑定 UnityEvent

### BattleLifecycleEvent（TDD-07）

**菜单路径**：`Create → ShooterGame/Events/Battle Lifecycle Event`
**命名空间**：`MiniGameTemplate.Battle`
**源码**：`Assets/_Framework/BattleLifecycle/BattleLifecycleEvent.cs`
**实例**：`Assets/_Game/Configs/ShooterGame/Events/SG_OnBattleEnd.asset`

| API | 说明 |
|-----|------|
| `Register(IBattleCleanup)` | 注册退场监听者（按 CleanupOrder 排序） |
| `Unregister(IBattleCleanup)` | 注销（广播期间延迟移除） |
| `Raise()` | 广播退场事件（按 CleanupOrder 顺序） |
| `ListenerCount` | 当前注册数（调试用） |

**详细设计**：见 `SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE.md`

---

## TransformRuntimeSet

**菜单路径**：`Create → MiniGameTemplate/Runtime Sets/Transform Set`
**命名空间**：`MiniGameTemplate.Data`
**源码**：`Assets/_Framework/DataSystem/Scripts/RuntimeSets/`
**实例目录**：`Assets/_Game/Configs/Variables/`

### API

| 方法/属性 | 说明 |
|-----------|------|
| `Items` | 只读 `List<Transform>` |
| `GetFirst()` | 获取首个元素（替代 FindObjectOfType） |

### 使用模式

- 场景中目标 GameObject 挂 `RuntimeSetRegistrar`，指定 RuntimeSet 资产
- OnEnable 自动注册，OnDisable 自动注销
- 其他系统通过 RuntimeSet.GetFirst() 获取引用（零搜索开销）

---

## PoolDefinition

**菜单路径**：`Create → MiniGameTemplate/Pool/Pool Definition`
**命名空间**：`MiniGameTemplate.Pool`
**源码**：`Assets/_Framework/ObjectPool/Scripts/PoolDefinition.cs`
**实例目录**：`Assets/_Game/Configs/Pool/`

### 字段清单

| 字段 | C# 属性 | 类型 | 默认值 | 说明 |
|------|---------|------|--------|------|
| `_prefab` | `Prefab` | `GameObject` | null | 池化预制件 |
| `_initialSize` | `InitialSize` | `int` | 10 | 预实例化数量 |
| `_maxSize` | `MaxSize` | `int` | 50 | 最大容量（0=无限） |

### Agent 创建代码

```csharp
var pd = ScriptableObject.CreateInstance<PoolDefinition>();
var so = new SerializedObject(pd);
so.FindProperty("_prefab").objectReferenceValue = prefabRef;
so.FindProperty("_initialSize").intValue = 20;
so.FindProperty("_maxSize").intValue = 100;
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.CreateAsset(pd, "Assets/_Game/Configs/Pool/Pool_Bullet_Normal.asset");
AssetDatabase.SaveAssets();
```

---

## FSM（有限状态机）

### State

**菜单路径**：`Create → MiniGameTemplate/FSM/State`
**命名空间**：`MiniGameTemplate.FSM`
**源码**：`Assets/_Framework/FSM/Scripts/State.cs`
**实例目录**：`Assets/_Game/Configs/FSM/`

#### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `_description` | `string` | 编辑器注释（Editor Only） |
| `_onEnterEvent` | `GameEvent` | 进入状态触发 |
| `_onExitEvent` | `GameEvent` | 退出状态触发 |

#### 虚方法

| 方法 | 说明 |
|------|------|
| `Enter()` | 进入时调用（触发 _onEnterEvent） |
| `Exit()` | 退出时调用（触发 _onExitEvent） |

### StateTransition

**菜单路径**：`Create → MiniGameTemplate/FSM/State Transition`
**源码**：`Assets/_Framework/FSM/Scripts/StateTransition.cs`

#### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `_fromState` | `State` | 源状态（null=通配，任意状态可转出） |
| `_toState` | `State` | 目标状态 |

#### API

| 方法 | 说明 |
|------|------|
| `IsValid(currentState)` | 判断当前状态是否允许此转换 |

---

## Audio（音频）

### AudioClipSO

**菜单路径**：`Create → MiniGameTemplate/Audio/Audio Clip`
**命名空间**：`MiniGameTemplate.Audio`
**源码**：`Assets/_Framework/AudioSystem/Scripts/AudioClipSO.cs`
**实例目录**：`Assets/_Game/Configs/Audio/`

#### 字段清单

| 字段 | C# 属性 | 类型 | 默认值 | 范围 | 说明 |
|------|---------|------|--------|------|------|
| `_clip` | `Clip` | `AudioClip` | null | — | 音频文件 |
| `_volume` | `Volume` | `float` | 1.0 | [0,1] | 音量 |
| `_pitch` | `Pitch` | `float` | 1.0 | [0.5,2] | 音调 |
| `_loop` | `Loop` | `bool` | false | — | 循环 |

### AudioLibrary

**菜单路径**：`Create → MiniGameTemplate/Audio/Audio Library`
**源码**：`Assets/_Framework/AudioSystem/Scripts/AudioLibrary.cs`

#### 字段清单

| 字段 | 类型 | 说明 |
|------|------|------|
| `_entries` | `AudioEntry[]` | key-clip 映射表 |

#### AudioEntry

| 字段 | 类型 | 说明 |
|------|------|------|
| `key` | `string` | 查找键 |
| `clip` | `AudioClipSO` | 音效配置 |

#### API

| 方法 | 说明 |
|------|------|
| `GetClip(key)` | 按 key 查找 AudioClipSO |

#### 使用模式

```csharp
// 代码中播放
var clip = audioLibrary.GetClip("explosion_01");
AudioManager.Play(clip);
```
