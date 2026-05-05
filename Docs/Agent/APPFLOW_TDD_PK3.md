---
system: navigation
scope: appflow-tdd-pk3
last_verified: 2026-05-05
---

# AppFlow TDD PK #3 — 编辑器工具开发者视角

> **攻方**：Unity 编辑器工具开发者（DX/可调试性/Inspector/构建验证/热重载）  
> **守方**：Unity 架构师（系统完整性/实施成本/V1 范围控制）  
> **结果**：10 问题 / 2 轮 / 100% 收敛  
> **日期**：2026-05-05  

---

## 问题总览

| # | 严重度 | 标题 | R1 裁定 | R2 结果 |
|---|--------|------|---------|---------|
| ET-001 | 🔴高 | AppFlowNavigatorEditor 规格完全缺失 | ✅ 接受 | — |
| ET-002 | 🔴高 | FlowNodeSO 缺少 CustomEditor / PropertyDrawer | ✅ 接受 | — |
| ET-003 | 🔴高 | [RIOM] 自注册无编辑期验证 | ✅ 接受 | — |
| ET-004 | 🟡中 | StopCoroutine 字符串/引用不匹配（bug） | ✅ 接受 | — |
| ET-005 | 🟡中 | Domain Reload 后栈重置 — Editor DX 灾难 | ⚠️ 部分接受 | ✅ 收敛 |
| ET-006 | 🟡中 | 缺少 Gizmo / Hierarchy Icon | ✅ 接受 | — |
| ET-007 | 🟡中 | OnNavigated 缺少 Editor-only 调试钩子 | ✅ 接受 | — |
| ET-008 | 🟡中 | FlowNodeSO 没有 OnValidate 编辑期校验 | ✅ 接受（合并 ET-002） | — |
| ET-009 | 🟢低 | IPreprocessBuildWithReport 构建验证未提及 | ✅ 接受（合并 ET-003） | — |
| ET-010 | 🟢低 | 缺少导航路径预览 Editor 工具 | ⚠️ 部分接受 | ✅ 收敛 |

---

## Round 1 — 攻方质疑

### ET-001 | 🔴高 | AppFlowNavigatorEditor 规格完全缺失

**涉及章节**：§7 Phase 1.8  
**质疑**：整个 TDD 仅一行"PlayMode 栈可视化 + 快速操作按钮"描述 Editor 工具。无法据此实施：渲染方式？显示字段？操作按钮清单？刷新机制？错误高亮？  
**建议**：新增 §3.5 "编辑器工具规格"子章节。

**守方回应**：✅ 接受。新增 §3.5 完整规格。决策：IMGUI CustomEditor + MenuItem 独立窗口入口，事件驱动刷新（OnNavigated + EditorApplication.update），栈列表含 Index/NodeName/Data.ToString()/时间戳，操作按钮 Pop/PopAll/Push下拉，3s+ 超时红色警告。

---

### ET-002 | 🔴高 | FlowNodeSO 缺少 CustomEditor

**涉及章节**：§3.1 FlowNodeSO  
**质疑**：`_panelTypeName` 裸字符串 → typo 概率 100%。应有下拉 + 配置一致性校验 + 无意义配置高亮。  
**建议**：Phase 1 包含 FlowNodeSOEditor.cs。

**守方回应**：✅ 接受。实施 FlowNodeSOEditor.cs：_panelTypeName 下拉（扫描项目 PanelKey 常量）+ HelpBox 警告 + Build Settings 校验按钮。合并 ET-008 的 OnValidate。

---

### ET-003 | 🔴高 | [RIOM] 自注册无编辑期验证

**涉及章节**：§3.2 注册表 + §4.1  
**质疑**：新增面板忘记 [RIOM] 注册 → 运行时才报错。需要编辑期交叉验证。  
**建议**：MenuItem 验证工具 + IPreprocessBuildWithReport 构建守护。

**守方回应**：✅ 接受。双重保护：  
1. `MenuItem("Tools/AppFlow/Validate Panel Registration")` — 静态正则扫描交叉验证  
2. `IPreprocessBuildWithReport` — 构建时自动执行同样逻辑，失败则 BuildFailedException  
（合并 ET-009）

---

### ET-004 | 🟡中 | StopCoroutine 字符串/引用不匹配（bug）

**涉及章节**：§3.2 PushAsync/PopAsync finally 块  
**质疑**：`StartCoroutine(IEnumerator)` 启动但用 `StopCoroutine(nameof(...))` 停止 → 不匹配不生效。  
**建议**：缓存 Coroutine 引用字段。

**守方回应**：✅ 接受。这是代码级 bug。修正为 `_timeoutCoroutine = StartCoroutine(...)` + `StopCoroutine(_timeoutCoroutine)`。

---

### ET-005 | 🟡中 | Domain Reload 后栈重置

**涉及章节**：§6 R3  
**质疑**：改代码 → Domain Reload → 栈丢失 → 回主菜单。开发迭代效率灾难。  
**建议**：[SerializeField] 序列化 FlowNodeSO 引用 + [SerializeReference] IFlowData。

**守方 R1 回应**：⚠️ 部分接受。FlowNodeSO 引用可序列化；Data 需 [Serializable] 才行。折中：推荐而非强制 [Serializable]。  
**攻方 R2 追问**：IFlowData 不强制 [Serializable] → 恢复时 Data 为 null 无提示。  
**守方 R2 回应**：✅ 收敛。约定："推荐 [Serializable]，Editor 热重载必须"；恢复时 LogWarning 诊断。

---

### ET-006 | 🟡中 | 缺少 Gizmo / Hierarchy Icon

**涉及章节**：全文  
**质疑**：DontDestroyOnLoad 中的隐形 Singleton，Scene View / Hierarchy 无视觉反馈。  
**建议**：Hierarchy icon（状态颜色）+ IFlowHandler GO Gizmo 文字标签。

**守方回应**：✅ 接受。纳入 Phase 1.8 Editor 工具范围。

---

### ET-007 | 🟡中 | OnNavigated 缺少 Editor-only 调试钩子

**涉及章节**：§3.2 OnNavigated  
**质疑**：运行时事件；Editor 调试工具挂载需侵入代码。  
**建议**：`#if UNITY_EDITOR internal static event EditorOnNavigated`。

**守方回应**：✅ 接受。零运行时开销。直接回写。

---

### ET-008 | 🟡中 | FlowNodeSO 没有 OnValidate

**涉及章节**：§3.1  
**质疑**：SO 修改后无即时校验。格式/配置错误运行时才暴露。  
**建议**：`#if UNITY_EDITOR OnValidate()` 校验 PanelTypeName 格式、RequiredScene Build Settings、DisplayName 自动填充。

**守方回应**：✅ 接受。合并到 ET-002 FlowNodeSOEditor 实施中。

---

### ET-009 | 🟢低 | 构建验证器

**涉及章节**：§7 / §6  
**质疑**：无 IPreprocessBuildWithReport 守护配置正确性。  
**建议**：构建时扫描 FlowNodeSO + Build Settings + root 节点存在性。

**守方回应**：✅ 接受。合并到 ET-003 方案中。

---

### ET-010 | 🟢低 | 导航路径预览 EditorWindow

**涉及章节**：全文  
**质疑**：散落 SO 无法直观预览流程。  
**建议**：节点图/列表 EditorWindow。

**守方 R1 回应**：⚠️ 部分接受。V1 仅 3 节点，ROI 不高。纳入 V2"8+ 节点触发"。  
**攻方 R2 追问**："8"是拍脑袋数字；通用模板可能一开始就超。  
**守方 R2 回应**：✅ 收敛。修改为建议性指引，不硬绑节点数量。

---

## TDD 修正清单（需回写）

| # | 修正内容 | 对应问题 |
|---|---------|----------|
| 1 | 新增 §3.5 "编辑器工具规格"（完整 AppFlowNavigatorEditor + FlowNodeSOEditor + 验证工具 + Gizmo 规格） | ET-001/002/006/008 |
| 2 | 修正 StopCoroutine 为 Coroutine 引用缓存 | ET-004 |
| 3 | 新增 Editor 热重载恢复机制描述 + IFlowData [Serializable] 推荐约定 | ET-005 |
| 4 | 新增 `#if UNITY_EDITOR EditorOnNavigated` 静态事件 | ET-007 |
| 5 | 新增 MenuItem 验证工具 + IPreprocessBuildWithReport 构建守护描述 | ET-003/009 |
| 6 | §8 后续演进新增"导航路径可视化 EditorWindow"条目 | ET-010 |
| 7 | Phase 1 步骤细化（FlowNodeSOEditor + 构建验证器 + Gizmo） | ET-002/003/006 |

---

_PK #3 完成 | 2026-05-05 | 10 问题 / 2 轮 / 100% 收敛_
