# 示例游戏：ClickCounter（点击计数器）

## 当前状态（2026-04-07）

模板现在提供两个独立示例入口：

1. **ClickGame（点击计数器）**
   - 启动后进入 `MainMenuPanel`
   - 点击“进入 ClickGame”会加载 `Assets/_Example/ClickGame/Scenes/ClickGame.unity`
   - 支持：点击加分、倒计时、结算、重开、Esc 返回主菜单、分享、最高分本地保存
   - FairyGUI 包源文件位于：`UIProject/assets/ClickGame/`
   - 运行时代码位于：`Assets/_Example/ClickGame/`

2. **DanmakuDemo（弹幕演示）**
   - 点击“进入 DanmakuDemo”会加载 `Assets/_Example/DanmakuDemo/Scenes/DanmakuDemo.unity`
   - 支持：数字键 1/2/3 切难度、Esc 返回主菜单



## 玩法
- 在限定时间内尽可能多地点击按钮
- 每次点击加 1 分
- 时间结束时显示本局得分和最高分
- 支持重开、返回主菜单、分享战绩

## 相关代码
```
_Example/
  ClickGame/
    Scenes/ClickGame.unity
    Scripts/
      ClickGameSceneEntry.cs
      ExampleSceneNavigator.cs
      ClickGameManager.cs
      ClickButton.cs
      CountdownDisplay.cs
      ScoreDisplay.cs
      HighScoreSaver.cs
    UI/ClickGame/
      ClickCounterPanel.cs
      ClickCounterPanel.Logic.cs
      ClickGameBinder.cs
      MenuIconButton.cs

  DanmakuDemo/
    Scenes/DanmakuDemo.unity
    Scripts/
      DanmakuDemoController.cs
      DanmakuDebugHUD.cs
      SimplePlayerMover.cs
```

## 运行方式
1. 打开 `Assets/_Framework/GameLifecycle/Boot.unity`
2. 点击 Play
3. 在主菜单点击“进入 ClickGame”或“进入 DanmakuDemo”

## FairyGUI 发布（ClickGame）
1. 用 FairyGUI 编辑器打开 `UIProject/MiniGameTemplate.fairy`
2. 确认 `ClickGame` 包包含 `ClickCounterPanel`
3. 点击发布，导出到：`UnityProj/Assets/_Game/FairyGUI_Export/`
4. 回到 Unity 运行


