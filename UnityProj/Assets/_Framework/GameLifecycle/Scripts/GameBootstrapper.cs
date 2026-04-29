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
    /// Boot scene should contain this script (and optionally an IStartupFlow
    /// MonoBehaviour) on a single "Boot" GameObject.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("Asset Configuration")]
        [SerializeField] private AssetConfig _assetConfig;

        [Header("Startup Flow (Optional)")]
        [Tooltip("Assign a MonoBehaviour implementing IStartupFlow to run game-specific startup " +
                 "(loading UI, privacy check, main menu). If null, goes directly to LoadInitialScene.")]
        [SerializeField] private MonoBehaviour _startupFlowBehaviour;

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

                // Run game-specific startup flow (loading UI, privacy check, etc.) if assigned
                if (_startupFlowBehaviour != null)
                {
                    var startupFlow = _startupFlowBehaviour as IStartupFlow;
                    if (startupFlow != null)
                    {
                        await startupFlow.RunAsync(_gameConfig);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(
                            $"[Bootstrapper] Startup flow '{_startupFlowBehaviour.GetType().Name}' " +
                            "does not implement IStartupFlow! Skipping startup flow.");
                    }
                }

                // Load the initial scene
                LoadInitialScene();
            }
            catch (OperationCanceledException cancelEx)
            {
                // Startup flow was intentionally cancelled (e.g. user rejected privacy policy).
                // This is NOT a fatal error — the game stays on the loading screen.
                GameLog.LogWarning($"[Bootstrapper] Startup cancelled: {cancelEx.Message}");
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
            // Set default font BEFORE UIManager init — WebGL has no OS fonts,
            // so without this all Chinese text renders as blank.
            await InitializeFairyGUIFontAsync();

            _ = UIManager.Instance;
            GameLog.Log("[Bootstrapper] UIManager initialized.");

            // NOTE: 不再强制 Stage.touchScreen = true。
            // 之前强制设为 true 导致"按钮点两次才响应"——因为微信 touch 回调
            // 存在一帧延迟，首次点击时 HandleTouchEvents() 读到 touchCount==0
            // → 空循环 → 点击丢失。而 HandleMouseEvents() 本可立即响应
            // (weapp-adapter 同时派发 mouse DOM 事件 → emscripten → Input.GetMouseButtonDown)
            // 但被 touchScreen=true 封死了。
            //
            // 现在依赖 FairyGUI 原生自动检测：
            //   启动时 touchScreen=false → mouse 模式 → 首次点击立即响应
            //   真机首次触摸 → Input.touchCount>0 → 自动切换到 touch 模式
            // 两端都能正常工作，且不存在首次点击丢失。
            GameLog.Log("[Bootstrapper] FairyGUI touchScreen auto-detect (no forced override).");

            // 6. Object Pool
            _ = PoolManager.Instance;
            GameLog.Log("[Bootstrapper] PoolManager initialized.");

            GameLog.Log("[Bootstrapper] All systems initialized.");
        }

        /// <summary>
        /// Timeout in seconds for WX.GetWXFont callback.
        /// If the JS layer fails silently (no callback), we must not block startup forever.
        /// </summary>
        private const float WXFontTimeoutSeconds = 5f;

        /// <summary>
        /// Initialize FairyGUI default font.
        /// WebGL/WeChat: use WX.GetWXFont() to load the WeChat system font (supports CJK).
        /// Editor/Standalone: use OS system font names.
        /// Must be called BEFORE UIManager.Instance or any FairyGUI panel creation.
        /// </summary>
        private async System.Threading.Tasks.Task InitializeFairyGUIFontAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WeChat Mini Game: load the WeChat system font asynchronously.
            //
            // Flow: SDK tries getCommonFont (system font API) first.
            //   - On real device: getCommonFont succeeds → done.
            //   - On DevTools: getCommonFont may fail → SDK falls back to HTTP-downloading
            //     the font from fallbackUrl.
            //
            // KNOWN ISSUES:
            //   - fallbackUrl="" (empty string) causes "URL不合法" → must provide a real URL.
            //   - When BOTH getCommonFont AND fallbackUrl fail, JS catch block does NOT fire
            //     the C# callback → tcs.Task hangs forever → need timeout guard.
            //
            // Solution: provide a subset CJK font in StreamingAssets as fallback + timeout.
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            // Fallback font: a 565KB subset of SimHei containing ~2100 CJK chars.
            // Lives at StreamingAssets/fonts/fallback-cjk.ttf → accessible via CDN URL.
            string fallbackFontUrl = UnityEngine.Application.streamingAssetsPath + "/fonts/fallback-cjk.ttf";
            GameLog.Log($"[Bootstrapper] Loading WeChat system font... (fallback: {fallbackFontUrl})");
            try
            {
                WeChatWASM.WX.GetWXFont(fallbackFontUrl, font =>
                {
                    if (font != null)
                    {
                        var fguiFont = new FairyGUI.DynamicFont();
                        fguiFont.name = "WXFont";
                        fguiFont.nativeFont = font;
                        FairyGUI.FontManager.RegisterFont(fguiFont);

                        FairyGUI.UIConfig.defaultFont = "WXFont";
                        GameLog.Log("[Bootstrapper] WeChat font loaded and set as FairyGUI default.");
                    }
                    else
                    {
                        GameLog.LogWarning("[Bootstrapper] WX.GetWXFont returned null! " +
                            "Chinese text may not display. Falling back to Arial.");
                        FairyGUI.UIConfig.defaultFont = "Arial";
                    }
                    tcs.TrySetResult(true);
                });
            }
            catch (System.Exception ex)
            {
                GameLog.LogWarning($"[Bootstrapper] WX.GetWXFont threw: {ex.Message}. Falling back to Arial.");
                FairyGUI.UIConfig.defaultFont = "Arial";
                tcs.TrySetResult(true);
            }

            // Timeout guard: if JS never fires the callback, don't block startup.
            // Use a coroutine-style polling loop since WebGL is single-threaded
            // (Task.Delay / CancellationTokenSource won't work reliably in WASM).
            float elapsed = 0f;
            while (!tcs.Task.IsCompleted && elapsed < WXFontTimeoutSeconds)
            {
                await System.Threading.Tasks.Task.Yield();
                elapsed += UnityEngine.Time.unscaledDeltaTime;
            }

            if (!tcs.Task.IsCompleted)
            {
                GameLog.LogWarning($"[Bootstrapper] WX.GetWXFont timed out after {WXFontTimeoutSeconds}s. " +
                    "Falling back to Arial.");
                FairyGUI.UIConfig.defaultFont = "Arial";
                tcs.TrySetResult(true);
            }
#else
            // Editor & Standalone: use OS system font
            FairyGUI.UIConfig.defaultFont = "Microsoft YaHei, SimHei";
            GameLog.Log("[Bootstrapper] FairyGUI default font set to system fonts.");
            await System.Threading.Tasks.Task.CompletedTask;
#endif
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
