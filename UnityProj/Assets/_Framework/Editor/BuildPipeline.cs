#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// One-click build pipeline for WeChat Mini Game (WebGL).
    /// Automates platform switch, player settings, build, and post-build steps.
    ///
    /// Access via: Tools → MiniGame Template → Build → Build WebGL (Dev/Release)
    ///
    /// Named MiniGameBuildPipeline to avoid collision with UnityEditor.BuildPipeline.
    /// </summary>
    public static class MiniGameBuildPipeline
    {
        private const string BUILD_OUTPUT = "Build/WebGL";
        private const string MENU_ROOT = "Tools/MiniGame Template/Build/";

        [MenuItem(MENU_ROOT + "Build WebGL (Development)", false, 400)]
        public static void BuildDevelopment()
        {
            Build(isDevelopment: true);
        }

        [MenuItem(MENU_ROOT + "Build WebGL (Release)", false, 401)]
        public static void BuildRelease()
        {
            Build(isDevelopment: false);
        }

        [MenuItem(MENU_ROOT + "Validate WeChat Settings", false, 410)]
        public static void ValidateWeChatSettings()
        {
            bool allGood = true;

            if (PlayerSettings.colorSpace != ColorSpace.Gamma)
            {
                Debug.LogWarning("[BuildPipeline] ColorSpace should be Gamma for WeChat Mini Game.");
                allGood = false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.LogWarning("[BuildPipeline] Active build target is not WebGL.");
                allGood = false;
            }

#if UNITY_2021_2_OR_NEWER
            if (!PlayerSettings.gcIncremental)
            {
                Debug.LogWarning("[BuildPipeline] Incremental GC should be enabled for WeChat Mini Game.");
                allGood = false;
            }
#endif

            if (allGood)
                Debug.Log("[BuildPipeline] All WeChat Mini Game settings validated OK.");
        }

        [MenuItem(MENU_ROOT + "Open Build Folder", false, 420)]
        public static void OpenBuildFolder()
        {
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BUILD_OUTPUT));
            if (Directory.Exists(fullPath))
                EditorUtility.RevealInFinder(fullPath);
            else
                Debug.LogWarning("[BuildPipeline] Build folder does not exist yet. Run a build first.");
        }

        private static void Build(bool isDevelopment)
        {
            // 1. Ensure platform
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[BuildPipeline] Switching to WebGL platform...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            // 2. Configure PlayerSettings for WeChat Mini Game
            ConfigurePlayerSettings(isDevelopment);

            // 3. Run pre-build validation
            try
            {
                ArchitectureValidator.RunValidation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildPipeline] Pre-build validation failed: {ex.Message}");
                // Continue build — validation is advisory, not blocking
            }

            // 4. Collect scenes
            var scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildPipeline] No scenes in Build Settings! Add at least the Boot scene.");
                return;
            }

            // 5. Build
            var buildPath = Path.Combine(Application.dataPath, "..", BUILD_OUTPUT);
            Directory.CreateDirectory(buildPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = isDevelopment
                    ? BuildOptions.Development | BuildOptions.ConnectWithProfiler
                    : BuildOptions.None,
            };

            Debug.Log($"[BuildPipeline] Starting {(isDevelopment ? "Development" : "Release")} build...");
            var report = UnityEditor.BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                var elapsed = report.summary.totalTime;
                var size = report.summary.totalSize / (1024f * 1024f);
                Debug.Log($"[BuildPipeline] Build succeeded! Time: {elapsed.TotalSeconds:F1}s, Size: {size:F1}MB");
                Debug.Log($"[BuildPipeline] Output: {Path.GetFullPath(buildPath)}");

                // Post-build: remind about WeChat Mini Game conversion
                Debug.Log("[BuildPipeline] Next step: Use WeChat Minigame Unity Plugin to convert WebGL output.");
                Debug.Log("[BuildPipeline] Run: minigame-unity-sdk-cli convert --input <build-path> --output <wx-project-path>");
            }
            else
            {
                Debug.LogError($"[BuildPipeline] Build failed: {report.summary.result}. Errors: {report.summary.totalErrors}");
            }
        }

        private static void ConfigurePlayerSettings(bool isDevelopment)
        {
            // === WeChat Mini Game MANDATORY settings ===

            // Color Space: MUST be Gamma — WeChat Mini Game does not support Linear
            if (PlayerSettings.colorSpace != ColorSpace.Gamma)
            {
                Debug.Log("[BuildPipeline] Setting Color Space to Gamma (required by WeChat Mini Game).");
                PlayerSettings.colorSpace = ColorSpace.Gamma;
            }

            // Auto Graphics API — let Unity pick the best WebGL version
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);

            // WebGL memory settings optimized for WeChat Mini Game
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;

            // Compression: Disabled for WeChat — the WX plugin handles its own compression
            // Using Brotli/Gzip will double-compress and waste size
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            // Decompression fallback: must be disabled for WeChat
            PlayerSettings.WebGL.decompressionFallback = false;

            // Name output files as hashes — required for WeChat CDN cache busting
            PlayerSettings.WebGL.nameFilesAsHashes = true;

#if UNITY_2022_1_OR_NEWER
            // Debug symbols for development builds
            PlayerSettings.WebGL.debugSymbolMode = isDevelopment
                ? WebGLDebugSymbolMode.External
                : WebGLDebugSymbolMode.Off;
#endif

            // === Performance optimization settings ===

            // Strip unused engine code to minimize WASM size
            PlayerSettings.stripEngineCode = true;

            // Aggressive stripping for release — saves significant WASM size
            // Use High for release, Medium for dev (easier debugging)
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL,
                isDevelopment ? ManagedStrippingLevel.Medium : ManagedStrippingLevel.High);

            // Exception support: minimal for release, full for dev
            PlayerSettings.WebGL.exceptionSupport = isDevelopment
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            // Incremental GC — reduces GC spikes, important for smooth gameplay
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.gcIncremental = true;
#endif

            // IL2CPP code generation: faster runtime, slightly larger build
            // For WeChat, smaller is usually better — use OptimizeSize for release
#if UNITY_2022_3_OR_NEWER
            EditorUserBuildSettings.il2CppCodeGeneration = isDevelopment
                ? Il2CppCodeGeneration.OptimizeSpeed
                : Il2CppCodeGeneration.OptimizeSize;
#endif

            Debug.Log($"[BuildPipeline] PlayerSettings configured for {(isDevelopment ? "Development" : "Release")} WebGL (WeChat Mini Game).");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var enabledScenes = new System.Collections.Generic.List<string>();
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                    enabledScenes.Add(scene.path);
            }
            return enabledScenes.ToArray();
        }
    }
}
#endif
