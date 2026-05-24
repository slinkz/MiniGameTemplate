using System;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Data;
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

        [Header("WeChat Platform")]
        [Tooltip("WeChat platform configuration (single source of truth).")]
        [SerializeField] private WeChatConfig _weChatConfig;

        // Resolved at runtime
        private IWeChatBridge _weChatBridge;


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

            _weChatBridge = GameBootstrapper.WeChatBridge ?? WeChatBridgeFactory.CreateWithConfig(_weChatConfig);
            _weChatBridge.PreloadRewardedAd();

            // Inject framework service providers — abstractions that let framework-level code
            // show UI without depending on game-layer FairyGUI implementations.
            NetworkRetryService.SetProvider(new ConfirmDialogRetryProvider());
            LoadingMaskService.SetProvider(new FairyGUILoadingMaskProvider());

            // Register cloud upload retry handler — uses NetworkRetryService (generic mechanism).
            // Any cloud operation that fails after its own MAX_RETRY will show a blocking retry dialog.
            RegisterCloudRetryHandler();


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

            // --- Phase 3: Complete loading and wait for cloud data ---
            loadingPanel.SetHintText("正在获取游戏数据...");
            loadingPanel.UpdateProgress(0.95f);

            // V4: MUST have cloud data before proceeding. Block here until IsCloudReady.
            // If pull fails and user retries via NetworkRetryService, we keep waiting.
            if (GameBootstrapper.SaveSystem is CloudSaveSystem cloudSave2)
            {
                while (!cloudSave2.IsCloudReady)
                {
                    await Task.Yield();
                }
                GameLog.Log("[StartupFlow] Cloud data ready — proceeding to main menu.");
            }

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

            // --- Phase 4: Clear stale navigation stack (冷启动清栈) ---
            // RunAsync 被调用 = 进程重启 = 冷启动，一律清空旧栈走正常启动。
            // 热启动恢复功能暂未启用（需 wx.onShow 回调配合内存标记判断）。
            bool restored = await TryRestoreNavigationStackAsync();
            if (!restored)
            {
                // Normal startup: Push root flow node (MainMenu)
                if (_rootFlowNode != null)
                {
                    var menuData = new MainMenu.MainMenuPanelData
                    {
                        StartGameEvent = _startGameEvent,
                        WeChatBridge = _weChatBridge,
                        EnableBannerAd = _weChatConfig != null && _weChatConfig.EnableBannerAdInMainMenu
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
                        EnableBannerAd = _weChatConfig != null && _weChatConfig.EnableBannerAdInMainMenu
                    };
                    await UIManager.Instance.OpenPanelAsync<MainMenu.MainMenuPanel>(menuData);
                    GameLog.Log("[StartupFlow] Main menu opened (legacy). Startup flow complete.");
                }
            }
        }

        /// <summary>
        /// Phase 4: 尝试从存储恢复导航栈。
        /// 
        /// 设计决策（2026-05-17 修改）：
        /// 走完整 Boot → Awake → RunAsync 流程 = 冷启动（包括微信开发者工具终止+刷新）。
        /// 冷启动一律清空 appflow_stack，走正常主界面流程。
        /// 
        /// 热启动恢复功能暂时禁用。未来如需支持微信 wx.onShow 热启动恢复，
        /// 应通过 jslib 注册 wx.onShow 回调设置内存标记，仅在标记为热启动时才恢复栈。
        /// 当前阶段每次 RunAsync 被调用都意味着进程重启，不存在热启动语义。
        /// </summary>
        private Task<bool> TryRestoreNavigationStackAsync()
        {
            // 冷启动：清空持久化的导航栈，走正常启动
            ClearStoredStack();
            GameLog.Log("[StartupFlow] Cold boot — cleared stored stack, using normal startup.");
            return Task.FromResult(false);
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

        /// <summary>
        /// Wire CloudSyncService.OnUploadFailedNeedRetry → NetworkRetryService.
        /// Also wires CloudSaveSystem.OnStartupPullFailedNeedRetry for startup blocking retry.
        /// Safe to call even if SaveSystem is not CloudSaveSystem (simply does nothing).
        /// This is the single bridge point — any future cloud operations can follow
        /// the same pattern: fail after retries → fire event → NetworkRetryService handles UI.
        /// </summary>
        private void RegisterCloudRetryHandler()
        {
            if (GameBootstrapper.SaveSystem is CloudSaveSystem cloudSave)
            {
                // Upload failure (during gameplay) → blocking retry dialog
                cloudSave.SyncService.OnUploadFailedNeedRetry += (retryAction) =>
                {
                    NetworkRetryService.ShowBlockingRetry(retryAction);
                };

                // Startup pull failure → blocking retry dialog (game cannot proceed without cloud data)
                cloudSave.OnStartupPullFailedNeedRetry += (retryAction) =>
                {
                    NetworkRetryService.ShowBlockingRetry(
                        retryAction,
                        "网络连接失败",
                        "无法获取游戏数据，请检查网络连接后重试。");
                };

                GameLog.Log("[StartupFlow] Cloud retry handlers registered (upload + startup pull).");
            }
        }
    }
}
