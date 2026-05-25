---
system: navigation
scope: appflow-tdd-pk
last_verified: 2026-05-05
---

# AppFlow TDD — PK 对抗评审记录

> **PK 规格**：1 轮攻防 + 回写  
> **攻方角色**：微信小游戏全栈开发者（10 年+经验，专精 WebGL/IL2CPP/FairyGUI/YooAsset）  
> **守方角色**：软件架构师（框架设计者）  
> **目标文档**：`APPFLOW_TDD.md` v1.0

---

## Round 1 攻方质疑（12 问题）

### WX-001 | 🔴高 | 反射 MakeGenericMethod 在 IL2CPP/WebGL 下被裁剪

**涉及章节**：§3.2 `OpenPanelByTypeNameAsync`  
**质疑**：`MakeGenericMethod(panelType)` 对从未在编译期静态引用的泛型实例化，在 IL2CPP AOT 环境下不生成代码 → 运行时 `ExecutionEngineException` 崩溃。`link.xml` 保留元数据但无法保留泛型方法体。  
**守方裁定**：✅ **接受 — 方案重构**  
**修正方案**：放弃反射，改用 **注册表模式**：`Dictionary<string, Func<object, Task>> _panelOpeners`，各面板模块在 `GameStartupFlow` 注册阶段注册自己的打开委托。

---

### WX-002 | 🔴高 | SceneLoader.UnloadScene 绕过 YooAsset 导致 Bundle 引用泄漏

**涉及章节**：§3.2 `ExitNodeAsync` / §4.4 场景策略  
**质疑**：当前 `UnloadScene` 直接调 `SceneManager.UnloadSceneAsync`，不释放 YooAsset SceneHandle 引用计数 → 累积后内存泄漏。  
**守方裁定**：✅ **接受 — SceneLoader 需修改**  
**修正方案**：
1. SceneLoader 新增 `_sceneHandleCache` 字典缓存 SceneHandle
2. `LoadSceneViaAssetServiceAsync` 加载后缓存 handle
3. 新增 `UnloadSceneAsync(SceneDefinition)` 方法，通过 `sceneHandle.UnloadAsync()` 正确释放
4. 整合方案表中 SceneLoader 变更幅度从"零修改"改为"小幅修改（+15 行）"

---

### WX-003 | 🔴高 | 反射调用参数匹配问题

**涉及章节**：§3.2 `OpenPanelByTypeNameAsync`  
**质疑**：`openMethod.Invoke(UIManager.Instance, new[] { data })` 参数匹配脆弱；`GetMethod("OpenPanelAsync")` 对泛型方法查找不稳定。  
**守方裁定**：✅ **合并到 WX-001 — 注册表模式替代后此问题自动消解**

---

### WX-004 | 🟡中 | Task.Yield() 不等于场景卸载完成

**涉及章节**：§3.2 `ExitNodeAsync`  
**质疑**：`Task.Yield()` 只让出一帧，场景卸载是异步多帧操作，旧场景系统可能仍在执行。  
**守方裁定**：✅ **接受**  
**修正方案**：`ExitNodeAsync` 改为 await SceneLoader 新增的 `UnloadSceneAsync` 返回的 Task（与 WX-002 联动）。

---

### WX-005 | 🟡中 | GetActiveScene 在 Additive 模式下判断错误

**涉及章节**：§3.2 `LoadSceneAsync` 辅助方法  
**质疑**：Additive 模式下 `GetActiveScene()` 永远是 Boot，判断永远不成立 → 无防重复加载保护。  
**守方裁定**：✅ **接受**  
**修正方案**：改用 `SceneManager.GetSceneByName(sceneDef.SceneName).isLoaded` 判断场景是否已加载。

---

### WX-006 | 🟡中 | "零修改 SceneLoader" 实际需改动

**涉及章节**：§4.1 整合方案  
**质疑**：SceneLoader 需要：缓存 SceneHandle、UnloadScene 返回 Task、处理重复加载检测、`_isLoading` 互斥影响。  
**守方裁定**：✅ **接受**  
**修正方案**：整合方案表中明确列出 SceneLoader 变更清单：
- 新增 `_sceneHandleCache: Dictionary<string, SceneHandle>`
- 新增 `UnloadSceneAsync(SceneDefinition): Task` 方法
- `_isLoading` 对 Additive 场景不互斥（仅对 Single 模式有效）
- 变更幅度从"零修改"改为"小幅修改（~20 行新增）"

---

### WX-007 | 🟡中 | Pop 时 CloseAllPanels 导致面板闪烁

**涉及章节**：§3.2 `PopAsync` / §4.3 导航流程  
**质疑**：Pop 的目标节点如果 CloseAllPanelsOnEnter=true，会先销毁再重建面板 → 违背"返回即恢复"直觉。  
**守方裁定**：✅ **接受**  
**修正方案**：`EnterNodeAsync` 新增 `isReturning` 参数。当 isReturning=true 且目标为纯 UI 节点时，跳过 CloseAllPanels，直接 OpenPanel（UIManager 的 OpenPanelAsync 已有"已打开则 OnRefresh"逻辑）。

---

### WX-008 | 🟡中 | LoadInitialScene 与 Navigator 首次 Push 冲突

**涉及章节**：§4.1 / §7 Phase 2  
**质疑**：`GameBootstrapper.Awake` 在 StartupFlow 完成后仍会调 `LoadInitialScene()`，与 Navigator.PushAsync 形成两条并行场景加载路径。  
**守方裁定**：✅ **接受**  
**修正方案**：当 `_startupFlowBehaviour != null` 且 RunAsync 正常完成时，跳过 `LoadInitialScene()`。整合方案表中 GameBootstrapper 变更幅度从"加 1 行"改为"改 ~5 行（条件跳过 + Navigator touch）"。

---

### WX-009 | 🟡中 | Type.GetType 跨 asmdef 失败

**涉及章节**：§3.1 / §4.2  
**质疑**：`Type.GetType(string)` 不带 AssemblyQualifiedName 时只搜索当前程序集，跨 asmdef 会返回 null。  
**守方裁定**：✅ **合并到 WX-001 — 注册表模式替代后无需 Type 解析**

---

### WX-010 | 🟡中 | Coroutine → async Task 桥接

**涉及章节**：§4.1 / §7 Phase 2  
**质疑**：BattleController 退出方法是 IEnumerator 协程，无法直接 await Task。  
**守方裁定**：✅ **接受**  
**修正方案**：AppFlowNavigator 新增同步入口 `void Pop()` / `void Push(FlowNodeSO, object)`，内部 `_ = PopAsync()` + 异常日志包装。BattleController 直接调用同步版本。

---

### WX-011 | 🟢低 | 热启动预留接口为空头支票

**涉及章节**：§4.3 / §8  
**质疑**：当前设计中 `StackEntry.Data` 是 object 无法序列化，"预留接口"描述给团队错误期望。  
**守方裁定**：✅ **接受**  
**修正方案**：删除"预留接口"描述，§8 明确标注为"V2 独立设计任务，需重新设计数据结构"。

---

### WX-012 | 🟢低 | transition 超时保护

**涉及章节**：§6 R6  
**质疑**：`_isTransitioning` 无超时机制，极端情况下导航器永久锁死。  
**守方裁定**：✅ **接受**  
**修正方案**：添加超时重置机制（默认 10s），超时后 `_isTransitioning = false` + LogError。§6 风险表新增 R7 条目。

---

## 收敛总结

| 统计 | 数值 |
|------|------|
| 总问题数 | 12 |
| 🔴 高 | 3（其中 1 个合并） |
| 🟡 中 | 7（其中 1 个合并） |
| 🟢 低 | 2 |
| 接受 | 12/12（100%） |
| 需重构 | 2（反射→注册表、SceneLoader 改造） |
| 需新增接口 | 2（同步 Pop/Push、UnloadSceneAsync） |
| 文档修正 | 5 处（整合方案表、Pop 语义、Bootstrapper、热启动、超时） |

**PK 结论**：全部问题一轮收敛，TDD 需要 v1.1 版本回写。核心变更：
1. 放弃反射 → 注册表模式
2. SceneLoader 小幅改造（SceneHandle 缓存 + UnloadSceneAsync）
3. Pop 语义优化（返回时不销毁重建面板）
4. 新增同步 API 入口桥接 Coroutine 场景

---

_PK 记录 | 2026-05-05 | 1 轮完成_
