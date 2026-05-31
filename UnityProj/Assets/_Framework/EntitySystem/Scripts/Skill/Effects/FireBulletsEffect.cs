using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 内置技能效果：发射弹幕。
    /// 无状态（SA-002），所有字段为配置参数。
    /// </summary>
    [System.Serializable]
    public class FireBulletsEffect : ISkillEffect
    {
        [Tooltip("弹幕 Pattern")]
        public BulletPatternSO Pattern;

        [Tooltip("发射偏移")]
        public Vector2 FireOffset;

        [Tooltip("true = 永远朝实体正前方射击，忽略自瞄方向")]
        public bool UseForwardDirection;

        public bool Execute(SkillContext ctx)
        {
            if (Pattern == null) return false;
            var ds = DanmakuSystem.Instance;
            if (ds == null) return false;

            Vector2 pos = ctx.CastPosition + FireOffset;

            // UseForwardDirection: 用 CasterTransform.up（实体正前方），忽略 AutoAim
            Vector2 dir = UseForwardDirection && ctx.CasterTransform != null
                ? (Vector2)ctx.CasterTransform.up
                : ctx.AimDirection;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 【PK-UA-001 + CR-005】Buff 弹幕数修正
            int? countOverride = null;
            var buffComp = ctx.Caster.GetComponent(ComponentType.Buff) as BuffComponent;
            if (buffComp != null && buffComp.BulletCountModifier != 1f)
            {
                int baseCount = Pattern.Count;
                countOverride = Mathf.Max(1, Mathf.RoundToInt(baseCount * buffComp.BulletCountModifier));
            }

            // PA-01 被动穿透：由调用者预计算，避免 DanmakuSystem 反向依赖 EntitySystem
            bool pierceOverride = buffComp != null && buffComp.HasActivePierce;

            ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value, ctx.SourceTagId,
                countOverride, pierceOverride);
            return true;
        }
    }
}
