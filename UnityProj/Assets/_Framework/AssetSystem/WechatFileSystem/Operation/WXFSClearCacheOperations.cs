#if UNITY_WEBGL && WEIXINMINIGAME
using System.Collections.Generic;
using YooAsset;

namespace MiniGameTemplate.Asset
{
    /// <summary>
    /// Clear all cached bundle files from the WeChat file system.
    /// </summary>
    internal class WXFSClearAllBundleFilesOperation : FSClearCacheFilesOperation
    {
        private enum ESteps
        {
            None,
            ClearFiles,
            Done,
        }

        private readonly WechatFileSystem _fileSystem;
        private ESteps _steps = ESteps.None;

        public WXFSClearAllBundleFilesOperation(WechatFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.ClearFiles;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ClearFiles)
            {
                // Use WeChat's file system manager to remove the entire cache directory
                var fsMgr = _fileSystem.GetFileSystemMgr();
                try
                {
                    string cacheRoot = _fileSystem.FileRoot;
                    fsMgr.RmdirSync(cacheRoot, true);
                    fsMgr.MkdirSync(cacheRoot, true);
                }
                catch (System.Exception e)
                {
                    YooLogger.Warning($"[WechatFileSystem] Clear cache warning: {e.Message}");
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
        }
    }

    /// <summary>
    /// Clear unused cached bundle files (files not in the current manifest).
    /// </summary>
    internal class WXFSClearUnusedBundleFilesOperation : FSClearCacheFilesOperation
    {
        private enum ESteps
        {
            None,
            ClearUnused,
            Done,
        }

        private readonly WechatFileSystem _fileSystem;
        private ESteps _steps = ESteps.None;

        public WXFSClearUnusedBundleFilesOperation(WechatFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.ClearUnused;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ClearUnused)
            {
                // Collect all bundle GUIDs that the current manifest considers active.
                // We use IsIncludeBundleFile to check membership during the directory scan.
                var fsMgr = _fileSystem.GetFileSystemMgr();
                try
                {
                    string cacheRoot = _fileSystem.FileRoot;
                    string[] files = fsMgr.ReaddirSync(cacheRoot);
                    if (files != null)
                    {
                        // Build a set of known file names from the wrappers maintained by WechatFileSystem.
                        var knownFileNames = _fileSystem.GetAllCachedFileNames();
                        foreach (var file in files)
                        {
                            // If the file is not tracked by the current file system wrappers, it's unused.
                            if (!knownFileNames.Contains(file))
                            {
                                string filePath = PathUtility.Combine(cacheRoot, file);
                                fsMgr.UnlinkSync(filePath);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    YooLogger.Warning($"[WechatFileSystem] Clear unused warning: {e.Message}");
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
        }
    }
}
#endif
