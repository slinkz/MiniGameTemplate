---
system: role-agent
scope: ui-agent-bootstrap
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/UI_DESIGN/README.md, Docs/Agent/SG_UI_DESIGN.md, Docs/Agent/CONTEXT_PACKS/FairyGUI_UI.md, skills/fairygui-tools/SKILL.md
---

# UI Agent Bootstrap

> 定位：UI/UX Agent 的上岗入口。用于新增界面、修改组件、整理交互、输出 FairyGUI 交接和 UI 走查。

## 1. 先读顺序

| 场景 | 必读 |
|------|------|
| 任意 UI 任务 | `UI_DESIGN/README.md`, `UI_DESIGN/UI_DESIGN_SYSTEM.md` |
| 新增界面 | `UI_DESIGN/SCREEN_CARDS.md`, `UI_DESIGN/FAIRYGUI_HANDOFF_CHECKLIST.md` |
| 新增/修改组件 | `UI_DESIGN/UI_COMPONENT_LIBRARY.md`, `skills/fairygui-tools/SKILL.md` |
| 改动效 | `UI_DESIGN/UI_MOTION_GUIDE.md`, `SG_UI_DESIGN.md` |
| 改文案 | `UI_DESIGN/UI_COPY_GUIDE.md` |
| 进入实现 | `CONTEXT_PACKS/FairyGUI_UI.md`, `MODULE_CARDS/UISystem_FairyGUI.md` |

## 2. UI 交付物

```text
UI Handoff
- Screen / Component 名称
- 入口、退出、返回路径
- 状态矩阵：normal/loading/disabled/locked/error/pause
- 交互事件和数据绑定 SO
- 动效和文案
- FairyGUI 包、组件、Controller、Transition、导出类
- 验收截图/录屏要求
```

## 3. 工作原则

- 先补状态矩阵，再谈视觉。
- 复用 Common 组件；新增组件必须说明为什么不能复用。
- FairyGUI 白模使用 `GGraph` 占位，禁止 Unicode 图形字符当图标。
- 自动生成代码禁止手改，业务写 `.Logic.cs`。
- UI 不直接追场景对象，优先通过 SO 变量和事件绑定。

## 4. 验收口径

1. 所有按钮是否有普通/按下/禁用态？
2. 安全区、触摸热区、暂停按钮和摇杆触摸是否冲突？
3. Open/Refresh/Close 是否会重复绑定事件？
4. Push/Pop/Replace/返回是否符合 AppFlow？
5. 微信真机点击区域是否可用？

