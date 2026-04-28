using UnityEngine;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Asset system configuration as a ScriptableObject.
    /// Controls YooAsset initialization parameters.
    ///
    /// CDN Base URL is the Single Source of Truth for remote asset loading.
    /// At runtime, the full HostServerUrl is derived automatically:
    ///   HostServerUrl = CDN + /StreamingAssets/yoo/{PackageName}
    ///
    /// This value MUST match MiniGameConfig.ProjectConf.CDN (which becomes
    /// DATA_CDN in game.js). The Dev Server editor tool can verify consistency.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Core/Asset Config", order = 2)]
    public class AssetConfig : ScriptableObject
    {
        [Header("Package Settings")]
        [Tooltip("The default YooAsset package name.")]
        [SerializeField] private string _defaultPackageName = "DefaultPackage";

        [Header("Play Mode")]
        [Tooltip("Editor Simulate: loads from AssetDatabase (no build needed).\n" +
                 "Offline: loads from pre-built bundles in StreamingAssets.\n" +
                 "Host: loads from a remote CDN/server.\n" +
                 "WebGL: WeChat Mini Game mode (requires CDN URL).")]
        [SerializeField] private EAssetPlayMode _playMode = EAssetPlayMode.EditorSimulate;

        [Header("CDN (Single Source of Truth)")]
        [Tooltip("CDN base URL — protocol + host + port only.\n" +
                 "Examples:\n" +
                 "  Production: https://cdn.example.com\n" +
                 "  Dev Server: http://192.168.1.100:8001\n\n" +
                 "At runtime, HostServerUrl is derived automatically:\n" +
                 "  {CDN}/StreamingAssets/yoo/{PackageName}\n\n" +
                 "MUST match MiniGameConfig.ProjectConf.CDN (→ game.js DATA_CDN).")]
        [SerializeField] private string _cdnUrl = "";

        [Tooltip("Fallback CDN base URL. Same format as CDN URL.\n" +
                 "Used when the primary CDN is unreachable.")]
        [SerializeField] private string _fallbackCdnUrl = "";

        public string DefaultPackageName => _defaultPackageName;
        public EAssetPlayMode PlayMode => _playMode;

        /// <summary>
        /// CDN base URL (protocol + host + port). Single Source of Truth.
        /// Must match MiniGameConfig.ProjectConf.CDN.
        /// </summary>
        public string CdnUrl => _cdnUrl;

        /// <summary>
        /// Fallback CDN base URL.
        /// </summary>
        public string FallbackCdnUrl => _fallbackCdnUrl;

        /// <summary>
        /// Full host server URL for YooAsset, derived from CDN base URL.
        /// Format: {CdnUrl}/StreamingAssets/yoo/{PackageName}
        /// Returns empty string if CdnUrl is not configured.
        /// </summary>
        public string HostServerUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_cdnUrl)) return string.Empty;
                return $"{_cdnUrl.TrimEnd('/')}/StreamingAssets/yoo/{_defaultPackageName}";
            }
        }

        /// <summary>
        /// Full fallback host server URL for YooAsset, derived from fallback CDN base URL.
        /// </summary>
        public string FallbackHostServerUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_fallbackCdnUrl)) return string.Empty;
                return $"{_fallbackCdnUrl.TrimEnd('/')}/StreamingAssets/yoo/{_defaultPackageName}";
            }
        }
    }

    public enum EAssetPlayMode
    {
        /// <summary>
        /// Editor-only: loads directly from AssetDatabase. No bundle build needed.
        /// </summary>
        EditorSimulate,

        /// <summary>
        /// Loads from pre-built bundles in StreamingAssets. No server required.
        /// </summary>
        Offline,

        /// <summary>
        /// Loads from a remote CDN/server with local cache fallback.
        /// </summary>
        Host,

        /// <summary>
        /// WeChat Mini Game mode. Requires CDN URL to be configured.
        /// Uses WechatFileSystem + CDN for both production and local dev.
        /// Requires: WX-WASM-SDK-V2 (com.qq.weixin.minigame).
        /// </summary>
        WebGL
    }
}
