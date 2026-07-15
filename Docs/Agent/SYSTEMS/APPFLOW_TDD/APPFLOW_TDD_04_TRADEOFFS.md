---
system: navigation
scope: appflow-tdd-tradeoffs
parent: APPFLOW_TDD_INDEX
last_verified: 2026-05-17
---

# AppFlow TDD — §5 权衡分析 + §6 风险与缓解

> 父文档：[SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md](SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md)

---

## 5. 权衡分析

### 5.1 方案对比

| 维度 | 方案 A: 栈式导航 (✅) | 方案 B: FSM 转换表 | 方案 C: Web Router |
|------|---------------------|-------------------|-------------------|
| "返回"语义 | 天然支持（Pop） | 需额外实现 | 需额外实现 |
| 新增页面成本 | 创建 SO + 配面板 | 改转换表 + 加状态 SO | 新增路由规则 |
| 实现复杂度 | **低**（~250 行核心） | 中（转换验证） | 高（路径解析） |
| 可逆性 | 高（一个 SO = 一个节点） | 中 | 中 |
| 深层嵌套 | 自然支持 | 需显式声明所有转换路径 | 自然支持 |
| 微信适配 | 无额外开销 | 无额外开销 | 路径字符串开销 |
| **放弃什么** | 复杂非线性跳转需 PopTo/Replace | 灵活性 | 简单性 |

### 5.2 设计取舍

| 决策 | 选了什么 | 放弃了什么 | 理由 |
|------|---------|-----------|------|
| 面板打开用注册表 + 自注册 | IL2CPP/WebGL 安全 + 零反射 + 零编译耦合 | 集中可见性（需跨文件查找注册点） | AOT 环境 + 数据驱动原则"新增内容不改代码"（PK UA-003） |
| IFlowData 标记接口 | 强类型 + V2 扩展无 break change | 稍许仪式感（每个 Data 类加一行 : IFlowData） | 避免 object 装箱 + 调试友好 + 编译期安全（PK UA-002） |
| ~~Battle 改 Additive~~ → **全 Single 模式** | Main⇄Battle 自动替换，零手动卸载 | Battle 中不保留 Boot/Main 场景 | DontDestroyOnLoad Singleton 常驻 + OnDestroy 清理 FairyGUI/弹幕（2026-05-06 重构） |
| Pop 时 Resume 面板（方案 B） | 面板 Hide/Show + IPanelSuspendable 生命周期 | Dispose+Recreate 的简单性 | 零 GC 面板恢复 + 保留面板状态（滚动位置/输入/动画）（v1.7 方案 B） |
| IFlowHandler 限 MonoBehaviour | SO 保持纯数据 | SO 子类化灵活性 | SO 共享实例 → 行为在 SO 上会互相覆盖（PK UA-008） |
| 不复用 StateMachine FSM | 导航器自带栈语义 | 复用已有代码 | FSM 的状态验证 + 转换表对导航场景是过度约束 |
| Singleton（MonoBehaviour） | 与 UIManager/SceneLoader 一致 | 可测试性（需 Mock） | 框架内部管理器统一用 Singleton（项目约定） |
| 同步 Push/Pop 入口 | Coroutine 场景零适配成本 | 丢失 await 异常传播 | try-catch + LogException 兜底（PK WX-010） |
| V1 不拆分 Navigator | 避免过度工程化 | 早期模块化 | 300 行硬性拆分规则兜底（PK UA-001） |

---

## 6. 风险与缓解

| # | 风险 | 影响 | 缓解 |
|---|------|------|------|
| R1 | FairyGUI 面板残留：Pop 返回时旧面板未关闭 | UI 叠加 | Pop 时 `CloseAllPanels()` 清理 leaving 层；面板 Suspend/Resume 有明确所有权跟踪（`OwnedPanelTypes`） |
| R2 | DontDestroyOnLoad 生命周期：Navigator 必须在 Bootstrapper 之后 | NullRef | Bootstrapper `InitializeSystemsAsync` 末尾 touch `AppFlowNavigator.Instance` + `#if EDITOR` 断言（PK UA-010） |
| R3 | 热重载 (Editor)：Domain Reload 后栈丢失 | 编辑器状态异常 | `#if UNITY_EDITOR` SerializeField/SerializeReference 保留栈数据 + OnEnable 恢复 + LogWarning 诊断（PK ET-005） |
| R4 | ~~反射裁剪~~ → **已消除**（v1.1 改用注册表模式，零反射） | — | — |
| R5 | Battle 场景 Additive 加载后 DanmakuSystem 初始化时序 | 系统未就绪 | EntitySystemBootstrap 已在 Battle 场景 Awake 中自初始化，不依赖场景加载模式 |
| R6 | 并发 Push/Pop（快速连点） | 栈状态不一致 | `_isTransitioning` 互斥锁 + 日志警告 + UI 层禁用按钮 |
| R7 | Transition 超时锁死 | 导航器永久不可用 | Coroutine 超时（10s）→ 强制重置 + LogError（PK WX-012 + UA-006） |
| R8 | 面板自注册时序：Navigator 尚未就绪时 [RIOM] 触发 | 注册失败 | `AfterSceneLoad` 时机确保所有 Singleton 已初始化；或用 lazy 注册队列 |
| R9 | ~~热启动恢复时 Data 类结构变更~~ → **风险已消除**（2026-05-17 热启动恢复暂时禁用） | 冷启动一律清栈 | 未来启用时再评估 |
| R10 | 存储写入失败（微信 `wx.setStorageSync` 容量满/异常） | 下次热启动无法恢复 | try-catch + 静默降级（不影响当前游戏）+ 超过 4KB 告警日志；当前热启动恢复已禁用，影响更低 |
