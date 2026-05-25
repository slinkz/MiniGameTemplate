using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// T5 ID 冲突检测工具（TDD_03 S3.6）。
    /// 扫描项目中所有 BuffConfigSO 和 DotConfigSO，校验：
    /// - BuffId 唯一性 + 范围 [1000, 3999]
    /// - DotId 唯一性 + 范围 [4000, 4999]
    /// </summary>
    public static class SG_IdConflictValidator
    {
        // ── ID 范围定义 ──
        private const int BUFF_ID_MIN = 1000;
        private const int BUFF_ID_MAX = 3999;
        private const int DOT_ID_MIN = 4000;
        private const int DOT_ID_MAX = 4999;
        private const int PASSIVE_ID_MIN = 5000;
        private const int PASSIVE_ID_MAX = 5999;

        [MenuItem("Tools/ShooterGame/校验/Check ID Conflicts")]
        public static void CheckIdConflicts()
        {
            bool passed = RunValidation();
            if (passed)
            {
                Debug.Log("[T5 ID Validator] ✅ 全部通过：无 ID 冲突，范围合规。");
            }
        }

        /// <summary>
        /// 执行完整校验。返回 true = 无问题。可在构建卡口中调用。
        /// </summary>
        public static bool RunValidation()
        {
            int errorCount = 0;

            // ── 1. 扫描 BuffConfigSO ──
            var buffGuids = AssetDatabase.FindAssets("t:BuffConfigSO");
            var buffIdMap = new Dictionary<int, string>(buffGuids.Length);

            foreach (var guid in buffGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue; // 跳过模板 SO
                var buff = AssetDatabase.LoadAssetAtPath<BuffConfigSO>(path);
                if (buff == null) continue;

                int id = buff.BuffId;

                // 范围检查
                if (id < BUFF_ID_MIN || id > BUFF_ID_MAX)
                {
                    Debug.LogError($"[T5 ID Validator] BuffId 越界: {buff.name} (Id={id})，有效范围 [{BUFF_ID_MIN},{BUFF_ID_MAX}]", buff);
                    errorCount++;
                }

                // 唯一性检查
                if (buffIdMap.TryGetValue(id, out var existingPath))
                {
                    Debug.LogError($"[T5 ID Validator] BuffId 冲突: Id={id} 被 '{existingPath}' 和 '{path}' 同时使用", buff);
                    errorCount++;
                }
                else
                {
                    buffIdMap[id] = path;
                }
            }

            // ── 2. 扫描 DotConfigSO ──
            var dotGuids = AssetDatabase.FindAssets("t:DotConfigSO");
            var dotIdMap = new Dictionary<int, string>(dotGuids.Length);

            foreach (var guid in dotGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue;
                var dot = AssetDatabase.LoadAssetAtPath<DotConfigSO>(path);
                if (dot == null) continue;

                int id = dot.DotId;

                // 范围检查
                if (id < DOT_ID_MIN || id > DOT_ID_MAX)
                {
                    Debug.LogError($"[T5 ID Validator] DotId 越界: {dot.name} (Id={id})，有效范围 [{DOT_ID_MIN},{DOT_ID_MAX}]", dot);
                    errorCount++;
                }

                // 唯一性检查
                if (dotIdMap.TryGetValue(id, out var existingPath))
                {
                    Debug.LogError($"[T5 ID Validator] DotId 冲突: Id={id} 被 '{existingPath}' 和 '{path}' 同时使用", dot);
                    errorCount++;
                }
                else
                {
                    dotIdMap[id] = path;
                }
            }

            // ── 3. 扫描 PassiveAbilitySO ──
            var passiveGuids = AssetDatabase.FindAssets("t:PassiveAbilitySO");
            var passiveIdMap = new Dictionary<int, string>(passiveGuids.Length);

            foreach (var guid in passiveGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsTemplateAsset(path)) continue;
                var passive = AssetDatabase.LoadAssetAtPath<PassiveAbilitySO>(path);
                if (passive == null) continue;

                int id = passive.PassiveId;

                // 范围检查
                if (id < PASSIVE_ID_MIN || id > PASSIVE_ID_MAX)
                {
                    Debug.LogError($"[T5 ID Validator] PassiveId 越界: {passive.name} (Id={id})，有效范围 [{PASSIVE_ID_MIN},{PASSIVE_ID_MAX}]", passive);
                    errorCount++;
                }

                // 唯一性检查
                if (passiveIdMap.TryGetValue(id, out var existingPath))
                {
                    Debug.LogError($"[T5 ID Validator] PassiveId 冲突: Id={id} 被 '{existingPath}' 和 '{path}' 同时使用", passive);
                    errorCount++;
                }
                else
                {
                    passiveIdMap[id] = path;
                }
            }

            // ── 4. 汇总 ──
            int totalBuffs = buffGuids.Length;
            int totalDots = dotGuids.Length;
            int totalPassives = passiveGuids.Length;

            if (errorCount > 0)
            {
                Debug.LogError($"[T5 ID Validator] ❌ 校验失败：{errorCount} 个问题（扫描 {totalBuffs} Buff + {totalDots} DOT + {totalPassives} Passive）");
            }
            else
            {
                Debug.Log($"[T5 ID Validator] 扫描 {totalBuffs} Buff + {totalDots} DOT + {totalPassives} Passive，全部合规。");
            }

            return errorCount == 0;
        }

        // ──────────────────── Validate Selected（PK-R2 ET-002）────────────────────

        [MenuItem("Tools/ShooterGame/校验/Validate Selected SOs")]
        public static void ValidateSelected()
        {
            var selected = Selection.objects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("[T5 Validator] 请先选中要验证的 SO 资产");
                return;
            }

            int total = 0;
            int errors = 0;

            foreach (var obj in selected)
            {
                if (obj is ScriptableObject so)
                {
                    total++;
                    if (!SOValidationRules.ValidateAny(so))
                    {
                        errors++;
                    }
                }
            }

            if (total == 0)
            {
                Debug.LogWarning("[T5 Validator] 选中的对象中没有 ScriptableObject");
            }
            else if (errors == 0)
            {
                Debug.Log($"[T5 Validator] ✅ 验证通过：{total} 个 SO 全部合规。");
            }
            else
            {
                Debug.LogError($"[T5 Validator] ❌ {errors}/{total} 个 SO 验证失败，请查看上方错误日志。");
            }
        }

        /// <summary>
        /// 判断是否为模板资产（_Template 目录下），跳过校验。
        /// </summary>
        private static bool IsTemplateAsset(string path)
        {
            return path.Contains("/_Template/") || path.Contains("\\_Template\\");
        }
    }
}
