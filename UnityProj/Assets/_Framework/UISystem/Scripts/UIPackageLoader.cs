using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using MiniGameTemplate.Asset;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.UI
{
    /// <summary>
    /// Manages FairyGUI package loading and unloading.
    /// Packages are reference-counted to avoid premature unloading.
    ///
    /// ALL loading goes through YooAsset — no Resources.Load fallback.
    /// AssetService must be initialized before any package loading.
    /// In editor, YooAsset EditorSimulate mode handles this transparently.
    /// </summary>
    public static class UIPackageLoader
    {
        private static readonly Dictionary<string, int> _refCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, YooAsset.AssetHandle> _assetHandles = new Dictionary<string, YooAsset.AssetHandle>();
        // Cache for assets loaded during async package add — used by LoadFairyGUIAsset callback
        private static readonly Dictionary<string, UnityEngine.Object> _loadedAssetCache = new Dictionary<string, UnityEngine.Object>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _refCounts.Clear();
            _assetHandles.Clear();
            _loadedAssetCache.Clear();
        }

        /// <summary>
        /// The base path prefix for FairyGUI package assets when loading via YooAsset.
        /// Default: "Assets/FairyGUI_Export/" — override if your export path differs.
        /// </summary>
        public static string YooAssetBasePath = "Assets/FairyGUI_Export/";

        /// <summary>
        /// Load a FairyGUI package asynchronously via YooAsset.
        /// This is the ONLY loading path — no Resources.Load fallback.
        /// AssetService must be initialized before calling this method.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when AssetService is not initialized.</exception>
        public static async System.Threading.Tasks.Task AddPackageAsync(string packageName)
        {
            if (_refCounts.ContainsKey(packageName))
            {
                _refCounts[packageName]++;
                return;
            }

            if (AssetService.Instance == null || !AssetService.Instance.IsInitialized)
            {
                throw new InvalidOperationException(
                    $"[UIPackageLoader] AssetService not initialized. Cannot load package '{packageName}'. " +
                    "Ensure GameBootstrapper has completed AssetService initialization before opening UI.");
            }

            await LoadViaYooAssetAsync(packageName);

            _refCounts[packageName] = 1;
            GameLog.Log($"[UIPackageLoader] Loaded package: {packageName}");
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
                GameLog.Log($"[UIPackageLoader] Unloaded package: {packageName}");
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

            GameLog.Log("[UIPackageLoader] All packages unloaded.");
        }

        private static async System.Threading.Tasks.Task LoadViaYooAssetAsync(string packageName)
        {
            string descPath = $"{YooAssetBasePath}{packageName}/{packageName}_fui.bytes";
            var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(descPath);
            await handle.Task;

            if (handle.Status != YooAsset.EOperationStatus.Succeed)
            {
                throw new InvalidOperationException(
                    $"[UIPackageLoader] Failed to load FairyGUI package descriptor: {descPath}. " +
                    $"Status: {handle.Status}. Ensure the package is exported and included in YooAsset collection.");
            }

            var descData = (handle.AssetObject as TextAsset).bytes;
            UIPackage.AddPackage(descData, packageName, LoadFairyGUIAsset);
            _assetHandles[packageName] = handle;
        }

        /// <summary>
        /// Callback for FairyGUI to load individual assets (textures, sounds, etc.)
        /// within a package. Routes through YooAsset.
        ///
        /// NOTE: FairyGUI calls this synchronously. We pre-cache assets during
        /// AddPackageAsync, or load sync from cache. In editor, WaitForAsyncComplete
        /// is used as last resort. On WebGL, assets MUST be pre-cached.
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

            // Fallback: try YooAsset sync load (editor only — NOT WebGL-safe)
            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                var handle = AssetService.Instance.LoadAssetAsync<UnityEngine.Object>(assetPath);

#if UNITY_EDITOR
                // In editor (non-WebGL), WaitForAsyncComplete works. On WebGL it would deadlock.
                handle.WaitForAsyncComplete();
                if (handle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    _loadedAssetCache[assetPath] = handle.AssetObject;
                    return handle.AssetObject;
                }
#endif
                GameLog.LogWarning($"[UIPackageLoader] FairyGUI asset not pre-cached: {assetPath}. " +
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
            if (AssetService.Instance == null || !AssetService.Instance.IsInitialized)
            {
                throw new InvalidOperationException(
                    "[UIPackageLoader] AssetService not initialized. Cannot pre-cache assets.");
            }

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
