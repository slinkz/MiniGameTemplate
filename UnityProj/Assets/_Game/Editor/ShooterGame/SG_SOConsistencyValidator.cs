using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;
using Game.ShooterGame;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// T8 SO 一致性构建验证器（TDD_05 S5.3）。
    /// 实现 IPreprocessBuildWithReport，在构建前自动执行全量 SO 验证。
    /// 验证失败时抛出 BuildFailedException 阻断构建。
    /// 
    /// 验证深度：
    /// - L1：字段级非空/范围/格式校验
    /// - L2：引用链深层校验（SkillConfig → Effect → BulletPatternSO → BulletTypeSO）
    /// </summary>
    public class SG_SOConsistencyValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool pass = RunFullValidation();
            if (!pass)
            {
                throw new BuildFailedException(
                    "[SG_SOConsistencyValidator] SO 验证失败！请修复 Console 中报告的错误后重新构建。");
            }
        }

        /// <summary>
        /// 执行完整验证（构建卡口 + 手动触发）。
        /// </summary>
        [MenuItem("Tools/ShooterGame/校验/Full SO Consistency Check")]
        public static bool RunFullValidation()
        {
            Debug.Log("[SOConsistencyValidator] 开始全量 SO 一致性校验...");

            bool pass = true;

            // ── T5 ID 冲突 ──
            pass &= SG_IdConflictValidator.RunValidation();

            // ── SkillConfigSO ──
            pass &= ValidateAllOfType<SkillConfigSO>(
                so => SOValidationRules.ValidateSkillConfig(so));

            // ── BuffConfigSO ──
            pass &= ValidateAllOfType<BuffConfigSO>(
                so => SOValidationRules.ValidateBuffConfig(so));

            // ── DotConfigSO ──
            pass &= ValidateAllOfType<DotConfigSO>(
                so => SOValidationRules.ValidateDotConfig(so));

            // ── EntityConfigSO ──
            pass &= ValidateAllOfType<EntityConfigSO>(
                so => SOValidationRules.ValidateEntityConfig(so));

            // ── BulletPatternSO ──
            pass &= ValidateAllOfType<BulletPatternSO>(
                so => SOValidationRules.ValidateBulletPattern(so));

            // ── PickupConfigSO ──
            pass &= ValidateAllOfType<PickupConfigSO>(
                so => SOValidationRules.ValidatePickupConfig(so));

            // ── DropTableSO ──
            pass &= ValidateAllOfType<DropTableSO>(
                so => SOValidationRules.ValidateDropTable(so));

            // ── UnlockTables（PK-R2 ET-003）──
            pass &= ValidateUnlockTables();

            // ── LevelConfigs（PK-R2 ET-010）──
            pass &= ValidateAllLevelConfigs();

            if (pass)
            {
                Debug.Log("[SOConsistencyValidator] ✅ 全量 SO 一致性校验通过。");
            }
            else
            {
                Debug.LogError("[SOConsistencyValidator] ❌ SO 一致性校验失败！请修复上方报告的错误。");
            }

            return pass;
        }

        // ──────────────────── 辅助方法 ────────────────────

        private static bool ValidateAllOfType<T>(System.Func<T, bool> validator) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            bool allPass = true;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue;
                var so = AssetDatabase.LoadAssetAtPath<T>(path);
                if (so == null) continue;

                allPass &= validator(so);
            }

            return allPass;
        }

        private static bool ValidateUnlockTables()
        {
            bool pass = true;

            // SkillUnlockTableSO
            var skillUnlockGuids = AssetDatabase.FindAssets("t:SkillUnlockTableSO");
            foreach (var guid in skillUnlockGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue;
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                pass &= SOValidationRules.ValidateSkillUnlockTable(so);
            }

            // PassiveUnlockTableSO
            var passiveUnlockGuids = AssetDatabase.FindAssets("t:PassiveUnlockTableSO");
            foreach (var guid in passiveUnlockGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue;
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                pass &= SOValidationRules.ValidatePassiveUnlockTable(so);
            }

            return pass;
        }

        private static bool ValidateAllLevelConfigs()
        {
            bool pass = true;

            var guids = AssetDatabase.FindAssets("t:SG_LevelConfigSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue;
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                pass &= SOValidationRules.ValidateLevelConfig(so);
            }

            return pass;
        }

        private static bool IsTemplateAsset(string path)
        {
            return path.Contains("/_Template/") || path.Contains("\\_Template\\");
        }
    }
}
