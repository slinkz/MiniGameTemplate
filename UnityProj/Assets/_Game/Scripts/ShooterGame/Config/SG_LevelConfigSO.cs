using MiniGameTemplate.Entity;
using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// 游戏级关卡配置——不污染框架 SO。
    /// 每关一个资产，由 BattleController._levelConfigs[] 索引。
    /// TDD_03 §1.1
    /// 
    /// 索引语义约定（TDD_03 §1.0）：
    ///   内部索引 = 0-based（_levelConfigs[]、SG_CurrentLevelIndex SO）
    ///   外部显示 = 1-based（ProgressManager 接口参数、UI 文字）
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/LevelConfig")]
    public class SG_LevelConfigSO : ScriptableObject
    {
        [Tooltip("本关波次配置")]
        public EntitySpawnWaveSO WaveConfig;

        [Tooltip("基地初始 HP 比例（0~1），1.0 = 满血")]
        [Range(0.1f, 1.0f)]
        public float BaseHpRatio = 1.0f;

        [Tooltip("前一关需要几星解锁（V1 = 0，通关即解锁）")]
        public int UnlockRequirement = 0;

        [Tooltip("基地底线 Y 坐标覆盖（正值表示启用，-1 = 使用全局默认）")]
        public float BaseLineYOverride = -1f;
    }
}
