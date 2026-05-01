using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 刷怪波次配置资产。策划在 Inspector 中编排关卡波次。
    /// 路径：Assets/_Game/Configs/SpawnWave/
    /// 
    /// v2.4 变更（GD-R4-005）：WaveTriggerMode 新增 OnCallback；
    /// 新增 Loop/LoopStartWave；SpawnGroup 新增 Formation 枚举。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpawnWave", menuName = "Entity/SpawnWaveConfig")]
    public class EntitySpawnWaveSO : ScriptableObject
    {
        public SpawnWaveEntry[] Waves;

        [Header("循环模式（v2.4 新增，GD-R4-005）")]
        [Tooltip("是否在最后一波结束后从 LoopStartWave 重新开始（无限模式）")]
        public bool Loop = false;

        [Tooltip("循环起始波次索引（0-based）")]
        public int LoopStartWave = 0;
    }

    [System.Serializable]
    public struct SpawnWaveEntry
    {
        [Tooltip("本波包含的怪物组（支持单波多怪种）")]
        public SpawnGroup[] Groups;

        [Tooltip("触发模式")]
        public WaveTriggerMode TriggerMode;

        [Tooltip("Timer 模式：上一波结束后的延迟秒数")]
        public float TriggerDelay;


    }

    [System.Serializable]
    public struct SpawnGroup
    {
        [Tooltip("怪种配置")]
        public EntityConfigSO EntityConfig;

        [Tooltip("阵营")]
        public Danmaku.EnumCamp Camp;

        [Tooltip("数量")]
        public int Count;

        [Tooltip("组内逐个生成间隔（秒）")]
        public float SpawnInterval;

        [Tooltip("阵型（v2.4 新增）")]
        public SpawnFormation Formation;
    }

    /// <summary>波次触发模式</summary>
    public enum WaveTriggerMode : byte
    {
        /// <summary>上一波结束后计时器触发</summary>
        Timer = 0,
        /// <summary>当前波所有 Entity 被消灭后触发</summary>
        AllCleared = 1,
        /// <summary>由外部代码回调触发</summary>
        OnCallback = 2,
    }

    /// <summary>生成阵型（v2.4 新增，GD-R4-005）</summary>
    public enum SpawnFormation : byte
    {
        Random = 0,
        Line = 1,
        Circle = 2,
        Grid = 3,
    }
}
