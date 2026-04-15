using MiniGameTemplate.VFX;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// Default runtime bridge that forwards Danmaku VFX calls to SpriteSheetVFXSystem.
    /// </summary>
    public sealed class DanmakuVFXRuntimeBridge : IDanmakuVFXRuntime
    {
        private readonly SpriteSheetVFXSystem _system;

        public DanmakuVFXRuntimeBridge(SpriteSheetVFXSystem system, IVFXPositionResolver resolver)
        {
            _system = system;
            if (_system != null)
                _system.SetPositionResolver(resolver);
        }

        public void SetTimeScale(float timeScale)
        {
            if (_system != null)
                _system.SetTimeScale(timeScale);
        }

        public void StopAttached(int slot)
        {
            if (_system != null)
                _system.StopAttached(slot);
        }

        public int PlayAttached(VFXTypeSO type, byte attachSourceId, float scale = 1f)
        {
            return _system != null ? _system.PlayAttached(type, attachSourceId, scale) : -1;
        }
    }
}
