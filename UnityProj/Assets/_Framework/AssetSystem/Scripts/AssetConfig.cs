using UnityEngine;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Asset system configuration as a ScriptableObject.
    /// Controls YooAsset initialization parameters.
    ///
    /// CDN URL is resolved at runtime from the WeChat conversion panel config
    /// (game.js DATA_CDN) via WXDataCDNHelper.GetDataCDN().
    /// No CDN field here — single source of truth lives in the conversion panel.
    ///
    /// At runtime, the full HostServerUrl is derived automatically by AssetService:
    ///   HostServerUrl = {DATA_CDN}/StreamingAssets/yoo/{PackageName}
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
                 "WebGL: WeChat Mini Game mode (CDN read from conversion panel at runtime).")]
        [SerializeField] private EAssetPlayMode _playMode = EAssetPlayMode.EditorSimulate;

        public string DefaultPackageName => _defaultPackageName;
        public EAssetPlayMode PlayMode => _playMode;
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
        /// WeChat Mini Game mode. CDN URL is read at runtime from the conversion panel config.
        /// Uses WechatFileSystem + CDN for both production and local dev.
        /// Requires: WX-WASM-SDK-V2 (com.qq.weixin.minigame).
        /// </summary>
        WebGL
    }
}
