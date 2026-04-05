using System;
using UnityEngine;
using MiniGameTemplate.Asset;
using MiniGameTemplate.Data;
using MiniGameTemplate.UI;
using MiniGameTemplate.Audio;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Pool;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Game entry point. This MonoBehaviour lives in the Boot scene and
    /// initializes all framework systems in the correct order.
    ///
    /// Boot scene should contain ONLY this script on a single GameObject.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("Asset Configuration")]
        [SerializeField] private AssetConfig _assetConfig;

        private static bool _hasBooted;

        private async void Awake()
        {
            // Guard: prevent duplicate Bootstrapper instances
            if (_hasBooted)
            {
                UnityEngine.Debug.LogWarning("[Bootstrapper] Duplicate detected — destroying this instance.");
                Destroy(gameObject);
                return;
            }
            _hasBooted = true;

            DontDestroyOnLoad(gameObject);

            try
            {
                // Apply game settings
                Application.targetFrameRate = _gameConfig.TargetFrameRate;
                Application.runInBackground = _gameConfig.RunInBackground;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;

                UnityEngine.Debug.Log($"[Bootstrapper] Starting {_gameConfig.GameName} v{_gameConfig.Version}");

                // Initialize systems in dependency order
                await InitializeSystemsAsync();

                // Load the initial scene
                LoadInitialScene();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                UnityEngine.Debug.LogError("[Bootstrapper] FATAL: Initialization failed. See exception above.");
                // TODO: Show a user-facing fatal error UI here
            }
        }

        private void OnDestroy()
        {
            // Reset boot flag when the Bootstrapper is destroyed (e.g., domain reload in editor)
            if (_hasBooted && this != null)
                _hasBooted = false;
        }

        private async System.Threading.Tasks.Task InitializeSystemsAsync()
        {
            // 1. Asset System (YooAsset) — must be first, other systems may need it
            if (_assetConfig != null)
            {
                await AssetService.Instance.InitializeAsync(_assetConfig);
                UnityEngine.Debug.Log("[Bootstrapper] AssetService initialized.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Bootstrapper] No AssetConfig assigned — AssetService skipped. " +
                    "Resources.Load fallback will be used.");
            }

            // 2. Config tables (Luban) — async to avoid WebGL deadlock
            await ConfigManager.InitializeAsync();
            UnityEngine.Debug.Log("[Bootstrapper] ConfigManager initialized.");

            // 3. Timer (needed by others)
            _ = TimerService.Instance;
            UnityEngine.Debug.Log("[Bootstrapper] TimerService initialized.");

            // 4. Audio
            // AudioManager auto-initializes via Singleton if present in scene/prefab
            // If not in scene, it will be created on first access
            UnityEngine.Debug.Log("[Bootstrapper] AudioManager ready.");

            // 5. UI (FairyGUI)
            _ = UIManager.Instance;
            UnityEngine.Debug.Log("[Bootstrapper] UIManager initialized.");

            // 6. Object Pool
            _ = PoolManager.Instance;
            UnityEngine.Debug.Log("[Bootstrapper] PoolManager initialized.");

            UnityEngine.Debug.Log("[Bootstrapper] All systems initialized.");
        }

        private void LoadInitialScene()
        {
            if (_gameConfig.InitialScene != null)
            {
                SceneLoader.Instance.LoadScene(_gameConfig.InitialScene);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Bootstrapper] No initial scene configured in GameConfig!");
            }
        }
    }
}
