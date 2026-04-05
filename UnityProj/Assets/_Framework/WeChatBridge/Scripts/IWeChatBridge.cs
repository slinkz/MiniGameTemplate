using System;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Abstraction layer for WeChat Mini Game SDK.
    /// Implement this interface to integrate the real WeChat SDK.
    /// </summary>
    public interface IWeChatBridge
    {
        // === Ads ===

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
        /// </summary>
        void SubmitScore(int score);

        /// <summary>
        /// Show the friend ranking panel.
        /// </summary>
        void ShowRankingPanel();

        // === User ===

        /// <summary>
        /// Request user login. Returns auth code on success.
        /// </summary>
        void Login(Action<bool, string> onComplete);

        /// <summary>
        /// Get cached user info (nickname, avatar URL).
        /// Returns null if not logged in or not authorized.
        /// </summary>
        WeChatUserInfo GetUserInfo();

        // === System ===

        /// <summary>
        /// Trigger vibration feedback (short or long).
        /// </summary>
        void Vibrate(bool isLong = false);

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
}
