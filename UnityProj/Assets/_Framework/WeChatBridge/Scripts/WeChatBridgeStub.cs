using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Stub implementation of IWeChatBridge for Editor and non-WeChat platforms.
    /// Logs all calls and returns simulated values.
    /// Ad callbacks are delayed to simulate real-world async behavior.
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
            Debug.Log("[WeChatBridge:Stub] PreloadRewardedAd — simulated preload.");
        }

        public void ShowRewardedAd(Action<bool> onComplete)
        {
            Debug.Log("[WeChatBridge:Stub] ShowRewardedAd — simulating 1.5s delay then success.");
            DelayedInvoke(1.5f, () => onComplete?.Invoke(true));
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

        // === Social ===

        public void Share(string title, string imageUrl, string query = "")
        {
            Debug.Log($"[WeChatBridge:Stub] Share — title: {title}, query: {query}");
        }

        public void SubmitScore(int score)
        {
            Debug.Log($"[WeChatBridge:Stub] SubmitScore: {score}");
        }

        public void ShowRankingPanel()
        {
            Debug.Log("[WeChatBridge:Stub] ShowRankingPanel");
        }

        public void RequestSubscribeMessage(string[] templateIds, Action<string[]> onComplete)
        {
            Debug.Log($"[WeChatBridge:Stub] RequestSubscribeMessage — {templateIds.Length} templates, simulating accept all.");
            DelayedInvoke(0.3f, () => onComplete?.Invoke(templateIds));
        }

        // === User ===

        public void Login(Action<bool, string> onComplete)
        {
            Debug.Log("[WeChatBridge:Stub] Login — simulating 0.5s delay then success.");
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
            Debug.Log("[WeChatBridge:Stub] OnShow callback registered.");
        }

        public void OnHide(Action callback)
        {
            _onHideCallback = callback;
            Debug.Log("[WeChatBridge:Stub] OnHide callback registered.");
        }

        public LaunchOptions GetLaunchOptions()
        {
            Debug.Log("[WeChatBridge:Stub] GetLaunchOptions — returning stub data.");
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
            Debug.Log($"[WeChatBridge:Stub] Vibrate (long={isLong})");
        }

        public void SetClipboardData(string text, Action<bool> onComplete = null)
        {
            Debug.Log($"[WeChatBridge:Stub] SetClipboardData: {text}");
            GUIUtility.systemCopyBuffer = text;
            onComplete?.Invoke(true);
        }

        public void GetClipboardData(Action<string> onComplete)
        {
            var text = GUIUtility.systemCopyBuffer;
            Debug.Log($"[WeChatBridge:Stub] GetClipboardData: {text}");
            onComplete?.Invoke(text);
        }

        // === Helper: Delayed callback to simulate async behavior ===

        /// <summary>
        /// Simulates async delay for callbacks. Uses a temporary GameObject with coroutine.
        /// In a real WeChat environment, these callbacks are inherently async.
        /// </summary>
        private static void DelayedInvoke(float seconds, Action action)
        {
            var go = new GameObject("[WeChatBridge:Stub] DelayHelper");
            go.hideFlags = HideFlags.HideAndDontSave;
            var runner = go.AddComponent<CoroutineRunner>();
            runner.StartCoroutine(DelayCoroutine(seconds, action, go));
        }

        private static IEnumerator DelayCoroutine(float seconds, Action action, GameObject go)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>
        /// Minimal MonoBehaviour for running coroutines in the Stub.
        /// </summary>
        private class CoroutineRunner : MonoBehaviour { }
    }
}
