---
system: navigation
scope: appflow-tdd-core-design
parent: APPFLOW_TDD_INDEX
last_verified: 2026-05-07
---

# AppFlow TDD — §1~3 核心设计

> 父文档：[SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md](SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md)

---

## 1. 问题定义

### 1.1 当前痛点

| # | 问题 | 根因 | 影响 |
|---|------|------|------|
| P1 | 暂停/胜利后无法返回选关界面 | 所有退出路径都是 `SceneManager.LoadScene("Boot")` → 全启动流程重走 | 用户需多次点击才能回到选关 |
| P2 | 不存在"返回上一级"语义 | 没有导航历史记录 | 每新增一个返回路径都要硬编码 |
| P3 | UI 管理碎片化 | 框架层 `UIManager` vs Battle 场景 `MonoBehaviour Controller` 两套并行 | 面板残留、生命周期不可控 |
| P4 | 场景切换硬编码 | `SceneManager.LoadScene("Boot")` 散落业务代码 | 未使用已有 SceneLoader + SceneDefinition |
| P5 | 启动流程无法跳过 | 每次回主菜单都走 Loading→Privacy→MainMenu | 用户体验差 + 不必要的 GC spike |

### 1.2 为什么现在必须做

1. **向下做只会更乱**：多关卡、结算分享、商店等功能每加一个跳转就要抄一遍 hack
2. **微信小游戏性能**：场景切换 = GC spike + 资源重建，能不切就不切
3. **框架层已备齐基础设施**：`SceneLoader`、`UIManager`、`StateMachine`（SO 驱动）全部就绪，缺的只是串联它们的编排层

---

## 2. 架构设计

### 2.1 核心概念

```
┌───────────────────────────────────────────────────────┐
│                  AppFlowNavigator                       │
│  ┌─────────────────────────────────────────────────┐  │
│  │        Navigation Stack (LIFO)                   │  │
│  │  [MainMenu] → [LevelSelect] → [Battle]          │  │
│  └─────────────────────────────────────────────────┘  │
│                                                        │
│  Push(node, data)  /  Pop(returnData)                  │
│  PopTo(node)       /  Replace(node, data)              │
│  PopAll()          /  Peek()                           │
└────────────────────────┬──────────────────────────────┘
                         │ 内部调用
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
  ┌──────────────┐ ┌──────────┐ ┌────────────────┐
  │ SceneLoader  │ │UIManager │ │ IFlowHandler   │
  │ (场景加载)    │ │(面板管理) │ │ (节点钩子接口)  │
  └──────────────┘ └──────────┘ └────────────────┘
```

### 2.2 层级定位

```
模块依赖关系（补充 L3.5 层）：

  L3   FSM, WeChatBridge
  L3.5 Navigation (AppFlowNavigator)   ← NEW
  L4   GameLifecycle (GameBootstrapper)
```

AppFlowNavigator 放在 L3.5（编排层），依赖 L2 的 UIManager/SceneLoader 但不依赖 L4。GameBootstrapper 在初始化完成后触发首次 Push。

### 2.3 模块放置

```
Assets/_Framework/Navigation/
├── Scripts/
│   ├── FlowNodeSO.cs           # 导航节点定义
│   ├── AppFlowNavigator.cs     # 栈式导航器 Singleton
│   └── IFlowHandler.cs         # 可选：节点进入/退出钩子
├── Navigation.asmdef           # Assembly Definition
└── MODULE_README.md
```

---

## 3. 详细设计

### 3.1 FlowNodeSO — 导航节点

```csharp
using UnityEngine;
using MiniGameTemplate.Core;

namespace MiniGameTemplate.Navigation
{
    [CreateAssetMenu(menuName = "MiniGameTemplate/Navigation/Flow Node", order = 0)]
    public class FlowNodeSO : ScriptableObject
    {
        [Header("场景控制")]
        [SerializeField] private SceneDefinition _requiredScene;

        [Header("UI 面板")]
        [SerializeField] private string _panelTypeName;

        [Header("行为")]
        [SerializeField] private bool _unloadSceneOnExit = true;

        [Header("元数据")]
        [SerializeField] private string _displayName;

        [Header("序列化（Phase 4 栈恢复）")]
        [SerializeField] private string _nodeId;

        // --- Public API ---
        public SceneDefinition RequiredScene => _requiredScene;
        public string PanelTypeName => _panelTypeName;
        public bool UnloadSceneOnExit => _unloadSceneOnExit;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string NodeId => _nodeId;
    }
}
```

**设计决策**：

| 决策 | 选型 | 理由 |
|------|------|------|
| 面板打开用注册表 + 分散自注册 | `Dictionary<string, Func<IFlowData, Task>>` + 各面板 [RIOM] 自注册 | 零反射 + 零编译耦合 + 新增面板不改 StartupFlow（PK WX-001 + UA-003） |
| 导航数据强类型化 | `IFlowData` 标记接口 | 避免 object 装箱 + 为 V2 序列化预留扩展点 + 不产生 break change（PK UA-002） |
| 面板 Suspend/Resume（方案 B） | Push 时 Hide 面板（保留实例），Pop 时 Show 恢复 | 避免 Dispose+Recreate 的 GC 开销 + 保留面板状态（2026-05-07 v1.7） |
| 场景 = null 表示纯 UI 节点 | MainMenu / LevelSelect 都是纯 UI 节点 | 与当前项目布局一致（Boot 场景常驻，UI 靠 FairyGUI 全局管理） |

### 3.2 AppFlowNavigator — 栈式导航器

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Utils;
using MiniGameTemplate.Core;
using MiniGameTemplate.UI;

namespace MiniGameTemplate.Navigation
{
    public class AppFlowNavigator : Singleton<AppFlowNavigator>
    {
        // ---------- 栈 ----------
        private readonly List<StackEntry> _stack = new(8);
        
        [Serializable]
        public struct StackEntry
        {
            public FlowNodeSO Node;
            [SerializeReference] public IFlowData Data;
            /// <summary>该栈层拥有的面板类型列表（Suspend/Resume 用）。</summary>
            [NonSerialized] public List<Type> OwnedPanelTypes;
        }
        
        // ---------- 状态 ----------
        private bool _isTransitioning;
        
        // ---------- 面板打开注册表（PK WX-001/003/009） ----------
        private readonly Dictionary<string, Func<IFlowData, Task>> _panelOpeners = new(8);

        public void RegisterPanelOpener(string panelKey, Func<IFlowData, Task> opener)
        {
            _panelOpeners[panelKey] = opener;
        }

        // ---------- 事件 ----------
        public event Action<FlowNodeSO, FlowNodeSO> OnNavigated;

        // ---------- 属性 ----------
        public FlowNodeSO CurrentNode => _stack.Count > 0 ? _stack[^1].Node : null;
        public int StackDepth => _stack.Count;
        public bool IsTransitioning => _isTransitioning;

        // ================================================================
        //  PushAsync — 压入新节点
        // ================================================================
        public async Task PushAsync(FlowNodeSO node, IFlowData data = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (_isTransitioning) { /* LogWarning + return */ return; }

            _isTransitioning = true;
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
            try
            {
                var previousHandler = _currentFlowHandler as IFlowSuspendable;
                previousHandler?.OnFlowSuspend();

                // 挂起前节点拥有的面板（方案 B：Hide 而非 Dispose）
                var previous = CurrentNode;
                if (_stack.Count > 0)
                {
                    var entry = _stack[_stack.Count - 1];
                    entry.OwnedPanelTypes = UIManager.Instance.SuspendAllPanels();
                    _stack[_stack.Count - 1] = entry;
                }

                _stack.Add(new StackEntry { Node = node, Data = data });
                await EnterNodeAsync(node, data);
                OnNavigated?.Invoke(previous, node);
            }
            finally { _isTransitioning = false; StopTimeoutCoroutine(); }
        }

        // ================================================================
        //  PopAsync — 弹出栈顶（返回）
        // ================================================================
        public async Task PopAsync(IFlowData returnData = null)
        {
            if (_stack.Count <= 1 || _isTransitioning) return;

            _isTransitioning = true;
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
            try
            {
                var leaving = _stack[^1];
                _stack.RemoveAt(_stack.Count - 1);

                // Close leaving node's active panels
                UIManager.Instance.CloseAllPanels();
                UIManager.Instance.CloseSuspendedPanels(leaving.OwnedPanelTypes);
                
                await ExitNodeAsync(leaving.Node);

                // Resume returning node's suspended panels
                var returning = _stack[^1];
                UIManager.Instance.ResumePanels(returning.OwnedPanelTypes, returnData ?? returning.Data);
                if (_stack.Count > 0)
                {
                    var entry = _stack[_stack.Count - 1];
                    entry.OwnedPanelTypes = null;
                    _stack[_stack.Count - 1] = entry;
                }

                await EnterNodeAsync(returning.Node, returnData ?? returning.Data, isReturning: true);
                OnNavigated?.Invoke(leaving.Node, returning.Node);
            }
            finally { _isTransitioning = false; StopTimeoutCoroutine(); }
        }

        // ================================================================
        //  PopToAsync / ReplaceAsync / PopAllAsync — 见完整实现
        // ================================================================
        // PopToAsync: 从栈顶逐个 ExitNode 到 target，然后 EnterNode(target, isReturning:true)
        // ReplaceAsync: ExitNode(leaving) → 压入新 entry → EnterNode(new)
        // PopAllAsync: 逐个 ExitNode 到根 → EnterNode(root, isReturning:true)

        // ================================================================
        //  Sync API — Coroutine 桥接（PK WX-010）
        // ================================================================
        public void Push(FlowNodeSO node, IFlowData data = null) => _ = PushAsyncSafe(node, data);
        public void Pop(IFlowData returnData = null) => _ = PopAsyncSafe(returnData);

        // ================================================================
        //  Transition 超时保护（PK WX-012 + UA-006 + ET-004）
        // ================================================================
        private const float TRANSITION_TIMEOUT = 10f;
        private Coroutine _timeoutCoroutine;
        // Coroutine 计时 → 超时强制重置 _isTransitioning + LogError

        // ================================================================
        //  EnterNodeAsync — 节点进入
        // ================================================================
        private async Task EnterNodeAsync(FlowNodeSO node, IFlowData data, bool isReturning = false)
        {
            // 1. 加载场景（如果需要）
            if (node.RequiredScene != null)
                await SceneLoader.Instance.LoadSceneAsync(node.RequiredScene);

            // 2. 打开面板（仅首次进入时，返回时面板已通过 Resume 恢复）
            if (!isReturning && !string.IsNullOrEmpty(node.PanelTypeName))
                await OpenPanelByRegistryAsync(node.PanelTypeName, data);

            // 3. 查找场景内 IFlowHandler
            _currentFlowHandler = null;
            if (node.RequiredScene != null) { /* GetComponentInChildren<IFlowHandler>() */ }

            // 4. 调用钩子（Enter vs Resume）
            if (isReturning)
                (_currentFlowHandler as IFlowSuspendable)?.OnFlowResume(data);
            else
                _currentFlowHandler?.OnFlowEnter(data);
        }

        // ================================================================
        //  ExitNodeAsync — 节点退出
        // ================================================================
        private async Task ExitNodeAsync(FlowNodeSO node)
        {
            _currentFlowHandler?.OnFlowExit();
            _currentFlowHandler = null;
            if (node.RequiredScene != null && node.UnloadSceneOnExit)
                await SceneLoader.Instance.UnloadSceneAsync(node.RequiredScene);
        }

        // ================================================================
        //  Editor 热重载支持（PK ET-005）
        // ================================================================
        // #if UNITY_EDITOR: SerializeField 保存/恢复栈 + OnEnable/OnDisable
        // EditorOnNavigated 静态事件（PK ET-007）
    }
}
```

### 3.3 IFlowData — 导航数据标记接口（PK UA-002）

```csharp
namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 所有导航数据的标记接口。
    /// - Data 必须是 class + 实现 ToString() 方便调试
    /// - 推荐标记 [Serializable]；热重载恢复则必须 [Serializable]（PK ET-005）
    /// - V2 可在此接口上扩展序列化能力而无 break change。
    /// </summary>
    public interface IFlowData { }
}
```

### 3.4 IFlowHandler / IFlowSuspendable — 可选钩子接口

```csharp
namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 由场景内 MonoBehaviour 实现（如 BattleFlowController）。
    /// ⚠️ ScriptableObject 禁止实现此接口（SO=纯数据原则）。
    /// Navigator 在场景加载后通过根对象 GetComponentInChildren 查找实现者。
    /// </summary>
    public interface IFlowHandler
    {
        void OnFlowEnter(IFlowData data);
        void OnFlowExit();
    }

    /// <summary>
    /// 可选挂起/恢复接口。Push 时 OnFlowSuspend()，Pop 返回时 OnFlowResume()。
    /// 纯 UI 节点通常无需实现 — 面板 Suspend/Resume 已充分处理。（PK UA-007）
    /// </summary>
    public interface IFlowSuspendable
    {
        void OnFlowSuspend();
        void OnFlowResume(IFlowData data);
    }
}
```

### 3.5 IPanelSuspendable — UI 面板挂起/恢复接口（v1.7 新增）

```csharp
// Assets/_Framework/UISystem/Scripts/IUIPanel.cs
namespace MiniGameTemplate.UI
{
    /// <summary>
    /// 可选接口。面板实现此接口以响应 Navigator Push/Pop 的挂起/恢复。
    /// 未实现此接口的面板仅做 Hide/Show，无额外生命周期回调。
    ///
    /// 实现场景：暂停/恢复动画/计时器、隐藏/显示广告、取消/重新订阅事件、Pop 返回时刷新数据
    /// </summary>
    public interface IPanelSuspendable
    {
        void OnSuspend();
        void OnResume(object data);
    }
}
```

**UIManager 新增 API（v1.7）**：

| 方法 | 说明 |
|------|------|
| `List<Type> SuspendAllPanels()` | 挂起所有活跃面板（Hide + OnSuspend），返回挂起的类型列表 |
| `void ResumePanels(List<Type>, object)` | 恢复指定类型的面板（Show + OnResume） |
| `void CloseSuspendedPanels(List<Type>)` | 关闭（Dispose）指定的挂起面板 |
| `bool IsPanelSuspended<T>()` | 查询面板是否处于挂起状态 |

**面板行为矩阵**：

| 导航操作 | 对 leaving 层面板 | 对 returning 层面板 |
|---------|------------------|-------------------|
| Push | SuspendAllPanels（Hide） | — |
| Pop | CloseAllPanels + CloseSuspendedPanels | ResumePanels |
| PopTo | CloseAllPanels + Close 中间层 | ResumePanels 目标层 |
| PopAll | CloseAllPanels + Close 中间层 | ResumePanels 根层 |
| Replace | CloseAllPanels + CloseSuspendedPanels | — (EnterNode 打开新面板) |
