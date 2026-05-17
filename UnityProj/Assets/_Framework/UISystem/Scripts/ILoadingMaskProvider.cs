using System;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Abstraction for showing a full-screen modal loading mask.
    /// Framework-level code (<see cref="LoadingMaskService"/>, <see cref="ServerRequestGate"/>)
    /// depends only on this interface; the concrete implementation (e.g. using FairyGUI)
    /// lives in the game layer and is injected at startup.
    ///
    /// Contract:
    /// - The mask MUST block all input (modal, no click-through).
    /// - The mask MUST display a spinner / loading indicator + configurable text.
    /// - Show/Hide are idempotent — calling Show while already shown just updates the message.
    /// - Calling Hide while already hidden is a no-op.
    /// </summary>
    public interface ILoadingMaskProvider
    {
        /// <summary>
        /// Show the full-screen loading mask with a message.
        /// If already showing, update the message text.
        /// </summary>
        /// <param name="message">Text to display (e.g. "正在与服务器通讯...").</param>
        void Show(string message);

        /// <summary>
        /// Update the displayed message text without changing visibility.
        /// No-op if the mask is not currently shown.
        /// </summary>
        void UpdateMessage(string message);

        /// <summary>
        /// Hide and dismiss the loading mask.
        /// No-op if already hidden.
        /// </summary>
        void Hide();
    }
}
