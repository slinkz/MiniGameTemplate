#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// TDD-06 迁移工具：从 EntityConfigSO 的 Components 数组中移除 ComponentType.Attack。
    /// 使用后 EntityPool 不再为实体创建 AttackComponent（已被 SkillComponent Slot[0] 替代）。
    /// </summary>
    public static class SG_AttackMigrationTool
    {
        private const string MENU_REMOVE = "Tools/ShooterGame/Migration/Remove Attack from All EntityConfigs";
        private const string MENU_VERIFY = "Tools/ShooterGame/Migration/Verify No Attack Components";

        /// <summary>
        /// 批量移除所有 EntityConfigSO 的 Components 数组中的 ComponentType.Attack。
        /// </summary>
        [MenuItem(MENU_REMOVE)]
        private static void RemoveAttackFromAllConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:EntityConfigSO");
            int modifiedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<EntityConfigSO>(path);
                if (config == null || config.Components == null) continue;

                bool hasAttack = false;
                for (int i = 0; i < config.Components.Length; i++)
                {
                    if (config.Components[i] == ComponentType.Attack)
                    {
                        hasAttack = true;
                        break;
                    }
                }

                if (!hasAttack) continue;

                // 移除 Attack
                int newCount = 0;
                for (int i = 0; i < config.Components.Length; i++)
                {
                    if (config.Components[i] != ComponentType.Attack) newCount++;
                }

                var newComponents = new ComponentType[newCount];
                int idx = 0;
                for (int i = 0; i < config.Components.Length; i++)
                {
                    if (config.Components[i] != ComponentType.Attack)
                        newComponents[idx++] = config.Components[i];
                }

                config.Components = newComponents;
                EditorUtility.SetDirty(config);
                modifiedCount++;
                Debug.Log($"[Migration] Removed Attack from: {path}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Migration] Done. Modified {modifiedCount}/{guids.Length} EntityConfigSO assets.");
            EditorUtility.DisplayDialog("Migration Complete",
                $"Removed ComponentType.Attack from {modifiedCount} EntityConfigSO assets.\nTotal scanned: {guids.Length}",
                "OK");
        }

        /// <summary>
        /// 验证所有 EntityConfigSO 均不包含 ComponentType.Attack。
        /// </summary>
        [MenuItem(MENU_VERIFY)]
        private static void VerifyNoAttackComponents()
        {
            string[] guids = AssetDatabase.FindAssets("t:EntityConfigSO");
            int failCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<EntityConfigSO>(path);
                if (config == null || config.Components == null) continue;

                for (int i = 0; i < config.Components.Length; i++)
                {
                    if (config.Components[i] == ComponentType.Attack)
                    {
                        Debug.LogError($"[Migration] FAIL: {path} still has ComponentType.Attack!");
                        failCount++;
                        break;
                    }
                }
            }

            if (failCount == 0)
            {
                Debug.Log($"[Migration] PASS: All {guids.Length} EntityConfigSO assets are Attack-free.");
                EditorUtility.DisplayDialog("Verification Passed",
                    $"All {guids.Length} EntityConfigSO assets have no ComponentType.Attack.", "OK");
            }
            else
            {
                Debug.LogError($"[Migration] FAIL: {failCount} assets still have ComponentType.Attack.");
                EditorUtility.DisplayDialog("Verification FAILED",
                    $"{failCount} assets still have ComponentType.Attack.\nCheck Console for details.", "OK");
            }
        }
    }
}
#endif
