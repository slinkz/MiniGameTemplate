namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 瞄准策略枚举——决定技能释放时的方向来源。
    /// </summary>
    public enum AimMode : byte
    {
        /// <summary>永远沿 Entity 朝向射击（当前普攻行为）</summary>
        FixedForward = 0,

        /// <summary>有锁定目标→跟踪，无目标→Entity 朝向（当前技能行为）</summary>
        AutoAim = 1,

        /// <summary>完全由 DecisionCommand.AimDirection 决定（预留手动操控）</summary>
        CommandDir = 2,
    }
}
