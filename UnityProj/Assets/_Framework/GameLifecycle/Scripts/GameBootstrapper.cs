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

        private async void Awake()
        {
            // Ensure only one instance survives
            DontDestroyOnLoad(gameObject);

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

            // 2. Config tables (Luban)
            ConfigManager.Initialize();
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
