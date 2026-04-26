#if UNITY_WEBGL && WEIXINMINIGAME
using YooAsset;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Load package manifest from CDN for WeChat Mini Game.
    /// Delegates to YooAsset's built-in LoadWebPackageManifestOperation.
    /// </summary>
    internal class WXFSLoadPackageManifestOperation : FSLoadPackageManifestOperation
    {
        private enum ESteps
        {
            None,
            RequestPackageHash,
            LoadManifest,
            Done,
        }

        private readonly WechatFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly int _timeout;
        private RequestWebPackageHashOperation _hashOp;
        private LoadWebPackageManifestOperation _manifestOp;
        private ESteps _steps = ESteps.None;

        public WXFSLoadPackageManifestOperation(
            WechatFileSystem fileSystem, string packageVersion, int timeout)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _timeout = timeout;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.RequestPackageHash;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestPackageHash)
            {
                if (_hashOp == null)
                {
                    _hashOp = new RequestWebPackageHashOperation(
                        _fileSystem.RemoteServices,
                        _fileSystem.PackageName,
                        _packageVersion,
                        _timeout);
                    _hashOp.StartOperation();
                    AddChildOperation(_hashOp);
                }

                _hashOp.UpdateOperation();
                Progress = _hashOp.Progress;
                if (!_hashOp.IsDone)
                    return;

                if (_hashOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.LoadManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _hashOp.Error;
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_manifestOp == null)
                {
                    _manifestOp = new LoadWebPackageManifestOperation(
                        _fileSystem.ManifestServices,
                        _fileSystem.RemoteServices,
                        _fileSystem.PackageName,
                        _packageVersion,
                        _hashOp.PackageHash,
                        _timeout);
                    _manifestOp.StartOperation();
                    AddChildOperation(_manifestOp);
                }

                _manifestOp.UpdateOperation();
                Progress = _manifestOp.Progress;
                if (!_manifestOp.IsDone)
                    return;

                if (_manifestOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.Done;
                    Manifest = _manifestOp.Manifest;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _manifestOp.Error;
                }
            }
        }
    }
}
#endif
