using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        // Guard against concurrent AddPackageAsync calls for the same package
        private static readonly HashSet<string> _loading = new HashSet<string>();

        // ── 设计约定 ──
        // FairyGUI 编辑器中包名（Package Name）= 导出文件名前缀。
        // 例：包名 "SG_Battle" → 导出 "SG_Battle_fui.bytes" + "SG_Battle_atlas0.png"。
        // UIPackageLoader 直接用 packageName 拼文件路径，零映射零配置。
        // 如果出现 "package not found"，请检查 FairyGUI 编辑器的发布设置（File Name）
        // 是否与包名一致——不要在代码里加映射 hack。

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _refCounts.Clear();
            _assetHandles.Clear();
            _loadedAssetCache.Clear();
            _loading.Clear();
        }

        /// <summary>
        /// The base path prefix for FairyGUI package assets when loading via YooAsset.
        /// Default: "Assets/FairyGUI_Export/" — override if your export path differs.
        /// </summary>
        public static string YooAssetBasePath = "Assets/_Game/FairyGUI_Export/";

        /// <summary>
        /// Load a FairyGUI package asynchronously via YooAsset.
        /// This is the ONLY loading path — no Resources.Load fallback.
        /// AssetService must be initialized before calling this method.
        ///
        /// binderAction is REQUIRED — every package load must pair with its Binder.
        /// This eliminates the need for a centralized RegisterBinder() call site.
        /// </summary>
        /// <param name="packageName">FairyGUI package name (must match publish file name)</param>
        /// <param name="binderAction">The XXXBinder.BindAll method for this package.
        /// Will be registered and activated (idempotent) before package loading.</param>
        /// <exception cref="InvalidOperationException">Thrown when AssetService is not initialized.</exception>
        public static async Task AddPackageAsync(string packageName, Action binderAction)
        {
            // Register + activate binder immediately (idempotent)
            UIManager.RegisterBinder(packageName, binderAction);
            UIManager.ActivateBinder(packageName);

            if (_refCounts.ContainsKey(packageName))
            {
                _refCounts[packageName]++;
                return;
            }

            // Guard: if another async call is already loading this package, skip duplicate load
            if (!_loading.Add(packageName))
            {
                GameLog.LogWarning($"[UIPackageLoader] Package '{packageName}' is already being loaded by another call. Skipping.");
                return;
            }

            if (AssetService.Instance == null || !AssetService.Instance.IsInitialized)
            {
                _loading.Remove(packageName);
                throw new InvalidOperationException(
                    $"[UIPackageLoader] AssetService not initialized. Cannot load package '{packageName}'. " +
                    "Ensure GameBootstrapper has completed AssetService initialization before opening UI.");
            }

            try
            {
                await LoadViaYooAssetAsync(packageName);
                _refCounts[packageName] = 1;
                GameLog.Log($"[UIPackageLoader] Loaded package: {packageName}");
            }
            finally
            {
                _loading.Remove(packageName);
            }
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

        private static async Task LoadViaYooAssetAsync(string packageName)
        {
            // 约定：packageName 直接作为文件名前缀（包名 = 导出文件名前缀）
            string descPath = $"{YooAssetBasePath}{packageName}_fui.bytes";
            var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(descPath);
            await handle.Task;

            if (handle.Status != YooAsset.EOperationStatus.Succeed)
            {
                throw new InvalidOperationException(
                    $"[UIPackageLoader] Failed to load FairyGUI package descriptor: {descPath}. " +
                    $"Status: {handle.Status}. Package='{packageName}'. " +
                    "Ensure FairyGUI publish file name matches the package name " +
                    "(e.g. package 'SG_Battle' → export 'SG_Battle_fui.bytes').");
            }

            var textAsset = handle.AssetObject as TextAsset;
            if (textAsset == null)
            {
                throw new InvalidOperationException(
                    $"[UIPackageLoader] Package descriptor is not a TextAsset: {descPath} " +
                    $"(actual type: {handle.AssetObject?.GetType().Name ?? "null"}). " +
                    "Check that the file is a valid FairyGUI _fui.bytes descriptor.");
            }

            var descData = textAsset.bytes;
            UIPackage.AddPackage(descData, packageName, LoadFairyGUIAsset);
            _assetHandles[packageName] = handle;
        }

        /// <summary>
        /// Callback for FairyGUI to load individual assets (textures, sounds, etc.)
        /// within a package. Routes through YooAsset.
        ///
        /// NOTE: FairyGUI calls this synchronously. We pre-cache assets during
        /// AddPackageAsync, or load sync from cache. In editor, WaitForAsyncComplete
        /// is used as last resort. On WebGL, assets MUST be pre-cached — cache miss
        /// logs an error to catch the problem early.
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
                GameLog.LogWarning($"[UIPackageLoader] FairyGUI asset not pre-cached: {assetPath}. " +
                    "Consider pre-loading assets before UIPackage.AddPackage.");
#else
                // Production: pre-cache is mandatory. Cache miss = missing texture on screen.
                Debug.LogError($"[UIPackageLoader] CRITICAL: FairyGUI asset not pre-cached: {assetPath}. " +
                    "On WebGL/WeChat, all FairyGUI package assets MUST be pre-cached via " +
                    "PreCachePackageAssetsAsync() before AddPackageAsync().");
#endif
            }

            return null;
        }

        /// <summary>
        /// Pre-cache FairyGUI package assets (textures etc.) before calling AddPackage.
        /// Call this in your async loading flow to ensure LoadFairyGUIAsset callback
        /// can return assets without blocking.
        /// </summary>
        public static async Task PreCachePackageAssetsAsync(string packageName, string[] assetPaths)
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
