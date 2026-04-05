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
    /// When AssetService is initialized, loads FairyGUI packages via YooAsset.
    /// Otherwise falls back to Resources.Load (for editor quick-iteration).
    /// </summary>
    public static class UIPackageLoader
    {
        private static readonly Dictionary<string, int> _refCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, YooAsset.AssetHandle> _assetHandles = new Dictionary<string, YooAsset.AssetHandle>();

        /// <summary>
        /// The base path prefix for FairyGUI package assets when loading via YooAsset.
        /// Default: "Assets/FairyGUI_Export/" — override if your export path differs.
        /// </summary>
        public static string YooAssetBasePath = "Assets/FairyGUI_Export/";

        /// <summary>
        /// Load a FairyGUI package. Uses YooAsset when available, Resources.Load otherwise.
        /// Increments reference count.
        /// </summary>
        public static void AddPackage(string packageName)
        {
            if (_refCounts.ContainsKey(packageName))
            {
                _refCounts[packageName]++;
                return;
            }

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                LoadViaYooAsset(packageName);
            }
            else
            {
                // Fallback: FairyGUI default Resources.Load behavior
                UIPackage.AddPackage(packageName);
            }

            _refCounts[packageName] = 1;
            Debug.Log($"[UIPackageLoader] Loaded package: {packageName}");
        }

        /// <summary>
        /// Load a FairyGUI package asynchronously via YooAsset.
        /// Use this for large UI packages to avoid frame spikes.
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

            Debug.Log("[UIPackageLoader] All packages unloaded.");
        }

        private static void LoadViaYooAsset(string packageName)
        {
            // FairyGUI package consists of a _fui.bytes (desc) file.
            // YooAsset loads the raw binary, then we feed it to FairyGUI.
            string descPath = $"{YooAssetBasePath}{packageName}/{packageName}_fui.bytes";
            var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(descPath);
            handle.WaitForAsyncComplete(); // Sync load — use AddPackageAsync for async

            if (handle.Status == YooAsset.EOperationStatus.Succeed)
            {
                var descData = (handle.AssetObject as TextAsset).bytes;
                UIPackage.AddPackage(descData, packageName, LoadFairyGUIAsset);
                _assetHandles[packageName] = handle;
            }
            else
            {
                Debug.LogError($"[UIPackageLoader] YooAsset failed to load: {descPath}. Error: {handle.LastError}");
                // Fallback to Resources
                UIPackage.AddPackage(packageName);
            }
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
        /// </summary>
        private static object LoadFairyGUIAsset(string name, string extension, Type type, out DestroyMethod destroyMethod)
        {
            destroyMethod = DestroyMethod.None;

            if (AssetService.Instance == null || !AssetService.Instance.IsInitialized)
                return null;

            // YooAsset will handle the actual loading
            string assetPath = $"{YooAssetBasePath}{name}{extension}";
            var handle = AssetService.Instance.LoadAssetAsync<UnityEngine.Object>(assetPath);
            handle.WaitForAsyncComplete();

            if (handle.Status == YooAsset.EOperationStatus.Succeed)
            {
                // Don't destroy — YooAsset manages the lifecycle
                destroyMethod = DestroyMethod.None;
                return handle.AssetObject;
            }

            Debug.LogWarning($"[UIPackageLoader] Failed to load FairyGUI asset: {assetPath}");
            return null;
        }
    }
}
