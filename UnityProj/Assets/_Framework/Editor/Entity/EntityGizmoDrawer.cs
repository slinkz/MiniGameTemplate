#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Entity 碰撞圈 + HP 标签 Gizmo 绘制器。
    /// 
    /// ET-003: 静态类 + [InitializeOnLoad] + SceneView.duringSceneGui。
    /// Play Mode：遍历 EntityManager 活跃 Entity 绘制碰撞圈和 HP。
    /// Edit Mode：EntitySpawnPoint 的 OnDrawGizmos 已自带区域可视化。
    /// 
    /// 零运行时开销——全部代码在 Editor asmdef 中，不打包。
    /// v2.6 WF-004：EntityManagerAccessor.Instance == null 时显示提示。
    /// </summary>
    [InitializeOnLoad]
    public static class EntityGizmoDrawer
    {
        static EntityGizmoDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!Application.isPlaying) return;

            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null)
            {
                // WF-004: Scene View 中央提示
                Handles.BeginGUI();
                GUILayout.Label(
                    "Entity System 未初始化 — 请在场景中添加 EntitySystemBootstrap",
                    EditorStyles.helpBox);
                Handles.EndGUI();
                return;
            }

            var entities = mgr.ActiveEntities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;

                // 阵营颜色：Enemy=红, Player=绿, Neutral=灰
                Color color = entity.Camp switch
                {
                    Danmaku.EnumCamp.Enemy => Color.red,
                    Danmaku.EnumCamp.Player => Color.green,
                    _ => Color.gray
                };

                float radius = entity.ConfigSO != null ? entity.ConfigSO.CollisionRadius : 0.3f;

                // 绘制碰撞圈
                Handles.color = color;
                Handles.DrawWireDisc(
                    (Vector3)new Vector3(entity.Position.x, entity.Position.y, 0f),
                    Vector3.forward,
                    radius);

                // HP 标签
                var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                if (health != null)
                {
                    int maxHp = entity.ConfigSO != null ? entity.ConfigSO.MaxHp : 0;
                    Handles.Label(
                        new Vector3(entity.Position.x, entity.Position.y + radius + 0.2f, 0f),
                        $"HP: {health.CurrentHp}/{maxHp}",
                        EditorStyles.boldLabel);
                }
            }
        }
    }
}
#endif
