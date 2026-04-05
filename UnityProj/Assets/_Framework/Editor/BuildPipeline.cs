#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// One-click build pipeline for WeChat Mini Game (WebGL).
    /// Automates platform switch, player settings, build, and post-build steps.
    ///
    /// Access via: Tools → MiniGame Template → Build → Build WebGL (Dev/Release)
    /// </summary>
    public static class BuildPipeline
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
            }
            else
            {
                Debug.LogError($"[BuildPipeline] Build failed: {report.summary.result}. Errors: {report.summary.totalErrors}");
            }
        }

        private static void ConfigurePlayerSettings(bool isDevelopment)
        {
            // WebGL memory settings optimized for WeChat Mini Game
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.compressionFormat = isDevelopment
                ? WebGLCompressionFormat.Disabled
                : WebGLCompressionFormat.Brotli;

#if UNITY_2022_1_OR_NEWER
            // Unity 2022+ WebGL settings
            PlayerSettings.WebGL.debugSymbolMode = isDevelopment
                ? WebGLDebugSymbolMode.External
                : WebGLDebugSymbolMode.Off;
#endif

            // Strip unused code to minimize package size
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Medium);

            // Disable exceptions in release for performance
            PlayerSettings.WebGL.exceptionSupport = isDevelopment
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            Debug.Log($"[BuildPipeline] PlayerSettings configured for {(isDevelopment ? "Development" : "Release")} WebGL.");
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
