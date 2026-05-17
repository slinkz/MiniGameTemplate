using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Data;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 启动扩展——提供 ProgressManager 静态访问点。
    /// 基于 TDD_01 §9 / V3 cloud-authoritative 设计。
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
        /// V3: 如果 SaveSystem 是 CloudSaveSystem，注册云端拉取完成后的 Reload 回调。
        /// </summary>
        public static void InitProgress()
        {
            if (Progress != null)
            {
                // Already initialized — no-op.
                // V3: no Reload needed here; cloud pull happens at startup only.
                return;
            }

            var saveSystem = GameBootstrapper.SaveSystem;
            if (saveSystem != null)
            {
                Progress = new SG_ProgressManager(saveSystem);

                // V3 (cloud-authoritative): after startup cloud pull completes,
                // reload progress manager so it reflects cloud state.
                if (saveSystem is CloudSaveSystem cloudSave)
                {
                    cloudSave.OnCloudPullCompleted += _ => Progress.Reload();
                }
            }
            else
            {
                Debug.LogWarning("[SG_Boot] SaveSystem 未初始化，ProgressManager 创建失败");
            }
        }
    }
}
