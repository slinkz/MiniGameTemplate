---
system: navigation
scope: appflow-tdd-index
last_verified: 2026-05-17
related_code: Assets/_Framework/Navigation/*.cs, Assets/_Framework/UISystem/Scripts/IUIPanel.cs, Assets/_Framework/UISystem/Scripts/UIManager.cs
---

# AppFlow 导航系统 — TDD 索引

> **版本**：v1.8（冷启动清栈 — 热启动恢复暂时禁用）  
> **状态**：✅ Phase 1~4 全部完成 + 3 轮 PK 评审通过 + 场景策略重构 + 面板 Hide/Show 生命周期 + 冷启动清栈  
> **作者**：广智  
> **日期**：2026-05-17  
> **ADR**：ADR-034  
> **PK 记录**：  
> - [APPFLOW_TDD_PK.md](APPFLOW_TDD_PK.md)（#1 微信全栈：12 问题 / 1 轮 / 100% 收敛）  
> - [APPFLOW_TDD_PK2.md](APPFLOW_TDD_PK2.md)（#2 Unity架构师：10 问题 / 2 轮 / 100% 收敛）  
> - [APPFLOW_TDD_PK3.md](APPFLOW_TDD_PK3.md)（#3 编辑器工具开发者：10 问题 / 2 轮 / 100% 收敛）

---

## 子文件列表

| # | 文件 | 内容 |
|---|------|------|
| 01 | [APPFLOW_TDD_01_CORE_DESIGN.md](APPFLOW_TDD_01_CORE_DESIGN.md) | §1 问题定义 + §2 架构设计 + §3 详细设计（FlowNodeSO / AppFlowNavigator / IFlowData / IFlowHandler / IPanelSuspendable） |
| 02 | [APPFLOW_TDD_02_EDITOR_TOOLS.md](APPFLOW_TDD_02_EDITOR_TOOLS.md) | §3.5 编辑器工具规格（Inspector / EditorWindow / BuildValidator / HierarchyIcon） |
| 03 | [APPFLOW_TDD_03_INTEGRATION.md](APPFLOW_TDD_03_INTEGRATION.md) | §4 系统集成（与现有系统关系 / SO 资产清单 / 导航流程时序 / 场景策略） |
| 04 | [APPFLOW_TDD_04_TRADEOFFS.md](APPFLOW_TDD_04_TRADEOFFS.md) | §5 权衡分析 + §6 风险与缓解 |
| 05 | [APPFLOW_TDD_05_IMPL_PLAN.md](APPFLOW_TDD_05_IMPL_PLAN.md) | §7 实施计划 + 验收标准 + §8 后续演进 |

---

## 核心概念速览

```
┌───────────────────────────────────────────────────────┐
│                  AppFlowNavigator                       │
│  ┌─────────────────────────────────────────────────┐  │
│  │        Navigation Stack (LIFO)                   │  │
│  │  [MainMenu] → [LevelSelect] → [Battle]          │  │
│  └─────────────────────────────────────────────────┘  │
│                                                        │
│  Push(node, data)  /  Pop(returnData)                  │
│  PopTo(node)       /  Replace(node, data)              │
│  PopAll()          /  Peek()                           │
└────────────────────────┬──────────────────────────────┘
                         │ 内部调用
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
  ┌──────────────┐ ┌──────────┐ ┌────────────────┐
  │ SceneLoader  │ │UIManager │ │ IFlowHandler   │
  │ (场景加载)    │ │(面板管理) │ │ (节点钩子接口)  │
  └──────────────┘ └──────────┘ └────────────────┘
```

**面板行为矩阵（方案 B v1.7）**：

| 导航操作 | 对 leaving 层面板 | 对 returning 层面板 |
|---------|------------------|-------------------|
| Push | SuspendAllPanels（Hide） | — |
| Pop | CloseAllPanels + CloseSuspendedPanels | ResumePanels |
| PopTo | CloseAllPanels + Close 中间层 | ResumePanels 目标层 |
| PopAll | CloseAllPanels + Close 中间层 | ResumePanels 根层 |
| Replace | CloseAllPanels + CloseSuspendedPanels | — (EnterNode 打开新面板) |

---

## 版本记录

| 版本 | 日期 | 作者 | 变更 |
|------|------|------|------|
| v1.8 | 2026-05-17 | 广智 | 冷启动清栈：TryRestoreNavigationStackAsync 一律清空 appflow_stack + return false（热启动恢复暂时禁用）|
| v1.7 | 2026-05-07 | 广智 | 面板 Suspend/Resume 生命周期（方案 B）：删除 CloseAllPanelsOnEnter，新增 IPanelSuspendable + UIManager API + OwnedPanelTypes |
| v1.6 | 2026-05-06 | 广智 | 双 Single 场景切换重构（Boot→Main⇄Battle） |
| v1.5 | 2026-05-05 | 广智 | Phase 4 栈序列化（微信热启动恢复） |
| v1.4 | 2026-05-05 | 广智 | 3 轮 PK 评审完成，全部回写 |
| v1.0 | 2026-05-04 | 广智 | 初版：栈式导航 + 注册表面板 + SceneLoader 对称 API |
