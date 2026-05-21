using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Buff 伤害修正器——V2 Sprint 3 新增。
    /// 
    /// 读取目标 Entity 的 BuffComponent.DamageTakenModifier，
    /// 将当前 FinalDamage 乘以该倍率后继续传递。
    /// 
    /// Priority = 10（在 DamageRedirectModifier(0) 之后执行）。
    /// 
    /// 典型用途：
    /// - 护盾 Buff（DamageTaken×0）→ 免伤
    /// - 脆弱 Debuff（DamageTaken×2）→ 加伤
    /// 
    /// 注册位置：
    /// - 飞机：InvincibilityModifier(-1) → DamageRedirectModifier(0) → BuffDamageModifier(10)
    ///   注意：飞机的 DamageRedirectModifier return false 中断链，
    ///         所以飞机自身的 BuffDamageModifier 实际不生效——
    ///         但基地的 HealthComponent 有自己的 BuffDamageModifier。
    /// - 敌机：仅 BuffDamageModifier(10)
    /// - 基地：BuffDamageModifier(10)（用于转发伤害的 DamageTaken 修正）
    /// 
    /// 设计决策：
    /// - 独立 modifier 而非嵌入 HealthComponent，保持 SRP
    /// - 无状态（从 target 实时读取），避免同步问题
    /// </summary>
    public sealed class BuffDamageModifier : IDamageModifier
    {
        public int Priority => 10;

        public bool ProcessDamage(ref DamageContext context, Entity target)
        {
            // 获取目标的 BuffComponent
            var buff = target.GetComponent(ComponentType.Buff) as BuffComponent;
            if (buff == null) return true; // 无 Buff 组件 → 不修正

            float mod = buff.DamageTakenModifier;

            // 1.0 = 不修正，跳过浮点乘法
            if (Mathf.Approximately(mod, 1f)) return true;

            // 免伤（×0）时设 FinalDamage = 0，不中断链（让后续 modifier 有机会统计）
            context.FinalDamage = Mathf.RoundToInt(context.FinalDamage * mod);
            return true;
        }
    }
}
