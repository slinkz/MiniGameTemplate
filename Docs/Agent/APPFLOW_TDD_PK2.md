---
system: navigation
scope: appflow-tdd-pk2
last_verified: 2026-05-05
---

# AppFlow TDD — PK 对抗评审记录 #2（Unity 架构师视角）

> **PK 规格**：2 轮攻防 + 回写  
> **攻方角色**：Unity 架构师（数据驱动模块化专家，专精 ScriptableObjects、解耦系统、SRP）  
> **守方角色**：软件架构师（框架设计者）  
> **目标文档**：`APPFLOW_TDD.md` v1.1  
> **收敛结果**：10 问题 / 2 轮 / 100% 收敛

---

## Round 1 攻方质疑（10 问题）

### UA-001 | 🔴高 | AppFlowNavigator 违反单一职责（栈+场景+面板+超时）

**涉及章节**：§3.2  
**质疑**：AppFlowNavigator 同时承担栈管理、场景加载编排、面板注册表维护、Update() 超时检测。V2 加入转场动画/拦截器/深层链接后必然膨胀成上帝类。  
**守方裁定**：⚠️ 部分接受 → Round 2 收敛  
**最终方案**：
- V1 保持当前结构（核心 ~250 行，方法均 private 隔离）
- TDD §8 新增硬性规则：Navigator.cs 超过 300 行时，该变更必须同步提取 ISceneTransition/IPanelResolver 策略接口
- 超时检测改为 Coroutine（UA-006 联动），减少不必要 Update
- coding-standards + code-review-checklist skill 作为自动化监控手段

---

### UA-002 | 🔴高 | object data 类型擦除违背数据驱动核心原则

**涉及章节**：§3.2 StackEntry.Data / PushAsync  
**质疑**：object 导致：向下转型 + null 检查 + 无法 Inspector 预览 + 装箱 + V2 序列化困难。  
**守方裁定**：⚠️ 部分接受 → Round 2 升级为完全接受  
**最终方案**：
- V1 立即引入 `IFlowData` 空标记接口
- 签名改为 `Func<IFlowData, Task>`（避免 V2 break change）
- 所有 Data 类实现 IFlowData（零额外成本：一行继承声明）
- 约定：Data 必须是 class + 实现 ToString()
- `#if DEVELOPMENT_BUILD` 添加类型校验断言

---

### UA-003 | 🔴高 | 注册表模式将 Navigator 与所有面板类型编译耦合

**涉及章节**：§3.2 RegisterPanelOpener / §7 Phase 2.4  
**质疑**：GameStartupFlow 中集中注册所有面板 → 逆向编译依赖 → 新增面板=改代码=违背数据驱动。  
**守方裁定**：✅ 接受  
**修正方案**：
- 每个面板类定义 `public const string PanelKey`
- 面板注册分散到各面板所在 asmdef 的 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 中自注册
- GameStartupFlow 不再集中调用 RegisterPanelOpener
- 新增面板 = 0 行 GameStartupFlow 改动

---

### UA-004 | 🟡中 | LoadSceneAsync/UnloadSceneAsync 抽象层不对称

**涉及章节**：§3.2 LoadSceneAsync vs UnloadSceneAsync  
**质疑**：LoadSceneAsync 半绕过 SceneLoader（自注册 sceneLoaded 事件），UnloadSceneAsync 完全委托。加载/卸载路径层级不对称。  
**守方裁定**：✅ 接受  
**修正方案**：
- SceneLoader v1.1 新增 `public Task LoadSceneAsync(SceneDefinition)` 对称 API
- Navigator 简化为 `return SceneLoader.Instance.LoadSceneAsync(sceneDef)`
- 删除 Navigator 中自注册 sceneLoaded 事件的代码

---

### UA-005 | 🟡中 | _panelOpeners 字典 key 为魔法字符串

**涉及章节**：§3.2 / §4.2  
**质疑**：FlowNodeSO._panelTypeName 是检查器输入的裸字符串，与注册代码中的字符串两处散落。  
**守方裁定**：✅ 接受 — 合并到 UA-003  
**修正方案**：面板类定义 `public const string PanelKey`，FlowNodeSO 中填写同一常量。未来 CustomEditor 支持下拉选择。

---

### UA-006 | 🟡中 | Update() 仅为超时检测常驻运行

**涉及章节**：§3.2 超时保护  
**质疑**：99.99% 时间 _isTransitioning==false 仍每帧执行。违背"可事件驱动的 Update 逻辑"反模式警告。  
**守方裁定**：✅ 接受  
**修正方案**：改为 Coroutine — 仅在 _isTransitioning=true 时 StartCoroutine，transition 结束时 StopCoroutine。零 Update 开销。

---

### UA-007 | 🟡中 | Push 时不 Exit 前节点 — 缺少挂起/恢复语义

**涉及章节**：§3.2 PushAsync  
**质疑**：Push 不调用前节点 IFlowHandler.OnFlowExit()。如果前节点有事件订阅等全局副作用，它们仍在运行。缺少明确的 Suspend/Resume 概念。  
**守方裁定**：⚠️ 部分接受 → Round 2 收敛  
**最终方案**：
- IFlowHandler 保留 2 个方法：`OnFlowEnter(IFlowData)` + `OnFlowExit()`
- 新增独立接口 `IFlowSuspendable`：`OnFlowSuspend()` + `OnFlowResume(IFlowData)`
- Navigator Push 时：`is IFlowSuspendable` → 调用 OnFlowSuspend()
- Navigator Pop 返回时：`is IFlowSuspendable` → 调用 OnFlowResume(data)
- 纯 UI 节点：CloseAllPanels 已充分"挂起"，不需实现 IFlowSuspendable
- 场景节点：BattleFlowController 实现 IFlowSuspendable 管理游戏状态暂停/恢复

---

### UA-008 | 🟡中 | FlowNodeSO 继承 IFlowHandler 违反 SO=纯数据原则

**涉及章节**：§3.2 / §3.3  
**质疑**：SO 应该是纯数据容器。让 SO 实现 IFlowHandler → SO 变成活对象 → Editor 多实例共享同一 SO 时状态污染。  
**守方裁定**：✅ 接受  
**修正方案**：
- 移除"FlowNodeSO 子类可实现 IFlowHandler"的描述
- IFlowHandler 由场景内 MonoBehaviour 实现
- Navigator 在场景加载后通过约定寻找场景内实现 IFlowHandler 的组件
- 纯 UI 节点无需 IFlowHandler

---

### UA-009 | 🟡中 | 缺少导航栈 Editor 可视化工具

**涉及章节**：§7 实施计划  
**质疑**：Singleton 无 CustomEditor 在调试时是黑盒。SO 架构应在 Inspector 展示运行时值。  
**守方裁定**：✅ 接受  
**修正方案**：实施计划新增 Phase 1.7：`AppFlowNavigatorEditor` — PlayMode 显示栈内容 + 快速 Pop/Push 按钮。

---

### UA-010 | 🟢低 | Singleton 自动创建时序不明确

**涉及章节**：§6 R2 / §4.1  
**质疑**：如果在 Bootstrapper touch 之前有代码访问 Navigator.Instance，实例在系统初始化链之前被创建。  
**守方裁定**：✅ 接受  
**修正方案**：Navigator Instance getter 首次创建时 `#if UNITY_EDITOR` 断言检查 UIManager/SceneLoader 是否就绪。

---

## Round 2 追问（3 问题）

### UA-001-R2 | SRP 折中：谁监控 300 行阈值？

**攻方追问**：独立开发者无 PR gate，300 行折中靠什么执行？另外 LoadScene/UnloadScene/OpenPanel 是 private 吗？  
**守方回应**：方法已全部 private ✅ + 300 行规则写入 TDD §8 + coding-standards skill 做编码前检查 → **收敛**

### UA-002-R2 | object→IFlowData 签名应在 V1 直接落地

**攻方追问**：V1 发布 `Func<object, Task>` 后 V2 改为 `Func<IFlowData, Task>` 是 break change。为何不现在就用？  
**守方回应**：完全正确，升级为完全接受。V1 直接定义 IFlowData 接口 + 改签名 → **收敛**

### UA-007-R2 | IFlowHandler 接口膨胀 + CloseAllPanels 语义重叠

**攻方追问**：Enter/Exit/Suspend/Resume 4 方法是否所有实现者都要？纯 UI 节点面板已被 CloseAllPanels 关闭，还需 Suspend 通知吗？  
**守方回应**：拆为两个独立接口（IFlowHandler + IFlowSuspendable），按需实现。纯 UI 节点不实现 IFlowSuspendable → **收敛**

---

## 最终收敛总结

| 统计 | 数值 |
|------|------|
| 总问题数 | 10 |
| 🔴 高 | 3 |
| 🟡 中 | 6 |
| 🟢 低 | 1 |
| 完全接受 | 7（UA-003/004/005/006/008/009/010，含合并） |
| 部分接受→收敛 | 3（UA-001/002/007，均 Round 2 收敛） |
| 驳回 | 0 |
| 轮次 | 2 |

**核心架构变更清单**：
1. **IFlowData 接口** — 替代 object，V1 直接落地
2. **面板自注册** — 分散 RegisterPanelOpener 到各面板 asmdef，消除编译耦合
3. **SceneLoader 对称 API** — LoadSceneAsync 返回 Task，Navigator 不再自注册 sceneLoaded
4. **IFlowSuspendable** — 独立接口，Push 时挂起/Pop 时恢复
5. **IFlowHandler 限制为 MonoBehaviour** — SO 不实现行为接口
6. **超时检测改 Coroutine** — 移除 Update()
7. **Editor 可视化** — Phase 1.7 新增 AppFlowNavigatorEditor
8. **300 行拆分规则** — TDD §8 硬性约束

---

_PK 记录 #2 | 2026-05-05 | 2 轮完成_
