using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// DOT（持续伤害）配置 ScriptableObject（V2 Sprint 3）。
    /// DOT = 独立于 Buff 的持续伤害层，每 Interval 秒造成一次固定伤害。
    /// ID 范围：4000~4999
    /// </summary>
    [CreateAssetMenu(fileName = "NewDotConfig", menuName = "Entity/DotConfig")]
    public class DotConfigSO : ScriptableObject
    {
        [Header("基础")]
        public string DisplayName;

        /// <summary>
        /// 唯一标识。同 ID 的 DOT 施加时刷新 Duration（不叠加）。
        /// ID 范围：4000~4999（T5 工具校验）。
        /// </summary>
        public int DotId;

        [Tooltip("DOT 标签（用于 RemoveByTag 清除）")]
        public BuffTag Tag = BuffTag.Negative;

        [Header("伤害")]
        [Tooltip("每 tick 伤害值")]
        [Min(1)]
        public int DamagePerTick = 5;

        [Tooltip("tick 间隔（秒）")]
        [Min(0.1f)]
        public float Interval = 0.5f;

        [Header("持续时间")]
        [Tooltip("总持续秒数")]
        [Min(0.1f)]
        public float Duration = 3f;

        [Header("视觉效果")]
        [Tooltip("DOT 激活时 Spawn 的 VFX Prefab（null=无视觉）")]
        public GameObject VfxPrefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ID 范围校验：4000~4999
            if (DotId > 0 && (DotId < 4000 || DotId > 4999))
            {
                Debug.LogError($"[DotConfigSO] '{name}' DotId={DotId} 越界！有效范围 [4000,4999]", this);
            }

            // Interval 合理性
            if (Interval > Duration)
            {
                Debug.LogWarning($"[DotConfigSO] '{name}' Interval({Interval}s) > Duration({Duration}s)——DOT 只会触发 0~1 次", this);
            }
        }
#endif
    }
}
