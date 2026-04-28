using System;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using MiniGameTemplate.Utils;
#if UNITY_WEBGL && WEIXINMINIGAME
using WeChatWASM;
#endif

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Lightweight wrapper around YooAsset for the MiniGameTemplate framework.
    /// Provides a clean API for asset loading without exposing YooAsset internals.
    ///
    /// Supports 4 play modes: EditorSimulate, Offline, Host, and WebGL (WeChat Mini Game).
    ///
    /// Usage:
    ///   await AssetService.Instance.InitializeAsync(config);
    ///   var handle = AssetService.Instance.LoadAssetAsync&lt;GameObject&gt;("Assets/Prefabs/Player.prefab");
    ///   await handle.Task;
    ///   var prefab = handle.AssetObject as GameObject;
    /// </summary>
    public class AssetService : Singleton<AssetService>
    {
        private ResourcePackage _defaultPackage;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        /// <summary>
        /// Initialize the asset system with the given config.
        /// Must be called once during game bootstrap, before any asset loading.
        /// </summary>
        public async Task InitializeAsync(AssetConfig config)
        {
            if (_initialized)
            {
                GameLog.LogWarning("[AssetService] Already initialized.");
                return;
            }

            // SEC-04: Enforce HTTPS for CDN URLs to prevent MITM attacks on asset downloads.
            // Only applies to Host and WebGL modes where remote URLs are used.
            if (config.PlayMode == EAssetPlayMode.Host || config.PlayMode == EAssetPlayMode.WebGL)
            {
                ValidateUrlSecurity(config.CdnUrl, "CdnUrl");
                ValidateUrlSecurity(config.FallbackCdnUrl, "FallbackCdnUrl");
            }

            // Initialize YooAsset
            YooAssets.Initialize();

            // Create the default resource package
            _defaultPackage = YooAssets.CreatePackage(config.DefaultPackageName);
            YooAssets.SetDefaultPackage(_defaultPackage);

            // Initialize based on play mode
            InitializationOperation initOp = null;

            switch (config.PlayMode)
            {
#if UNITY_EDITOR
                case EAssetPlayMode.EditorSimulate:
                {
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(config.DefaultPackageName);
                    var parameters = new EditorSimulateModeParameters();
                    parameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory);
                    initOp = _defaultPackage.InitializeAsync(parameters);
                    break;
                }
#endif
                case EAssetPlayMode.Offline:
                {
                    var parameters = new OfflinePlayModeParameters();
                    parameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    initOp = _defaultPackage.InitializeAsync(parameters);
                    break;
                }

                case EAssetPlayMode.Host:
                {
                    string hostUrl = config.HostServerUrl;
                    string fallbackUrl = config.FallbackHostServerUrl;

                    if (string.IsNullOrEmpty(hostUrl))
                    {
                        Debug.LogError("[AssetService] Host mode requires CdnUrl to be configured in AssetConfig. " +
                            "Set it to your CDN base URL (e.g. https://cdn.example.com).");
                        return;
                    }

                    GameLog.Log($"[AssetService] Host mode: CDN={config.CdnUrl} → HostServerUrl={hostUrl}");

                    var parameters = new HostPlayModeParameters();
                    parameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    parameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                        new RemoteServices(hostUrl, fallbackUrl));
                    initOp = _defaultPackage.InitializeAsync(parameters);
                    break;
                }

                case EAssetPlayMode.WebGL:
                {
#if UNITY_WEBGL && WEIXINMINIGAME
                    // Increase async operation time slice to prevent single-frame-per-load stalling.
                    YooAssets.SetOperationSystemMaxTimeSlice(100);

                    var webParams = new WebPlayModeParameters();
                    // Force sync-like asset loading on WebGL to avoid frame-by-frame drip loading.
                    webParams.WebGLForceSyncLoadAsset = true;

                    // CDN URL is the Single Source of Truth — derived HostServerUrl points to
                    // the YooAsset package directory on CDN.
                    string hostUrl = config.HostServerUrl;

                    if (string.IsNullOrEmpty(hostUrl))
                    {
                        // CDN URL is required for WeChat Mini Game.
                        // In WeChat environment, ALL asset loading goes through HTTP (UnityWebRequest → wx.request).
                        // There is no local file:// fallback.
                        //
                        // Setup:
                        // 1. Set CdnUrl in AssetConfig to your CDN base URL
                        //    - Production: https://cdn.example.com
                        //    - Dev testing: http://192.168.x.x:8001 (use Dev Server tool)
                        // 2. Set the SAME value in MiniGameConfig.ProjectConf.CDN (WeChat export panel)
                        // 3. Re-export WebGL + WeChat conversion
                        GameLog.LogWarning("[AssetService] WebGL mode: CdnUrl is empty! " +
                            "WeChat Mini Game requires a CDN URL for asset loading. " +
                            "Set CdnUrl in AssetConfig (e.g. https://cdn.example.com for production, " +
                            "or http://192.168.x.x:8001 for local Dev Server). " +
                            "MUST also set the same value in MiniGameConfig.ProjectConf.CDN.");
                        throw new Exception(
                            "[AssetService] CdnUrl must be configured in AssetConfig for WeChat Mini Game. " +
                            "See console log for setup instructions.");
                    }

                    // Always use WechatFileSystem in WeChat Mini Game environment.
                    // DefaultWebServerFileSystem uses Application.streamingAssetsPath which resolves to
                    // "https://game.weixin.qq.com" (when DATA_CDN is empty) — a domain we don't control.
                    string packageRoot = $"{WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE/yoo";
                    string fallbackUrl = config.FallbackHostServerUrl;
                    var remoteServices = new RemoteServices(hostUrl, fallbackUrl);
                    webParams.WebServerFileSystemParameters =
                        WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, null);

                    GameLog.Log($"[AssetService] WebGL mode: CDN={config.CdnUrl} → HostServerUrl={hostUrl}");

                    initOp = _defaultPackage.InitializeAsync(webParams);
#else
                    // Non-WEIXINMINIGAME WebGL builds: fall back to standard web server mode.
                    GameLog.LogWarning("[AssetService] WebGL mode without WEIXINMINIGAME define. " +
                        "Using DefaultWebServerFileSystem as fallback.");
                    var webFallbackParams = new WebPlayModeParameters();
                    webFallbackParams.WebServerFileSystemParameters =
                        FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                    initOp = _defaultPackage.InitializeAsync(webFallbackParams);
#endif
                    break;
                }

                default:
                {
                    // Fallback for non-editor EditorSimulate selection
                    var parameters = new OfflinePlayModeParameters();
                    parameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    initOp = _defaultPackage.InitializeAsync(parameters);
                    break;
                }
            }

            await initOp.Task;

            if (initOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[AssetService] Failed to initialize: {initOp.Error}");
                return;
            }

            // Request package version and update manifest.
            // YooAsset 2.x requires this step after InitializeAsync to activate the manifest.
            // Without it, ActiveManifest remains null and all asset loads will throw.
            var versionOp = _defaultPackage.RequestPackageVersionAsync();
            await versionOp.Task;
            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[AssetService] Failed to request package version: {versionOp.Error}");
                return;
            }

            var manifestOp = _defaultPackage.UpdatePackageManifestAsync(versionOp.PackageVersion);
            await manifestOp.Task;
            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[AssetService] Failed to update package manifest: {manifestOp.Error}");
                return;
            }

            _initialized = true;
            GameLog.Log($"[AssetService] Initialized. Package: {config.DefaultPackageName}, Mode: {config.PlayMode}");
        }

        #region Asset Loading

        /// <summary>
        /// Load an asset asynchronously by its addressable path.
        /// </summary>
        public AssetHandle LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            EnsureInitialized();
            return _defaultPackage.LoadAssetAsync<T>(assetPath);
        }

        /// <summary>
        /// Load a sub-asset asynchronously (e.g., sprites from a sprite atlas).
        /// </summary>
        public SubAssetsHandle LoadSubAssetsAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            EnsureInitialized();
            return _defaultPackage.LoadSubAssetsAsync<T>(assetPath);
        }

        /// <summary>
        /// Load raw file data asynchronously.
        /// </summary>
        public RawFileHandle LoadRawFileAsync(string assetPath)
        {
            EnsureInitialized();
            return _defaultPackage.LoadRawFileAsync(assetPath);
        }

        /// <summary>
        /// Load all assets of a given type from a specific location.
        /// </summary>
        public AllAssetsHandle LoadAllAssetsAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            EnsureInitialized();
            return _defaultPackage.LoadAllAssetsAsync<T>(assetPath);
        }

        #endregion

        #region Scene Loading

        /// <summary>
        /// Load a scene asynchronously via YooAsset.
        /// </summary>
        public SceneHandle LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneMode sceneMode = UnityEngine.SceneManagement.LoadSceneMode.Single)
        {
            EnsureInitialized();
            return _defaultPackage.LoadSceneAsync(scenePath, sceneMode);
        }

        #endregion

        #region Resource Update (Host mode)

        /// <summary>
        /// Request the resource manifest version from the server.
        /// Only meaningful in Host play mode.
        /// </summary>
        public async Task<string> RequestPackageVersionAsync()
        {
            EnsureInitialized();
            var op = _defaultPackage.RequestPackageVersionAsync();
            await op.Task;
            if (op.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[AssetService] Failed to request version: {op.Error}");
                return null;
            }
            return op.PackageVersion;
        }

        /// <summary>
        /// Update the package manifest to a specific version.
        /// </summary>
        public async Task<bool> UpdatePackageManifestAsync(string version)
        {
            EnsureInitialized();
            var op = _defaultPackage.UpdatePackageManifestAsync(version);
            await op.Task;
            if (op.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[AssetService] Failed to update manifest: {op.Error}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a downloader for all pending resource updates.
        /// Returns null if nothing needs downloading.
        /// </summary>
        public ResourceDownloaderOperation CreateResourceDownloader()
        {
            EnsureInitialized();
            var downloader = _defaultPackage.CreateResourceDownloader(10, 3);
            if (downloader.TotalDownloadCount == 0)
            {
                GameLog.Log("[AssetService] No resources need downloading.");
                return null;
            }
            return downloader;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Force unload all unused assets from memory.
        /// Call after scene transitions or large UI closures.
        /// Returns null only if asset system is not initialized — caller should null-check before awaiting.
        /// </summary>
        public UnloadUnusedAssetsOperation UnloadUnusedAssetsAsync()
        {
            if (!_initialized || _defaultPackage == null)
            {
                GameLog.LogWarning("[AssetService] UnloadUnusedAssetsAsync called before initialization. Ignored.");
                return null;
            }
            return _defaultPackage.UnloadUnusedAssetsAsync();
        }

        /// <summary>
        /// Force unload ALL assets. Use sparingly — typically only on full game reset.
        /// Returns null only if asset system is not initialized — caller should null-check before awaiting.
        /// </summary>
        public UnloadAllAssetsOperation ForceUnloadAllAssetsAsync()
        {
            if (!_initialized || _defaultPackage == null)
            {
                GameLog.LogWarning("[AssetService] ForceUnloadAllAssetsAsync called before initialization. Ignored.");
                return null;
            }
            return _defaultPackage.UnloadAllAssetsAsync();
        }

        #endregion

        private void EnsureInitialized()
        {
            if (!_initialized)
                Debug.LogError("[AssetService] Not initialized! Call InitializeAsync() first.");
        }

        /// <summary>
        /// SEC: Validate that remote URLs use HTTPS to prevent MITM attacks.
        /// Local/private network addresses (127.0.0.1, 10.x, 172.16-31.x, 192.168.x, localhost)
        /// are exempt — HTTP is expected for local development servers.
        /// </summary>
        private static void ValidateUrlSecurity(string url, string fieldName)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                // Allow HTTP for local/private network addresses (development only).
                if (IsLocalNetworkUrl(url))
                {
                    GameLog.Log($"[AssetService] {fieldName} uses HTTP on local network — OK for development.");
                    return;
                }

#if UNITY_EDITOR
                Debug.LogWarning($"[AssetService] SEC: {fieldName} uses HTTP (insecure). " +
                    "This is acceptable for local testing but MUST use HTTPS in production builds.");
#else
                Debug.LogError($"[AssetService] SEC: {fieldName} uses HTTP (insecure). " +
                    "All CDN URLs MUST use HTTPS to prevent man-in-the-middle attacks on asset downloads. " +
                    "Change the URL to https:// in AssetConfig.");
#endif
            }
        }

        /// <summary>
        /// Check if a URL points to a local/private network address.
        /// </summary>
        private static bool IsLocalNetworkUrl(string url)
        {
            try
            {
                var uri = new System.Uri(url);
                string host = uri.Host;
                return host == "127.0.0.1" ||
                       host == "localhost" ||
                       host.StartsWith("10.") ||
                       host.StartsWith("192.168.") ||
                       (host.StartsWith("172.") && IsPrivate172(host));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPrivate172(string host)
        {
            // 172.16.0.0 - 172.31.255.255
            var parts = host.Split('.');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int second))
                return second >= 16 && second <= 31;
            return false;
        }

        protected override void OnDestroy()
        {
            if (_initialized)
            {
                YooAssets.Destroy();
                _initialized = false;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Remote server URL provider for YooAsset Host mode.
    /// Includes URL normalization to prevent WeChat silent-failure bugs.
    /// </summary>
    internal class RemoteServices : IRemoteServices
    {
        private readonly string _hostServer;
        private readonly string _fallbackServer;

        public RemoteServices(string hostServer, string fallbackServer)
        {
            _hostServer = hostServer?.TrimEnd('/') ?? string.Empty;
            _fallbackServer = fallbackServer?.TrimEnd('/') ?? string.Empty;
        }

        public string GetRemoteMainURL(string fileName)
        {
            return NormalizeUrl($"{_hostServer}/{fileName}");
        }

        public string GetRemoteFallbackURL(string fileName)
        {
            return NormalizeUrl($"{_fallbackServer}/{fileName}");
        }

        /// <summary>
        /// Normalize a URL to prevent WeChat Mini Game silent loading failures:
        /// 1. Replace backslashes with forward slashes (Windows path leak)
        /// 2. Collapse double slashes (except in protocol "://")
        /// </summary>
        private static string NormalizeUrl(string url)
        {
            // Replace backslashes (Windows paths leaking into URLs)
            url = url.Replace('\\', '/');

            // Collapse double slashes after the protocol prefix
            int schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0)
            {
                string scheme = url.Substring(0, schemeEnd + 3);
                string path = url.Substring(schemeEnd + 3);
                // Collapse all double slashes in the path portion
                while (path.Contains("//"))
                    path = path.Replace("//", "/");
                url = scheme + path;
            }

            return url;
        }
    }
}
