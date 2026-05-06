using System.Collections;
using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Navigation;
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
        [SerializeField] private IntVariable _currentLevelIndex;

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
        private BaseLineDetector _baseLineDetector;
        private EntitySystemBootstrap _entityBootstrap;
        private EntityClass _baseEntity;
        private EntityClass _playerEntity;
        private SG_ProgressManager _progressManager;
        private int _displayWaveIndex;


        // UI Controller 接口（通过 Init 或 GetComponent 动态绑定）
        private IBattleHUDController _hudController;
        private IPausePanelController _pausePanel;
        private IVictoryPanelController _victoryPanel;
        private IDefeatPanelController _defeatPanel;
        private IJoystickController _joystickController;

        // ── 公共接口 ──

#if UNITY_EDITOR
        public void DebugForceVictory() => EnterState(BattleState.Victory);
        public void DebugForceDefeat() => EnterState(BattleState.Defeat);
        public void DebugRetryBattle() => StartCoroutine(HandleRetry());
#endif

        // ── 生命周期 ──

        private void Awake()
        {
            int index = Mathf.Clamp(_currentLevelIndex.Value, 0, _levelConfigs.Length - 1);
            _currentLevel = _levelConfigs[index];
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
            // 1. 读取关卡配置
            _currentLevel = _levelConfigs[Mathf.Clamp(_currentLevelIndex.Value, 0, _levelConfigs.Length - 1)];

            // 2. 设置波次计数 SO（1-based 展示）
            // 统一以运行时实际启动的 SpawnPoint.WaveConfig 为权威来源，
            // 避免与 LevelConfig.WaveConfig 不一致时出现 HUD 显示 1/5、2/4 这类分子分母不同源问题。
            var runtimeWaveConfig = _spawnPoint != null ? _spawnPoint.WaveConfig : _currentLevel.WaveConfig;
            int totalWaves = runtimeWaveConfig != null && runtimeWaveConfig.Waves != null
                ? runtimeWaveConfig.Waves.Length
                : 0;
            _totalWaveCount.SetValue(totalWaves);
            _currentWaveIndex.SetValue(totalWaves > 0 ? 1 : 0);
            _displayWaveIndex = totalWaves > 0 ? 1 : 0;


            // 3. 计算总敌机数
            int totalEnemy = 0;
            if (runtimeWaveConfig != null && runtimeWaveConfig.Waves != null)
            {
                foreach (var wave in runtimeWaveConfig.Waves)
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
            _victoryPanel?.BindEvents(
                () => StartCoroutine(HandleVictoryConfirm()));
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

        // ── 状态转换 ──

        private void EnterState(BattleState newState)
        {
            Debug.Log($"[SG_Battle] State → {newState} (time={Time.time:F2}s, frame={Time.frameCount})");
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

        private void SetSpawnerEnabled(bool enabled)
        {
            if (enabled && !_spawnerStarted)
            {
                // 首次启用时启动刷怪（SpawnPoint.AutoStartOnEnable 应设为 false）
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

        private IEnumerator HandleVictoryConfirm()
        {
            SetBattleTimePaused(false);
            _progressManager?.MarkLevelCleared(_currentLevelIndex.Value + 1); // 0-based → 1-based
            yield return null; // 一帧等待
            AppFlowNavigator.Instance.Pop();
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
