namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 渲染层枚举（ADR-029 v2：彻底移除 Additive，统一 Normal）。
    /// 保留枚举类型而非直接删除文件，确保 BucketKey 等引用编译通过，
    /// 且已序列化的 .asset 文件（byte 0 = Normal）天然兼容。
    /// </summary>
    public enum RenderLayer : byte
    {
        /// <summary>普通混合（Alpha Blend）——唯一可用的混合模式</summary>
        Normal = 0,
    }
}
