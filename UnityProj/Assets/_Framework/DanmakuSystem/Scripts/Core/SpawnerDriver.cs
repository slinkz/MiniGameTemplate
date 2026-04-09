using MiniGameTemplate.Utils;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 发射器驱动器——驱动 <see cref="SpawnerProfileSO"/> 的自动弹幕发射。
    /// 纯逻辑类（非 MonoBehaviour），由 DanmakuSystem 每帧 Tick。
    /// 
    /// 支持最多 8 个并行发射器（8 个 Boss/敌人同时挂弹幕足够了）。
    /// </summary>
    public class SpawnerDriver
    {
        /// <summary>最大并行发射器数量</summary>
        public const int MAX_SPAWNERS = 8;

        private readonly SpawnerState[] _states = new SpawnerState[MAX_SPAWNERS];

        /// <summary>当前活跃发射器数量</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MAX_SPAWNERS; i++)
                    if (_states[i].Active) count++;
                return count;
            }
        }

        /// <summary>
        /// 启动一个发射器。
        /// </summary>
        /// <param name="profile">发射器配置</param>
        /// <param name="originProvider">每帧提供发射原点的委托（通常是 Boss 的 Transform.position）</param>
        /// <param name="baseAngle">基准角度（度）</param>
        /// <returns>发射器槽位索引（0~7），-1 表示已满</returns>
        public int Start(SpawnerProfileSO profile, System.Func<Vector2> originProvider, float baseAngle = 270f)
        {
            if (profile == null || profile.PatternGroups == null || profile.PatternGroups.Length == 0)
                return -1;

            int slot = FindFreeSlot();
            if (slot == -1)
            {
                GameLog.LogWarning("[Danmaku] SpawnerDriver full, cannot start spawner");
                return -1;
            }

            ref var state = ref _states[slot];
            state.Active = true;
            state.Profile = profile;
            state.OriginProvider = originProvider;
            state.BaseAngle = baseAngle;
            state.CurrentGroupIndex = 0;
            state.CooldownTimer = 0f;
            state.Paused = false;

            return slot;
        }

        /// <summary>
        /// 停止一个发射器。
        /// </summary>
        public void Stop(int slot)
        {
            if (slot < 0 || slot >= MAX_SPAWNERS) return;
            _states[slot] = default;
        }

        /// <summary>
        /// 暂停一个发射器（保留状态）。
        /// </summary>
        public void Pause(int slot)
        {
            if (slot < 0 || slot >= MAX_SPAWNERS) return;
            _states[slot].Paused = true;
        }

        /// <summary>
        /// 恢复一个暂停的发射器。
        /// </summary>
        public void Resume(int slot)
        {
            if (slot < 0 || slot >= MAX_SPAWNERS) return;
            _states[slot].Paused = false;
        }

        /// <summary>
        /// 外部控制切换弹幕组（仅 External 模式有效）。
        /// </summary>
        public void SetGroupIndex(int slot, int groupIndex)
        {
            if (slot < 0 || slot >= MAX_SPAWNERS) return;
            ref var state = ref _states[slot];
            if (!state.Active) return;

            if (state.Profile.SwitchMode != SpawnerSwitchMode.External)
            {
                GameLog.LogWarning($"[Danmaku] SpawnerDriver slot {slot} is not in External mode");
                return;
            }

            if (groupIndex >= 0 && groupIndex < state.Profile.PatternGroups.Length)
            {
                state.CurrentGroupIndex = groupIndex;
                state.CooldownTimer = 0f; // 立即发射
            }
        }

        /// <summary>
        /// 替换发射器的 SpawnerProfile（运行时热切换）。
        /// </summary>
        public void SetProfile(int slot, SpawnerProfileSO profile)
        {
            if (slot < 0 || slot >= MAX_SPAWNERS) return;
            ref var state = ref _states[slot];
            if (!state.Active) return;

            state.Profile = profile;
            state.CurrentGroupIndex = 0;
            state.CooldownTimer = 0f;
        }

        /// <summary>
        /// 每帧由 DanmakuSystem 调用。
        /// </summary>
        public void Tick(float dt, DanmakuSystem system)
        {
            for (int i = 0; i < MAX_SPAWNERS; i++)
            {
                ref var state = ref _states[i];
                if (!state.Active || state.Paused) continue;

                var profile = state.Profile;
                if (profile == null || profile.PatternGroups == null || profile.PatternGroups.Length == 0)
                    continue;

                // 冷却递减
                state.CooldownTimer -= dt;
                if (state.CooldownTimer > 0f) continue;

                // 到期——发射当前弹幕组
                var group = profile.PatternGroups[state.CurrentGroupIndex];
                if (group != null)
                {
                    Vector2 origin;
                    try
                    {
                        origin = state.OriginProvider != null
                            ? state.OriginProvider()
                            : Vector2.zero;
                    }
                    catch (System.Exception)
                    {
                        // OriginProvider 委托引用的对象已被销毁，自动停止此发射器
                        GameLog.LogWarning($"[Danmaku] SpawnerDriver slot {i}: OriginProvider threw exception (target destroyed?), auto-stopping");
                        _states[i] = default;
                        continue;
                    }
                    system.FireGroup(group, origin, state.BaseAngle);
                }

                // 重置冷却
                state.CooldownTimer = profile.CooldownBetweenGroups;

                // 切换到下一个弹幕组（External 模式不自动切换）
                if (profile.SwitchMode != SpawnerSwitchMode.External)
                {
                    AdvanceGroupIndex(ref state, profile);
                }
            }
        }

        /// <summary>清除所有发射器。</summary>
        public void ClearAll()
        {
            for (int i = 0; i < MAX_SPAWNERS; i++)
                _states[i] = default;
        }

        private static void AdvanceGroupIndex(ref SpawnerState state, SpawnerProfileSO profile)
        {
            int count = profile.PatternGroups.Length;
            if (count <= 1) return;

            switch (profile.SwitchMode)
            {
                case SpawnerSwitchMode.Sequential:
                    state.CurrentGroupIndex = (state.CurrentGroupIndex + 1) % count;
                    break;

                case SpawnerSwitchMode.Random:
                    // 避免连续同一个
                    int prev = state.CurrentGroupIndex;
                    int attempts = 0;
                    do
                    {
                        state.CurrentGroupIndex = UnityEngine.Random.Range(0, count);
                        attempts++;
                    }
                    while (state.CurrentGroupIndex == prev && attempts < 5);
                    break;
            }
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < MAX_SPAWNERS; i++)
            {
                if (!_states[i].Active) return i;
            }
            return -1;
        }

        /// <summary>发射器内部状态</summary>
        private struct SpawnerState
        {
            public bool Active;
            public bool Paused;
            public SpawnerProfileSO Profile;
            public System.Func<Vector2> OriginProvider;
            public float BaseAngle;
            public int CurrentGroupIndex;
            public float CooldownTimer;
        }
    }
}
