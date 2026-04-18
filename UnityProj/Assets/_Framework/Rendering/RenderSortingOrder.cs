namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 共享渲染排序常量（按 ADR-014）。
    /// 值越小越先渲染（更靠后方），值越大越后渲染（更靠前方）。
    /// ADR-029 v2：移除 Additive 层排序常量。
    /// </summary>
    public static class RenderSortingOrder
    {
        /// <summary>弹丸层</summary>
        public const int Bullet = 100;

        /// <summary>激光默认层</summary>
        public const int LaserDefault = 120;

        /// <summary>VFX 层</summary>
        public const int VFX = 200;

        /// <summary>伤害飘字（最上层）</summary>
        public const int DamageNumber = 300;
    }
}
