using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Data;
using Game.ShooterGame;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// 编辑器工具：清理本地通关进度（含星级）。
    /// 用途：测试 D5（星级只升不降）时需要还原干净状态。
    /// 
    /// 原理：
    ///   - 非运行时：直接 PlayerPrefs.DeleteKey 删除 sg_progress + HMAC 签名
    ///   - 运行时：通过 SG_Boot.Progress.ResetAll() 正式清空（含云同步触发）
    /// </summary>
    public static class SG_ProgressResetTool
    {
        private const string SAVE_KEY = "sg_progress";
        private const string HMAC_SUFFIX = "__hmac";

        [MenuItem("Tools/ShooterGame/存档/清空本地通关进度（含星级）", priority = 100)]
        private static void ResetLocalProgress()
        {
            if (!EditorUtility.DisplayDialog(
                "⚠️ 清空通关进度",
                "将删除本地所有关卡通关记录、星级、成就计数器。\n\n" +
                "• 此操作不影响云端存档（下次启动会从云端拉取覆盖）\n" +
                "• 若需同时清空云端，请在微信后台手动清理\n\n" +
                "确认清空？",
                "确认清空", "取消"))
            {
                return;
            }

            if (Application.isPlaying)
            {
                // 运行时：使用正式 API（会触发 Save → 云同步 EnqueueUpload）
                var progress = SG_Boot.Progress;
                if (progress != null)
                {
                    progress.ResetAll();
                    Debug.Log("[SG_ProgressResetTool] ✅ 运行时清空完成（已通过 SG_Boot.Progress.ResetAll()）");
                }
                else
                {
                    // Fallback: 运行时但 Progress 未初始化（极端情况）
                    DeletePlayerPrefsKeys();
                    Debug.Log("[SG_ProgressResetTool] ✅ 运行时 Fallback 清空完成（直接删 PlayerPrefs）");
                }
            }
            else
            {
                // 非运行时：直接操作 PlayerPrefs
                DeletePlayerPrefsKeys();
                Debug.Log("[SG_ProgressResetTool] ✅ 编辑器模式清空完成（已删除 PlayerPrefs 中的进度数据）");
            }
        }

        [MenuItem("Tools/ShooterGame/存档/查看本地通关进度（调试）", priority = 101)]
        private static void ViewLocalProgress()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SG_ProgressResetTool] 本地无进度数据（空白状态）");
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<SharedProgressData>(json);
                if (data == null)
                {
                    Debug.Log("[SG_ProgressResetTool] 进度数据解析失败（null）");
                    return;
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("══════ 本地通关进度 ══════");
                sb.AppendLine($"版本: {data.version}");
                sb.AppendLine($"已通关关卡: [{string.Join(", ", data.clearedLevels)}]");
                sb.AppendLine($"关卡星级:");
                if (data.levelStars != null)
                {
                    foreach (var entry in data.levelStars)
                        sb.AppendLine($"  关卡 {entry.levelIndex}: {"★".PadRight(entry.stars, '★')} ({entry.stars}星)");
                }
                sb.AppendLine($"累计死亡: {data.totalDeaths}");
                sb.AppendLine($"单关最高击杀: {data.maxKillsInOneLevel}");
                sb.AppendLine($"累计被命中: {data.totalHitsTaken}");
                sb.AppendLine($"已解锁技能: [{string.Join(", ", data.unlockedSkillIds)}]");
                sb.AppendLine($"已解锁被动: [{string.Join(", ", data.unlockedPassiveIds)}]");
                sb.AppendLine("══════════════════════════");

                Debug.Log(sb.ToString());
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SG_ProgressResetTool] 解析进度数据失败: {ex.Message}\n原始 JSON: {json}");
            }
        }

        private static void DeletePlayerPrefsKeys()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteKey(SAVE_KEY + HMAC_SUFFIX);
            PlayerPrefs.Save();
        }
    }
}
