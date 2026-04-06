# UISystem 模块

## 用途
基于 FairyGUI 的 UI 管理系统。负责 UI 包加载、面板生命周期管理、层级控制。

## 核心类
| 类 | 用途 |
|---|------|
| `UIManager` | UI面板打开/关闭/层级管理（Singleton，框架内部） |
| `UIPackageLoader` | FairyGUI包加载管理（YooAsset only，无 Resources fallback） |
| `UIBase` | 所有UI面板的基类 |
| `UIDialogBase` | 弹窗/对话框基类（在UIBase上增加遮罩和关闭逻辑） |
| `UIConstants` | UI常量定义（包名、组件名），避免魔法字符串 |

## 使用方式
```csharp
// 打开一个面板（异步，通过 YooAsset 加载）
await UIManager.Instance.OpenPanelAsync<MainMenuPanel>();

// 关闭面板
UIManager.Instance.ClosePanel<MainMenuPanel>();
```

## 自定义面板
继承 `UIBase`，重写 `OnOpen()` / `OnClose()` / `OnRefresh()`。

## 资源加载
- **所有 FairyGUI 包通过 YooAsset 异步加载**，无 Resources.Load fallback
- 编辑器中使用 YooAsset EditorSimulate 模式，无需构建 AB
- `AssetService` 必须在 UI 加载前完成初始化（由 GameBootstrapper 保证）
- `AddPackageAsync` 有并发保护：同一个包不会被两个 async 调用重复加载
- `OpenPanelAsync` 采用"成功后注册"策略：加载失败的面板不会残留在活跃列表中
- **非编辑器构建**中 FairyGUI 资源缓存未命中会输出 Error 日志（编辑器中为 Warning）

## FairyGUI 工程
FairyGUI 编辑器工程在仓库根目录 `UIProject/`，导出资源到 `Assets/_Game/FairyGUI_Export/`。

## 注意
- 面板类与 FairyGUI 包/组件的映射通过 `UIConstants` 管理
- 禁止在 UI 脚本中直接引用其他系统，通过 SO 事件通信
- **禁止使用 Resources.Load** — 所有资源通过 YooAsset 加载
