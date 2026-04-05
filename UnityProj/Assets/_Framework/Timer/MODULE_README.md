# Timer 模块

## 用途
不依赖 MonoBehaviour 的计时器服务。支持延迟调用、重复调用、暂停/恢复。

## 核心类
| 类 | 用途 |
|---|------|
| `TimerService` | 计时器服务（挂在场景中驱动所有计时器） |
| `TimerHandle` | 计时器句柄（取消、暂停、恢复单个计时器） |

## 使用方式
```csharp
// 延迟3秒后执行一次
var handle = TimerService.Instance.Delay(3f, () => Debug.Log("Done!"));

// 每1秒重复执行
var handle = TimerService.Instance.Repeat(1f, () => Debug.Log("Tick!"));

// 取消计时器
handle.Cancel();

// 暂停/恢复
handle.Pause();
handle.Resume();
```

## 注意
- 计时器受 Time.timeScale 影响（暂停游戏时自动冻结）
- 使用 `realTime: true` 参数创建不受timeScale影响的计时器
