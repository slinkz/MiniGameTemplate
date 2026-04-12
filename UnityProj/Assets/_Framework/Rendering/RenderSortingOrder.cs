namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 共享渲染排序常量（按 ADR-014）。
    /// 值越小越先渲染（更靠后方），值越大越后渲染（更靠前方）。
    /// </summary>
    public static class RenderSortingOrder
    {
        /// <summary>弹丸 Normal 层</summary>
        public const int BulletNormal = 100;

        /// <summary>弹丸 Additive 层</summary>
        public const int BulletAdditive = 110;

        /// <summary>激光默认层</summary>
        public const int LaserDefault = 120;

        /// <summary>VFX Normal 层</summary>
        public const int VFXNormal = 200;

        /// <summary>VFX Additive 层</summary>
        public const int VFXAdditive = 210;

        /// <summary>伤害飘字（最上层）</summary>
        public const int DamageNumber = 300;
    }
}
