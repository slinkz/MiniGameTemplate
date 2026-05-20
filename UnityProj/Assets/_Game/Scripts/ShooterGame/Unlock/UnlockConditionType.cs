namespace Game.ShooterGame
{
    /// <summary>
    /// 技能/被动解锁条件类型。
    /// TDD_02 S2.1
    /// </summary>
    public enum UnlockConditionType : byte
    {
        /// <summary>默认解锁（新存档即可用）</summary>
        Default = 0,
        /// <summary>通关指定关卡后解锁</summary>
        ClearLevel = 1,
        /// <summary>达成指定成就后解锁</summary>
        Achievement = 2,
    }
}
