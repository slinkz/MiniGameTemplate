using MiniGameTemplate.Utils;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    // DanmakuSystem.Runtime.cs — 持有所有子系统引用、初始化/销毁
    public partial class DanmakuSystem
    {
        // ──── 子系统 ────
        private BulletWorld _bulletWorld;
        private LaserPool _laserPool;
        private SprayPool _sprayPool;
        private ObstaclePool _obstaclePool;
        private AttachSourceRegistry _attachRegistry;
        private TargetRegistry _targetRegistry;
        private CollisionSolver _collisionSolver;
        private CollisionEventBuffer _collisionEventBuffer;
        private PatternScheduler _scheduler;
        private SpawnerDriver _spawnerDriver;
        private BulletRenderer _bulletRenderer;
        private LaserRenderer _laserRenderer;
        private LaserWarningRenderer _laserWarningRenderer;
        private DamageNumberSystem _damageNumbers;
        private TrailPool _trailPool;

        // ──── 特效桥接 ────
        private IDanmakuEffectsBridge _effectsBridge;

        // ──── VFX 桥接运行时（Phase 4 — DEV-008） ────
        private IDanmakuVFXRuntime _vfxRuntime;


        // ──── 内置 Player 目标适配器 ────
        private PlayerCollisionTarget _builtinPlayerTarget;

        // ──── 无敌帧 ────
        private float _invincibleTimer;

        private void InitializeSubsystems()
        {
            // 运动策略注册表初始化
            MotionRegistry.Initialize();

            // 类型注册表索引分配
            _typeRegistry.AssignRuntimeIndices();

            // 数据容器
            _bulletWorld = new BulletWorld(_worldConfig.MaxBullets);
            _laserPool = new LaserPool(_worldConfig.MaxLasers);
            _sprayPool = new SprayPool(_worldConfig.MaxSprays);
            _obstaclePool = new ObstaclePool();
            _attachRegistry = new AttachSourceRegistry();
            _targetRegistry = new TargetRegistry();

            // 碰撞事件 Buffer
            _collisionEventBuffer = new CollisionEventBuffer(_worldConfig.CollisionEventBufferCapacity);

            // 碰撞
            _collisionSolver = new CollisionSolver();
            _collisionSolver.Initialize(_worldConfig, _collisionEventBuffer);

            // 调度器
            _scheduler = new PatternScheduler();

            // 发射器驱动
            _spawnerDriver = new SpawnerDriver();

            // 渲染
            _bulletRenderer = new BulletRenderer();
            _bulletRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxBullets * 4);

            _laserRenderer = new LaserRenderer();
            _laserRenderer.Initialize(_renderConfig, _typeRegistry,
                _worldConfig.MaxLasers * LaserPool.MAX_SEGMENTS_PER_LASER);

            _laserWarningRenderer = new LaserWarningRenderer();
            _laserWarningRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxLasers);

            // 伤害飘字
            _damageNumbers = new DamageNumberSystem();
            _damageNumbers.Initialize(_renderConfig);

            // 重量拖尾
            _trailPool = new TrailPool(_worldConfig.MaxTrails);
            _trailPool.Initialize(_renderConfig.BulletMaterial);

            // 特效桥接——从 BridgeConfig 组件获取 VFX 引用（DEV-002）
            var bridgeConfig = GetComponent<DanmakuEffectsBridgeConfig>();
            if (bridgeConfig != null)
            {
                _effectsBridge = new DefaultDanmakuEffectsBridge(bridgeConfig.HitVfxSystem, bridgeConfig.HitVfxType, bridgeConfig.HitVfxScale);

                _vfxRuntime = bridgeConfig.CreateRuntimeBridge(_attachRegistry);

            }
            else
            {
                _effectsBridge = null;
                _vfxRuntime = null;
            }


            GameLog.Log($"[Danmaku] System initialized — bullets: {_worldConfig.MaxBullets}, lasers: {_worldConfig.MaxLasers}, sprays: {_worldConfig.MaxSprays}, trails: {_worldConfig.MaxTrails}");
        }

        private void DisposeSubsystems()
        {
            _bulletRenderer?.Dispose();
            _laserRenderer?.Dispose();
            _laserWarningRenderer?.Dispose();
            _damageNumbers?.Dispose();
            _trailPool?.Dispose();
        }
    }
}
