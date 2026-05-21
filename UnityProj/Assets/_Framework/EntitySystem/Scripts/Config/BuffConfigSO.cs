using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Buff 配置 ScriptableObject。
    /// V1：持续时间 + 属性修正（乘法叠加）。
    /// V2 Sprint 3：+ Tag/StackMode/BulletCountModifier/VFX。
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuffConfig", menuName = "Entity/BuffConfig")]
    public class BuffConfigSO : ScriptableObject
    {
        [Header("基础")]
        public string DisplayName;

        /// <summary>
        /// 唯一标识。同 ID 的 Buff 施加时行为由 StackMode 决定。
        /// 
        /// ⚠️ BuffId 唯一性由 T5 工具校验（ATK-006）。
        /// ID 范围：Buff 1000~2999 / Debuff 3000~3999
        /// </summary>
        public int BuffId;

        // ── V2 Sprint 3 新增 ──

        [Header("V2 分类与叠加")]
        [Tooltip("Buff 标签分类（用于 RemoveByTag 批量清除）")]
        public BuffTag Tag = BuffTag.Positive;

        [Tooltip("同 ID 再次施加时的行为：Refresh=刷新时间 / Stack=叠层")]
        public StackMode StackMode = StackMode.Refresh;

        [Tooltip("最大叠加层数（仅 StackMode=Stack 时有效）")]
        [Min(1)]
        public int MaxStacks = 1;

        [Header("持续时间")]
        [Tooltip("持续秒数。0=永久Buff（不会自动过期，需通过 RemoveBuff 手动移除）")]
        [Min(0f)]
        public float Duration = 5f;

        [Header("属性修正（乘法：最终值 = 基础值 × Modifier）")]
        [Tooltip("移速倍率（1=不变，0.5=减速50%，2=加速100%）")]
        public float MoveSpeedModifier = 1f;

        [Tooltip("攻击间隔倍率（1=不变，0.5=攻速翻倍，2=减速50%）")]
        public float AttackIntervalModifier = 1f;

        [Tooltip("受伤倍率（1=不变，0.5=减伤50%，2=受伤翻倍）")]
        public float DamageTakenModifier = 1f;

        [Tooltip("子弹数修正倍率（1=不变，2=双倍子弹数）。V2 Sprint 3 新增。")]
        public float BulletCountModifier = 1f;

        // ── V2 被动 Buff 扩展字段 ──

        [Header("V2 被动/特殊效果")]
        [Tooltip("是否启用穿透标志（被动 PA-01 用）")]
        public bool GrantsPierce;

        [Tooltip("暴击率加成（绝对值，如 0.2 = +20%）（被动 PA-02 用）")]
        public float CritRateBonus;

        [Tooltip("暴击倍率覆盖（>0 时覆盖 EntityConfigSO.CritDamageMultiplier）")]
        public float CritMultiplierOverride;

        [Tooltip("拾取半径倍率（1=不变，2=双倍拾取范围）（被动 PA-03 用）")]
        public float PickupRadiusModifier = 1f;

        // ── VFX ──

        [Header("视觉效果")]
        [Tooltip("Buff 激活时 Spawn 的 VFX Prefab（从池中获取）。为 null 则无视觉。")]
        public GameObject VfxPrefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ID 范围校验：Buff 1000~2999 / Debuff 3000~3999
            if (BuffId > 0)
            {
                bool isValidBuff = BuffId >= 1000 && BuffId <= 2999;
                bool isValidDebuff = BuffId >= 3000 && BuffId <= 3999;
                if (!isValidBuff && !isValidDebuff)
                {
                    Debug.LogError($"[BuffConfigSO] '{name}' BuffId={BuffId} 越界！有效范围：Buff [1000,2999] / Debuff [3000,3999]", this);
                }
            }

            // StackMode=Stack 时 MaxStacks 必须 > 1
            if (StackMode == StackMode.Stack && MaxStacks <= 1)
            {
                Debug.LogWarning($"[BuffConfigSO] '{name}' StackMode=Stack 但 MaxStacks={MaxStacks}，应 > 1", this);
            }

            // Duration=0（永久 Buff）警告
            if (Duration <= 0f && Tag == BuffTag.Negative)
            {
                Debug.LogWarning($"[BuffConfigSO] '{name}' 是永久 Debuff (Duration=0)——通常不合理", this);
            }
        }
#endif
    }
}
