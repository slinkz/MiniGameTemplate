#if UNITY_WEBGL && WEIXINMINIGAME
using YooAsset;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// WeChat file system initialization.
    /// Unlike DefaultWebServerFileSystem, we don't need to load a catalog
    /// because all bundles are managed via remote CDN + local WX cache.
    /// </summary>
    internal class WXFSInitializeOperation : FSInitializeFileSystemOperation
    {
        private enum ESteps
        {
            None,
            Done,
        }

        private ESteps _steps = ESteps.None;

        public WXFSInitializeOperation(WechatFileSystem fileSystem)
        {
        }

        internal override void InternalStart()
        {
            _steps = ESteps.Done;
            Status = EOperationStatus.Succeed;
        }

        internal override void InternalUpdate()
        {
        }
    }
}
#endif
