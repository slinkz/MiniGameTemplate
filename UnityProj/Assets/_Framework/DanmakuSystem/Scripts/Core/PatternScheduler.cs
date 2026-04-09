using MiniGameTemplate.Utils;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕组合执行引擎——预分配 64 槽，驱动 PatternGroupSO 的延迟/重复/旋转编排。
    /// 每帧由 DanmakuSystem.Update 驱动 Tick()。
    /// </summary>
    public class PatternScheduler
    {
        public const int MAX_TASKS = 64;

        private readonly ScheduleTask[] _tasks = new ScheduleTask[MAX_TASKS];
        private int _activeTasks;
        private int _peakTasks;
        private int _totalScheduled;

        /// <summary>当前活跃调度任务数</summary>
        public int ActiveTasks => _activeTasks;

        /// <summary>历史峰值活跃任务数（调试用）</summary>
        public int PeakTasks => _peakTasks;

        /// <summary>累计调度总次数（调试用）</summary>
        public int TotalScheduled => _totalScheduled;

        /// <summary>
        /// 调度一个弹幕组合。
        /// </summary>
        /// <param name="group">弹幕组合配置</param>
        /// <param name="origin">发射原点</param>
        /// <param name="baseAngle">基准角度（度）</param>
        /// <param name="playerPos">调度时玩家位置（AimAtPlayer 快照用）</param>
        public void Schedule(PatternGroupSO group, Vector2 origin, float baseAngle, Vector2 playerPos)
        {
            if (group == null || group.Entries == null || group.Entries.Length == 0) return;

            for (int repeat = 0; repeat < group.RepeatCount; repeat++)
            {
                float repeatDelay = repeat * group.RepeatInterval;
                float repeatAngle = repeat * group.AngleIncrementPerRepeat;

                for (int e = 0; e < group.Entries.Length; e++)
                {
                    ref var entry = ref group.Entries[e];
                    if (entry.Pattern == null) continue;

                    int burstCount = Mathf.Max(1, entry.Pattern.BurstCount);
                    for (int b = 0; b < burstCount; b++)
                    {
                        int slot = FindFreeSlot();
                        if (slot == -1)
                        {
                            GameLog.LogWarning("[Danmaku] PatternScheduler full, dropping tasks");
                            return;
                        }

                        float entryAngle = baseAngle + repeatAngle;
                        if (entry.AngleOverride >= 0)
                            entryAngle = entry.AngleOverride + repeatAngle;
                        if (entry.AimAtPlayer)
                        {
                            Vector2 dir = playerPos - origin;
                            entryAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + repeatAngle;
                        }

                        // 每次 Burst 叠加 AnglePerShot
                        entryAngle += entry.Pattern.AnglePerShot * b;

                        ref var task = ref _tasks[slot];
                        task.Pattern = entry.Pattern;
                        task.Origin = origin;
                        task.Angle = entryAngle;
                        task.Delay = repeatDelay + entry.Delay + b * entry.Pattern.BurstInterval;
                        task.Elapsed = 0f;
                        task.Active = true;
                        _activeTasks++;
                        _totalScheduled++;
                        if (_activeTasks > _peakTasks) _peakTasks = _activeTasks;
                    }
                }
            }
        }

        /// <summary>
        /// 调度单个弹幕（无 PatternGroup，直接发射含 Burst）。
        /// </summary>
        public void ScheduleSingle(BulletPatternSO pattern, Vector2 origin, float baseAngle)
        {
            if (pattern == null) return;

            int burstCount = Mathf.Max(1, pattern.BurstCount);
            for (int b = 0; b < burstCount; b++)
            {
                int slot = FindFreeSlot();
                if (slot == -1)
                {
                    GameLog.LogWarning("[Danmaku] PatternScheduler full, dropping tasks");
                    return;
                }

                float angle = baseAngle + pattern.AnglePerShot * b;

                ref var task = ref _tasks[slot];
                task.Pattern = pattern;
                task.Origin = origin;
                task.Angle = angle;
                task.Delay = b * pattern.BurstInterval;
                task.Elapsed = 0f;
                task.Active = true;
                _activeTasks++;
                _totalScheduled++;
                if (_activeTasks > _peakTasks) _peakTasks = _activeTasks;
            }
        }

        /// <summary>
        /// 每帧由 DanmakuSystem 调用——推进时间，到期的任务触发发射。
        /// </summary>
        public void Tick(float dt, BulletWorld world, DanmakuTypeRegistry registry,
            DifficultyProfileSO difficulty = null, TrailPool trailPool = null)
        {
            for (int i = 0; i < MAX_TASKS; i++)
            {
                ref var task = ref _tasks[i];
                if (!task.Active) continue;

                task.Elapsed += dt;
                if (task.Elapsed < task.Delay) continue;

                // 到期——执行发射
                BulletSpawner.Fire(task.Pattern, task.Origin, task.Angle, world, registry, difficulty, trailPool);

                // 任务完成，释放槽位
                task.Active = false;
                _activeTasks--;
            }
        }

        /// <summary>清除所有待执行任务。</summary>
        public void ClearAll()
        {
            for (int i = 0; i < MAX_TASKS; i++)
                _tasks[i].Active = false;
            _activeTasks = 0;
            _peakTasks = 0;
        }

        /// <summary>重置累计统计。</summary>
        public void ResetStats()
        {
            _peakTasks = _activeTasks;
            _totalScheduled = 0;
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < MAX_TASKS; i++)
            {
                if (!_tasks[i].Active) return i;
            }
            return -1;
        }

        /// <summary>调度任务内部数据。</summary>
        private struct ScheduleTask
        {
            public BulletPatternSO Pattern;
            public Vector2 Origin;
            public float Angle;
            public float Delay;
            public float Elapsed;
            public bool Active;
        }
    }
}
