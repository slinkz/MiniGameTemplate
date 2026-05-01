namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 伤害修正器接口（P2.4 新增，TDD §4.2 / 未决项 #10）。
    /// 
    /// 任何需要拦截/修改伤害的系统（护甲、护盾、减伤 Buff、免伤、反弹等）
    /// 实现此接口并注册到 HealthComponent。
    /// 
    /// HealthComponent.TakeDamage 流程：
    ///   1. 收到 DamageContext（BaseDamage）
    ///   2. 按优先级遍历 IDamageModifier 链，每个 modifier 可修改 context
    ///   3. 读取 context.FinalDamage 进行扣血
    /// 
    /// 设计原则：
    /// - 修正器是有状态的（如护盾有剩余值），但 struct DamageContext 通过 ref 传递——零 GC
    /// - 固定数组（最多 4 个 modifier），避免 List GC
    /// - 修正器优先级由注册顺序决定（先注册先执行）
    /// </summary>
    public interface IDamageModifier
    {
        /// <summary>
        /// 修正器优先级（升序执行，数字越小越先）。
        /// 推荐范围：0-100=护盾/免伤，100-200=护甲/减伤，200-300=暴击修正，300+=反弹/吸血
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 处理伤害上下文。可修改 context 中的任何字段。
        /// 返回 true = 继续传递给下一个 modifier；返回 false = 中断链（伤害被完全吸收）。
        /// </summary>
        bool ProcessDamage(ref DamageContext context, Entity target);
    }
}
