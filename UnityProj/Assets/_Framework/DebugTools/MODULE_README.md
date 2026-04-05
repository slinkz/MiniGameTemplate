# DebugTools 模块

## 用途
运行时调试工具集合。所有工具在Release构建中自动禁用。

## 核心类
| 类 | 用途 |
|---|------|
| `FPSDisplay` | 左上角帧率显示 |
| `RuntimeSOViewer` | 查看SO变量实时值（编辑器专用） |
| `DebugConsole` | 简易运行时控制台，显示Debug.Log输出 |

## 使用方式
- 将 `FPSDisplay` 挂到场景任意GameObject上
- `DebugConsole` 通过摇一摇 / 多指点击 激活（可配置）
- `RuntimeSOViewer` 仅在Editor中生效

## 注意
- 所有调试代码包裹在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 中
- Release构建中零开销
