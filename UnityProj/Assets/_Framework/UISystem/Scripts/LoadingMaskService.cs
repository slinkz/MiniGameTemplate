using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Static service for showing/hiding a full-screen modal loading mask.
    ///
    /// Architecture: follows the same Provider-injection pattern as <see cref="NetworkRetryService"/>.
    /// The concrete UI implementation is injected from the game layer via <see cref="SetProvider"/>.
    ///
    /// Usage (game startup):
    /// <code>
    ///   LoadingMaskService.SetProvider(new FairyGUILoadingMaskProvider());
    /// </code>
    ///
    /// Usage (anywhere in framework or game code):
    /// <code>
    ///   LoadingMaskService.Show("正在与服务器通讯...");
    ///   // ... await some async work ...
    ///   LoadingMaskService.Hide();
    /// </code>
    /// </summary>
    public static class LoadingMaskService
    {
        private const string DEFAULT_MESSAGE = "正在加载...";

        private static ILoadingMaskProvider _provider;
        private static bool _isShowing;

        /// <summary>
        /// Inject the loading mask provider. Must be called once at startup.
        /// </summary>
        public static void SetProvider(ILoadingMaskProvider provider)
        {
            _provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
            GameLog.Log("[LoadingMask] Provider set: " + provider.GetType().Name);
        }

        /// <summary>
        /// Show the loading mask. Blocks all player input until <see cref="Hide"/> is called.
        /// If already showing, just updates the message.
        /// </summary>
        public static void Show(string message = null)
        {
            if (_provider == null)
            {
                GameLog.LogWarning("[LoadingMask] No provider set — Show() ignored.");
                return;
            }

            _isShowing = true;
            _provider.Show(message ?? DEFAULT_MESSAGE);
        }

        /// <summary>
        /// Update the displayed message without changing visibility.
        /// </summary>
        public static void UpdateMessage(string message)
        {
            if (!_isShowing || _provider == null) return;
            _provider.UpdateMessage(message);
        }

        /// <summary>
        /// Hide the loading mask and restore player input.
        /// </summary>
        public static void Hide()
        {
            if (_provider == null) return;

            _isShowing = false;
            _provider.Hide();
        }

        /// <summary>Whether the loading mask is currently displayed.</summary>
        public static bool IsShowing => _isShowing;

        /// <summary>Reset on domain reload (editor) or app restart.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _provider = null;
            _isShowing = false;
        }
    }
}
