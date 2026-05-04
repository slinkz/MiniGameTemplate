using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;
using MiniGameTemplate.EditorTools;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// EntitySpawnWaveSO 编辑器增强（Game 层）。
    /// 继承框架层 EntitySpawnWaveSOEditor，保留波次摘要面板。
    /// 新增：统计面板（总波次/总敌机/预估时长）+ 一键复制最后一波。
    /// TDD: SG_TOOLS_TDD_01
    /// </summary>
    [CustomEditor(typeof(EntitySpawnWaveSO))]
    public class SG_SpawnWaveSOEditor : EntitySpawnWaveSOEditor
    {
        private EntitySpawnWaveSO _target;

        private void OnEnable()
        {
            _target = (EntitySpawnWaveSO)target;
        }

        public override void OnInspectorGUI()
        {
            // 1. ShooterGame 统计面板（置顶）
            DrawStatisticsPanel();

            EditorGUILayout.Space(8);

            // 2. 调用 base → 框架层摘要面板 + DrawDefaultInspector
            base.OnInspectorGUI();

            EditorGUILayout.Space(4);

            // 3. 一键复制按钮（Waves 列表底部）
            DrawCopyLastWaveButton();
        }

        // ── 统计面板 ──

        private void DrawStatisticsPanel()
        {
            if (_target.Waves == null || _target.Waves.Length == 0)
            {
                EditorGUILayout.HelpBox("暂无波次数据", MessageType.Info);
                return;
            }

            int totalWaves = _target.Waves.Length;
            int totalEnemies = 0;
            float totalDuration = 0f;
            int timerWaveCount = 0;

            for (int w = 0; w < _target.Waves.Length; w++)
            {
                var wave = _target.Waves[w];

                if (wave.TriggerMode == WaveTriggerMode.Timer)
                {
                    totalDuration += wave.TriggerDelay;
                    timerWaveCount++;
                }

                if (wave.Groups == null) continue;

                float maxGroupDuration = 0f;
                for (int g = 0; g < wave.Groups.Length; g++)
                {
                    var group = wave.Groups[g];
                    totalEnemies += group.Count;

                    float groupDuration = group.Count > 1
                        ? (group.Count - 1) * group.SpawnInterval
                        : 0f;
                    maxGroupDuration = Mathf.Max(maxGroupDuration, groupDuration);
                }

                if (wave.TriggerMode == WaveTriggerMode.Timer)
                    totalDuration += maxGroupDuration;
            }

            // 绘制面板
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 波次统计", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"共 {totalWaves} 波", GUILayout.Width(80));
            EditorGUILayout.LabelField($"共 {totalEnemies} 架敌机", GUILayout.Width(120));

            if (timerWaveCount == 0)
                EditorGUILayout.LabelField("时长不可预估（全部为 AllCleared/OnCallback）");
            else if (timerWaveCount < totalWaves)
                EditorGUILayout.LabelField($"预估 \u2265 {totalDuration:F1} 秒（仅 {timerWaveCount} 波 Timer）");
            else
                EditorGUILayout.LabelField($"预估 {totalDuration:F1} 秒");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── 一键复制最后一波 ──

        private void DrawCopyLastWaveButton()
        {
            if (_target.Waves == null || _target.Waves.Length == 0) return;

            EditorGUILayout.Space(4);

            if (GUILayout.Button("+ 复制最后一波", GUILayout.Height(28)))
            {
                CopyLastWave();
            }
        }

        private void CopyLastWave()
        {
            Undo.RecordObject(_target, "复制最后一波");

            var lastWave = _target.Waves[_target.Waves.Length - 1];

            // 深拷贝
            var newWave = new SpawnWaveEntry
            {
                TriggerMode = lastWave.TriggerMode,
                TriggerDelay = CalculateNewDelay(lastWave),
                Groups = DeepCopyGroups(lastWave.Groups),
            };

            // 追加
            var newWaves = new SpawnWaveEntry[_target.Waves.Length + 1];
            System.Array.Copy(_target.Waves, newWaves, _target.Waves.Length);
            newWaves[newWaves.Length - 1] = newWave;
            _target.Waves = newWaves;

            EditorUtility.SetDirty(_target);
            Debug.Log($"[SG] 已复制波次 → 共 {newWaves.Length} 波");
        }

        private float CalculateNewDelay(SpawnWaveEntry sourceWave)
        {
            // AllCleared/OnCallback 模式下 TriggerDelay 不生效，保持源值不自动递增
            if (sourceWave.TriggerMode != WaveTriggerMode.Timer)
                return sourceWave.TriggerDelay;

            // Timer 模式：源 Delay + 源预估时长 + 3s
            float waveDuration = 0f;
            if (sourceWave.Groups != null)
            {
                for (int g = 0; g < sourceWave.Groups.Length; g++)
                {
                    var grp = sourceWave.Groups[g];
                    float d = grp.Count > 1 ? (grp.Count - 1) * grp.SpawnInterval : 0f;
                    waveDuration = Mathf.Max(waveDuration, d);
                }
            }
            return sourceWave.TriggerDelay + waveDuration + 3f;
        }

        private SpawnGroup[] DeepCopyGroups(SpawnGroup[] source)
        {
            if (source == null) return null;

            // AT-006: 显式深拷贝（字段少，性能更好）。
            // 当框架 SpawnGroup 新增字段时此处必须同步更新！
            var copy = new SpawnGroup[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                copy[i] = new SpawnGroup
                {
                    EntityConfig = source[i].EntityConfig, // SO 引用浅拷贝（正确）
                    Camp = source[i].Camp,
                    Count = source[i].Count,
                    SpawnInterval = source[i].SpawnInterval,
                    Formation = source[i].Formation,
                    FormationParams = source[i].FormationParams, // struct 值拷贝
                };
            }
            return copy;
        }
    }
}
