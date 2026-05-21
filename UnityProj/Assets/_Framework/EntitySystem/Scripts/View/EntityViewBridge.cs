using UnityEngine;
using MiniGameTemplate.Pool;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 逻辑层与视觉层的桥接器。
    /// 持有 EntityId → View GO 映射，Entity 本身不持有 GO 引用（BC-01.1 不变）。
    /// 
    /// Phase 1: 使用内置 Debug Prefab（彩色圆 + HP 文本）
    /// Phase 2: 使用 EntityConfigSO.ViewPrefab（策划指定的正式 Prefab）+ IEntityView 接口
    /// 
    /// v2.3 变更（SA-005）：内部存储从 Dictionary 改为预分配数组。
    /// P2.1 变更：缓存 SpriteRenderer/TextMesh/IEntityView 引用（消除每帧 GetComponentInChildren GC）。
    /// </summary>
    public class EntityViewBridge
    {
        private const int MAX_VIEWS = 256; // 预分配上限

        // ──────────── 预分配数组——零 GC 遍历 ────────────
        private readonly GameObject[] _viewGOs = new GameObject[MAX_VIEWS];
        private readonly uint[] _viewEntityIds = new uint[MAX_VIEWS];
        private readonly EntityConfigSO[] _viewConfigs = new EntityConfigSO[MAX_VIEWS];

        // P2.1 新增：缓存组件引用（消除每帧 GetComponentInChildren）
        private readonly IEntityView[] _entityViews = new IEntityView[MAX_VIEWS];
        private readonly SpriteRenderer[] _cachedSRs = new SpriteRenderer[MAX_VIEWS];
        private readonly TextMesh[] _cachedTMs = new TextMesh[MAX_VIEWS];
        private readonly bool[] _isOfficialView = new bool[MAX_VIEWS]; // true = 正式 View，false = Debug View

        private int _activeCount;

        private readonly PoolManager _poolManager;
        private readonly PoolDefinition _debugViewPool;
        private EntityHitReactionHandler _hitHandler;

        /// <summary>当前活跃视图数量</summary>
        public int ActiveViewCount => _activeCount;

        public EntityViewBridge(PoolManager poolManager, PoolDefinition debugViewPool)
        {
            _poolManager = poolManager;
            _debugViewPool = debugViewPool;
        }

        /// <summary>设置受击管线引用（Bootstrap 初始化后调用）</summary>
        public void SetHitReactionHandler(EntityHitReactionHandler handler)
        {
            _hitHandler = handler;
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

            bool isOfficial = config.ViewPrefab != null;
            PoolDefinition pool = isOfficial
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
            _isOfficialView[idx] = isOfficial;

            // P2.1：缓存组件引用（一次性 GetComponent，不再每帧查找）
            _cachedSRs[idx] = go.GetComponentInChildren<SpriteRenderer>();
            _cachedTMs[idx] = go.GetComponentInChildren<TextMesh>();
            _entityViews[idx] = go.GetComponent<IEntityView>();

            if (isOfficial)
            {
                // 正式 View：通过 IEntityView 接口初始化
                if (_entityViews[idx] != null)
                {
                    var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                    _entityViews[idx].OnViewInit(new EntityViewContext
                    {
                        Config = config,
                        EntityId = entity.Id,
                        Position = entity.Position,
                        Rotation = entity.Rotation,
                        MaxHp = health != null ? health.MaxHp : config.MaxHp,
                        CurrentHp = health != null ? health.CurrentHp : config.MaxHp,
                    });
                }
            }
            else
            {
                // Debug View：设置颜色和初始 HP 文本
                if (_cachedSRs[idx] != null)
                    _cachedSRs[idx].color = config.DebugColor;

                if (_cachedTMs[idx] != null)
                {
                    var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                    if (health != null)
                        _cachedTMs[idx].text = health.CurrentHp + "/" + health.MaxHp;
                    else
                        _cachedTMs[idx].text = config.DisplayName;
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
                    continue;
                }

                // 同步位置和朝向
                go.transform.position = new Vector3(entity.Position.x, entity.Position.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, entity.Rotation);

                if (_isOfficialView[i])
                {
                    // ── 正式 View：通过 IEntityView 接口同步 ──
                    var view = _entityViews[i];
                    if (view != null)
                    {
                        var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                        var anim = entity.GetComponent(ComponentType.Animation) as AnimationComponent;

                        view.OnViewSync(new EntityViewSyncData
                        {
                            Position = entity.Position,
                            Rotation = entity.Rotation,
                            CurrentHp = health != null ? health.CurrentHp : 0,
                            MaxHp = health != null ? health.MaxHp : 0,
                            CurrentAnimId = anim != null ? anim.CurrentAnimId : 0,
                            IsAlive = entity.IsAlive,
                        });

                        // P2.1：闪白通知走 IEntityView
                        if (_hitHandler != null)
                        {
                            if (_hitHandler.IsFlashing(entity.Id))
                            {
                                float progress = _hitHandler.GetFlashProgress(entity.Id);
                                if (progress < 0.01f) // 刚开始闪
                                {
                                    view.OnViewHitFlash(
                                        _viewConfigs[i].HitFlashColor,
                                        _viewConfigs[i].HitFlashDuration);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ── Debug View：直接操作缓存的组件引用 ──
                    var tm = _cachedTMs[i];
                    if (tm != null)
                    {
                        var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                        if (health != null)
                            tm.text = health.CurrentHp + "/" + health.MaxHp;

                        // HP 文本不跟随 Entity 旋转
                        tm.transform.rotation = Quaternion.identity;
                        tm.transform.position = go.transform.position + new Vector3(0f, 0.6f, 0f);
                    }

                    // 闪白表现——SpriteRenderer 颜色
                    if (_hitHandler != null)
                    {
                        var sr = _cachedSRs[i];
                        if (sr != null)
                        {
                            if (_hitHandler.IsFlashing(entity.Id))
                            {
                                float progress = _hitHandler.GetFlashProgress(entity.Id);
                                sr.color = Color.Lerp(
                                    _viewConfigs[i].HitFlashColor,
                                    _viewConfigs[i].DebugColor,
                                    progress);
                            }
                            else
                            {
                                sr.color = _viewConfigs[i].DebugColor;
                            }
                        }
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
                    // P2.1：通知 IEntityView 重置
                    if (_entityViews[i] != null)
                        _entityViews[i].OnViewReset();

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
                        _entityViews[i] = _entityViews[last];
                        _cachedSRs[i] = _cachedSRs[last];
                        _cachedTMs[i] = _cachedTMs[last];
                        _isOfficialView[i] = _isOfficialView[last];
                    }
                    _viewGOs[last] = null;
                    _viewConfigs[last] = null;
                    _entityViews[last] = null;
                    _cachedSRs[last] = null;
                    _cachedTMs[last] = null;
                    _activeCount--;
                    return;
                }
            }
        }

        /// <summary>
        /// 清除所有视图（DespawnAll 时调用）。
        /// </summary>
        public void ClearAllViews()
        {
            for (int i = 0; i < _activeCount; i++)
            {
                // 通知 IEntityView 重置
                if (_entityViews[i] != null)
                    _entityViews[i].OnViewReset();

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
                _entityViews[i] = null;
                _cachedSRs[i] = null;
                _cachedTMs[i] = null;
            }
            _activeCount = 0;
        }

        // ──────────── 公开查询 ────────────

        /// <summary>
        /// 根据 EntityId 查找对应 View GO 的 Transform。
        /// 用途：激光/喷雾挂载源需要 Transform 跟踪（Attached 模式）。
        /// 返回 null 表示该 Entity 尚无视图（如直跑场景测试模式）。
        /// 复杂度 O(N)，N ≤ MAX_VIEWS(256)，非热路径（仅发射瞬间调用一次）。
        /// </summary>
        public Transform GetViewTransform(uint entityId)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (_viewEntityIds[i] == entityId && _viewGOs[i] != null)
                    return _viewGOs[i].transform;
            }
            return null;
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
