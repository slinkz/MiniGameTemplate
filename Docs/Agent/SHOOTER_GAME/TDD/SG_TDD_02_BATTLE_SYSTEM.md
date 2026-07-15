# SG_TDD_02: 战斗系统

> 父文档：[SHOOTER_GAME/TDD/SG_TDD_INDEX.md](SHOOTER_GAME/TDD/SG_TDD_INDEX.md)

---

## 1. BattleState 状态机

### 1.1 状态枚举

```csharp
namespace Game.ShooterGame
{
    public enum BattleState : byte
    {
        None = 0,
        Intro,      // 飞机进场动画，不 Tick Spawner、不检测碰撞、不响应输入
        Playing,    // 正常战斗
        Victory,    // 0.5s 静默 → 胜利界面
        Defeat,     // 基地爆炸 → 失败界面
    }
}
```

### 1.2 BattleController（核心编排器）

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗场景唯一编排指挥。单一职责：状态机驱动 + 子系统协调。
    /// 不直接操作 Entity（委托给 BaseLineDetector / Spawner 等）。
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("关卡配置")]
        [SerializeField] private SG_LevelConfigSO[] _levelConfigs;  // 5 关
        [SerializeField] private IntVariable _currentLevelIndex;     // SO 输入
        
        // ET-009: V2 考虑将以下 SO 变量引用收拢到 BattleConfigSO 减少 Inspector 字段数
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
        
        // 运行时状态
        public BattleState CurrentState { get; private set; }
        private float _stateTimer;
        private SG_LevelConfigSO _currentLevel;
        private BaseLineDetector _baseLineDetector;
        private Entity _baseEntity;
        private Entity _playerEntity;
        private SG_ProgressManager _progressManager;  // 通过 GameStartupFlow.Progress 获取
        
        [Header("UI Controller 引用（Inspector 拖拽）")]
        [SerializeField] private BattleHUDController _hudController;
        [SerializeField] private PausePanelController _pausePanel;
        [SerializeField] private VictoryPanelController _victoryPanel;
        [SerializeField] private DefeatPanelController _defeatPanel;
        [SerializeField] private JoystickController _joystickController;
        [SerializeField] private SG_PlayerInputBridge _playerInputBridge;
        
        // ── 公共接口 ──
        public void RetryBattle() { /* §5.1 重试流程 */ }
        
#if UNITY_EDITOR
        // 调试接口（仅编辑器，TOOLS_TDD_02 §2.2）
        public void DebugForceVictory() => EnterState(BattleState.Victory);
        public void DebugForceDefeat() => EnterState(BattleState.Defeat);
#endif
        
        // ── 生命周期 ──
        
        private void Awake()
        {
            int index = Mathf.Clamp(_currentLevelIndex.Value, 0, _levelConfigs.Length - 1);
            _currentLevel = _levelConfigs[index];
            _baseLineDetector = new BaseLineDetector();
        }
        
        private void Start()
        {
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
                    // 结算状态不 Tick 战斗逻辑
                    break;
            }
        }
    }
}
```

### 1.3 状态转换规则

| 从 | 到 | 条件 |
|----|----|----|
| None → Intro | Start() 调用 | 无条件 |
| Intro → Playing | _stateTimer ≥ _introDuration | 进场动画结束 |
| Playing → Victory | IsAllWavesCleared == true && 存活敌机 == 0 | 通关 |
| Playing → Defeat | 基地 HP ≤ 0 | 基地毁灭 |
| Defeat → Playing | 重试（不换场景） | [再试一次] 按钮 |

### 1.4 状态进入/退出行为

```csharp
private void EnterState(BattleState newState)
{
    CurrentState = newState;
    _stateTimer = 0f;
    
    switch (newState)
    {
        case BattleState.Intro:
            // 禁用输入 + 禁用 Spawner + 播放进场动画
            SetInputEnabled(false);
            SetSpawnerEnabled(false);
            break;
            
        case BattleState.Playing:
            // 启用输入 + 启用 Spawner + 启用碰撞
            SetInputEnabled(true);
            SetSpawnerEnabled(true);
            break;
            
        case BattleState.Victory:
            // 停止 Spawner + 延迟后显示胜利界面
            SetSpawnerEnabled(false);
            StartCoroutine(ShowVictoryAfterDelay(_victoryDelay));
            break;
            
        case BattleState.Defeat:
            // 停止 Spawner + 基地爆炸特效 + 显示失败界面
            SetSpawnerEnabled(false);
            TriggerBaseExplosion();
            ShowDefeatPanel();
            break;
    }
}
```

---

## 2. 底线检测系统（BaseLineDetector）

### 2.1 设计决策

- **不使用** EntityCollisionSolver 检测基地碰撞（基地是逻辑概念，不是物理实体）
- **每帧遍历**敌方 Entity 检查 Position.y ≤ BaseLineY
- 命中后：对基地 HealthComponent 造成伤害 + 触发敌机 Despawn

### 2.2 类设计

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// 底线检测——每帧扫描敌方 Entity，穿过底线则扣基地 HP。
    /// 纯 C# 类（无 MonoBehaviour），由 BattleController 驱动 Tick。
    /// 只负责检测+扣血，不触发视觉反馈（SRP）。
    /// </summary>
    public class BaseLineDetector
    {
        private float _baseLineY;
        private Entity _baseEntity;
        private HealthComponent _baseHealth;
        private FloatVariable _baseHPVariable;  // 写入归一化比例
        
        /// <summary>本帧是否有敌机突破底线</summary>
        public bool HasBreachThisFrame { get; private set; }
        
        /// <summary>本帧突破底线的敌机数量（BattleController 据此触发反馈）</summary>
        public int BreachCountThisFrame { get; private set; }
        
        public void Init(float baseLineY, Entity baseEntity, FloatVariable baseHPVar)
        {
            _baseLineY = baseLineY;
            _baseEntity = baseEntity;
            _baseHealth = baseEntity.GetComponent(ComponentType.Health) as HealthComponent;
            _baseHPVariable = baseHPVar;
        }
        
        /// <summary>
        /// 每帧由 BattleController 调用（在 EntityManager.Tick 之后）。
        /// 返回基地是否死亡。
        /// 注意：先收集待 Despawn 列表，循环结束后统一 Despawn，
        /// 避免遍历 ActiveEntities 时 swap-remove 导致跳过元素。
        /// </summary>
        private readonly List<Entity> _breachedEnemies = new List<Entity>(8);
        
        public bool Tick(EntityManager mgr)
        {
            HasBreachThisFrame = false;
            BreachCountThisFrame = 0;
            _breachedEnemies.Clear();
            var entities = mgr.ActiveEntities;
            
            // Phase 1: 收集越线敌机
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;
                if (entity.Camp != EnumCamp.Enemy) continue;
                
                if (entity.Position.y <= _baseLineY)
                {
                    HasBreachThisFrame = true;
                    BreachCountThisFrame++;
                    _breachedEnemies.Add(entity);
                    
                    // 对基地造成伤害
                    int damage = entity.ConfigSO != null 
                        ? entity.ConfigSO.ContactDamage : 15;
                    var ctx = new DamageContext
                    {
                        BaseDamage = damage,
                        AttackerId = entity.Id,
                    };
                    _baseHealth.TakeDamage(ref ctx);
                    
                    // 更新 SO 变量（归一化 0~1）
                    _baseHPVariable.SetValue(_baseHealth.HpRatio);
                }
            }
            
            // Phase 2: 统一 Despawn（避免遍历中修改列表）
            for (int i = 0; i < _breachedEnemies.Count; i++)
            {
                mgr.Despawn(_breachedEnemies[i]);
            }
            
            return _baseHealth.IsDead;
        }
    }
}
```

### 2.3 BaseLineY 参数来源

- `BattleController` Inspector 字段 `[SerializeField] float _baseLineY = -7f;`
- 对应 CameraSize=8 时底部可视区域 y = -8，基地留 1 单位缓冲
- **Gizmo 可视化**：BattleController.OnDrawGizmos 画红色横线

---

## 3. 屏幕震动（CameraShaker）

### 3.1 ScreenShakeConfigSO

```csharp
namespace Game.ShooterGame
{
    [CreateAssetMenu(menuName = "ShooterGame/ScreenShakeConfig")]
    public class ScreenShakeConfigSO : ScriptableObject
    {
        [Header("飞机撞击敌机")]
        public float CollisionDuration = 0.15f;
        public float CollisionIntensity = 0.3f;
        public AnimationCurve CollisionDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("敌机突破底线")]
        public float BaseHitDuration = 0.3f;
        public float BaseHitIntensity = 0.6f;
        public AnimationCurve BaseHitDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }
}
```

### 3.2 CameraShaker

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// Camera 震动——Perlin Noise 位移偏移，零 GC。
    /// 挂载在 Main Camera 所在 GO 上。
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        private Vector3 _originalPos;
        private float _duration;
        private float _intensity;
        private float _elapsed;
        private AnimationCurve _decayCurve;
        private bool _isShaking;
        
        private void Awake()
        {
            _originalPos = transform.localPosition;
        }
        
        public void Shake(float duration, float intensity, AnimationCurve decay)
        {
            // 新震动覆盖旧震动（不叠加，简化 V1）
            _duration = duration;
            _intensity = intensity;
            _decayCurve = decay;
            _elapsed = 0f;
            _isShaking = true;
        }
        
        private void LateUpdate()
        {
            if (!_isShaking) return;
            
            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                _isShaking = false;
                transform.localPosition = _originalPos;
                return;
            }
            
            float t = _elapsed / _duration;
            float strength = _intensity * (_decayCurve != null 
                ? _decayCurve.Evaluate(t) : (1f - t));
            
            // Perlin Noise 生成随机偏移（比 Random.Range 更平滑）
            float offsetX = (Mathf.PerlinNoise(_elapsed * 25f, 0f) - 0.5f) * 2f * strength;
            float offsetY = (Mathf.PerlinNoise(0f, _elapsed * 25f) - 0.5f) * 2f * strength;
            
            transform.localPosition = _originalPos + new Vector3(offsetX, offsetY, 0f);
        }
        
        /// <summary>强制停止震动并复位</summary>
        public void StopShake()
        {
            _isShaking = false;
            transform.localPosition = _originalPos;
        }
    }
}
```

---

## 4. 通关判定

### 4.1 接口需求

需要框架 `EntitySpawner` 提供以下能力：

```csharp
// 如果 EntitySpawner 已有此属性，直接使用；否则需补充实现
public bool IsAllWavesCleared { get; }
// 语义：所有波次生成完毕 + 所有 SpawnGroup 对应 EntityConfig 的在场存活数为 0
```

### 4.2 BattleController 通关检测

```csharp
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
    
    // 2. 通关判定（在 Spawner.Tick 之后检查）
    if (EntityManagerAccessor.Spawner.IsAllWavesCleared)
    {
        // 额外确认：场上无存活敌机
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
```

---

## 5. 重试流程（不重载场景）

### 5.1 时序

```
[用户点击"再试一次"]
    │
    ▼
DefeatPanelController.OnRetryClicked()
    → BattleController.RetryBattle()
        │
        ▼
    1. await BlackScreenFadeIn(0.2f)       // 黑屏遮盖
    2. EntityManagerAccessor.Instance.DespawnAll()  // 回收所有 Entity
    3. EntityManagerAccessor.Spawner.RestartAll()   // 重置所有刷怪点到第一波
    3b. _cameraShaker.StopShake()             // ET-004: 复位相机，防止震动偏移残留
    3. 重置 SO 变量:
       _baseHP.SetValue(1.0f)
       _currentWaveIndex.SetValue(0)
       _killCount.SetValue(0)
       → UI 自动响应 OnValueChanged
    4. 重新 Spawn 基地 + 玩家
    5. BattleHUDController.ForceRefresh()   // 兜底同步
    6. await BlackScreenFadeOut(0.2f)       // 淡出
    7. EnterState(BattleState.Intro)        // 重新走 Intro
```

### 5.2 关键实现约束

- **不调用** `SceneManager.LoadScene()`——避免重新初始化 FairyGUI 和资源加载
- **不调用** `SO.ResetToInitial()`——因为战斗中 SO 初始值可能已被关卡配置覆盖
- 黑屏期间重置在单帧内完成，玩家不会看到"闪烁"
- CameraShaker 必须 `StopShake()` 复位

---

## 6. 击杀计数

### 6.1 订阅机制

```csharp
// BattleController.InitBattle() 中订阅
// 注意：OnDespawned 签名是 Action<Entity, EntityConfigSO>（两参数）
EntityManagerAccessor.Instance.OnDespawned += OnEntityDespawned;

private void OnEntityDespawned(Entity entity, EntityConfigSO config)
{
    // 击杀定义：敌方 Entity 的 HealthComponent.IsDead == true
    // 排除：边界回收（HP>0）和底线突破回收（敌机自身 HP 未扣减）
    if (entity.Camp == EnumCamp.Enemy)
    {
        var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
        if (health != null && health.IsDead)
        {
            _killCount.ApplyChange(1);
        }
    }
}
```

> **击杀计数规则明确**：
> - ✅ 计入击杀：被玩家子弹/接触伤害打死的敌机（IsDead == true）
> - ❌ 不计入：被边界回收的敌机（HP > 0，直接 Despawn）
> - ❌ 不计入：突破底线的敌机（底线检测对基地 TakeDamage，敌机直接 Despawn，HP > 0）

### 6.2 飞机撞击屏幕震动

```csharp
// BattleController 中：每帧轮询 CollisionSolver 碰撞状态
// 注意：EntityCollisionSolver 无公开碰撞事件，需通过 Bootstrap.CollisionSolver.PairCount 判断
// 方案：订阅玩家 Entity 的 OnDamaged 事件（ContactHit 类型触发震动）
private void SubscribePlayerCollisionShake()
{
    _playerEntity.EventBus.Subscribe<OnDamaged>(OnPlayerDamaged);
}

private void OnPlayerDamaged(OnDamaged evt)
{
    if (CurrentState != BattleState.Playing) return;
    
    _cameraShaker.Shake(
        _shakeConfig.CollisionDuration,
        _shakeConfig.CollisionIntensity,
        _shakeConfig.CollisionDecayCurve);
}
```

> **设计决策**：通过 Entity 内部 EventBus 的 `OnDamaged` 事件触发震动，而非碰撞回调。
> 原因：`EntityCollisionSolver` 不暴露碰撞事件（内部直接处理伤害），
> 但 `HealthComponent.TakeDamage` 会发布 `OnDamaged` 事件到 Entity 的 EventBus。
> 玩家飞机 ContactDamage=9999 → 敌机一撞即死 → 但敌机也会对玩家造成接触伤害 → 触发 OnDamaged。
> **注意**：如果玩家飞机不挂 HealthComponent（GDD 设计飞机免疫），则需要改为：
> 在 BattleController.TickPlaying 中直接订阅 EntityManager.OnDespawned，
> 当敌机因 ContactHit 死亡时触发震动。

---

## 7. InitBattle 完整流程

```csharp
private void InitBattle()
{
    // 1. 读取关卡配置
    _currentLevel = _levelConfigs[Mathf.Clamp(_currentLevelIndex.Value, 0, _levelConfigs.Length - 1)];
    
    // 2. 设置波次计数 SO
    int totalWaves = _currentLevel.WaveConfig.Waves.Length;
    _totalWaveCount.SetValue(totalWaves);
    _currentWaveIndex.SetValue(0);
    
    // 3. 计算总敌机数
    int totalEnemy = 0;
    foreach (var wave in _currentLevel.WaveConfig.Waves)
        foreach (var group in wave.Groups)
            totalEnemy += group.Count;
    _totalEnemyCount.SetValue(totalEnemy);
    
    // 4. 重置击杀计数
    _killCount.SetValue(0);
    
    // 5. Spawn 基地 Entity（HP = MaxHP * BaseHpRatio）
    SpawnBase();
    
    // 6. Spawn 玩家飞机 Entity
    SpawnPlayer();
    
    // 7. 初始化底线检测器（ET-003: 只传检测所需参数，不传视觉反馈引用）
    _baseLineDetector.Init(_baseLineY, _baseEntity, _baseHP);
    
    // 8. 设置 BaseHP SO 初始值
    _baseHP.SetValue(1.0f);  // 满血 = 1.0
    
    // 9. 初始化 UI Controllers（ET-007: 明确 UI 初始化时序）
    // BattleHUDController 先 Show（创建 _view）
    _hudController.Show();
    // JoystickController.Init 需要 BattleHUD 的 _view（通过公共 getter 获取）
    _joystickController.Init(_hudController.View);
    // 注册 UI 事件
    // PM-006: IEnumerator 方法不能直接赋值给 Action，用 lambda 包装 StartCoroutine
    _defeatPanel.OnRetry += () => StartCoroutine(HandleRetry());
    _defeatPanel.OnQuit += () => StartCoroutine(HandleDefeatQuit());
    _victoryPanel.OnConfirm += () => StartCoroutine(HandleVictoryConfirm());
    _pausePanel.OnResume += OnResumeFromPause;  // void 方法，直接赋值
    _pausePanel.OnQuit += () => StartCoroutine(HandlePauseQuit());
    // PlayerInputBridge 初始化
    _playerInputBridge.Init(_playerEntity);
}
```

### 7.1 SetInputEnabled 实现（PM-007）

```csharp
/// <summary>
/// PM-007: 同时控制 JoystickController 和 PlayerInputBridge。
/// 时序：禁用时先停 Bridge（停移动）再停 Joystick（隐藏视觉）；
///       启用时先启 Joystick（激活触摸）再启 Bridge（恢复移动）。
/// </summary>
private void SetInputEnabled(bool enabled)
{
    if (enabled)
    {
        _joystickController.SetEnabled(true);
        _playerInputBridge.SetEnabled(true);
    }
    else
    {
        _playerInputBridge.SetEnabled(false);   // 先停移动
        _joystickController.SetEnabled(false);  // 再隐摇杆
    }
}
```
