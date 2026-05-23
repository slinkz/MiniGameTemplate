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

        public bool Execute(SkillContext ctx)
        {
            if (Pattern == null) return false;
            var ds = DanmakuSystem.Instance;
            if (ds == null) return false;

            Vector2 pos = ctx.CastPosition + FireOffset;
            float angle = Mathf.Atan2(ctx.AimDirection.y, ctx.AimDirection.x) * Mathf.Rad2Deg;
            ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value, ctx.SourceTagId);
            return true;
        }
    }
}
