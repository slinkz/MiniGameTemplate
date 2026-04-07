namespace MiniGameTemplate.Platform
{
    /// <summary>
    /// Optional ad-configuration extension for IWeChatBridge implementations.
    /// Keeps ad unit IDs outside gameplay code.
    /// </summary>
    internal interface IWeChatAdConfigurable
    {
        void ConfigureAds(string rewardedAdUnitId, string bannerAdUnitId, string interstitialAdUnitId);
    }
}
