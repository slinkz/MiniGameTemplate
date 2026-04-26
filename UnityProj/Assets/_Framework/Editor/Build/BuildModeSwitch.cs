#if UNITY_2019_4_OR_NEWER
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// 一键切换「导出模式」与「编辑器模式」，并在导出前自动构建 Bundle。
    /// 菜单位置：Tools/MiniGame/Switch to Export Mode
    ///           Tools/MiniGame/Switch to Editor Mode
    /// </summary>
    public static class BuildModeSwitch
    {
        private const string ASSET_CONFIG_PATH =
            "Assets/_Game/ScriptableObjects/Config/DefaultAssetConfig.asset";

        private const string PACKAGE_NAME = "DefaultPackage";
        private const string PIPELINE_NAME = "ScriptableBuildPipeline";

        // ──────────────────────────── 导出模式 ────────────────────────────
        [MenuItem("Tools/MiniGame/切换到导出模式 (Build Bundle + WebGL)", priority = 100)]
        public static void SwitchToExportMode()
        {
            if (!EditorUtility.DisplayDialog(
                    "切换到导出模式",
                    "将执行以下操作：\n\n" +
                    "1. AssetConfig.PlayMode → WebGL\n" +
                    "2. 构建 AssetBundle (SBP + LZ4 + ClearAndCopyAll)\n\n" +
                    "确认继续？",
                    "开始", "取消"))
                return;

            // Step 1: 切换 PlayMode
            if (!SetPlayMode(EAssetPlayMode.WebGL))
                return;

            // Step 2: 构建 Bundle
            EditorApplication.delayCall += () =>
            {
                bool success = BuildBundles();
                if (success)
                {
                    Debug.Log("<color=green>[BuildModeSwitch] ✅ 导出模式就绪！可以执行微信小游戏导出了。</color>");
                    EditorUtility.DisplayDialog("导出模式就绪",
                        "Bundle 构建完成，PlayMode 已切换为 WebGL。\n" +
                        "现在可以执行微信小游戏导出。", "好的");
                }
                else
                {
                    Debug.LogError("[BuildModeSwitch] ❌ Bundle 构建失败，请查看 Console 日志。");
                }
            };
        }

        // ──────────────────────────── 编辑器模式 ────────────────────────────
        [MenuItem("Tools/MiniGame/切换到编辑器模式 (EditorSimulate)", priority = 101)]
        public static void SwitchToEditorMode()
        {
            if (!SetPlayMode(EAssetPlayMode.EditorSimulate))
                return;

            Debug.Log("<color=cyan>[BuildModeSwitch] ✅ 已切换到编辑器模式 (EditorSimulate)，无需构建 Bundle。</color>");
        }

        // ──────────────────────────── 菜单校验 ────────────────────────────
        [MenuItem("Tools/MiniGame/切换到导出模式 (Build Bundle + WebGL)", true)]
        private static bool ValidateExportMode()
        {
            // 正在编译时禁用
            return !EditorApplication.isCompiling && !EditorApplication.isPlaying;
        }

        [MenuItem("Tools/MiniGame/切换到编辑器模式 (EditorSimulate)", true)]
        private static bool ValidateEditorMode()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isPlaying;
        }

        // ──────────────────────────── 内部方法 ────────────────────────────

        /// <summary>
        /// 修改 DefaultAssetConfig 的 PlayMode 字段。
        /// </summary>
        private static bool SetPlayMode(EAssetPlayMode mode)
        {
            var config = AssetDatabase.LoadAssetAtPath<MiniGameTemplate.Asset.AssetConfig>(ASSET_CONFIG_PATH);
            if (config == null)
            {
                Debug.LogError($"[BuildModeSwitch] 找不到 AssetConfig: {ASSET_CONFIG_PATH}");
                return false;
            }

            var so = new SerializedObject(config);
            var playModeProp = so.FindProperty("_playMode");
            if (playModeProp == null)
            {
                Debug.LogError("[BuildModeSwitch] 无法找到 _playMode 属性。");
                return false;
            }

            playModeProp.intValue = (int)mode;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.Log($"[BuildModeSwitch] PlayMode → {mode}");
            return true;
        }

        /// <summary>
        /// 使用 YooAsset SBP 管线构建 Bundle。
        /// 自动清理同版本残留目录（处理 ErrorCode115）。
        /// </summary>
        private static bool BuildBundles()
        {
            try
            {
                // 生成版本号
                string version = DateTime.Now.ToString("yyyy-MM-dd-HHmm");

                // 检查并清理残留输出目录（防止 ErrorCode115）
                string outputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
                string targetDir = $"{outputRoot}/{EditorUserBuildSettings.activeBuildTarget}/{PACKAGE_NAME}/{version}";
                if (Directory.Exists(targetDir))
                {
                    Debug.LogWarning($"[BuildModeSwitch] 清理残留构建目录: {targetDir}");
                    Directory.Delete(targetDir, true);
                }

                // 从 EditorPrefs 读取用户上次在 YooAsset Builder 窗口中设置的参数
                var fileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(PACKAGE_NAME, PIPELINE_NAME);
                var clearBuildCache = AssetBundleBuilderSetting.GetPackageClearBuildCache(PACKAGE_NAME, PIPELINE_NAME);
                var useAssetDependencyDB = AssetBundleBuilderSetting.GetPackageUseAssetDependencyDB(PACKAGE_NAME, PIPELINE_NAME);

                var buildParameters = new ScriptableBuildParameters
                {
                    BuildOutputRoot = outputRoot,
                    BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                    BuildPipeline = PIPELINE_NAME,
                    BuildBundleType = (int)EBuildBundleType.AssetBundle,
                    BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                    PackageName = PACKAGE_NAME,
                    PackageVersion = version,
                    EnableSharePackRule = true,
                    VerifyBuildingResult = true,
                    FileNameStyle = fileNameStyle,
                    BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                    BuildinFileCopyParams = string.Empty,
                    CompressOption = ECompressOption.LZ4,
                    ClearBuildCacheFiles = clearBuildCache,
                    UseAssetDependencyDB = useAssetDependencyDB,
                };

                EditorUtility.ClearProgressBar();

                var pipeline = new ScriptableBuildPipeline();
                var result = pipeline.Run(buildParameters, true);

                if (result.Success)
                {
                    Debug.Log($"[BuildModeSwitch] Bundle 构建成功 → {result.OutputPackageDirectory}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[BuildModeSwitch] Bundle 构建失败: [{result.FailedTask}] {result.ErrorInfo}\n{result.ErrorStack}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildModeSwitch] Bundle 构建异常: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        // ────────────────── 枚举桥接（避免跨 asmdef 使用 Asset 命名空间的枚举）──────────────────
        private enum EAssetPlayMode
        {
            EditorSimulate = 0,
            Offline = 1,
            Host = 2,
            WebGL = 3
        }
    }
}
#endif
