namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 共享渲染排序常量（按 ADR-014）。
    /// 值越小越先渲染（更靠后方），值越大越后渲染（更靠前方）。
    /// ADR-029 v2：移除 Additive 层排序常量。
    /// 
    /// 层级间预留间距（10~50），方便未来插入新层级而不影响现有排序。
    /// 当前层级栈（从后到前）：Trail(90) → Bullet(100) → Laser(120) → Pickup(150) → VFX(200) → DmgNum(300)
    /// </summary>
    public static class RenderSortingOrder
    {
        /// <summary>拖尾层（在弹丸后方）</summary>
        public const int Trail = 90;

        /// <summary>弹丸层</summary>
        public const int Bullet = 100;

        /// <summary>激光默认层</summary>
        public const int LaserDefault = 120;

        /// <summary>道具层（弹丸之上、VFX 之下）</summary>
        public const int Pickup = 150;

        /// <summary>VFX 层</summary>
        public const int VFX = 200;

        /// <summary>伤害飘字（最上层）</summary>
        public const int DamageNumber = 300;
    }
}
