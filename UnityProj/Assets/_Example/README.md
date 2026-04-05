# 示例游戏：ClickCounter（点击计数器）

## 玩法
- 在限定时间内尽可能多地点击按钮
- 每次点击加 1 分
- 时间结束时显示最终得分和最高分
- 可重新开始或返回主菜单

## 演示的模板功能
| 功能 | 模板模块 |
|------|---------|
| 分数管理 | `IntVariable` (PlayerScore, HighScore) |
| 游戏状态 | `StateMachine` (Menu → Playing → GameOver) |
| 状态变更通知 | `GameEvent` (OnGameStart, OnGameOver) |
| 倒计时 | `TimerService` |
| UI面板 | `UIBase` / `UIManager` |
| 音效 | `AudioManager` + `AudioClipSO` |
| 本地存储 | `ISaveSystem` (保存最高分) |
| 场景管理 | `SceneLoader` + `SceneDefinition` |
| 微信集成 | `WeChatBridge` (分享得分) |

## 代码结构
```
_Example/
  Scripts/
    ClickGameManager.cs      # 游戏流程控制（连接FSM与事件）
    ClickButton.cs            # 点击按钮逻辑（加分）
    CountdownDisplay.cs       # 倒计时UI更新
    ScoreDisplay.cs           # 分数UI更新
    HighScoreSaver.cs         # 最高分本地存储
  ScriptableObjects/
    ExamplePlayerScore.asset  # IntVariable
    ExampleHighScore.asset    # IntVariable
    ExampleGameTime.asset     # FloatVariable (倒计时)
    ExampleOnGameStart.asset  # GameEvent
    ExampleOnGameOver.asset   # GameEvent
  Scenes/
    ExampleGame.unity         # 示例游戏场景
```

## 运行方式
1. 打开 `Assets/Scenes/Boot.unity`
2. 将 `DefaultGameConfig` 的 Initial Scene 指向 ExampleGame 的 SceneDefinition
3. 点击 Play
