using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕组合配置——将多个 BulletPatternSO 编排在一起（多层/延迟/重复/旋转）。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Pattern Group")]
    public class PatternGroupSO : ScriptableObject
    {
        [Header("弹幕编排")]
        public PatternEntry[] Entries;

        [Header("重复")]
        [Tooltip("整组重复次数（1 = 只发一轮）")]
        public int RepeatCount = 1;

        [Tooltip("每轮之间的间隔（秒）")]
        public float RepeatInterval = 0.5f;

        [Header("角度递增")]
        [Tooltip("每轮整组的角度偏移（旋转花弹幕用）")]
        public float AngleIncrementPerRepeat = 0f;
    }

    /// <summary>
    /// 弹幕组合中的单个条目。
    /// </summary>
    [System.Serializable]
    public struct PatternEntry
    {
        [Tooltip("使用的弹幕模式")]
        public BulletPatternSO Pattern;

        [Tooltip("相对于组开始时间的发射延迟（秒）")]
        public float Delay;

        [Tooltip("覆盖 Pattern 的起始角度（-1 = 不覆盖）")]
        public float AngleOverride;

        [Tooltip("发射时初始角度指向玩家（快照 position）")]
        public bool AimAtPlayer;
    }
}
