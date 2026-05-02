# 框架模块使用手册 — Part 1：核心模块

> EventSystem · DataSystem · GameLifecycle · UISystem

---

## 1. EventSystem — 事件通道

**用途**：让组件之间传递消息，而不需要互相引用。

**位置**：`Assets/_Framework/EventSystem/`

### 创建事件

右键 Project → Create → MiniGameTemplate → Events，选择类型：

| 类型 | 用途 | 示例 |
|------|------|------|
| `GameEvent` | 无参事件 | "游戏开始"、"游戏结束" |
| `IntGameEvent` | 带 int 参数 | "分数变化"（传递新分数） |
| `FloatGameEvent` | 带 float 参数 | "血量变化"（传递新血量） |
| `StringGameEvent` | 带 string 参数 | "系统消息"（传递消息文本） |

### 代码中发送事件

```csharp
[SerializeField] private GameEvent _onGameOver;

void TriggerGameOver()
{
    _onGameOver.Raise(); // 通知所有监听者
}
```

### 代码中监听事件（方式一：代码订阅）

```csharp
[SerializeField] private GameEvent _onGameOver;

// ⚠️ 注册和注销必须配对，放在 OnEnable/OnDisable 中
void OnEnable()  { _onGameOver.RegisterListener(this); }
void OnDisable() { _onGameOver.UnregisterListener(this); }

public void OnEventRaised()
{
    // 处理游戏结束
}
```

### 监听事件（方式二：Inspector 配置）

1. 给 GameObject 添加 `GameEventListener` 组件
2. 拖入事件 SO 到 `Event` 字段
3. 在 `Response` 的 UnityEvent 中配置要调用的方法

这种方式适合策划同学直接在 Inspector 中配置逻辑，不需要写代码。

### 注意事项

- **不要在代码中 `new` 事件**，所有事件必须是 `.asset` 文件
- 注册/注销**必须**在 `OnEnable`/`OnDisable` 中配对。不要放在 `Start`/`OnDestroy` 中（可能导致 MissingReferenceException）

---

## 2. DataSystem — 数据管理

**用途**：管理游戏运行时数据，包括四个子模块。

**位置**：`Assets/_Framework/DataSystem/`

### 2.1 Variables（SO 变量）

替代静态字段和单例来共享数据。

**创建**：右键 → Create → MiniGameTemplate → Variables → 选择类型

| 类型 | 用途 |
|------|------|
| `IntVariable` | 整数值（分数、等级、计数） |
| `FloatVariable` | 浮点值（血量、进度、音量） |
| `BoolVariable` | 开关状态（是否暂停、是否解锁） |
| `StringVariable` | 文本值（玩家名称、系统消息） |

**读写数据：**

```csharp
[SerializeField] private IntVariable _playerScore;

// 设置值
_playerScore.SetValue(100);

// 增减值
_playerScore.ApplyChange(10);  // +10

// 读取值
int current = _playerScore.Value;

// 重置为 Inspector 中设定的初始值
_playerScore.ResetToInitial();
```

**监听数据变化：**

```csharp
void OnEnable()
{
    _playerScore.OnValueChanged += OnScoreChanged;
}

void OnDisable()
{
    _playerScore.OnValueChanged -= OnScoreChanged;
}

void OnScoreChanged(int newValue)
{
    // 更新 UI 等
}
```

### 2.2 RuntimeSets（运行时集合）

追踪场景中的活跃对象，替代 `FindObjectOfType()`。

```csharp
// 获取第一个敌人（替代 FindObjectOfType<Enemy>()）
Transform firstEnemy = _enemySet.GetFirst();

// 遍历所有敌人
foreach (var enemy in _enemySet.Items)
{
    // ...
}
```

被跟踪的对象需要挂上 `RuntimeSetRegistrar` 组件，它会在 OnEnable/OnDisable 时自动注册/移除。

### 2.3 Persistence（持久化存储）

所有本地存储通过 `ISaveSystem` 接口操作，具体实现是 `PlayerPrefsSaveSystem`。

```csharp
// ✅ 正确：使用全局唯一实例
var save = GameBootstrapper.SaveSystem;
save.SaveInt("high_score", 42);
save.SaveString("player_name", "Player1");
save.Save(); // 刷新到磁盘

// 读取
int score = save.LoadInt("high_score", 0); // 第二个参数是默认值
```

> ⚠️ **不要** `new PlayerPrefsSaveSystem()`。使用 `GameBootstrapper.SaveSystem` 保证全局唯一实例，框架会在应用暂停/退出时自动刷新数据。

### 2.4 Config（配置表）

基于 Luban v4.6.0 的配置数据系统，使用 **Binary ByteBuf** 格式，采用 **Lazy Deserialization（延迟反序列化）** 策略。

**加载机制**：
1. `ConfigManager.InitializeAsync()` 启动时通过 YooAsset **异步预加载**全部 `.bytes` 到内存缓存（仅 I/O，速度很快）
2. **不在启动时反序列化任何表**——反序列化推迟到首次访问表属性时自动执行
3. 反序列化后自动调用 `ResolveRef()`，并释放原始 `byte[]` 缓存以节省内存

> 💡 **对使用者的影响：无。** API 完全不变，`ConfigManager.Tables.TbItem.Get(1001)` 照常使用。

运行时通过 YooAsset 加载 `.bytes` 二进制文件（编辑器使用 EditorSimulate 模式，无需构建 AB）。编辑器下额外生成 JSON 预览文件（`Editor/ConfigPreview/`，不打包）。

```csharp
// 配置表在 GameBootstrapper 中已经初始化（bytes 预加载完成）
// 直接使用 ConfigManager.Tables 访问数据（首次访问时自动反序列化）
var itemConfig = ConfigManager.Tables.TbItem.Get(1001);
Debug.Log(itemConfig.Name);

// 辅助方法：查询某表是否已反序列化
bool loaded = ConfigManager.IsTableLoaded("tbitem");
```

---

## 3. GameLifecycle — 启动与场景管理

**用途**：编排游戏启动流程，管理场景加载。

**位置**：`Assets/_Framework/GameLifecycle/`

### GameBootstrapper

挂在 Boot 场景的唯一 GameObject 上。`Awake()` 中按依赖顺序初始化所有系统。**你不需要修改它**，只需要在 Inspector 中配置 `GameConfig` 和 `AssetConfig`。

### IStartupFlow — 游戏启动编排

框架提供了 `IStartupFlow` 接口（`_Framework/GameLifecycle/`），让游戏层在系统初始化完成后、场景加载前插入自定义启动逻辑（如加载界面、隐私授权、公告弹窗等）。

```csharp
public interface IStartupFlow
{
    Task RunAsync(GameConfig gameConfig);
}
```

**使用方式**：在 Game 层创建实现类，`GameBootstrapper` 会在所有系统初始化后自动调用。

```csharp
// Assets/_Game/Scripts/GameStartupFlow.cs
public class GameStartupFlow : IStartupFlow
{
    public async Task RunAsync(GameConfig gameConfig)
    {
        // Phase 1: 显示 LoadingPanel，模拟加载进度
        await UIManager.Instance.OpenPanelAsync<LoadingPanel>();

        // Phase 2: 检查隐私授权
        await CheckPrivacyAsync();

        // Phase 3: 关闭 Loading，打开主菜单
        UIManager.Instance.ClosePanel<LoadingPanel>();
        await UIManager.Instance.OpenPanelAsync<MainMenuPanel>();
    }
}
```

> 💡 如果 `IStartupFlow.RunAsync()` 抛出 `OperationCanceledException`（如用户拒绝隐私授权），`GameBootstrapper` 会将其视为非致命错误并继续（不会 crash）。

### SceneLoader

基于 SceneDefinition SO 的场景加载器。

```csharp
[SerializeField] private SceneDefinition _gameScene;

void LoadGameScene()
{
    SceneLoader.Instance.LoadScene(_gameScene);
}
```

### SceneDefinition

右键 → Create → MiniGameTemplate → Core → Scene Definition

在 Inspector 中配置：
- **SceneName**：Build Settings 中的场景名（用于 SceneManager 回退）
- **ScenePath**：YooAsset 资源路径（如 `Assets/Scenes/GameScene.unity`）
- **IsAdditive**：是否叠加加载

### GameConfig

右键 → Create → MiniGameTemplate → Core → Game Config

配置游戏名称、版本号、目标帧率、初始场景等。

---

## 4. UISystem — UI 管理 (FairyGUI)

**用途**：管理 FairyGUI 面板的生命周期，基于 FairyGUI 原生 Extension 机制。

**位置**：`Assets/_Framework/UISystem/`

### 架构概述

> 📐 Agent/AI 开发者请参见 [Agent/CONVENTIONS.md — FairyGUI 面板规范](../Agent/CONVENTIONS.md#agent-fairygui-面板规范强制--extension--iuipanel-模式)，包含强制规则清单和提交检查项。

1. FairyGUI 编辑器启用 `genCode="true"` 自动导出 C# 代码（`XXXPanel.cs` + `XXXBinder.cs`）
2. 手写 `XXXPanel.Logic.cs` 作为 `partial class`，实现 `IUIPanel` 接口
3. UIManager 通过 Binder 注册 Extension，`OpenPanelAsync` 创建面板实例

### 创建自定义面板

1. 在 FairyGUI 编辑器中创建 UI 组件
   - 当前模板内置包：`Common`（通用弹窗/加载）、`MainMenu`（主菜单）、`Example`（示例玩法）
2. 确保 `package.xml` 中 `<publish>` 含 `genCode="true"`
3. FairyGUI 导出代码到 `_Game/Scripts/UI/<PackageName>/`（自动生成，不要手改）
4. 创建业务逻辑文件 `XXXPanel.Logic.cs`：

```csharp
using MiniGameTemplate.UI;

namespace MainMenu
{
    public partial class MainMenuPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "MainMenu";

        public void OnOpen(object data)
        {
            if (btnStart != null) btnStart.onClick.Add(OnStartClicked);
            ApplyData(data);
        }

        public void OnClose() { }

        public void OnRefresh(object data)
        {
            ApplyData(data);
        }

        private void ApplyData(object data) { }
        private void OnStartClicked() { }
    }
}
```

5. 在 `GameStartupFlow.RunAsync` 中注册 Binder：
```csharp
UIManager.RegisterBinder("MainMenu", MainMenu.MainMenuBinder.BindAll);
```

### 打开/关闭面板

```csharp
// 打开（如果已打开则调用 OnRefresh）
await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>();

// 打开时传入数据
await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>(someData);

// 关闭
UIManager.Instance.ClosePanel<MainMenu.MainMenuPanel>();

// 关闭所有面板（场景切换时调用）
UIManager.Instance.CloseAllPanels();
```

### 加载 FairyGUI 包

```csharp
// ✅ 异步加载（WebGL 必须用这个）
await UIPackageLoader.AddPackageAsync("CommonUI");

// 用完后移除包
UIPackageLoader.RemovePackage("CommonUI");
```

> ⚠️ WebGL 环境下**禁止**使用同步 `AddPackage()`，因为它走 `Resources.Load`，不走 YooAsset。

### FairyGUI 显示 Spine（可选）

前置条件：
1. 运行 `UnityProj/Tools/setup_spine.bat` 或 `.sh`
2. 在 Unity 菜单启用 `Tools -> MiniGame Template -> Integrations -> Spine -> Enable Spine`

示例：
```csharp
FairySpineHelper.TryPlaySpine(root, "role3d", "idle", loop: true);
```

### 对话框 (IModalDialog)

实现 `IModalDialog` 接口的面板会自动获得半透明遮罩和可选的点击外部关闭功能。

- `IsFullScreen = false`：对话框保持原始尺寸并居中显示
- 自动创建半透明遮罩（SortOrder - 1）
- 通过 `CloseOnClickOutside` 控制点击遮罩是否关闭

### UI 层级系统

所有 UI 面板通过 `PanelSortOrder` 属性控制显示层级，常量定义在 `UIConstants.cs`，范围从 `LAYER_BACKGROUND(0)` 到 `LAYER_LOADING(600)`。

> ⚠️ **常见坑**：LoadingPanel 显示期间弹出对话框，对话框的 `PanelSortOrder` 必须 > 600，否则被遮挡。
