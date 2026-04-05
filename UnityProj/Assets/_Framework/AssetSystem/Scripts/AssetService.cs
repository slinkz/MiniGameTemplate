using System;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Lightweight wrapper around YooAsset for the MiniGameTemplate framework.
    /// Provides a clean API for asset loading without exposing YooAsset internals.
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
                Debug.LogWarning("[AssetService] Already initialized.");
                return;
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
                    parameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult);
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

            _initialized = true;
            Debug.Log($"[AssetService] Initialized. Package: {config.DefaultPackageName}, Mode: {config.PlayMode}");
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
                Debug.Log("[AssetService] No resources need downloading.");
                return null;
            }
            return downloader;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Force unload all unused assets from memory.
        /// Call after scene transitions or large UI closures.
        /// </summary>
        public void UnloadUnusedAssets()
        {
            if (_defaultPackage != null)
                _defaultPackage.UnloadUnusedAssets();
        }

        /// <summary>
        /// Force unload ALL assets. Use sparingly — typically only on full game reset.
        /// </summary>
        public void ForceUnloadAllAssets()
        {
            if (_defaultPackage != null)
                _defaultPackage.ForceUnloadAllAssets();
        }

        #endregion

        private void EnsureInitialized()
        {
            if (!_initialized)
                Debug.LogError("[AssetService] Not initialized! Call InitializeAsync() first.");
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                YooAssets.Destroy();
                _initialized = false;
            }
        }
    }

    /// <summary>
    /// Remote server URL provider for YooAsset Host mode.
    /// </summary>
    internal class RemoteServices : IRemoteServices
    {
        private readonly string _hostServer;
        private readonly string _fallbackServer;

        public RemoteServices(string hostServer, string fallbackServer)
        {
            _hostServer = hostServer;
            _fallbackServer = fallbackServer;
        }

        public string GetRemoteMainURL(string fileName)
        {
            return $"{_hostServer}/{fileName}";
        }

        public string GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackServer}/{fileName}";
        }
    }
}
