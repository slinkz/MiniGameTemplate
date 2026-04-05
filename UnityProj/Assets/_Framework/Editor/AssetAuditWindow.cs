#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Asset audit tool — scans for oversized textures, unused assets in Resources,
    /// and other budget violations relevant to WeChat Mini Game.
    ///
    /// Access via: Tools → MiniGame Template → Validate → Asset Audit
    /// </summary>
    public class AssetAuditWindow : EditorWindow
    {
        [MenuItem("Tools/MiniGame Template/Validate/Asset Audit", false, 210)]
        public static void ShowWindow() => GetWindow<AssetAuditWindow>("Asset Audit");

        private Vector2 _scrollPos;
        private List<AuditEntry> _results = new List<AuditEntry>();
        private bool _hasRun;
        private int _errorCount;
        private int _warningCount;

        private enum Severity { Error, Warning, Info }

        private struct AuditEntry
        {
            public string Path;
            public string Message;
            public Severity Severity;
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Audit — Mini Game Budget Check", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans project for oversized textures, uncompressed assets, Read/Write enabled textures, " +
                "and assets in Resources/ that shouldn't be there.",
                MessageType.Info);

            GUILayout.Space(5);

            if (GUILayout.Button("Run Full Audit", GUILayout.Height(28)))
            {
                _results.Clear();
                _errorCount = 0;
                _warningCount = 0;
                RunAudit();
                _hasRun = true;
            }

            if (!_hasRun) return;

            GUILayout.Space(5);
            var msgType = _errorCount > 0 ? MessageType.Error
                : _warningCount > 0 ? MessageType.Warning
                : MessageType.Info;
            EditorGUILayout.HelpBox(
                $"Found {_errorCount} error(s), {_warningCount} warning(s), {_results.Count} total entries.",
                msgType);

            GUILayout.Space(3);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var entry in _results)
            {
                EditorGUILayout.BeginHorizontal();

                var icon = entry.Severity switch
                {
                    Severity.Error => "console.erroricon.sml",
                    Severity.Warning => "console.warnicon.sml",
                    _ => "console.infoicon.sml"
                };
                GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20), GUILayout.Height(18));
                EditorGUILayout.LabelField(entry.Message, EditorStyles.wordWrappedMiniLabel);

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(entry.Path);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunAudit()
        {
            AuditTextures();
            AuditAudio();
            AuditResources();
        }

        private void AuditTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D");
            int total = guids.Length;

            for (int i = 0; i < total; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Contains("ThirdParty/") || path.Contains("Packages/")) continue;

                EditorUtility.DisplayProgressBar("Auditing Textures...", path, (float)i / total);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // Check: oversized
                if (importer.maxTextureSize > 1024)
                {
                    AddEntry(path, $"Oversized texture ({importer.maxTextureSize}px) — max 1024 for mini games", Severity.Warning);
                }

                // Check: Read/Write enabled
                if (importer.isReadable)
                {
                    AddEntry(path, "Read/Write enabled — wastes 2x memory", Severity.Warning);
                }

                // Check: uncompressed
                var webglSettings = importer.GetPlatformTextureSettings("WebGL");
                if (webglSettings.overridden && webglSettings.format == TextureImporterFormat.RGBA32)
                {
                    AddEntry(path, "Uncompressed WebGL texture (RGBA32) — use ASTC", Severity.Error);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private void AuditAudio()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip");
            int total = guids.Length;

            for (int i = 0; i < total; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Contains("ThirdParty/") || path.Contains("Packages/")) continue;

                EditorUtility.DisplayProgressBar("Auditing Audio...", path, (float)i / total);

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                // Check: no WebGL override
                if (!importer.ContainsSampleSettingsOverride("WebGL"))
                {
                    AddEntry(path, "No WebGL audio override — may use unoptimized settings", Severity.Warning);
                }

                // Check: WAV files should be compressed
                if (path.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase))
                {
                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > 500 * 1024)
                    {
                        AddEntry(path, $"Large WAV ({fi.Length / 1024}KB) — consider converting to OGG", Severity.Warning);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private void AuditResources()
        {
            // Check for large files in Resources/
            var resourcesDir = "Assets/Resources";
            if (!Directory.Exists(resourcesDir)) return;

            var files = Directory.GetFiles(resourcesDir, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta"));

            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.Length > 1024 * 1024) // > 1MB
                {
                    var assetPath = file.Replace("\\", "/");
                    AddEntry(assetPath, $"Large file in Resources/ ({fi.Length / 1024}KB) — consider YooAsset instead", Severity.Warning);
                }
            }
        }

        private void AddEntry(string path, string message, Severity severity)
        {
            _results.Add(new AuditEntry { Path = path, Message = $"[{path}] {message}", Severity = severity });
            if (severity == Severity.Error) _errorCount++;
            else if (severity == Severity.Warning) _warningCount++;
        }
    }
}
#endif
