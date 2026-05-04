using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 刷怪驱动器——管理 EntitySpawnPoint 的波次推进逻辑。
    /// 由 EntitySystemBootstrap 持有并在 Update 中驱动。
    /// 
    /// Phase 1 实现范围：Timer + AllCleared + OnCallback 三种模式 + Loop 循环。
    /// P2.5：TriggerZone 启动控制由 Bootstrap 层处理（SpawnPoint 级开关），Spawner 不感知。
    /// Phase 1 阵型只实现 Random（AreaRadius 内随机散布）。
    /// </summary>
    public class EntitySpawner
    {
        // ──────────── 常量 ────────────

        private const int MAX_ACTIVE_POINTS = 16; // 同时活跃的刷怪点上限

        // ──────────── 活跃波次状态 ────────────

        private readonly ActiveSpawnState[] _states = new ActiveSpawnState[MAX_ACTIVE_POINTS];
        private int _activeCount;

        /// <summary>所有活跃刷怪点是否全部完成</summary>
        public bool IsAllWavesCleared
        {
            get
            {
                if (_activeCount == 0) return true;
                for (int i = 0; i < _activeCount; i++)
                    if (!_states[i].IsCompleted) return false;
                return true;
            }
        }

        /// <summary>当前活跃刷怪点数量</summary>
        public int ActivePointCount => _activeCount;

        // ──────────── 公共 API ────────────

        /// <summary>
        /// 启动一个刷怪点的波次。由 Bootstrap 在 Awake 中对 AutoStart 的点调用。
        /// </summary>
        public void StartWave(EntitySpawnPoint point)
        {
            if (point == null || point.WaveConfig == null || point.WaveConfig.Waves == null)
            {
                Debug.LogWarning("[EntitySpawner] StartWave: point 或 WaveConfig 为空，跳过");
                return;
            }

            if (_activeCount >= MAX_ACTIVE_POINTS)
            {
                Debug.LogWarning($"[EntitySpawner] 活跃刷怪点已满 ({MAX_ACTIVE_POINTS})，无法启动: {point.gameObject.name}");
                return;
            }

            var state = new ActiveSpawnState
            {
                Point = point,
                Config = point.WaveConfig,
                CurrentWaveIndex = 0,
                IsCompleted = false,
                WaveTimer = 0f,
                IsWaitingForTrigger = false,
                GroupStates = null,
            };

            // 第一波初始化
            InitializeWave(ref state);

            _states[_activeCount] = state;
            _activeCount++;
        }

        /// <summary>
        /// OnCallback 模式：外部调用此方法推进下一波。
        /// </summary>
        public void ContinueNextWave(EntitySpawnPoint point)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (_states[i].Point == point && _states[i].IsWaitingForCallback)
                {
                    _states[i].IsWaitingForCallback = false;
                    AdvanceToNextWave(ref _states[i]);
                    return;
                }
            }
            Debug.LogWarning($"[EntitySpawner] ContinueNextWave: 找不到正在等待回调的 point: {point?.gameObject.name}");
        }

        /// <summary>
        /// 停止指定刷怪点（清除其状态）。
        /// </summary>
        public void StopWave(EntitySpawnPoint point)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (_states[i].Point == point)
                {
                    RemoveState(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 停止所有刷怪点。
        /// </summary>
        public void StopAll()
        {
            _activeCount = 0;
        }

        /// <summary>
        /// 重启所有刷怪点（DespawnAll 后调用）。
        /// </summary>
        public void RestartAll()
        {
            // 重新初始化所有已注册的刷怪点（从第一波开始）
            for (int i = 0; i < _activeCount; i++)
            {
                _states[i].CurrentWaveIndex = 0;
                _states[i].IsCompleted = false;
                _states[i].WaveTimer = 0f;
                _states[i].IsWaitingForTrigger = false;
                _states[i].IsWaitingForCallback = false;
                InitializeWave(ref _states[i]);
            }
        }

        // ──────────── 每帧驱动 ────────────

        /// <summary>
        /// 每帧由 Bootstrap 在 EntityManager.Tick() 之后调用。
        /// SA-006 时序保证：Phase B 延迟销毁已执行完毕再调用本方法。
        /// </summary>
        public void Tick(float dt, EntityManager entityManager)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                ref var state = ref _states[i];
                if (state.IsCompleted) continue;

                // 处理组内逐个生成（SpawnInterval）
                TickGroupSpawning(ref state, dt, entityManager);

                // 处理波次推进逻辑
                TickWaveAdvance(ref state, dt, entityManager);
            }
        }

        // ──────────── 组内生成逻辑 ────────────

        private void TickGroupSpawning(ref ActiveSpawnState state, float dt, EntityManager entityManager)
        {
            if (state.GroupStates == null) return;

            for (int g = 0; g < state.GroupStates.Length; g++)
            {
                ref var gs = ref state.GroupStates[g];
                if (gs.IsGroupDone) continue;

                float interval = gs.Group.SpawnInterval;

                if (interval <= 0f)
                {
                    // SpawnInterval=0：一帧内全量生成（不依赖 Timer）
                    while (gs.SpawnedCount < gs.Group.Count)
                    {
                        var formCfg = gs.Group.FormationParams;
                        Vector2 pos = GetSpawnPosition(state.Point, gs.Group.Formation, ref formCfg, gs.SpawnedCount, gs.Group.Count);
                        var entity = entityManager.Spawn(gs.Group.EntityConfig, pos, 270f);
                        if (entity != null)
                        {
                            entity.Camp = gs.Group.Camp;
                        }
                        gs.SpawnedCount++;
                    }
                }
                else
                {
                    // SpawnInterval>0：逐个生成，每帧按计时器推进
                    gs.SpawnTimer += dt;
                    while (gs.SpawnedCount < gs.Group.Count && gs.SpawnTimer >= interval)
                    {
                        gs.SpawnTimer -= interval;

                        var formCfg = gs.Group.FormationParams;
                        Vector2 pos = GetSpawnPosition(state.Point, gs.Group.Formation, ref formCfg, gs.SpawnedCount, gs.Group.Count);
                        var entity = entityManager.Spawn(gs.Group.EntityConfig, pos, 270f);
                        if (entity != null)
                        {
                            entity.Camp = gs.Group.Camp;
                        }
                        gs.SpawnedCount++;
                    }
                }

                if (gs.SpawnedCount >= gs.Group.Count)
                {
                    gs.IsGroupDone = true;
                }
            }
        }

        // ──────────── 波次推进逻辑 ────────────

        private void TickWaveAdvance(ref ActiveSpawnState state, float dt, EntityManager entityManager)
        {
            // 如果还在等待 OnCallback 回调，不推进
            if (state.IsWaitingForCallback) return;

            // 如果当前波还没生成完，不推进
            if (!IsCurrentWaveSpawnDone(ref state)) return;

            // ── 语义：下一波的 TriggerMode 决定"何时开始下一波" ──
            // 即 TriggerMode 描述的是"我这一波在什么条件满足后才出场"
            // 对于最后一波 / Loop 尾：用当前波自己的 TriggerMode 判定是否结束

            int nextIndex = state.CurrentWaveIndex + 1;
            bool isLastWave = nextIndex >= state.Config.Waves.Length;

            // 确定生效的 TriggerMode：
            // - 如果还有下一波 → 看下一波的 TriggerMode（下一波何时出场）
            // - 如果是最后一波 → 看当前波自己的 TriggerMode（用于 Loop 判定 / 通关判定）
            WaveTriggerMode effectiveMode;
            float effectiveDelay;
            if (!isLastWave)
            {
                var nextWave = state.Config.Waves[nextIndex];
                effectiveMode = nextWave.TriggerMode;
                effectiveDelay = nextWave.TriggerDelay;
            }
            else
            {
                var currentWave = state.Config.Waves[state.CurrentWaveIndex];
                effectiveMode = currentWave.TriggerMode;
                effectiveDelay = currentWave.TriggerDelay;
            }

            switch (effectiveMode)
            {
                case WaveTriggerMode.Timer:
                    if (!state.IsWaitingForTrigger)
                    {
                        // 刚完成本波所有生成，开始计时
                        state.IsWaitingForTrigger = true;
                        state.WaveTimer = 0f;
                    }
                    state.WaveTimer += dt;
                    if (state.WaveTimer >= effectiveDelay)
                    {
                        state.IsWaitingForTrigger = false;
                        AdvanceToNextWave(ref state);
                    }
                    break;

                case WaveTriggerMode.AllCleared:
                    // 检查当前波所有 Entity 是否全灭
                    if (IsCurrentWaveAllCleared(ref state, entityManager))
                    {
                        AdvanceToNextWave(ref state);
                    }
                    break;

                case WaveTriggerMode.OnCallback:
                    // 当前波生成完毕后等待外部回调
                    if (!state.IsWaitingForCallback)
                    {
                        state.IsWaitingForCallback = true;
                    }
                    break;
            }
        }

        private void AdvanceToNextWave(ref ActiveSpawnState state)
        {
            int nextIndex = state.CurrentWaveIndex + 1;

            if (nextIndex >= state.Config.Waves.Length)
            {
                // 最后一波结束
                if (state.Config.Loop)
                {
                    // 循环模式：跳转到 LoopStartWave
                    state.CurrentWaveIndex = Mathf.Clamp(state.Config.LoopStartWave, 0, state.Config.Waves.Length - 1);
                    InitializeWave(ref state);
                }
                else
                {
                    state.IsCompleted = true;
                }
            }
            else
            {
                state.CurrentWaveIndex = nextIndex;
                InitializeWave(ref state);
            }
        }

        // ──────────── 工具方法 ────────────

        private void InitializeWave(ref ActiveSpawnState state)
        {
            var wave = state.Config.Waves[state.CurrentWaveIndex];
            int groupCount = wave.Groups?.Length ?? 0;
            state.GroupStates = new GroupSpawnState[groupCount];
            state.WaveTimer = 0f;
            state.IsWaitingForTrigger = false;

            for (int i = 0; i < groupCount; i++)
            {
                state.GroupStates[i] = new GroupSpawnState
                {
                    Group = wave.Groups[i],
                    SpawnedCount = 0,
                    SpawnTimer = wave.Groups[i].SpawnInterval, // 首次立即生成
                    IsGroupDone = false,
                };
            }
        }

        private bool IsCurrentWaveSpawnDone(ref ActiveSpawnState state)
        {
            if (state.GroupStates == null) return true;
            for (int i = 0; i < state.GroupStates.Length; i++)
                if (!state.GroupStates[i].IsGroupDone) return false;
            return true;
        }

        private bool IsCurrentWaveAllCleared(ref ActiveSpawnState state, EntityManager entityManager)
        {
            // 检查当前波所有 Group 的 EntityConfig 在场活跃数是否为 0
            if (state.GroupStates == null) return true;
            for (int i = 0; i < state.GroupStates.Length; i++)
            {
                var config = state.GroupStates[i].Group.EntityConfig;
                if (config != null && entityManager.CountAliveByConfig(config) > 0)
                    return false;
            }
            return true;
        }

        private Vector2 GetSpawnPosition(EntitySpawnPoint point, SpawnFormation formation,
            ref FormationConfig cfg, int index, int total)
        {
            Vector2 center = (Vector2)point.transform.position;
            float areaRadius = point.AreaRadius;
            Vector2 pos;

            switch (formation)
            {
                case SpawnFormation.Line:
                    pos = CalcLine(center, areaRadius, ref cfg, index, total);
                    break;

                case SpawnFormation.Circle:
                    pos = CalcCircle(center, areaRadius, ref cfg, index, total);
                    break;

                case SpawnFormation.Grid:
                    pos = CalcGrid(center, areaRadius, ref cfg, index, total);
                    break;

                case SpawnFormation.Random:
                default:
                {
                    // cfg.Radius > 0 时用自定义半径；否则用 SpawnPoint.AreaRadius
                    float rRadius = cfg.Radius > 0f ? cfg.Radius : areaRadius;
                    // 底边对齐：整体上移 rRadius，使最低点 = center.y
                    pos = center + new Vector2(0f, rRadius) + Random.insideUnitCircle * rRadius;
                    break;
                }
            }

            // 通用噪声
            if (cfg.Jitter > 0f)
                pos += Random.insideUnitCircle * cfg.Jitter;

            return pos;
        }

        /// <summary>
        /// Line 阵型：沿指定角度方向等间距排列。
        /// Spacing > 0 用固定间距；Spacing = 0 时自动 = 2*AreaRadius/(total-1)。
        /// Angle = 0 水平，90 垂直。
        /// </summary>
        private static Vector2 CalcLine(Vector2 center, float areaRadius,
            ref FormationConfig cfg, int index, int total)
        {
            float spacing = cfg.Spacing > 0f ? cfg.Spacing : (total > 1 ? areaRadius * 2f / (total - 1) : 0f);
            float totalSpan = spacing * (total - 1);
            float t = total > 1 ? (float)index / (total - 1) : 0.5f;
            float offset = -totalSpan * 0.5f + t * totalSpan;

            float rad = cfg.Angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            return center + dir * offset;
        }

        /// <summary>
        /// Circle 阵型：沿圆周等角度分布。
        /// Radius > 0 用固定半径；Radius = 0 时用 AreaRadius。
        /// AngleOffset 控制起始角度。
        /// 底边对齐 center（整体上移 radius），确保所有怪从屏幕外进场。
        /// </summary>
        private static Vector2 CalcCircle(Vector2 center, float areaRadius,
            ref FormationConfig cfg, int index, int total)
        {
            float radius = cfg.Radius > 0f ? cfg.Radius : areaRadius;
            float angleStep = total > 0 ? 360f / total : 0f;
            float angle = cfg.Angle + angleStep * index; // cfg.Angle 作为起始角度偏移
            float rad = angle * Mathf.Deg2Rad;
            // 底边对齐：整体上移 radius，使阵型最低点 = center.y
            Vector2 offset = new Vector2(0f, radius);
            return center + offset + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        /// <summary>
        /// Grid 阵型：行列网格排列，底边对齐 center（整体上移）。
        /// Columns = 0 时自动取 ceil(√total)。
        /// Spacing > 0 用固定间距；Spacing = 0 时自动 = 2*AreaRadius/(cols-1)。
        /// 行序从下到上：row=0 是最底行（= center.y），向上递增。
        /// </summary>
        private static Vector2 CalcGrid(Vector2 center, float areaRadius,
            ref FormationConfig cfg, int index, int total)
        {
            int cols = cfg.Columns > 0 ? cfg.Columns : Mathf.CeilToInt(Mathf.Sqrt(total));
            if (cols < 1) cols = 1;
            int rows = Mathf.CeilToInt((float)total / cols);

            int col = index % cols;
            int row = index / cols;

            float spacing = cfg.Spacing > 0f ? cfg.Spacing : (cols > 1 ? areaRadius * 2f / (cols - 1) : 0f);
            float gridW = spacing * (cols - 1);
            float gridH = spacing * (rows - 1);

            float x = cols > 1 ? -gridW * 0.5f + col * spacing : 0f;
            // 底边对齐：row=0 在 center.y，向上递增
            float y = row * spacing;

            return center + new Vector2(x, y);
        }

        private void RemoveState(int index)
        {
            // swap-remove O(1)
            int last = _activeCount - 1;
            if (index != last)
            {
                _states[index] = _states[last];
            }
            _states[last] = default;
            _activeCount--;
        }

        // ──────────── 内部状态结构 ────────────

        private struct ActiveSpawnState
        {
            public EntitySpawnPoint Point;
            public EntitySpawnWaveSO Config;
            public int CurrentWaveIndex;
            public bool IsCompleted;
            public float WaveTimer;
            public bool IsWaitingForTrigger;
            public bool IsWaitingForCallback;
            public GroupSpawnState[] GroupStates;
        }

        private struct GroupSpawnState
        {
            public SpawnGroup Group;
            public int SpawnedCount;
            public float SpawnTimer;
            public bool IsGroupDone;
        }
    }
}
