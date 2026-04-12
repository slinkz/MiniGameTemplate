namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 共享渲染层枚举——Danmaku / VFX 等系统共用。
    /// 值与旧 MiniGameTemplate.Danmaku.RenderLayer 和 MiniGameTemplate.VFX.VFXRenderLayer 完全一致，
    /// 保证 ScriptableObject 序列化兼容。
    /// </summary>
    public enum RenderLayer : byte
    {
        /// <summary>普通混合（Alpha Blend）</summary>
        Normal = 0,

        /// <summary>叠加发光（Additive Blend）</summary>
        Additive = 1,
    }
}
