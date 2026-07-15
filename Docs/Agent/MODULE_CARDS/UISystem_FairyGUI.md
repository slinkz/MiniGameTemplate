---
system: knowledge-engineering
scope: module-card-uisystem-fairygui
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/FairyGUI_UI.md
---

# Module Card: UISystem_FairyGUI

## 1. 模块职责

UISystem_FairyGUI 负责 FairyGUI 包加载、面板生命周期、SortingOrder、全屏/弹窗布局、遮罩、Binder 注册、IUIPanel 接口、面板打开/刷新/关闭，以及与 AppFlow 的 Suspend/Resume 配合。

## 2. 不负责什么

- 不负责 FairyGUI 编辑器源工程的设计规范本身，源工程在 `UIProject/`。
- 不负责业务 UI 的具体按钮逻辑，业务写在 `.Logic.cs` 或 Controller。
- 不负责战斗数据生产，UI 通过 SO 变量、事件和传入 data 消费数据。
- 不直接管理场景流转，导航交给 AppFlow。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `UIManager` | 面板生命周期与层级管理 |
| `IUIPanel` | 面板基础接口 |
| `IPanelSuspendable` | 面板挂起/恢复接口 |
| `UIPackageLoader` | FairyGUI 包加载 |
| `UIConstants` | SortingOrder 层级 |
| `XXXBinder` | FairyGUI 导出 Binder |
| `XXXPanel.Logic.cs` | 手写业务逻辑 |

## 4. 数据流

```text
FairyGUI Editor (UIProject)
  -> 发布到 UnityProj/Assets/_Game/FairyGUI_Export
  -> 生成 Binder / Panel partial class
  -> GameStartupFlow 注册 Binder
  -> UIManager.OpenPanel
  -> IUIPanel.OnOpen/OnRefresh/OnClose
  -> SO Variables / data 驱动 UI 刷新
  -> AppFlow Suspend/Resume 控制隐藏恢复
```

## 5. 生命周期

```text
Register Binder -> Load Package -> Open Panel -> OnOpen -> Refresh -> Suspend/Resume(optional) -> OnClose -> Dispose/Hide
```

## 6. 依赖关系

UISystem 是框架服务层，依赖 FairyGUI 和 AssetSystem。Game 层通过 Binder、Panel Logic、Controller 使用它。AppFlow 调用 UIManager 做面板编排。

## 7. 关键 SO / 配置路径

UI 通常消费：

```text
Assets/_Game/Configs/Variables/
Assets/_Game/Configs/Events/
Assets/_Game/Configs/ShooterGame/Variables/
Assets/_Game/FairyGUI_Export/
UIProject/assets/SG_*/
```

## 8. 关键 ADR

- ADR-034：AppFlow 栈式导航系统。
- 与启动 UI、隐私弹窗、Loading 层级相关内容见 `ARCHITECTURE.md` UI 工作流。

## 9. 热路径 / 性能约束

UI 不是战斗最热路径，但战斗 HUD 高频刷新应避免重复创建对象、重复绑定事件和不必要 Tween。微信小游戏上注意触摸区域、字体、贴图尺寸和包加载开销。

## 10. 常见错误

- 手改 FairyGUI 自动生成代码。
- OnRefresh 调用 OnOpen，导致事件重复绑定。
- Binder 未注册导致 CreateObject 失败。
- 弹窗 SortingOrder 低于 Loading 被遮挡。
- AppFlow Suspend 后面板未正确恢复。
- UIProject 发布后 Unity 资源未更新。

## 11. 修改前必读

- `CONTEXT_PACKS/FairyGUI_UI.md`
- `SHOOTER_GAME/TDD/SG_TDD_04_UI_CONTROLLERS.md`
- `SHOOTER_GAME/SG_UI_DESIGN.md`
- `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md`
- `skills/fairygui-tools/SKILL.md`
- `skills/fairygui-tools/references/pitfalls.md`

## 12. 修改后必验

- FairyGUI 包发布成功，Unity 中可加载。
- Binder 注册成功。
- Open/Refresh/Close 生命周期无重复事件。
- FullScreen、Dialog、Loading、Toast 层级正确。
- AppFlow Push/Pop/Suspend/Resume 后面板状态正确。
- 微信真机触摸和显示无错位。