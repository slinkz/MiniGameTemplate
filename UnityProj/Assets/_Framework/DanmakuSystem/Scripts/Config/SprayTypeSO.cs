using MiniGameTemplate.Pool;
using MiniGameTemplate.VFX;
using UnityEngine;


namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 喷雾类型配置——视觉、判定范围、伤害。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spray Type")]
    public class SprayTypeSO : ScriptableObject
    {
        [Header("视觉")]
        [Tooltip("对象池 ParticleSystem 预制件")]
        public PoolDefinition ParticleEffectPrefab;

        [Tooltip("Sprite Sheet VFX 类型（与 ParticleEffectPrefab 二选一，DEC-006）")]
        public VFXTypeSO SprayVFXType;

        [Header("判定")]
        [Tooltip("喷雾阵营（决定与哪些目标碰撞）")]
        public BulletFaction Faction = BulletFaction.Enemy;

        [Tooltip("扇形半角（度）")]
        public float ConeAngle = 30f;

        [Tooltip("射程")]
        public float Range = 5f;

        [Header("伤害")]
        public float DamagePerTick = 5f;

        [Tooltip("伤害间隔（秒）")]
        public float TickInterval = 0.5f;

        [Header("碰撞响应 — 障碍物")]
        [Tooltip("喷雾碰到障碍物时的行为")]
        public SprayObstacleResponse OnHitObstacle = SprayObstacleResponse.Ignore;

        [Header("碰撞响应 — 屏幕边缘")]
        [Tooltip("Origin 越界时是否回收喷雾")]
        public bool RecycleOnOriginOutOfBounds = true;

        [Tooltip("Origin 越界回收的边缘余量（世界单位）")]
        public float ScreenEdgeRecycleMargin = 1f;

        /// <summary>DanmakuTypeRegistry 分配的运行时索引</summary>
        [HideInInspector]
        public byte RuntimeIndex;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (TickInterval < 0.001f) TickInterval = 0.001f;
            if (ScreenEdgeRecycleMargin < 0f) ScreenEdgeRecycleMargin = 0f;
            Editor.DanmakuEditorRefreshCoordinator.MarkDirty(this);
        }
#endif
    }
}

