using MiniGameTemplate.VFX;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// Runtime bridge for Danmaku-facing VFX operations.
    /// R4.0：新增 TickVFX / RenderVFX，由 DanmakuSystem 管线统一驱动。
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
        /// Plays a world-space VFX instance.
        /// </summary>
        int Play(VFXTypeSO type, UnityEngine.Vector3 position, float scale = 1f, float rotationDegrees = 0f);

        /// <summary>
        /// Plays an attached VFX instance.
        /// </summary>
        int PlayAttached(VFXTypeSO type, byte attachSourceId, float scale = 1f);

        /// <summary>
        /// 更新 VFX 逻辑帧（帧动画推进、附着位置同步）。
        /// 由 DanmakuSystem.RunUpdatePipeline 调用。
        /// </summary>
        void TickVFX(float deltaTime);

        /// <summary>
        /// 重建 VFX 渲染数据并提交 DrawMesh。
        /// 由 DanmakuSystem.RunLateUpdatePipeline 在 BeginFrame/EndFrame 区间内调用。
        /// </summary>
        void RenderVFX();
    }
}
