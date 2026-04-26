#if UNITY_WEBGL && WEIXINMINIGAME
using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using WeChatWASM;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Factory for creating WeChat Mini Game file system parameters.
    /// Used with YooAsset's WebPlayModeParameters.
    /// </summary>
    public static class WechatFileSystemCreater
    {
        /// <summary>
        /// Create file system parameters for WeChat Mini Game.
        /// </summary>
        /// <param name="packageRoot">Cache root directory, e.g. WX.env.USER_DATA_PATH + "/__GAME_FILE_CACHE/yoo"</param>
        /// <param name="remoteServices">Remote CDN URL provider</param>
        [UnityEngine.Scripting.Preserve]
        public static FileSystemParameters CreateFileSystemParameters(
            string packageRoot, IRemoteServices remoteServices)
        {
            string fileSystemClass = typeof(WechatFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, packageRoot);
            fileSystemParams.AddParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            return fileSystemParams;
        }

        /// <summary>
        /// Create file system parameters with optional decryption support.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static FileSystemParameters CreateFileSystemParameters(
            string packageRoot, IRemoteServices remoteServices,
            IWebDecryptionServices decryptionServices)
        {
            string fileSystemClass = typeof(WechatFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, packageRoot);
            fileSystemParams.AddParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            if (decryptionServices != null)
            {
                fileSystemParams.AddParameter(FileSystemParametersDefine.DECRYPTION_SERVICES, decryptionServices);
            }
            return fileSystemParams;
        }
    }

    /// <summary>
    /// WeChat Mini Game file system implementation for YooAsset.
    /// Uses WX.GetCachePath() for cache existence checks and WXFileSystemManager for file I/O.
    ///
    /// Reference: https://wechat-miniprogram.github.io/minigame-unity-webgl-transform/Design/UsingAssetBundle.html
    /// </summary>
    internal class WechatFileSystem : IFileSystem
    {
        /// <summary>
        /// Fallback remote services when CDN is not configured — serves from StreamingAssets.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private class WebRemoteServices : IRemoteServices
        {
            private readonly string _webPackageRoot;
            private readonly Dictionary<string, string> _mapping = new Dictionary<string, string>(10000);

            public WebRemoteServices(string buildinPackRoot)
            {
                _webPackageRoot = buildinPackRoot;
            }

            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return GetFileLoadURL(fileName);
            }

            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return GetFileLoadURL(fileName);
            }

            private string GetFileLoadURL(string fileName)
            {
                if (!_mapping.TryGetValue(fileName, out string url))
                {
                    string filePath = PathUtility.Combine(_webPackageRoot, fileName);
                    url = DownloadSystemHelper.ConvertToWWWPath(filePath);
                    _mapping.Add(fileName, url);
                }
                return url;
            }
        }

        private readonly Dictionary<string, string> _cacheFilePathMapping = new Dictionary<string, string>(10000);
        private WXFileSystemManager _fileSystemMgr;
        private string _wxCacheRoot = string.Empty;

        /// <summary>
        /// Package name.
        /// </summary>
        public string PackageName { private set; get; }

        /// <summary>
        /// Cache root directory on the WeChat virtual file system.
        /// </summary>
        public string FileRoot => _wxCacheRoot;

        /// <summary>
        /// Number of cached files (not tracked — always returns 0).
        /// </summary>
        public int FileCount => 0;

        #region Custom Parameters

        /// <summary>
        /// Remote CDN services.
        /// </summary>
        public IRemoteServices RemoteServices { private set; get; }

        /// <summary>
        /// Optional decryption services for encrypted bundles.
        /// </summary>
        public IWebDecryptionServices DecryptionServices { private set; get; }

        /// <summary>
        /// Optional manifest restore/decrypt services.
        /// </summary>
        public IManifestRestoreServices ManifestServices { private set; get; }

        #endregion

        [UnityEngine.Scripting.Preserve]
        public WechatFileSystem()
        {
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSInitializeFileSystemOperation InitializeFileSystemAsync()
        {
            var operation = new WXFSInitializeOperation(this);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(string packageVersion, int timeout)
        {
            var operation = new WXFSLoadPackageManifestOperation(this, packageVersion, timeout);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(bool appendTimeTicks, int timeout)
        {
            var operation = new WXFSRequestPackageVersionOperation(this, appendTimeTicks, timeout);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSClearCacheFilesOperation ClearCacheFilesAsync(
            PackageManifest manifest, ClearCacheFilesOptions options)
        {
            if (options.ClearMode == EFileClearMode.ClearAllBundleFiles.ToString())
            {
                var operation = new WXFSClearAllBundleFilesOperation(this);
                return operation;
            }
            else if (options.ClearMode == EFileClearMode.ClearUnusedBundleFiles.ToString())
            {
                var operation = new WXFSClearUnusedBundleFilesOperation(this);
                return operation;
            }
            else
            {
                string error = $"Invalid clear mode : {options.ClearMode}";
                var operation = new FSClearCacheFilesCompleteOperation(error);
                return operation;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSDownloadFileOperation DownloadFileAsync(PackageBundle bundle, DownloadFileOptions options)
        {
            string mainURL = RemoteServices.GetRemoteMainURL(bundle.FileName);
            string fallbackURL = RemoteServices.GetRemoteFallbackURL(bundle.FileName);
            options.SetURL(mainURL, fallbackURL);
            var operation = new WXFSDownloadFileOperation(this, bundle, options);
            return operation;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual FSLoadBundleOperation LoadBundleFile(PackageBundle bundle)
        {
            if (bundle.BundleType == (int)EBuildBundleType.AssetBundle)
            {
                var operation = new WXFSLoadBundleOperation(this, bundle);
                return operation;
            }
            else
            {
                string error = $"{nameof(WechatFileSystem)} does not support bundle type: {bundle.BundleType}";
                var operation = new FSLoadBundleCompleteOperation(error);
                return operation;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void SetParameter(string name, object value)
        {
            if (name == FileSystemParametersDefine.REMOTE_SERVICES)
            {
                RemoteServices = (IRemoteServices)value;
            }
            else if (name == FileSystemParametersDefine.DECRYPTION_SERVICES)
            {
                DecryptionServices = (IWebDecryptionServices)value;
            }
            else if (name == FileSystemParametersDefine.MANIFEST_SERVICES)
            {
                ManifestServices = (IManifestRestoreServices)value;
            }
            else
            {
                YooLogger.Warning($"Invalid parameter : {name}");
            }
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void OnCreate(string packageName, string packageRoot)
        {
            PackageName = packageName;
            _wxCacheRoot = packageRoot;

            if (string.IsNullOrEmpty(_wxCacheRoot))
            {
                throw new Exception(
                    "[WechatFileSystem] packageRoot is not set! " +
                    "Must be: WX.env.USER_DATA_PATH + \"/__GAME_FILE_CACHE/yoo\"");
            }

            // Fallback: if RemoteServices is null, serve from StreamingAssets
            if (RemoteServices == null)
            {
                string webRoot = PathUtility.Combine(
                    Application.streamingAssetsPath,
                    YooAssetSettingsData.Setting.DefaultYooFolderName,
                    packageName);
                RemoteServices = new WebRemoteServices(webRoot);
            }

            // CRITICAL: double slashes in URL cause WeChat to silently fail loading
            {
                var mainURL = RemoteServices.GetRemoteMainURL("test.bundle");
                var fallbackURL = RemoteServices.GetRemoteFallbackURL("test.bundle");
                if (PathUtility.HasDoubleSlashes(mainURL) || PathUtility.HasDoubleSlashes(fallbackURL))
                {
                    throw new Exception(
                        $"[WechatFileSystem] RemoteServices URL contains double slashes! " +
                        $"Main: {mainURL}, Fallback: {fallbackURL}. " +
                        "WeChat will silently fail to load bundles. Fix your CDN URL configuration.");
                }
            }

            _fileSystemMgr = WX.GetFileSystemManager();
        }

        [UnityEngine.Scripting.Preserve]
        public virtual void OnDestroy()
        {
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool Belong(PackageBundle bundle)
        {
            return true;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool Exists(PackageBundle bundle)
        {
            string filePath = GetCacheFileLoadPath(bundle);
            return CheckCacheFileExist(filePath);
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool NeedDownload(PackageBundle bundle)
        {
            if (!Belong(bundle))
                return false;
            return !Exists(bundle);
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool NeedUnpack(PackageBundle bundle)
        {
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual bool NeedImport(PackageBundle bundle)
        {
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public virtual string GetBundleFilePath(PackageBundle bundle)
        {
            return GetCacheFileLoadPath(bundle);
        }

        [UnityEngine.Scripting.Preserve]
        public virtual byte[] ReadBundleFileData(PackageBundle bundle)
        {
            string filePath = GetCacheFileLoadPath(bundle);
            if (CheckCacheFileExist(filePath))
                return _fileSystemMgr.ReadFileSync(filePath);
            else
                return Array.Empty<byte>();
        }

        [UnityEngine.Scripting.Preserve]
        public virtual string ReadBundleFileText(PackageBundle bundle)
        {
            string filePath = GetCacheFileLoadPath(bundle);
            if (CheckCacheFileExist(filePath))
                return _fileSystemMgr.ReadFileSync(filePath, "utf8");
            else
                return string.Empty;
        }

        #region Internal Helpers

        [UnityEngine.Scripting.Preserve]
        public WXFileSystemManager GetFileSystemMgr()
        {
            return _fileSystemMgr;
        }

        /// <summary>
        /// Check if a cached file exists via WeChat's cache system.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public bool CheckCacheFileExist(string filePath)
        {
            string result = WX.GetCachePath(filePath);
            return !string.IsNullOrEmpty(result);
        }

        /// <summary>
        /// Get the local cache file path for a bundle.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public string GetCacheFileLoadPath(PackageBundle bundle)
        {
            if (!_cacheFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath))
            {
                filePath = PathUtility.Combine(_wxCacheRoot, bundle.FileName);
                _cacheFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// Get all file names currently tracked in the cache path mapping.
        /// Used by WXFSClearUnusedBundleFilesOperation to determine which files are still in use.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public HashSet<string> GetAllCachedFileNames()
        {
            var fileNames = new HashSet<string>();
            foreach (var kvp in _cacheFilePathMapping)
            {
                // Extract just the file name from the full cache path
                string path = kvp.Value;
                int lastSlash = path.LastIndexOf('/');
                string fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
                fileNames.Add(fileName);
            }
            return fileNames;
        }

        #endregion
    }
}
#endif
