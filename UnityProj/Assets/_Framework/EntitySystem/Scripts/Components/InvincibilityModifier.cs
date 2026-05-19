namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 无敌帧伤害修正器——V2 Sprint 1 新增。
    /// 
    /// 当飞机处于无敌帧状态时（碰撞后 0.5s），中断伤害链，伤害归零。
    /// Priority = -1（最高优先级，在所有其他 Modifier 之前执行）。
    /// 
    /// 无敌帧状态由 HealthComponent 内置的 IFrame 机制管理
    /// （通过 EntityConfigSO.IFrameCount 配置）。
    /// 本 Modifier 读取 HealthComponent.IsInvincible 判定。
    /// 
    /// 设计要点（TDD S1.3）：
    /// - 不持有自己的计时器——复用 HealthComponent 已有的无敌帧机制
    /// - Priority = -1 确保最先执行，避免后续 Modifier 做无用计算
    /// 
    /// 注意：当前 HealthComponent.TakeDamage 内部在 modifier 链之前已有硬编码
    /// _iFrameRemaining 检查。本 Modifier 作为防御性冗余层 + 未来扩展点保留：
    /// - 当通过 Buff 或外部系统设置无敌（不依赖 IFrame 机制）时，本 Modifier 生效
    /// - 若未来移除 HealthComponent 内置检查，本 Modifier 作为唯一无敌网关
    /// </summary>
    public sealed class InvincibilityModifier : IDamageModifier
    {
        public int Priority => -1;

        private HealthComponent _healthComp;

        /// <summary>
        /// 绑定目标 HealthComponent（飞机的）。
        /// 在 BattleController.InitBattle 中调用。
        /// </summary>
        public void SetHealthComponent(HealthComponent comp)
        {
            _healthComp = comp;
        }

        public bool ProcessDamage(ref DamageContext context, Entity target)
        {
            // 无敌帧期间：中断链，伤害归零
            if (_healthComp != null && _healthComp.IsInvincible)
                return false;

            // 继续传递
            return true;
        }
    }
}
