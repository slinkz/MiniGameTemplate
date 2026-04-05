using System.Collections;
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

        private bool _isLoading;

        /// <summary>
        /// Load a scene defined by a SceneDefinition SO.
        /// Uses AssetService (YooAsset) when initialized, falls back to SceneManager.
        /// </summary>
        public void LoadScene(SceneDefinition sceneDef)
        {
            if (sceneDef == null)
            {
                UnityEngine.Debug.LogError("[SceneLoader] SceneDefinition is null!");
                return;
            }

            if (_isLoading)
            {
                GameLog.LogWarning("[SceneLoader] Already loading a scene. Ignoring request.");
                return;
            }

            // Prefer AssetService (YooAsset) for scene loading when available
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

        /// <summary>
        /// Load scene through YooAsset — ensures all assets go through the same pipeline.
        /// </summary>
        private IEnumerator LoadSceneViaAssetServiceAsync(SceneDefinition sceneDef)
        {
            _isLoading = true;
            _onSceneLoadStarted?.Raise();

            var loadMode = sceneDef.IsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var sceneHandle = AssetService.Instance.LoadSceneAsync(sceneDef.ScenePath, loadMode);

            yield return sceneHandle;

            if (sceneHandle.Status == YooAsset.EOperationStatus.Succeed)
            {
                GameLog.Log($"[SceneLoader] Scene loaded via AssetService: {sceneDef.SceneName}");
            }
            else
            {
                UnityEngine.Debug.LogError($"[SceneLoader] AssetService failed to load scene: {sceneDef.ScenePath}. " +
                    $"Error: {sceneHandle.LastError}. Falling back to SceneManager.");
                // Fallback to SceneManager
                yield return LoadSceneViaSceneManagerAsync(sceneDef);
                yield break;
            }

            _isLoading = false;
            _onSceneLoadCompleted?.Raise();
        }

        /// <summary>
        /// Fallback: load scene via Unity SceneManager (for editor or when AssetService is unavailable).
        /// </summary>
        private IEnumerator LoadSceneViaSceneManagerAsync(SceneDefinition sceneDef)
        {
            _isLoading = true;
            if (_onSceneLoadStarted != null)
                _onSceneLoadStarted.Raise();

            var loadMode = sceneDef.IsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var operation = SceneManager.LoadSceneAsync(sceneDef.SceneName, loadMode);

            if (operation == null)
            {
                UnityEngine.Debug.LogError($"[SceneLoader] Failed to load scene: {sceneDef.SceneName}");
                _isLoading = false;
                yield break;
            }

            while (!operation.isDone)
            {
                // Throttled progress update — avoids per-frame SO event overhead
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
        }

        /// <summary>
        /// Unload an additively-loaded scene.
        /// </summary>
        public void UnloadScene(SceneDefinition sceneDef)
        {
            if (sceneDef == null) return;
            SceneManager.UnloadSceneAsync(sceneDef.SceneName);
        }
    }
}
