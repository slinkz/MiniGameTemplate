using MiniGameTemplate.Pool;
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

        [Header("判定")]
        [Tooltip("扇形半角（度）")]
        public float ConeAngle = 30f;

        [Tooltip("射程")]
        public float Range = 5f;

        [Header("伤害")]
        public float DamagePerTick = 5f;

        [Tooltip("伤害间隔（秒）")]
        public float TickInterval = 0.5f;

        /// <summary>DanmakuTypeRegistry 分配的运行时索引</summary>
        [HideInInspector]
        public byte RuntimeIndex;
    }
}
