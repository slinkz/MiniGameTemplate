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
                 "Host: loads from a remote CDN/server.")]
        [SerializeField] private EAssetPlayMode _playMode = EAssetPlayMode.EditorSimulate;

        [Header("Host Server (only for Host play mode)")]
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
        Host
    }
}
