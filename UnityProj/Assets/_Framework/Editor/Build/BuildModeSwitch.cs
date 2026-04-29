#if UNITY_2019_4_OR_NEWER
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WeChatWASM;
using YooAsset;
using YooAsset.Editor;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// 一键切换「导出模式」与「编辑器模式」，并在导出前自动构建 Bundle。
    /// 菜单位置：Tools/MiniGame/切换到导出模式 (Build Bundle + WebGL)
    ///           Tools/MiniGame/切换到编辑器模式 (EditorSimulate)
    ///           Tools/MiniGame/导出后处理 (Post-Export)
    ///
    /// CHANGELOG:
    /// 2026-04-30  移除多余的「设置微信导出目录」菜单项和 EditorPrefs，导出路径改为直接读取微信转换工具配置。
    /// 2026-04-28  将「拷贝 StreamingAssets」升级为「导出后处理」，统一处理 StreamingAssets + 首包资源拷贝。
    /// 2026-04-26  新增「拷贝 StreamingAssets 到小游戏」菜单项，微信转换后自动同步 YooAsset Bundle。
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

                    // 提示下一步：微信转换后执行导出后处理
                    bool postExportNow = EditorUtility.DisplayDialog("导出模式就绪",
                        "Bundle 构建完成，PlayMode 已切换为 WebGL。\n\n" +
                        "下一步操作：\n" +
                        "1. 使用微信小游戏转换工具导出\n" +
                        "2. 导出完成后执行「导出后处理 (Post-Export)」\n\n" +
                        "如果已完成微信转换，可立即执行导出后处理。",
                        "立即执行导出后处理", "稍后手动执行");

                    if (postExportNow)
                    {
                        PostExport();
                    }
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

        // ──────────────── 导出后处理 (Post-Export) ────────────────

        /// <summary>
        /// 微信小游戏转换工具导出后的一站式后处理。
        /// 统一执行：
        ///   1. 拷贝 StreamingAssets（YooAsset Bundle）到 minigame/
        ///   2. 拷贝首包资源（*.webgl.data.unityweb.bin.txt）到 minigame/（CDN 模式所需）
        /// </summary>
        [MenuItem("Tools/MiniGame/导出后处理 (Post-Export)", priority = 110)]
        public static void PostExport()
        {
            // 1. 从微信转换工具配置读取导出根目录
            string exportRoot = GetWXExportRoot();
            if (string.IsNullOrEmpty(exportRoot))
                return;

            string webglDir = Path.Combine(exportRoot, "webgl");
            string minigameDir = Path.Combine(exportRoot, "minigame");

            // 2. 校验基础目录
            if (!Directory.Exists(webglDir))
            {
                Debug.LogError($"[PostExport] webgl 目录不存在: {webglDir}\n请先完成微信小游戏转换导出。");
                EditorUtility.DisplayDialog("导出后处理失败",
                    $"找不到 webgl 目录:\n{webglDir}\n\n请先执行微信小游戏转换工具导出。", "知道了");
                return;
            }

            if (!Directory.Exists(minigameDir))
            {
                Debug.LogError($"[PostExport] minigame 目录不存在: {minigameDir}\n请先完成微信小游戏转换导出。");
                EditorUtility.DisplayDialog("导出后处理失败",
                    $"找不到 minigame 目录:\n{minigameDir}\n\n请先执行微信小游戏转换工具导出。", "知道了");
                return;
            }

            var report = new System.Text.StringBuilder();
            bool hasError = false;

            // ─── Step 1: 拷贝 StreamingAssets ───
            hasError |= !CopyStreamingAssets(exportRoot, webglDir, minigameDir, report);

            // ─── Step 2: 拷贝首包资源 ───
            hasError |= !CopyDataPackageFiles(webglDir, minigameDir, report);

            // ─── Step 3: 主包大小预警 ───
            long mainPkgBytes = GetMainPackageSize(minigameDir);
            double mainPkgMB = mainPkgBytes / (1024.0 * 1024.0);
            if (mainPkgMB > 1.9)
                report.AppendLine($"\n⚠️ 主包预估 {mainPkgMB:F2} MB，接近 2MB 限制！");
            else
                report.AppendLine($"\n主包预估 {mainPkgMB:F2} MB（2MB 限制内）。");

            // ─── 汇报 ───
            string title = hasError ? "导出后处理（部分失败）" : "导出后处理完成 ✅";
            string logColor = hasError ? "yellow" : "green";
            Debug.Log($"<color={logColor}>[PostExport] {title}</color>\n{report}");
            EditorUtility.DisplayDialog(title, report.ToString(), "好的");
        }

        [MenuItem("Tools/MiniGame/导出后处理 (Post-Export)", true)]
        private static bool ValidatePostExport()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isPlaying;
        }

        /// <summary>
        /// 拷贝 StreamingAssets（YooAsset Bundle）到 minigame/。
        /// </summary>
        private static bool CopyStreamingAssets(string exportRoot, string webglDir, string minigameDir,
            System.Text.StringBuilder report)
        {
            string srcDir = Path.Combine(webglDir, "StreamingAssets");
            string dstDir = Path.Combine(minigameDir, "StreamingAssets");

            if (!Directory.Exists(srcDir))
            {
                report.AppendLine($"⚠️ StreamingAssets 源目录不存在，跳过: {srcDir}");
                Debug.LogWarning($"[PostExport] StreamingAssets 源目录不存在: {srcDir}");
                return true; // 不算致命错误，可能还没构建 Bundle
            }

            // 幂等：先删后拷
            if (Directory.Exists(dstDir))
            {
                Directory.Delete(dstDir, true);
            }

            try
            {
                CopyDirectoryRecursive(srcDir, dstDir);
                var copiedFiles = Directory.GetFiles(dstDir, "*", SearchOption.AllDirectories);
                long totalBytes = copiedFiles.Sum(f => new FileInfo(f).Length);
                double totalMB = totalBytes / (1024.0 * 1024.0);

                report.AppendLine($"✅ StreamingAssets: {copiedFiles.Length} 个文件, {totalMB:F2} MB");
                Debug.Log($"[PostExport] StreamingAssets 拷贝完成 → {dstDir} ({copiedFiles.Length} files, {totalMB:F2} MB)");
                return true;
            }
            catch (Exception e)
            {
                report.AppendLine($"❌ StreamingAssets 拷贝失败: {e.Message}");
                Debug.LogError($"[PostExport] StreamingAssets 拷贝失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 拷贝首包资源文件到 minigame/ 根目录。
        /// 支持未压缩（*.bin.txt）和 Brotli 压缩（*.bin.br）两种格式。
        /// CDN 模式下，微信插件会用 DATA_CDN/{hash}.webgl.data.unityweb.bin.{txt|br} 下载首包，
        /// 而该文件只存在于 webgl/ 目录中，需要手动拷贝到 minigame/（Dev Server 服务目录）。
        /// </summary>
        private static bool CopyDataPackageFiles(string webglDir, string minigameDir,
            System.Text.StringBuilder report)
        {
            // 扫描 webgl/ 根目录下的首包资源文件（未压缩 .bin.txt 或 Brotli 压缩 .bin.br）
            string[] dataFiles = Directory.GetFiles(webglDir, "*.webgl.data.unityweb.bin.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".bin.txt", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".bin.br", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (dataFiles.Length == 0)
            {
                report.AppendLine("ℹ️ 未发现首包资源文件（*.webgl.data.unityweb.bin.txt / .bin.br），跳过。");
                Debug.Log("[PostExport] 未发现首包资源文件，跳过。");
                return true;
            }

            int copied = 0;
            long totalBytes = 0;

            foreach (string srcFile in dataFiles)
            {
                string fileName = Path.GetFileName(srcFile);
                string dstFile = Path.Combine(minigameDir, fileName);

                try
                {
                    File.Copy(srcFile, dstFile, true);
                    long size = new FileInfo(dstFile).Length;
                    totalBytes += size;
                    copied++;
                    Debug.Log($"[PostExport] 首包资源拷贝: {fileName} ({size / (1024.0 * 1024.0):F2} MB)");
                }
                catch (Exception e)
                {
                    report.AppendLine($"❌ 首包资源拷贝失败: {fileName} — {e.Message}");
                    Debug.LogError($"[PostExport] 首包资源拷贝失败: {fileName}\n{e.Message}");
                    return false;
                }
            }

            double totalMB = totalBytes / (1024.0 * 1024.0);
            report.AppendLine($"✅ 首包资源: {copied} 个文件, {totalMB:F2} MB");
            return true;
        }

        /// <summary>
        /// [向后兼容] 仅拷贝 StreamingAssets 的旧入口。
        /// 新代码请使用 PostExport()。
        /// </summary>
        public static void CopyStreamingAssetsToMiniGame()
        {
            PostExport();
        }

        // ──────────────── 拷贝相关内部方法 ────────────────

        /// <summary>
        /// 从微信小游戏转换工具的配置中读取导出根目录（ProjectConf.DST）。
        /// 单一数据源，零同步问题。
        /// </summary>
        private static string GetWXExportRoot()
        {
            var wxConfig = UnityUtil.GetEditorConf();
            if (wxConfig == null)
            {
                Debug.LogError("[BuildModeSwitch] 无法获取微信转换工具配置（UnityUtil.GetEditorConf() 返回 null）。");
                return null;
            }

            string dst = wxConfig.ProjectConf.DST;
            if (string.IsNullOrEmpty(dst))
            {
                Debug.LogError("[BuildModeSwitch] 微信转换工具的导出路径为空，请先在转换工具面板中设置导出路径。");
                EditorUtility.DisplayDialog("导出路径未配置",
                    "微信小游戏转换工具的导出路径为空。\n\n请先打开 微信小游戏转换工具 面板，设置导出路径后重试。",
                    "知道了");
                return null;
            }

            return dst;
        }

        /// <summary>递归拷贝目录。</summary>
        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, destSubDir);
            }
        }

        /// <summary>
        /// 估算 minigame 主包大小（排除已知分包目录 wasmcode/ 和 data-package/）。
        /// </summary>
        private static long GetMainPackageSize(string minigameDir)
        {
            long total = 0;
            var excludeDirs = new[] { "wasmcode", "data-package" };

            foreach (var file in Directory.GetFiles(minigameDir, "*", SearchOption.AllDirectories))
            {
                // 检查是否在排除的分包目录中
                string relativePath = file.Substring(minigameDir.Length + 1).Replace('\\', '/');
                bool excluded = excludeDirs.Any(d => relativePath.StartsWith(d + "/", StringComparison.OrdinalIgnoreCase));
                if (!excluded)
                    total += new FileInfo(file).Length;
            }

            return total;
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
