#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Entity 碰撞体 + HP 标签 Gizmo 绘制器。
    /// 
    /// ET-003: 静态类 + [InitializeOnLoad] + SceneView.duringSceneGui。
    /// Play Mode：遍历 EntityManager 活跃 Entity 绘制碰撞体（圆形/矩形）和 HP。
    /// Edit Mode：EntitySpawnPoint 的 OnDrawGizmos 已自带区域可视化。
    /// 
    /// 零运行时开销——全部代码在 Editor asmdef 中，不打包。
    /// v2.6 WF-004：EntityManagerAccessor.Instance == null 时显示提示。
    /// </summary>
    [InitializeOnLoad]
    public static class EntityGizmoDrawer
    {
        // 缓存矩形碰撞体绘制用顶点数组，避免每帧 GC
        private static readonly Vector3[] _rectCorners = new Vector3[5];
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

                // 绘制碰撞体（根据 HitboxType 画圆或矩形）
                Handles.color = color;
                var cfg = entity.ConfigSO;
                float labelOffsetY;

                if (cfg != null && cfg.HitboxType == Danmaku.HitboxShape.Rect)
                {
                    // 矩形碰撞体：画 AABB 线框
                    float hw = cfg.CollisionHalfWidth;
                    float hh = cfg.CollisionHalfHeight;
                    Vector3 center = new Vector3(entity.Position.x, entity.Position.y, 0f);
                    _rectCorners[0] = center + new Vector3(-hw, -hh, 0f);
                    _rectCorners[1] = center + new Vector3( hw, -hh, 0f);
                    _rectCorners[2] = center + new Vector3( hw,  hh, 0f);
                    _rectCorners[3] = center + new Vector3(-hw,  hh, 0f);
                    _rectCorners[4] = _rectCorners[0]; // 闭合
                    Handles.DrawPolyLine(_rectCorners);
                    labelOffsetY = hh + 0.2f;
                }
                else
                {
                    // 圆形碰撞体
                    float radius = cfg != null ? cfg.CollisionRadius : 0.3f;
                    Handles.DrawWireDisc(
                        new Vector3(entity.Position.x, entity.Position.y, 0f),
                        Vector3.forward,
                        radius);
                    labelOffsetY = radius + 0.2f;
                }

                // HP 标签
                var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                if (health != null)
                {
                    int maxHp = cfg != null ? cfg.MaxHp : 0;
                    Handles.Label(
                        new Vector3(entity.Position.x, entity.Position.y + labelOffsetY, 0f),
                        $"HP: {health.CurrentHp}/{maxHp}",
                        EditorStyles.boldLabel);
                }
            }
        }
    }
}
#endif
