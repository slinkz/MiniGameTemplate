---
system: navigation
scope: appflow-tdd
last_verified: 2026-05-06
related_code: Assets/_Framework/Navigation/*.cs, Assets/_Game/Scripts/GameStartupFlow.cs, Assets/_Game/Scenes/Main.unity, Assets/_Game/ScriptableObjects/Config/SD_Main.asset
---

# AppFlow 导航系统 — 技术设计文档 (TDD)

> **版本**：v1.6（双 Single 场景切换重构）  
> **状态**：✅ Phase 1~4 全部完成 + 3 轮 PK 评审通过 + 场景策略重构  
> **作者**：广智  
> **日期**：2026-05-06  
> **ADR**：ADR-034  
> **PK 记录**：  
> - [APPFLOW_TDD_PK.md](APPFLOW_TDD_PK.md)（#1 微信全栈：12 问题 / 1 轮 / 100% 收敛）  
> - [APPFLOW_TDD_PK2.md](APPFLOW_TDD_PK2.md)（#2 Unity架构师：10 问题 / 2 轮 / 100% 收敛）  
> - [APPFLOW_TDD_PK3.md](APPFLOW_TDD_PK3.md)（#3 编辑器工具开发者：10 问题 / 2 轮 / 100% 收敛）

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
    /// <summary>
    /// 定义一个导航节点（屏幕/页面）。
    /// 每个节点声明它需要什么场景 + 什么 UI 面板。
    /// 纯数据，零逻辑。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Navigation/Flow Node", order = 0)]
    public class FlowNodeSO : ScriptableObject
    {
        [Header("场景控制")]
        [Tooltip("进入此节点时需要加载的场景。null = 纯 UI 节点（不切场景）")]
        [SerializeField] private SceneDefinition _requiredScene;

        [Header("UI 面板")]
        [Tooltip("进入此节点时需要打开的面板注册表 key。空 = 不自动打开面板。需在 GameStartupFlow 中通过 RegisterPanelOpener 注册对应委托。")]
        [SerializeField] private string _panelTypeName;

        [Header("行为")]
        [Tooltip("离开时是否卸载场景（仅 _requiredScene != null 时生效）")]
        [SerializeField] private bool _unloadSceneOnExit = true;

        [Tooltip("进入此节点前是否关闭所有已打开面板")]
        [SerializeField] private bool _closeAllPanelsOnEnter = true;

        [Header("元数据")]
        [SerializeField] private string _displayName;

        // --- Public API ---
        public SceneDefinition RequiredScene => _requiredScene;
        public string PanelTypeName => _panelTypeName;
        public bool UnloadSceneOnExit => _unloadSceneOnExit;
        public bool CloseAllPanelsOnEnter => _closeAllPanelsOnEnter;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    }
}
```

**设计决策**：

| 决策 | 选型 | 理由 |
|------|------|------|
| 面板打开用注册表 + 分散自注册 | `Dictionary<string, Func<IFlowData, Task>>` + 各面板 [RIOM] 自注册 | 零反射 + 零编译耦合 + 新增面板不改 StartupFlow（PK WX-001 + UA-003） |
| 导航数据强类型化 | `IFlowData` 标记接口 | 避免 object 装箱 + 为 V2 序列化预留扩展点 + 不产生 break change（PK UA-002） |
| `CloseAllPanelsOnEnter` 默认 true | 进入节点时清理前一个节点的面板残留 | 安全起见避免 UI 泄漏；Pop 返回时通过 isReturning 跳过（PK WX-007） |
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
    /// <summary>
    /// 全局导航器。维护一个 FlowNode 栈，提供 Push/Pop/Replace 语义。
    /// 
    /// 生命周期：Singleton<T>（MonoBehaviour, DontDestroyOnLoad）
    /// 初始化时机：GameBootstrapper 系统初始化完成后、IStartupFlow 执行前
    /// 
    /// 线程安全：无锁（WebGL 单线程 + Unity API 限主线程）
    /// </summary>
    public class AppFlowNavigator : Singleton<AppFlowNavigator>
    {
        // ---------- 栈 ----------
        private readonly List<StackEntry> _stack = new(8);
        
        private struct StackEntry
        {
            public FlowNodeSO Node;
            public IFlowData Data;    // 传递给该节点的参数（PK UA-002：强类型化）
        }
        
        // ---------- 状态 ----------
        private bool _isTransitioning;
        
        // ---------- 面板打开注册表（替代反射，PK WX-001/003/009） ----------
        private readonly Dictionary<string, Func<IFlowData, Task>> _panelOpeners = new(8);

        /// <summary>
        /// 注册面板打开委托。各面板在自己的 asmdef 中通过 [RuntimeInitializeOnLoadMethod] 自注册。
        /// key = FlowNodeSO._panelTypeName（如 "MainMenuPanel"，由面板类定义 public const string PanelKey）
        /// （PK UA-003：分散注册，消除编译耦合）
        /// </summary>
        public void RegisterPanelOpener(string panelKey, Func<IFlowData, Task> opener)
        {
            _panelOpeners[panelKey] = opener;
        }

        // ---------- 事件 ----------
        /// <summary>节点切换完成后触发。Args: (离开的节点, 进入的节点)</summary>
        public event Action<FlowNodeSO, FlowNodeSO> OnNavigated;

        // ---------- 属性 ----------
        public FlowNodeSO CurrentNode => _stack.Count > 0 ? _stack[^1].Node : null;
        public int StackDepth => _stack.Count;
        public bool IsTransitioning => _isTransitioning;

        // ================================================================
        //  Public Navigation API
        // ================================================================

        /// <summary>
        /// 压入新节点（前进）。
        /// </summary>
        /// <param name="node">目标节点 SO</param>
        /// <param name="data">传递给目标节点的参数（实现 IFlowData）</param>
        public async Task PushAsync(FlowNodeSO node, IFlowData data = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (_isTransitioning)
            {
                Debug.LogWarning($"[AppFlow] PushAsync({node.DisplayName}) ignored — transition in progress.");
                return;
            }

            _isTransitioning = true;
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine()); // PK UA-006 + ET-004
            try
            {
                // 挂起前节点（PK UA-007）
                var previousHandler = _currentFlowHandler as IFlowSuspendable;
                previousHandler?.OnFlowSuspend();

                var previous = CurrentNode;
                _stack.Add(new StackEntry { Node = node, Data = data });
                await EnterNodeAsync(node, data);
                OnNavigated?.Invoke(previous, node);
#if UNITY_EDITOR
                EditorOnNavigated?.Invoke(previous, node); // PK ET-007
#endif
            }
            finally
            {
                _isTransitioning = false;
                StopTimeoutCoroutine(); // PK ET-004：正确停止方式
            }
        }

        /// <summary>
        /// 弹出栈顶（返回上一级）。
        /// </summary>
        /// <param name="returnData">传回给上一节点的数据</param>
        public async Task PopAsync(IFlowData returnData = null)
        {
            if (_stack.Count <= 1)
            {
                Debug.LogWarning("[AppFlow] PopAsync — already at root node. Ignoring.");
                return;
            }
            if (_isTransitioning)
            {
                Debug.LogWarning("[AppFlow] PopAsync ignored — transition in progress.");
                return;
            }

            _isTransitioning = true;
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
            try
            {
                var leaving = _stack[^1];
                _stack.RemoveAt(_stack.Count - 1);
                
                await ExitNodeAsync(leaving.Node);
                
                var returning = _stack[^1];
                await EnterNodeAsync(returning.Node, returnData ?? returning.Data, isReturning: true);
                OnNavigated?.Invoke(leaving.Node, returning.Node);
#if UNITY_EDITOR
                EditorOnNavigated?.Invoke(leaving.Node, returning.Node);
#endif
            }
            finally
            {
                _isTransitioning = false;
                StopTimeoutCoroutine();
            }
        }

        /// <summary>
        /// 弹到指定节点（跳过中间层）。
        /// </summary>
        public async Task PopToAsync(FlowNodeSO target, IFlowData returnData = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (_isTransitioning) return;

            int targetIndex = -1;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].Node == target) { targetIndex = i; break; }
            }

            if (targetIndex < 0)
            {
                Debug.LogWarning($"[AppFlow] PopToAsync — node '{target.DisplayName}' not found in stack.");
                return;
            }

            _isTransitioning = true;
            try
            {
                // 从栈顶逐个退出到目标
                while (_stack.Count - 1 > targetIndex)
                {
                    var leaving = _stack[^1];
                    _stack.RemoveAt(_stack.Count - 1);
                    await ExitNodeAsync(leaving.Node);
                }

                var returning = _stack[^1];
                await EnterNodeAsync(returning.Node, returnData ?? returning.Data, isReturning: true);
                OnNavigated?.Invoke(null, returning.Node);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// 替换栈顶节点（不留历史）。
        /// 典型用途：Battle 结束 → 直接到 LevelSelect，不保留 Battle 在栈里。
        /// </summary>
        public async Task ReplaceAsync(FlowNodeSO node, IFlowData data = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (_isTransitioning) return;

            _isTransitioning = true;
            try
            {
                FlowNodeSO previous = null;
                if (_stack.Count > 0)
                {
                    var leaving = _stack[^1];
                    previous = leaving.Node;
                    _stack.RemoveAt(_stack.Count - 1);
                    await ExitNodeAsync(leaving.Node);
                }

                _stack.Add(new StackEntry { Node = node, Data = data });
                await EnterNodeAsync(node, data);
                OnNavigated?.Invoke(previous, node);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// 清空栈回到根节点。
        /// </summary>
        public async Task PopAllAsync()
        {
            if (_stack.Count <= 1) return;
            if (_isTransitioning) return;

            _isTransitioning = true;
            try
            {
                // 从栈顶逐个退出到根
                while (_stack.Count > 1)
                {
                    var leaving = _stack[^1];
                    _stack.RemoveAt(_stack.Count - 1);
                    await ExitNodeAsync(leaving.Node);
                }

                var root = _stack[0];
                await EnterNodeAsync(root.Node, root.Data, isReturning: true);
                OnNavigated?.Invoke(null, root.Node);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        // ================================================================
        //  Sync API — Coroutine 场景桥接（PK WX-010）
        // ================================================================

        /// <summary>
        /// 同步 Push（fire-and-forget + 异常日志）。供 Coroutine 场景使用。
        /// </summary>
        public void Push(FlowNodeSO node, IFlowData data = null)
        {
            _ = PushAsyncSafe(node, data);
        }

        /// <summary>
        /// 同步 Pop（fire-and-forget + 异常日志）。供 Coroutine 场景使用。
        /// </summary>
        public void Pop(IFlowData returnData = null)
        {
            _ = PopAsyncSafe(returnData);
        }

        private async Task PushAsyncSafe(FlowNodeSO node, IFlowData data)
        {
            try { await PushAsync(node, data); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        private async Task PopAsyncSafe(IFlowData returnData)
        {
            try { await PopAsync(returnData); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        // ================================================================
        //  Transition 超时保护（PK WX-012 + UA-006 + ET-004）
        // ================================================================

        private const float TRANSITION_TIMEOUT = 10f;
        private Coroutine _timeoutCoroutine; // PK ET-004：缓存引用以正确停止

        /// <summary>
        /// 仅在 _isTransitioning=true 时运行。transition 结束时通过 StopTimeoutCoroutine 停止。
        /// 零 Update 开销。（PK UA-006）
        /// </summary>
        private System.Collections.IEnumerator TransitionTimeoutCoroutine()
        {
            float elapsed = 0f;
            while (elapsed < TRANSITION_TIMEOUT)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_isTransitioning)
            {
                Debug.LogError("[AppFlow] Transition timed out! Forcibly resetting _isTransitioning.");
                _isTransitioning = false;
            }
        }

        private void StopTimeoutCoroutine()
        {
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }
        }

        // ================================================================
        //  Editor-only 调试钩子（PK ET-007）
        // ================================================================

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only 静态事件。Editor 工具可无侵入挂载调试逻辑（如导航历史记录、自动截图）。
        /// 零运行时开销。
        /// </summary>
        internal static event Action<FlowNodeSO, FlowNodeSO> EditorOnNavigated;
#endif

        // ================================================================
        //  Editor 热重载支持（PK ET-005）
        // ================================================================

#if UNITY_EDITOR
        [SerializeField] private List<FlowNodeSO> _editorStackNodes = new();
        [SerializeReference] private List<IFlowData> _editorStackData = new();

        private void OnEnable()
        {
            // 恢复栈（Domain Reload 后）
            if (_editorStackNodes.Count > 0 && _stack.Count == 0)
            {
                for (int i = 0; i < _editorStackNodes.Count; i++)
                {
                    var node = _editorStackNodes[i];
                    var data = i < _editorStackData.Count ? _editorStackData[i] : null;
                    if (node == null) continue;
                    _stack.Add(new StackEntry { Node = node, Data = data });
                    if (data == null && node != null)
                        Debug.LogWarning($"[AppFlow Editor] Node '{node.DisplayName}' Data lost after reload. " +
                            "Add [Serializable] to your IFlowData class for hot-reload support.");
                }
                // 重新查找 IFlowHandler
                var currentNode = CurrentNode;
                if (currentNode?.RequiredScene != null)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(currentNode.RequiredScene.SceneName);
                    if (scene.isLoaded)
                    {
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            _currentFlowHandler = root.GetComponentInChildren<IFlowHandler>();
                            if (_currentFlowHandler != null) break;
                        }
                    }
                }
            }
        }

        private void OnDisable()
        {
            // 存储栈到 SerializeField（Domain Reload 前）
            _editorStackNodes.Clear();
            _editorStackData.Clear();
            foreach (var entry in _stack)
            {
                _editorStackNodes.Add(entry.Node);
                _editorStackData.Add(entry.Data);
            }
        }
#endif

        // ================================================================
        //  IFlowHandler / IFlowSuspendable 查找（PK UA-008）
        // ================================================================

        /// <summary>当前活跃的 IFlowHandler（场景内 MonoBehaviour）。</summary>
        private IFlowHandler _currentFlowHandler;

        // ================================================================
        //  Internal — 节点进入/退出
        // ================================================================

        private async Task EnterNodeAsync(FlowNodeSO node, IFlowData data, bool isReturning = false)
        {
            // 1. 清面板（Pop 返回纯 UI 节点时跳过，PK WX-007）
            if (node.CloseAllPanelsOnEnter && !isReturning)
            {
                UIManager.Instance.CloseAllPanels();
            }

            // 2. 加载场景（如果需要）— 对称 API（PK UA-004）
            if (node.RequiredScene != null)
            {
                await SceneLoader.Instance.LoadSceneAsync(node.RequiredScene);
            }

            // 3. 打开面板（如果配置了）— 注册表模式，PK WX-001
            if (!string.IsNullOrEmpty(node.PanelTypeName))
            {
                await OpenPanelByRegistryAsync(node.PanelTypeName, data);
            }

            // 4. 查找场景内 IFlowHandler（PK UA-008：不在 SO 上）
            _currentFlowHandler = null;
            if (node.RequiredScene != null)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(node.RequiredScene.SceneName);
                if (scene.isLoaded)
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        _currentFlowHandler = root.GetComponentInChildren<IFlowHandler>();
                        if (_currentFlowHandler != null) break;
                    }
                }
            }

            // 5. 调用钩子（PK UA-007：区分 Enter vs Resume）
            if (isReturning)
            {
                (_currentFlowHandler as IFlowSuspendable)?.OnFlowResume(data);
            }
            else
            {
                _currentFlowHandler?.OnFlowEnter(data);
            }
        }

        private async Task ExitNodeAsync(FlowNodeSO node)
        {
            // 1. 调用钩子（PK UA-008：从场景 MonoBehaviour 获取）
            _currentFlowHandler?.OnFlowExit();
            _currentFlowHandler = null;

            // 2. 卸载场景（如果需要）— await 完成，PK WX-002/004
            if (node.RequiredScene != null && node.UnloadSceneOnExit)
            {
                await SceneLoader.Instance.UnloadSceneAsync(node.RequiredScene);
            }
        }

        // ================================================================
        //  Internal — 辅助方法
        // ================================================================

        /// <summary>
        /// 通过注册表调用面板打开委托。零反射，IL2CPP/WebGL 安全。（PK WX-001）
        /// 
        /// 注册时机：各面板在自己的 asmdef 中通过 [RuntimeInitializeOnLoadMethod] 自注册。（PK UA-003）
        /// 示例（在 MainMenuPanel.cs 中）：
        ///   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        ///   private static void RegisterSelf()
        ///   {
        ///       AppFlowNavigator.Instance.RegisterPanelOpener(PanelKey,
        ///           data => UIManager.Instance.OpenPanelAsync&lt;MainMenuPanel&gt;(data as MainMenuPanelData)
        ///                       .ContinueWith(_ => Task.CompletedTask));
        ///   }
        /// </summary>
        private async Task OpenPanelByRegistryAsync(string panelKey, IFlowData data)
        {
            if (!_panelOpeners.TryGetValue(panelKey, out var opener))
            {
                Debug.LogError($"[AppFlow] No panel opener registered for key: '{panelKey}'. " +
                    "Ensure the panel class has [RuntimeInitializeOnLoadMethod] self-registration.");
                return;
            }

            await opener(data);
        }
    }
}
```

### 3.3 IFlowData — 导航数据标记接口（PK UA-002）

```csharp
namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 所有导航数据的标记接口。
    /// 约束：
    /// - Data 必须是 class + 实现 ToString() 方便调试
    /// - 推荐标记 [Serializable]；如需 Editor 热重载恢复则 **必须** [Serializable]（PK ET-005）
    /// V2 可在此接口上扩展序列化能力（如 bool IsSerializable）而无 break change。
    /// </summary>
    public interface IFlowData { }
}
```

### 3.4 IFlowHandler — 可选钩子接口（PK UA-008 修正：仅 MonoBehaviour 实现）

```csharp
namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 可选接口。由场景内 MonoBehaviour 实现（如 BattleFlowController）。
    /// ⚠️ ScriptableObject 禁止实现此接口（SO=纯数据原则）。
    /// 
    /// Navigator 在场景加载后通过场景根对象 GetComponentInChildren 查找实现者。
    /// 纯 UI 节点无需实现 — 面板自身的 OnOpen/OnClose 已覆盖。
    /// </summary>
    public interface IFlowHandler
    {
        /// <summary>导航器进入此节点时调用（首次 Push）。</summary>
        void OnFlowEnter(IFlowData data);

        /// <summary>导航器永久离开此节点时调用（节点被移出栈）。</summary>
        void OnFlowExit();
    }

    /// <summary>
    /// 可选挂起/恢复接口。场景节点（如 Battle）实现此接口管理暂停/恢复。
    /// Push 时 Navigator 调用 OnFlowSuspend()，Pop 返回时调用 OnFlowResume()。
    /// 纯 UI 节点通常无需实现 — CloseAllPanels 已充分"挂起"。
    /// （PK UA-007）
    /// </summary>
    public interface IFlowSuspendable
    {
        /// <summary>被新节点压入时调用（挂起：释放事件订阅、暂停逻辑）。</summary>
        void OnFlowSuspend();

        /// <summary>从上层节点 Pop 回来时调用（恢复：重新订阅事件、继续逻辑）。</summary>
        void OnFlowResume(IFlowData data);
    }
}
```

### 3.5 编辑器工具规格（PK ET-001/002/003/006/008/009）

#### 3.5.1 AppFlowNavigatorEditor（CustomEditor + EditorWindow）

| 项目 | 决策 |
|------|------|
| 类型 | `[CustomEditor(typeof(AppFlowNavigator))]` Inspector 内嵌 + `MenuItem("Tools/AppFlow/Navigator")` 独立 EditorWindow 入口 |
| 渲染 | IMGUI（与项目其他 Editor 工具一致，轻量） |
| 显示内容 | 栈列表表格：Index / Node.DisplayName / Data?.ToString() / 进入时间戳 |
| 操作按钮 | Pop（弹出栈顶）/ PopAll（回根）/ Push 预设下拉（从项目 FlowNodeSO 资产列表动态获取） |
| 刷新机制 | 事件驱动：订阅 `EditorOnNavigated` + `EditorApplication.update` 仅 PlayMode 启用 `Repaint()` |
| 错误高亮 | `_isTransitioning == true` 持续 > 3s 时红色 HelpBox 警告 |
| 非 PlayMode | 显示 "请进入播放模式查看导航栈" |

#### 3.5.2 FlowNodeSOEditor（CustomEditor）

```csharp
// Assets/_Framework/Navigation/Editor/FlowNodeSOEditor.cs
[CustomEditor(typeof(FlowNodeSO))]
public class FlowNodeSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // _panelTypeName：下拉列表（数据源=项目中所有 public const string PanelKey）
        // fallback：手动输入模式（如果扫描不到 PanelKey 定义）
        DrawPanelTypeDropdown();

        // _requiredScene：标准 ObjectField + Build Settings 校验
        DrawSceneField();

        // 一致性校验 HelpBox
        ValidateConfiguration();

        serializedObject.ApplyModifiedProperties();
    }

    private void ValidateConfiguration()
    {
        var node = (FlowNodeSO)target;

        // 无意义配置
        if (node.RequiredScene == null && node.UnloadSceneOnExit)
            EditorGUILayout.HelpBox("UnloadSceneOnExit=true 但 RequiredScene 为空，此配置无意义。", MessageType.Warning);

        // Build Settings 校验
        if (node.RequiredScene != null)
        {
            // 检查场景是否在 Build Settings 中
            bool inBuildSettings = /* EditorBuildSettings.scenes 遍历 */;
            if (!inBuildSettings)
            {
                EditorGUILayout.HelpBox($"场景 '{node.RequiredScene.SceneName}' 不在 Build Settings 中！", MessageType.Error);
                if (GUILayout.Button("添加到 Build Settings"))
                    AddSceneToBuildSettings(node.RequiredScene);
            }
        }
    }
}
```

#### 3.5.3 FlowNodeSO.OnValidate（编辑期即时校验）

```csharp
// 在 FlowNodeSO.cs 中
#if UNITY_EDITOR
private void OnValidate()
{
    // 1. PanelTypeName 格式校验
    if (!string.IsNullOrEmpty(_panelTypeName) && _panelTypeName.Contains(' '))
        Debug.LogWarning($"[FlowNodeSO] '{name}': PanelTypeName 不应包含空格，请使用 PascalCase。");

    // 2. 无意义配置
    if (_requiredScene == null && _unloadSceneOnExit)
        Debug.LogWarning($"[FlowNodeSO] '{name}': UnloadSceneOnExit=true 但 RequiredScene 为空。");

    // 3. DisplayName 自动填充
    if (string.IsNullOrEmpty(_displayName))
        _displayName = name;
}
#endif
```

#### 3.5.4 面板注册验证工具 + 构建守护

```csharp
// Assets/_Framework/Navigation/Editor/AppFlowBuildValidator.cs

// === MenuItem 手动验证 ===
[MenuItem("Tools/AppFlow/Validate Panel Registration")]
private static void ValidatePanelRegistration()
{
    // 1. 收集所有 FlowNodeSO 的 _panelTypeName
    // 2. 正则扫描项目源文件中 RegisterPanelOpener("xxx" 调用
    // 3. 交叉对比 → 输出未匹配列表到 Console
}

// === 构建守护 ===
public class AppFlowBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 10;

    public void OnPreprocessBuild(BuildReport report)
    {
        var errors = new List<string>();

        // 1. 所有 FlowNodeSO._panelTypeName 有对应注册代码
        // 2. 所有 FlowNodeSO._requiredScene 在 Build Settings 中已启用
        // 3. GameStartupFlow 引用的 root 节点 SO 存在

        if (errors.Count > 0)
            throw new BuildFailedException($"[AppFlow] 构建验证失败：\n" + string.Join("\n", errors));

        Debug.Log("[AppFlow] 构建验证通过。");
    }
}
```

#### 3.5.5 Hierarchy Icon + Scene Gizmo

```csharp
// Assets/_Framework/Navigation/Editor/AppFlowHierarchyIcon.cs
[InitializeOnLoad]
static class AppFlowHierarchyIcon
{
    static AppFlowHierarchyIcon()
    {
        EditorApplication.hierarchyWindowItemOnGUI += DrawIcon;
    }

    private static void DrawIcon(int instanceID, Rect selectionRect)
    {
        // Navigator GO：绿色（idle）/ 黄色（transitioning）/ 红色（超时）
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;
        var nav = go.GetComponent<AppFlowNavigator>();
        if (nav == null) return;

        var color = nav.IsTransitioning ? Color.yellow : Color.green;
        var iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 16, 16);
        EditorGUI.DrawRect(iconRect, color);
    }
}

// IFlowHandler 实现者 Gizmo（在实现类中）
private void OnDrawGizmos()
{
    #if UNITY_EDITOR
    UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
        $"[FlowNode: {AppFlowNavigator.Instance?.CurrentNode?.DisplayName}]");
    #endif
}
```

---

### 4.1 与现有系统的关系

| 现有系统 | 整合方式 | 变更幅度 |
|---------|---------|---------|
| `UIManager` | AppFlowNavigator 通过注册表调用 `OpenPanelAsync<T>()`（零反射） | 零修改 |
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

### 4.2 FlowNode SO 资产清单

| SO 资产名 | RequiredScene | PanelTypeName（注册表 key） | CloseAllPanelsOnEnter | UnloadSceneOnExit |
|-----------|---------------|---------------|----------------------|-------------------|
| `Node_MainMenu` | SD_Main (Single) | `MainMenuPanel` | true | — |
| `Node_LevelSelect` | SD_Main (Single) | `LevelSelectScreen` | true | — |
| `Node_Battle` | SD_Battle (Single) | _(空，由 BattleController 自管)_ | true | false |

> **2026-05-06 重构**：MainMenu/LevelSelect 从纯 UI 节点改为关联 SD_Main（Single 模式），Battle 改为 `UnloadSceneOnExit=false`。场景切换由 Single 模式自动替换完成——Push Battle 时 Main 被替换，Pop 回 LevelSelect 时 Battle 被替换。

### 4.3 导航流程时序

#### 正常游戏流程（2026-05-06 双 Single 场景版本）

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
  → SceneLoader: SD_Main 已加载，短路跳过
  → CloseAllPanels() → OpenPanelAsync<LevelSelectScreen>()
  ↓
[选关确认] 
  Push(Node_Battle, { levelIndex = 2 })
  → Stack: [MainMenu, LevelSelect, Battle]
  → CloseAllPanels() → SceneLoader.LoadScene(SD_Battle, Single) → Main 被替换
  → BattleController 自行初始化 HUD
  ↓
[暂停 → 返回] 
  Pop()
  → Stack: [MainMenu, LevelSelect]
  → ExitNode: UnloadSceneOnExit=false → 不手动卸载
  → EnterNode: SceneLoader.LoadScene(SD_Main, Single) → Battle 被替换
  → Battle 场景 MonoBehaviour.OnDestroy → 清理 FairyGUI + DanmakuSystem.ClearAll
  → OpenPanelAsync<LevelSelectScreen>()
  ↓
[胜利 → 确认]
  Pop()  ← 同上效果
```

#### 热启动恢复（V1 Phase 4）

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

### 4.4 场景策略（2026-05-06 重构）

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
| Pop 时跳过 CloseAll + Resume | 返回即恢复语义 + 无闪烁 | 面板残留隐患 | UIManager.OpenPanelAsync 自带"已打开则 OnRefresh"逻辑兜底 + IFlowSuspendable 管理挂起/恢复（PK UA-007） |
| IFlowHandler 限 MonoBehaviour | SO 保持纯数据 | SO 子类化灵活性 | SO 共享实例 → 行为在 SO 上会互相覆盖（PK UA-008） |
| 不复用 StateMachine FSM | 导航器自带栈语义 | 复用已有代码 | FSM 的状态验证 + 转换表对导航场景是过度约束 |
| Singleton（MonoBehaviour） | 与 UIManager/SceneLoader 一致 | 可测试性（需 Mock） | 框架内部管理器统一用 Singleton（项目约定） |
| 同步 Push/Pop 入口 | Coroutine 场景零适配成本 | 丢失 await 异常传播 | try-catch + LogException 兜底（PK WX-010） |
| V1 不拆分 Navigator | 避免过度工程化 | 早期模块化 | 300 行硬性拆分规则兜底（PK UA-001） |

---

## 6. 风险与缓解

| # | 风险 | 影响 | 缓解 |
|---|------|------|------|
| R1 | FairyGUI 面板残留：Pop 回 Boot 时旧面板未关闭 | UI 叠加 | Push 时 `CloseAllPanelsOnEnter=true`；Pop 返回时 `OpenPanelAsync` 自带"已打开则 OnRefresh"逻辑 |
| R2 | DontDestroyOnLoad 生命周期：Navigator 必须在 Bootstrapper 之后 | NullRef | Bootstrapper `InitializeSystemsAsync` 末尾 touch `AppFlowNavigator.Instance` + `#if EDITOR` 断言（PK UA-010） |
| R3 | 热重载 (Editor)：Domain Reload 后栈丢失 | 编辑器状态异常 | `#if UNITY_EDITOR` SerializeField/SerializeReference 保留栈数据 + OnEnable 恢复 + LogWarning 诊断未标记 [Serializable] 的 Data（PK ET-005） |
| R4 | ~~反射裁剪~~ → **已消除**（v1.1 改用注册表模式，零反射） | — | — |
| R5 | Battle 场景 Additive 加载后 DanmakuSystem 初始化时序 | 系统未就绪 | EntitySystemBootstrap 已在 Battle 场景 Awake 中自初始化，不依赖场景加载模式 |
| R6 | 并发 Push/Pop（快速连点） | 栈状态不一致 | `_isTransitioning` 互斥锁 + 日志警告 + UI 层禁用按钮 |
| R7 | Transition 超时锁死 | 导航器永久不可用 | Coroutine 超时（10s）→ 强制重置 + LogError（PK WX-012 + UA-006：无 Update 开销） |
| R8 | 面板自注册时序：Navigator 尚未就绪时 [RIOM] 触发 | 注册失败 | `AfterSceneLoad` 时机确保所有 Singleton 已初始化；或用 lazy 注册队列 |
| R9 | 热启动恢复时 Data 类结构变更（字段增删改） | JSON 反序列化失败 | `JsonUtility` try-catch + 失败则丢弃该层级 → fallback 到上一有效节点 |
| R10 | 存储写入失败（微信 `wx.setStorageSync` 容量满/异常） | 下次热启动无法恢复 | try-catch + 静默降级（不影响当前游戏）+ 超过 4KB 告警日志 |

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
| 1.6 | `SceneLoader` 增加 `LoadSceneAsync(Task)` + SceneHandle 缓存 + `UnloadSceneAsync` 方法 | SceneLoader v1.1（对称 API，PK UA-004） |
| 1.7 | `SceneLoader._isLoading` Additive 场景不互斥 | 支持并发 Additive |
| 1.8 | 实现 `FlowNodeSOEditor.cs`（下拉 + 校验 + Build Settings 修复按钮） | SO Inspector DX（PK ET-002/008） |
| 1.9 | 实现 `AppFlowNavigatorEditor.cs`（PlayMode 栈可视化 + 快速操作按钮） | Editor 工具（PK UA-009/ET-001） |
| 1.10 | 实现 `AppFlowBuildValidator.cs`（MenuItem 验证 + IPreprocessBuildWithReport） | 构建守护（PK ET-003/009） |
| 1.11 | 实现 `AppFlowHierarchyIcon.cs`（Hierarchy 状态图标 + Gizmo 文字标签） | 视觉反馈（PK ET-006） |

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

| 步骤 | 内容 | 产出 | 状态 |
|------|------|------|------|
| 4.1 | `FlowNodeSO` 新增 `[SerializeField] string _nodeId`（唯一标识） | 节点可序列化寻址 | ✅ |
| 4.2 | `IFlowData` 扩展：推荐实现 `[Serializable]`，约束 Data 为纯 POCO | 数据可 JSON 序列化 | ✅ |
| 4.3 | 新增 `FlowStackSerializer` 静态工具类 | 序列化/反序列化核心 | ✅ |
| 4.4 | 序列化格式：`{ "version": 1, "entries": [...] }` | 紧凑 JSON，版本化 | ✅ |
| 4.5 | `FlowNodeRegistry`（SO 资产）→ 通过 nodeId 反查 SO 实例 | nodeId→SO 映射 | ✅ |
| 4.6 | `AppFlowNavigator.SaveStackToStorage()` — `OnNavigated` 后持久化 | 自动持久化 | ✅ |
| 4.7 | `AppFlowNavigator.TryRestoreStackAsync()` — 热启动恢复 | 热启动恢复入口 | ✅ |
| 4.8 | `GameStartupFlow` 先尝试 Restore，失败 fallback PushAsync(MainMenu) | 集成入口 | ✅ |
| 4.9 | 版本兼容处理：version 不匹配时丢弃旧栈 + 清除存储 | 安全降级 | ✅ |
| 4.10 | nodeId 找不到时丢弃该层级以上 + 从最后有效节点恢复 | 容错 | ✅ |

### 验收标准

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
| **⚠️ 300 行拆分规则** | 当 AppFlowNavigator.cs 超过 300 行时，该变更必须同步提取 `ISceneTransition` / `IPanelResolver` 策略接口（PK UA-001） | **硬性约束** |
| ~~栈序列化~~ | ~~微信热启动恢复~~ → **已移入 V1 Phase 4**（见 §7） | — |
| 转场动画 | Push/Pop 时可配置渐变/滑动动画（通过 ISceneTransition 策略注入） | 视觉打磨阶段 |
| 导航拦截器 | `INavigationGuard` — 离开前确认（如"是否放弃当前关卡"） | 需要时再加 |
| 深层链接 | 从微信分享链接直接进入指定关卡 → `PushAsync` 链式调用 | 社交分享阶段 |
| 导航路径可视化 | EditorWindow 节点图/列表展示所有 FlowNodeSO 的跳转关系（PK ET-010） | 当项目 FlowNodeSO 达到团队难以凭文档梳理的规模时（推荐 8+） |

---

_TDD v1.6 | 2026-05-06 | 广智 | 双 Single 场景切换重构（Boot→Main⇄Battle），§4.2/4.3/4.4/5.2 更新_
