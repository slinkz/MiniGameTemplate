---
name: coding-standards
description: >
  MiniGameTemplate 项目的 C# 编码规范。此 Skill 应在以下场景 **编码前** 加载：
  (1) 编写新的 C# 脚本之前；(2) 修改现有 C# 代码之前；(3) 生成代码模板之前。
  与 code-review-checklist（编码后审查）互补——本规范关注"怎么写"，审查清单关注"写完检查什么"。
  触发关键词：编码规范、代码规范、coding standards、coding convention、编码前。
---

# C# 编码规范 — MiniGameTemplate

## 适用时机

**编码前必须加载**此 Skill。每次写新代码或修改现有代码前，快速过一遍本文档。

---

## 1. Unity 序列化陷阱（CRITICAL）

### 1.1 禁止对 UnityEngine.Object 派生类型使用 `??` 和 `?.`

> **严重级别：P0 — 必须遵守**

Unity 的 `UnityEngine.Object`（包括 `GameObject`、`Component`、`ScriptableObject`、`Texture2D` 等）重载了 `== null` 运算符。未赋值的序列化字段在 C# 层面**不是** `null`（它是一个 "fake null" 壳对象），但 `== null` 返回 `true`。

C# 的 `??`（null-coalescing）和 `?.`（null-conditional）运算符**不走** Unity 的 `== null` 重载，而是使用 C# 原生 null 检查。这导致 `??` 不会触发 fallback，`?.` 不会短路。

```csharp
// ❌ 错误：fake-null 不触发 fallback，texture 仍然是 "fake null"
var texture = bulletType.SourceTexture ?? _fallbackAtlas;

// ❌ 错误：fake-null 不会短路，会尝试访问已销毁对象的成员
var name = destroyedObject?.name;

// ✅ 正确：显式使用 Unity 重载的 != null
var srcTex = bulletType.SourceTexture;
var texture = (srcTex != null) ? srcTex : _fallbackAtlas;

// ✅ 正确：显式判断
if (targetObject != null)
{
    var name = targetObject.name;
}
```

**规则**：凡是类型继承自 `UnityEngine.Object` 的变量，一律使用 `!= null` / `== null` 显式判断，**禁止** `??`、`??=`、`?.`。

**速查**：以下类型都受影响：
- `GameObject`、`Transform`、`Component` 及所有子类
- `ScriptableObject` 及所有子类
- `Texture`、`Texture2D`、`Material`、`Shader`、`Mesh`、`Sprite`
- `AudioClip`、`AnimationClip`、`RuntimeAnimatorController`
- 任何 `UnityEngine.Object` 派生类型

### 1.2 新增 [SerializeField] 后检查实例

新增或修改 `[SerializeField]` 字段后，必须同步检查所有引用该类型的 `.unity` 场景文件和 `.prefab` 预制件，确保序列化数据不会丢失或产生默认值覆盖。

### 1.3 ScriptableObject 不存储场景实例引用

`ScriptableObject` 中**禁止**存储场景内 GameObject/Component 实例引用（导致内存泄漏和序列化错误）。只能存储其他 SO 资产引用或原始值。

---

## 2. 命名空间安全

### 2.1 避免与项目命名空间冲突

当代码位于名为 `Editor` 的文件夹下时，命名空间中通常包含 `Editor`，这会与 `UnityEditor.Editor` 基类冲突。

```csharp
// ❌ 错误：Editor 被解析为命名空间而非类型
public class MyEditor : Editor { }

// ✅ 正确：使用全限定名
public class MyEditor : UnityEditor.Editor { }
```

**规则**：在编辑器代码中继承 `Editor` 基类时，一律使用 `UnityEditor.Editor` 全限定名。

### 2.2 程序集边界

- `MiniGameFramework.Runtime.asmdef` (`Assets/_Framework/`)：框架运行时代码
- `MiniGameFramework.Editor.asmdef` (`Assets/_Framework/Editor/`)：框架编辑器工具
- `Game.Runtime.asmdef` (`Assets/_Game/`)：游戏逻辑
- 跨程序集引用必须在 `.asmdef` 中声明依赖

---

## 3. ScriptableObject 驱动设计

### 3.1 共享数据必须使用 SO

所有跨系统共享的运行时数据（生命值、分数、配置参数等）必须存在于 `ScriptableObject` 资产中，禁止存放在跨场景传递的 MonoBehaviour 字段或静态变量中。

### 3.2 跨系统通信用 SO 事件通道

禁止 `GameObject.Find()`、`FindObjectOfType()` 或静态单例进行跨系统通信。使用 `GameEvent : ScriptableObject` 事件通道。

### 3.3 所有自定义 SO 添加 `[CreateAssetMenu]`

确保设计师可以在 Project 窗口右键创建资产。

---

## 4. 单一职责

### 4.1 MonoBehaviour < 150 行

如果一个 MonoBehaviour 超过 150 行，审视是否可以拆分。用"和"描述一个组件的职责时，应该拆分。

### 4.2 预制件自包含

拖入空场景的每个预制件必须能正常工作，不依赖场景层级中的其他对象。组件间通过 Inspector 分配的 SO 资产连接。

---

## 5. 性能规范

### 5.1 热路径零 GC

渲染循环、物理循环、弹幕更新等热路径中禁止产生 GC 分配。禁止在这些路径中：
- 使用 `string` 拼接（改用 `StringBuilder` 或避免）
- 创建 `new` 引用类型对象
- 使用 LINQ
- 使用 `foreach`（对非数组集合会产生装箱）

### 5.2 事件驱动优先于轮询

如果逻辑可以用事件触发，禁止放在 `Update()` 中轮询。

### 5.3 对象池

频繁创建销毁的对象使用对象池。已有的池化系统：
- `BulletWorld` — 弹丸对象池（SoA 数据布局）
- `SpriteSheetVFXSystem` — VFX 实例池

---

## 6. 编辑器代码规范

### 6.1 编辑器脚本修改 SO 时调用 `SetDirty`

```csharp
// ✅ 正确
EditorUtility.SetDirty(target);
```

### 6.2 CustomEditor 遍历字段用 `SerializedProperty`

使用 `serializedObject.GetIterator()` + `NextVisible()` 遍历字段，不要手写每个字段名（除非需要条件显隐）。

---

## 7. 防御性编程

### 7.1 禁止魔法字符串

标签、层、动画器参数等使用 `const string` 或基于 SO 的引用。

```csharp
// ❌ 错误
if (other.CompareTag("Player")) { }

// ✅ 正确
private const string TAG_PLAYER = "Player";
if (other.CompareTag(TAG_PLAYER)) { }
```

### 7.2 `DontDestroyOnLoad` 限制使用

禁止滥用 `DontDestroyOnLoad` 单例。如果必须使用，需在代码审查中说明理由。

---

## 8. 文件组织

### 8.1 一个文件一个类型

每个 `.cs` 文件只包含一个公开类型（`partial class` 拆分除外）。

### 8.2 SO 资产路径

所有 ScriptableObject 资产放在 `Assets/ScriptableObjects/` 下按域组织子文件夹。

### 8.3 编辑器脚本路径

编辑器脚本放在对应模块的 `Editor/` 子目录中。

---

## 9. 跨场景数据传递（CRITICAL — 2026-05-07 血泪教训）

> **严重级别：P0 — 必须遵守**

### 9.1 唯一正确路径：AppFlowNavigator + IFlowData

所有跨场景数据传递**必须且只能**通过框架提供的 Navigation 机制：

```csharp
// ✅ 正确：通过导航框架传递数据
var data = new BattleLevelData { LevelIndex = levelIndex };
await AppFlowNavigator.Instance.PushAsync(battleNode, data);

// 接收端：IFlowHandler.OnFlowEnter(IFlowData data)
public void OnFlowEnter(IFlowData data)
{
    if (data is BattleLevelData battleData)
        _controller.SetLaunchContext(battleData.LevelIndex);
}
```

### 9.2 禁止的跨场景传参方式

以下方式**全部禁止**，无论看起来多"方便"：

| 禁止方式 | 为什么不行 |
|----------|-----------|
| ❌ SO 写运行时值当全局变量 | SO 是项目级资产，Play Mode 下修改不持久化；多入口时状态残留 |
| ❌ static 字段传参 | 无生命周期管理，热重载丢失，测试困难 |
| ❌ Resources.Load 在运行时读 SO 当通信 | 本质是文件 IO 伪装通信，与 Addressables/YooAsset 冲突 |
| ❌ PlayerPrefs 传临时数据 | 持久化到磁盘，性能差，无类型安全 |
| ❌ DontDestroyOnLoad 当消息总线 | 生命周期不可控，场景重载不重置 |
| ❌ AssetDatabase.LoadAssetAtPath | Editor-only，真机直接崩 |

### 9.3 直跑场景 = 测试模式

直接运行一个场景（不经导航框架）时，`IFlowData` 为空。此时：
- 控制器应有**安全的 fallback 行为**（如使用 Inspector 配置的默认值）
- **禁止写入存档/进度**（`_launchLevelIndex == null` → 不写 ProgressManager）
- 这是调试通道，不是正式游戏流程

---

## 10. Bug 修复 SOP（修改代码前必须执行）

> **适用场景**：修复任何 bug 之前，必须按此流程操作。禁止直接跳到"改代码"。

### Step 0: 定位问题类型

先判断 bug 属于哪种类型：

| 类型 | 表现 | 诊断方向 |
|------|------|----------|
| 数据流断裂 | 数据从 A 到 B 传不过去 | 画完整链路图，找断点 |
| 时序错误 | 有时好使有时不好使 | 列出生命周期调用顺序（Awake→OnEnable→Start→OnFlowEnter） |
| 逻辑错误 | 稳定复现但结果不对 | 写出期望 vs 实际的对比 |
| 配置遗漏 | 功能存在但未生效 | 检查 Inspector 绑定、SO 引用、场景设置 |

### Step 1: 确认框架是否已提供解决机制

**在动手写任何代码之前，先回答这个问题：**

> "这个项目已有的框架/机制中，是否已经提供了解决这个问题的正确路径？"

- 如果有 → 用框架提供的路径，找出为什么它没生效
- 如果不确定 → 读 TDD / ARCHITECTURE 文档，或**问天命人**
- **绝不允许**在不确定的情况下自己发明一条路

### Step 2: 禁止事项 Checklist

修复方案写好后，逐项验证：

- [ ] 没有用 SO 当全局变量
- [ ] 没有用 static 传参
- [ ] 没有用 Resources.Load 当通信手段
- [ ] 没有引入 Editor-only API（AssetDatabase、EditorUtility 等）
- [ ] 修复方案在真机（WebGL/微信小游戏）环境下同样有效
- [ ] 没有绕过现有框架另起炉灶
- [ ] 修改不会在场景常驻/热重载时产生状态残留

### Step 3: 真机可行性检查

微信小游戏环境**不支持**：
- `System.IO.File.*`（用 WX SDK 的 FileSystemManager）
- `AssetDatabase.*`（Editor-only）
- `Thread`（WebGL 单线程）
- 同步阻塞 IO

### 红线

> 如果觉得框架不支持当前需求，**先提出框架改进方案，等天命人确认**。
> 不要自己 hack。这是铁律。

---

## 11. 最低可视化原则（Minimum Visual Representation）

> **严重级别：P0 — 必须遵守**
> 
> **来源**：ShooterGame V2 Sprint 3 复盘——多次因"纯逻辑实现无显示对象"导致天命人无法验收

### 11.1 核心规则

每个涉及运行时实体/效果的功能，实现时**必须**同时创建或关联最低限度的显示对象。**没有显示 = 功能未完成。**

### 11.2 各类型最低要求

| 功能类型 | 最低可视化要求 | 禁止行为 |
|---------|--------------|---------|
| 新增实体（道具、弹幕、敌机、特效） | 必须有 SpriteRenderer / MeshRenderer 或等效可视组件，至少用占位 Sprite | ❌ 纯逻辑空 GameObject |
| 新增 Buff / 状态效果 | 至少在运行时有可观测表现（图标、颜色变化、粒子、或 Debug Overlay） | ❌ 只改数值不改显示 |
| 新增碰撞交互 | 碰撞体必须有对应的可视边界（运行时 Sprite 或编辑器 Gizmo） | ❌ 不可见的碰撞体 |
| 新增音效/反馈 | 至少有 Console 日志或临时 UI 提示 | ❌ 完全无感知的逻辑 |

### 11.3 判定标准

> **如果天命人在 Game 视图中看不到/听不到任何变化，这个功能就是未完成。**

即使最终效果需要美术资源替换，实现阶段也必须用占位资源（Placeholder）让功能可见、可验收。

### 11.4 Checklist（编码完成时自查）

- [ ] 新增的每个运行时实体都有可视组件
- [ ] Buff/状态变化在 Game 视图中有可观测表现
- [ ] 碰撞区域有对应的视觉表示
- [ ] 天命人不需要看 Console 就能判断功能是否在工作

---

## 12. 云函数必须同步客户端数据结构（CRITICAL — 2026-05-24 血泪教训）

> **严重级别：P0 — 必须遵守**
> 
> **来源**：`saveProgress` 云函数只解构了 V1 字段（`clearedLevels`），V3 新增的 `levelStars`、解锁列表、成就计数器全被静默丢弃，星级数据在云端完全为空

### 12.1 核心规则

当客户端的**持久化数据结构**（如 `SharedProgressData`）增删字段时，所有涉及该数据的**云函数**必须同步更新。否则新字段会被静默丢弃——无报错、无日志、上传"成功"但数据丢失。

### 12.2 适用范围

| 组件 | 必须同步的内容 |
|------|---------------|
| C# 数据类（`SharedProgressData` 等） | 字段定义 |
| 云函数 `saveProgress` | 解构 & 写入的字段列表 |
| 云函数 `getProgress` | 返回的字段（通常原样返回无需改，但需确认） |
| `CloudSyncService.DoUpload()` | 序列化的 JSON 包含新字段（通常自动包含） |

### 12.3 操作 Checklist

每次修改持久化数据结构时：

- [ ] C# 数据类已增加新字段
- [ ] `saveProgress` 云函数已解构并写入新字段（含合理默认值）
- [ ] `getProgress` 云函数的返回不会过滤掉新字段
- [ ] 本地已有存档在 merge 逻辑中对新字段有 fallback（`?? []` / `?? 0`）
- [ ] 云函数已重新部署（微信开发者工具上传）

### 12.4 为什么容易踩坑

- 客户端 `JSON.stringify(data)` 自动包含所有字段 → **看起来**上传正确
- 云函数 `const { a, b } = event` 解构时，未列出的字段被**静默丢弃**——JavaScript 解构不报错
- `getProgress` 返回的数据看起来"结构完整"，因为读的就是写进去的（写少了读回来自然少）
- **唯一发现方式**：打完关后换设备登录/清缓存，发现新字段数据丢失

---

## 附录：踩坑快速索引

| ID | 陷阱 | 严重级别 | 规则章节 |
|----|------|----------|----------|
| PIT-034 | `??` / `?.` 对 Unity Object 无效 | P0 | 1.1 |
| PIT-035 | `Editor` 命名空间与基类冲突 | P1 | 2.1 |
| PIT-036 | 跨场景传参绕过 Navigation 框架 | P0 | 9.1 |
| PIT-037 | 直跑场景测试模式写入真实存档 | P1 | 9.3 |
| PIT-038 | 纯逻辑实现无显示对象，无法验收 | P0 | 11.1 |
| PIT-041 | 云函数未同步客户端数据结构，新字段静默丢弃 | P0 | 12.1 |

> 更多踩坑记录见 `code-review-checklist` Skill 的 `references/known-pitfalls.md`
