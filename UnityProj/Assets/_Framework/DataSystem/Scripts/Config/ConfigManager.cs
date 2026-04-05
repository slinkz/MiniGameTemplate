using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Asset;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// Loads Luban-generated config data at runtime.
    /// Generated C# table classes live in the Generated/ subfolder.
    ///
    /// All loading paths are fully async to avoid WebGL/IL2CPP deadlocks.
    /// On WebGL (WeChat Mini Game), WaitForAsyncComplete() will deadlock the single thread.
    /// </summary>
    public static class ConfigManager
    {
        private static bool _initialized;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _initialized = false;

        /// <summary>
        /// Base path for config data when loading via YooAsset.
        /// </summary>
        public static string YooAssetConfigPath = "Assets/ConfigData/";

        // TODO: Replace with actual Luban-generated Tables class after running gen_config
        // private static cfg.Tables _tables;
        // public static cfg.Tables Tables => _tables;

        /// <summary>
        /// Initialize config tables asynchronously. Call once during game bootstrap.
        /// Luban's Tables constructor accepts an async loader — use InitializeAsync() for WebGL safety.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_initialized) return;

            // TODO: Uncomment after Luban generates code:
            // _tables = new cfg.Tables(file => await LoadConfigTextAsync(file));
            // For Luban async loader pattern:
            // _tables = await cfg.Tables.CreateAsync(LoadConfigTextAsync);

            _initialized = true;
            GameLog.Log("[ConfigManager] Config tables initialized.");
        }

        /// <summary>
        /// Synchronous Initialize for backward compatibility (editor-only or Resources.Load fallback).
        /// WARNING: This only works when NOT using YooAsset path. For WebGL builds, use InitializeAsync().
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // Sync path can only use Resources.Load — safe on all platforms
            // TODO: Uncomment after Luban generates code:
            // _tables = new cfg.Tables(file => LoadConfigTextSync(file));

            _initialized = true;
            GameLog.Log("[ConfigManager] Config tables initialized (sync/Resources fallback).");
        }

        /// <summary>
        /// Load a config JSON file asynchronously. WebGL-safe.
        /// Uses YooAsset when available, Resources.Load otherwise.
        /// </summary>
        public static async Task<string> LoadConfigTextAsync(string fileName)
        {
            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                string path = $"{YooAssetConfigPath}{fileName}.json";
                var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(path);
                await handle.Task;

                if (handle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    var text = (handle.AssetObject as TextAsset).text;
                    handle.Release();
                    return text;
                }

                GameLog.LogWarning($"[ConfigManager] YooAsset load failed for {path}, falling back to Resources.");
            }

            // Fallback: Resources.Load (synchronous but safe on all platforms)
            return LoadConfigTextSync(fileName);
        }

        /// <summary>
        /// Synchronous fallback loader via Resources.Load.
        /// Safe on all platforms but only loads from Resources/ConfigData/.
        /// </summary>
        public static string LoadConfigTextSync(string fileName)
        {
            var textAsset = Resources.Load<TextAsset>($"ConfigData/{fileName}");
            if (textAsset != null)
                return textAsset.text;

            Debug.LogError($"[ConfigManager] Failed to load config: {fileName}");
            return null;
        }

        /// <summary>
        /// Force reload all config tables asynchronously.
        /// </summary>
        public static async Task ReloadAsync()
        {
            _initialized = false;
            await InitializeAsync();
        }

        /// <summary>
        /// Force reload all config tables (sync fallback for editor).
        /// </summary>
        public static void Reload()
        {
            _initialized = false;
            Initialize();
        }
    }
}
