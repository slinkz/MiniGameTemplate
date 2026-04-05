using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Timing
{
    /// <summary>
    /// Centralized timer service. Drives all timers from a single Update loop.
    /// Use TimerService.Instance.Delay() / Repeat() to create timers.
    /// </summary>
    public class TimerService : Singleton<TimerService>
    {
        private readonly List<TimerHandle> _timers = new List<TimerHandle>();
        private readonly List<TimerHandle> _toAdd = new List<TimerHandle>();

        /// <summary>
        /// Create a one-shot timer that fires after the given delay.
        /// </summary>
        public TimerHandle Delay(float seconds, Action callback, bool realTime = false)
        {
            var handle = new TimerHandle
            {
                Duration = seconds,
                Elapsed = 0f,
                IsRepeating = false,
                UseRealTime = realTime,
                Callback = callback
            };
            _toAdd.Add(handle);
            return handle;
        }

        /// <summary>
        /// Create a repeating timer that fires every interval.
        /// </summary>
        public TimerHandle Repeat(float intervalSeconds, Action callback, bool realTime = false)
        {
            var handle = new TimerHandle
            {
                Duration = intervalSeconds,
                Elapsed = 0f,
                IsRepeating = true,
                UseRealTime = realTime,
                Callback = callback
            };
            _toAdd.Add(handle);
            return handle;
        }

        /// <summary>
        /// Cancel all active timers.
        /// </summary>
        public void CancelAll()
        {
            foreach (var t in _timers)
                t.IsCancelled = true;
            foreach (var t in _toAdd)
                t.IsCancelled = true;
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
            }

            // Batch remove all cancelled timers in one pass (O(n) total, not O(n) per removal)
            if (needsCompact)
            {
                _timers.RemoveAll(t => t.IsCancelled);
            }
        }
    }
}
