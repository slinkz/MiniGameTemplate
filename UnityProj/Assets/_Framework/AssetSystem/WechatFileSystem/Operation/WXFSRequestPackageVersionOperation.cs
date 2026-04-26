#if UNITY_WEBGL && WEIXINMINIGAME
using YooAsset;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Request package version from CDN for WeChat Mini Game.
    /// Delegates to YooAsset's built-in RequestWebPackageVersionOperation.
    /// </summary>
    internal class WXFSRequestPackageVersionOperation : FSRequestPackageVersionOperation
    {
        private enum ESteps
        {
            None,
            RequestVersion,
            Done,
        }

        private readonly WechatFileSystem _fileSystem;
        private readonly bool _appendTimeTicks;
        private readonly int _timeout;
        private RequestWebPackageVersionOperation _requestOp;
        private ESteps _steps = ESteps.None;

        public WXFSRequestPackageVersionOperation(WechatFileSystem fileSystem, bool appendTimeTicks, int timeout)
        {
            _fileSystem = fileSystem;
            _appendTimeTicks = appendTimeTicks;
            _timeout = timeout;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.RequestVersion;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestVersion)
            {
                if (_requestOp == null)
                {
                    _requestOp = new RequestWebPackageVersionOperation(
                        _fileSystem.RemoteServices,
                        _fileSystem.PackageName,
                        _appendTimeTicks,
                        _timeout);
                    _requestOp.StartOperation();
                    AddChildOperation(_requestOp);
                }

                _requestOp.UpdateOperation();
                Progress = _requestOp.Progress;
                if (!_requestOp.IsDone)
                    return;

                if (_requestOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _requestOp.PackageVersion;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestOp.Error;
                }
            }
        }
    }
}
#endif
