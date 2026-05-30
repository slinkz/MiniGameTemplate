#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Entity 配置资产批量校验工具。
    /// ET-006: MenuItem: Tools/Entity/Validate All Configs
    /// 
    /// 校验项：
    /// 1. EntityConfigSO: Components 去重/互斥 + Pool + AI/Attack 配置完整性
    /// 2. AIBehaviorSO: Entries 非空 + Always 兜底（Error 级别）
    /// 3. EntitySpawnWaveSO: Waves 非空 + 每个 Group 配置完整
    /// 
    /// WF-006: Components 为空时 Error。
    /// WF-005: Always 兜底从 Warning 提升为 Error。
    /// WF-008: 输出末尾新增反向引用摘要。
    /// </summary>
    public static class EntityConfigValidator
    {
        [MenuItem("Tools/Entity/Validate All Configs")]
        public static void ValidateAll()
        {
            int errorCount = 0;
            int warningCount = 0;

            Debug.Log("═══════════ Entity Config Validation Start ═══════════");

            // ── 1. 校验 EntityConfigSO ──
            var entityGuids = AssetDatabase.FindAssets("t:EntityConfigSO");
            var aiReverseRef = new Dictionary<AIBehaviorSO, List<string>>();

            foreach (var guid in entityGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<EntityConfigSO>(path);
                if (config == null) continue;

                string prefix = $"[EntityConfig] {config.name}";

                // WF-006: Components 为空
                if (config.Components == null || config.Components.Length == 0)
                {
                    LogError(prefix, "Components 列表为空！至少需要 State 组件。", config);
                    errorCount++;
                }
                else
                {
                    // 去重检查
                    var seen = new HashSet<ComponentType>();
                    foreach (var c in config.Components)
                    {
                        if (!seen.Add(c))
                        {
                            LogWarning(prefix, $"Components 存在重复项: {c}", config);
                            warningCount++;
                        }
                    }

                    // Control/AI 互斥
                    if (seen.Contains(ComponentType.Control) && seen.Contains(ComponentType.AI))
                    {
                        LogError(prefix, "Control 和 AI 不应同时存在！", config);
                        errorCount++;
                    }

                    // 有 AI 时 AIBehavior 不为空
                    if (seen.Contains(ComponentType.AI) && config.AIBehavior == null)
                    {
                        LogWarning(prefix, "含 AI 组件但 AIBehavior 未填——运行时 fallback Idle。", config);
                        warningCount++;
                    }

                    // Collision 半径
                    if (seen.Contains(ComponentType.Collision) && config.CollisionRadius <= 0)
                    {
                        LogWarning(prefix, "含 Collision 组件但 CollisionRadius ≤ 0。", config);
                        warningCount++;
                    }
                }

                // PoolMax
                if (config.PoolMax <= 0)
                {
                    LogError(prefix, "PoolMax 必须 > 0。", config);
                    errorCount++;
                }

                // 记录 AI 反向引用
                if (config.AIBehavior != null)
                {
                    if (!aiReverseRef.ContainsKey(config.AIBehavior))
                        aiReverseRef[config.AIBehavior] = new List<string>();
                    aiReverseRef[config.AIBehavior].Add(config.name);
                }
            }

            // ── 2. 校验 AIBehaviorSO ──
            var aiGuids = AssetDatabase.FindAssets("t:AIBehaviorSO");
            foreach (var guid in aiGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ai = AssetDatabase.LoadAssetAtPath<AIBehaviorSO>(path);
                if (ai == null) continue;

                string prefix = $"[AIBehavior] {ai.name}";

                if (ai.Entries == null || ai.Entries.Length == 0)
                {
                    LogError(prefix, "条件-动作表为空！", ai);
                    errorCount++;
                    continue;
                }

                // WF-005: Always 兜底——Error 级别
                bool hasAlways = false;
                foreach (var entry in ai.Entries)
                {
                    if (entry.Condition == AIConditionType.Always)
                    {
                        hasAlways = true;
                        break;
                    }
                }

                if (!hasAlways)
                {
                    LogError(prefix, "缺少 Always 兜底条目！运行时将默认 Idle，建议显式配置。", ai);
                    errorCount++;
                }
            }

            // ── 3. 校验 EntitySpawnWaveSO ──
            var waveGuids = AssetDatabase.FindAssets("t:EntitySpawnWaveSO");
            foreach (var guid in waveGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var wave = AssetDatabase.LoadAssetAtPath<EntitySpawnWaveSO>(path);
                if (wave == null) continue;

                string prefix = $"[SpawnWave] {wave.name}";

                if (wave.Waves == null || wave.Waves.Length == 0)
                {
                    LogError(prefix, "波次数组为空！", wave);
                    errorCount++;
                    continue;
                }

                for (int w = 0; w < wave.Waves.Length; w++)
                {
                    var entry = wave.Waves[w];
                    if (entry.Groups == null || entry.Groups.Length == 0)
                    {
                        LogWarning(prefix, $"Wave[{w}] 的 Groups 为空。", wave);
                        warningCount++;
                        continue;
                    }

                    for (int g = 0; g < entry.Groups.Length; g++)
                    {
                        var group = entry.Groups[g];
                        if (group.EntityConfig == null)
                        {
                            LogError(prefix, $"Wave[{w}].Group[{g}].EntityConfig 为 null！", wave);
                            errorCount++;
                        }
                        if (group.Count <= 0)
                        {
                            LogWarning(prefix, $"Wave[{w}].Group[{g}].Count ≤ 0。", wave);
                            warningCount++;
                        }
                    }
                }

                // Loop 校验
                if (wave.Loop && wave.LoopStartWave >= wave.Waves.Length)
                {
                    LogError(prefix, $"Loop=true 但 LoopStartWave({wave.LoopStartWave}) >= Waves.Length({wave.Waves.Length})。", wave);
                    errorCount++;
                }
            }

            // ── WF-008: 反向引用摘要 ──
            if (aiReverseRef.Count > 0)
            {
                Debug.Log("─── AIBehaviorSO 反向引用摘要 ───");
                foreach (var kvp in aiReverseRef)
                {
                    Debug.Log($"  {kvp.Key.name} ← [{string.Join(", ", kvp.Value)}]", kvp.Key);
                }
            }

            Debug.Log($"═══════════ Validation Complete: {errorCount} Error(s), {warningCount} Warning(s) ═══════════");
        }

        private static void LogError(string prefix, string msg, Object context)
        {
            Debug.LogError($"{prefix}: {msg}", context);
        }

        private static void LogWarning(string prefix, string msg, Object context)
        {
            Debug.LogWarning($"{prefix}: {msg}", context);
        }
    }
}
#endif
