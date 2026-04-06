using System;
using UnityEngine;
using MiniGameTemplate.Asset;
using MiniGameTemplate.Data;
using MiniGameTemplate.UI;
using MiniGameTemplate.Audio;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Pool;
using MiniGameTemplate.Utils;

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

        /// <summary>Marks whether this particular instance is the primary (first) bootstrapper.</summary>
        private bool _isPrimaryInstance;

        /// <summary>
        /// Shared SaveSystem instance — used to flush on pause/quit.
        /// Game code can reference this to avoid creating duplicate instances.
        /// </summary>
        public static ISaveSystem SaveSystem { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hasBooted = false;
            SaveSystem = null;
        }

        private async void Awake()
        {
            // Guard: prevent duplicate Bootstrapper instances
            if (_hasBooted)
            {
                GameLog.LogWarning("[Bootstrapper] Duplicate detected — destroying this instance.");
                Destroy(gameObject);
                return;
            }
            _hasBooted = true;
            _isPrimaryInstance = true;

            DontDestroyOnLoad(gameObject);

            try
            {
                // Apply game settings
                Application.targetFrameRate = _gameConfig.TargetFrameRate;
                Application.runInBackground = _gameConfig.RunInBackground;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;

                GameLog.Log($"[Bootstrapper] Starting {_gameConfig.GameName} v{_gameConfig.Version}");

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
            // Only reset boot flag if this is the primary instance being destroyed
            // (e.g., domain reload in editor). Duplicate instances must not reset the flag.
            if (_isPrimaryInstance)
                _hasBooted = false;
        }

        /// <summary>
        /// SEC: Flush save data when the app is paused (minimized, switched to background).
        /// Critical for WeChat Mini Games — the OS may kill the process at any time after pause.
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveSystem?.FlushIfDirty();
        }

        /// <summary>
        /// SEC: Flush save data before the app quits.
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveSystem?.FlushIfDirty();
        }

        private async System.Threading.Tasks.Task InitializeSystemsAsync()
        {
            // 0. Save System — initialize early so other systems can use it
            SaveSystem = new PlayerPrefsSaveSystem();

            // 1. Asset System (YooAsset) — must be first, other systems depend on it
            if (_assetConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[Bootstrapper] FATAL: AssetConfig is not assigned on GameBootstrapper! " +
                    "Open the Boot scene, select the GameBootstrapper GameObject, and assign a " +
                    "DefaultAssetConfig asset to the 'Asset Configuration' field.");
            }
            await AssetService.Instance.InitializeAsync(_assetConfig);
            GameLog.Log("[Bootstrapper] AssetService initialized.");

            // 2. Config tables (Luban) — async to avoid WebGL deadlock
            await ConfigManager.InitializeAsync();
            GameLog.Log("[Bootstrapper] ConfigManager initialized.");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Verify config tables loaded correctly (stripped from release builds)
            if (ConfigManager.Tables?.TbGlobalConst != null)
            {
                var helloWorld = ConfigManager.Tables.TbGlobalConst.Get("HelloWorld");
                if (helloWorld != null)
                {
                    GameLog.Log($"[Bootstrapper] GlobalConst verification: key={helloWorld.Key}, " +
                        $"stringValue={helloWorld.StringValue}, intValue={helloWorld.IntValue}");
                }
            }
#endif

            // 3. Timer (needed by others)
            _ = TimerService.Instance;
            GameLog.Log("[Bootstrapper] TimerService initialized.");

            // 4. Audio
            // AudioManager auto-initializes via Singleton if present in scene/prefab
            // If not in scene, it will be created on first access
            GameLog.Log("[Bootstrapper] AudioManager ready.");

            // 5. UI (FairyGUI)
            _ = UIManager.Instance;
            GameLog.Log("[Bootstrapper] UIManager initialized.");

            // 6. Object Pool
            _ = PoolManager.Instance;
            GameLog.Log("[Bootstrapper] PoolManager initialized.");

            GameLog.Log("[Bootstrapper] All systems initialized.");
        }

        private void LoadInitialScene()
        {
            if (_gameConfig.InitialScene == null)
            {
                GameLog.LogWarning("[Bootstrapper] No initial scene configured in GameConfig!");
                return;
            }

            // Skip loading if we're already in the target scene (e.g. Boot → Boot)
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name == _gameConfig.InitialScene.SceneName)
            {
                GameLog.Log($"[Bootstrapper] Already in target scene '{currentScene.name}' — skipping load.");
                return;
            }

            SceneLoader.Instance.LoadScene(_gameConfig.InitialScene);
        }
    }
}
