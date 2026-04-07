using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Timing
{
    /// <summary>
    /// Centralized timer service. Drives all timers from a single Update loop.
    /// Use TimerService.Instance.Delay() / Repeat() to create timers.
    ///
    /// Performance: Timer data is stored in flat Lists (struct-of-arrays style).
    /// TimerHandle is a lightweight struct (just an int ID) — no heap allocation per timer.
    /// </summary>
    public class TimerService : Singleton<TimerService>
    {
        /// <summary>Internal timer data — kept inside the service, not on the handle.</summary>
        private struct TimerData
        {
            public int Id;
            public float Duration;
            public float Elapsed;
            public bool IsRepeating;
            public bool UseRealTime;
            public Action Callback;
            public bool IsCancelled;
            public bool IsPaused;
        }

        private readonly List<TimerData> _timers = new List<TimerData>(32);
        private readonly List<TimerData> _toAdd = new List<TimerData>(8);
        private int _nextId = 1; // 0 = invalid

        /// <summary>
        /// Generate next unique timer ID with overflow wrap-around.
        /// Wraps from int.MaxValue back to 1 (skipping 0 which is "invalid").
        /// </summary>
        private int NextId()
        {
            int id = _nextId;
            _nextId = (_nextId == int.MaxValue) ? 1 : _nextId + 1;
            return id;
        }

        /// <summary>
        /// Create a one-shot timer that fires after the given delay.
        /// </summary>
        public TimerHandle Delay(float seconds, Action callback, bool realTime = false)
        {
            int id = NextId();
            _toAdd.Add(new TimerData
            {
                Id = id,
                Duration = seconds,
                Elapsed = 0f,
                IsRepeating = false,
                UseRealTime = realTime,
                Callback = callback
            });
            return new TimerHandle(id);
        }

        /// <summary>
        /// Create a repeating timer that fires every interval.
        /// </summary>
        public TimerHandle Repeat(float intervalSeconds, Action callback, bool realTime = false)
        {
            int id = NextId();
            _toAdd.Add(new TimerData
            {
                Id = id,
                Duration = intervalSeconds,
                Elapsed = 0f,
                IsRepeating = true,
                UseRealTime = realTime,
                Callback = callback
            });
            return new TimerHandle(id);
        }

        /// <summary>Cancel a specific timer by handle.</summary>
        public void Cancel(TimerHandle handle)
        {
            if (!handle.IsValid) return;
            SetFlag(handle.Id, cancelled: true);
        }

        /// <summary>Pause a specific timer by handle.</summary>
        public void Pause(TimerHandle handle)
        {
            if (!handle.IsValid) return;
            SetPaused(handle.Id, true);
        }

        /// <summary>Resume a paused timer by handle.</summary>
        public void Resume(TimerHandle handle)
        {
            if (!handle.IsValid) return;
            SetPaused(handle.Id, false);
        }

        /// <summary>Check whether a timer is still active.</summary>
        public bool IsActive(TimerHandle handle)
        {
            if (!handle.IsValid) return false;
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Id == handle.Id)
                    return !_timers[i].IsCancelled;
            }
            // Also check pending list
            for (int i = 0; i < _toAdd.Count; i++)
            {
                if (_toAdd[i].Id == handle.Id)
                    return !_toAdd[i].IsCancelled;
            }
            return false;
        }

        /// <summary>Get remaining time for a timer. Returns 0 if not found.</summary>
        public float GetRemaining(TimerHandle handle)
        {
            if (!handle.IsValid) return 0f;
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Id == handle.Id)
                    return Mathf.Max(0f, _timers[i].Duration - _timers[i].Elapsed);
            }
            return 0f;
        }

        /// <summary>
        /// Cancel all active timers.
        /// </summary>
        public void CancelAll()
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                var t = _timers[i];
                t.IsCancelled = true;
                _timers[i] = t;
            }
            for (int i = 0; i < _toAdd.Count; i++)
            {
                var t = _toAdd[i];
                t.IsCancelled = true;
                _toAdd[i] = t;
            }
        }

        private void Update()
        {
            // Add pending timers
            if (_toAdd.Count > 0)
            {
                _timers.AddRange(_toAdd);
                _toAdd.Clear();
            }

            float dt = Time.deltaTime;
            float unscaledDt = Time.unscaledDeltaTime;
            bool needsCompact = false;

            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                var t = _timers[i];

                if (t.IsCancelled)
                {
                    needsCompact = true;
                    continue;
                }

                if (t.IsPaused) continue;

                t.Elapsed += t.UseRealTime ? unscaledDt : dt;

                if (t.Elapsed >= t.Duration)
                {
                    t.Callback?.Invoke();

                    // Callback may cancel this timer (for example, repeating gameplay timer
                    // cancels itself when a round ends). If we continue writing back local state,
                    // we could accidentally resurrect a cancelled timer.
                    if (_timers[i].IsCancelled)
                    {
                        needsCompact = true;
                        continue;
                    }

                    if (t.IsRepeating)
                    {
                        t.Elapsed -= t.Duration; // Preserve overflow for accuracy
                    }
                    else
                    {
                        t.IsCancelled = true;
                        needsCompact = true;
                    }
                }


                _timers[i] = t; // Write back (struct)
            }

            // Batch remove all cancelled timers in one pass (O(n) total, not O(n) per removal)
            if (needsCompact)
            {
                _timers.RemoveAll(t => t.IsCancelled);
            }
        }

        private void SetFlag(int id, bool cancelled)
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Id == id)
                {
                    var t = _timers[i];
                    t.IsCancelled = cancelled;
                    _timers[i] = t;
                    return;
                }
            }
            for (int i = 0; i < _toAdd.Count; i++)
            {
                if (_toAdd[i].Id == id)
                {
                    var t = _toAdd[i];
                    t.IsCancelled = cancelled;
                    _toAdd[i] = t;
                    return;
                }
            }
        }

        private void SetPaused(int id, bool paused)
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Id == id)
                {
                    var t = _timers[i];
                    t.IsPaused = paused;
                    _timers[i] = t;
                    return;
                }
            }
            for (int i = 0; i < _toAdd.Count; i++)
            {
                if (_toAdd[i].Id == id)
                {
                    var t = _toAdd[i];
                    t.IsPaused = paused;
                    _toAdd[i] = t;
                    return;
                }
            }
        }
    }
}
