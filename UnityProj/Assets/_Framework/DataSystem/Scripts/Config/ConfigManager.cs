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
    /// In the Editor, human-readable JSON copies exist under Resources/ConfigData/ for inspection only.
    ///
    /// Runtime loading: Binary .bytes files via YooAsset (primary) or Resources (fallback).
    /// Editor inspection: JSON files in Resources/ConfigData/ are read-only reference copies for designers.
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
        public static string YooAssetConfigPath = "Assets/_Game/ConfigData/";

        private static cfg.Tables _tables;

        /// <summary>
        /// Luban-generated Tables instance. Access individual tables via Tables.TbItem, Tables.TbGlobalConst, etc.
        /// Null until InitializeAsync() or Initialize() completes successfully.
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
        /// Loads binary .bytes files via YooAsset (primary) or Resources (fallback),
        /// then constructs Luban Tables with ByteBuf deserialization.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_initialized) return;

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
                    Debug.LogError($"[ConfigManager] FATAL: Failed to load config '{name}'. Tables cannot be initialized.");
                    return;
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
            GameLog.Log("[ConfigManager] Config tables initialized (binary).");
        }

        /// <summary>
        /// Synchronous Initialize for backward compatibility (editor-only or Resources.Load fallback).
        /// Loads .bytes from Resources/ConfigData/. For WebGL builds, use InitializeAsync().
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _tables = new cfg.Tables(name =>
            {
                var bytes = LoadConfigBytesSync(name);
                if (bytes == null)
                    throw new System.Exception($"[ConfigManager] Failed to load config '{name}' from Resources.");
                return new ByteBuf(bytes);
            });

            _initialized = true;
            GameLog.Log("[ConfigManager] Config tables initialized (sync/Resources fallback, binary).");
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
        /// Load a config .bytes file asynchronously. WebGL-safe.
        /// Uses YooAsset when available, Resources.Load otherwise.
        /// </summary>
        public static async Task<byte[]> LoadConfigBytesAsync(string fileName)
        {
            if (!IsValidConfigFileName(fileName))
            {
                Debug.LogError($"[ConfigManager] SEC: Rejected invalid config file name: '{fileName}'.");
                return null;
            }

            byte[] bytes = null;

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                try
                {
                    string path = $"{YooAssetConfigPath}{fileName}.bytes";
                    var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(path);
                    await handle.Task;

                    if (handle.Status == YooAsset.EOperationStatus.Succeed)
                    {
                        bytes = (handle.AssetObject as TextAsset).bytes;
                        handle.Release();
                    }
                    else
                    {
                        GameLog.LogWarning($"[ConfigManager] YooAsset load failed for {path}, falling back to Resources.");
                    }
                }
                catch (System.Exception ex)
                {
                    GameLog.LogWarning($"[ConfigManager] YooAsset exception: {ex.Message}. Falling back to Resources.");
                }
            }

            if (bytes == null)
            {
                bytes = LoadConfigBytesSync(fileName);
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
        /// Synchronous fallback loader via Resources.Load.
        /// Loads .bytes from Resources/ConfigData/.
        /// </summary>
        public static byte[] LoadConfigBytesSync(string fileName)
        {
            if (!IsValidConfigFileName(fileName))
            {
                Debug.LogError($"[ConfigManager] SEC: Rejected invalid config file name: '{fileName}'.");
                return null;
            }

            var textAsset = Resources.Load<TextAsset>($"ConfigData/{fileName}");
            if (textAsset != null)
                return textAsset.bytes;

            Debug.LogError($"[ConfigManager] Failed to load config: {fileName}");
            return null;
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

        /// <summary>
        /// Force reload all config tables (sync fallback for editor).
        /// </summary>
        public static void Reload()
        {
            _initialized = false;
            _tables = null;
            Initialize();
        }
    }
}
