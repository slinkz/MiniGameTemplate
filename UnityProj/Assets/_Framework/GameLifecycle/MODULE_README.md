# GameLifecycle 模块

## 用途
游戏启动流程编排和场景管理。负责从 Boot 场景开始的完整初始化链路。

## 核心类
| 类 | 用途 |
|---|------|
| `GameBootstrapper` | 启动入口，挂在Boot场景的唯一GameObject上。按顺序初始化所有系统 |
| `SceneLoader` | 基于SO配置的场景加载器，优先走 AssetService(YooAsset)，自动回退到 SceneManager |
| `SceneDefinition` | [CreateAssetMenu] 场景定义SO（场景名、YooAsset资源路径、是否附加加载等） |
| `GameConfig` | [CreateAssetMenu] 全局游戏配置SO（版本号、初始场景等） |
| `GameStateController` | 连接FSM与游戏生命周期事件 |

## 启动流程
```
Boot场景加载 → GameBootstrapper.Awake()
  → 初始化 AssetService（YooAsset，必须在前）
  → 初始化 ConfigManager（Luban配置表，异步）
  → 初始化 TimerService
  → 初始化 AudioManager（Singleton自动创建）
  → 初始化 UIManager（FairyGUI）
  → 初始化 PoolManager
  → 加载主场景（通过 SceneLoader → AssetService）
```

## 场景管理
```csharp
[SerializeField] private SceneDefinition _gameScene;

void LoadGameScene() {
    SceneLoader.Instance.LoadScene(_gameScene);
}
```

### SceneDefinition 配置
- **SceneName**：Build Settings 中的场景名（SceneManager 回退用）
- **ScenePath**：YooAsset 资源路径，如 `Assets/Scenes/GameScene.unity`（为空时走 SceneManager）
- **IsAdditive**：是否叠加加载

## 注意
- Boot场景**必须**在Build Settings中排第一位
- Boot场景仅包含GameBootstrapper和必要的DontDestroyOnLoad对象
- 不要在Boot场景中放任何游戏内容
- 所有系统初始化均为异步（async），确保WebGL平台兼容性
