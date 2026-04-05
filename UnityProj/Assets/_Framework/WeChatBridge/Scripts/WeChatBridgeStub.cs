using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Stub implementation of IWeChatBridge for Editor and non-WeChat platforms.
    /// Logs all calls and returns simulated values.
    /// Ad callbacks are delayed to simulate real-world async behavior.
    ///
    /// SEC: All diagnostic logging uses GameLog (conditional-compiled) so that
    /// zero log output is emitted in release builds.
    /// </summary>
    public class WeChatBridgeStub : IWeChatBridge
    {
        public bool IsWeChatPlatform => false;

        // Lifecycle callbacks
        private Action<Dictionary<string, string>> _onShowCallback;
        private Action _onHideCallback;

        // === Ads ===

        public void PreloadRewardedAd()
        {
            GameLog.Log("[WeChatBridge:Stub] PreloadRewardedAd — simulated preload.");
        }

        public void ShowRewardedAd(Action<bool> onComplete)
        {
            GameLog.Log("[WeChatBridge:Stub] ShowRewardedAd — simulating 1.5s delay then success.");
            DelayedInvoke(1.5f, () => onComplete?.Invoke(true));
        }

        public void ShowBannerAd()
        {
            GameLog.Log("[WeChatBridge:Stub] ShowBannerAd");
        }

        public void HideBannerAd()
        {
            GameLog.Log("[WeChatBridge:Stub] HideBannerAd");
        }

        public void ShowInterstitialAd()
        {
            GameLog.Log("[WeChatBridge:Stub] ShowInterstitialAd");
        }

        // === Social ===

        public void Share(string title, string imageUrl, string query = "")
        {
            GameLog.Log($"[WeChatBridge:Stub] Share — title: {title}, query: {query}");
        }

        public void SubmitScore(int score)
        {
            GameLog.Log($"[WeChatBridge:Stub] SubmitScore: {score}");
        }

        public void ShowRankingPanel()
        {
            GameLog.Log("[WeChatBridge:Stub] ShowRankingPanel");
        }

        public void RequestSubscribeMessage(string[] templateIds, Action<string[]> onComplete)
        {
            GameLog.Log($"[WeChatBridge:Stub] RequestSubscribeMessage — {templateIds.Length} templates, simulating accept all.");
            DelayedInvoke(0.3f, () => onComplete?.Invoke(templateIds));
        }

        // === User ===

        public void Login(Action<bool, string> onComplete)
        {
            // SEC: Do NOT log auth codes (even stub ones) — prevents copy-paste into real implementations.
            // IMPORTANT: The real IWeChatBridge implementation must NEVER log the auth code.
            // The auth code should be sent directly to your backend server for code2session exchange.
            GameLog.Log("[WeChatBridge:Stub] Login — simulating 0.5s delay then success.");
            DelayedInvoke(0.5f, () => onComplete?.Invoke(true, "stub_auth_code_12345"));
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

        // === Lifecycle ===

        public void OnShow(Action<Dictionary<string, string>> callback)
        {
            _onShowCallback = callback;
            GameLog.Log("[WeChatBridge:Stub] OnShow callback registered.");
        }

        public void OnHide(Action callback)
        {
            _onHideCallback = callback;
            GameLog.Log("[WeChatBridge:Stub] OnHide callback registered.");
        }

        public LaunchOptions GetLaunchOptions()
        {
            GameLog.Log("[WeChatBridge:Stub] GetLaunchOptions — returning stub data.");
            return new LaunchOptions
            {
                Scene = 1001,
                Query = new Dictionary<string, string> { { "from", "stub" } },
                ReferrerAppId = ""
            };
        }

        // === System ===

        public void Vibrate(bool isLong = false)
        {
            GameLog.Log($"[WeChatBridge:Stub] Vibrate (long={isLong})");
        }

        public void SetClipboardData(string text, Action<bool> onComplete = null)
        {
            // SEC: Never log clipboard content — may contain passwords, tokens, or PII.
            GameLog.Log("[WeChatBridge:Stub] SetClipboardData called.");
            GUIUtility.systemCopyBuffer = text;
            onComplete?.Invoke(true);
        }

        public void GetClipboardData(Action<string> onComplete)
        {
            var text = GUIUtility.systemCopyBuffer;
            // SEC: Never log clipboard content — may contain passwords, tokens, or PII.
            GameLog.Log("[WeChatBridge:Stub] GetClipboardData called.");
            onComplete?.Invoke(text);
        }

        // === Helper: Delayed callback to simulate async behavior ===

        /// <summary>
        /// Simulates async delay for callbacks using the framework's CoroutineRunner.
        /// In a real WeChat environment, these callbacks are inherently async.
        /// </summary>
        private static void DelayedInvoke(float seconds, Action action)
        {
            CoroutineRunner.Run(DelayCoroutine(seconds, action));
        }

        private static IEnumerator DelayCoroutine(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }
}
