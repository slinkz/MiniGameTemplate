using UnityEngine;

namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// WeChat platform configuration — single source of truth.
    /// One instance per project, referenced wherever WeChat config is needed.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Platform/WeChat Config", order = 10)]
    public class WeChatConfig : ScriptableObject
    {
        [Header("Cloud Development")]
        [Tooltip("Cloud development environment ID (from WeChat Cloud Console). " +
                 "Leave empty to use default environment.")]
        [SerializeField] private string _cloudEnvId = "";

        [Header("Ads")]
        [Tooltip("Rewarded video ad unit ID. Leave empty to disable.")]
        [SerializeField] private string _rewardedAdUnitId = "";

        [Tooltip("Banner ad unit ID. Leave empty to disable.")]
        [SerializeField] private string _bannerAdUnitId = "";

        [Tooltip("Interstitial ad unit ID. Leave empty to disable.")]
        [SerializeField] private string _interstitialAdUnitId = "";

        [Header("Behavior")]
        [Tooltip("Whether to display banner ads in the main menu.")]
        [SerializeField] private bool _enableBannerAdInMainMenu = true;

        // --- Public accessors ---
        public string CloudEnvId => _cloudEnvId ?? string.Empty;
        public string RewardedAdUnitId => _rewardedAdUnitId ?? string.Empty;
        public string BannerAdUnitId => _bannerAdUnitId ?? string.Empty;
        public string InterstitialAdUnitId => _interstitialAdUnitId ?? string.Empty;
        public bool EnableBannerAdInMainMenu => _enableBannerAdInMainMenu;
    }
}
