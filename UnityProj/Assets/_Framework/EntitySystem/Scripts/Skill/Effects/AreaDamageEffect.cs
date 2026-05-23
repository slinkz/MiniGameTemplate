using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 内置技能效果：AOE 直接伤害。
    /// 无状态（SA-002），所有字段为配置参数。
    /// </summary>
    [System.Serializable]
    public class AreaDamageEffect : ISkillEffect
    {
        [Tooltip("伤害半径")]
        public float Radius = 3f;

        [Tooltip("基础伤害")]
        public int BaseDamage = 50;

        [Tooltip("最大目标数")]
        public int MaxTargets = 16;

        public bool Execute(SkillContext ctx)
        {
            var hostileCamp = CampUtility.GetHostileCamp(ctx.Caster.Camp);
            var dmgCtx = new DamageContext
            {
                BaseDamage = BaseDamage,
                AttackerId = ctx.Caster.Id,
                SourcePosition = ctx.CastPosition,
                HasSourcePosition = true,
                SourceId = ctx.SourceTagId,
            };
            DamageDealer.DealAreaDamage(ctx.CastPosition, Radius, hostileCamp, dmgCtx, MaxTargets);
            return true; // v0.4 ATK-012：施放成功语义
        }
    }
}
