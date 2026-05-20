namespace Game.ShooterGame
{
    /// <summary>
    /// 道具类型枚举（TDD_02 S2.4）。
    /// </summary>
    public enum PickupType : byte
    {
        /// <summary>施加 Buff（移速/攻速等）</summary>
        Buff = 0,
        /// <summary>修复基地 HP</summary>
        Repair = 1,
        /// <summary>弹药强化（走 Buff 桥接）</summary>
        Ammo = 2,
        /// <summary>金币</summary>
        Coin = 3,
    }
}
