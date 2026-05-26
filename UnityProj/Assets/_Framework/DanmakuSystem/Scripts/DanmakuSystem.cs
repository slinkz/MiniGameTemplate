using MiniGameTemplate.Battle;
using MiniGameTemplate.Events;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕系统唯一 MonoBehaviour 入口（Facade）——生命周期管理。
    /// DontDestroyOnLoad，关卡切换时调用 ClearAll 清场而非销毁。
    /// 
    /// 职责拆分：
    /// - DanmakuSystem.cs（本文件）：Facade，Awake/Update/LateUpdate/单例
    /// - DanmakuSystem.Runtime.cs：持有所有子系统引用、初始化/销毁
    /// - DanmakuSystem.API.cs：Fire/Register/Clear 等公开 API
    /// - DanmakuSystem.UpdatePipeline.cs：Update 内的逐步驱动逻辑
    /// 
    /// DEV-002：VFX 序列化字段已迁移到 DanmakuEffectsBridgeConfig 组件。
    /// TDD-07 B1：实现 IBattleCleanup（DDOL 永久监听者，Awake Register 不注销）。
    /// </summary>
    public partial class DanmakuSystem : MonoBehaviour, IBattleCleanup
    {
        [Header("配置")]
        [SerializeField] private DanmakuWorldConfig _worldConfig;
        [SerializeField] private DanmakuRenderConfig _renderConfig;
        [SerializeField] private DanmakuTimeScaleSO _timeScale;
        [SerializeField] private DifficultyProfileSO _difficulty;

        private DanmakuTypeRegistry _typeRegistry;

        [Header("事件")]
        [Tooltip("玩家被命中时触发")]
        [SerializeField] private GameEvent _onPlayerHit;

        [Tooltip("造成伤害时触发（传递伤害值）")]
        [SerializeField] private IntGameEvent _onDamageDealt;

        [Header("TDD-07: 退场事件通道")]
        [SerializeField] private BattleLifecycleEvent _onBattleEnd;

        // ──── 单例 ────
        public static DanmakuSystem Instance { get; private set; }

        /// <summary>碰撞事件旁路 Buffer（DebugHUD 等外部消费者用）</summary>
        public CollisionEventBuffer CollisionEventBuffer => _collisionEventBuffer;


        // ──── 生命周期 ────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSubsystems();

            // TDD-07 B1: DDOL 永久监听者——Awake 中 Register，不注销
            // WX-007: null 防御——SerializeField 漏拖时输出错误日志
            if (_onBattleEnd != null)
                _onBattleEnd.Register(this);
            else
                Debug.LogError("[DanmakuSystem] _onBattleEnd SO 未赋值！退场时弹丸不会被自动清理。");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DisposeSubsystems();
        }

        // ──── TDD-07: IBattleCleanup 实现 ────

        /// <summary>清弹丸+激光+喷雾+飘字——最先执行，阻止新命中事件产生。</summary>
        public int CleanupOrder => 0;

        /// <summary>退场清理回调。</summary>
        public void OnBattleCleanup() => ClearAll();

        private void Update()
        {
            RunUpdatePipeline();
        }

        private void LateUpdate()
        {
            RunLateUpdatePipeline();
        }

        /// <summary>
        /// Rebuilds registry indices and warms batch state through the controlled editor workflow entry.
        /// </summary>
        public void EditorWarmupBatches()
        {
#if UNITY_EDITOR
            if (_renderConfig == null || _worldConfig == null)
                throw new System.InvalidOperationException("DanmakuSystem missing config references for editor warmup.");

            _typeRegistry ??= new DanmakuTypeRegistry();
            _typeRegistry.WarmUp(
                Danmaku.Editor.DanmakuEditorRefreshCoordinator.FindAllBulletTypes(),
                Danmaku.Editor.DanmakuEditorRefreshCoordinator.FindAllLaserTypes(),
                Danmaku.Editor.DanmakuEditorRefreshCoordinator.FindAllSprayTypes());

            _bulletRenderer?.Dispose();
            _laserRenderer?.Dispose();
            _laserWarningRenderer?.Dispose();

            _bulletRenderer = new BulletRenderer();
            _bulletRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxBullets * 4, _sharedAtlas);

            _laserRenderer = new LaserRenderer();
            _laserRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxLasers * LaserPool.MAX_SEGMENTS_PER_LASER, _sharedAtlas);

            _laserWarningRenderer = new LaserWarningRenderer();
            _laserWarningRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxLasers, _sharedAtlas);
#else
            throw new System.InvalidOperationException("EditorWarmupBatches is editor only.");
#endif
        }
    }


    /// <summary>
    /// 内置 Player 碰撞目标适配器——将旧 SetPlayer API 适配到 ICollisionTarget 接口。
    /// </summary>
    internal class PlayerCollisionTarget : ICollisionTarget
    {
        private readonly Transform _transform;
        private readonly float _radius;
        private readonly GameEvent _onPlayerHit;
        private readonly IntGameEvent _onDamageDealt;

        public PlayerCollisionTarget(Transform transform, float radius,
            GameEvent onPlayerHit, IntGameEvent onDamageDealt)
        {
            _transform = transform;
            _radius = radius;
            _onPlayerHit = onPlayerHit;
            _onDamageDealt = onDamageDealt;
        }

        public CircleHitbox Hitbox
        {
            get
            {
                if (_transform == null) return new CircleHitbox(Vector2.zero, 0f);
                return new CircleHitbox(_transform.position, _radius);
            }
        }
        public EnumCamp Faction => EnumCamp.Player;

        public void OnBulletHit(int damage, int bulletIndex) { }
        public void OnLaserHit(int damage, int laserIndex) { }
        public void OnSprayHit(int damage, int sprayIndex) { }
    }
}
