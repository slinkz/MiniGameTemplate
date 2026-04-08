using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 发射器配置——描述一个 Boss/敌人挂哪些弹幕组、怎么切换。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Spawner Profile")]
    public class SpawnerProfileSO : ScriptableObject
    {
        [Header("弹幕组列表")]
        [Tooltip("该发射器可使用的弹幕组（按顺序或按条件切换）")]
        public PatternGroupSO[] PatternGroups;

        [Header("发射间隔")]
        [Tooltip("两次弹幕组发射之间的冷却时间（秒）")]
        public float CooldownBetweenGroups = 2f;

        [Header("切换条件")]
        [Tooltip("弹幕组切换模式")]
        public SpawnerSwitchMode SwitchMode = SpawnerSwitchMode.Sequential;
    }

    /// <summary>发射器弹幕组切换模式</summary>
    public enum SpawnerSwitchMode
    {
        /// <summary>按顺序循环</summary>
        Sequential,

        /// <summary>随机选择</summary>
        Random,

        /// <summary>由外部逻辑（如 Boss FSM）控制</summary>
        External,
    }
}
