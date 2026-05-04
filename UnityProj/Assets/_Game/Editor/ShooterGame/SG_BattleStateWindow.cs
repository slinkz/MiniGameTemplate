using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Data;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// 战斗状态监视 EditorWindow——集中显示所有 SG_* SO 变量实时值 + Entity 统计。
    /// AT-002: 定时刷新（0.1s 间隔），不使用每帧 Repaint。
    /// TDD: SG_TOOLS_TDD_02 §4
    /// </summary>
    public class SG_BattleStateWindow : EditorWindow
    {
        [MenuItem("Tools/SG/战斗状态面板")]
        public static void ShowWindow()
        {
            GetWindow<SG_BattleStateWindow>("SG 战斗状态");
        }

        // SO 引用缓存
        private FloatVariable _baseHP;
        private IntVariable _currentWaveIndex;
        private IntVariable _totalWaveCount;
        private IntVariable _killCount;
        private IntVariable _totalEnemyCount;
        private IntVariable _currentLevelIndex;
        private BattleController _cachedBattleController;
        private double _nextRepaintTime;
        private const double REPAINT_INTERVAL = 0.1; // 100ms

        private void OnEnable()
        {
            CacheSOs();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        // AT-002: 定时刷新替代每帧 Repaint
        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup >= _nextRepaintTime)
            {
                _nextRepaintTime = EditorApplication.timeSinceStartup + REPAINT_INTERVAL;
                Repaint();
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            _cachedBattleController = null;
            if (state == PlayModeStateChange.EnteredPlayMode)
                CacheSOs();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅 Play Mode 可用", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("\ud83c\udfae 战斗状态", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 战斗状态（缓存引用，避免每帧 FindObjectOfType）
            if (_cachedBattleController == null)
                _cachedBattleController = FindObjectOfType<BattleController>();
            if (_cachedBattleController != null)
            {
                EditorGUILayout.LabelField("战斗状态", _cachedBattleController.CurrentState.ToString());
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("\ud83d\udcca SO 变量实时值", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawSOField("关卡索引", _currentLevelIndex);
            DrawSOField("基地 HP", _baseHP, "F2");
            DrawSOField("当前波次", _currentWaveIndex);
            DrawSOField("总波次", _totalWaveCount);
            DrawSOField("击杀数", _killCount);
            DrawSOField("总敌机数", _totalEnemyCount);

            EditorGUILayout.Space(8);

            // Entity 统计
            var mgr = EntityManagerAccessor.Instance;
            if (mgr != null)
            {
                EditorGUILayout.LabelField("\ud83d\udce6 Entity 统计", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("活跃 Entity", mgr.ActiveEntities.Count.ToString());

                // AT-010: 拆分敌方/友方/子弹
                int enemyCount = 0, allyCount = 0, bulletCount = 0;
                for (int i = 0; i < mgr.ActiveEntities.Count; i++)
                {
                    var e = mgr.ActiveEntities[i];
                    bool hasHealth = e.GetComponent(ComponentType.Health) != null;
                    if (!hasHealth && e.Camp != EnumCamp.Neutral)
                    {
                        bulletCount++;
                    }
                    else if (e.Camp == EnumCamp.Enemy) enemyCount++;
                    else if (e.Camp == EnumCamp.Player) allyCount++;
                }
                EditorGUILayout.LabelField("  敌方单位", enemyCount.ToString());
                EditorGUILayout.LabelField("  友方单位", allyCount.ToString());
                EditorGUILayout.LabelField("  子弹", bulletCount.ToString());
            }
        }

        private void DrawSOField(string label, FloatVariable so, string format = "F1")
        {
            float val = so != null ? so.Value : 0f;
            EditorGUILayout.LabelField(label, val.ToString(format));
        }

        private void DrawSOField(string label, IntVariable so)
        {
            int val = so != null ? so.Value : 0;
            EditorGUILayout.LabelField(label, val.ToString());
        }

        private void CacheSOs()
        {
            _baseHP = SG_EditorUtility.FindSOByName<FloatVariable>("SG_BaseHP");
            _currentWaveIndex = SG_EditorUtility.FindSOByName<IntVariable>("SG_CurrentWaveIndex");
            _totalWaveCount = SG_EditorUtility.FindSOByName<IntVariable>("SG_TotalWaveCount");
            _killCount = SG_EditorUtility.FindSOByName<IntVariable>("SG_KillCount");
            _totalEnemyCount = SG_EditorUtility.FindSOByName<IntVariable>("SG_TotalEnemyCount");
            _currentLevelIndex = SG_EditorUtility.FindSOByName<IntVariable>("SG_CurrentLevelIndex");
        }
    }
}
