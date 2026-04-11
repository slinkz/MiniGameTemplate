# 示例游戏代码解读

> **预计阅读**：20 分钟 &nbsp;|&nbsp; **目标**：通过完整的示例代码，理解框架各模块如何配合工作

## 示例：点击计数器（ClickCounter）

这是一个最简单的小游戏：在限定时间内点击按钮，点的次数就是你的分数。游戏结束时保存最高分，还可以分享到微信。

虽然简单，但它用到了模板的几乎所有核心模块：

> **当前版本说明（2026-04-11）**
> - 示例入口已调整为主菜单 `示例 Demo` 分组下的三个按钮：`点击游戏`、`弹幕Demo`、`特效Demo`。
> - ClickGame 的 FairyGUI 源文件位于 `UIProject/assets/ClickGame/`，运行时代码位于 `Assets/_Example/ClickGame/`。
> - VFXDemo 的正式验证场景位于 `Assets/_Example/VFXDemo/Scenes/VFXDemo.unity`，不要在 `Boot` 场景直接堆测试对象。
> - 若修改主菜单 UI，请同步更新 FairyGUI 源文件 `UIProject/assets/MainMenu/MainMenuPanel.xml` 与导出代码。





| 框架模块 | 在示例中的角色 |
|----------|---------------|
| FSM | 管理游戏状态（菜单 → 游戏中 → 结束） |
| DataSystem | SO 变量存储分数、最高分、剩余时间 |
| EventSystem | SO 事件通知游戏开始/结束 |
| Timer | 倒计时实现 |
| WeChatBridge | 分享功能 |
| SaveSystem | 最高分持久化 |

## 目录结构

```text
Assets/_Example/
├── Example.asmdef
├── README.md
├── ClickGame/
│   ├── Scenes/ClickGame.unity
│   ├── Scripts/
│   │   ├── ClickGameSceneEntry.cs
│   │   ├── ExampleSceneNavigator.cs
│   │   ├── ClickButton.cs
│   │   ├── ClickGameManager.cs
│   │   ├── CountdownDisplay.cs
│   │   ├── HighScoreSaver.cs
│   │   └── ScoreDisplay.cs
│   └── UI/ClickGame/
│       ├── ClickCounterPanel.cs
│       ├── ClickCounterPanel.Logic.cs
│       ├── ClickGameBinder.cs
│       └── MenuIconButton.cs
├── DanmakuDemo/
│   ├── Scenes/DanmakuDemo.unity
│   └── Scripts/
│       ├── DanmakuDemoController.cs
│       ├── DanmakuDebugHUD.cs
│       └── SimplePlayerMover.cs
└── VFXDemo/
    ├── Scenes/VFXDemo.unity
    ├── Scripts/VFXDemoSpawner.cs
    ├── Config/VFXRenderConfig_Demo.asset
    ├── Registry/VFXTypeRegistry_Demo.asset
    └── Type/VFXType_Explosion_Test.asset
```

本篇重点解读 ClickGame；DanmakuDemo 作为第二个示例，主要用于展示 DanmakuSystem 的集成方式与场景切换流程；VFXDemo 作为第三个示例，用于展示 Sprite Sheet VFX 的最小闭环与独立测试场景组织方式。



---

## ClickGameManager.cs — 游戏主控制器

这是最核心的文件。它把 FSM、事件、变量、计时器串联起来：

```csharp
public class ClickGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float _gameDuration = 10f;

    [Header("State Machine")]
    [SerializeField] private StateMachine _stateMachine;
    [SerializeField] private State _menuState;
    [SerializeField] private State _playingState;
    [SerializeField] private State _gameOverState;

    [Header("Data")]
    [SerializeField] private IntVariable _playerScore;
    [SerializeField] private IntVariable _highScore;
    [SerializeField] private FloatVariable _remainingTime;

    [Header("Events")]
    [SerializeField] private GameEvent _onGameStart;
    [SerializeField] private GameEvent _onGameOver;

    private const string HIGH_SCORE_KEY = "example_high_score";
    private TimerHandle _countdownTimer = TimerHandle.Invalid;
```

**注意几个设计要点：**

1. **所有依赖都是 `[SerializeField]`**：不需要 `Find()`、不需要 `GetComponent()`、不需要 Singleton。在 Inspector 中拖拽配置。
2. **状态用 SO 表示**：`_menuState`、`_playingState`、`_gameOverState` 都是 ScriptableObject 资产。
3. **数据用 SO 变量**：`_playerScore`、`_highScore`、`_remainingTime` 是项目中的 `.asset` 文件，其他组件也能引用同一个文件来读取数据。

### 游戏启动

```csharp
private void Start()
{
    // 从本地存储加载最高分
    _highScore.SetValue(GameBootstrapper.SaveSystem.LoadInt(HIGH_SCORE_KEY, 0));
}
```

通过 `GameBootstrapper.SaveSystem` 读取持久化数据，而不是 `new PlayerPrefsSaveSystem()`。

### 开始游戏

```csharp
public void StartGame()
{
    _playerScore.ResetToInitial();          // 分数归零
    _remainingTime.SetValue(_gameDuration); // 重置倒计时

    _stateMachine.TransitionTo(_playingState); // FSM 切到"游戏中"
    _onGameStart?.Raise();                     // 通知其他系统"游戏开始了"

    // 创建倒计时计时器：每 0.1 秒扣减时间
    _countdownTimer = TimerService.Instance.Repeat(0.1f, () =>
    {
        _remainingTime.ApplyChange(-0.1f);
        if (_remainingTime.Value <= 0f)
        {
            _remainingTime.SetValue(0f);
            EndGame();
        }
    });
}
```

**这里展示了三个模块的配合：**
- `_playerScore.ResetToInitial()` → **DataSystem**（SO 变量重置）
- `_stateMachine.TransitionTo()` → **FSM**（状态切换）
- `_onGameStart.Raise()` → **EventSystem**（事件广播）
- `TimerService.Instance.Repeat()` → **Timer**（计时器）

### 处理点击

```csharp
public void OnClick()
{
    if (_stateMachine.CurrentState != _playingState) return; // 只有在"游戏中"才计分
    _playerScore.ApplyChange(1); // +1 分，自动触发所有监听者
}
```

`_playerScore.ApplyChange(1)` 不仅仅是加了个 1——它会触发 `OnValueChanged` 事件，导致 `ScoreDisplay` 和 `HighScoreSaver` 立即收到通知。**这就是 SO 变量的威力：你只管写数据，不需要知道谁在读。**

### 游戏结束

```csharp
private void EndGame()
{
    TimerService.Instance.Cancel(_countdownTimer); // ⚠️ 必须取消计时器
    _stateMachine.TransitionTo(_gameOverState);
    _onGameOver?.Raise();

    // 检查最高分
    if (_playerScore.Value > _highScore.Value)
    {
        _highScore.SetValue(_playerScore.Value);
        GameBootstrapper.SaveSystem.SaveInt(HIGH_SCORE_KEY, _highScore.Value);
        GameBootstrapper.SaveSystem.Save(); // 刷新到磁盘
    }
}
```

### 分享到微信

```csharp
public void ShareScore()
{
    var wx = WeChatBridgeFactory.Create(); // 工厂自动选择桩/真实实现
    wx.Share($"I scored {_playerScore.Value} points!", "", $"score={_playerScore.Value}");
}
```

在 Editor 中运行时，`WeChatBridgeStub` 只是打印一条日志；发布到微信后会调用真实的分享 API。游戏代码完全不需要改。

---

## ScoreDisplay.cs — 分数显示

这是框架"数据驱动 UI"模式的最小示例：

```csharp
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private IntVariable _score;

    private void OnEnable()
    {
        if (_score != null)
            _score.OnValueChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (_score != null)
            _score.OnValueChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int value)
    {
        GameLog.Log($"[ScoreDisplay] Score: {value}");
    }

}
```

**关键模式：**
- `OnEnable` 订阅，`OnDisable` 注销——**始终配对**
- 组件不知道谁在修改分数，它只关心 `_score` 这个 SO 变量
- 在 Inspector 中拖入与 `ClickGameManager` 相同的 `PlayerScore.asset`，就自动建立了数据绑定

---

## CountdownDisplay.cs — 倒计时显示

模式完全一样，只是监听的是 `FloatVariable`：

```csharp
public class CountdownDisplay : MonoBehaviour
{
    [SerializeField] private FloatVariable _remainingTime;

    private void OnEnable()
    {
        if (_remainingTime != null)
            _remainingTime.OnValueChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (_remainingTime != null)
            _remainingTime.OnValueChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(float value)
    {
        GameLog.Log($"[CountdownDisplay] Remaining: {value:F1}s");
    }

}
```

---

## HighScoreSaver.cs — 自动保存最高分

监听 `_highScore` 变化，一有变化就持久化：

```csharp
public class HighScoreSaver : MonoBehaviour
{
    [SerializeField] private IntVariable _highScore;
    private const string HIGH_SCORE_KEY = "example_high_score";

    private void Start()
    {
        int saved = GameBootstrapper.SaveSystem.LoadInt(HIGH_SCORE_KEY, 0);
        _highScore.SetValue(saved);
    }

    private void OnEnable()  { _highScore.OnValueChanged += OnHighScoreChanged; }
    private void OnDisable() { _highScore.OnValueChanged -= OnHighScoreChanged; }

    private void OnHighScoreChanged(int value)
    {
        GameBootstrapper.SaveSystem.SaveInt(HIGH_SCORE_KEY, value);
        GameBootstrapper.SaveSystem.Save();
        GameLog.Log($"[HighScoreSaver] High score saved: {value}");
    }
}
```

**这里有一个微妙但重要的设计**：保存逻辑和游戏逻辑是分离的。`ClickGameManager` 只负责更新 `_highScore` 的值，它不关心数据怎么存。`HighScoreSaver` 只关心"高分变了就存"。

如果以后你想把存储从 PlayerPrefs 换成服务端 API，只需要修改 `ISaveSystem` 的实现，**这两个组件的代码一行都不用改**。

---

## ClickButton.cs — 按钮输入

最简单的一个，纯粹做输入转发：

```csharp
public class ClickButton : MonoBehaviour
{
    [SerializeField] private ClickGameManager _gameManager;

    public void OnButtonClicked()
    {
        _gameManager.OnClick();
    }
}
```

在 FairyGUI 的按钮点击事件中调用 `OnButtonClicked()`。**单一职责**：这个组件只处理输入，不关心计分逻辑。

---

## 数据流全景图

把所有组件放在一起看，数据是这样流动的：

```
用户点击                                          屏幕显示
   │                                                  ↑
   ↓                                                  │
ClickButton ──→ ClickGameManager ──写→ PlayerScore.asset ──→ ScoreDisplay
                     │                                       ──→ HighScoreSaver → 磁盘
                     │
                     ├──写→ RemainingTime.asset ──→ CountdownDisplay
                     │
                     ├──切换→ StateMachine (FSM)
                     │
                     ├──Raise→ OnGameStart.asset ──→ (其他监听者)
                     │
                     ├──Raise→ OnGameOver.asset ──→ (其他监听者)
                     │
                     └──调用→ WeChatBridgeFactory ──→ 分享
```

**每个组件只认识自己引用的 SO 资产**，不认识其他组件。这就是 SO 驱动架构的核心好处——你可以自由添加新的监听者（比如一个"音效播放器"组件监听 PlayerScore 的变化来播放加分音效），而不需要修改已有代码。

---

## 你学到了什么

通过这个示例，你已经看到了：

| 模式 | 在哪体现 |
|------|----------|
| **SO 变量作为数据总线** | PlayerScore、HighScore、RemainingTime |
| **SO 事件作为消息通道** | OnGameStart、OnGameOver |
| **OnEnable/OnDisable 配对** | 所有 Display 和 Saver 组件 |
| **FSM 管理游戏状态** | StateMachine + State SO |
| **TimerService 替代 Coroutine** | 倒计时实现 |
| **GameBootstrapper.SaveSystem** | 持久化最高分 |
| **WeChatBridgeFactory** | 平台无关的微信 SDK 调用 |
| **单一职责组件** | 每个脚本只做一件事，都不超过 50 行 |

## 入口与返回流程

当前模板的示例流转如下：

1. 从 `Boot.unity` 启动，进入主菜单
2. 在主菜单的“示例 Demo”区域选择目标入口：`点击游戏` / `弹幕Demo` / `特效Demo`
3. `ClickGame` 由 `ClickGameSceneEntry` 注册 `ClickGameBinder` 并打开 `ClickCounterPanel`
4. `DanmakuDemo` 与 `VFXDemo` 分别加载各自独立场景，避免把示例对象直接堆在 `Boot`
5. 示例内通过返回按钮或 `Esc` 重载 `Boot` 场景并重新打开主菜单

DanmakuDemo 复用统一返回策略；VFXDemo 则用于独立验证 Sprite Sheet VFX 的播放链与渲染链。

## VFXDemo 模板化要点

如果你要基于模板再加一个新的 VFX 示例，建议直接照着 VFXDemo 的三段式结构走：

1. **系统根对象**：只挂 `SpriteSheetVFXSystem`，负责渲染配置与类型注册
2. **播放根对象**：只挂播放控制脚本，负责触发某个 `VFXTypeSO`
3. **交互根对象**：优先挂通用 `ExampleSceneHotkeys` 负责返回主菜单与说明文字，再叠加 Demo 专用输入脚本处理额外快捷键

这样做的好处很直接：
- 播放逻辑不和输入逻辑互相缠住
- 场景结构一眼能看懂
- 后续如果要把“返回主菜单 + 说明提示”抽成通用示例组件，不需要先拆烂代码

> 📐 精确的场景最小根对象、必配资产和快捷键闭环，请直接看 `UnityProj/Assets/_Example/VFXDemo/README.md`。

## 下一步

现在你已经理解了框架的工作方式，可以：

- 查阅 [框架模块使用手册](FRAMEWORK_MODULES.md) 了解更多模块的详细 API
- 在 `Assets/_Game/` 中开始开发你自己的游戏
- 遇到问题看 [常见问题与排错](FAQ.md)

