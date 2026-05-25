using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 技能配置 ScriptableObject。
    /// 一个 Skill = 一个 SkillConfigSO + N 个 ISkillEffect。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillConfig", menuName = "Entity/SkillConfig")]
    public class SkillConfigSO : ScriptableObject
    {
        [Header("基础")]
        public string DisplayName;

        [Tooltip("触发模式")]
        public SkillTriggerMode TriggerMode = SkillTriggerMode.Auto;

        [Header("瞄准")]
        [Tooltip("瞄准策略：决定技能释放方向")]
        public AimMode AimMode = AimMode.AutoAim;

        [Header("普攻标记")]
        [Tooltip("标记此技能为普攻（Slot[0]）。影响：Buff 攻速修正作用于此技能的 CD")]
        public bool IsNormalAttack;

        [Header("时间轴")]
        [Tooltip("冷却时间（秒，0=无冷却，受 Recovery 限制最小间隔）")]
        [Min(0f)]
        public float CooldownTime = 5f;

        [Tooltip("前摇时间（秒，0=瞬发）")]
        [Min(0f)]
        public float CastTime = 0f;

        [Tooltip("后摇时间（秒）")]
        [Min(0f)]
        public float RecoveryTime = 0.5f;

        [Header("效果列表")]
        [SerializeReference]
        public ISkillEffect[] Effects = System.Array.Empty<ISkillEffect>();

        [Header("V2 Sprint 3: DOT 附带")]
        [Tooltip("技能命中时附带施加的 DOT（null=不施加）。用于激光等持续命中技能。")]
        public DotConfigSO AttachedDotConfig;

        [Header("V2 Sprint 4: 伤害统计")]
        [Tooltip("伤害来源标记 ID（0=基础攻击，1~6=技能槽位，7=反击弹幕）。用于 damageStats 累加。")]
        public int SourceTagId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Effects == null || Effects.Length == 0)
                Debug.LogWarning($"[SkillConfigSO] '{name}' Effects 为空——技能无实际效果", this);

            if (IsNormalAttack)
            {
                if (TriggerMode != SkillTriggerMode.Auto)
                    Debug.LogWarning($"[SkillConfigSO] '{name}' IsNormalAttack=true 但 TriggerMode!=Auto", this);
                if (AimMode != AimMode.FixedForward)
                    Debug.LogWarning($"[SkillConfigSO] '{name}' IsNormalAttack=true 建议 AimMode=FixedForward", this);
            }
        }
#endif
    }

    public enum SkillTriggerMode : byte
    {
        /// <summary>玩家手动触发（需 WantsAttack 决策）</summary>
        Manual = 0,
        /// <summary>CD 就绪自动触发</summary>
        Auto = 1,
    }

    public enum SkillState : byte
    {
        Idle = 0,
        Casting = 1,
        Recovery = 2,
        Cooldown = 3,
    }
}
