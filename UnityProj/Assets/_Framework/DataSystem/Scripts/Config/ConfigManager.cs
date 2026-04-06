using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Luban;
using MiniGameTemplate.Asset;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// Loads Luban-generated config data at runtime using binary format (.bytes).
    /// In the Editor, human-readable JSON copies exist under Editor/ConfigPreview/ for inspection only.
    ///
    /// Runtime loading: Binary .bytes files via YooAsset exclusively.
    /// YooAsset must be initialized before calling InitializeAsync().
    /// In Editor, YooAsset EditorSimulate mode loads directly from AssetDatabase — no bundle build needed.
    ///
    /// Security:
    /// - File names are validated against path traversal attacks.
    /// - An optional integrity verification hook is provided for production builds.
    /// </summary>
    public static class ConfigManager
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _initialized = false;
            _tables = null;
        }

        /// <summary>
        /// Base path for binary config data when loading via YooAsset.
        /// Binary .bytes files are placed under Assets/_Game/ConfigData/ and collected by YooAsset.
        /// </summary>
        public static readonly string YooAssetConfigPath = "Assets/_Game/ConfigData/";

        private static cfg.Tables _tables;

        /// <summary>
        /// Luban-generated Tables instance. Access individual tables via Tables.TbItem, Tables.TbGlobalConst, etc.
        /// Null until InitializeAsync() completes successfully.
        /// </summary>
        public static cfg.Tables Tables => _tables;

        /// <summary>
        /// Optional: Assign a delegate to verify config file integrity after loading.
        /// Example: (fileName, rawBytes) => verify hash against a known manifest.
        /// Return true if valid, false to reject.
        /// </summary>
        public static System.Func<string, byte[], bool> IntegrityVerifier { get; set; }

        /// <summary>
        /// Initialize config tables asynchronously. Call once during game bootstrap.
        /// Requires YooAsset (AssetService) to be initialized first.
        /// Loads binary .bytes files via YooAsset, then constructs Luban Tables with ByteBuf deserialization.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_initialized) return;

            // Fail-fast: YooAsset must be ready
            if (AssetService.Instance == null || !AssetService.Instance.IsInitialized)
            {
                throw new System.Exception(
                    "[ConfigManager] FATAL: AssetService is not initialized. " +
                    "ConfigManager requires YooAsset. Ensure AssetConfig is assigned on GameBootstrapper " +
                    "and AssetService.InitializeAsync() completes before calling ConfigManager.InitializeAsync().");
            }

            // Collect all table names that Tables constructor will request.
            // These must match the file names Luban generates (lowercase table names).
            var tableNames = cfg.Tables.GetTableNames();

            // Pre-load all binary data asynchronously
            var bytesCache = new Dictionary<string, byte[]>(tableNames.Length);
            foreach (var name in tableNames)
            {
                var bytes = await LoadConfigBytesAsync(name);
                if (bytes == null)
                {
                    throw new System.Exception(
                        $"[ConfigManager] FATAL: Failed to load config '{name}'. " +
                        "Check that gen_config has been run and .bytes files exist in " +
                        "Assets/_Game/ConfigData/.");
                }
                bytesCache[name] = bytes;
            }

            // Synchronous construction with pre-loaded data
            _tables = new cfg.Tables(name =>
            {
                if (bytesCache.TryGetValue(name, out var bytes))
                    return new ByteBuf(bytes);
                throw new System.Exception($"[ConfigManager] Config data not found for '{name}'.");
            });

            _initialized = true;
            GameLog.Log("[ConfigManager] Config tables initialized (binary via YooAsset).");
        }

        /// <summary>
        /// SEC: Validate config file name to prevent path traversal attacks.
        /// </summary>
        private static bool IsValidConfigFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                return false;
            if (fileName.Contains("\0"))
                return false;
            return true;
        }

        /// <summary>
        /// Load a config .bytes file asynchronously via YooAsset. WebGL-safe.
        /// </summary>
        public static async Task<byte[]> LoadConfigBytesAsync(string fileName)
        {
            if (!IsValidConfigFileName(fileName))
            {
                Debug.LogError($"[ConfigManager] SEC: Rejected invalid config file name: '{fileName}'.");
                return null;
            }

            byte[] bytes = null;
            string path = $"{YooAssetConfigPath}{fileName}.bytes";

            try
            {
                var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(path);
                await handle.Task;

                try
                {
                    if (handle.Status == YooAsset.EOperationStatus.Succeed)
                    {
                        var textAsset = handle.AssetObject as TextAsset;
                        if (textAsset != null)
                        {
                            bytes = textAsset.bytes;
                        }
                        else
                        {
                            Debug.LogError($"[ConfigManager] Asset loaded but is not TextAsset: {path}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[ConfigManager] YooAsset load failed for {path}. " +
                            "Ensure gen_config has been run and AssetBundle collector includes _Game/ConfigData/.");
                    }
                }
                finally
                {
                    handle.Release();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigManager] Exception loading config '{fileName}': {ex.Message}");
            }

            // Optional integrity verification
            if (bytes != null && IntegrityVerifier != null && !IntegrityVerifier(fileName, bytes))
            {
                Debug.LogError($"[ConfigManager] SEC: Integrity check failed for config '{fileName}'. Data rejected.");
                return null;
            }

            return bytes;
        }

        /// <summary>
        /// Force reload all config tables asynchronously.
        /// </summary>
        public static async Task ReloadAsync()
        {
            _initialized = false;
            _tables = null;
            await InitializeAsync();
        }
    }
}
