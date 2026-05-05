using System;
using System.Collections;
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
    /// 生命周期：Singleton&lt;T&gt;（MonoBehaviour, DontDestroyOnLoad）
    /// 初始化时机：GameBootstrapper 系统初始化完成后、IStartupFlow 执行前
    /// 
    /// 线程安全：无锁（WebGL 单线程 + Unity API 限主线程）
    /// </summary>
    public class AppFlowNavigator : Singleton<AppFlowNavigator>
    {
        // ---------- 栈 ----------
        private readonly List<StackEntry> _stack = new(8);

        [Serializable]
        public struct StackEntry
        {
            public FlowNodeSO Node;
            [SerializeReference] public IFlowData Data;
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

        /// <summary>
        /// 是否启用栈序列化（Phase 4）。
        /// 由外部（GameStartupFlow）在初始化时设置。默认 false。
        /// </summary>
        public bool EnableStackPersistence { get; set; }

        // ---------- 属性 ----------
        public FlowNodeSO CurrentNode => _stack.Count > 0 ? _stack[^1].Node : null;
        public int StackDepth => _stack.Count;
        public bool IsTransitioning => _isTransitioning;
        /// <summary>当前栈的只读视图（Editor 可视化 + 序列化用）。</summary>
        public IReadOnlyList<StackEntry> Stack => _stack;


        // ================================================================
        //  Public Navigation API
        // ================================================================

        /// <summary>
        /// 压入新节点（前进）。
        /// </summary>
        public async Task PushAsync(FlowNodeSO node, IFlowData data = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (_isTransitioning)
            {
                Debug.LogWarning($"[AppFlow] PushAsync({node.DisplayName}) ignored — transition in progress.");
                return;
            }

            _isTransitioning = true;
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
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
                EditorOnNavigated?.Invoke(previous, node);
#endif
            }
            finally
            {
                _isTransitioning = false;
                StopTimeoutCoroutine();
                SaveStackToStorage();
            }
        }

        /// <summary>
        /// 弹出栈顶（返回上一级）。
        /// </summary>
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
                SaveStackToStorage();
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
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
            try
            {
                while (_stack.Count - 1 > targetIndex)
                {
                    var leaving = _stack[^1];
                    _stack.RemoveAt(_stack.Count - 1);
                    await ExitNodeAsync(leaving.Node);
                }

                var returning = _stack[^1];
                await EnterNodeAsync(returning.Node, returnData ?? returning.Data, isReturning: true);
                OnNavigated?.Invoke(null, returning.Node);
#if UNITY_EDITOR
                EditorOnNavigated?.Invoke(null, returning.Node);
#endif
            }
            finally
            {
                _isTransitioning = false;
                StopTimeoutCoroutine();
                SaveStackToStorage();
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
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
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
#if UNITY_EDITOR
                EditorOnNavigated?.Invoke(previous, node);
#endif
            }
            finally
            {
                _isTransitioning = false;
                StopTimeoutCoroutine();
                SaveStackToStorage();
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
            _timeoutCoroutine = StartCoroutine(TransitionTimeoutCoroutine());
            try
            {
                while (_stack.Count > 1)
                {
                    var leaving = _stack[^1];
                    _stack.RemoveAt(_stack.Count - 1);
                    await ExitNodeAsync(leaving.Node);
                }

                var root = _stack[0];
                await EnterNodeAsync(root.Node, root.Data, isReturning: true);
                OnNavigated?.Invoke(null, root.Node);
#if UNITY_EDITOR
                EditorOnNavigated?.Invoke(null, root.Node);
#endif
            }
            finally
            {
                _isTransitioning = false;
                StopTimeoutCoroutine();
                SaveStackToStorage();
            }
        }

        // ================================================================
        //  Sync API — Coroutine 场景桥接（PK WX-010）
        // ================================================================

        /// <summary>同步 Push（fire-and-forget + 异常日志）。供 Coroutine 场景使用。</summary>
        public void Push(FlowNodeSO node, IFlowData data = null)
        {
            _ = PushAsyncSafe(node, data);
        }

        /// <summary>同步 Pop（fire-and-forget + 异常日志）。供 Coroutine 场景使用。</summary>
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
        private Coroutine _timeoutCoroutine;

        private IEnumerator TransitionTimeoutCoroutine()
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
        /// Editor-only 静态事件。Editor 工具可无侵入挂载调试逻辑。
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
            // 1. 调用钩子
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

        // ================================================================
        //  栈序列化（Phase 4）— 供 FlowStackSerializer 调用
        // ================================================================

        /// <summary>清空当前栈并替换为给定条目（热启动恢复用）。</summary>
        internal void RestoreStack(List<StackEntry> entries)
        {
            _stack.Clear();
            _stack.AddRange(entries);
        }

        /// <summary>静默压入节点，不触发 EnterNodeAsync（热启动恢复中间层用）。</summary>
        public void PushSilent(FlowNodeSO node, IFlowData data)
        {
            _stack.Add(new StackEntry { Node = node, Data = data });
        }

        /// <summary>
        /// 保存当前栈到存储（每次导航完成后自动调用）。
        /// 微信小游戏用 wx.setStorageSync，Editor 用 PlayerPrefs。
        /// </summary>
        private void SaveStackToStorage()
        {
            if (!EnableStackPersistence) return;
            if (_stack.Count == 0) return;

            try
            {
                var json = FlowStackSerializer.SerializeStack(Stack);
#if UNITY_WEBGL && !UNITY_EDITOR
                WeChatWASM.WX.StorageSetStringSync("appflow_stack", json);
#else
                UnityEngine.PlayerPrefs.SetString("appflow_stack", json);
                UnityEngine.PlayerPrefs.Save();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AppFlow] SaveStackToStorage failed: {ex.Message}");
            }
        }
    }
}
