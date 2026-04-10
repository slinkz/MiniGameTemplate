---
name: code-review-checklist
description: >
  MiniGameTemplate 项目的代码审查检查清单 Skill。此 Skill 应在以下场景加载：
  (1) 任何代码改动后进行代码审查；(2) 大段代码一次性生成后的完整性验证；
  (3) 涉及 FairyGUI、Unity API、跨文件重构等高风险操作时。
  此 Skill 基于项目历史踩坑经验，提供分类检查清单，逐项验证以降低编译错误和运行时 bug。
---

# Code Review Checklist — MiniGameTemplate

## 适用时机

**强制加载**此 Skill 的场景：

1. 任何代码改动完成后，进行代码审查之前
2. 一次性生成或修改超过 3 个文件时
3. 涉及 FairyGUI UI 改动（包括按钮增减、面板重构、包移动）
4. 涉及 Unity API 版本敏感操作（条件编译、EditorOnly API）
5. 涉及跨程序集（asmdef）的代码移动或重构
6. 涉及资产路径、场景名、包名等字符串级引用

## 审查 SOP（标准操作流程）

### 第一步：加载踩坑经验库（分层加载）

踩坑经验库采用**活跃层 + 归档层**双层结构，避免随时间膨胀导致上下文溢出：

1. **必读**：`references/known-pitfalls.md`（活跃层，≤ 30 条 + 高频经典条目）
2. **按需读**：`references/known-pitfalls-archive.md`（归档层）— 仅当活跃层未覆盖当前改动涉及的错误模式时才读取

浏览活跃层全部记录，建立当前审查的心理检查清单。

### 第二步：常规四维审查

按以下四个维度逐一审查代码改动：

1. **正确性** — 代码是否实现了预期功能？逻辑是否正确？
2. **安全性** — 是否有空引用风险？边界条件处理？
3. **可维护性** — 命名是否清晰？结构是否合理？
4. **性能** — 是否有不必要的分配？N+1 查询？热路径上的 GC？

### 第三步：分类检查清单逐项验证

以下是从历史踩坑中提炼的 **8 类必检项**。每个改动必须逐一过一遍，标记 ✅ 或 ❌：

---

#### CL-1: 跨文件引用完整性

> 如果 A 文件引用了 B 文件中的字段/方法/类型，B 文件中必须确实存在该成员。

**验证方法**：对新增/修改的每个成员引用，使用 grep/search 确认定义端存在。

**典型错误**：BulletMover 引用 `BulletTypeSO.SpeedOverLifetime`，但 BulletTypeSO 未定义该字段。

---

#### CL-2: 命名空间安全

> 自定义命名空间不得与 Unity/C# 内置类型重名，不得在自身命名空间下产生歧义解析。

**验证方法**：
- 检查新增命名空间是否与 `System.*`、`UnityEngine.*`、`UnityEditor.*` 中的常用类重名
- 检查命名空间内部是否存在与外部同名类型导致的就近解析歧义
- 跨命名空间引用时考虑是否需要 `global::` 前缀

**典型错误**：`namespace MiniGameTemplate.Debug` 导致所有 `Debug.LogWarning` 被解析为当前命名空间而非 `UnityEngine.Debug`。

---

#### CL-3: Unity API 版本兼容

> 条件编译宏必须**全文搜索**确保无遗漏，不能只改一处而忘了其他处。

**验证方法**：
- 使用版本敏感 API 时，确认该 API 在目标 Unity 版本（2022 LTS）中存在
- 如果添加了条件编译 `#if`，全局搜索同一 API 确认所有调用点都已覆盖
- 特别注意 `NamedBuildTarget`（2022.1+）、`Il2CppCodeGeneration`（namespace 变化）等

**典型错误**：修了一处 `NamedBuildTarget` 的条件编译，漏了同文件另一处。

---

#### CL-4: 字符串级引用验证

> 场景名、FairyGUI 包名、资产路径、配置文件名等字符串引用必须与磁盘实际文件一致。

**验证方法**：
- `SceneManager.LoadScene("X")` → 确认 `EditorBuildSettings` 和磁盘上 X.unity 存在
- `UIPackage.CreateObject("X", "Y")` → 确认 FairyGUI 包名和组件名匹配
- `PanelPackageName` → 确认与 FairyGUI 包名一致
- 资产加载路径 → 确认 YooAsset 收集路径与实际文件位置匹配

**典型错误**：`MainMenuPanel.Logic.cs` 加载场景 `ClickGame` 但实际场景文件名为 `Game`。

---

#### CL-5: 生命周期与时序

> 回调中修改状态、先后顺序假设、事件绑定/解绑时机必须仔细审查。

**验证方法**：
- `OnOpen` 中绑定事件 → `OnClose` 中是否解绑？
- `OnRefresh` 是否错误调用了 `OnOpen`？（禁止！会导致事件双绑定）
- 回调中取消自身（如 Timer 回调中 Cancel Timer） → 后续代码是否安全？
- ClosePanel 后的回调引用 → 闭包是否捕获了已释放的对象？

**典型错误**：OnRefresh→OnOpen 在 4 个面板中重复 `onClick.Add()`，导致事件双绑定。

---

#### CL-6: 渲染管线与 Mesh API

> Unity Mesh API 的隐式规则不在 IDE 检查范围内，必须人工验证。

**验证方法**：
- 自定义顶点结构的字段顺序必须与 `VertexAttributeDescriptor[]` 声明顺序一致
- Unity 标准顺序：Position → Color → UV（**不是** Position → UV → Color）
- 新建材质/Renderer 时必须确认纹理绑定（`material.mainTexture = ...` 或 `SetTexture("_MainTex", ...)`）
- Shader 中的属性名必须与 C# 端的 SetXxx 调用匹配

**典型错误**：`DanmakuVertex` 字段顺序 Position→UV→Color，但 Unity 自动重排导致数据错位。

---

#### CL-7: FairyGUI 改动完整性（关键！）

> FairyGUI 相关改动必须遵循 **UI 源文件优先** 原则，并确保三端同步。

**FairyGUI 改动操作顺序（必须严格遵循）**：

1. **先改 FairyGUI 源文件**（`UIProject/assets/包名/` 下的 XML 文件）
2. **再导出/生成 C# 代码**（由 FairyGUI 编辑器完成，或提醒用户重新发布）
3. **最后改业务逻辑代码**（`*.Logic.cs` 和调用端）

**绝不允许**：
- ❌ 只改 C# 代码而不改 FairyGUI 源文件（例如：在 C# 中引用新按钮，但 FairyGUI XML 中没有该按钮）
- ❌ 手动修改 FairyGUI 自动生成的代码文件（文件头有 `/** This is an automatically generated class **/` 标注）
- ❌ 在 `.Logic.cs` 中引用不存在的 UI 组件字段

**移动 FairyGUI 包时必须同步处理**：

当把一个 FairyGUI 包的**相关代码**从一个 asmdef 区域移动到另一个时（例如从 `_Game` 移动到 `_Example`），必须：

1. 修改 FairyGUI `package.xml` 中的 `codePath` 属性，指向新的代码输出目录
2. 将 FairyGUI 自动生成的 C# 代码（`XXXBinder.cs`、`XXXPanel.cs`、组件类）移动到新目录
3. 手写的 `.Logic.cs` 文件也要跟着移动
4. 验证 namespace 是否仍然正确（FairyGUI 生成代码的 namespace = 包名，不会变；但调用端可能需要 `global::` 前缀）
5. 确认新目录所在的 asmdef 引用了 `FairyGUI` 程序集

**当前项目的正确示例（ClickGame）**：
```
UIProject/assets/ClickGame/package.xml:
  codePath="../UnityProj/Assets/_Example/ClickGame/UI"

Assets/_Example/ClickGame/
  UI/ClickGame/          ← FairyGUI 生成代码（namespace ClickGame）
    ClickGameBinder.cs
    ClickCounterPanel.cs
    MenuIconButton.cs
    ClickCounterPanel.Logic.cs  ← 手写逻辑（partial class, namespace ClickGame）
  Scripts/               ← 手写脚本（namespace MiniGameTemplate.Example）
    ClickGameSceneEntry.cs  ← 使用 global::ClickGame.XXX 跨命名空间引用
```

**验证清单**：
- [ ] FairyGUI XML 中的按钮/组件是否与 C# 代码引用匹配？
- [ ] `package.xml` 的 `codePath` 是否指向正确目录？
- [ ] 生成代码的 namespace 是否与包名一致？
- [ ] `.Logic.cs` 的 namespace 是否与生成代码一致？
- [ ] 调用端是否使用了正确的 namespace 前缀（`global::包名.XXX`）？

---

#### CL-8: 第三方库命名空间验证

> 使用第三方库时，必须验证实际命名空间，不能想当然地写 using。

**验证方法**：
- 使用 grep 在第三方库源码中搜索实际的 `namespace` 声明
- 确认 using 语句与实际命名空间一致
- 特别注意 wrapper/fork 库可能修改过原始命名空间

**典型错误**：`SimpleJSON` 实际命名空间是 `Luban.SimpleJSON`，5 个文件全报 CS0246。

---

### 第四步：输出审查报告

审查完成后，输出结构化审查报告：

```
## 代码审查报告

### 总体印象
[一两句总结]

### 检查清单
- [✅/❌] CL-1 跨文件引用完整性
- [✅/❌] CL-2 命名空间安全
- [✅/❌] CL-3 Unity API 版本兼容
- [✅/❌] CL-4 字符串级引用验证
- [✅/❌] CL-5 生命周期与时序
- [✅/❌] CL-6 渲染管线与 Mesh API
- [✅/❌] CL-7 FairyGUI 改动完整性
- [✅/❌] CL-8 第三方库命名空间验证

### 发现的问题
[按 🔴/🟡/💭 分级列出]

### 优点
[值得肯定的地方]
```

## 踩坑追加协议

当新的编译错误或运行时 bug 被修复后，**必须立即**执行以下操作：

1. 在 `references/known-pitfalls.md`（活跃层）中追加一条新的 PIT 记录
2. 评估该错误是否属于已有的 CL-1 ~ CL-8 类别
3. 如果不属于任何已有类别，在 SKILL.md 中新增 CL-N 检查项
4. 如果属于已有类别但暴露了新的子模式，在对应 CL 中补充说明

**PIT 记录格式**见 `references/known-pitfalls.md`。

---

## 经验库维护规范（分层加载 & 定期蒸馏）

### 文件结构

```
references/
├── known-pitfalls.md              ← 活跃层（强制读取）
└── known-pitfalls-archive.md      ← 归档层（按需读取）
```

### 容量阈值

- **活跃层上限**：30 条（+ 不限数量的高频经典条目，标记 `[经典]`）
- **触发蒸馏**：当活跃层总条目 > 30 条时，必须执行一次蒸馏

### 蒸馏规则

蒸馏时按以下优先级将条目从活跃层移至归档层：

1. **已固化到 CL 检查清单中的条目** — 该错误模式已被 CL 规则覆盖，活跃层留简短引用即可
2. **重复模式合并** — 多条同根因的条目合并为 1 条通用规则（保留在活跃层），原始条目移至归档层
3. **长期未命中** — 连续 3 个月（90 天）未被任何审查引用的条目降级到归档层
4. **标记 `[经典]` 的条目永不降级** — 这些是高频命中、容易反复犯的错误

### 蒸馏操作步骤

1. 检查活跃层条目数量是否超过 30 条
2. 按上述优先级选出降级候选条目
3. 将候选条目**剪切**到 `known-pitfalls-archive.md` 末尾（保留完整 PIT 格式）
4. 在活跃层原位置留下单行引用：`> 已归档 → PIT-0XX（归档原因）`
5. 更新活跃层和归档层的条目计数注释

### 归档层格式

归档层保持与活跃层相同的 PIT 格式，额外增加：
- 头部注释：`归档日期: YYYY-MM-DD`
- 归档原因：`已被 CL-X 覆盖` / `合并入 PIT-0YY` / `90 天未命中`

### 检查清单稳定度标记

当某条 CL 规则连续 3 个月（90 天）零违规时，可以在该规则标题后标注 `[stable]`：
- `[stable]` 规则在审查时**降低优先级**（快速扫过即可），集中精力在活跃规则上
- 一旦再次违规，立即移除 `[stable]` 标记
