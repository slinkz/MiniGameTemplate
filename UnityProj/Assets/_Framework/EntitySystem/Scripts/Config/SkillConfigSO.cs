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
