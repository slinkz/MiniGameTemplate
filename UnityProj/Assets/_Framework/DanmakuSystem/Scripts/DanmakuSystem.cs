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
    /// </summary>
    public partial class DanmakuSystem : MonoBehaviour
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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DisposeSubsystems();
        }

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
            _bulletRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxBullets * 4);

            _laserRenderer = new LaserRenderer();
            _laserRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxLasers * LaserPool.MAX_SEGMENTS_PER_LASER);

            _laserWarningRenderer = new LaserWarningRenderer();
            _laserWarningRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxLasers);
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
        public BulletFaction Faction => BulletFaction.Player;

        public void OnBulletHit(int damage, int bulletIndex) { }
        public void OnLaserHit(int damage, int laserIndex) { }
        public void OnSprayHit(int damage, int sprayIndex) { }
    }
}
