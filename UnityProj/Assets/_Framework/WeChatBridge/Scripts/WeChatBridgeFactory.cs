namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Factory that returns the appropriate IWeChatBridge implementation
    /// based on current platform and build target.
    /// </summary>
    public static class WeChatBridgeFactory
    {
        private static IWeChatBridge _instance;

        private static string _rewardedAdUnitId = string.Empty;
        private static string _bannerAdUnitId = string.Empty;
        private static string _interstitialAdUnitId = string.Empty;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _rewardedAdUnitId = string.Empty;
            _bannerAdUnitId = string.Empty;
            _interstitialAdUnitId = string.Empty;
        }

        public static IWeChatBridge Create()
        {
            if (_instance != null)
                return _instance;

#if UNITY_WEBGL && !UNITY_EDITOR
            _instance = new WeChatBridgeWebGL();
#else
            _instance = new WeChatBridgeStub();
#endif
            ApplyAdConfig(_instance);
            return _instance;
        }

        /// <summary>
        /// Configure ad unit IDs for the runtime bridge.
        /// Can be called before or after Create().
        /// </summary>
        public static void SetAdUnitIds(string rewardedAdUnitId, string bannerAdUnitId, string interstitialAdUnitId)
        {
            _rewardedAdUnitId = (rewardedAdUnitId ?? string.Empty).Trim();
            _bannerAdUnitId = (bannerAdUnitId ?? string.Empty).Trim();
            _interstitialAdUnitId = (interstitialAdUnitId ?? string.Empty).Trim();

            if (_instance != null)
                ApplyAdConfig(_instance);
        }

        /// <summary>
        /// Override the bridge instance (useful for testing).
        /// SEC: Only available in Editor and Development builds to prevent runtime injection attacks.
        /// </summary>
        public static void SetOverride(IWeChatBridge bridge)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _instance = bridge;
            ApplyAdConfig(_instance);
#else
            UnityEngine.Debug.LogError("[WeChatBridgeFactory] SEC: SetOverride is disabled in release builds.");
#endif
        }

        private static void ApplyAdConfig(IWeChatBridge bridge)
        {
            if (bridge is IWeChatAdConfigurable configurable)
            {
                configurable.ConfigureAds(_rewardedAdUnitId, _bannerAdUnitId, _interstitialAdUnitId);
            }
        }
    }
}
