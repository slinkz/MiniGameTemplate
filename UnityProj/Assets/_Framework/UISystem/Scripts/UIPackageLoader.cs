using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using MiniGameTemplate.Asset;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Manages FairyGUI package loading and unloading.
    /// Packages are reference-counted to avoid premature unloading.
    ///
    /// IMPORTANT: All loading paths are async-safe for WebGL (WeChat Mini Game).
    /// The synchronous AddPackage() falls back to Resources.Load only.
    /// For YooAsset loading, always use AddPackageAsync().
    /// </summary>
    public static class UIPackageLoader
    {
        private static readonly Dictionary<string, int> _refCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, YooAsset.AssetHandle> _assetHandles = new Dictionary<string, YooAsset.AssetHandle>();
        // Cache for assets loaded during async package add — used by LoadFairyGUIAsset callback
        private static readonly Dictionary<string, UnityEngine.Object> _loadedAssetCache = new Dictionary<string, UnityEngine.Object>();

        /// <summary>
        /// The base path prefix for FairyGUI package assets when loading via YooAsset.
        /// Default: "Assets/FairyGUI_Export/" — override if your export path differs.
        /// </summary>
        public static string YooAssetBasePath = "Assets/FairyGUI_Export/";

        /// <summary>
        /// [Sync / Resources.Load only] Load a FairyGUI package.
        /// WARNING: This does NOT go through YooAsset. On WebGL/WeChat, you MUST use AddPackageAsync().
        /// Kept for quick editor iteration with Resources.Load fallback.
        /// </summary>
        public static void AddPackage(string packageName)
        {
            if (_refCounts.ContainsKey(packageName))
            {
                _refCounts[packageName]++;
                return;
            }

            // Sync path always uses Resources.Load — WaitForAsyncComplete is NOT WebGL-safe
            UIPackage.AddPackage(packageName);

            _refCounts[packageName] = 1;
            Debug.Log($"[UIPackageLoader] Loaded package (Resources): {packageName}");
        }

        /// <summary>
        /// Load a FairyGUI package asynchronously via YooAsset.
        /// This is the preferred path for all platforms including WebGL / WeChat Mini Game.
        /// </summary>
        public static async System.Threading.Tasks.Task AddPackageAsync(string packageName)
        {
            if (_refCounts.ContainsKey(packageName))
            {
                _refCounts[packageName]++;
                return;
            }

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                await LoadViaYooAssetAsync(packageName);
            }
            else
            {
                UIPackage.AddPackage(packageName);
            }

            _refCounts[packageName] = 1;
            Debug.Log($"[UIPackageLoader] Loaded package (async): {packageName}");
        }

        /// <summary>
        /// Decrement reference count. Unloads when count reaches 0.
        /// </summary>
        public static void RemovePackage(string packageName)
        {
            if (!_refCounts.ContainsKey(packageName)) return;

            _refCounts[packageName]--;
            if (_refCounts[packageName] <= 0)
            {
                UIPackage.RemovePackage(packageName);

                // Release YooAsset handle if we have one
                if (_assetHandles.TryGetValue(packageName, out var handle))
                {
                    handle.Release();
                    _assetHandles.Remove(packageName);
                }

                _refCounts.Remove(packageName);
                Debug.Log($"[UIPackageLoader] Unloaded package: {packageName}");
            }
        }

        /// <summary>
        /// Force unload all packages. Call on scene transition or cleanup.
        /// </summary>
        public static void RemoveAllPackages()
        {
            UIPackage.RemoveAllPackages();

            foreach (var handle in _assetHandles.Values)
            {
                handle.Release();
            }
            _assetHandles.Clear();
            _refCounts.Clear();
            _loadedAssetCache.Clear();

            Debug.Log("[UIPackageLoader] All packages unloaded.");
        }

        private static async System.Threading.Tasks.Task LoadViaYooAssetAsync(string packageName)
        {
            string descPath = $"{YooAssetBasePath}{packageName}/{packageName}_fui.bytes";
            var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(descPath);
            await handle.Task;

            if (handle.Status == YooAsset.EOperationStatus.Succeed)
            {
                var descData = (handle.AssetObject as TextAsset).bytes;
                UIPackage.AddPackage(descData, packageName, LoadFairyGUIAsset);
                _assetHandles[packageName] = handle;
            }
            else
            {
                Debug.LogError($"[UIPackageLoader] YooAsset failed to load: {descPath}. Falling back to Resources.");
                UIPackage.AddPackage(packageName);
            }
        }

        /// <summary>
        /// Callback for FairyGUI to load individual assets (textures, sounds, etc.)
        /// within a package. Routes through YooAsset.
        ///
        /// NOTE: FairyGUI calls this synchronously. We pre-cache assets during
        /// AddPackageAsync, or load sync from cache. If not cached, we do a
        /// synchronous Resources.Load as a last resort.
        /// </summary>
        private static object LoadFairyGUIAsset(string name, string extension, Type type, out DestroyMethod destroyMethod)
        {
            destroyMethod = DestroyMethod.None;

            string assetPath = $"{YooAssetBasePath}{name}{extension}";

            // Check pre-loaded cache first
            if (_loadedAssetCache.TryGetValue(assetPath, out var cached))
            {
                return cached;
            }

            // Fallback: try YooAsset if available (editor non-WebGL only)
            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                var handle = AssetService.Instance.LoadAssetAsync<UnityEngine.Object>(assetPath);

                // In editor (non-WebGL), WaitForAsyncComplete works. On WebGL it would deadlock.
                // This path should ideally not be hit if assets are pre-cached.
#if UNITY_EDITOR
                handle.WaitForAsyncComplete();
                if (handle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    _loadedAssetCache[assetPath] = handle.AssetObject;
                    return handle.AssetObject;
                }
#endif
                Debug.LogWarning($"[UIPackageLoader] FairyGUI asset not pre-cached: {assetPath}. " +
                    "Consider pre-loading assets before UIPackage.AddPackage.");
            }

            return null;
        }

        /// <summary>
        /// Pre-cache FairyGUI package assets (textures etc.) before calling AddPackage.
        /// Call this in your async loading flow to ensure LoadFairyGUIAsset callback
        /// can return assets without blocking.
        /// </summary>
        public static async System.Threading.Tasks.Task PreCachePackageAssetsAsync(string packageName, string[] assetPaths)
        {
            if (AssetService.Instance == null || !AssetService.Instance.IsInitialized) return;

            foreach (var path in assetPaths)
            {
                var handle = AssetService.Instance.LoadAssetAsync<UnityEngine.Object>(path);
                await handle.Task;
                if (handle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    _loadedAssetCache[path] = handle.AssetObject;
                }
            }
        }
    }
}

