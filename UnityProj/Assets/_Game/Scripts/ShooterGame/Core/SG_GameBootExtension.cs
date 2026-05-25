using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Data;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 启动扩展——提供 ProgressManager + SkillUnlockManager 静态访问点。
    /// 基于 TDD_01 §9 / V4 cloud-authoritative (memory + cloud, no local) 设计。
    ///
    /// 在 Boot 场景中由 GameBootstrapper 初始化流程完成后调用 InitProgress()。
    /// Battle 场景通过 SG_Boot.Progress / SG_Boot.UnlockManager 访问。
    /// </summary>
    public static class SG_Boot
    {
        /// <summary>跨场景访问进度管理器（静态引用，不用 DontDestroyOnLoad）</summary>
        public static SG_ProgressManager Progress { get; private set; }

        /// <summary>技能解锁管理器（V2 Sprint 5：出战准备面板使用）</summary>
        public static SkillUnlockManager UnlockManager { get; private set; }

        // 策划配置的解锁表 SO（通过 Resources 加载）
        private const string SKILL_TABLE_PATH = "ShooterGame/SkillUnlockTable";
        private const string PASSIVE_TABLE_PATH = "ShooterGame/PassiveUnlockTable";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Progress = null;
            UnlockManager = null;
        }

        /// <summary>
        /// 初始化 ProgressManager + SkillUnlockManager。
        /// 确保 GameBootstrapper.SaveSystem 已初始化后再调用。
        /// </summary>
        public static void InitProgress()
        {
            if (Progress != null)
            {
                // Already initialized — no-op.
                return;
            }

            var saveSystem = GameBootstrapper.SaveSystem;
            if (saveSystem != null)
            {
                Progress = new SG_ProgressManager(saveSystem);

                // V4 (cloud-authoritative, memory-only): after startup cloud pull completes,
                // reload progress manager so it reflects cloud state.
                if (saveSystem is CloudSaveSystem cloudSave)
                {
                    cloudSave.OnCloudPullCompleted += _ =>
                    {
                        Progress.Reload();
                        // 云端数据到达后重新创建 UnlockManager（依赖 Progress 数据）
                        InitUnlockManager();
                    };
                }

                // 立即尝试创建 UnlockManager（编辑器模式下无云端回调，直接可用）
                InitUnlockManager();
            }
            else
            {
                Debug.LogWarning("[SG_Boot] SaveSystem 未初始化，ProgressManager 创建失败");
            }
        }

        private static void InitUnlockManager()
        {
            var skillTable = Resources.Load<SkillUnlockTableSO>(SKILL_TABLE_PATH);
            var passiveTable = Resources.Load<PassiveUnlockTableSO>(PASSIVE_TABLE_PATH);

            if (skillTable == null || passiveTable == null)
            {
                Debug.LogWarning("[SG_Boot] SkillUnlockTable 或 PassiveUnlockTable 未找到，" +
                    $"路径: Resources/{SKILL_TABLE_PATH}, Resources/{PASSIVE_TABLE_PATH}");
                return;
            }

            UnlockManager = new SkillUnlockManager(skillTable, passiveTable, Progress);
        }
    }
}
