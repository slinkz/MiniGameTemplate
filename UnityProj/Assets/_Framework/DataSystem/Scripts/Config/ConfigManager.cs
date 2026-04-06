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
    /// Uses lazy deserialization: binary data is pre-loaded at startup, but each table
    /// is only deserialized on first access. This reduces startup time while keeping
    /// a synchronous access API (no async needed at call sites).
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
        private static Dictionary<string, byte[]> _bytesCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _initialized = false;
            _tables = null;
            _bytesCache = null;
        }

        /// <summary>
        /// Base path for binary config data when loading via YooAsset.
        /// Binary .bytes files are placed under Assets/_Game/ConfigData/ and collected by YooAsset.
        /// </summary>
        public static readonly string YooAssetConfigPath = "Assets/_Game/ConfigData/";

        private static cfg.Tables _tables;

        /// <summary>
        /// Luban-generated Tables instance. Access individual tables via Tables.TbItem, Tables.TbGlobalConst, etc.
        /// Tables use lazy deserialization: each table is deserialized on first property access.
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
        /// Initialize config system asynchronously. Call once during game bootstrap.
        /// Requires YooAsset (AssetService) to be initialized first.
        /// Pre-loads all binary .bytes files via YooAsset into memory, then creates a Tables
        /// instance with a lazy loader. Actual deserialization is deferred to first table access.
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

            // Collect all table names for pre-loading binary data.
            var tableNames = cfg.Tables.GetTableNames();

            // Pre-load all binary data asynchronously (I/O only, no deserialization).
            _bytesCache = new Dictionary<string, byte[]>(tableNames.Length);
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
                _bytesCache[name] = bytes;
            }

            // Create Tables with lazy loader. No deserialization happens here;
            // each table is deserialized on first property access.
            _tables = new cfg.Tables(name =>
            {
                if (_bytesCache != null && _bytesCache.TryGetValue(name, out var bytes))
                {
                    // Remove from cache after consumption to free the raw byte[] memory.
                    _bytesCache.Remove(name);
                    return new ByteBuf(bytes);
                }
                throw new System.Exception($"[ConfigManager] Config data not found for '{name}'.");
            });

            _initialized = true;
            GameLog.Log($"[ConfigManager] Config system ready (lazy deserialization, {tableNames.Length} tables pre-loaded).");
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
        /// Check whether a specific table's raw bytes have been consumed (deserialized).
        /// Returns true if the table has already been deserialized and its bytes freed.
        /// </summary>
        /// <param name="fileName">Lowercase file name as used by the loader, e.g. "tbitem".</param>
        public static bool IsTableLoaded(string fileName)
        {
            // If bytesCache exists and does NOT contain the key, the table was already consumed.
            // If bytesCache is null, ConfigManager hasn't been initialized yet.
            if (_bytesCache == null) return false;
            return !_bytesCache.ContainsKey(fileName);
        }

        /// <summary>
        /// Force reload all config tables asynchronously.
        /// Clears all cached data and re-initializes from scratch.
        /// WARNING: During reload, Tables property will be null until InitializeAsync completes.
        /// Callers must ensure no other code accesses ConfigManager.Tables while reload is in progress.
        /// </summary>
        public static async Task ReloadAsync()
        {
            _initialized = false;
            _tables = null;
            _bytesCache = null;
            await InitializeAsync();
        }
    }
}
