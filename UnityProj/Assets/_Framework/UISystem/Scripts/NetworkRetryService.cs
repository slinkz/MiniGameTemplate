using System;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Generic "cloud operation failed → blocking retry dialog" service.
    ///
    /// Design: any cloud-bound operation that fails after exhausting its own retries
    /// should call <see cref="ShowBlockingRetry"/> to present a modal dialog.
    /// The dialog has a single "Retry" button (no cancel, no close-on-click-outside).
    /// The player's only options are: tap Retry (resets the operation's retry counter),
    /// or kill the process (abandons the failed write; next launch reads cloud state).
    ///
    /// LoadingMask coordination:
    /// If <see cref="LoadingMaskService"/> is showing when the retry dialog opens,
    /// this service automatically hides the mask (so the dialog is interactable) and
    /// restores it when the player taps Retry. This ensures the retry dialog is never
    /// obscured by the loading mask regardless of caller-side timing.
    ///
    /// Architecture: this class lives in the framework layer and depends only on
    /// <see cref="IRetryDialogProvider"/> (also framework-level). The concrete dialog
    /// implementation (e.g. FairyGUI ConfirmDialog) is injected from the game layer
    /// via <see cref="SetProvider"/>.
    ///
    /// Usage (game startup):
    /// <code>
    ///   // 1. Inject provider (once, at startup after binders are registered)
    ///   NetworkRetryService.SetProvider(new ConfirmDialogRetryProvider());
    ///
    ///   // 2. Wire cloud events
    ///   cloudSave.SyncService.OnUploadFailedNeedRetry += (retryAction) =>
    ///       NetworkRetryService.ShowBlockingRetry(retryAction);
    /// </code>
    ///
    /// Usage (any future cloud operation):
    /// <code>
    ///   NetworkRetryService.ShowBlockingRetry(myRetryAction, "自定义标题", "自定义内容");
    /// </code>
    /// </summary>
    public static class NetworkRetryService
    {
        private const string DEFAULT_TITLE = "网络连接失败";
        private const string DEFAULT_CONTENT = "无法连接到服务器，请检查网络连接后重试。";
        private const string DEFAULT_RETRY_TEXT = "重试";

        private static bool _isShowing;
        private static IRetryDialogProvider _provider;

        /// <summary>
        /// Inject the dialog provider. Must be called before any ShowBlockingRetry calls.
        /// Typically called once during game startup after FairyGUI binders are registered.
        /// </summary>
        public static void SetProvider(IRetryDialogProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            GameLog.Log("[NetworkRetry] Provider set: " + provider.GetType().Name);
        }

        /// <summary>
        /// Show a blocking modal retry dialog. The player can only tap "Retry" or kill the process.
        /// Safe to call multiple times — duplicate calls while dialog is showing are ignored.
        /// </summary>
        /// <param name="retryAction">Callback invoked when the player taps "Retry".</param>
        /// <param name="title">Dialog title (default: "网络连接失败").</param>
        /// <param name="content">Dialog body text (default: generic network error message).</param>
        public static void ShowBlockingRetry(
            Action retryAction,
            string title = null,
            string content = null)
        {
            if (_isShowing)
            {
                GameLog.LogWarning("[NetworkRetry] Retry dialog already showing — ignoring duplicate request.");
                return;
            }

            if (_provider == null)
            {
                GameLog.LogWarning("[NetworkRetry] No provider set — invoking retry directly as fallback.");
                retryAction?.Invoke();
                return;
            }

            _isShowing = true;

            // Coordinate with LoadingMask: if it's currently showing, hide it so the
            // retry dialog is fully visible and interactable. Restore it on retry.
            bool wasMaskShowing = LoadingMaskService.IsShowing;
            if (wasMaskShowing)
            {
                LoadingMaskService.Hide();
                GameLog.Log("[NetworkRetry] LoadingMask was showing — hidden to reveal retry dialog.");
            }

            try
            {
                _provider.ShowRetryDialog(
                    title ?? DEFAULT_TITLE,
                    content ?? DEFAULT_CONTENT,
                    DEFAULT_RETRY_TEXT,
                    onRetry: () =>
                    {
                        _isShowing = false;

                        // Restore loading mask before retry action re-triggers the async operation
                        if (wasMaskShowing)
                        {
                            LoadingMaskService.Show();
                            GameLog.Log("[NetworkRetry] LoadingMask restored before retry.");
                        }

                        GameLog.Log("[NetworkRetry] Player tapped Retry.");
                        retryAction?.Invoke();
                    });
            }
            catch (Exception ex)
            {
                _isShowing = false;
                // Restore mask on exception path too
                if (wasMaskShowing) LoadingMaskService.Show();
                GameLog.LogWarning($"[NetworkRetry] Failed to open retry dialog: {ex.Message}. Invoking retry directly.");
                retryAction?.Invoke();
            }
        }

        /// <summary>
        /// Reset internal state. Called on domain reload (editor) or app restart.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isShowing = false;
            _provider = null;
        }
    }
}
