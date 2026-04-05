# 框架模块使用手册

> 按需查阅。每个模块独立成节，你只需要读用到的部分。

---

## 目录

1. [EventSystem — 事件通道](#1-eventsystem--事件通道)
2. [DataSystem — 数据管理](#2-datasystem--数据管理)
3. [GameLifecycle — 启动与场景管理](#3-gamelifecycle--启动与场景管理)
4. [UISystem — UI 管理 (FairyGUI)](#4-uisystem--ui-管理-fairygui)
5. [AudioSystem — 音频管理](#5-audiosystem--音频管理)
6. [AssetSystem — 资源管理 (YooAsset)](#6-assetsystem--资源管理-yooasset)
7. [Timer — 计时器](#7-timer--计时器)
8. [ObjectPool — 对象池](#8-objectpool--对象池)
9. [FSM — 有限状态机](#9-fsm--有限状态机)
10. [WeChatBridge — 微信 SDK 桥接](#10-wechatbridge--微信-sdk-桥接)
11. [DebugTools — 调试工具](#11-debugtools--调试工具)
12. [Utils — 通用工具](#12-utils--通用工具)
13. [Editor 工具](#13-editor-工具)

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

基于 Luban 的配置数据系统。生成的代码和数据由 `ConfigManager` 管理。

```csharp
// 配置表在 GameBootstrapper 中已经初始化
// 直接使用生成的 Tables 类访问数据
var itemConfig = Tables.Instance.TbItem.Get(1001);
Debug.Log(itemConfig.Name);
```

---

## 3. GameLifecycle — 启动与场景管理

**用途**：编排游戏启动流程，管理场景加载。

**位置**：`Assets/_Framework/GameLifecycle/`

### GameBootstrapper

挂在 Boot 场景的唯一 GameObject 上。`Awake()` 中按依赖顺序初始化所有系统。**你不需要修改它**，只需要在 Inspector 中配置 `GameConfig` 和 `AssetConfig`。

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

**用途**：管理 FairyGUI 面板的生命周期。

**位置**：`Assets/_Framework/UISystem/`

### 创建自定义面板

1. 在 FairyGUI 编辑器中创建 UI 组件
2. 在 `UIConstants.cs` 中添加包名和组件名常量
3. 创建面板类：

```csharp
using MiniGameTemplate.UI;

public class MainMenuPanel : UIBase
{
    protected override string PackageName => UIConstants.PKG_MAIN_MENU;
    protected override string ComponentName => UIConstants.COMP_MAIN_PANEL;

    protected override void OnInit()
    {
        // 首次创建时调用。绑定 UI 元素事件。
    }

    protected override void OnOpen(object data)
    {
        // 每次打开面板时调用。data 是可选的传入参数。
    }

    protected override void OnClose()
    {
        // 面板关闭时调用。清理资源。
    }
}
```

### 打开/关闭面板

```csharp
// 打开（如果已打开则刷新）
UIManager.Instance.OpenPanel<MainMenuPanel>();

// 打开时传入数据
UIManager.Instance.OpenPanel<MainMenuPanel>(someData);

// 关闭
UIManager.Instance.ClosePanel<MainMenuPanel>();

// 检查是否打开
bool isOpen = UIManager.Instance.IsPanelOpen<MainMenuPanel>();

// 获取已打开的面板实例
var panel = UIManager.Instance.GetPanel<MainMenuPanel>();

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

### UIDialogBase

弹窗的基类，在 UIBase 基础上增加了半透明遮罩和点击遮罩关闭的功能。

---

## 5. AudioSystem — 音频管理

**用途**：播放 BGM 和 SFX，音量通过 SO 变量驱动。

**位置**：`Assets/_Framework/AudioSystem/`

### 配置音效

1. 右键 → Create → MiniGameTemplate → Audio → Audio Clip，创建 `AudioClipSO`
2. 在 Inspector 中拖入音频文件，调整音量和音调

或者创建一个 `AudioLibrary`（按 key 索引的音效集合），把多个 `AudioClipSO` 集中管理。

### 播放音效

```csharp
// 方式一：直接引用 AudioClipSO
[SerializeField] private AudioClipSO _clickSound;

void OnClick()
{
    AudioManager.Instance.PlaySFX(_clickSound);
}

// 方式二：通过 AudioLibrary 按 key 播放
AudioManager.Instance.PlaySFX("click");
```

### BGM 控制

```csharp
[SerializeField] private AudioClipSO _bgm;

void Start()
{
    AudioManager.Instance.PlayBGM(_bgm);
}

void StopMusic()
{
    AudioManager.Instance.StopBGM();
}
```

### 音量控制

音量使用 `FloatVariable` SO 驱动：
- `MasterVolume` — 主音量
- `BGMVolume` — 背景音乐音量
- `SFXVolume` — 音效音量

只需要在 UI 滑块上绑定对应的 FloatVariable 即可实现音量调节。

### SFX 通道池

SFX 使用 AudioSource 池化方案（默认 4 个通道，round-robin 分配），支持多个音效同时播放。在 AudioManager Inspector 中可以调整 `SFX Pool Size`。

---

## 6. AssetSystem — 资源管理 (YooAsset)

**用途**：封装 YooAsset，提供统一的资源加载 API。

**位置**：`Assets/_Framework/AssetSystem/`

### 运行模式

在 `AssetConfig` SO 的 Inspector 中选择：

| 模式 | 用途 | 何时使用 |
|------|------|----------|
| **EditorSimulate** | 直接从 AssetDatabase 加载 | 编辑器中开发时 |
| **Offline** | 从 StreamingAssets 中的 Bundle 加载 | 离线发布/首包 |
| **Host** | 从远程 CDN 加载 + 本地缓存 | 线上热更新 |
| **WebGL** | 微信小游戏专用文件系统 | 微信小游戏发布 |

### 加载资源

```csharp
// 加载预制件
var handle = AssetService.Instance.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemy.prefab");
await handle.Task;
var prefab = handle.AssetObject as GameObject;
var instance = Object.Instantiate(prefab);

// ⚠️ 用完后必须释放 handle，否则资源无法卸载
handle.Release();
```

### 加载场景

```csharp
var sceneHandle = AssetService.Instance.LoadSceneAsync("Assets/Scenes/GameScene.unity");
await sceneHandle.Task;
```

### 热更新（Host 模式）

```csharp
// 1. 请求最新版本号
var version = await AssetService.Instance.RequestPackageVersionAsync();

// 2. 更新清单
await AssetService.Instance.UpdatePackageManifestAsync(version);

// 3. 下载需要更新的资源
var downloader = AssetService.Instance.CreateResourceDownloader();
if (downloader != null)
{
    downloader.BeginDownload();
    await downloader.Task;
}
```

### 内存清理

```csharp
// 场景切换后调用，卸载未使用的资源
AssetService.Instance.UnloadUnusedAssets();

// 完全重置（慎用）
AssetService.Instance.ForceUnloadAllAssets();
```

---

## 7. Timer — 计时器

**用途**：不依赖 MonoBehaviour 的计时器服务。

**位置**：`Assets/_Framework/Timer/`

### 延迟调用（一次性）

```csharp
// 3 秒后执行
var handle = TimerService.Instance.Delay(3f, () => {
    Debug.Log("3 seconds passed!");
});
```

### 重复调用

```csharp
// 每 0.5 秒执行一次
var handle = TimerService.Instance.Repeat(0.5f, () => {
    Debug.Log("Tick!");
});
```

### 控制计时器

```csharp
// 取消
TimerService.Instance.Cancel(handle);

// 暂停/恢复
TimerService.Instance.Pause(handle);
TimerService.Instance.Resume(handle);

// 检查是否还在运行
bool active = TimerService.Instance.IsActive(handle);

// 获取剩余时间
float remaining = TimerService.Instance.GetRemaining(handle);
```

### 不受 TimeScale 影响

```csharp
// realTime: true → 不受 Time.timeScale 影响（暂停菜单中也会计时）
var handle = TimerService.Instance.Delay(3f, callback, realTime: true);
```

> ⚠️ 持有 `TimerHandle` 的组件必须在 `OnDisable` 中 Cancel 计时器，防止组件销毁后回调触发空引用。

---

## 8. ObjectPool — 对象池

**用途**：避免频繁创建/销毁 GameObject 导致的 GC 开销。

**位置**：`Assets/_Framework/ObjectPool/`

### 创建池定义

右键 → Create → MiniGameTemplate → Pool → Pool Definition

在 Inspector 中配置：
- **Prefab**：要池化的预制件
- **Initial Size**：预热数量（提前创建好放在池中）
- **Max Size**：最大数量（0 = 无限制）

### 使用对象池

```csharp
[SerializeField] private PoolDefinition _bulletPoolDef;

void Shoot()
{
    // 从池中获取
    var bullet = PoolManager.Instance.Get(_bulletPoolDef);
    bullet.transform.position = spawnPoint.position;
}

void OnBulletHit(GameObject bullet)
{
    // 归还到池中（不是 Destroy！）
    PoolManager.Instance.Return(_bulletPoolDef, bullet);
}
```

### 自动延时回收

给池化对象挂上 `PooledObject` 组件，可以配置自动延时回收（比如粒子特效播放完毕后自动归还）。

---

## 9. FSM — 有限状态机

**用途**：管理游戏全局状态流转（菜单 → 游戏中 → 暂停 → 结束）。

**位置**：`Assets/_Framework/FSM/`

### 创建状态和转换

1. 右键 → Create → MiniGameTemplate → FSM → State，创建状态 SO
2. 右键 → Create → MiniGameTemplate → FSM → State Transition，创建转换规则
3. 在转换 SO 中配置 FromState 和 ToState

### 设置状态机

1. 给 GameObject 添加 `StateMachine` 组件
2. 在 Inspector 中设置 Initial State
3. 拖入所有 Valid Transitions

### 代码中切换状态

```csharp
[SerializeField] private StateMachine _gameFSM;
[SerializeField] private State _playingState;
[SerializeField] private State _menuState;

void StartGame()
{
    // 有转换验证——只有配置了对应转换规则才能成功
    bool success = _gameFSM.TransitionTo(_playingState);
}

void ResetToMenu()
{
    // 强制切换，跳过验证（用于重置/重启）
    _gameFSM.ForceTransitionTo(_menuState);
}
```

### 监听状态变化

```csharp
_gameFSM.OnStateChanged += (previousState, newState) => {
    Debug.Log($"State changed: {previousState?.name} → {newState.name}");
};
```

### 状态事件

每个 State SO 可以配置 OnEnter 和 OnExit 事件（GameEvent）。进入/离开状态时自动 Raise，方便在 Inspector 中配置响应。

---

## 10. WeChatBridge — 微信 SDK 桥接

**用途**：统一的微信小游戏 SDK 接口，开发/测试时使用桩实现，发布时替换为真实实现。

**位置**：`Assets/_Framework/WeChatBridge/`

### 使用方式

```csharp
// 获取桥接实例（工厂自动选择桩/真实实现）
var wx = WeChatBridgeFactory.Create();

// 广告
wx.PreloadRewardedAd();
wx.ShowRewardedAd(success => {
    if (success) GiveReward();
});

// 分享
wx.Share("我的小游戏", imageUrl, "score=100");

// 排行榜
wx.SubmitScore(playerScore);
wx.ShowRankingPanel();

// 登录
wx.Login((success, code) => {
    if (success) SendCodeToServer(code);
});

// 前后台切换
wx.OnShow(query => HandleResume());
wx.OnHide(() => PauseGame());

// 系统工具
wx.Vibrate();
wx.SetClipboardData("复制的文本");
```

### 桩实现的行为

在 Editor 中运行时，`WeChatBridgeStub` 模拟所有 SDK 调用：
- 广告回调延迟 1.5 秒后返回 true
- 登录回调延迟 0.5 秒后返回模拟 code
- 其他调用打印日志

### 接入真实 SDK

详见 [Docs/Agent/WECHAT_INTEGRATION.md](../Agent/WECHAT_INTEGRATION.md)。简要步骤：

1. 导入微信 WX-WASM-SDK-V2
2. 创建 `WeChatBridgeImpl` 实现 `IWeChatBridge`
3. 在 `WeChatBridgeFactory` 中注册

---

## 11. DebugTools — 调试工具

**用途**：运行时调试辅助。Release 构建中自动禁用。

**位置**：`Assets/_Framework/DebugTools/`

| 工具 | 用途 | 激活方式 |
|------|------|----------|
| `FPSDisplay` | 左上角帧率显示 | 挂到场景任意 GameObject |
| `RuntimeSOViewer` | 查看 SO 变量实时值 | 仅 Editor |
| `DebugConsole` | 简易运行时控制台 | 多指点击/摇一摇 |

所有调试代码包裹在 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 中，Release 构建零开销。

---

## 12. Utils — 通用工具

**位置**：`Assets/_Framework/Utils/`

| 工具 | 用途 | 注意事项 |
|------|------|----------|
| `Singleton<T>` | MonoBehaviour 单例基类 | **仅限框架内部使用**，游戏代码禁用 |
| `GameLog` | 日志工具 | Release 构建自动剥离（`[Conditional]` 编译） |
| `CoroutineRunner` | 为非 MonoBehaviour 类提供协程能力 | 框架内部设施 |
| `MathUtils` | 数学工具方法 | 通用 |

### GameLog 使用

```csharp
// ✅ 日常调试——Release 中自动消失（包括字符串拼接的开销）
GameLog.Log("[MySystem] Something happened");
GameLog.LogWarning("[MySystem] Something suspicious");

// ✅ 致命错误——Release 中仍然可见
Debug.LogError("[MySystem] FATAL: Initialization failed");
```

---

## 13. Editor 工具

**位置**：`Assets/_Framework/Editor/`

通过 Unity 菜单 `Tools → MiniGame Template` 访问：

| 工具 | 菜单位置 | 用途 |
|------|----------|------|
| **架构验证** | Validate → Architecture Check | 检查代码是否违反架构规范（超行数、禁止 API、缺少 MODULE_README） |
| **资源审计** | Validate → Asset Audit | 检查纹理尺寸、音频格式、材质复杂度等 |
| **SO 创建向导** | Create → SO Creation Wizard | 可视化界面创建各种 SO 资产 |
| **一键构建** | Build → Build WebGL (Release) | 自动配置 PlayerSettings 并构建 WebGL |
| **打开构建目录** | Build → Open Build Folder | 快速定位到构建输出目录 |
| **SO 运行时调试** | Debug → SO Runtime Viewer | 运行时查看所有 SO 变量的当前值 |

### 资源导入规范自动化

模板包含 `AssetImportEnforcer`（AssetPostprocessor），自动执行以下规则：

| 资源类型 | 自动处理 |
|----------|----------|
| 纹理 | WebGL 平台最大 1024px |
| 音频 | WebGL 平台强制 Vorbis 压缩、50% 质量、短音效强制 Mono |

你不需要手动设置这些，导入资源时自动应用。
