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

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Effects 为空时提示（预期 Sprint 3+ 才实装 ISkillEffect）
            if (Effects == null || Effects.Length == 0)
            {
                Debug.LogWarning($"[SkillConfigSO] '{name}' Effects 为空——技能无实际效果", this);
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
