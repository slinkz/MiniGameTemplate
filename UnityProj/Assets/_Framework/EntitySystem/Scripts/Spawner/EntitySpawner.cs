using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 刷怪驱动器——管理 EntitySpawnPoint 的波次推进逻辑。
    /// 由 EntitySystemBootstrap 持有并在 Update 中驱动。
    /// 
    /// Phase 1 实现范围：Timer + AllCleared + OnCallback 三种模式 + Loop 循环。
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

                gs.SpawnTimer += dt;
                float interval = gs.Group.SpawnInterval;
                if (interval <= 0f) interval = 0f; // 0 间隔 = 本帧全部生成

                while (gs.SpawnedCount < gs.Group.Count && gs.SpawnTimer >= interval)
                {
                    gs.SpawnTimer -= Mathf.Max(interval, 0.001f); // 防止 0 间隔死循环

                    // 计算生成位置（Phase 1 只实现 Random）
                    Vector2 pos = GetSpawnPosition(state.Point, gs.Group.Formation, gs.SpawnedCount, gs.Group.Count);

                    // 生成 Entity
                    var entity = entityManager.Spawn(gs.Group.EntityConfig, pos, 0f);
                    if (entity != null)
                    {
                        entity.Camp = gs.Group.Camp;
                    }
                    gs.SpawnedCount++;

                    // 如果间隔为 0，一帧内全部生成
                    if (interval <= 0f) continue;
                    break; // 间隔 > 0 时每帧只生成一个
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

            // 根据触发模式判断是否推进到下一波
            var wave = state.Config.Waves[state.CurrentWaveIndex];

            switch (wave.TriggerMode)
            {
                case WaveTriggerMode.Timer:
                    if (!state.IsWaitingForTrigger)
                    {
                        // 刚完成本波所有生成，开始计时
                        state.IsWaitingForTrigger = true;
                        state.WaveTimer = 0f;
                    }
                    state.WaveTimer += dt;
                    if (state.WaveTimer >= wave.TriggerDelay)
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

        private Vector2 GetSpawnPosition(EntitySpawnPoint point, SpawnFormation formation, int index, int total)
        {
            Vector2 center = (Vector2)point.transform.position;
            float radius = point.AreaRadius;

            switch (formation)
            {
                case SpawnFormation.Random:
                default:
                    // Phase 1：AreaRadius 内随机散布
                    Vector2 offset = Random.insideUnitCircle * radius;
                    return center + offset;

                case SpawnFormation.Line:
                    // Phase 2 实现：暂时 fallback 到 Random
                    return center + Random.insideUnitCircle * radius;

                case SpawnFormation.Circle:
                    // Phase 2 实现：暂时 fallback 到 Random
                    return center + Random.insideUnitCircle * radius;

                case SpawnFormation.Grid:
                    // Phase 2 实现：暂时 fallback 到 Random
                    return center + Random.insideUnitCircle * radius;
            }
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
