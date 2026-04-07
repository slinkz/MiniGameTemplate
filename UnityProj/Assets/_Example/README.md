# 示例游戏：ClickCounter（点击计数器）

## 当前状态（2026-04-07）

模板现在提供两条可玩路径：

1. **开箱即玩（无需额外配置）**
   - 启动后进入 `MainMenuPanel`
   - 若 `GameStartupFlow._startGameEvent` 未配置，主菜单自动启用内置 ClickCounter 回退玩法
   - 支持：开始、点击加分、倒计时、结算、重开、返回、分享、最高分本地保存

2. **独立面板模式（推荐）**
   - 已新增 FairyGUI 组件源文件：`UIProject/assets/Example/ClickCounterPanel.xml`
   - 已新增运行时代码：`_Game/Scripts/UI/ClickCounterPanel.cs`
   - 需要在 FairyGUI 编辑器中重新发布 Example 包后使用（见下文）


## 玩法
- 在限定时间内尽可能多地点击按钮
- 每次点击加 1 分
- 时间结束时显示本局得分和最高分
- 支持重开、返回主菜单、分享战绩

## 相关代码
```
_Game/
  Scripts/UI/
    MainMenuPanel.cs         # 主菜单 + 回退玩法（开箱即玩）
    ClickCounterPanel.cs     # 独立 ClickCounter 面板（配合 FairyGUI 导出）

_Example/
  Scripts/
    ClickGameManager.cs      # SO/FSM 驱动示例逻辑参考
    ClickButton.cs
    CountdownDisplay.cs
    ScoreDisplay.cs
    HighScoreSaver.cs
```

## 运行方式
1. 打开 `Assets/_Framework/GameLifecycle/Boot.unity`
2. 点击 Play
3. 在主菜单点击“开始游戏”

## 启用独立面板模式（可选）
1. 用 FairyGUI 编辑器打开 `UIProject/MiniGameTemplate.fairy`
2. 确认 Example 包包含 `ClickCounterPanel`
3. 点击发布，导出到：`UnityProj/Assets/_Game/FairyGUI_Export/`
4. 回到 Unity 运行

