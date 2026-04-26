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
                ValidateUrlSecurity(config.HostServerUrl, "HostServerUrl");
                ValidateUrlSecurity(config.FallbackHostServerUrl, "FallbackHostServerUrl");
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
                    var parameters = new HostPlayModeParameters();
                    parameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    parameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                        new RemoteServices(config.HostServerUrl, config.FallbackHostServerUrl));
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

                    bool hasRemoteUrl = !string.IsNullOrEmpty(config.HostServerUrl);

                    if (hasRemoteUrl)
                    {
                        // --- Production CDN mode ---
                        // WeChat Mini Game: use WechatFileSystem with CDN + WX cache.
                        string packageRoot = $"{WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE/yoo";
                        var remoteServices = new RemoteServices(config.HostServerUrl, config.FallbackHostServerUrl);
                        webParams.WebServerFileSystemParameters =
                            WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, null);

                        GameLog.Log("[AssetService] WebGL CDN mode: WechatFileSystem + RemoteServices.");
                    }
                    else
                    {
                        // --- Local testing mode (no CDN) ---
                        // Bundle files are pre-built and copied into StreamingAssets.
                        // DefaultWebServerFileSystem loads them via UnityWebRequest from the
                        // local HTTP server that WeChat DevTools provides.
                        // This allows real-device testing without deploying a CDN.
                        webParams.WebServerFileSystemParameters =
                            FileSystemParameters.CreateDefaultWebServerFileSystemParameters();

                        GameLog.LogWarning("[AssetService] WebGL LOCAL mode: loading bundles from StreamingAssets. " +
                            "Set HostServerUrl in AssetConfig to enable CDN mode for production.");
                    }

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
        /// Logs a warning in editor (for local testing with http://), errors in builds.
        /// </summary>
        private static void ValidateUrlSecurity(string url, string fieldName)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
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
