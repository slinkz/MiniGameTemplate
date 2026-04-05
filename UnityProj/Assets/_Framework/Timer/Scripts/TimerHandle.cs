using System;

namespace MiniGameTemplate.Timing
{
    /// <summary>
    /// Handle to a running timer. Use to cancel, pause, or resume.
    /// </summary>
    public class TimerHandle
    {
        internal float Duration;
        internal float Elapsed;
        internal bool IsRepeating;
        internal bool UseRealTime;
        internal Action Callback;
        internal bool IsCancelled;
        internal bool IsPaused;

        /// <summary>
        /// Cancel this timer. It will not fire again.
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
        }

        /// <summary>
        /// Pause this timer. Elapsed time is preserved.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Resume a paused timer.
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// How much time remains until the next fire.
        /// </summary>
        public float Remaining => Duration - Elapsed;

        /// <summary>
        /// Whether this timer is still active (not cancelled, not completed for non-repeating).
        /// </summary>
        public bool IsActive => !IsCancelled;
    }
}
