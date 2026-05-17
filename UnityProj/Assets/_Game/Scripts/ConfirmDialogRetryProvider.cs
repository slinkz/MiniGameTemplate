using System;
using UnityEngine;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;

namespace Game
{
    /// <summary>
    /// Game-layer implementation of <see cref="IRetryDialogProvider"/>.
    /// Bridges the framework's NetworkRetryService to the FairyGUI-based ConfirmDialog.
    ///
    /// This class lives in Game.Runtime and can freely reference Common.ConfirmDialog
    /// (same assembly). The framework layer never knows about the concrete dialog.
    /// </summary>
    public class ConfirmDialogRetryProvider : IRetryDialogProvider
    {
        public async void ShowRetryDialog(string title, string content, string retryButtonText, Action onRetry)
        {
            try
            {
                var dialogData = new Common.ConfirmDialogData
                {
                    Title = title,
                    Content = content,
                    ConfirmText = retryButtonText,
                    ShowCancel = false,
                    OnConfirm = () =>
                    {
                        onRetry?.Invoke();
                    },
                    OnCancel = () =>
                    {
                        // Safety net: if dialog is force-closed (e.g. CloseAllPanels during scene transition),
                        // still invoke retry to avoid permanently stuck state.
                        GameLog.LogWarning("[ConfirmDialogRetryProvider] Dialog force-closed — invoking retry as safety fallback.");
                        onRetry?.Invoke();
                    }
                };

                await UIManager.Instance.OpenPanelAsync<Common.ConfirmDialog>(dialogData);
            }
            catch (Exception ex)
            {
                GameLog.LogWarning($"[ConfirmDialogRetryProvider] Failed to open dialog — invoking retry directly.");
                Debug.LogException(ex);   // Full stack trace for debugging
                onRetry?.Invoke();
            }
        }
    }
}
