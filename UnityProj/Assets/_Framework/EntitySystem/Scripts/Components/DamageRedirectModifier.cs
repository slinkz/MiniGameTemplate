namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 伤害转发修正器——V2 Sprint 1 新增。
    /// 
    /// 将飞机受到的伤害转发到基地 HealthComponent：
    /// - 敌弹命中飞机 → 基地扣固定伤害（由 context.FinalDamage 决定）
    /// - 飞机自身不扣血（返回 false 中断链）
    /// 
    /// Priority = 0（在 InvincibilityModifier(-1) 之后执行）。
    /// 
    /// 设计要点（TDD S1.3）：
    /// - 读取 context.FinalDamage 作为转发伤害值
    /// - 构造新 DamageContext 传给基地，保留 SourceId 用于追踪
    /// - 中断飞机的伤害链（return false），飞机自身零扣血
    /// </summary>
    public sealed class DamageRedirectModifier : IDamageModifier
    {
        public int Priority => 0;

        private Entity _baseEntity;
        private HealthComponent _baseHealth;

        /// <summary>
        /// 绑定基地 Entity。在 BattleController.InitBattle 中调用。
        /// </summary>
        public void SetBaseEntity(Entity baseEntity)
        {
            _baseEntity = baseEntity;
            _baseHealth = baseEntity?.GetComponent(ComponentType.Health) as HealthComponent;
        }

        public bool ProcessDamage(ref DamageContext context, Entity target)
        {
            // 防御性检查：基地不存在或已死亡时让伤害正常穿透
            if (_baseEntity == null || !_baseEntity.IsAlive || _baseHealth == null)
                return true;

            // 构造转发 DamageContext
            // 注意：FinalDamage 已含暴击修正，设为 BaseDamage 并清除暴击标记
            // 避免基地 HealthComponent 二次乘暴击倍率
            var redirectCtx = new DamageContext
            {
                BaseDamage = context.FinalDamage,
                FinalDamage = context.FinalDamage,
                AttackerId = context.AttackerId,
                HitType = context.HitType,
                IsCritical = false,
                CritMultiplier = 1f,
            };

            // 对基地造伤害
            _baseHealth.TakeDamage(ref redirectCtx);

            // 中断飞机的伤害链——飞机自身不扣血
            return false;
        }
    }
}
