---
name: ui-designer
description: "MiniGameTemplate UI/UX Agent 工作流。用于新增或修改 FairyGUI 界面、HUD、弹窗、组件、按钮状态、状态矩阵、动效、文案、UI 走查和 FairyGUI handoff；当任务需要产出 UI Handoff、白模结构、组件规格、数据绑定、交互事件或验收截图要求时触发。"
---

# UI Designer

## 使用流程

1. 先读 `Docs/Agent/UI_AGENT_BOOTSTRAP.md`。
2. 按任务读 `Docs/Agent/UI_DESIGN/README.md` 中的专题文档。
3. 若要生成 XML/白模/面板代码，继续读 `skills/fairygui-tools/SKILL.md`。
4. 若涉及资产，读 `Docs/Agent/ART_ASSET_AGENT_BOOTSTRAP.md`。
5. 输出 UI Handoff，不只输出视觉描述。

## 任务路由

| 任务 | 必读 |
|------|------|
| UI 风格/token | `UI_DESIGN/UI_DESIGN_SYSTEM.md` |
| 组件 | `UI_DESIGN/UI_COMPONENT_LIBRARY.md` |
| 界面/流程 | `UI_DESIGN/SCREEN_CARDS.md` |
| 动效 | `UI_DESIGN/UI_MOTION_GUIDE.md` |
| 文案 | `UI_DESIGN/UI_COPY_GUIDE.md` |
| FairyGUI 交接 | `UI_DESIGN/FAIRYGUI_HANDOFF_CHECKLIST.md`, `CONTEXT_PACKS/FairyGUI_UI.md` |

## UI Handoff 模板

```text
UI Handoff
- Screen / Component：
- 入口、退出、返回路径：
- 状态矩阵：
- 交互事件：
- 数据绑定 SO：
- FairyGUI 包/组件/Controller/Transition/导出类：
- 文案：
- 动效：
- 验收截图/录屏：
```

## 必须检查

- 状态矩阵是否完整。
- 安全区、触摸热区、摇杆和暂停按钮是否冲突。
- 是否复用 Common 组件。
- 是否遵守 FairyGUI 四统一命名和导出规则。
- 是否禁止 Unicode 图形字符当核心图标。

