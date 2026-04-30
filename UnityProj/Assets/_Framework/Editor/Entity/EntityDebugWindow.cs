#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Entity 系统 Play Mode 调试窗口。
    /// ET-008: MenuItem: Window/Entity/Debug Overview
    /// 
    /// Phase 1 功能：
    /// 1. EntityManager 概览：活跃 Entity 总数 / 各 Pool 使用率 / PendingDespawn 队列长度
    /// 2. Entity 列表表格：Id | ConfigName | HP | Position | AI 当前 Action
    /// 3. WF-002: "Restart All Waves" 按钮
    /// 4. WF-004: EntityManagerAccessor.Instance == null 时显示 HelpBox
    /// </summary>
    public class EntityDebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _filterConfig = "";

        [MenuItem("Window/Entity/Debug Overview")]
        public static void ShowWindow() => GetWindow<EntityDebugWindow>("Entity Debug");

        private void OnInspectorUpdate()
        {
            // Play Mode 下每帧刷新
            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在 Play Mode 下可用。", MessageType.Info);
                return;
            }

            // WF-004: 区分"未初始化"和"Play Mode 正常"
            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null)
            {
                EditorGUILayout.HelpBox(
                    "Entity System 未初始化。请确认场景中有 EntitySystemBootstrap 组件。",
                    MessageType.Warning);
                return;
            }

            // ── 概览 ──
            EditorGUILayout.LabelField("Entity Manager 概览", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"活跃 Entity：{mgr.ActiveCount}");

            // Pool 使用率
            var pools = mgr.Pools;
            if (pools != null && pools.Count > 0)
            {
                EditorGUILayout.LabelField("Pool 使用率：");
                EditorGUI.indentLevel++;
                foreach (var kvp in pools)
                {
                    var config = kvp.Key;
                    var pool = kvp.Value;
                    string name = config != null ? config.name : "???";
                    EditorGUILayout.LabelField($"  {name}: {pool.ActiveCount}/{pool.Capacity}");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // WF-002: Restart All Waves 按钮
            if (GUILayout.Button("🔄 Restart All Waves", GUILayout.Height(30)))
            {
                mgr.DespawnAll();
                Debug.Log("[EntityDebug] DespawnAll 完成。（注：Spawner 重启需 P1.10 实现）");
            }

            EditorGUILayout.Space(8);

            // ── Entity 列表 ──
            EditorGUILayout.LabelField("Entity 列表", EditorStyles.boldLabel);
            _filterConfig = EditorGUILayout.TextField("筛选 ConfigName", _filterConfig);
            EditorGUILayout.Space(4);

            // 表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("ID", GUILayout.Width(40));
            GUILayout.Label("Config", GUILayout.Width(120));
            GUILayout.Label("HP", GUILayout.Width(80));
            GUILayout.Label("Position", GUILayout.Width(120));
            GUILayout.Label("AI Action", GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            var entities = mgr.ActiveEntities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;

                string configName = entity.ConfigSO != null ? entity.ConfigSO.name : "???";

                // 筛选
                if (!string.IsNullOrEmpty(_filterConfig) &&
                    !configName.Contains(_filterConfig, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(entity.Id.Value.ToString(), GUILayout.Width(40));
                GUILayout.Label(configName, GUILayout.Width(120));

                // HP
                var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                string hpStr = health != null ? $"{health.CurrentHp}/{health.MaxHp}" : "-";
                GUILayout.Label(hpStr, GUILayout.Width(80));

                // Position
                GUILayout.Label($"({entity.Position.x:F1}, {entity.Position.y:F1})", GUILayout.Width(120));

                // AI Action（显示当前 Decision）
                var aiComp = entity.GetComponent(ComponentType.AI) as AIComponent;
                string aiStr = "-";
                if (aiComp != null)
                {
                    var decision = aiComp.GetDecision();
                    aiStr = decision.WantsAttack ? "Attack" : (decision.MoveDirection.sqrMagnitude > 0.01f ? "Moving" : "Idle");
                }
                GUILayout.Label(aiStr, GUILayout.MinWidth(100));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
