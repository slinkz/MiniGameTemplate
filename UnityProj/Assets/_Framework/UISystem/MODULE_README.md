# UISystem 模块

## 用途
基于 FairyGUI 的 UI 管理系统。负责 UI 包加载、面板生命周期管理、层级控制。

## 核心类
| 类 | 用途 |
|---|------|
| `UIManager` | UI面板打开/关闭/层级管理（Singleton，框架内部） |
| `UIPackageLoader` | FairyGUI包加载管理 |
| `UIBase` | 所有UI面板的基类 |
| `UIDialogBase` | 弹窗/对话框基类（在UIBase上增加遮罩和关闭逻辑） |
| `UIConstants` | UI常量定义（包名、组件名），避免魔法字符串 |

## 使用方式
```csharp
// 打开一个面板
UIManager.Instance.OpenPanel<MainMenuPanel>();

// 关闭面板
UIManager.Instance.ClosePanel<MainMenuPanel>();
```

## 自定义面板
继承 `UIBase`，重写 `OnOpen()` / `OnClose()` / `OnRefresh()`。

## FairyGUI 工程
FairyGUI编辑器工程文件放在 `FairyGUIProject/` 下（可选），导出资源放 `Resources/`。

## 注意
- 面板类与FairyGUI包/组件的映射通过 `UIConstants` 管理
- 禁止在UI脚本中直接引用其他系统，通过SO事件通信
