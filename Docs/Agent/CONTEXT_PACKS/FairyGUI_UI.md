---
system: knowledge-engineering
scope: context-pack-fairygui-ui
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Context Pack: FairyGUI UI

## 适用任务

- 新增或修改 FairyGUI 面板、包、导出代码、UI Controller。
- 修改主菜单、关卡选择、战斗 HUD、胜利/失败/暂停、出战准备面板。
- 排查 UI 点击、层级、遮罩、Suspend/Resume、包加载问题。

## 必读文档

| 目的 | 文档 |
|------|------|
| UI 系统架构 | `ARCHITECTURE.md` 中 UI 工作流和 SortingOrder |
| UI 模块卡 | `MODULE_CARDS/UISystem_FairyGUI.md` |
| ShooterGame UI | `SHOOTER_GAME/SG_UI_DESIGN.md`, `SHOOTER_GAME/TDD/SG_TDD_04_UI_CONTROLLERS.md` |
| AppFlow | `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md`, `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_03_INTEGRATION.md`, `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` 第六部分 |
| FairyGUI Skill | `skills/fairygui-tools/SKILL.md` |
| FairyGUI 坑 | `skills/fairygui-tools/references/pitfalls.md` |
| 强类型绑定检查 | `Tools/check_fairygui_typed_bindings.py`, `Tools/fairygui-typed-bindings-baseline.txt` |
| 编辑器工具 | `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_INDEX.md` |

## 关键代码入口

```text
UIProject/
UnityProj/Assets/_Game/FairyGUI_Export/
UnityProj/Assets/_Game/Scripts/UI/
UnityProj/Assets/_Framework/UISystem/
UnityProj/Assets/_Framework/Navigation/
UnityProj/Assets/_Game/Scripts/GameStartupFlow.cs
```

## 关键 SO / 配置路径

```text
UnityProj/Assets/_Game/Configs/Core/
UnityProj/Assets/_Game/Configs/Variables/
UnityProj/Assets/_Game/Configs/Events/
UnityProj/Assets/_Game/Configs/ShooterGame/Variables/
```

UI 通常通过 SO 变量和事件绑定数据，不直接追场景对象。

## 关键 ADR / 约束

- ADR-034：AppFlow 栈式导航系统。
- `IUIPanel` 是 UIManager 管理面板生命周期的核心接口。
- `IPanelSuspendable` 用于面板 Suspend/Resume。
- FairyGUI 自动生成文件不要手动改，业务逻辑写 `.Logic.cs`。
- 手写 Controller / `.Logic.cs` 必须优先使用 FairyGUI 生成字段，禁止新增 `GetChild("...")` / `GetController("...")` 字符串绑定。
- UI 组件和代码必须成对出现；没有 XML / 生成类的 UI Controller 逻辑按死代码处理，不允许代码先行。
- 对话框/Loading 层级要遵守 `UIConstants` SortingOrder。

## 常见坑

- 改了 FairyGUI 源 XML/FUI 后忘记发布到 Unity。
- 直接修改自动生成的 `XXXPanel.cs`，下次导出被覆盖。
- OnRefresh 调 OnOpen 导致事件重复绑定。
- 弹窗层级低于 Loading，被遮挡。
- AppFlow Pop/Push 时面板没有正确 Suspend/Resume。
- UIProject 与 Unity 导出资源不同步。

## 修改后必验

- FairyGUI 发布成功，Unity 中包可加载。
- 执行 `python Tools/check_fairygui_typed_bindings.py`，确认没有 FairyGUI 字符串绑定；baseline 正常应为 0。
- Binder 在 `GameStartupFlow` 或对应启动流程中注册。
- 面板 Open/Refresh/Close 不重复绑定事件。
- Push/Pop/Replace/返回流程符合 AppFlow。
- 遮罩、层级、全屏/非全屏行为正确。
- 微信真机点击区域和触摸行为正常。
