# UISystem 模块

## 用途
基于 FairyGUI Extension 机制的 UI 管理系统。负责 UI 包加载、面板生命周期管理、层级控制。

## 核心类
| 类 | 用途 |
|---|------|
| `UIManager` | UI 面板打开/关闭/层级管理（Singleton，框架内部） |
| `UIPackageLoader` | FairyGUI 包加载管理（YooAsset only，无 Resources fallback） |
| `IUIPanel` | 面板生命周期接口（`OnOpen`/`OnClose`/`OnRefresh`/`PanelSortOrder`/`IsFullScreen`/`PanelPackageName`） |
| `IModalDialog` | 对话框接口（`CloseOnClickOutside`），UIManager 自动创建半透明遮罩 |
| `UIConstants` | UI 层级常量定义（`LAYER_BACKGROUND` ~ `LAYER_LOADING`） |
| `FairySpineHelper` | FairyGUI 的 Spine（GLoader3D）播放辅助，统一开关与调用方式 |

## 架构模式

采用 **FairyGUI 原生 Extension 机制**：
1. FairyGUI 编辑器启用 `genCode="true"` 导出 C# 代码（`XXXPanel.cs` + `XXXBinder.cs`）
2. 手写 `XXXPanel.Logic.cs` 作为 `partial class`，实现 `IUIPanel` 接口
3. `UIManager.RegisterBinder()` 注册 Binder，`UIManager.OpenPanelAsync<T>()` 创建面板
4. 命名空间 = FairyGUI 包名（UIManager 通过 `type.Namespace` 推导包名）

## 使用方式
```csharp
// 注册 Binder（启动时调用一次）
UIManager.RegisterBinder("Common", Common.CommonBinder.BindAll);

// 打开面板（async，T 必须是 GComponent + IUIPanel）
await UIManager.Instance.OpenPanelAsync<Common.LoadingPanel>(optionalData);

// 关闭面板
UIManager.Instance.ClosePanel<Common.LoadingPanel>();
```

## 自定义面板

创建 `XXXPanel.Logic.cs`，实现 `IUIPanel` 接口：
- `PanelSortOrder`：层级顺序
- `IsFullScreen`：是否全屏（false = 居中弹窗）
- `PanelPackageName`：FairyGUI 包名
- `OnOpen(data)`：面板创建后调用，绑定事件
- `OnClose()`：面板关闭前调用，清理资源
- `OnRefresh(data)`：面板已打开时再次调用 `OpenPanelAsync`，刷新数据（**不要在 OnRefresh 中调 OnOpen，避免事件双绑定**）

对话框额外实现 `IModalDialog.CloseOnClickOutside`。

## 并发安全
- `OpenPanelAsync`：同一面板类型有并发防护（`_pendingOpens` HashSet），重复调用被忽略
- `AddPackageAsync`：同一包有加载防护（`_loading` HashSet），避免重复加载
- `CloseAllPanels`：快照模式（`_closeBuffer`），避免迭代中修改字典

## 资源加载
- **所有 FairyGUI 包通过 YooAsset 异步加载**，无 Resources.Load fallback
- 编辑器中使用 YooAsset EditorSimulate 模式，无需构建 AB
- `AssetService` 必须在 UI 加载前完成初始化（由 GameBootstrapper 保证）

## FairyGUI 工程
FairyGUI 编辑器工程在仓库根目录 `UIProject/`，导出资源到 `Assets/_Game/FairyGUI_Export/`。

## Spine（可选）
- 集成模式：源码子模块（`ThirdParty/spine-runtimes`）+ 目录链接（`Assets/Spine` 与 `Assets/SpineCSharp`）
- 开关方式：`FAIRYGUI_SPINE`（同时建议保留 `ENABLE_SPINE` 作为模板级标识）
- 菜单工具：`Tools -> MiniGame Template -> Integrations -> Spine`

## 注意
- FairyGUI 导出的 `*.cs` 禁止手动修改（会被重新导出覆盖）
- 业务逻辑全部写在 `*.Logic.cs` 中
- 禁止在 UI 脚本中直接引用其他系统，通过 SO 事件通信
- **禁止使用 Resources.Load** — 所有资源通过 YooAsset 加载
