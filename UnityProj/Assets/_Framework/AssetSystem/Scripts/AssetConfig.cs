using UnityEngine;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Asset system configuration as a ScriptableObject.
    /// Controls YooAsset initialization parameters.
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
                 "WebGL: WeChat Mini Game mode.\n" +
                 "  - With CDN URL: WechatFileSystem + CDN (production)\n" +
                 "  - Without CDN URL: StreamingAssets local mode (testing)")]
        [SerializeField] private EAssetPlayMode _playMode = EAssetPlayMode.EditorSimulate;

        [Header("Host Server (for Host and WebGL play modes)")]
        [Tooltip("Leave EMPTY for local testing (bundles loaded from StreamingAssets).\n" +
                 "Set HTTPS URL for production CDN mode.")]
        [SerializeField] private string _hostServerUrl = "";
        [SerializeField] private string _fallbackHostServerUrl = "";

        public string DefaultPackageName => _defaultPackageName;
        public EAssetPlayMode PlayMode => _playMode;
        public string HostServerUrl => _hostServerUrl;
        public string FallbackHostServerUrl => _fallbackHostServerUrl;
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
        /// WeChat Mini Game mode.
        /// - If HostServerUrl is set: uses WechatFileSystem + CDN (production).
        /// - If HostServerUrl is empty: uses DefaultWebServerFileSystem from StreamingAssets (local testing).
        /// Requires: WX-WASM-SDK-V2 (com.qq.weixin.minigame).
        /// </summary>
        WebGL
    }
}
