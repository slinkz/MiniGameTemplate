using MiniGameTemplate.VFX;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// Default runtime bridge that forwards Danmaku VFX calls to SpriteSheetVFXSystem.
    /// R4.0：新增 TickVFX / RenderVFX 转发，由 DanmakuSystem 管线统一驱动。
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

        public int Play(VFXTypeSO type, UnityEngine.Vector3 position, float scale = 1f, float rotationDegrees = 0f)
        {
            return _system != null ? _system.Play(type, position, scale, rotationDegrees) : -1;
        }

        public int PlayAttached(VFXTypeSO type, byte attachSourceId, float scale = 1f)
        {
            return _system != null ? _system.PlayAttached(type, attachSourceId, scale) : -1;
        }

        public void TickVFX(float deltaTime)
        {
            if (_system != null)
                _system.TickVFX(deltaTime);
        }

        public void RenderVFX()
        {
            if (_system != null)
                _system.RenderVFX();
        }
    }
}
