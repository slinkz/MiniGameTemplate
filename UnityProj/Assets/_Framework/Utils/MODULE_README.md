# Utils 模块

## 用途
通用工具类集合，零外部依赖。供框架其他模块内部使用。

## 包含
| 类 | 用途 |
|---|------|
| `Singleton<T>` | 受限使用的MonoBehaviour单例基类。**仅限框架内部**，游戏逻辑禁用 |
| `CoroutineRunner` | 为非MonoBehaviour类提供协程启动能力 |
| `MathUtils` | 数学工具方法 |

## 使用原则
- 游戏逻辑**不应直接使用Singleton**，应通过SO事件/变量通信
- CoroutineRunner是框架内部设施，游戏逻辑优先用async/await或Timer模块
