using System;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Abstraction for showing a blocking retry dialog.
    /// Framework-level code (<see cref="NetworkRetryService"/>) depends only on this interface;
    /// the concrete implementation (e.g. using FairyGUI ConfirmDialog) lives in the game layer
    /// and is injected at startup.
    ///
    /// Contract:
    /// - The dialog MUST be modal (no dismiss by tapping outside).
    /// - The dialog MUST have exactly one "Retry" button — no cancel/close option.
    /// - When the player taps Retry, invoke <paramref name="onRetry"/> of <see cref="ShowRetryDialog"/>.
    /// - If the dialog fails to open for any reason, invoke <paramref name="onRetry"/> as fallback
    ///   to avoid permanently stuck state.
    /// </summary>
    public interface IRetryDialogProvider
    {
        /// <summary>
        /// Show a blocking modal retry dialog.
        /// </summary>
        /// <param name="title">Dialog title text.</param>
        /// <param name="content">Dialog body / description text.</param>
        /// <param name="retryButtonText">Text for the retry button.</param>
        /// <param name="onRetry">
        /// Callback to invoke when the player taps Retry.
        /// Must also be invoked if the dialog is force-closed (safety fallback).
        /// </param>
        void ShowRetryDialog(string title, string content, string retryButtonText, Action onRetry);
    }
}
