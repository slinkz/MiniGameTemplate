using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Asset;
using MiniGameTemplate.Core;
using MiniGameTemplate.Data;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;

namespace Game.ShooterGame
{
    /// <summary>
    /// Battle 场景直跑启动器。
    /// 单一职责：当未经过 Boot 场景时，补齐 Battle 场景运行所需的最小框架初始化。
    ///
    /// 不负责游戏业务流程，不替代 GameStartupFlow。
    /// 仅保证：AssetService / ConfigManager / SaveSystem / Progress / UIManager 可用。
    /// </summary>
    public class BattleSceneBootstrapper : MonoBehaviour
    {
        [Header("Minimal Runtime Config")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private AssetConfig _assetConfig;

        private static Task _initializationTask;

        public static Task EnsureInitializedAsync()
        {
            return _initializationTask ?? Task.CompletedTask;
        }

        private void Awake()
        {
            if (_initializationTask == null)
                _initializationTask = InitializeIfNeededAsync();
        }

        private async Task InitializeIfNeededAsync()
        {
            // 1. SaveSystem / Progress
            GameBootstrapper.EnsureSaveSystemInitialized();
            SG_Boot.InitProgress();

            // 2. AssetService
            if (!AssetService.Instance.IsInitialized)
            {
                if (_assetConfig == null)
                {
                    throw new System.InvalidOperationException(
                        "[BattleSceneBootstrapper] AssetConfig is not assigned. " +
                        "Direct-running Battle scene requires a valid AssetConfig.");
                }

                await AssetService.Instance.InitializeAsync(_assetConfig);
                GameLog.Log("[BattleSceneBootstrapper] AssetService initialized for direct Battle scene run.");
            }

            // 3. Config tables
            if (ConfigManager.Tables == null)
            {
                await ConfigManager.InitializeAsync();
                GameLog.Log("[BattleSceneBootstrapper] ConfigManager initialized for direct Battle scene run.");
            }

            // 4. Basic app settings (keep minimal and safe)
            if (_gameConfig != null)
            {
                Application.targetFrameRate = _gameConfig.TargetFrameRate;
                Application.runInBackground = _gameConfig.RunInBackground;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            // 5. UIManager singleton
            _ = UIManager.Instance;

            // 6. Progress may depend on SaveSystem only, but call again to be explicit/idempotent
            SG_Boot.InitProgress();

            GameLog.Log("[BattleSceneBootstrapper] Minimal runtime initialization completed.");
        }
    }
}
