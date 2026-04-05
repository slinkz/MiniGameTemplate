using System;
using UnityEngine;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Stub implementation of IWeChatBridge for Editor and non-WeChat platforms.
    /// Logs all calls and returns simulated success values.
    /// </summary>
    public class WeChatBridgeStub : IWeChatBridge
    {
        public bool IsWeChatPlatform => false;

        public void ShowRewardedAd(Action<bool> onComplete)
        {
            Debug.Log("[WeChatBridge:Stub] ShowRewardedAd — simulating success.");
            onComplete?.Invoke(true);
        }

        public void ShowBannerAd()
        {
            Debug.Log("[WeChatBridge:Stub] ShowBannerAd");
        }

        public void HideBannerAd()
        {
            Debug.Log("[WeChatBridge:Stub] HideBannerAd");
        }

        public void ShowInterstitialAd()
        {
            Debug.Log("[WeChatBridge:Stub] ShowInterstitialAd");
        }

        public void Share(string title, string imageUrl, string query = "")
        {
            Debug.Log($"[WeChatBridge:Stub] Share — title: {title}");
        }

        public void SubmitScore(int score)
        {
            Debug.Log($"[WeChatBridge:Stub] SubmitScore: {score}");
        }

        public void ShowRankingPanel()
        {
            Debug.Log("[WeChatBridge:Stub] ShowRankingPanel");
        }

        public void Login(Action<bool, string> onComplete)
        {
            Debug.Log("[WeChatBridge:Stub] Login — simulating success.");
            onComplete?.Invoke(true, "stub_auth_code_12345");
        }

        public WeChatUserInfo GetUserInfo()
        {
            return new WeChatUserInfo
            {
                Nickname = "TestPlayer",
                AvatarUrl = "",
                OpenId = "stub_open_id"
            };
        }

        public void Vibrate(bool isLong = false)
        {
            Debug.Log($"[WeChatBridge:Stub] Vibrate (long={isLong})");
        }
    }
}
