---
system: navigation
scope: appflow-tdd-integration
parent: APPFLOW_TDD_INDEX
last_verified: 2026-05-07
---

# AppFlow TDD — §4 系统集成

> 父文档：[APPFLOW_TDD_INDEX.md](APPFLOW_TDD_INDEX.md)

---

## 4.1 与现有系统的关系

| 现有系统 | 整合方式 | 变更幅度 |
|---------|---------|---------|
| `UIManager` | Navigator 调用 `SuspendAllPanels/ResumePanels/CloseSuspendedPanels` 管理面板生命周期；`OpenPanelAsync` 自动检测挂起面板并恢复 | **v1.7 新增 ~80 行** |
| `SceneLoader` | 新增 `LoadSceneAsync(Task)` + `UnloadSceneAsync(Task)` + SceneHandle 缓存 | **小幅修改（~30 行）** |
| `GameBootstrapper` | touch Navigator + 条件跳过 `LoadInitialScene()` | 改 ~5 行 |
| `GameStartupFlow` | ~~集中注册面板~~（已移除）+ 启动完成后 `PushAsync(Node_MainMenu)` | 改 ~5 行 |
| 各面板类 | 自注册 `[RuntimeInitializeOnLoadMethod]` 调用 `RegisterPanelOpener`（PK UA-003） | 每面板 +5 行 |
| `StateMachine` (FSM) | **不使用** | 无冲突 |
| `BattleController` | 实现 `IFlowSuspendable` + 退出时 `Pop()` 同步入口 | 改 ~10 行 |
| `ExampleSceneNavigator` | 弃用（标 `[Obsolete]`） | 不删除 |

> **SceneLoader 变更清单**（PK WX-002/004/006 + UA-004）：
> 1. 新增 `_sceneHandleCache: Dictionary<string, SceneHandle>` 缓存加载的 SceneHandle
> 2. 新增 `public Task LoadSceneAsync(SceneDefinition)` — 对称 API，包装现有 Coroutine 返回 Task
> 3. `LoadSceneViaAssetServiceAsync` 加载成功后缓存 handle
> 4. 新增 `public Task UnloadSceneAsync(SceneDefinition)` — 通过 `sceneHandle.UnloadAsync()` 释放
> 5. 场景已加载判断内置（`GetSceneByName().isLoaded` 短路返回）
> 6. `_isLoading` 互斥仅对 Single 模式生效（Additive 不阻塞后续操作）

---

## 4.2 FlowNode SO 资产清单

| SO 资产名 | RequiredScene | PanelTypeName（注册表 key） | UnloadSceneOnExit |
|-----------|---------------|---------------|-------------------|
| `Node_MainMenu` | SD_Main (Single) | `MainMenuPanel` | — |
| `Node_LevelSelect` | SD_Main (Single) | `LevelSelectScreen` | — |
| `Node_Battle` | SD_Battle (Single) | _(空，由 BattleController 自管)_ | true |

> **2026-05-06 重构**：MainMenu/LevelSelect 从纯 UI 节点改为关联 SD_Main（Single 模式），Battle 改为 `UnloadSceneOnExit=false`。场景切换由 Single 模式自动替换完成——Push Battle 时 Main 被替换，Pop 回 LevelSelect 时 Battle 被替换。

---

## 4.3 导航流程时序

### 正常游戏流程（2026-05-07 Suspend/Resume 版本）

```
[App 启动] 
  GameBootstrapper.Awake → InitializeSystems → IStartupFlow.RunAsync
  ↓
[StartupFlow 完成] 
  Push(Node_MainMenu, menuData)
  → Stack: [MainMenu]
  → SceneLoader.LoadScene(SD_Main, Single) → Boot 被替换
  → UIManager.OpenPanelAsync<MainMenuPanel>()
  ↓
[点击"弹幕射击"] 
  Push(Node_LevelSelect)
  → Stack: [MainMenu, LevelSelect]
  → SuspendAllPanels() → MainMenuPanel.OnSuspend() → Hide
  → SceneLoader: SD_Main 已加载，短路跳过
  → OpenPanelAsync<LevelSelectScreen>()
  ↓
[选关确认] 
  Push(Node_Battle, { levelIndex = 2 })
  → Stack: [MainMenu, LevelSelect, Battle]
  → SuspendAllPanels() → LevelSelectScreen Hide
  → SceneLoader.LoadScene(SD_Battle, Single) → Main 被替换
  → BattleFlowHandler.OnFlowEnter → BattleController.StartBattle()
  ↓
[暂停 → 返回] 
  Pop()
  → Stack: [MainMenu, LevelSelect]
  → CloseAllPanels() + CloseSuspendedPanels(Battle层)
  → ExitNode: UnloadSceneOnExit=true → 不手动卸载（Single 自动替换）
  → ResumePanels(LevelSelect层) → LevelSelectScreen.OnResume() → Show
  → SceneLoader.LoadScene(SD_Main, Single) → Battle 被替换
  ↓
[胜利 → 确认]
  Pop()  ← 同上效果
```

### 热启动恢复（V1 Phase 4）

```
[微信热启动] 
  App.onShow → GameBootstrapper 判断 isHotRestart
  ↓
[GameStartupFlow 完成]
  TryRestoreStackAsync() → wx.getStorageSync("appflow_stack")
  ↓ JSON 存在且 version 匹配
  FlowStackSerializer.DeserializeStack(json)
  → FlowNodeRegistry.GetByNodeId(id) 逐层恢复
  → Stack: [MainMenu, LevelSelect, Battle]  (恢复到杀进程前)
  → 进入栈顶节点 EnterNodeAsync(Battle, savedData)
  ↓ JSON 不存在 / version 不匹配 / nodeId 找不到
  fallback → PushAsync(Node_MainMenu)  (正常首屏)
```

---

## 4.4 场景策略（2026-05-06 重构）

```
场景布局：
  Boot.unity  → 仅启动时短暂存在，GameBootstrapper 在此初始化所有 Singleton 后切走
  Main.unity  → 非战斗宿主场景（正交相机 Size=8），承载 MainMenu / LevelSelect 面板
  Battle.unity → 战斗场景，承载 BattleController / EntitySystem / UI Controllers

常驻层（DontDestroyOnLoad）：
  GameBootstrapper → AppFlowNavigator / SceneLoader / UIManager / DanmakuSystem / FairyGUI GRoot

场景切换流程：
  Boot ──Single──→ Main ──Single──→ Battle ──Single──→ Main
                                      ↑ Push              ↑ Pop
```

**设计决策**：
- **所有场景都是 Single 模式加载**——利用 Unity 的 LoadSceneMode.Single 自动替换前一个场景
- `Node_Battle.UnloadSceneOnExit = false`——Pop 时不需手动卸载，因为 EnterNode(LevelSelect) 会加载 SD_Main(Single)，自动替换 Battle
- 战斗中的 FairyGUI 对象直接挂 GRoot（DontDestroyOnLoad），各 UI Controller 必须在 `OnDestroy` 中 `Dispose()` 清理
- DanmakuSystem 是 DontDestroyOnLoad，需在 `BattleController.OnDestroy` 中显式 `ClearAll()`
