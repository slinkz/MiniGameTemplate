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

        [Tooltip("阵型参数")]
        public FormationConfig FormationParams;
    }

    /// <summary>
    /// 阵型可配参数。不同阵型使用不同字段：
    /// - Random：Radius（散布半径，0=使用 SpawnPoint.AreaRadius）、Jitter
    /// - Line：Spacing（间距）、Angle（排列角度，0=水平）、Jitter
    /// - Circle：Radius（圆半径，0=使用 SpawnPoint.AreaRadius）、Angle（起始角度偏移）、Jitter
    /// - Grid：Spacing（格间距）、Columns（列数，0=自动取 √Count）、Jitter
    /// </summary>
    [System.Serializable]
    public struct FormationConfig
    {
        [Tooltip("Line/Grid：相邻单位间距（世界单位）。0 = 使用 SpawnPoint.AreaRadius 自动计算")]
        public float Spacing;

        [Tooltip("Line：排列角度（度）。0=水平、90=垂直。Circle：起始角度偏移")]
        public float Angle;

        [Tooltip("Circle/Random：半径（世界单位）。0 = 使用 SpawnPoint.AreaRadius")]
        public float Radius;

        [Tooltip("Grid：列数。0 = 自动取 ceil(√Count)")]
        public int Columns;

        [Tooltip("各阵型通用：每个单位附加随机偏移量（噪声）")]
        public float Jitter;
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
