#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// EntitySpawnWaveSO 自定义 Inspector——Phase 1 最小版。
    /// ET-007: 在 Waves[] 上方显示只读摘要面板。
    /// 下方保留默认 Inspector 用于实际编辑。
    /// </summary>
    [CustomEditor(typeof(EntitySpawnWaveSO))]
    public class EntitySpawnWaveSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var waveSO = (EntitySpawnWaveSO)target;

            // ── 摘要面板 ──
            if (waveSO.Waves != null && waveSO.Waves.Length > 0)
            {
                EditorGUILayout.LabelField("波次摘要", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                for (int w = 0; w < waveSO.Waves.Length; w++)
                {
                    var wave = waveSO.Waves[w];
                    string triggerStr = FormatTrigger(wave);
                    string groupsStr = FormatGroups(wave);
                    EditorGUILayout.LabelField($"  Wave {w} [{triggerStr}]: {groupsStr}");
                }

                // Loop 标记
                if (waveSO.Loop)
                {
                    EditorGUILayout.LabelField($"  ──── Loop → Wave {waveSO.LoopStartWave} ────");
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(8);
            }

            // ── 默认 Inspector ──
            DrawDefaultInspector();
        }

        private static string FormatTrigger(SpawnWaveEntry wave)
        {
            return wave.TriggerMode switch
            {
                WaveTriggerMode.Timer => $"Timer {wave.TriggerDelay:F1}s",
                WaveTriggerMode.AllCleared => "AllCleared",
                WaveTriggerMode.OnCallback => "OnCallback",
                _ => wave.TriggerMode.ToString()
            };
        }

        private static string FormatGroups(SpawnWaveEntry wave)
        {
            if (wave.Groups == null || wave.Groups.Length == 0)
                return "(空)";

            var parts = new string[wave.Groups.Length];
            for (int g = 0; g < wave.Groups.Length; g++)
            {
                var group = wave.Groups[g];
                string name = group.EntityConfig != null ? group.EntityConfig.DisplayName : "???";
                if (string.IsNullOrEmpty(name) && group.EntityConfig != null)
                    name = group.EntityConfig.name;
                parts[g] = $"{name}×{group.Count}";
            }
            return string.Join(", ", parts);
        }
    }
}
#endif
