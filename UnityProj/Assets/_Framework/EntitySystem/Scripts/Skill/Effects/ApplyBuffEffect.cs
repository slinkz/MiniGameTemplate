using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 内置技能效果：施加 Buff（Skill→Buff 桥接）。
    /// 无状态（SA-002），所有字段为配置参数。
    /// </summary>
    [System.Serializable]
    public class ApplyBuffEffect : ISkillEffect
    {
        [Tooltip("要施加的 Buff 配置")]
        public BuffConfigSO BuffConfig;

        [Tooltip("施加给自己还是目标")]
        public bool ApplyToSelf = true;

        [Tooltip("搜索半径（仅 ApplyToSelf=false 时生效）")]
        [Min(0.1f)]
        public float SearchRadius = 5f; // v0.4 GD-013

        public bool Execute(SkillContext ctx)
        {
            Entity target;
            if (ApplyToSelf)
            {
                target = ctx.Caster;
            }
            else
            {
                var mgr = EntityManagerAccessor.Instance;
                Debug.Assert(mgr != null, "[ApplyBuffEffect] EntityManager not initialized!");
                target = mgr?.FindNearestEntity(
                    ctx.CastPosition, SearchRadius, CampUtility.GetHostileCamp(ctx.Caster.Camp));
            }

            if (target == null) return false;
            var buffComp = target.GetComponent(ComponentType.Buff) as BuffComponent;
            if (buffComp == null) return false;
            return buffComp.ApplyBuff(BuffConfig);
        }
    }
}
