using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MiniGameTemplate.Data;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Entity;
using EntityClass = MiniGameTemplate.Entity.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗场景唯一编排指挥。单一职责：状态机驱动 + 子系统协调。
    /// 不直接操作 Entity（委托给 BaseLineDetector / Spawner 等）。
    /// TDD_02 §1.2
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        private const string SCENE_BOOT = "Boot";

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
#endif

        // ── 生命周期 ──

        private void Awake()
        {
            int index = Mathf.Clamp(_currentLevelIndex.Value, 0, _levelConfigs.Length - 1);
            _currentLevel = _levelConfigs[index];
            _baseLineDetector = new BaseLineDetector();

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

        private void Start()
        {
            // 确保后台也能推进帧（Editor 调试 + 微信小游戏后台兼容）
            Application.runInBackground = true;

            // 防御性初始化：若从 Boot 场景启动则已初始化，直接跳场景测试时兜底
            SG_Boot.InitProgress();
            _progressManager = SG_Boot.Progress;
            InitBattle();
            EnterState(BattleState.Intro);
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
        }

        // ── 初始化 ──

        private void InitBattle()
        {
            // 1. 读取关卡配置
            _currentLevel = _levelConfigs[Mathf.Clamp(_currentLevelIndex.Value, 0, _levelConfigs.Length - 1)];

            // 2. 设置波次计数 SO
            int totalWaves = _currentLevel.WaveConfig.Waves.Length;
            _totalWaveCount.SetValue(totalWaves);
            _currentWaveIndex.SetValue(0);
            _displayWaveIndex = 0;

            // 3. 计算总敌机数
            int totalEnemy = 0;
            foreach (var wave in _currentLevel.WaveConfig.Waves)
                foreach (var group in wave.Groups)
                    totalEnemy += group.Count;
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
            _hudController?.Show();
            _joystickController?.Init(_hudController?.GetView());
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
                    StartCoroutine(ShowVictoryAfterDelay(_victoryDelay));
                    break;

                case BattleState.Defeat:
                    SetSpawnerEnabled(false);
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
            // 1. 底线检测
            bool baseDead = _baseLineDetector.Tick(EntityManagerAccessor.Instance);

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
        }

        /// <summary>
        /// 波次推进检测（TDD_03 §5.1）
        /// V1 五关全部使用 AllCleared 推进模式（PM-008）。
        /// </summary>
        private void UpdateWaveIndex()
        {
            int aliveEnemies = CountAliveEnemies();
            if (aliveEnemies == 0 && !EntityManagerAccessor.Spawner.IsAllWavesCleared
                && _displayWaveIndex < _totalWaveCount.Value)
            {
                _displayWaveIndex++;
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

        // ── 转场流程 ──

        private IEnumerator ShowVictoryAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _victoryPanel?.Show();
        }

        private IEnumerator HandleVictoryConfirm()
        {
            _progressManager?.MarkLevelCleared(_currentLevelIndex.Value + 1); // 0-based → 1-based
            yield return null; // 一帧等待
            SceneManager.LoadScene(SCENE_BOOT);
        }

        private IEnumerator HandleDefeatQuit()
        {
            yield return null;
            SceneManager.LoadScene(SCENE_BOOT);
        }

        /// <summary>
        /// 重试流程（不重载场景）。TDD_02 §5.1
        /// </summary>
        private IEnumerator HandleRetry()
        {
            // 1. 黑屏淡入（V1 简化：直接跳过视觉过渡）
            yield return null;

            // 2. 回收所有 Entity
            EntityManagerAccessor.Instance.DespawnAll();

            // 3. 重置 Spawner
            EntityManagerAccessor.Spawner.RestartAll();
            _spawnerStarted = false; // 允许下次 EnterState(Playing) 重新 StartWave

            // 4. 复位相机震动（ET-004）
            _cameraShaker.StopShake();

            // 5. 重置 SO 变量
            _baseHP.SetValue(1.0f);
            _currentWaveIndex.SetValue(0);
            _killCount.SetValue(0);
            _displayWaveIndex = 0;

            // 6. 重新 Spawn 基地 + 玩家
            SpawnBase();
            SpawnPlayer();

            // 7. 重新初始化底线检测器
            float effectiveBaseLineY = _currentLevel.BaseLineYOverride >= 0
                ? -_currentLevel.BaseLineYOverride : _baseLineY;
            _baseLineDetector.Init(effectiveBaseLineY, _baseEntity, _baseHP);

            // 8. 重新初始化 PlayerInputBridge
            _playerInputBridge.Init(_playerEntity);

            // 9. 重新订阅 OnDespawned（DespawnAll 后旧引用失效，但委托仍在）
            // 不需要重新订阅，因为 OnDespawned 回调不持有 Entity 引用

            // 10. 兜底 UI 同步
            _hudController?.ForceRefresh();

            // 11. 黑屏淡出
            yield return null;

            // 12. 重新走 Intro
            EnterState(BattleState.Intro);
        }

        private void OnResumeFromPause()
        {
            _pausePanel?.Hide();
        }

        private IEnumerator HandlePauseQuit()
        {
            Time.timeScale = 1f;
            yield return null;
            SceneManager.LoadScene(SCENE_BOOT);
        }

        // ── Pause 按钮由 HUD 触发 ──

        public void OnPauseButtonClicked()
        {
            if (CurrentState != BattleState.Playing) return;
            _pausePanel?.Show();
        }

        // ── Gizmo：BaseLineY 可视化 ──

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(-20f, _baseLineY, 0f),
                new Vector3(20f, _baseLineY, 0f));
        }
#endif
    }
}
