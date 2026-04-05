namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Factory that returns the appropriate IWeChatBridge implementation
    /// based on the current platform.
    ///
    /// To integrate the real WeChat SDK:
    /// 1. Create a class implementing IWeChatBridge with real SDK calls
    /// 2. Add a platform check here to return that implementation
    /// </summary>
    public static class WeChatBridgeFactory
    {
        private static IWeChatBridge _instance;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        public static IWeChatBridge Create()
        {
            if (_instance != null)
                return _instance;

            // TODO: When integrating real WeChat SDK, add platform detection:
            // #if UNITY_WEBGL && !UNITY_EDITOR
            //     _instance = new WeChatBridgeImpl(); // Real implementation
            // #else
            //     _instance = new WeChatBridgeStub();
            // #endif

            _instance = new WeChatBridgeStub();
            return _instance;
        }

        /// <summary>
        /// Override the bridge instance (useful for testing).
        /// </summary>
        public static void SetOverride(IWeChatBridge bridge)
        {
            _instance = bridge;
        }
    }
}
