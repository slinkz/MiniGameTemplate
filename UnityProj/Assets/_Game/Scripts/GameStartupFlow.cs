using System;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Events;
using MiniGameTemplate.Platform;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;

namespace Game.UI
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


        public async Task RunAsync(GameConfig gameConfig)
        {
            GameLog.Log($"[StartupFlow] Starting UI flow for {gameConfig.GameName} v{gameConfig.Version}...");

            WeChatBridgeFactory.SetAdUnitIds(_rewardedAdUnitId, _bannerAdUnitId, _interstitialAdUnitId);
            _weChatBridge = WeChatBridgeFactory.Create();
            _weChatBridge.PreloadRewardedAd();


            // --- Phase 1: Loading screen ---
            LoadingPanel loadingPanel;
            try
            {
                loadingPanel = await UIManager.Instance.OpenPanelAsync<LoadingPanel>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartupFlow] Failed to open LoadingPanel: {ex.Message}");
                // Cannot show UI — fall through to scene load without startup flow
                return;
            }

            loadingPanel.SetHintText("正在加载游戏资源...");
            loadingPanel.UpdateProgress(0f);

            // Simulate loading progress (real loading already happened in Bootstrapper's InitializeSystems)
            // We animate progress to give visual feedback and meet minimum display time.
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
                // User rejected privacy — show a confirm dialog asking them to reconsider
                loadingPanel.SetHintText("需要同意隐私协议才能继续...");
                privacyPassed = await RetryPrivacyAsync();

                if (!privacyPassed)
                {
                    // Still rejected — we can't proceed. Keep loading screen as dead-end.
                    Debug.LogWarning("[StartupFlow] User rejected privacy policy. Cannot continue.");
                    loadingPanel.SetHintText("请同意隐私协议后重新打开游戏");
                    loadingPanel.UpdateProgress(1f);
                    // Throw to prevent Bootstrapper from continuing to LoadInitialScene
                    throw new OperationCanceledException(
                        "[StartupFlow] Startup aborted: user rejected privacy policy.");
                }
            }

            // --- Phase 3: Complete loading and show main menu ---
            loadingPanel.SetHintText("加载完成！");
            loadingPanel.UpdateProgress(1f);

            // Ensure minimum loading time
            while (elapsed < _minLoadingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            // Fade out loading panel
            await loadingPanel.FadeOutAndCloseAsync(0.3f);

            // Open main menu
            try
            {
                var menuData = new MainMenuPanelData
                {
                    StartGameEvent = _startGameEvent,
                    WeChatBridge = _weChatBridge,
                    EnableBannerAd = _enableBannerAdInMainMenu
                };
                await UIManager.Instance.OpenPanelAsync<MainMenuPanel>(menuData);
                GameLog.Log("[StartupFlow] Main menu opened. Startup flow complete.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartupFlow] Failed to open MainMenuPanel: {ex.Message}");
            }
        }

        /// <summary>
        /// Check privacy authorization via the WeChat bridge.
        /// Returns true if no auth needed or user agreed.
        /// </summary>
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

            // Needs authorization — show the privacy dialog
            GameLog.Log("[StartupFlow] Privacy authorization required. Showing dialog...");
            bool agreed = await PrivacyDialog.ShowAndWaitAsync();
            GameLog.Log($"[StartupFlow] Privacy dialog result: {(agreed ? "agreed" : "rejected")}");
            if (!agreed)
                return false;

            return await RequestPrivacyAuthorizeAsync();
        }


        /// <summary>
        /// Retry privacy authorization — show a confirm dialog explaining why it's needed,
        /// then re-show the privacy dialog if user confirms.
        /// </summary>
        private async Task<bool> RetryPrivacyAsync()
        {
            // Show a confirm dialog asking the user to reconsider
            var confirmTcs = new TaskCompletionSource<bool>();

            var confirmData = new ConfirmDialogData
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
                await UIManager.Instance.OpenPanelAsync<ConfirmDialog>(confirmData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartupFlow] Failed to open ConfirmDialog: {ex.Message}");
                return false;
            }

            bool wantsRetry = await confirmTcs.Task;
            if (!wantsRetry)
                return false;

            // Re-show privacy dialog
            bool agreed = await PrivacyDialog.ShowAndWaitAsync();
            GameLog.Log($"[StartupFlow] Privacy retry result: {(agreed ? "agreed" : "rejected")}");
            if (!agreed)
                return false;

            return await RequestPrivacyAuthorizeAsync();
        }

    }
}
