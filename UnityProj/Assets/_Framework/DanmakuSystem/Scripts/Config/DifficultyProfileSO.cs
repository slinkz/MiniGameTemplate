using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 难度配置——全局乘数 + 可选 Pattern 替换。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Difficulty Profile")]
    public class DifficultyProfileSO : ScriptableObject
    {
        [Header("全局乘数")]
        [Tooltip("弹丸速度乘数")]
        public float SpeedMultiplier = 1f;

        [Tooltip("弹幕数量乘数（四舍五入取整）")]
        public float CountMultiplier = 1f;

        [Tooltip("弹丸存活时间乘数")]
        public float LifetimeMultiplier = 1f;

        [Header("Pattern 替换（可选）")]
        [Tooltip("在特定难度下替换整个 PatternGroupSO")]
        public PatternOverride[] PatternOverrides;
    }

    /// <summary>难度替换条目</summary>
    [System.Serializable]
    public struct PatternOverride
    {
        [Tooltip("原始弹幕组")]
        public PatternGroupSO Original;

        [Tooltip("替换为")]
        public PatternGroupSO Replacement;
    }
}
