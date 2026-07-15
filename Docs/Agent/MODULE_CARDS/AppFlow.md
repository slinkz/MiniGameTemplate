---
system: knowledge-engineering
scope: module-card-appflow
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Module Card: AppFlow

## 1. 模块职责

AppFlow 是栈式应用导航系统，负责 FlowNode、Navigation Stack、Push/Pop/Replace/PopTo、场景加载策略、面板 Suspend/Resume，以及主界面、选关、战斗等流程之间的可恢复导航语义。

## 2. 不负责什么

- 不实现具体 UI 面板内部业务逻辑。
- 不直接决定战斗胜负或关卡数据。
- 不替 UIManager 管理 FairyGUI 包加载细节。
- 不恢复已禁用的热启动栈恢复策略，当前以冷启动清栈为准。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `AppFlowNavigator` | 栈式导航主入口 |
| `FlowNodeSO` | 节点定义 |
| `StackEntry` | 导航栈条目，含面板归属信息 |
| `IFlowData` | 节点传参数据 |
| `IFlowHandler` | 节点进入/离开钩子 |
| `IPanelSuspendable` | 面板 Suspend/Resume 可选接口 |
| `UIManager` Suspend/Resume API | 面板隐藏与恢复 |

## 4. 数据流

```text
UI Button / Game Logic
  -> AppFlowNavigator.Push/Pop/Replace
  -> 计算 leaving/entering StackEntry
  -> SceneLoader 加载/切换场景
  -> UIManager 打开/关闭/挂起/恢复面板
  -> IFlowHandler 执行节点钩子
  -> Stack 持久化策略（当前冷启动清栈）
```

## 5. 生命周期

```text
Cold Start -> Clear Stack -> Enter Root Node
  -> Push 子节点
  -> Suspend leaving panels
  -> Enter target scene/panels
  -> Pop/PopTo/Replace
  -> Close or Resume panels
```

## 6. 依赖关系

AppFlow 位于框架导航层，依赖 SceneLoader、UIManager、SO 节点配置和 GameStartupFlow 集成。游戏业务通过 FlowNode 和 Handler 接入，不应绕过导航栈随意切场景和关面板。

## 7. 关键 SO / 配置路径

```text
Assets/_Game/ScriptableObjects/Config/SD_Main.asset
Assets/_Game/Configs/Core/
Assets/_Framework/Navigation/**
```

FlowNodeSO 相关资产应在 AppFlow TDD 集成文档中查找。

## 8. 关键 ADR

- ADR-034：AppFlow 栈式导航系统。
- 相关实施：`SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md` v1.8，冷启动清栈，热启动恢复暂时禁用。

## 9. 热路径 / 性能约束

AppFlow 不是高频战斗热路径，但必须避免异步/场景切换竞态、面板重复绑定和跨场景残留。

## 10. 常见错误

- 直接调用 SceneManager 或 UIManager 绕过 AppFlow，导致栈状态和面板状态不一致。
- Pop 时只关闭当前面板，忘记恢复上一层 suspended panels。
- Replace/PopAll 时没有清理中间层面板。
- 把热启动恢复文档当当前事实，忽略 v1.8 冷启动清栈。
- UI 面板没有实现正确 Suspend/Resume 行为。

## 11. 修改前必读

- `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md`
- `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_01_CORE_DESIGN.md`
- `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_03_INTEGRATION.md`
- `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` 第六部分
- `ADR/ADR_INDEX.md` 中 ADR-034
- 触碰 UI 时读 `MODULE_CARDS/UISystem_FairyGUI.md`

## 12. 修改后必验

- Push、Pop、PopTo、PopAll、Replace 行为符合面板矩阵。
- Main -> LevelSelect -> Battle -> 返回流程可重复。
- Cold Start 清栈行为符合 v1.8。
- Suspend/Resume 不重复绑定事件、不丢 UI 状态。
- 场景切换无循环加载、无残留面板。
