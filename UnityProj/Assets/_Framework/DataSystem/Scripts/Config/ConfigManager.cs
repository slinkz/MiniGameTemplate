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
    ///
    /// Security:
    /// - File names are validated against path traversal attacks (../ sequences).
    /// - An optional integrity verification hook is provided for production builds.
    /// </summary>
    public static class ConfigManager
    {
        private static bool _initialized;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _initialized = false;
            _tables = null;
        }

        /// <summary>
        /// Base path for config data when loading via YooAsset.
        /// JSON files are placed under Assets/_Game/ConfigData/ and collected by YooAsset.
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
        /// Example: (fileName, content) => verify content hash against a known manifest.
        /// Return true if valid, false to reject.
        /// </summary>
        public static System.Func<string, string, bool> IntegrityVerifier { get; set; }

        /// <summary>
        /// Initialize config tables asynchronously. Call once during game bootstrap.
        /// Luban's Tables constructor accepts an async loader — use InitializeAsync() for WebGL safety.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_initialized) return;

            _tables = await cfg.Tables.CreateAsync(LoadConfigTextAsync);

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

            _tables = cfg.Tables.Create(LoadConfigTextSync);

            _initialized = true;
            GameLog.Log("[ConfigManager] Config tables initialized (sync/Resources fallback).");
        }

        /// <summary>
        /// SEC: Validate config file name to prevent path traversal attacks.
        /// Rejects names containing "..", "/", "\", or other dangerous patterns.
        /// </summary>
        private static bool IsValidConfigFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            // Block path traversal sequences and absolute paths
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                return false;
            // Block null bytes (classic path traversal trick)
            if (fileName.Contains("\0"))
                return false;
            return true;
        }

        /// <summary>
        /// Load a config JSON file asynchronously. WebGL-safe.
        /// Uses YooAsset when available, Resources.Load otherwise.
        /// </summary>
        public static async Task<string> LoadConfigTextAsync(string fileName)
        {
            if (!IsValidConfigFileName(fileName))
            {
                Debug.LogError($"[ConfigManager] SEC: Rejected invalid config file name: '{fileName}' (possible path traversal).");
                return null;
            }

            string content = null;

            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                try
                {
                    string path = $"{YooAssetConfigPath}{fileName}.json";
                    var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(path);
                    await handle.Task;

                    if (handle.Status == YooAsset.EOperationStatus.Succeed)
                    {
                        content = (handle.AssetObject as TextAsset).text;
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

            if (content == null)
            {
                // Fallback: Resources.Load (synchronous but safe on all platforms)
                content = LoadConfigTextSync(fileName);
            }

            // Optional integrity verification
            if (content != null && IntegrityVerifier != null && !IntegrityVerifier(fileName, content))
            {
                Debug.LogError($"[ConfigManager] SEC: Integrity check failed for config '{fileName}'. Data rejected.");
                return null;
            }

            return content;
        }

        /// <summary>
        /// Synchronous fallback loader via Resources.Load.
        /// Safe on all platforms but only loads from Resources/ConfigData/.
        /// </summary>
        public static string LoadConfigTextSync(string fileName)
        {
            if (!IsValidConfigFileName(fileName))
            {
                Debug.LogError($"[ConfigManager] SEC: Rejected invalid config file name: '{fileName}' (possible path traversal).");
                return null;
            }

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
