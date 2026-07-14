---
system: navigation
scope: acceptance
last_verified: 2026-05-17
---

# AppFlow 导航系统 — 验收计划

> 归档说明：本文是 AppFlow 独立验收计划，当前 AppFlow 验收入口已并入 `Docs/Agent/SG_V2_DEVICE_ACCEPTANCE.md` 第六部分。本文仅作历史参考。

> **对应 TDD**：APPFLOW_TDD v1.5  
> **状态**：⬜ 待天命人验收  
> **编译验证**：✅ MCP 确认 0 errors / 0 warnings（2026-05-05 23:30）

---

## 1. 前置条件

- [x] Unity 项目编译通过（0 errors / 0 warnings）
- [x] Phase 1~4 代码全部提交
- [ ] Unity Editor 打开 MiniGameTemplate 项目
- [ ] Boot 场景为默认启动场景

---

## 2. SO 资产确认（编辑器检查）

| # | 检查项 | 预期位置 | 验证方式 |
|---|--------|---------|---------|
| S1 | `Node_MainMenu` FlowNodeSO | `Assets/_Game/Configs/Navigation/` | Inspector：RequiredScene=null, PanelTypeName="MainMenuPanel" |
| S2 | `Node_LevelSelect` FlowNodeSO | 同上 | Inspector：RequiredScene=null, PanelTypeName="LevelSelectScreen" |
| S3 | `Node_Battle` FlowNodeSO | 同上 | Inspector：RequiredScene=Scene_Battle, PanelTypeName="" |
| S4 | `Scene_Battle` SceneDefinition SO | `Assets/_Game/Configs/Navigation/` 或 `Scenes/` | Inspector：IsAdditive=true |
| S5 | `FlowNodeRegistry` SO | `Assets/_Game/Configs/Navigation/` | Inspector：包含上述 3 个 FlowNode 引用 |
| S6 | `GameStartupFlow` 引用 Node_MainMenu | Inspector → GameStartupFlow 脚本 | _rootNode 字段已赋值 |

---

## 3. PlayMode 验收（Boot 场景启动）

### 正常流程

| # | 操作 | 预期结果 | PASS/FAIL |
|---|------|---------|-----------|
| AC-1 | 进入 Play Mode（Boot 场景） | 启动流程走完 → 显示主菜单面板 | ⬜ |
| AC-2 | 主菜单点击"弹幕射击" | 显示选关界面（LevelSelect） | ⬜ |
| AC-3 | 选关选择第 1 关 → 确认 | Battle 场景 Additive 加载 → 战斗开始 | ⬜ |
| AC-4 | 暂停 → 返回 | Battle 场景卸载 → 立刻回到选关界面 (**不走启动流程**) | ⬜ |
| AC-5 | 再次进入战斗 → 获胜 → 确认 | Battle 场景卸载 → 立刻回到选关界面 | ⬜ |
| AC-6 | 选关 → 返回 | 回到主菜单 | ⬜ |

### 异常防护

| # | 操作 | 预期结果 | PASS/FAIL |
|---|------|---------|-----------|
| AC-7 | 战斗中快速连点暂停→返回按钮 | 不崩溃，最多只执行一次 Pop（Console 有 Warning） | ⬜ |
| AC-8 | AppFlowNavigator Inspector（Play Mode） | 栈可视化显示正确层级 | ⬜ |

### ~~热启动恢复~~ → 冷启动清栈验证（2026-05-17 变更）

> **背景**：热启动恢复功能已暂时禁用。`TryRestoreNavigationStackAsync()` 冷启动一律清空 `appflow_stack`。
> 以下验收项更新为验证冷启动行为的正确性。

| # | 操作 | 预期结果 | PASS/FAIL |
|---|------|---------|-----------|
| AC-9 | 进入选关界面 → 在微信开发者工具终止 → 刷新 | 回到主菜单首屏（不跳到选关界面）。Console 输出 `[StartupFlow] Cold boot — cleared stored stack` | ⬜ |
| AC-10 | 手动在 Storage 中写入损坏的 appflow_stack JSON → 刷新 | 正常回到主菜单首屏（不崩溃）。ClearStoredStack 清理损坏数据 | ⬜ |

---

## 4. 编辑器工具验证

| # | 检查项 | 操作 | 预期 | PASS/FAIL |
|---|--------|------|------|-----------|
| ET-1 | AppFlowNavigator Inspector | Play Mode 下选中 Navigator GO | 显示栈表格 + Pop/PopAll 按钮 | ⬜ |
| ET-2 | FlowNodeSO Inspector | 选中任意 FlowNodeSO | 显示面板下拉 + 校验 HelpBox | ⬜ |
| ET-3 | 构建验证 | 菜单 Tools/AppFlow/Validate Panel Registration | Console 输出验证结果 | ⬜ |
| ET-4 | Hierarchy 图标 | Play Mode 下查看 Navigator GO | 绿色小方块（idle） | ⬜ |

---

## 5. 关键代码路径确认

| # | 文件 | 关键变更 | 确认方式 |
|---|------|---------|---------|
| C1 | `BattleController.cs` | 3 处 `SceneManager.LoadScene("Boot")` → `AppFlowNavigator.Instance.Pop()` | 全文搜索无 "LoadScene" |
| C2 | `BattleController.cs` | 无 `using UnityEngine.SceneManagement` | 全文搜索确认 |
| C3 | `GameStartupFlow.cs` | `TryRestoreNavigationStackAsync()` 冷启动一律清栈 + return false（2026-05-17 改） | 读代码确认 |
| C4 | `SceneLoader.cs` | `LoadSceneAsync(Task)` + `UnloadSceneAsync` + `_sceneHandleCache` 存在 | 读代码确认 |
| C5 | `ExampleSceneNavigator.cs` | 类标注 `[Obsolete]` | 读代码确认 |

---

## 6. 行动指导

### 验收步骤（按顺序执行）

1. **打开 Unity Editor** → 确认编译通过
2. **检查 SO 资产**（§2 表格逐项核对）
3. **进入 Play Mode** → 执行 §3 正常流程 AC-1~AC-6
4. **测试异常防护** → AC-7~AC-8
5. **测试冷启动清栈** → AC-9~AC-10（在微信开发者工具中验证）
6. **验证编辑器工具** → §4 ET-1~ET-4
7. **代码路径检查**（可选）→ §5 C1~C5

### 如果 AC-4/AC-5/AC-6 失败

- 检查 `AppFlowNavigator` Singleton 是否在 Boot 场景中存在
- 检查 `GameBootstrapper` 是否在 `InitializeSystems` 中 touch 了 Navigator
- 检查面板注册是否在 `[RuntimeInitializeOnLoadMethod]` 中完成

### 如果 AC-9 失败

- 确认 `TryRestoreNavigationStackAsync` 是否为新版（冷启动一律清栈 + return false）
- 检查是否有其他代码在启动时读取 `appflow_stack` 并恢复栈
- 检查 Console 日志中 `[StartupFlow]` 前缀的输出

---

_验收计划 v1.0 | 2026-05-05 | 广智_
