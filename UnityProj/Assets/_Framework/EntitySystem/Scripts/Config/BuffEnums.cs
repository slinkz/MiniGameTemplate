namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Buff 标签分类（V2 Sprint 3）。
    /// 用于 RemoveByTag 批量清除（如"净化"清除所有 Negative）。
    /// </summary>
    public enum BuffTag : byte
    {
        /// <summary>增益（移速/攻速/护盾等）</summary>
        Positive = 0,
        /// <summary>减益（减速/脆弱/致盲等）</summary>
        Negative = 1,
        /// <summary>状态标记（不属于增益/减益的中性状态）</summary>
        Status = 2,
        /// <summary>光环（由场景/Boss 施加的范围效果）</summary>
        Aura = 3,
    }

    /// <summary>
    /// Buff 叠加模式（V2 Sprint 3）。
    /// 同 ID Buff 再次施加时的行为。
    /// </summary>
    public enum StackMode : byte
    {
        /// <summary>刷新持续时间（V1 默认行为）</summary>
        Refresh = 0,
        /// <summary>叠加层数（CurrentStacks++，属性按层数递增）</summary>
        Stack = 1,
    }
}
