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

## 附录：踩坑快速索引

| ID | 陷阱 | 严重级别 | 规则章节 |
|----|------|----------|----------|
| PIT-034 | `??` / `?.` 对 Unity Object 无效 | P0 | 1.1 |
| PIT-035 | `Editor` 命名空间与基类冲突 | P1 | 2.1 |

> 更多踩坑记录见 `code-review-checklist` Skill 的 `references/known-pitfalls.md`
