---
system: navigation
scope: appflow-tdd-impl-plan
parent: APPFLOW_TDD_INDEX
last_verified: 2026-05-07
---

# AppFlow TDD — §7 实施计划 + §8 后续演进

> 父文档：[APPFLOW_TDD_INDEX.md](APPFLOW_TDD_INDEX.md)

---

## 7. 实施计划

### Phase 1：框架层基础设施（~4h）✅ 已完成

| 步骤 | 内容 | 产出 |
|------|------|------|
| 1.1 | 创建 `Assets/_Framework/Navigation/` 目录 + asmdef | Navigation.asmdef |
| 1.2 | 实现 `IFlowData.cs`（空标记接口） | 导航数据契约 |
| 1.3 | 实现 `FlowNodeSO.cs`（含 `#if EDITOR OnValidate`） | 导航节点 SO + 编辑期校验 |
| 1.4 | 实现 `IFlowHandler.cs` + `IFlowSuspendable.cs` | 钩子接口（PK UA-007/008） |
| 1.5 | 实现 `AppFlowNavigator.cs`（含注册表 + 同步 API + Coroutine 超时 + Suspend/Resume + Editor 热重载 + EditorOnNavigated） | 栈式导航器 |
| 1.6 | `SceneLoader` 增加 `LoadSceneAsync(Task)` + SceneHandle 缓存 + `UnloadSceneAsync` 方法 | SceneLoader v1.1 |
| 1.7 | `SceneLoader._isLoading` Additive 场景不互斥 | 支持并发 Additive |
| 1.8 | 实现 `FlowNodeSOEditor.cs`（下拉 + 校验 + Build Settings 修复按钮） | SO Inspector DX |
| 1.9 | 实现 `AppFlowNavigatorEditor.cs`（PlayMode 栈可视化 + 快速操作按钮） | Editor 工具 |
| 1.10 | 实现 `AppFlowBuildValidator.cs`（MenuItem 验证 + IPreprocessBuildWithReport） | 构建守护 |
| 1.11 | 实现 `AppFlowHierarchyIcon.cs`（Hierarchy 状态图标 + Gizmo 文字标签） | 视觉反馈 |

### Phase 2：SO 资产 + 业务接入（~2h）✅ 已完成

| 步骤 | 内容 | 产出 |
|------|------|------|
| 2.1 | 创建 FlowNode SO：`Node_MainMenu` / `Node_LevelSelect` / `Node_Battle` | 3 个 SO 资产 |
| 2.2 | 创建 `Scene_Battle` SceneDefinition SO（IsAdditive=true） | 1 个 SO 资产 |
| 2.3 | `GameBootstrapper` 添加 Navigator touch + 条件跳过 `LoadInitialScene` | 改 5 行 |
| 2.4 | `GameStartupFlow` → 启动完成后 `PushAsync(Node_MainMenu, menuData)` | 改 ~5 行 |
| 2.5 | 各面板类添加 `[RuntimeInitializeOnLoadMethod]` 自注册（PK UA-003） | 每面板 +5 行 |
| 2.6 | Data 类实现 IFlowData（`MainMenuPanelData : IFlowData` 等） | 每类 +1 行 |
| 2.7 | `MainMenuPanel` → "弹幕射击"按钮 → `PushAsync(Node_LevelSelect)` | 改 3 行 |
| 2.8 | `LevelSelectScreen` → 选关 → `PushAsync(Node_Battle, levelData)` | 改 3 行 |
| 2.9 | `BattleController` → 实现 IFlowSuspendable + `Pop()` 同步入口 | 改 ~10 行 |

### Phase 3：清理旧代码（~0.5h）✅ 已完成

| 步骤 | 内容 | 状态 |
|------|------|------|
| 3.1 | 删除 `SceneManager.LoadScene("Boot")` 硬编码（BattleController） | ✅ 2026-05-05 |
| 3.2 | `ExampleSceneNavigator` 标注 `[Obsolete]` | ✅ 2026-05-05 |
| 3.3 | 验证 Battle 场景 IsAdditive 模式下 EntitySystemBootstrap 正常工作 | ✅ MCP 编译验证通过 |

### Phase 4：栈序列化 — 微信热启动恢复（~2h）✅ 已完成

> **从 V2 移入 V1**（2026-05-05）。微信小游戏随时可能被系统杀死并热启动恢复，不支持栈恢复 = 每次热启动回到首屏，用户体验不可接受。

| 步骤 | 内容 | 状态 |
|------|------|------|
| 4.1 | `FlowNodeSO` 新增 `_nodeId` | ✅ |
| 4.2 | `IFlowData` 推荐 `[Serializable]` | ✅ |
| 4.3 | 新增 `FlowStackSerializer` 静态工具类 | ✅ |
| 4.4 | 序列化格式：`{ "version": 1, "entries": [...] }` | ✅ |
| 4.5 | `FlowNodeRegistry`（SO 资产）→ nodeId→SO 映射 | ✅ |
| 4.6 | `AppFlowNavigator.SaveStackToStorage()` | ✅ |
| 4.7 | `AppFlowNavigator.TryRestoreStackAsync()` | ✅ |
| 4.8 | `GameStartupFlow` 先尝试 Restore | ✅ |
| 4.9 | 版本兼容处理 | ✅ |
| 4.10 | nodeId 找不到时容错 | ✅ |

---

## 验收标准

| # | 验收项 | 通过条件 |
|---|--------|---------|
| AC-1 | 主菜单 → 选关 → 进入战斗 → 正常游戏 | 无回归 |
| AC-2 | 暂停 → 返回 → 立即看到选关界面（不经过启动流程） | ≤ 1s |
| AC-3 | 胜利 → 确定 → 立即看到选关界面 | ≤ 1s |
| AC-4 | 失败 → 返回 → 立即看到选关界面 | ≤ 1s |
| AC-5 | 选关 → 返回 → 看到主菜单 | Stack 正确回退 |
| AC-6 | 快速连点不崩溃 | _isTransitioning 防护 |
| AC-7 | 编译 0 errors 0 warnings | — |
| AC-8 | 微信小游戏真机验证 | 场景切换无白屏 |
| AC-9 | 热启动恢复：进入战斗 → 杀进程 → 重新打开 → 恢复到战斗节点 | 栈正确恢复 |
| AC-10 | 存储数据损坏/版本不匹配 → 正常降级到首屏 | 无崩溃无白屏 |

---

## 8. 后续演进（V2 不在本次范围）

| 特性 | 说明 | 时机 |
|------|------|------|
| **⚠️ 300 行拆分规则** | 当 AppFlowNavigator.cs 超过 300 行时，必须同步提取 `ISceneTransition` / `IPanelResolver` 策略接口（PK UA-001） | **硬性约束** |
| 转场动画 | Push/Pop 时可配置渐变/滑动动画（通过 ISceneTransition 策略注入） | 视觉打磨阶段 |
| 导航拦截器 | `INavigationGuard` — 离开前确认（如"是否放弃当前关卡"） | 需要时再加 |
| 深层链接 | 从微信分享链接直接进入指定关卡 → `PushAsync` 链式调用 | 社交分享阶段 |
| 导航路径可视化 | EditorWindow 节点图展示所有 FlowNodeSO 跳转关系（PK ET-010） | 当 FlowNodeSO 达到 8+ 时 |
