using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Navigation;
using MiniGameTemplate.Core;
using MiniGameTemplate.Platform;
using MiniGameTemplate.UI;
using EntityClass = MiniGameTemplate.Entity.Entity;
#if UNITY_EDITOR
using Unity.Profiling;
#endif

namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗场景唯一编排指挥。单一职责：状态机驱动 + 子系统协调。
    /// 不直接操作 Entity（委托给 BaseLineDetector / Spawner 等）。
    /// TDD_02 §1.2
    /// </summary>
    public class BattleController : MonoBehaviour
    {

#if UNITY_EDITOR
        // ── ProfilerMarker ──
        private static readonly ProfilerMarker s_TickPlayingMarker =
            new ProfilerMarker("SG.BattleController.TickPlaying");
        private static readonly ProfilerMarker s_BaseLineDetectMarker =
            new ProfilerMarker("SG.BaseLineDetector.Tick");

        // ── Debug 字段（Inspector 可视化）──
        [Header("== DEBUG (编辑器专用) ==")]
        [SerializeField] private string _debug_CurrentState;
        [SerializeField] private float _debug_StateTimer;
        [SerializeField] private int _debug_AliveEnemyCount;
#endif

        [Header("关卡配置")]
        [SerializeField] private SG_LevelConfigSO[] _levelConfigs;


        [Header("SO 变量（输出）")]
        [SerializeField] private FloatVariable _baseHP;
        [SerializeField] private IntVariable _currentWaveIndex;
        [SerializeField] private IntVariable _totalWaveCount;
        [SerializeField] private IntVariable _killCount;
        [SerializeField] private IntVariable _totalEnemyCount;

        [Header("子系统引用")]
        [SerializeField] private CameraShaker _cameraShaker;
        [SerializeField] private ScreenShakeConfigSO _shakeConfig;
        [SerializeField] private float _introDuration = 1.5f;
        [SerializeField] private float _victoryDelay = 0.5f;
        [SerializeField] private float _baseLineY = -7f;

        [Header("Entity 配置")]
        [SerializeField] private EntityConfigSO _baseEntityConfig;
        [SerializeField] private EntityConfigSO _playerEntityConfig;
        [SerializeField] private EntitySpawnPoint _spawnPoint;

        [Header("UI Controller 引用（Inspector 拖拽）")]
        [SerializeField] private MonoBehaviour _hudControllerRef;
        [SerializeField] private MonoBehaviour _pausePanelRef;
        [SerializeField] private MonoBehaviour _victoryPanelRef;
        [SerializeField] private MonoBehaviour _defeatPanelRef;
        [SerializeField] private MonoBehaviour _joystickControllerRef;
        [SerializeField] private SG_PlayerInputBridge _playerInputBridge;

        // 运行时状态
        public BattleState CurrentState { get; private set; }
        private float _stateTimer;
        private SG_LevelConfigSO _currentLevel;
        private EntitySpawnWaveSO _runtimeWaveConfig;
        private int? _launchLevelIndex;
        private bool _battleStartedByFlow;
        private BaseLineDetector _baseLineDetector;


        private EntitySystemBootstrap _entityBootstrap;
        private EntityClass _baseEntity;
        private EntityClass _playerEntity;
        private SG_ProgressManager _progressManager;
        private int _displayWaveIndex;

        // V2 Sprint 1: 伤害转发链路
        private InvincibilityModifier _invincibilityModifier;
        private DamageRedirectModifier _damageRedirectModifier;


        // UI Controller 接口（通过 Init 或 GetComponent 动态绑定）
        private IBattleHUDController _hudController;
        private IPausePanelController _pausePanel;
        private IVictoryPanelController _victoryPanel;
        private IDefeatPanelController _defeatPanel;
        private IJoystickController _joystickController;
        private bool _isHandlingVictory;

        // ── 公共接口 ──

#if UNITY_EDITOR
        public void DebugForceVictory() => EnterState(BattleState.Victory);
        public void DebugForceDefeat() => EnterState(BattleState.Defeat);
        public void DebugRetryBattle() => StartCoroutine(HandleRetry());
#endif

        // ── 生命周期 ──

        private void Awake()
        {
            _baseLineDetector = new BaseLineDetector();
            _entityBootstrap = FindObjectOfType<EntitySystemBootstrap>();


            // 获取 UI Controller 接口

            if (_hudControllerRef != null)
                _hudController = _hudControllerRef as IBattleHUDController;
            if (_pausePanelRef != null)
                _pausePanel = _pausePanelRef as IPausePanelController;
            if (_victoryPanelRef != null)
                _victoryPanel = _victoryPanelRef as IVictoryPanelController;
            if (_defeatPanelRef != null)
                _defeatPanel = _defeatPanelRef as IDefeatPanelController;
            if (_joystickControllerRef != null)
                _joystickController = _joystickControllerRef as IJoystickController;
        }

        private async void Start()
        {
            try
            {
                // 确保后台也能推进帧（Editor 调试 + 微信小游戏后台兼容）
                Application.runInBackground = true;

                // 若直跑 Battle 场景，先等待最小运行时初始化完成。
                await BattleSceneBootstrapper.EnsureInitializedAsync();

                // 防御性初始化：若从 Boot 场景启动则已初始化，直接跳场景测试时兜底
                SG_Boot.InitProgress();
                _progressManager = SG_Boot.Progress;

                // 等待一帧，给 AppFlowNavigator.OnFlowEnter → StartBattle() 时机
                await System.Threading.Tasks.Task.Yield();

                // 如果 FlowHandler 已经触发了 StartBattle()，则不重复初始化
                if (_battleStartedByFlow) return;

                // 直跑 Battle 场景：自行启动战斗
                await InitBattleAsync();
                EnterState(BattleState.Intro);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _stateTimer += dt;

            switch (CurrentState)
            {
                case BattleState.Intro:
                    TickIntro(dt);
                    break;
                case BattleState.Playing:
                    TickPlaying(dt);
                    break;
                case BattleState.Victory:
                case BattleState.Defeat:
                    break;
            }
        }

        private void OnDestroy()
        {
            // 取消订阅
            var mgr = EntityManagerAccessor.Instance;
            if (mgr != null)
                mgr.OnDespawned -= OnEntityDespawned;

            // 清理暂停按钮事件绑定
            var hudView = _hudController?.GetView();
            var btnPause = hudView?.GetChild("btn_pause");
            if (btnPause != null)
                btnPause.onClick.Remove(OnPauseButtonClicked);

            // 清理弹幕系统（DontDestroyOnLoad，不随场景销毁）
            if (DanmakuSystem.Instance != null)
                DanmakuSystem.Instance.ClearAll();
        }

        // ── 初始化 ──

        private async System.Threading.Tasks.Task InitBattleAsync()
        {
            // 1. 解析本次战斗启动上下文
            ResolveBattleContext();

            // 2. 设置波次计数 SO（1-based 展示）

            int totalWaves = _runtimeWaveConfig != null && _runtimeWaveConfig.Waves != null
                ? _runtimeWaveConfig.Waves.Length
                : 0;


            _totalWaveCount.SetValue(totalWaves);
            _currentWaveIndex.SetValue(totalWaves > 0 ? 1 : 0);
            _displayWaveIndex = totalWaves > 0 ? 1 : 0;


            // 3. 计算总敌机数
            int totalEnemy = 0;
            if (_runtimeWaveConfig != null && _runtimeWaveConfig.Waves != null)
            {
                foreach (var wave in _runtimeWaveConfig.Waves)

                {
                    if (wave.Groups == null) continue;
                    foreach (var group in wave.Groups)
                    {
                        if (group.Count > 0)
                            totalEnemy += group.Count;
                    }
                }
            }
            _totalEnemyCount.SetValue(totalEnemy);

            // 4. 重置击杀计数
            _killCount.SetValue(0);

            // 5. Spawn 基地 Entity
            SpawnBase();

            // 6. Spawn 玩家飞机 Entity
            SpawnPlayer();

            // 6b. V2 Sprint 1: 注入伤害转发链路
            SetupDamageRedirectChain();

            // 6c. 订阅基地 OnDamaged 事件 → 同步 BaseHP SO + 死亡判定
            _baseEntity.EventBus.Subscribe<OnDamaged>(OnBaseDamaged);

            // 7. 初始化底线检测器（ET-003: 只传检测所需参数）
            float effectiveBaseLineY = _currentLevel.BaseLineYOverride >= 0
                ? -_currentLevel.BaseLineYOverride : _baseLineY;
            _baseLineDetector.Init(effectiveBaseLineY, _baseEntity, _baseHP);

            // 8. 设置 BaseHP SO 初始值
            _baseHP.SetValue(1.0f);

            // 9. 初始化 UI Controllers（ET-007）
            // 异步加载 SG_Battle + SG_Popup FairyGUI 包 → 再创建 HUD
            if (_hudController != null)
            {
                await _hudController.ShowAsync();
                _hudController.ForceRefresh();
            }
            await MiniGameTemplate.UI.UIPackageLoader.AddPackageAsync("SG_Popup", SG_Popup.SG_PopupBinder.BindAll);
            var hudView = _hudController?.GetView();
            _joystickController?.Init(hudView);

            // 暂停按钮：从 HUD view 中获取并绑定（TDD_04 §4 + UI Design §2.3）
            if (hudView != null)
            {
                var btnPause = hudView.GetChild("btn_pause");
                if (btnPause != null)
                    btnPause.onClick.Add(OnPauseButtonClicked);
            }

            _defeatPanel?.BindEvents(
                () => StartCoroutine(HandleRetry()),
                () => StartCoroutine(HandleDefeatQuit()));
            _victoryPanel?.BindEvents(HandleVictoryConfirmAsync);
            _pausePanel?.BindEvents(
                OnResumeFromPause,
                () => StartCoroutine(HandlePauseQuit()));

            // 10. PlayerInputBridge 初始化
            _playerInputBridge.Init(_playerEntity);

            // 11. 订阅击杀计数
            var mgr = EntityManagerAccessor.Instance;
            if (mgr != null)
                mgr.OnDespawned += OnEntityDespawned;
        }

        // ── Entity Spawn ──

        private void SpawnBase()
        {
            var mgr = EntityManagerAccessor.Instance;
            _baseEntity = mgr.Spawn(
                _baseEntityConfig,
                new Vector2(0, _baseLineY),
                0f);
            // Camp 由 EntityConfigSO.Camp 自动设置，SG_Base 配置为 Player 阵营

            var health = _baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
            if (health != null)
            {
                int maxHp = _baseEntityConfig.MaxHp;
                int initialHp = Mathf.RoundToInt(maxHp * _currentLevel.BaseHpRatio);
                health.SetHp(initialHp);
            }
        }

        private void SpawnPlayer()
        {
            var mgr = EntityManagerAccessor.Instance;
            _playerEntity = mgr.Spawn(
                _playerEntityConfig,
                new Vector2(0, -5f),
                270f); // 朝上（竖版飞机默认朝上）
            // Camp 由 EntityConfigSO.Camp 自动设置，SG_Player 配置为 Player 阵营
        }

        // ── V2 Sprint 1: 伤害转发链路 ──

        /// <summary>
        /// 为玩家飞机注入 IDamageModifier 链：
        /// InvincibilityModifier(priority=-1) → DamageRedirectModifier(priority=0)
        /// 并订阅飞机的 OnCollisionHit 事件，将弹幕碰撞转化为 TakeDamage 调用。
        /// TDD S1.3
        /// </summary>
        private void SetupDamageRedirectChain()
        {
            var playerHealth = _playerEntity.GetComponent(ComponentType.Health) as HealthComponent;
            if (playerHealth == null) return;

            // 1. 创建并注册 InvincibilityModifier
            _invincibilityModifier = new InvincibilityModifier();
            _invincibilityModifier.SetHealthComponent(playerHealth);
            playerHealth.AddModifier(_invincibilityModifier);

            // 2. 创建并注册 DamageRedirectModifier
            _damageRedirectModifier = new DamageRedirectModifier();
            _damageRedirectModifier.SetBaseEntity(_baseEntity);
            playerHealth.AddModifier(_damageRedirectModifier);

            // 3. 订阅飞机的 OnCollisionHit 事件 → TakeDamage
            _playerEntity.EventBus.Subscribe<OnCollisionHit>(OnPlayerCollisionHit);
        }

        /// <summary>
        /// 飞机被弹丸命中回调。构造 DamageContext → HealthComponent.TakeDamage。
        /// 伤害流程：TakeDamage → InvincibilityModifier → DamageRedirectModifier → 基地扣血。
        /// </summary>
        private void OnPlayerCollisionHit(OnCollisionHit evt)
        {
            var playerHealth = _playerEntity?.GetComponent(ComponentType.Health) as HealthComponent;
            if (playerHealth == null) return;

            var ctx = evt.Context;
            playerHealth.TakeDamage(ref ctx);

            // BaseHP SO 同步已由 OnBaseDamaged 统一处理
        }

        /// <summary>
        /// 基地受伤回调（无论来源：底线突破 / 弹丸直击 / 飞机伤害转发）。
        /// 同步 BaseHP SO 变量 + 检查死亡 → Defeat。
        /// </summary>
        private void OnBaseDamaged(OnDamaged evt)
        {
            var baseHealth = _baseEntity?.GetComponent(ComponentType.Health) as HealthComponent;
            if (baseHealth == null) return;

            _baseHP.SetValue(baseHealth.HpRatio);

            if (baseHealth.IsDead && CurrentState == BattleState.Playing)
            {
                EnterState(BattleState.Defeat);
            }
        }

        // ── 状态转换 ──

        private void EnterState(BattleState newState)
        {
#if UNITY_EDITOR
            Debug.Log($"[SG_Battle] State → {newState} (time={Time.time:F2}s, frame={Time.frameCount})");
#endif
            CurrentState = newState;
            _stateTimer = 0f;

            switch (newState)
            {
                case BattleState.Intro:
                    SetInputEnabled(false);
                    SetSpawnerEnabled(false);
                    break;

                case BattleState.Playing:
                    SetInputEnabled(true);
                    SetSpawnerEnabled(true);
                    break;

                case BattleState.Victory:
                    SetSpawnerEnabled(false);
                    SetInputEnabled(false);
                    SetBattleTimePaused(true);
                    StartCoroutine(ShowVictoryAfterDelay(_victoryDelay));
                    break;

                case BattleState.Defeat:
                    SetSpawnerEnabled(false);
                    SetInputEnabled(false);
                    SetBattleTimePaused(true);
                    _defeatPanel?.Show();
                    break;
            }
        }

        // ── Tick 逻辑 ──

        private void TickIntro(float dt)
        {
            if (_stateTimer >= _introDuration)
            {
                EnterState(BattleState.Playing);
            }
        }

        private void TickPlaying(float dt)
        {
#if UNITY_EDITOR
            using (s_TickPlayingMarker.Auto())
#endif
            {
                // 1. 底线检测
#if UNITY_EDITOR
                bool baseDead;
                using (s_BaseLineDetectMarker.Auto())
                {
                    baseDead = _baseLineDetector.Tick(EntityManagerAccessor.Instance);
                }
#else
                bool baseDead = _baseLineDetector.Tick(EntityManagerAccessor.Instance);
#endif

                // 1b. 底线突破反馈（ET-003: 由 BattleController 触发，不污染 Detector）
                if (_baseLineDetector.HasBreachThisFrame)
                {
                    _cameraShaker.Shake(
                        _shakeConfig.BaseHitDuration,
                        _shakeConfig.BaseHitIntensity,
                        _shakeConfig.BaseHitDecayCurve);
                }

                if (baseDead)
                {
                    EnterState(BattleState.Defeat);
                    return;
                }

                // 2. 波次推进检测
                UpdateWaveIndex();

                // 3. 通关判定
                if (EntityManagerAccessor.Spawner.IsAllWavesCleared)
                {
                    bool hasAliveEnemy = false;
                    var entities = EntityManagerAccessor.Instance.ActiveEntities;
                    for (int i = 0; i < entities.Count; i++)
                    {
                        if (entities[i].Camp == EnumCamp.Enemy && !entities[i].IsPendingDespawn)
                        {
                            hasAliveEnemy = true;
                            break;
                        }
                    }
                    if (!hasAliveEnemy)
                    {
                        EnterState(BattleState.Victory);
                    }
                }

#if UNITY_EDITOR
                // Debug 字段更新
                _debug_CurrentState = CurrentState.ToString();
                _debug_StateTimer = _stateTimer;
                _debug_AliveEnemyCount = CountAliveEnemies();
#endif
            }
        }

        /// <summary>
        /// 波次推进检测（TDD_03 §5.1）
        /// 直接从 EntitySpawner 读取权威波次索引（0-based），转为 1-based 显示。
        /// 不再用 aliveEnemies==0 推测——避免多帧空窗期导致跳波。
        /// </summary>
        private void UpdateWaveIndex()
        {
            int spawnerWaveIndex = EntityManagerAccessor.Spawner.CurrentWaveIndexOfFirst;
            if (spawnerWaveIndex < 0) return; // 无活跃刷怪点

            int displayValue = spawnerWaveIndex + 1;
            if (displayValue != _displayWaveIndex)
            {
                _displayWaveIndex = displayValue;
                _currentWaveIndex.SetValue(_displayWaveIndex);
            }
        }


        private int CountAliveEnemies()
        {
            int count = 0;
            var entities = EntityManagerAccessor.Instance.ActiveEntities;
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Camp == EnumCamp.Enemy && !entities[i].IsPendingDespawn)
                    count++;
            }
            return count;
        }

        // ── 击杀计数 ──

        private void OnEntityDespawned(EntityClass entity, EntityConfigSO config)
        {
            if (entity.Camp == EnumCamp.Enemy)
            {
                var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                if (health != null && health.IsDead)
                {
                    _killCount.ApplyChange(1);
                }
            }
        }

        // ── 输入控制 ──

        /// <summary>
        /// PM-007: 同时控制 JoystickController 和 PlayerInputBridge。
        /// 时序：禁用时先停 Bridge 再停 Joystick；启用时先启 Joystick 再启 Bridge。
        /// </summary>
        private void SetInputEnabled(bool enabled)
        {
            if (enabled)
            {
                _joystickController?.SetEnabled(true);
                _playerInputBridge.SetEnabled(true);
            }
            else
            {
                _playerInputBridge.SetEnabled(false);
                _joystickController?.SetEnabled(false);
            }
        }

        private bool _spawnerStarted;

        public void SetLaunchContext(int? levelIndex)
        {
            _launchLevelIndex = levelIndex;
        }

        /// <summary>
        /// 由 BattleFlowHandler.OnFlowEnter 显式调用。
        /// 完整初始化战斗会话，确保时序正确（不依赖 Start 的执行顺序）。
        /// </summary>
        public async void StartBattle()
        {
            try
            {
                _battleStartedByFlow = true;

                // 确保运行时基础设施就绪（从导航进入时应已就绪，防御性等待）
                await BattleSceneBootstrapper.EnsureInitializedAsync();

                SG_Boot.InitProgress();
                _progressManager = SG_Boot.Progress;

                await InitBattleAsync();
                EnterState(BattleState.Intro);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void ResolveBattleContext()
        {
            if (_launchLevelIndex.HasValue)
            {
                int index = Mathf.Clamp(_launchLevelIndex.Value, 0, _levelConfigs.Length - 1);
                _currentLevel = _levelConfigs[index];
                _runtimeWaveConfig = _currentLevel != null ? _currentLevel.WaveConfig : null;
                return;
            }

            _currentLevel = _levelConfigs != null && _levelConfigs.Length > 0 ? _levelConfigs[0] : null;
            _runtimeWaveConfig = _spawnPoint != null ? _spawnPoint.WaveConfig : null;

            if (_runtimeWaveConfig == null && _currentLevel != null)
                _runtimeWaveConfig = _currentLevel.WaveConfig;
        }

        private void SetSpawnerEnabled(bool enabled)
        {
            if (enabled && !_spawnerStarted)
            {
                if (_spawnPoint != null && _runtimeWaveConfig != null)
                {
                    _spawnPoint.WaveConfig = _runtimeWaveConfig;
                }

                // 启动刷怪（SpawnPoint.AutoStartOnEnable 应设为 false）
                var spawner = EntityManagerAccessor.Spawner;
                if (spawner != null && _spawnPoint != null)
                {
                    spawner.StartWave(_spawnPoint);
                    _spawnerStarted = true;
                }
            }
            // V1 不实现暂停/恢复 Spawner（Intro 期间不会启动，Victory/Defeat 时波次已结束）
        }




        private void SetBattleTimePaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
        }

        /// <summary>
        /// 将战斗运行时状态恢复到“新一局”初始值。
        /// 语义上等价于重新进入 Battle 场景，但不重载场景。
        /// </summary>
        private void ResetBattleRuntimeState()
        {
            // 1. 回收所有 Entity
            EntityManagerAccessor.Instance.DespawnAll();

            // 2. 清理所有子系统运行时残留状态
            _entityBootstrap?.HitReactionHandler?.ClearAll();
            _entityBootstrap?.CollisionSolver?.ClearCooldowns();
            if (DanmakuSystem.Instance != null)
                DanmakuSystem.Instance.ClearAll();

            // 3. 重置刷怪驱动（StopAll 彻底清空注册状态，避免重复注册导致怪物翻倍）
            EntityManagerAccessor.Spawner.StopAll();
            _spawnerStarted = false;

            // 4. 复位相机震动
            _cameraShaker.StopShake();

            // 5. 重置 SO 变量
            _baseHP.SetValue(1.0f);
            _currentWaveIndex.SetValue(1);
            _killCount.SetValue(0);
            _displayWaveIndex = 1;
        }

        // ── 转场流程 ──


        private IEnumerator ShowVictoryAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _victoryPanel?.Show();
        }

        /// <summary>
        /// 胜利确认流程（async 版）。
        /// 存档 + 等云端确认 → Pop 回选关。
        /// 云端重试由全局 OnUploadFailedNeedRetry → NetworkRetryService 弹框处理，
        /// 本方法只需 await 直到上传成功。
        /// </summary>
        private async void HandleVictoryConfirmAsync()
        {
            if (_isHandlingVictory) return;
            _isHandlingVictory = true;

            try
            {
                SetBattleTimePaused(false);

                // 直跑场景属于测试模式，不写存档，直接 Pop
                if (!_launchLevelIndex.HasValue)
                {
                    await Task.Yield();
                    AppFlowNavigator.Instance.Pop();
                    return;
                }

                int clearedLevelIndex = _launchLevelIndex.Value + 1; // 0-based → 1-based

                // 本地写入 + 触发 EnqueueUpload
                _progressManager?.MarkLevelCleared(clearedLevelIndex);

                // 尝试获取 CloudSyncService（仅微信环境有）
                if (GameBootstrapper.SaveSystem is CloudSaveSystem cloudSave)
                {
                    var syncService = cloudSave.SyncService;

                    // 显示遮罩：屏蔽输入 + 视觉反馈"正在保存"
                    // 如果上传失败弹重试框，NetworkRetryService 会自动 Hide 遮罩再弹框
                    LoadingMaskService.Show("正在保存进度...");

                    // 等待上传完成。如果失败，全局 NetworkRetryService 弹框会自动处理重试，
                    // WaitForIdleAsync 会一直等到最终成功才 complete。
                    await syncService.WaitForIdleAsync();

                    LoadingMaskService.Hide();
                }

                await Task.Yield(); // 一帧等待
                AppFlowNavigator.Instance.Pop();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                LoadingMaskService.Hide(); // 兜底清理遮罩（Hide 在未 Show 时是 no-op）
                // 兜底：即使出异常也尝试 Pop，避免玩家卡死在胜利面板
                try { AppFlowNavigator.Instance.Pop(); } catch { }
            }
        }

        private IEnumerator HandleDefeatQuit()
        {
            SetBattleTimePaused(false);
            yield return null;
            AppFlowNavigator.Instance.Pop();
        }

        /// <summary>
        /// 重试流程（不重载场景）。TDD_02 §5.1
        /// </summary>
        private IEnumerator HandleRetry()
        {
            // 0. 恢复 timeScale（Defeat 状态下已冻结）
            SetBattleTimePaused(false);

            // 1. 黑屏淡入（V1 简化：直接跳过视觉过渡）
            yield return null;

            // 2. 重置战斗运行时状态
            ResetBattleRuntimeState();

            // 3. 重新 Spawn 基地 + 玩家
            SpawnBase();
            SpawnPlayer();

            // 3b. V2 Sprint 1: 重新注入伤害转发链路
            SetupDamageRedirectChain();

            // 3c. 重新订阅基地 OnDamaged 事件
            _baseEntity.EventBus.Subscribe<OnDamaged>(OnBaseDamaged);

            // 4. 重新初始化底线检测器
            float effectiveBaseLineY = _currentLevel.BaseLineYOverride >= 0
                ? -_currentLevel.BaseLineYOverride : _baseLineY;
            _baseLineDetector.Init(effectiveBaseLineY, _baseEntity, _baseHP);

            // 5. 重新初始化 PlayerInputBridge
            _playerInputBridge.Init(_playerEntity);

            // 6. 兜底 UI 同步
            _hudController?.ForceRefresh();

            // 7. 黑屏淡出
            yield return null;

            // 8. 重新走 Intro
            EnterState(BattleState.Intro);
        }

        private void OnResumeFromPause()
        {
            _pausePanel?.Hide();
        }

        private IEnumerator HandlePauseQuit()
        {
            SetBattleTimePaused(false);
            yield return null;
            AppFlowNavigator.Instance.Pop();
        }

        // ── Pause 按钮由 HUD 触发 ──

        public void OnPauseButtonClicked()
        {
            if (CurrentState != BattleState.Playing) return;
            _pausePanel?.Show();
        }

        // ── Gizmo：BaseLineY 可视化 ──

#if UNITY_EDITOR
        private EntitySystemBootstrap _cachedBootstrap;

        private void OnDrawGizmos()
        {
            // AT-008: 缓存 bootstrap 引用，null 时重新查找
            if (_cachedBootstrap == null)
                _cachedBootstrap = FindObjectOfType<EntitySystemBootstrap>();

            float xMin = -6f, xMax = 6f;
            if (_cachedBootstrap != null)
            {
                xMin = _cachedBootstrap.KillBounds.xMin;
                xMax = _cachedBootstrap.KillBounds.xMax;
            }

            // 底线红色横线
            Gizmos.color = Color.red;
            Vector3 left = new Vector3(xMin, _baseLineY, 0f);
            Vector3 right = new Vector3(xMax, _baseLineY, 0f);
            Gizmos.DrawLine(left, right);

            // 标签
            UnityEditor.Handles.Label(
                new Vector3(xMin, _baseLineY + 0.3f, 0f),
                $"BaseLine Y={_baseLineY:F1}",
                new GUIStyle { normal = { textColor = Color.red }, fontSize = 10 });
        }
#endif
    }
}
