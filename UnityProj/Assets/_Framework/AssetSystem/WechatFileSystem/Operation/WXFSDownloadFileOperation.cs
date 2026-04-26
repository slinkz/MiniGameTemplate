#if UNITY_WEBGL && WEIXINMINIGAME
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Download a bundle file in WeChat Mini Game environment.
    /// WeChat SDK handles caching transparently via WX.GetCachePath().
    /// This operation only needs to trigger the download; the WX runtime caches it.
    /// </summary>
    internal class WXFSDownloadFileOperation : FSDownloadFileOperation
    {
        private enum ESteps
        {
            None,
            Download,
            Done,
        }

        private readonly WechatFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private readonly DownloadFileOptions _options;
        private UnityWebRequest _webRequest;
        private ESteps _steps = ESteps.None;

        public WXFSDownloadFileOperation(
            WechatFileSystem fileSystem, PackageBundle bundle, DownloadFileOptions options)
            : base(bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
            _options = options;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.Download;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Download)
            {
                if (_webRequest == null)
                {
                    // Use the URL set by DownloadFileOptions (main or fallback)
                    string url = _fileSystem.RemoteServices.GetRemoteMainURL(_bundle.FileName);
                    _webRequest = UnityWebRequest.Get(url);
                    _webRequest.SendWebRequest();
                }

                if (!_webRequest.isDone)
                {
                    Progress = _webRequest.downloadProgress;
                    DownloadedBytes = (long)_webRequest.downloadedBytes;
                    return;
                }

                if (_webRequest.result == UnityWebRequest.Result.Success)
                {
                    // WeChat SDK automatically caches the downloaded content
                    // via the WX plugin when using the minigame WebGL build.
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"[WechatFileSystem] Download failed: {_bundle.FileName}, Error: {_webRequest.error}";
                }

                _webRequest.Dispose();
                _webRequest = null;
            }
        }
    }
}
#endif
