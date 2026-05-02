using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Buff 配置 ScriptableObject。
    /// Buff = 持续时间 + 属性修正（乘法叠加）。
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuffConfig", menuName = "Entity/BuffConfig")]
    public class BuffConfigSO : ScriptableObject
    {
        [Header("基础")]
        public string DisplayName;

        /// <summary>
        /// 唯一标识。同 ID 的 Buff 施加时刷新（不叠层）。
        /// 
        /// ⚠️ BuffId 唯一性由策划保证（ATK-006）。
        /// 推荐命名规范：{类型前缀}{三位数字}
        ///   - buff_speed_001 → BuffId = 1001
        ///   - buff_atk_002   → BuffId = 2002
        ///   - debuff_slow_001 → BuffId = 3001
        /// </summary>
        public int BuffId;

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
    }
}
