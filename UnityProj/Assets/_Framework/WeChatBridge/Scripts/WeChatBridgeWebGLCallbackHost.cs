using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Receives callbacks from WebGL jslib via Unity SendMessage.
    /// </summary>
    internal sealed class WeChatBridgeWebGLCallbackHost : Singleton<WeChatBridgeWebGLCallbackHost>
    {
        private static WeChatBridgeWebGL _bridge;

        public static string BridgeGameObjectName => Instance != null ? Instance.gameObject.name : string.Empty;

        public static void Bind(WeChatBridgeWebGL bridge)
        {
            _bridge = bridge;
        }

        // Invoked by WeChatBridge.jslib
        public void OnRewardedAdClosed(string isEnded)
        {
            _bridge?.HandleRewardedAdClosed(isEnded == "1");
        }

        // Invoked by WeChatBridge.jslib
        public void OnRewardedAdError(string error)
        {
            _bridge?.HandleRewardedAdError(error);
        }

        // Invoked by WeChatBridge.jslib
        public void OnPrivacyCheckResult(string needAuth)
        {
            _bridge?.HandlePrivacyCheckResult(needAuth);
        }

        // Invoked by WeChatBridge.jslib
        public void OnPrivacyRequireResult(string accepted)
        {
            _bridge?.HandlePrivacyRequireResult(accepted);
        }

        // Invoked by WeChatBridge.jslib (V2 — SG_TDD_06)
        public void OnCloudFunctionResult(string jsonResult)
        {
            _bridge?.HandleCloudFunctionResult(jsonResult);
        }
    }
}
