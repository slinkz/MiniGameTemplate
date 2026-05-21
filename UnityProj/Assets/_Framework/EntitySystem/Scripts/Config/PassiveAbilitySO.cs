using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 被动技能触发模式（V2 Sprint 3）。
    /// </summary>
    public enum PassiveTriggerMode : byte
    {
        /// <summary>CD 就绪自动激活（PA-01/02/03）</summary>
        AutoOnReady = 0,
        /// <summary>被命中时触发（PA-04 尾翼反击）</summary>
        OnHit = 1,
    }

    /// <summary>
    /// 被动技能配置 ScriptableObject（V2 Sprint 3）。
    /// 
    /// 被动技能 = CD 定时器 + 激活行为。
    /// 激活行为分两类：
    /// - Buff 桥接型（PA-01/02/03）：通过 ApplyBuff(LinkedBuff) 实现效果
    /// - 即时型（PA-04）：直接执行效果（发射弹幕等），不走 Buff
    /// 
    /// 设计原则：
    /// - SO 只存配置数据，不含逻辑
    /// - 逻辑由 PassiveComponent 驱动
    /// - 最多装备 3 个被动
    /// </summary>
    [CreateAssetMenu(fileName = "NewPassiveAbility", menuName = "Entity/PassiveAbility")]
    public class PassiveAbilitySO : ScriptableObject
    {
        [Header("基础")]
        public string DisplayName;

        [Tooltip("被动唯一 ID（5000~5999 范围，T5 工具校验）")]
        public int PassiveId;

        [Header("冷却")]
        [Tooltip("冷却时间（秒）")]
        [Min(0.1f)]
        public float CooldownTime = 5f;

        [Tooltip("触发模式")]
        public PassiveTriggerMode TriggerMode = PassiveTriggerMode.AutoOnReady;

        [Header("Buff 桥接（Buff 型被动用）")]
        [Tooltip("激活时施加的 Buff（null=即时型，不走 Buff）")]
        public BuffConfigSO LinkedBuff;

        [Header("即时效果（PA-04 等用）")]
        [Tooltip("激活时执行的技能效果（与 SkillConfigSO.Effects 共用接口）")]
        [SerializeReference]
        public ISkillEffect[] ActivateEffects = System.Array.Empty<ISkillEffect>();

        [Tooltip("即时效果的弹幕发射方向数量（PA-04 环形弹 = 8）")]
        [Min(0)]
        public int BulletDirections = 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ID 范围校验：5000~5999
            if (PassiveId > 0 && (PassiveId < 5000 || PassiveId > 5999))
            {
                Debug.LogError($"[PassiveAbilitySO] '{name}' PassiveId={PassiveId} 越界！有效范围 [5000,5999]", this);
            }

            // Buff 桥接型校验
            if (TriggerMode == PassiveTriggerMode.AutoOnReady && LinkedBuff == null
                && (ActivateEffects == null || ActivateEffects.Length == 0))
            {
                Debug.LogWarning($"[PassiveAbilitySO] '{name}' 既无 LinkedBuff 也无 ActivateEffects——被动无实际效果", this);
            }
        }
#endif
    }
}
