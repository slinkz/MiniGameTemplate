#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Enforces texture import standards for WeChat Mini Game.
    /// Runs automatically on every texture import.
    ///
    /// Rules:
    /// - Max texture size: 1024 (mini game budget constraint)
    /// - Normal maps: auto-detected by _N suffix
    /// - UI textures (under UI/): disable mipmaps
    /// - Read/Write: always disabled (saves memory)
    /// - Compression: ASTC for mobile, DXT for standalone
    ///
    /// To customize rules, modify the constants below.
    /// </summary>
    public class TextureImportEnforcer : AssetPostprocessor
    {
        // === Configurable Rules ===
        private const int MAX_TEXTURE_SIZE = 1024;
        private const string NORMAL_SUFFIX = "_N";
        private const string UI_PATH_MARKER = "/UI/";
        private const string FAIRY_GUI_EXPORT_MARKER = "FairyGUI_Export";

        void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            string path = assetPath;
            bool changed = false;

            // Skip third-party / submodule assets
            if (path.Contains("ThirdParty/") || path.Contains("FairyGUI/Scripts/"))
                return;

            // --- Rule: Max resolution budget ---
            if (importer.maxTextureSize > MAX_TEXTURE_SIZE)
            {
                importer.maxTextureSize = MAX_TEXTURE_SIZE;
                Debug.LogWarning($"[TextureEnforcer] Clamped '{path}' to {MAX_TEXTURE_SIZE}px max.");
                changed = true;
            }

            // --- Rule: Normal map detection by suffix ---
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith(NORMAL_SUFFIX) && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                Debug.LogWarning($"[TextureEnforcer] Set '{path}' to NormalMap (detected '_N' suffix).");
                changed = true;
            }

            // --- Rule: UI textures — disable mipmaps ---
            if (path.Contains(UI_PATH_MARKER) || path.Contains(FAIRY_GUI_EXPORT_MARKER))
            {
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (importer.textureType != TextureImporterType.Sprite &&
                    importer.textureType != TextureImporterType.GUI)
                {
                    // Don't override if already set correctly
                }
            }

            // --- Rule: Disable Read/Write (saves memory) ---
            if (importer.isReadable)
            {
                importer.isReadable = false;
                Debug.LogWarning($"[TextureEnforcer] Disabled Read/Write on '{path}' (memory optimization).");
                changed = true;
            }

            // --- Platform-specific compression ---
            SetPlatformCompression(importer, "WebGL");
            SetPlatformCompression(importer, "Android");
            SetPlatformCompression(importer, "iPhone");

            if (changed)
            {
                Debug.Log($"[TextureEnforcer] Applied import rules to: {path}");
            }
        }

        private void SetPlatformCompression(TextureImporter importer, string platform)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            if (!settings.overridden)
            {
                settings.overridden = true;

                bool isNormalMap = importer.textureType == TextureImporterType.NormalMap;

                switch (platform)
                {
                    case "WebGL":
                    case "Android":
                        settings.format = isNormalMap
                            ? TextureImporterFormat.ASTC_4x4
                            : TextureImporterFormat.ASTC_6x6;
                        break;
                    case "iPhone":
                        settings.format = isNormalMap
                            ? TextureImporterFormat.ASTC_4x4
                            : TextureImporterFormat.ASTC_6x6;
                        break;
                }

                settings.maxTextureSize = MAX_TEXTURE_SIZE;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }

    /// <summary>
    /// Enforces audio import standards for WeChat Mini Game.
    /// Keeps audio files small and compatible.
    ///
    /// Short SFX clips (< 3s) are forced to mono via OnPostprocessAudio.
    /// A static guard set prevents recursive reimport when AssetDatabase.ImportAsset
    /// is called from within the postprocessor.
    /// </summary>
    public class AudioImportEnforcer : AssetPostprocessor
    {
        private const float SHORT_CLIP_THRESHOLD = 3f; // seconds

        /// <summary>
        /// Guard set to prevent recursive reimport when we trigger AssetDatabase.ImportAsset
        /// from within OnPostprocessAudio.
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> _reimportingPaths
            = new System.Collections.Generic.HashSet<string>();

        void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            string path = assetPath;

            // Skip third-party
            if (path.Contains("ThirdParty/")) return;

            // Skip if we're in a recursive reimport triggered by ourselves
            if (_reimportingPaths.Contains(path)) return;

            // WebGL / mini game audio settings
            if (!importer.ContainsSampleSettingsOverride("WebGL"))
            {
                var webglSettings = importer.GetOverrideSampleSettings("WebGL");
                webglSettings.loadType = AudioClipLoadType.CompressedInMemory;
                webglSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                webglSettings.quality = 0.5f; // 50% quality — good balance for mini games
                webglSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                importer.SetOverrideSampleSettings("WebGL", webglSettings);

                Debug.Log($"[AudioEnforcer] Applied WebGL audio settings to: {path}");
            }
        }

        void OnPostprocessAudio(AudioClip clip)
        {
            string path = assetPath;

            // Skip third-party
            if (path.Contains("ThirdParty/")) return;

            // Skip if we're already reimporting this asset (prevents recursion)
            if (_reimportingPaths.Contains(path)) return;

            // Force mono for short SFX clips to save memory
            if (clip.length < SHORT_CLIP_THRESHOLD && clip.channels > 1)
            {
                var importer = (AudioImporter)assetImporter;
                if (!importer.forceToMono)
                {
                    importer.forceToMono = true;
                    Debug.LogWarning($"[AudioEnforcer] Forced mono on short clip '{path}' ({clip.length:F1}s, was {clip.channels}ch).");

                    // Trigger reimport with guard to prevent infinite recursion
                    _reimportingPaths.Add(path);
                    try
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    finally
                    {
                        _reimportingPaths.Remove(path);
                    }
                }
            }
        }
    }
}
#endif
