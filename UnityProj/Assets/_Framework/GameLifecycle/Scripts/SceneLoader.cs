using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MiniGameTemplate.Utils;
using MiniGameTemplate.Events;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Scene loading manager. Loads scenes based on SceneDefinition SOs.
    /// Supports async loading with optional transition events.
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Events")]
        [SerializeField] private GameEvent _onSceneLoadStarted;
        [SerializeField] private GameEvent _onSceneLoadCompleted;

        private bool _isLoading;

        /// <summary>
        /// Load a scene defined by a SceneDefinition SO.
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
                UnityEngine.Debug.LogWarning("[SceneLoader] Already loading a scene. Ignoring request.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneDef));
        }

        private IEnumerator LoadSceneAsync(SceneDefinition sceneDef)
        {
            _isLoading = true;
            _onSceneLoadStarted?.Raise();

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
                // Progress is available via operation.progress (0..1)
                yield return null;
            }

            _isLoading = false;
            _onSceneLoadCompleted?.Raise();
            UnityEngine.Debug.Log($"[SceneLoader] Scene loaded: {sceneDef.SceneName}");
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
