namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 百分比减伤修正器（IDamageModifier 实现）。
    /// 注册到 HealthComponent 后，所有受到的伤害乘以 (1 - Reduction)。
    /// 例如 Reduction = 0.5 → 最终伤害 = FinalDamage × 0.5。
    /// 
    /// 用法：
    ///   var mod = new DamageReductionModifier(0.5f); // 50% 减伤
    ///   healthComponent.AddModifier(mod);
    ///   // 之后所有 TakeDamage 调用的 FinalDamage 都会减半
    ///   healthComponent.RemoveModifier(mod); // 移除减伤
    /// 
    /// Priority = 150（在护盾 0-100 之后，暴击修正 200-300 之前）。
    /// </summary>
    public class DamageReductionModifier : IDamageModifier
    {
        /// <summary>修正器优先级（150 = 减伤层）</summary>
        public int Priority => 150;

        /// <summary>减伤比例（0~1，0.5 = 减伤 50%）</summary>
        public float Reduction { get; set; }

        public DamageReductionModifier(float reduction)
        {
            Reduction = UnityEngine.Mathf.Clamp01(reduction);
        }

        public bool ProcessDamage(ref DamageContext context, Entity target)
        {
            // FinalDamage 在此之前已由 HealthComponent 计算（含暴击倍率）
            context.FinalDamage = (int)(context.FinalDamage * (1f - Reduction));
            return true; // 继续传递给下一个 modifier
        }
    }
}
