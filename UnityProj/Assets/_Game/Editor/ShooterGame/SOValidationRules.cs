using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;
using Game.ShooterGame;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// SO 验证共享规则（TDD_05 S5.3 T8）。
    /// 供 SOConsistencyValidator（构建卡口）和 ValidateSelected（开发期快速验证）复用。
    /// 
    /// L1 = 字段级非空/范围校验
    /// L2 = 引用链深层校验（如 SkillConfig → FireBulletsEffect → BulletPatternSO → BulletTypeSO）
    /// </summary>
    public static class SOValidationRules
    {
        private const int MAX_LEVEL = 30;
        private const int MAX_WAVES_WARNING = 30;

        // ──────────────────── SkillConfigSO ────────────────────

        /// <summary>
        /// 验证单个 SkillConfigSO。
        /// </summary>
        public static bool ValidateSkillConfig(SkillConfigSO skill, string context = "")
        {
            if (skill == null) return true; // null 由调用方决定是否合法
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? skill.name : context;

            // L1: Effects 非空非 null
            if (skill.Effects == null || skill.Effects.Length == 0)
            {
                Debug.LogError($"[SOValidator] {prefix}: Effects 为空——技能无实际效果", skill);
                pass = false;
            }
            else
            {
                for (int i = 0; i < skill.Effects.Length; i++)
                {
                    if (skill.Effects[i] == null)
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Effects[{i}] 为 null", skill);
                        pass = false;
                    }
                    else
                    {
                        // L2: FireBulletsEffect → BulletPatternSO 非 null
                        pass &= ValidateSkillEffect(skill.Effects[i], $"{prefix}.Effects[{i}]", skill);
                    }
                }
            }

            return pass;
        }

        private static bool ValidateSkillEffect(ISkillEffect effect, string context, Object pingTarget)
        {
            bool pass = true;

            if (effect is FireBulletsEffect fireBullets)
            {
                if (fireBullets.Pattern == null)
                {
                    Debug.LogError($"[SOValidator] {context}: FireBulletsEffect.Pattern 为 null", pingTarget);
                    pass = false;
                }
                else
                {
                    // L2 深层：BulletPatternSO → BulletTypeSO 非 null
                    if (fireBullets.Pattern.BulletType == null)
                    {
                        Debug.LogError($"[SOValidator] {context}: Pattern '{fireBullets.Pattern.name}' → BulletTypeSO 为 null", pingTarget);
                        pass = false;
                    }
                }
            }

            return pass;
        }

        // ──────────────────── BuffConfigSO ────────────────────

        public static bool ValidateBuffConfig(BuffConfigSO buff, string context = "")
        {
            if (buff == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? buff.name : context;

            // Duration 不应为负
            if (buff.Duration < 0f)
            {
                Debug.LogError($"[SOValidator] {prefix}: Duration={buff.Duration} < 0", buff);
                pass = false;
            }

            // StackMode=Stack 但 MaxStacks <= 1
            if (buff.StackMode == StackMode.Stack && buff.MaxStacks <= 1)
            {
                Debug.LogError($"[SOValidator] {prefix}: StackMode=Stack 但 MaxStacks={buff.MaxStacks} ≤ 1", buff);
                pass = false;
            }

            return pass;
        }

        // ──────────────────── DotConfigSO ────────────────────

        public static bool ValidateDotConfig(DotConfigSO dot, string context = "")
        {
            if (dot == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? dot.name : context;

            if (dot.DamagePerTick <= 0)
            {
                Debug.LogError($"[SOValidator] {prefix}: DamagePerTick={dot.DamagePerTick} ≤ 0", dot);
                pass = false;
            }

            if (dot.Interval <= 0f)
            {
                Debug.LogError($"[SOValidator] {prefix}: Interval={dot.Interval} ≤ 0", dot);
                pass = false;
            }

            if (dot.Duration <= 0f)
            {
                Debug.LogError($"[SOValidator] {prefix}: Duration={dot.Duration} ≤ 0", dot);
                pass = false;
            }

            return pass;
        }

        // ──────────────────── PickupConfigSO ────────────────────

        public static bool ValidatePickupConfig(PickupConfigSO pickup, string context = "")
        {
            if (pickup == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? pickup.name : context;

            // PickupType 有效性 — 枚举默认值为 0(None)，无需强制
            // 数值字段已通过 [Min] Attribute 限制，此处仅做逻辑一致性检查

            return pass;
        }

        // ──────────────────── DropTableSO ────────────────────

        public static bool ValidateDropTable(DropTableSO dropTable, string context = "")
        {
            if (dropTable == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? dropTable.name : context;

            if (dropTable.Entries == null || dropTable.Entries.Length == 0)
            {
                Debug.LogError($"[SOValidator] {prefix}: Entries 为空", dropTable);
                pass = false;
            }
            else
            {
                for (int i = 0; i < dropTable.Entries.Length; i++)
                {
                    if (dropTable.Entries[i].Pickup == null)
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].Pickup 为 null", dropTable);
                        pass = false;
                    }
                }
            }

            if (dropTable.BaseDropRate <= 0f || dropTable.BaseDropRate > 1f)
            {
                Debug.LogError($"[SOValidator] {prefix}: BaseDropRate={dropTable.BaseDropRate} 不在 (0,1] 范围", dropTable);
                pass = false;
            }

            return pass;
        }

        // ──────────────────── EntityConfigSO ────────────────────

        public static bool ValidateEntityConfig(EntityConfigSO entity, string context = "")
        {
            if (entity == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? entity.name : context;

            // MaxHp > 0
            if (entity.MaxHp <= 0)
            {
                Debug.LogError($"[SOValidator] {prefix}: MaxHp={entity.MaxHp} ≤ 0", entity);
                pass = false;
            }

            // SkillConfig 如果引用非 null，则验证其内容
            if (entity.SkillConfig != null)
            {
                pass &= ValidateSkillConfig(entity.SkillConfig, $"{prefix}.SkillConfig({entity.SkillConfig.name})");
            }

            return pass;
        }

        // ──────────────────── BulletPatternSO ────────────────────

        public static bool ValidateBulletPattern(BulletPatternSO pattern, string context = "")
        {
            if (pattern == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? pattern.name : context;

            if (pattern.BulletType == null)
            {
                Debug.LogError($"[SOValidator] {prefix}: BulletType 为 null", pattern);
                pass = false;
            }

            if (pattern.Count <= 0)
            {
                Debug.LogError($"[SOValidator] {prefix}: Count={pattern.Count} ≤ 0", pattern);
                pass = false;
            }

            return pass;
        }

        // ──────────────────── SkillUnlockTableSO ────────────────────

        public static bool ValidateSkillUnlockTable(ScriptableObject so, string context = "")
        {
            if (so == null) return true;
            bool pass = true;

            // _entries 是 private，需 BindingFlags
            var type = so.GetType();
            var entriesField = type.GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (entriesField == null) return true;

            var entries = entriesField.GetValue(so) as System.Array;
            if (entries == null || entries.Length == 0) return pass;

            string prefix = string.IsNullOrEmpty(context) ? so.name : context;
            var seenSkills = new HashSet<Object>();

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries.GetValue(i);
                var entryType = entry.GetType();

                // Skill 非 null
                var skillField = entryType.GetField("Skill");
                if (skillField != null)
                {
                    var skill = skillField.GetValue(entry) as Object;
                    if (skill == null)
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].Skill 为 null", so);
                        pass = false;
                    }
                    else if (!seenSkills.Add(skill))
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].Skill '{skill.name}' 重复引用", so);
                        pass = false;
                    }
                }

                // ConditionParam 范围检查（ClearLevel 类）
                var condParamField = entryType.GetField("ConditionParam");
                var condTypeField = entryType.GetField("ConditionType");
                if (condParamField != null && condTypeField != null)
                {
                    int condParam = (int)condParamField.GetValue(entry);
                    var condValue = condTypeField.GetValue(entry);
                    if (condValue != null && condValue.ToString() == "ClearLevel")
                    {
                        if (condParam < 1 || condParam > MAX_LEVEL)
                        {
                            Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].ConditionParam={condParam} 不在 [1,{MAX_LEVEL}] 范围", so);
                            pass = false;
                        }
                    }
                }
            }

            return pass;
        }

        // ──────────────────── PassiveUnlockTableSO ────────────────────

        public static bool ValidatePassiveUnlockTable(ScriptableObject so, string context = "")
        {
            if (so == null) return true;
            bool pass = true;

            var type = so.GetType();
            var entriesField = type.GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (entriesField == null) return true;

            var entries = entriesField.GetValue(so) as System.Array;
            if (entries == null || entries.Length == 0) return pass;

            string prefix = string.IsNullOrEmpty(context) ? so.name : context;
            var seenPassives = new HashSet<Object>();

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries.GetValue(i);
                var entryType = entry.GetType();

                // PassiveConfig 非 null
                var passiveField = entryType.GetField("PassiveConfig");
                if (passiveField != null)
                {
                    var passive = passiveField.GetValue(entry) as Object;
                    if (passive == null)
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].PassiveConfig 为 null", so);
                        pass = false;
                    }
                    else if (!seenPassives.Add(passive))
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].PassiveConfig '{passive.name}' 重复引用", so);
                        pass = false;
                    }
                }

                // ConditionParam 范围检查
                var condParamField = entryType.GetField("ConditionParam");
                var condTypeField = entryType.GetField("ConditionType");
                if (condParamField != null && condTypeField != null)
                {
                    int condParam = (int)condParamField.GetValue(entry);
                    var condValue = condTypeField.GetValue(entry);
                    if (condValue != null && condValue.ToString() == "ClearLevel")
                    {
                        if (condParam < 1 || condParam > MAX_LEVEL)
                        {
                            Debug.LogError($"[SOValidator] {prefix}: Entries[{i}].ConditionParam={condParam} 不在 [1,{MAX_LEVEL}] 范围", so);
                            pass = false;
                        }
                    }
                }
            }

            return pass;
        }

        // ──────────────────── SG_LevelConfigSO ────────────────────

        /// <summary>
        /// 验证 SG_LevelConfigSO。
        /// 结构：SG_LevelConfigSO.WaveConfig (EntitySpawnWaveSO) → .Waves (SpawnWaveEntry[]) → .Groups (SpawnGroup[]) → .EntityConfig
        /// </summary>
        public static bool ValidateLevelConfig(ScriptableObject so, string context = "")
        {
            if (so == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? so.name : context;

            // 获取 WaveConfig 字段
            var type = so.GetType();
            var waveConfigField = type.GetField("WaveConfig");
            if (waveConfigField == null) return true;

            var waveConfig = waveConfigField.GetValue(so) as EntitySpawnWaveSO;
            if (waveConfig == null)
            {
                Debug.LogError($"[SOValidator] {prefix}: WaveConfig 为 null", so);
                return false;
            }

            pass &= ValidateSpawnWaveSO(waveConfig, $"{prefix}.WaveConfig({waveConfig.name})");
            return pass;
        }

        /// <summary>
        /// 验证 EntitySpawnWaveSO：Waves 非空、Groups.EntityConfig 非 null。
        /// </summary>
        public static bool ValidateSpawnWaveSO(EntitySpawnWaveSO waveSO, string context = "")
        {
            if (waveSO == null) return true;
            bool pass = true;
            string prefix = string.IsNullOrEmpty(context) ? waveSO.name : context;

            if (waveSO.Waves == null || waveSO.Waves.Length == 0)
            {
                Debug.LogError($"[SOValidator] {prefix}: Waves 为 null 或空", waveSO);
                return false;
            }

            // 总波次数警告
            if (waveSO.Waves.Length > MAX_WAVES_WARNING)
            {
                Debug.LogWarning($"[SOValidator] {prefix}: Waves.Length={waveSO.Waves.Length} > {MAX_WAVES_WARNING}，请确认是否为误配", waveSO);
            }

            for (int i = 0; i < waveSO.Waves.Length; i++)
            {
                var wave = waveSO.Waves[i];
                if (wave.Groups == null || wave.Groups.Length == 0)
                {
                    Debug.LogError($"[SOValidator] {prefix}: Waves[{i}].Groups 为 null 或空", waveSO);
                    pass = false;
                    continue;
                }

                for (int j = 0; j < wave.Groups.Length; j++)
                {
                    if (wave.Groups[j].EntityConfig == null)
                    {
                        Debug.LogError($"[SOValidator] {prefix}: Waves[{i}].Groups[{j}].EntityConfig 为 null", waveSO);
                        pass = false;
                    }
                }
            }

            return pass;
        }

        // ──────────────────── 通用分发（根据类型路由到对应方法）────────────────────

        /// <summary>
        /// 验证任意 SO，根据类型分发到对应规则。
        /// 返回 true = 无问题。
        /// </summary>
        public static bool ValidateAny(ScriptableObject so)
        {
            if (so == null) return true;

            if (so is SkillConfigSO skill) return ValidateSkillConfig(skill);
            if (so is BuffConfigSO buff) return ValidateBuffConfig(buff);
            if (so is DotConfigSO dot) return ValidateDotConfig(dot);
            if (so is EntityConfigSO entity) return ValidateEntityConfig(entity);
            if (so is BulletPatternSO pattern) return ValidateBulletPattern(pattern);
            if (so is PickupConfigSO pickup) return ValidatePickupConfig(pickup);
            if (so is DropTableSO dropTable) return ValidateDropTable(dropTable);
            if (so is EntitySpawnWaveSO waveSO) return ValidateSpawnWaveSO(waveSO);

            // 按类名路由（跨命名空间类型）
            string typeName = so.GetType().Name;
            switch (typeName)
            {
                case "SkillUnlockTableSO": return ValidateSkillUnlockTable(so);
                case "PassiveUnlockTableSO": return ValidatePassiveUnlockTable(so);
                case "SG_LevelConfigSO": return ValidateLevelConfig(so);
            }

            // 未注册类型 — 跳过
            return true;
        }
    }
}
