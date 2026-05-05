using System;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Navigation;
using MiniGameTemplate.Platform;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using MiniGameTemplate.Events;

namespace Game
{
    /// <summary>
    /// Orchestrates the game's startup UI flow:
    ///   1. Show LoadingPanel with simulated progress
    ///   2. Check WeChat privacy authorization → show PrivacyDialog if needed
    ///   3. Fade out LoadingPanel → show MainMenuPanel
    ///
    /// Attach to the same GameObject as GameBootstrapper in the Boot scene.
    /// Assign to GameBootstrapper's "Startup Flow" field.
    /// </summary>
    public class GameStartupFlow : MonoBehaviour, IStartupFlow
    {
        [Header("Dependencies (SO)")]
        [Tooltip("Event raised when the user clicks 'Start Game' in the main menu.")]
        [SerializeField] private GameEvent _startGameEvent;

        [Header("Timing")]
        [Tooltip("Minimum duration (seconds) the loading screen is shown, even if everything loads instantly.")]
        [SerializeField] private float _minLoadingDuration = 1.5f;

        [Tooltip("Simulated progress speed per second (0..1 range).")]
        [SerializeField] private float _progressSpeed = 0.4f;

        [Header("Navigation")]
        [Tooltip("启动完成后首次 Push 的 FlowNode（通常是 MainMenu）")]
        [SerializeField] private FlowNodeSO _rootFlowNode;

        [Tooltip("FlowNode Registry（栈序列化恢复需要）")]
        [SerializeField] private FlowNodeRegistry _flowNodeRegistry;

        [Header("WeChat Ads (Optional)")]
        [Tooltip("Rewarded video ad unit id. Leave empty to fallback to stub behavior.")]
        [SerializeField] private string _rewardedAdUnitId = "";

        [Tooltip("Banner ad unit id. Leave empty to fallback to stub behavior.")]
        [SerializeField] private string _bannerAdUnitId = "";

        [Tooltip("Interstitial ad unit id. Leave empty to fallback to stub behavior.")]
        [SerializeField] private string _interstitialAdUnitId = "";

        [Tooltip("Whether main menu should display banner ads.")]
        [SerializeField] private bool _enableBannerAdInMainMenu = true;

        // Resolved at runtime
        private IWeChatBridge _weChatBridge;

        // 本次启动是否经历了隐私授权流程（首次启动 or 策略更新）
        // 如果是，则不恢复旧导航栈，走正常启动到 MainMenu
        private bool _didPrivacyAuthorization;


        public async Task RunAsync(GameConfig gameConfig)
        {
            GameLog.Log($"[StartupFlow] Starting UI flow for {gameConfig.GameName} v{gameConfig.Version}...");

            // Touch AppFlowNavigator — ensure Singleton exists before any panel self-registration
            _ = AppFlowNavigator.Instance;
            AppFlowNavigator.Instance.EnableStackPersistence = (_flowNodeRegistry != null);
            GameLog.Log("[StartupFlow] AppFlowNavigator initialized.");

            // Register all FairyGUI Binders before opening any panels
            UIManager.RegisterBinder("Common", Common.CommonBinder.BindAll);
            UIManager.RegisterBinder("MainMenu", MainMenu.MainMenuBinder.BindAll);
            UIManager.RegisterBinder("SG_LevelSelect", SG_LevelSelect.SG_LevelSelectBinder.BindAll);
            UIManager.RegisterBinder("SG_Battle", SG_Battle.SG_BattleBinder.BindAll);
            UIManager.RegisterBinder("SG_Popup", SG_Popup.SG_PopupBinder.BindAll);
            UIManager.RegisterBinder("SG_Loading", SG_Loading.SG_LoadingBinder.BindAll);

            WeChatBridgeFactory.SetAdUnitIds(_rewardedAdUnitId, _bannerAdUnitId, _interstitialAdUnitId);
            _weChatBridge = WeChatBridgeFactory.Create();
            _weChatBridge.PreloadRewardedAd();


            // --- Phase 1: Loading screen ---
            Common.LoadingPanel loadingPanel;
            try
            {
                loadingPanel = await UIManager.Instance.OpenPanelAsync<Common.LoadingPanel>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartupFlow] Failed to open LoadingPanel: {ex.Message}");
                return;
            }

            loadingPanel.SetHintText("正在加载游戏资源...");
            loadingPanel.UpdateProgress(0f);

            // Simulate loading progress
            float progress = 0f;
            float elapsed = 0f;

            while (progress < 0.9f || elapsed < _minLoadingDuration * 0.8f)
            {
                elapsed += Time.unscaledDeltaTime;
                progress = Mathf.Min(progress + _progressSpeed * Time.unscaledDeltaTime, 0.9f);
                loadingPanel.UpdateProgress(progress);
                await Task.Yield();
            }

            // --- Phase 2: Privacy check ---
            loadingPanel.SetHintText("正在检查隐私授权...");
            loadingPanel.UpdateProgress(0.92f);

            bool privacyPassed = await CheckPrivacyAsync();
            if (!privacyPassed)
            {
                loadingPanel.SetHintText("需要同意隐私协议才能继续...");
                privacyPassed = await RetryPrivacyAsync();

                if (!privacyPassed)
                {
                    Debug.LogWarning("[StartupFlow] User rejected privacy policy. Cannot continue.");
                    loadingPanel.SetHintText("请同意隐私协议后重新打开游戏");
                    loadingPanel.UpdateProgress(1f);
                    throw new OperationCanceledException(
                        "[StartupFlow] Startup aborted: user rejected privacy policy.");
                }
            }

            // --- Phase 3: Complete loading and show main menu ---
            loadingPanel.SetHintText("加载完成！");
            loadingPanel.UpdateProgress(1f);

            while (elapsed < _minLoadingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            // Fade out loading panel
            await loadingPanel.FadeOutAndCloseAsync(0.3f);

            // Initialize ShooterGame progress manager (idempotent)
            Game.ShooterGame.SG_Boot.InitProgress();

            // --- Phase 4: Try restore navigation stack (热启动恢复) ---
            // 如果本次启动走了隐私授权流程（首次启动/策略更新），不应恢复旧栈。
            // 理由：首次授权意味着游戏状态可能已过期，应从主菜单开始。
            if (_didPrivacyAuthorization)
            {
                ClearStoredStack();
                GameLog.Log("[StartupFlow] First-time privacy authorization — cleared stored stack, forcing normal startup.");
            }

            // If stored stack exists and is valid, restore it; otherwise push root node.
            bool restored = !_didPrivacyAuthorization && await TryRestoreNavigationStackAsync();
            if (!restored)
            {
                // Normal startup: Push root flow node (MainMenu)
                if (_rootFlowNode != null)
                {
                    var menuData = new MainMenu.MainMenuPanelData
                    {
                        StartGameEvent = _startGameEvent,
                        WeChatBridge = _weChatBridge,
                        EnableBannerAd = _enableBannerAdInMainMenu
                    };
                    await AppFlowNavigator.Instance.PushAsync(_rootFlowNode, menuData);
                    GameLog.Log("[StartupFlow] Root node pushed via AppFlowNavigator. Startup flow complete.");
                }
                else
                {
                    // Fallback: 兼容旧模式（无 FlowNode 配置时直接打开面板）
                    var menuData = new MainMenu.MainMenuPanelData
                    {
                        StartGameEvent = _startGameEvent,
                        WeChatBridge = _weChatBridge,
                        EnableBannerAd = _enableBannerAdInMainMenu
                    };
                    await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>(menuData);
                    GameLog.Log("[StartupFlow] Main menu opened (legacy). Startup flow complete.");
                }
            }
        }

        /// <summary>
        /// Phase 4: 尝试从存储恢复导航栈（微信热启动恢复）。
        /// 成功返回 true，失败返回 false（走正常启动）。
        /// </summary>
        private async Task<bool> TryRestoreNavigationStackAsync()
        {
            if (_flowNodeRegistry == null) return false;

            string json = null;
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // 微信小游戏: wx.getStorageSync
                json = WeChatWASM.WX.StorageGetStringSync("appflow_stack", "");
#else
                // Editor/Standalone: PlayerPrefs
                json = UnityEngine.PlayerPrefs.GetString("appflow_stack", "");
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[StartupFlow] Failed to read stored stack: {ex.Message}");
                return false;
            }

            if (string.IsNullOrEmpty(json)) return false;

            var entries = FlowStackSerializer.DeserializeStack(json, _flowNodeRegistry);
            if (entries == null || entries.Count == 0)
            {
                GameLog.Log("[StartupFlow] Stored stack invalid or empty — fallback to normal startup.");
                ClearStoredStack();
                return false;
            }

            GameLog.Log($"[StartupFlow] Restoring navigation stack ({entries.Count} entries)...");

            // 恢复栈：将中间层静默压入，只对栈顶执行完整 EnterNode
            var navigator = AppFlowNavigator.Instance;
            for (int i = 0; i < entries.Count - 1; i++)
            {
                navigator.PushSilent(entries[i].Node, entries[i].Data);
            }

            // 栈顶节点完整进入
            var top = entries[^1];
            await navigator.PushAsync(top.Node, top.Data);

            GameLog.Log("[StartupFlow] Navigation stack restored successfully.");
            return true;
        }

        private void ClearStoredStack()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WeChatWASM.WX.StorageDeleteKeySync("appflow_stack");
#else
                UnityEngine.PlayerPrefs.DeleteKey("appflow_stack");
#endif
            }
            catch { /* ignore */ }
        }

        private async Task<bool> CheckPrivacyAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            _weChatBridge.CheckPrivacyAuthorize(needAuth =>
            {
                tcs.TrySetResult(needAuth);
            });

            bool needsAuth = await tcs.Task;
            if (!needsAuth)
            {
                GameLog.Log("[StartupFlow] Privacy already authorized.");
                return true;
            }

            GameLog.Log("[StartupFlow] Privacy authorization required. Showing dialog...");
            _didPrivacyAuthorization = true; // 标记：本次走了授权流程，不应恢复旧栈
            bool agreed = await Common.PrivacyDialog.ShowAndWaitAsync();
            GameLog.Log($"[StartupFlow] Privacy dialog result: {(agreed ? "agreed" : "rejected")}");
            if (!agreed)
                return false;

            return await RequestPrivacyAuthorizeAsync();
        }

        private async Task<bool> RequestPrivacyAuthorizeAsync()
        {
            if (_weChatBridge == null)
            {
                Debug.LogError("[StartupFlow] WeChat bridge is null when requesting privacy authorization.");
                return false;
            }

            var tcs = new TaskCompletionSource<bool>();
            _weChatBridge.RequirePrivacyAuthorize(granted =>
            {
                tcs.TrySetResult(granted);
            });

            bool grantedResult = await tcs.Task;
            GameLog.Log($"[StartupFlow] RequirePrivacyAuthorize result: {(grantedResult ? "granted" : "rejected")}");
            return grantedResult;
        }

        private async Task<bool> RetryPrivacyAsync()
        {
            var confirmTcs = new TaskCompletionSource<bool>();

            var confirmData = new Common.ConfirmDialogData
            {
                Title = "需要授权",
                Content = "为了正常使用游戏功能，需要您同意隐私保护协议。是否重新查看？",
                ConfirmText = "重新查看",
                CancelText = "退出",
                ShowCancel = true,
                OnConfirm = () => confirmTcs.TrySetResult(true),
                OnCancel = () => confirmTcs.TrySetResult(false)
            };

            try
            {
                await UIManager.Instance.OpenPanelAsync<Common.ConfirmDialog>(confirmData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartupFlow] Failed to open ConfirmDialog: {ex.Message}");
                return false;
            }

            bool wantsRetry = await confirmTcs.Task;
            if (!wantsRetry)
                return false;

            bool agreed = await Common.PrivacyDialog.ShowAndWaitAsync();
            GameLog.Log($"[StartupFlow] Privacy retry result: {(agreed ? "agreed" : "rejected")}");
            if (!agreed)
                return false;

            return await RequestPrivacyAuthorizeAsync();
        }
    }
}
