using UnityEngine;
using MiniGameTemplate.Pool;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 逻辑层与视觉层的桥接器。
    /// 持有 EntityId → View GO 映射，Entity 本身不持有 GO 引用（BC-01.1 不变）。
    /// Phase 1: 使用内置 Debug Prefab（彩色圆 + HP 文本）
    /// Phase 2: 使用 EntityConfigSO.ViewPrefab（策划指定的正式 Prefab）
    /// 
    /// v2.3 变更（SA-005）：内部存储从 Dictionary 改为预分配数组。
    /// 原因：Mono 运行时 Dictionary.GetEnumerator() 每次 foreach 产生 ~40 bytes GC Alloc（装箱），
    /// 违反零 GC 承诺。改为平铺数组 + for 循环遍历，彻底消除 GC。
    /// </summary>
    public class EntityViewBridge
    {
        private const int MAX_VIEWS = 256; // 预分配上限（远超 Phase 1 需求，可调）

        // 预分配数组——零 GC 遍历
        private readonly GameObject[] _viewGOs = new GameObject[MAX_VIEWS];
        private readonly uint[] _viewEntityIds = new uint[MAX_VIEWS];
        private readonly EntityConfigSO[] _viewConfigs = new EntityConfigSO[MAX_VIEWS];
        private int _activeCount;

        private readonly PoolManager _poolManager;
        private readonly PoolDefinition _debugViewPool; // Phase 1 内置 Debug Prefab 的池

        /// <summary>当前活跃视图数量</summary>
        public int ActiveViewCount => _activeCount;

        public EntityViewBridge(PoolManager poolManager, PoolDefinition debugViewPool)
        {
            _poolManager = poolManager;
            _debugViewPool = debugViewPool;
        }

        /// <summary>
        /// Entity 生成时调用——创建/获取对应的 View GO。
        /// 由 EntityManager.Spawn 后立即调用。
        /// </summary>
        public void OnEntitySpawned(Entity entity, EntityConfigSO config)
        {
            if (_activeCount >= MAX_VIEWS)
            {
                Debug.LogWarning("[ViewBridge] 视图数量超限（MAX_VIEWS=" + MAX_VIEWS + "）");
                return;
            }

            PoolDefinition pool = config.ViewPrefab != null
                ? config.ViewPoolDef   // Phase 2: 正式 View
                : _debugViewPool;      // Phase 1: Debug View

            if (pool == null)
            {
                Debug.LogWarning("[ViewBridge] 无可用 PoolDefinition（config=" + config.name + "）");
                return;
            }

            var go = _poolManager.Get(pool);
            go.transform.position = new Vector3(entity.Position.x, entity.Position.y, 0f);

            // append 到数组尾部
            int idx = _activeCount++;
            _viewGOs[idx] = go;
            _viewEntityIds[idx] = entity.Id.Value;
            _viewConfigs[idx] = config;

            // Phase 1: 设置 Debug 颜色
            if (config.ViewPrefab == null)
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = config.DebugColor;

                // 设置 HP 文本初始值
                var tm = go.GetComponentInChildren<TextMesh>();
                if (tm != null)
                {
                    var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                    if (health != null)
                        tm.text = health.CurrentHp + "/" + health.MaxHp;
                    else
                        tm.text = config.DisplayName;
                }
            }
        }

        /// <summary>
        /// 每帧同步位置/朝向/HP 显示——零 GC for 循环遍历。
        /// 由 EntitySystemBootstrap.Update() 在 EntityManager.Tick() 之后调用。
        /// </summary>
        public void SyncAll(EntityManager manager)
        {
            var activeEntities = manager.ActiveEntities;

            for (int i = 0; i < _activeCount; i++)
            {
                var go = _viewGOs[i];
                if (go == null) continue;

                uint entityId = _viewEntityIds[i];

                // 从 EntityManager 查 Entity 位置
                Entity entity = FindEntityById(activeEntities, entityId);
                if (entity == null || !entity.IsAlive)
                {
                    // Entity 已不存在但 View 还在——异常情况，跳过（OnEntityDespawned 会处理回收）
                    continue;
                }

                // 同步位置和朝向
                go.transform.position = new Vector3(entity.Position.x, entity.Position.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, entity.Rotation);

                // Phase 1 Debug View: 更新 HP 文本
                // TODO Phase 2 优化：缓存 TextMesh 引用到预分配数组，避免每帧 GetComponentInChildren
                if (_viewConfigs[i] != null && _viewConfigs[i].ViewPrefab == null)
                {
                    var tm = go.GetComponentInChildren<TextMesh>();
                    if (tm != null)
                    {
                        var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                        if (health != null)
                            tm.text = health.CurrentHp + "/" + health.MaxHp;
                    }
                }
            }
        }

        /// <summary>
        /// Entity 回收时调用——归还 View GO 到池（swap-remove O(1)）。
        /// 由 EntityManager.ExecuteDespawn 后立即调用。
        /// </summary>
        public void OnEntityDespawned(Entity entity, EntityConfigSO config)
        {
            uint targetId = entity.Id.Value;
            for (int i = 0; i < _activeCount; i++)
            {
                if (_viewEntityIds[i] == targetId)
                {
                    // 归还 GO 到池
                    PoolDefinition pool = config.ViewPrefab != null
                        ? config.ViewPoolDef
                        : _debugViewPool;

                    if (pool != null && _viewGOs[i] != null)
                        _poolManager.Return(pool, _viewGOs[i]);

                    // swap-remove
                    int last = _activeCount - 1;
                    if (i != last)
                    {
                        _viewGOs[i] = _viewGOs[last];
                        _viewEntityIds[i] = _viewEntityIds[last];
                        _viewConfigs[i] = _viewConfigs[last];
                    }
                    _viewGOs[last] = null;
                    _viewConfigs[last] = null;
                    _activeCount--;
                    return;
                }
            }
        }

        /// <summary>
        /// 清除所有视图（DespawnAll 时调用）——不归还池（由 DespawnAll 自行处理）。
        /// 安全版本：通过池回收所有 View GO 再清空数组。
        /// </summary>
        public void ClearAllViews()
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (_viewGOs[i] != null)
                {
                    PoolDefinition pool = (_viewConfigs[i] != null && _viewConfigs[i].ViewPrefab != null)
                        ? _viewConfigs[i].ViewPoolDef
                        : _debugViewPool;

                    if (pool != null)
                        _poolManager.Return(pool, _viewGOs[i]);
                }
                _viewGOs[i] = null;
                _viewConfigs[i] = null;
            }
            _activeCount = 0;
        }

        // ──────────── 内部工具 ────────────

        /// <summary>线性查找 Entity（N≤256，可接受）</summary>
        private static Entity FindEntityById(System.Collections.Generic.IReadOnlyList<Entity> entities, uint id)
        {
            int count = entities.Count;
            for (int i = 0; i < count; i++)
            {
                if (entities[i].Id.Value == id)
                    return entities[i];
            }
            return null;
        }
    }
}
