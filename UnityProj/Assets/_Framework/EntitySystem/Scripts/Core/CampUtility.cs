namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 阵营工具类——提供阵营相关的通用判断方法。
    /// 
    /// 当前版本仅支持二元阵营（Player ↔ Enemy）。(v0.4 SA-008)
    /// 框架品类定位：弹幕射击 + 塔防核心，均为严格二元对立。
    /// 多阵营支持（PvP/三方/中立可攻击）属于 Phase 5 品类扩展范畴。
    /// 扩展方向：关系矩阵（bool[,]）或 [Flags] bitmask。
    /// </summary>
    public static class CampUtility
    {
        /// <summary>
        /// 获取指定阵营的敌对阵营。
        /// Player → Enemy，Enemy → Player，其余 → Neutral。
        /// </summary>
        public static Danmaku.EnumCamp GetHostileCamp(Danmaku.EnumCamp self)
        {
            return self switch
            {
                Danmaku.EnumCamp.Player => Danmaku.EnumCamp.Enemy,
                Danmaku.EnumCamp.Enemy  => Danmaku.EnumCamp.Player,
                _ => Danmaku.EnumCamp.Neutral
            };
        }
    }
}
