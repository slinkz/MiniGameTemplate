using MiniGameTemplate.Rendering;
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

        // ──── PI-001: 共享 RuntimeAtlasManager ────
        private RuntimeAtlasManager _sharedAtlas;

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

            // 运行时类型注册表（ADR-030）
            _typeRegistry = new DanmakuTypeRegistry();

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

            // PI-001: 共享 RuntimeAtlasManager — DanmakuSystem 统一持有
            _sharedAtlas = null;
            if (_renderConfig != null && _renderConfig.RuntimeAtlasConfig != null)
            {
                _sharedAtlas = new RuntimeAtlasManager();
                _sharedAtlas.Initialize(_renderConfig.RuntimeAtlasConfig);
            }

            // 渲染——注入共享 Atlas
            _bulletRenderer = new BulletRenderer();
            _bulletRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxBullets * 4, _sharedAtlas);

            _laserRenderer = new LaserRenderer();
            _laserRenderer.Initialize(_renderConfig, _typeRegistry,
                _worldConfig.MaxLasers * LaserPool.MAX_SEGMENTS_PER_LASER, _sharedAtlas);

            _laserWarningRenderer = new LaserWarningRenderer();
            _laserWarningRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxLasers, _sharedAtlas);

            // 伤害飘字
            _damageNumbers = new DamageNumberSystem();
            _damageNumbers.Initialize(_renderConfig, _sharedAtlas);

            // 重量拖尾
            _trailPool = new TrailPool(_worldConfig.MaxTrails);
            _trailPool.Initialize(_renderConfig.BulletMaterial, _sharedAtlas);

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

            // PI-001: 共享 Atlas 最后统一 Dispose
            _sharedAtlas?.Dispose();
            _sharedAtlas = null;
        }
    }
}
