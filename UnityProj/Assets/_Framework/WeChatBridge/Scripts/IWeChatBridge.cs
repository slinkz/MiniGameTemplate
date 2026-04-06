using System;
using System.Collections.Generic;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Abstraction layer for WeChat Mini Game SDK.
    /// Implement this interface to integrate the real WeChat SDK.
    ///
    /// Coverage: Ads, Social, User, Lifecycle, System utilities.
    /// </summary>
    public interface IWeChatBridge
    {
        // === Ads ===

        /// <summary>
        /// Preload a rewarded video ad so it's ready to show instantly.
        /// Call this early (e.g. on scene load) to avoid showing a loading spinner.
        /// </summary>
        void PreloadRewardedAd();

        /// <summary>
        /// Show a rewarded video ad. Callback: true if reward earned, false if skipped/failed.
        /// </summary>
        void ShowRewardedAd(Action<bool> onComplete);

        /// <summary>
        /// Show a banner ad.
        /// </summary>
        void ShowBannerAd();

        /// <summary>
        /// Hide the banner ad.
        /// </summary>
        void HideBannerAd();

        /// <summary>
        /// Show an interstitial ad.
        /// </summary>
        void ShowInterstitialAd();

        // === Social ===

        /// <summary>
        /// Share to WeChat with title, image URL, and optional query string.
        /// </summary>
        void Share(string title, string imageUrl, string query = "");

        /// <summary>
        /// Submit score to the WeChat friend ranking.
        ///
        /// SECURITY: Client-reported scores can be trivially forged. For competitive features,
        /// validate scores server-side (e.g., replay verification, server-authoritative game logic,
        /// or statistical anomaly detection) before committing to the leaderboard.
        /// </summary>
        void SubmitScore(int score);

        /// <summary>
        /// Show the friend ranking panel.
        /// </summary>
        void ShowRankingPanel();

        /// <summary>
        /// Request subscription message permission.
        /// Callback receives list of accepted template IDs.
        /// </summary>
        void RequestSubscribeMessage(string[] templateIds, Action<string[]> onComplete);

        // === User ===

        /// <summary>
        /// Request user login. Returns auth code on success.
        ///
        /// SECURITY: The auth code MUST be sent to your backend server immediately for
        /// wx.code2session exchange. NEVER store the auth code locally, log it, or use it
        /// for any client-side logic. The session_key returned by code2session is a server
        /// secret and must never be sent to the client.
        /// </summary>
        void Login(Action<bool, string> onComplete);

        /// <summary>
        /// Get cached user info (nickname, avatar URL).
        /// Returns null if not logged in or not authorized.
        /// </summary>
        WeChatUserInfo GetUserInfo();

        // === Lifecycle ===

        /// <summary>
        /// Register callback for when the mini game comes to foreground (wx.onShow).
        /// The dictionary contains launch query parameters.
        /// </summary>
        void OnShow(Action<Dictionary<string, string>> callback);

        /// <summary>
        /// Register callback for when the mini game goes to background (wx.onHide).
        /// </summary>
        void OnHide(Action callback);

        /// <summary>
        /// Get the launch options (scene, query, referrerInfo).
        /// Useful for handling share-card entry and scene-based routing.
        /// </summary>
        LaunchOptions GetLaunchOptions();

        // === Privacy ===

        /// <summary>
        /// Check whether the user needs to authorize privacy settings.
        /// Callback: true if authorization is needed (first launch or policy updated), false otherwise.
        /// </summary>
        void CheckPrivacyAuthorize(Action<bool> onResult);

        /// <summary>
        /// Execute the privacy authorization flow.
        /// Callback: true if the user accepted, false if rejected or failed.
        /// </summary>
        void RequirePrivacyAuthorize(Action<bool> onComplete);

        /// <summary>
        /// Get the display name of the privacy policy (e.g. "隐私保护指引").
        /// </summary>
        string GetPrivacySettingName();

        // === System ===

        /// <summary>
        /// Trigger vibration feedback (short or long).
        /// </summary>
        void Vibrate(bool isLong = false);

        /// <summary>
        /// Copy text to clipboard.
        /// </summary>
        void SetClipboardData(string text, Action<bool> onComplete = null);

        /// <summary>
        /// Get text from clipboard.
        /// </summary>
        void GetClipboardData(Action<string> onComplete);

        /// <summary>
        /// Check if the current platform is WeChat Mini Game.
        /// </summary>
        bool IsWeChatPlatform { get; }
    }

    /// <summary>
    /// Basic user info from WeChat.
    /// </summary>
    public class WeChatUserInfo
    {
        public string Nickname;
        public string AvatarUrl;
        public string OpenId;
    }

    /// <summary>
    /// Mini game launch options (corresponds to wx.getLaunchOptionsSync).
    /// </summary>
    public class LaunchOptions
    {
        /// <summary>Scene ID that triggered the launch.</summary>
        public int Scene;

        /// <summary>Query parameters from the share card or QR code.</summary>
        public Dictionary<string, string> Query = new Dictionary<string, string>();

        /// <summary>Referrer app ID (if launched from another mini program).</summary>
        public string ReferrerAppId;
    }
}
