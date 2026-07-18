---
system: navigation
scope: appflow-tdd-integration
parent: APPFLOW_TDD_INDEX
last_verified: 2026-05-17
---

# AppFlow TDD — §4 系统集成

> 父文档：[SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md](SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md)

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
> 7. 当待卸载场景是最后一个已加载场景时，先切入 `Transition.unity` 再释放旧 SceneHandle，避免 Unity 最后场景卸载 warning

---

## 4.2 FlowNode SO 资产清单

| SO 资产名 | RequiredScene | PanelTypeName（注册表 key） | UnloadSceneOnExit |
|-----------|---------------|---------------|-------------------|
| `Node_MainMenu` | SD_Main (Single) | `MainMenuPanel` | — |
| `Node_LevelSelect` | SD_Main (Single) | `LevelSelectScreen` | — |
| `Node_Battle` | SD_Battle (Single) | _(空，由 BattleController 自管)_ | true |

> **2026-07-18 更新**：MainMenu/LevelSelect 关联 SD_Main（Single 模式），Battle 离开时允许卸载；若 Battle 是最后一个已加载场景，SceneLoader 会先切入空 `Transition.unity`，再回到 SD_Main。

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
  → ExitNode: UnloadSceneOnExit=true → SceneLoader 先切 Transition，再释放 Battle handle
  → ResumePanels(LevelSelect层) → LevelSelectScreen.OnResume() → Show
  → SceneLoader.LoadScene(SD_Main, Single) → Transition 被替换
  ↓
[胜利 → 确认]
  Pop()  ← 同上效果
```

### ~~热启动恢复（V1 Phase 4）~~ → 冷启动清栈（2026-05-17 变更）

> **设计决策**：走完整 Boot → Awake → RunAsync 流程 = 冷启动（包括微信开发者工具终止+刷新）。
> 冷启动一律清空 `appflow_stack`，走正常主界面流程。
> 热启动恢复功能**暂时禁用**。未来如需支持微信 wx.onShow 热启动恢复，
> 应通过 jslib 注册 wx.onShow 回调设置内存标记，仅在标记为热启动时才恢复栈。

```
[进程启动 / 微信开发者工具刷新]
  GameBootstrapper.Awake → InitializeSystems → IStartupFlow.RunAsync
  ↓
[GameStartupFlow Phase 4]
  TryRestoreNavigationStackAsync()
  → ClearStoredStack()  // 一律清空 appflow_stack
  → return false        // 走正常启动
  ↓
[正常启动]
  PushAsync(Node_MainMenu) → 主菜单首屏
```

> **SaveStackToStorage 仍在运行**：每次导航后仍写入 `appflow_stack`（为未来热启动做准备），
> 但 `TryRestoreNavigationStackAsync` 在启动时不读回。

---

## 4.4 场景策略（2026-05-06 重构）

```
场景布局：
  Boot.unity  → 仅启动时短暂存在，GameBootstrapper 在此初始化所有 Singleton 后切走
  Main.unity  → 非战斗宿主场景（正交相机 Size=8），承载 MainMenu / LevelSelect 面板
  Transition.unity → 空过渡场景，用于卸载最后一个业务场景前的安全落点
  Battle.unity → 战斗场景，承载 BattleController / EntitySystem / UI Controllers

常驻层（DontDestroyOnLoad）：
  GameBootstrapper → AppFlowNavigator / SceneLoader / UIManager / DanmakuSystem / FairyGUI GRoot

场景切换流程：
  Boot ──Single──→ Main ──Single──→ Battle ──Single──→ Transition ──Single──→ Main
                                      ↑ Push              ↑ Pop
```

**设计决策**：
- **所有场景都是 Single 模式加载**——利用 Unity 的 LoadSceneMode.Single 自动替换前一个场景
- `Node_Battle.UnloadSceneOnExit = true`——Pop 时先经过 `Transition.unity` 释放 Battle handle，再加载 SD_Main(Single)
- 战斗中的 FairyGUI 对象直接挂 GRoot（DontDestroyOnLoad），各 UI Controller 必须在 `OnDestroy` 中 `Dispose()` 清理
- DanmakuSystem 是 DontDestroyOnLoad，需在 `BattleController.OnDestroy` 中显式 `ClearAll()`
