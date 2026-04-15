using MiniGameTemplate.VFX;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// Runtime bridge for Danmaku-facing VFX operations.
    /// </summary>
    public interface IDanmakuVFXRuntime
    {
        /// <summary>
        /// Sets the runtime time scale for VFX playback.
        /// </summary>
        void SetTimeScale(float timeScale);

        /// <summary>
        /// Stops an attached VFX instance by slot.
        /// </summary>
        void StopAttached(int slot);

        /// <summary>
        /// Plays an attached VFX instance.
        /// </summary>
        int PlayAttached(VFXTypeSO type, byte attachSourceId, float scale = 1f);
    }
}
