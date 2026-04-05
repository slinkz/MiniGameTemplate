# EventSystem 模块

## 用途
基于 ScriptableObject 的事件通道系统。实现跨系统的解耦消息传递，替代直接组件引用和单例调用。

## 核心类
| 类 | 用途 |
|---|------|
| `GameEvent` | 无参事件通道 SO |
| `GameEventListener` | 无参事件监听器组件（挂在GameObject上，Inspector配置响应） |
| `GameEvent<T>` | 泛型事件通道基类 |
| `IntGameEvent` | int参数事件通道 |
| `FloatGameEvent` | float参数事件通道 |
| `StringGameEvent` | string参数事件通道 |
| `GameEventListener<T>` | 泛型监听器基类 |

## 使用方式
1. 右键 → Create → Events → Game Event 创建事件SO资产
2. 在发送方脚本中引用该SO，调用 `myEvent.Raise()`
3. 在接收方GameObject上挂 `GameEventListener`，Inspector中拖入同一个SO，配置UnityEvent响应

## 预置事件
- `OnGameStart` / `OnGamePause` / `OnGameResume` / `OnGameOver` — 无参GameEvent
- `OnScoreChanged` — IntGameEvent

## 原则
- **禁止**在代码中 new 事件对象，所有事件必须是项目中的 .asset 文件
- **禁止**跨系统的直接 GetComponent 引用，用事件通道替代
