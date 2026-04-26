#if UNITY_WEBGL && WEIXINMINIGAME
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Load an AssetBundle in WeChat Mini Game environment.
    /// Priority: local cache → remote CDN via UnityWebRequestAssetBundle.
    /// </summary>
    internal class WXFSLoadBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadFromCache,
            LoadFromRemote,
            CheckResult,
            Done,
        }

        private readonly WechatFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private UnityWebRequest _webRequest;
        private AssetBundleCreateRequest _createRequest;
        private ESteps _steps = ESteps.None;

        public WXFSLoadBundleOperation(WechatFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.LoadFromCache;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadFromCache)
            {
                string cachePath = _fileSystem.GetCacheFileLoadPath(_bundle);
                if (_fileSystem.CheckCacheFileExist(cachePath))
                {
                    // Load from WeChat local cache
                    string mainURL = _fileSystem.RemoteServices.GetRemoteMainURL(_bundle.FileName);
                    _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(mainURL);
                    _webRequest.SendWebRequest();
                    _steps = ESteps.CheckResult;
                }
                else
                {
                    // Not cached, load from remote
                    _steps = ESteps.LoadFromRemote;
                }
            }

            if (_steps == ESteps.LoadFromRemote)
            {
                string mainURL = _fileSystem.RemoteServices.GetRemoteMainURL(_bundle.FileName);
                _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(mainURL);
                _webRequest.SendWebRequest();
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (!_webRequest.isDone)
                {
                    DownloadProgress = _webRequest.downloadProgress;
                    return;
                }

                if (_webRequest.result == UnityWebRequest.Result.Success)
                {
                    var assetBundle = DownloadHandlerAssetBundle.GetContent(_webRequest);
                    if (assetBundle != null)
                    {
                        _steps = ESteps.Done;
                        Result = new AssetBundleResult(_fileSystem, _bundle, assetBundle, null);
                        Status = EOperationStatus.Succeed;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"[WechatFileSystem] Failed to get AssetBundle content: {_bundle.FileName}";
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"[WechatFileSystem] Failed to load bundle: {_bundle.FileName}, Error: {_webRequest.error}";
                }

                _webRequest.Dispose();
                _webRequest = null;
            }
        }

        internal override void InternalWaitForAsyncComplete()
        {
            // WebGL does not support synchronous waiting — this is a no-op.
        }
    }
}
#endif
