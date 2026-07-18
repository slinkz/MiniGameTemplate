using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MiniGameTemplate.Utils;
using MiniGameTemplate.Events;
using MiniGameTemplate.Data;
using MiniGameTemplate.Asset;
using static MiniGameTemplate.Utils.GameLog;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Scene loading manager. Loads scenes based on SceneDefinition SOs.
    /// Routes through AssetService (YooAsset) when available for consistent
    /// asset pipeline. Falls back to SceneManager for editor/quick iteration.
    /// 
    /// v1.1: 新增 Task-based API (LoadSceneAsync/UnloadSceneAsync) + SceneHandle 缓存
    ///       + Additive 场景不阻塞 _isLoading 互斥锁（PK UA-004 / WX-002）
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Events")]
        [SerializeField] private GameEvent _onSceneLoadStarted;
        [SerializeField] private GameEvent _onSceneLoadCompleted;

        [Header("Progress (Optional)")]
        [Tooltip("Optional float event for loading progress [0..1]. Throttled to avoid per-frame overhead.")]
        [SerializeField] private FloatVariable _loadingProgress;

        /// <summary>Minimum interval between progress updates (seconds).</summary>
        private const float PROGRESS_THROTTLE = 0.1f;
        private const string TRANSITION_SCENE_NAME = "Transition";
        private const string TRANSITION_SCENE_PATH = "Assets/_Game/Scenes/Transition.unity";

        private bool _isLoading;

        // SceneHandle 缓存 — 用于 UnloadSceneAsync（PK WX-006）
        private readonly Dictionary<string, YooAsset.SceneHandle> _sceneHandleCache = new(4);

        // ================================================================
        //  Task-based API（AppFlowNavigator 调用）
        // ================================================================

        /// <summary>
        /// 异步加载场景（Task 版）。对称 API 供 AppFlowNavigator 使用。
        /// 若场景已加载则短路返回。Additive 场景不阻塞互斥锁。（PK UA-004）
        /// </summary>
        public Task LoadSceneAsync(SceneDefinition sceneDef)
        {
            if (sceneDef == null)
            {
                Debug.LogError("[SceneLoader] LoadSceneAsync: SceneDefinition is null!");
                return Task.CompletedTask;
            }

            // 短路：场景已加载
            var existingScene = SceneManager.GetSceneByName(sceneDef.SceneName);
            if (existingScene.isLoaded)
            {
                GameLog.Log($"[SceneLoader] Scene '{sceneDef.SceneName}' already loaded — skipping.");
                return Task.CompletedTask;
            }

            // Additive 场景不阻塞互斥锁（PK WX-006）
            if (!sceneDef.IsAdditive && _isLoading)
            {
                GameLog.LogWarning("[SceneLoader] Already loading a scene (Single mode). Ignoring request.");
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(LoadSceneCoroutineAsync(sceneDef, tcs));
            return tcs.Task;
        }

        /// <summary>
        /// 异步卸载场景（Task 版）。通过 sceneHandle.UnloadAsync() 或 SceneManager 释放。（PK WX-002/004）
        /// </summary>
        public Task UnloadSceneAsync(SceneDefinition sceneDef)
        {
            if (sceneDef == null) return Task.CompletedTask;

            var scene = SceneManager.GetSceneByName(sceneDef.SceneName);
            if (!scene.isLoaded)
            {
                GameLog.Log($"[SceneLoader] Scene '{sceneDef.SceneName}' not loaded — nothing to unload.");
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(UnloadSceneCoroutine(sceneDef, tcs));
            return tcs.Task;
        }

        private IEnumerator LoadSceneCoroutineAsync(SceneDefinition sceneDef, TaskCompletionSource<bool> tcs)
        {
            if (!sceneDef.IsAdditive)
                _isLoading = true;

            _onSceneLoadStarted?.Raise();

            var loadMode = sceneDef.IsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized
                && !string.IsNullOrEmpty(sceneDef.ScenePath))
            {
                var sceneHandle = AssetService.Instance.LoadSceneAsync(sceneDef.ScenePath, loadMode);
                yield return sceneHandle;

                if (sceneHandle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    _sceneHandleCache[sceneDef.SceneName] = sceneHandle;
                    GameLog.Log($"[SceneLoader] Scene loaded via AssetService: {sceneDef.SceneName}");
                }
                else
                {
                    Debug.LogError($"[SceneLoader] AssetService failed: {sceneDef.ScenePath}. Falling back.");
                    yield return LoadViaSceneManagerCoroutine(sceneDef, loadMode);
                }
            }
            else
            {
                yield return LoadViaSceneManagerCoroutine(sceneDef, loadMode);
            }

            if (!sceneDef.IsAdditive)
                ReleaseUnloadedSceneHandles(exceptSceneName: sceneDef.SceneName);

            if (!sceneDef.IsAdditive)
                _isLoading = false;

            _onSceneLoadCompleted?.Raise();
            tcs.TrySetResult(true);
        }

        private IEnumerator LoadViaSceneManagerCoroutine(SceneDefinition sceneDef, LoadSceneMode loadMode)
        {
            var operation = SceneManager.LoadSceneAsync(sceneDef.SceneName, loadMode);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneDef.SceneName}");
                yield break;
            }

            while (!operation.isDone)
            {
                if (_loadingProgress != null)
                    _loadingProgress.SetValue(operation.progress);
                yield return null;
            }

            if (_loadingProgress != null)
                _loadingProgress.SetValue(1f);

            GameLog.Log($"[SceneLoader] Scene loaded via SceneManager: {sceneDef.SceneName}");
        }

        private IEnumerator UnloadSceneCoroutine(SceneDefinition sceneDef, TaskCompletionSource<bool> tcs)
        {
            if (IsLastLoadedScene(sceneDef.SceneName))
            {
                yield return TransitionAwayFromLastSceneCoroutine(sceneDef.SceneName);
                tcs.TrySetResult(true);
                yield break;
            }

            if (_sceneHandleCache.TryGetValue(sceneDef.SceneName, out var handle))
            {
                _sceneHandleCache.Remove(sceneDef.SceneName);
                var unloadOp = handle.UnloadAsync();
                yield return unloadOp;
                GameLog.Log($"[SceneLoader] Scene unloaded via SceneHandle: {sceneDef.SceneName}");
            }
            else
            {
                var op = SceneManager.UnloadSceneAsync(sceneDef.SceneName);
                if (op != null)
                    yield return op;
                GameLog.Log($"[SceneLoader] Scene unloaded via SceneManager: {sceneDef.SceneName}");
            }

            tcs.TrySetResult(true);
        }

        // ================================================================
        //  Legacy Coroutine API（向后兼容）
        // ================================================================

        /// <summary>
        /// Load a scene defined by a SceneDefinition SO (legacy coroutine-based).
        /// </summary>
        public void LoadScene(SceneDefinition sceneDef)
        {
            if (sceneDef == null)
            {
                Debug.LogError("[SceneLoader] SceneDefinition is null!");
                return;
            }

            if (_isLoading)
            {
                GameLog.LogWarning("[SceneLoader] Already loading a scene. Ignoring request.");
                return;
            }

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized
                && !string.IsNullOrEmpty(sceneDef.ScenePath))
            {
                StartCoroutine(LoadSceneViaAssetServiceAsync(sceneDef));
            }
            else
            {
                StartCoroutine(LoadSceneViaSceneManagerAsync(sceneDef));
            }
        }

        private IEnumerator LoadSceneViaAssetServiceAsync(SceneDefinition sceneDef)
        {
            _isLoading = true;
            _onSceneLoadStarted?.Raise();

            var loadMode = sceneDef.IsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var sceneHandle = AssetService.Instance.LoadSceneAsync(sceneDef.ScenePath, loadMode);

            yield return sceneHandle;

            if (sceneHandle.Status == YooAsset.EOperationStatus.Succeed)
            {
                _sceneHandleCache[sceneDef.SceneName] = sceneHandle;
                GameLog.Log($"[SceneLoader] Scene loaded via AssetService: {sceneDef.SceneName}");
            }
            else
            {
                Debug.LogError($"[SceneLoader] AssetService failed to load scene: {sceneDef.ScenePath}. " +
                    $"Error: {sceneHandle.LastError}. Falling back to SceneManager.");
                yield return LoadSceneViaSceneManagerAsync(sceneDef);
                yield break;
            }

            if (!sceneDef.IsAdditive)
                ReleaseUnloadedSceneHandles(exceptSceneName: sceneDef.SceneName);

            _isLoading = false;
            _onSceneLoadCompleted?.Raise();
        }

        private IEnumerator LoadSceneViaSceneManagerAsync(SceneDefinition sceneDef)
        {
            _isLoading = true;
            if (_onSceneLoadStarted != null)
                _onSceneLoadStarted.Raise();

            var loadMode = sceneDef.IsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var operation = SceneManager.LoadSceneAsync(sceneDef.SceneName, loadMode);

            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneDef.SceneName}");
                _isLoading = false;
                yield break;
            }

            while (!operation.isDone)
            {
                if (_loadingProgress != null)
                {
                    _loadingProgress.SetValue(operation.progress);
                }
                yield return null;
            }

            if (_loadingProgress != null)
                _loadingProgress.SetValue(1f);

            _isLoading = false;
            if (_onSceneLoadCompleted != null)
                _onSceneLoadCompleted.Raise();
            GameLog.Log($"[SceneLoader] Scene loaded via SceneManager: {sceneDef.SceneName}");

            if (!sceneDef.IsAdditive)
                ReleaseUnloadedSceneHandles(exceptSceneName: sceneDef.SceneName);
        }

        /// <summary>
        /// Unload an additively-loaded scene (legacy fire-and-forget).
        /// </summary>
        public void UnloadScene(SceneDefinition sceneDef)
        {
            if (sceneDef == null) return;

            if (IsLastLoadedScene(sceneDef.SceneName))
            {
                StartCoroutine(TransitionAwayFromLastSceneCoroutine(sceneDef.SceneName));
                return;
            }

            if (_sceneHandleCache.TryGetValue(sceneDef.SceneName, out var handle))
            {
                _sceneHandleCache.Remove(sceneDef.SceneName);
                handle.UnloadAsync();
            }
            else
            {
                SceneManager.UnloadSceneAsync(sceneDef.SceneName);
            }
        }

        private IEnumerator TransitionAwayFromLastSceneCoroutine(string sceneName)
        {
            if (sceneName == TRANSITION_SCENE_NAME)
                yield break;

            yield return LoadTransitionSceneCoroutine();
            var transitionScene = SceneManager.GetSceneByName(TRANSITION_SCENE_NAME);
            if (!transitionScene.isLoaded)
            {
                Debug.LogError($"[SceneLoader] Transition scene failed to load. Keeping scene handle for: {sceneName}");
                yield break;
            }

            ReleaseSceneHandle(sceneName);
            GameLog.Log($"[SceneLoader] Transitioned through '{TRANSITION_SCENE_NAME}' before unloading last scene: {sceneName}");
        }

        private IEnumerator LoadTransitionSceneCoroutine()
        {
            var transitionScene = SceneManager.GetSceneByName(TRANSITION_SCENE_NAME);
            if (transitionScene.isLoaded)
                yield break;

            ReleaseSceneHandle(TRANSITION_SCENE_NAME);

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                var sceneHandle = AssetService.Instance.LoadSceneAsync(TRANSITION_SCENE_PATH, LoadSceneMode.Single);
                yield return sceneHandle;

                if (sceneHandle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    _sceneHandleCache[TRANSITION_SCENE_NAME] = sceneHandle;
                    yield break;
                }

                GameLog.Log($"[SceneLoader] AssetService failed to load transition scene: {sceneHandle.LastError}. Falling back to SceneManager.");
                if (sceneHandle.IsValid)
                    sceneHandle.Release();
            }

            var operation = SceneManager.LoadSceneAsync(TRANSITION_SCENE_NAME, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load transition scene: {TRANSITION_SCENE_NAME}");
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        private void ReleaseSceneHandle(string sceneName)
        {
            if (!_sceneHandleCache.TryGetValue(sceneName, out var handle))
                return;

            _sceneHandleCache.Remove(sceneName);
            if (handle.IsValid)
                handle.Release();
        }

        private bool IsLastLoadedScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
                return false;

            int loadedCount = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isLoaded)
                    loadedCount++;
            }

            return loadedCount <= 1;
        }

        private void ReleaseUnloadedSceneHandles(string exceptSceneName)
        {
            if (_sceneHandleCache.Count == 0)
                return;

            var staleSceneNames = new List<string>(2);
            foreach (var pair in _sceneHandleCache)
            {
                if (pair.Key == exceptSceneName)
                    continue;

                var scene = SceneManager.GetSceneByName(pair.Key);
                if (!scene.isLoaded)
                    staleSceneNames.Add(pair.Key);
            }

            for (int i = 0; i < staleSceneNames.Count; i++)
            {
                string staleSceneName = staleSceneNames[i];
                var handle = _sceneHandleCache[staleSceneName];
                _sceneHandleCache.Remove(staleSceneName);
                if (handle.IsValid)
                    handle.Release();

                GameLog.Log($"[SceneLoader] Released stale SceneHandle: {staleSceneName}");
            }
        }
    }
}
