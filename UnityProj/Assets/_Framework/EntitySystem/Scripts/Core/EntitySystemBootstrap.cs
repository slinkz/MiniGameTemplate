using UnityEngine;
using MiniGameTemplate.Pool;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 系统启动器——策划拖到场景根 GO 即可激活整个 Entity 系统。
    /// 负责创建 EntityManager / EntityViewBridge / EntitySpawner 实例并每帧驱动。
    /// 这是策划工作流的"引擎启动钥匙"（v2.6 WF-001）。
    /// 
    /// 时序（§3.12）：
    ///   EntityManager.Tick(dt)          ← Phase A: Entity 组件更新 + Phase B: 延迟销毁
    ///   EntitySpawner.Tick(dt, mgr)     ← 波次推进（AllCleared 判定在延迟销毁后，SA-006）
    ///   EntityViewBridge.SyncAll(mgr)   ← 视觉层位置同步
    /// </summary>
    public class EntitySystemBootstrap : MonoBehaviour
    {
        [Header("调试视觉")]
        [Tooltip("Debug View 的 PoolDefinition（Phase 1 必填）")]
        public PoolDefinition DebugViewPool;

        private EntityManager _entityManager;
        private EntityViewBridge _viewBridge;
        private EntitySpawner _spawner;

        // ──────────── 生命周期 ────────────

        private void Awake()
        {
            // 创建子系统实例
            _entityManager = new EntityManager();
            _viewBridge = new EntityViewBridge(PoolManager.Instance, DebugViewPool);
            _spawner = new EntitySpawner();

            // 注册 EntityManager 事件回调（P1.9 解耦设计）
            _entityManager.OnSpawned += _viewBridge.OnEntitySpawned;
            _entityManager.OnDespawned += _viewBridge.OnEntityDespawned;

            // 注册到全局访问点
            EntityManagerAccessor.Instance = _entityManager;
            EntityManagerAccessor.ViewBridge = _viewBridge;
            EntityManagerAccessor.Spawner = _spawner;

            // 自动发现场景中的 EntitySpawnPoint 并启动
            var points = FindObjectsOfType<EntitySpawnPoint>();
            foreach (var point in points)
            {
                if (point.AutoStartOnEnable && point.WaveConfig != null)
                    _spawner.StartWave(point);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 时序保证（§3.12 / SA-006）：
            // 1. EntityManager.Tick → Phase A + Phase B（延迟销毁执行完毕）
            // 2. EntitySpawner.Tick → 波次推进（此时 CountAliveByConfig 准确）
            // 3. EntityViewBridge.SyncAll → 视觉同步
            _entityManager.Tick(dt);
            _spawner.Tick(dt, _entityManager);
            _viewBridge.SyncAll(_entityManager);
        }

        private void OnDestroy()
        {
            // 注销事件回调
            if (_entityManager != null)
            {
                _entityManager.OnSpawned -= _viewBridge.OnEntitySpawned;
                _entityManager.OnDespawned -= _viewBridge.OnEntityDespawned;
            }

            // 清理 ViewBridge 中所有 View GO
            _viewBridge?.ClearAllViews();

            // 注销全局访问点
            EntityManagerAccessor.Instance = null;
            EntityManagerAccessor.ViewBridge = null;
            EntityManagerAccessor.Spawner = null;
        }
    }
}
