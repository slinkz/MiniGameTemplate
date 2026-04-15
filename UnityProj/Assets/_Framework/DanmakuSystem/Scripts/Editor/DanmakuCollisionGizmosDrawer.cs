#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Danmaku.Editor
{
    /// <summary>
    /// Scene gizmos for collision visualization.
    /// Uses [DrawGizmo] static method — does NOT need [CustomEditor] (that belongs to DanmakuSystemEditor).
    /// </summary>
    public static class DanmakuCollisionGizmosDrawer
    {
        [DrawGizmo(GizmoType.Selected)]
        private static void DrawGizmos(DanmakuSystem system, GizmoType gizmoType)
        {
            if (system == null || !system.enabled)
                return;

            var world = system.BulletWorld;
            if (world == null)
                return;

            var cores = world.Cores;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
            for (int i = 0; i < world.Capacity; i++)
            {
                if ((cores[i].Flags & BulletCore.FLAG_ACTIVE) == 0)
                    continue;

                Gizmos.DrawWireSphere(cores[i].Position, cores[i].Radius);
            }

            var lasers = system.LaserPool;
            if (lasers != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
                for (int i = 0; i < LaserPool.MAX_LASERS; i++)
                {
                    ref var laser = ref lasers.Data[i];
                    if (laser.Phase == 0)
                        continue;

                    for (int s = 0; s < laser.SegmentCount; s++)
                    {
                        ref var segment = ref laser.Segments[s];
                        Gizmos.DrawLine(segment.Start, segment.End);
                    }
                }
            }
        }
    }
}
#endif
