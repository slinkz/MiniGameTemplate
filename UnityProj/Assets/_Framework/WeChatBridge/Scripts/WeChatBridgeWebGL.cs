using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Utils;


namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// WebGL WeChat bridge implementation.
    ///
    /// Scope in this template version:
    /// - Ads (Rewarded / Banner / Interstitial): real JS bridge implementation
    /// - Other capabilities: delegated to WeChatBridgeStub as safe fallback
    ///
    /// This keeps gameplay code stable while allowing projects to complete ad integration quickly.
    /// </summary>
    public sealed class WeChatBridgeWebGL : IWeChatBridge, IWeChatAdConfigurable
    {
        private readonly WeChatBridgeStub _fallback = new WeChatBridgeStub();

        private string _rewardedAdUnitId = string.Empty;
        private string _bannerAdUnitId = string.Empty;
        private string _interstitialAdUnitId = string.Empty;

        private const float RewardedAdTimeoutSeconds = 12f;

        private bool _nativeInitialized;
        private bool _isRewardedPending;
        private Action<bool> _rewardedCallback;
        private TimerHandle _rewardedTimeoutTimer = TimerHandle.Invalid;


        public WeChatBridgeWebGL()
        {
            WeChatBridgeWebGLCallbackHost.Bind(this);
            TryInitializeNativeBridge();
        }

        public bool IsWeChatPlatform
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (!TryInitializeNativeBridge())
                    return false;

                return WXBridge_IsWeChatEnv() == 1;
#else
                return false;
#endif
            }
        }

        public void ConfigureAds(string rewardedAdUnitId, string bannerAdUnitId, string interstitialAdUnitId)
        {
            _rewardedAdUnitId = (rewardedAdUnitId ?? string.Empty).Trim();
            _bannerAdUnitId = (bannerAdUnitId ?? string.Empty).Trim();
            _interstitialAdUnitId = (interstitialAdUnitId ?? string.Empty).Trim();

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!TryInitializeNativeBridge())
                return;

            WXBridge_SetAdUnitIds(_rewardedAdUnitId, _bannerAdUnitId, _interstitialAdUnitId);
#endif
        }

        public void PreloadRewardedAd()
        {
            if (!CanUseNativeAds(_rewardedAdUnitId))
            {
                _fallback.PreloadRewardedAd();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WXBridge_PreloadRewardedAd();
#endif
        }

        public void ShowRewardedAd(Action<bool> onComplete)
        {
            if (!CanUseNativeAds(_rewardedAdUnitId))
            {
                _fallback.ShowRewardedAd(onComplete);
                return;
            }

            if (_isRewardedPending)
            {
                GameLog.LogWarning("[WeChatBridge:WebGL] Rewarded ad request ignored: previous request still pending.");
                onComplete?.Invoke(false);
                return;
            }

            _isRewardedPending = true;
            _rewardedCallback = onComplete;
            TimerService.Instance.Cancel(_rewardedTimeoutTimer);
            _rewardedTimeoutTimer = TimerService.Instance.Delay(RewardedAdTimeoutSeconds, OnRewardedAdTimeout, true);

#if UNITY_WEBGL && !UNITY_EDITOR
            WXBridge_ShowRewardedAd();
#endif

        }

        public void ShowBannerAd()
        {
            if (!CanUseNativeAds(_bannerAdUnitId))
            {
                _fallback.ShowBannerAd();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WXBridge_ShowBannerAd();
#endif
        }

        public void HideBannerAd()
        {
            if (!CanUseNativeAds(_bannerAdUnitId))
            {
                _fallback.HideBannerAd();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WXBridge_HideBannerAd();
#endif
        }

        public void ShowInterstitialAd()
        {
            if (!CanUseNativeAds(_interstitialAdUnitId))
            {
                _fallback.ShowInterstitialAd();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WXBridge_ShowInterstitialAd();
#endif
        }

        public void Share(string title, string imageUrl, string query = "")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (TryInitializeNativeBridge() && IsWeChatPlatform)
            {
                WXBridge_Share(title ?? string.Empty, imageUrl ?? string.Empty, query ?? string.Empty);
                return;
            }
#endif
            _fallback.Share(title, imageUrl, query);
        }

        public void SubmitScore(int score)
        {
            _fallback.SubmitScore(score);
        }

        public void ShowRankingPanel()
        {
            _fallback.ShowRankingPanel();
        }

        public void RequestSubscribeMessage(string[] templateIds, Action<string[]> onComplete)
        {
            _fallback.RequestSubscribeMessage(templateIds, onComplete);
        }

        public void Login(Action<bool, string> onComplete)
        {
            _fallback.Login(onComplete);
        }

        public WeChatUserInfo GetUserInfo()
        {
            return _fallback.GetUserInfo();
        }

        public void OnShow(Action<Dictionary<string, string>> callback)
        {
            _fallback.OnShow(callback);
        }

        public void OnHide(Action callback)
        {
            _fallback.OnHide(callback);
        }

        public LaunchOptions GetLaunchOptions()
        {
            return _fallback.GetLaunchOptions();
        }

        public void CheckPrivacyAuthorize(Action<bool> onResult)
        {
            _fallback.CheckPrivacyAuthorize(onResult);
        }

        public void RequirePrivacyAuthorize(Action<bool> onComplete)
        {
            _fallback.RequirePrivacyAuthorize(onComplete);
        }

        public string GetPrivacySettingName()
        {
            return _fallback.GetPrivacySettingName();
        }

        public void Vibrate(bool isLong = false)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (TryInitializeNativeBridge() && IsWeChatPlatform)
            {
                WXBridge_Vibrate(isLong ? 1 : 0);
                return;
            }
#endif
            _fallback.Vibrate(isLong);
        }

        public void SetClipboardData(string text, Action<bool> onComplete = null)
        {
            _fallback.SetClipboardData(text, onComplete);
        }

        public void GetClipboardData(Action<string> onComplete)
        {
            _fallback.GetClipboardData(onComplete);
        }

        internal void HandleRewardedAdClosed(bool isEnded)
        {
            CompleteRewardedAdRequest(isEnded);
        }

        internal void HandleRewardedAdError(string error)
        {
            GameLog.LogWarning($"[WeChatBridge:WebGL] Rewarded ad error: {error}");
            CompleteRewardedAdRequest(false);
        }

        private void OnRewardedAdTimeout()
        {
            GameLog.LogWarning("[WeChatBridge:WebGL] Rewarded ad timeout. Fallback to failed result.");
            CompleteRewardedAdRequest(false);
        }

        private void CompleteRewardedAdRequest(bool isEnded)
        {
            if (!_isRewardedPending)
                return;

            TimerService.Instance.Cancel(_rewardedTimeoutTimer);
            _rewardedTimeoutTimer = TimerHandle.Invalid;

            var callback = _rewardedCallback;
            _rewardedCallback = null;
            _isRewardedPending = false;
            callback?.Invoke(isEnded);
        }


        private bool CanUseNativeAds(string adUnitId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!TryInitializeNativeBridge())
                return false;

            if (!IsWeChatPlatform)
                return false;

            if (string.IsNullOrEmpty(adUnitId))
            {
                GameLog.LogWarning("[WeChatBridge:WebGL] Missing ad unit id. Falling back to stub behavior.");
                return false;
            }

            return true;
#else
            return false;
#endif
        }

        private bool TryInitializeNativeBridge()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_nativeInitialized)
                return true;

            try
            {
                WeChatBridgeWebGLCallbackHost.Bind(this);
                WXBridge_Init(WeChatBridgeWebGLCallbackHost.BridgeGameObjectName);
                _nativeInitialized = true;
            }
            catch (Exception ex)
            {
                GameLog.LogWarning($"[WeChatBridge:WebGL] Native init failed: {ex.Message}");
                _nativeInitialized = false;
            }

            return _nativeInitialized;
#else
            return false;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WXBridge_Init(string gameObjectName);

        [DllImport("__Internal")]
        private static extern int WXBridge_IsWeChatEnv();

        [DllImport("__Internal")]
        private static extern void WXBridge_SetAdUnitIds(string rewardedAdUnitId, string bannerAdUnitId, string interstitialAdUnitId);

        [DllImport("__Internal")]
        private static extern void WXBridge_PreloadRewardedAd();

        [DllImport("__Internal")]
        private static extern void WXBridge_ShowRewardedAd();

        [DllImport("__Internal")]
        private static extern void WXBridge_ShowBannerAd();

        [DllImport("__Internal")]
        private static extern void WXBridge_HideBannerAd();

        [DllImport("__Internal")]
        private static extern void WXBridge_ShowInterstitialAd();

        [DllImport("__Internal")]
        private static extern void WXBridge_Share(string title, string imageUrl, string query);

        [DllImport("__Internal")]
        private static extern void WXBridge_Vibrate(int isLong);
#endif
    }
}
