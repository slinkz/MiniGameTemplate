using UnityEngine;
using MiniGameTemplate.Asset;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// Loads Luban-generated config data at runtime.
    /// Generated C# table classes live in the Generated/ subfolder.
    ///
    /// When AssetService is initialized, loads data via YooAsset.
    /// Otherwise falls back to Resources.Load from Resources/ConfigData/.
    /// </summary>
    public static class ConfigManager
    {
        private static bool _initialized;

        /// <summary>
        /// Base path for config data when loading via YooAsset.
        /// </summary>
        public static string YooAssetConfigPath = "Assets/ConfigData/";

        // TODO: Replace with actual Luban-generated Tables class after running gen_config
        // private static cfg.Tables _tables;
        // public static cfg.Tables Tables => _tables;

        /// <summary>
        /// Initialize config tables. Call once during game bootstrap.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // TODO: Uncomment after Luban generates code:
            // _tables = new cfg.Tables(file => LoadConfigJson(file));

            _initialized = true;
            Debug.Log("[ConfigManager] Config tables initialized.");
        }

        /// <summary>
        /// Load a config JSON file by name. Uses YooAsset when available,
        /// Resources.Load otherwise.
        /// </summary>
        public static string LoadConfigText(string fileName)
        {
            if (AssetService.Instance != null && AssetService.Instance.IsInitialized)
            {
                string path = $"{YooAssetConfigPath}{fileName}.json";
                var handle = AssetService.Instance.LoadAssetAsync<TextAsset>(path);
                handle.WaitForAsyncComplete();

                if (handle.Status == YooAsset.EOperationStatus.Succeed)
                {
                    var text = (handle.AssetObject as TextAsset).text;
                    handle.Release();
                    return text;
                }

                Debug.LogWarning($"[ConfigManager] YooAsset load failed for {path}, falling back to Resources.");
            }

            // Fallback: Resources.Load
            var textAsset = Resources.Load<TextAsset>($"ConfigData/{fileName}");
            if (textAsset != null)
                return textAsset.text;

            Debug.LogError($"[ConfigManager] Failed to load config: {fileName}");
            return null;
        }

        /// <summary>
        /// Force reload all config tables (e.g., after hot-reload in editor).
        /// </summary>
        public static void Reload()
        {
            _initialized = false;
            Initialize();
        }
    }
}
