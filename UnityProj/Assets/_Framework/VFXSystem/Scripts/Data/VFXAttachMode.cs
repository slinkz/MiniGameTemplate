namespace MiniGameTemplate.VFX
{
    /// <summary>VFX 附着模式（ADR-013）</summary>
    public enum VFXAttachMode : byte
    {
        /// <summary>世界空间固定位置</summary>
        World = 0,

        /// <summary>跟随附着源（每帧从 IVFXPositionResolver 获取位置）</summary>
        FollowTarget = 1,

        /// <summary>挂载到骨骼插槽（ADR-013 占位，Phase 4+ 实现）</summary>
        Socket = 2,
    }
}
