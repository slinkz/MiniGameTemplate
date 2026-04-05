namespace MiniGameTemplate.Timing
{
    /// <summary>
    /// Lightweight handle to a running timer. This is a value type (struct) to avoid
    /// GC pressure from high-frequency timer creation. The actual timer data lives
    /// inside TimerService — this handle is just an ID token.
    ///
    /// Use TimerService.Instance.Cancel/Pause/Resume(handle) to control the timer.
    /// </summary>
    public readonly struct TimerHandle
    {
        /// <summary>
        /// Unique identifier for this timer within TimerService.
        /// A value of 0 means "invalid / no timer".
        /// </summary>
        public readonly int Id;

        internal TimerHandle(int id)
        {
            Id = id;
        }

        /// <summary>Whether this handle points to a valid timer (non-zero id).</summary>
        public bool IsValid => Id != 0;

        public static readonly TimerHandle Invalid = default;
    }
}
