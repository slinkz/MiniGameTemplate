using System.Collections;
using System.Collections.Generic;
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

        // ── Debug Launcher 注入点 ──
        private BattleDebugLauncher _debugLauncher;

        /// <summary>由 BattleDebugLauncher.Awake() 调用注册。</summary>
        public void SetDebugLauncher(BattleDebugLauncher launcher) => _debugLauncher = launcher;
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

        // V2 Sprint 3: Buff 伤害修正（共享实例——无状态，所有 Entity 复用）
        private readonly BuffDamageModifier _buffDamageModifier = new BuffDamageModifier();

        // V2 Sprint 2: 道具 & 技能系统
        [Header("V2 Sprint 2: 道具系统")]
        [SerializeField] private DropTableSO _normalDropTable;
        [SerializeField] private DropTableSO _eliteDropTable;
        [SerializeField] private Material _pickupMaterial;

        private PickupSystem _pickupSystem;
        private ItemDropSystem _itemDropSystem;
        private PickupRenderer _pickupRenderer;
        private BattleLevelData _battleLevelData;

        // V2 Sprint 4: 伤害统计 + 星级评价
        private Dictionary<int, int> _damageStats;
        private float _battleTimer;
        private BattleResultData _lastBattleResult;
        private bool _damageStatsFrozen;


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

                // 直跑 Battle 场景：注入 DebugLauncher 配置（仅编辑器）
#if UNITY_EDITOR
                if (_debugLauncher != null)
                {
                    var debugData = _debugLauncher.BuildDebugLevelData();
                    if (debugData != null)
                    {
                        _launchLevelIndex = debugData.LevelIndex;
                        _battleLevelData = debugData;
                    }
                }
#endif

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

        private void LateUpdate()
        {
            // V2 Sprint 2: 道具渲染（与弹幕/VFX 同阶段提交 DrawMesh）
            if (_pickupRenderer != null && _pickupSystem != null)
                _pickupRenderer.Render(_pickupSystem);
        }

        private void OnDestroy()
        {
            // 取消订阅
            var mgr = EntityManagerAccessor.Instance;
            if (mgr != null)
            {
                mgr.OnDespawned -= OnEntityDespawned;
                mgr.OnSpawned -= OnEntitySpawnedRegisterBuff; // V2 Sprint 3
            }

            // 清理暂停按钮事件绑定
            var hudView = _hudController?.GetView();
            var btnPause = hudView?.GetChild("btn_pause");
            if (btnPause != null)
                btnPause.onClick.Remove(OnPauseButtonClicked);

            // 清理弹幕系统（DontDestroyOnLoad，不随场景销毁）
            if (DanmakuSystem.Instance != null)
                DanmakuSystem.Instance.ClearAll();

            // 清理道具渲染器
            _pickupRenderer?.Dispose();
            _pickupRenderer = null;
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

            // 4b. V2 Sprint 4: 初始化伤害统计
            _damageStats = new Dictionary<int, int>(16);
            _battleTimer = 0f;
            _damageStatsFrozen = false;
            _lastBattleResult = null;

            // 5. Spawn 基地 Entity
            SpawnBase();

            // 6. Spawn 玩家飞机 Entity
            SpawnPlayer();

            // 6b. V2 Sprint 1: 注入伤害转发链路
            SetupDamageRedirectChain();

            // 6b2. V2 Sprint 3: 为基地和玩家注册 BuffDamageModifier
            RegisterBuffDamageModifier(_baseEntity);
            RegisterBuffDamageModifier(_playerEntity);

            // 6b3. V2 Sprint 3: 订阅 OnSpawned 为后续敌机自动注册
            var mgr0 = EntityManagerAccessor.Instance;
            if (mgr0 != null)
                mgr0.OnSpawned += OnEntitySpawnedRegisterBuff;

            // 6c. 订阅基地 OnDamaged 事件 → 死亡判定
            _baseEntity.EventBus.Subscribe<OnDamaged>(OnBaseDamaged);

            // 6c2. 订阅基地 HealthComponent.OnHpChanged → 自动同步 BaseHP SO
            //       单一数据源：HealthComponent 是权威，所有 HP 变化自动推送到 FloatVariable。
            var baseHealthForSync = _baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
            if (baseHealthForSync != null)
                baseHealthForSync.OnHpChanged += OnBaseHpChanged;

            // 6d. V2 Sprint 2: 技能多槽位初始化（从 BattleLevelData 读取装备）
            if (_battleLevelData?.EquippedSkills != null && _battleLevelData.EquippedSkills.Length > 0)
            {
                var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
                skillComp?.InitWithEquipment(_battleLevelData.EquippedSkills);
            }

            // 6e. V2 Sprint 3: 被动技能 CD 驱动（升级 Sprint 2 的开局 ApplyBuff 方案）
            if (_battleLevelData?.EquippedPassives != null && _battleLevelData.EquippedPassives.Length > 0)
            {
                var passiveComp = _playerEntity.GetComponent(ComponentType.Passive) as PassiveComponent;
                passiveComp?.InitWithPassives(_battleLevelData.EquippedPassives);
            }

            // 6f. V2 Sprint 2: 初始化道具系统
            _pickupSystem = new PickupSystem();
            _pickupSystem.Init(_playerEntity, _baseEntity, _progressManager);

            _itemDropSystem = new ItemDropSystem();
            _itemDropSystem.Init(_normalDropTable, _eliteDropTable, _pickupSystem);

            // 6g. V2 Sprint 2: 初始化道具渲染器
            _pickupRenderer = new PickupRenderer();
            if (_pickupMaterial != null)
                _pickupRenderer.Initialize(_pickupMaterial);
            else
                Debug.LogWarning("[BattleController] _pickupMaterial is null — 道具将不可见！请在 Inspector 中赋值。");

            // 7. 初始化底线检测器（ET-003: 只传检测所需参数）
            float effectiveBaseLineY = _currentLevel.BaseLineYOverride >= 0
                ? -_currentLevel.BaseLineYOverride : _baseLineY;
            _baseLineDetector.Init(effectiveBaseLineY, _baseEntity);

            // 8. 初始同步 BaseHP SO（SpawnBase 中 SetHp 在订阅前执行，需要 kick 初始值）
            _baseHP.SetValue(baseHealthForSync != null ? baseHealthForSync.HpRatio : 1.0f);

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

            // GDD v1.6: 全自动射击——Spawn 后立即设置 ControlComponent（不等 InputBridge.Init，
            // 因为中间有 await 会导致 Entity Tick 使用默认 Vector2.right 瞄准方向）
            var ctrl = _playerEntity.GetComponent(ComponentType.Control) as ControlComponent;
            if (ctrl != null)
            {
                ctrl.SetAttackInput(true);
                ctrl.SetAimInput(Vector2.up);
            }
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

            // V2 Sprint 2: 记录被命中次数（成就 ID=3 用）
            _progressManager?.RecordHit();

            // BaseHP SO 同步已由 OnBaseDamaged 统一处理
        }

        // ── V2 Sprint 3: BuffDamageModifier 注册 ──

        /// <summary>
        /// 为指定 Entity 注册 BuffDamageModifier（共享实例）。
        /// 只对有 HealthComponent + BuffComponent 的 Entity 注册。
        /// </summary>
        private void RegisterBuffDamageModifier(EntityClass entity)
        {
            if (entity == null) return;
            var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
            if (health == null) return;
            // 检查是否有 BuffComponent（无则不需要修正）
            var buff = entity.GetComponent(ComponentType.Buff);
            if (buff == null) return;
            health.AddModifier(_buffDamageModifier);
        }

        /// <summary>
        /// EntityManager.OnSpawned 回调——自动为所有新生成的 Entity 注册 BuffDamageModifier。
        /// V2 Sprint 4: 同时订阅 Enemy OnDamaged 用于伤害统计。
        /// </summary>
        private void OnEntitySpawnedRegisterBuff(EntityClass entity, EntityConfigSO config)
        {
            RegisterBuffDamageModifier(entity);

            // Sprint 4: 订阅敌机受伤事件 → damageStats 累加
            if (entity.Camp == EnumCamp.Enemy)
            {
                entity.EventBus.Subscribe<OnDamaged>(OnEnemyDamaged);
            }
        }

        /// <summary>
        /// Sprint 4: 敌机受伤回调——累加 damageStats。
        /// 只在战斗进行中（未冻结）时累加。
        /// </summary>
        private void OnEnemyDamaged(OnDamaged evt)
        {
            if (_damageStatsFrozen || _damageStats == null) return;
            if (evt.Damage <= 0) return;

            int key = evt.SourceId;
            if (_damageStats.TryGetValue(key, out int current))
                _damageStats[key] = current + evt.Damage;
            else
                _damageStats[key] = evt.Damage;
        }

        /// <summary>
        /// 基地受伤回调（无论来源：底线突破 / 弹丸直击 / 飞机伤害转发）。
        /// BaseHP SO 同步已由 OnHpChanged 事件自动处理，此处只负责死亡判定 → Defeat。
        /// </summary>
        private void OnBaseDamaged(OnDamaged evt)
        {
            var baseHealth = _baseEntity?.GetComponent(ComponentType.Health) as HealthComponent;
            if (baseHealth == null) return;

            if (baseHealth.IsDead && CurrentState == BattleState.Playing)
            {
                EnterState(BattleState.Defeat);
            }
        }

        /// <summary>
        /// HealthComponent.OnHpChanged 回调——单一出口同步 BaseHP FloatVariable SO。
        /// 替代原先散落在 5+ 处的手动 SetValue()。
        /// </summary>
        private void OnBaseHpChanged(float hpRatio)
        {
            _baseHP.SetValue(hpRatio);
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
                    FreezeBattleResult(true);
                    // 立即存档（通关瞬间持久化，不等确认按钮）
                    PersistVictoryProgress();
                    StartCoroutine(ShowVictoryAfterDelay(_victoryDelay));
                    break;

                case BattleState.Defeat:
                    SetSpawnerEnabled(false);
                    SetInputEnabled(false);
                    SetBattleTimePaused(true);
                    FreezeBattleResult(false);
                    // V2 Sprint 2: 记录死亡次数 + 刷新计数器
                    _progressManager?.RecordDeath();
                    _progressManager?.FlushCounters();
                    _defeatPanel?.Show();
                    break;
            }
        }

        // ── Tick 逻辑 ──

        // ── V2 Sprint 4: 战斗结果冻结 ──

        /// <summary>
        /// 冻结伤害统计 + 构建 BattleResultData。
        /// Victory/Defeat 进入时调用。
        /// </summary>
        private void FreezeBattleResult(bool isVictory)
        {
            _damageStatsFrozen = true;

            // 读取基地 HP
            int baseHpRemaining = 0;
            int baseHpMax = _baseEntityConfig.MaxHp;
            var baseHealth = _baseEntity != null
                ? _baseEntity.GetComponent(ComponentType.Health) as HealthComponent
                : null;
            if (baseHealth != null)
            {
                baseHpRemaining = baseHealth.CurrentHp;
                baseHpMax = baseHealth.MaxHp;
            }

            // 计算星级
            int stars = isVictory
                ? BattleResultCalculator.CalcStars(baseHpRemaining, baseHpMax)
                : 0;

            // 构建快照副本（防止外部修改）
            var statsSnapshot = _damageStats != null
                ? new Dictionary<int, int>(_damageStats)
                : new Dictionary<int, int>();

            _lastBattleResult = new BattleResultData
            {
                IsVictory = isVictory,
                Stars = stars,
                LevelIndex = _launchLevelIndex ?? 0,
                TotalKills = _killCount.Value,
                BattleTime = _battleTimer,
                CoinsEarned = 0, // V3 预留
                DamageStats = statsSnapshot,
                BaseHpRemaining = baseHpRemaining,
                BaseHpMax = baseHpMax,
            };

#if UNITY_EDITOR
            Debug.Log($"[SG_Battle] Result: Victory={isVictory}, Stars={stars}, " +
                      $"HP={baseHpRemaining}/{baseHpMax}, Kills={_killCount.Value}, " +
                      $"Time={_battleTimer:F1}s, Sources={statsSnapshot.Count}");
#endif
        }

        /// <summary>获取最近一次战斗结果（结算面板用）</summary>
        public BattleResultData LastBattleResult => _lastBattleResult;

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
                // 0. V2 Sprint 4: 累加战斗计时
                _battleTimer += dt;

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

                // 2b. V2 Sprint 2: 道具系统 Tick
                _pickupSystem?.Tick(dt);

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

                    // V2 Sprint 2: 触发道具掉落
                    _itemDropSystem?.OnEnemyKilled(entity.Position);
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
        /// V2 Sprint 2: 接收完整的 BattleLevelData（含装备数据）。
        /// 由 BattleFlowHandler.OnFlowEnter 调用。
        /// </summary>
        public void SetLaunchContext(BattleLevelData data)
        {
            if (data == null) return;
            _launchLevelIndex = data.LevelIndex;
            _battleLevelData = data;
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
            // 注：_baseHP 不再手动 SetValue(1.0f)——由 HandleRetry 中
            //     SpawnBase() → SetHp() → OnHpChanged → OnBaseHpChanged 自动推送真实值。
            _currentWaveIndex.SetValue(1);
            _killCount.SetValue(0);
            _displayWaveIndex = 1;

            // 5b. V2 Sprint 4: 重置伤害统计
            _damageStats?.Clear();
            _battleTimer = 0f;
            _damageStatsFrozen = false;
            _lastBattleResult = null;
        }

        // ── 转场流程 ──


        private IEnumerator ShowVictoryAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _victoryPanel?.Show(_lastBattleResult);
        }

        /// <summary>
        /// 胜利时立即持久化通关数据。
        /// 确保即使玩家在结算面板关闭游戏，进度也已保存。
        /// 云端上传（异步）在确认按钮回调中等待完成。
        /// </summary>
        private void PersistVictoryProgress()
        {
            // 直跑场景 = 测试模式，不写存档
            if (!_launchLevelIndex.HasValue) return;

            int clearedLevelIndex = _launchLevelIndex.Value + 1; // 0-based → 1-based

            // 星级存档（只升不降）
            if (_lastBattleResult != null && _lastBattleResult.Stars > 0)
            {
                _progressManager?.UpdateLevelStars(clearedLevelIndex, _lastBattleResult.Stars);
            }

            // 成就计数器
            _progressManager?.UpdateMaxKills(_killCount.Value);
            _progressManager?.FlushCounters();

            // 标记关卡通关（本地写入 + 触发 EnqueueUpload）
            _progressManager?.MarkLevelCleared(clearedLevelIndex);
        }

        /// <summary>
        /// 胜利确认流程（async 版）。
        /// 存档已在 EnterState(Victory) 时立即完成（PersistVictoryProgress）。
        /// 此处只需等云端上传完成 → Pop 回选关。
        /// </summary>
        private async void HandleVictoryConfirmAsync()
        {
            if (_isHandlingVictory) return;
            _isHandlingVictory = true;

            try
            {
                SetBattleTimePaused(false);

                // 退场前清理战斗残留（飘字等池对象位于 DontDestroyOnLoad，不随场景卸载）
                _entityBootstrap?.HitReactionHandler?.ClearAll();
                if (DanmakuSystem.Instance != null)
                    DanmakuSystem.Instance.ClearAll();

                // 直跑场景属于测试模式，不写存档，直接 Pop
                if (!_launchLevelIndex.HasValue)
                {
                    await Task.Yield();
                    AppFlowNavigator.Instance.Pop();
                    return;
                }

                // 尝试获取 CloudSyncService（仅微信环境有）
                // 存档已在 PersistVictoryProgress 中写入本地并触发 EnqueueUpload，
                // 这里只需等待云端上传完成。
                if (GameBootstrapper.SaveSystem is CloudSaveSystem cloudSave)
                {
                    var syncService = cloudSave.SyncService;

                    // 显示遮罩：屏蔽输入 + 视觉反馈"正在保存"
                    LoadingMaskService.Show("正在保存进度...");

                    // 等待上传完成。如果失败，全局 NetworkRetryService 弹框自动处理重试。
                    await syncService.WaitForIdleAsync();

                    LoadingMaskService.Hide();
                }

                await Task.Yield(); // 一帧等待
                AppFlowNavigator.Instance.Pop();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                LoadingMaskService.Hide(); // 兜底清理遮罩
                // 兜底：即使出异常也尝试 Pop，避免玩家卡死在胜利面板
                try { AppFlowNavigator.Instance.Pop(); } catch { }
            }
        }

        private IEnumerator HandleDefeatQuit()
        {
            SetBattleTimePaused(false);
            // 退场前清理战斗残留（飘字等池对象位于 DontDestroyOnLoad，不随场景卸载）
            _entityBootstrap?.HitReactionHandler?.ClearAll();
            if (DanmakuSystem.Instance != null)
                DanmakuSystem.Instance.ClearAll();
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

            // 3b2. V2 Sprint 3: 重新注册 BuffDamageModifier
            RegisterBuffDamageModifier(_baseEntity);
            RegisterBuffDamageModifier(_playerEntity);

            // 3c. 重新订阅基地 OnDamaged 事件
            _baseEntity.EventBus.Subscribe<OnDamaged>(OnBaseDamaged);

            // 3c2. 重新订阅基地 OnHpChanged → 自动同步 BaseHP SO
            var retryBaseHealth = _baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
            if (retryBaseHealth != null)
                retryBaseHealth.OnHpChanged += OnBaseHpChanged;

            // 3c3. Kick 初始值（SetHp 在订阅前已执行）
            _baseHP.SetValue(retryBaseHealth != null ? retryBaseHealth.HpRatio : 1.0f);

            // 3d. V2 Sprint 2: 重新注入技能多槽位
            if (_battleLevelData?.EquippedSkills != null && _battleLevelData.EquippedSkills.Length > 0)
            {
                var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
                skillComp?.InitWithEquipment(_battleLevelData.EquippedSkills);
            }

            // 3e. V2 Sprint 3: 重新初始化被动组件
            if (_battleLevelData?.EquippedPassives != null && _battleLevelData.EquippedPassives.Length > 0)
            {
                var passiveComp = _playerEntity.GetComponent(ComponentType.Passive) as PassiveComponent;
                passiveComp?.InitWithPassives(_battleLevelData.EquippedPassives);
            }

            // 3f. V2 Sprint 2: 重置道具系统
            _pickupSystem?.Clear();
            _itemDropSystem?.Reset();
            _pickupSystem?.Init(_playerEntity, _baseEntity, _progressManager);

            // 4. 重新初始化底线检测器
            float effectiveBaseLineY = _currentLevel.BaseLineYOverride >= 0
                ? -_currentLevel.BaseLineYOverride : _baseLineY;
            _baseLineDetector.Init(effectiveBaseLineY, _baseEntity);

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
            // 退场前清理战斗残留（飘字等池对象位于 DontDestroyOnLoad，不随场景卸载）
            _entityBootstrap?.HitReactionHandler?.ClearAll();
            if (DanmakuSystem.Instance != null)
                DanmakuSystem.Instance.ClearAll();
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
