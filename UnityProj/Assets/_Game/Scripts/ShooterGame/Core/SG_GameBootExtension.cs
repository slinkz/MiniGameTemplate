using UnityEngine;
using MiniGameTemplate.Core;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 启动扩展——提供 ProgressManager 静态访问点。
    /// 基于 TDD_01 §9 设计。
    /// 
    /// 在 Boot 场景中由 GameBootstrapper 初始化流程完成后调用 InitProgress()。
    /// Battle 场景通过 SG_Boot.Progress 访问进度管理器。
    /// </summary>
    public static class SG_Boot
    {
        /// <summary>跨场景访问进度管理器（静态引用，不用 DontDestroyOnLoad）</summary>
        public static SG_ProgressManager Progress { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Progress = null;
        }

        /// <summary>
        /// 初始化 ProgressManager。
        /// 确保 GameBootstrapper.SaveSystem 已初始化后再调用。
        /// </summary>
        public static void InitProgress()
        {
            if (Progress != null) return;
            var saveSystem = GameBootstrapper.SaveSystem;
            if (saveSystem != null)
            {
                Progress = new SG_ProgressManager(saveSystem);
            }
            else
            {
                Debug.LogWarning("[SG_Boot] SaveSystem 未初始化，ProgressManager 创建失败");
            }
        }
    }
}
