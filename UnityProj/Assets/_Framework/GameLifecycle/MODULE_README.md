# GameLifecycle 模块

## 用途
游戏启动流程编排和场景管理。负责从 Boot 场景开始的完整初始化链路。

## 核心类
| 类 | 用途 |
|---|------|
| `GameBootstrapper` | 启动入口，挂在Boot场景的唯一GameObject上。按顺序初始化所有系统 |
| `SceneLoader` | 基于SO配置的场景加载器，支持异步加载+过渡 |
| `SceneDefinition` | [CreateAssetMenu] 场景定义SO（场景名、是否附加加载等） |
| `GameConfig` | [CreateAssetMenu] 全局游戏配置SO（版本号、初始场景等） |
| `GameStateController` | 连接FSM与游戏生命周期事件 |

## 启动流程
```
Boot场景加载 → GameBootstrapper.Awake()
  → 初始化 ConfigManager（配置表）
  → 初始化 AudioManager
  → 初始化 UIManager
  → 初始化 TimerService
  → 初始化 PoolManager
  → 加载主场景（通过 SceneLoader）
```

## 场景管理
```csharp
[SerializeField] private SceneDefinition _gameScene;

void LoadGameScene() {
    SceneLoader.Instance.LoadScene(_gameScene);
}
```

## 注意
- Boot场景**必须**在Build Settings中排第一位
- Boot场景仅包含GameBootstrapper和必要的DontDestroyOnLoad对象
- 不要在Boot场景中放任何游戏内容
