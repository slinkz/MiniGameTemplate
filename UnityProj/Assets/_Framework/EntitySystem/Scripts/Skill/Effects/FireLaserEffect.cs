using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 内置技能效果：发射激光。
    /// 桥接 SkillComponent → DanmakuSystem.FireLaser()。
    /// 
    /// 模式选择逻辑（自动降级）：
    /// - 有 CasterTransform → Attached 模式（激光跟随施法者移动）
    /// - 无 CasterTransform → Detached 模式（激光固定不动，兼容测试场景）
    /// 
    /// 目标需求：
    /// - RequiresTarget = true（默认）：无目标时不发射（return false，技能系统会重试下一帧）
    /// - RequiresTarget = false：无目标也发射（朝默认方向）
    /// 
    /// 无状态（SA-002），所有字段为配置参数。
    /// 
    /// 激光命中时的 DOT 施加由碰撞系统 + SkillConfigSO.AttachedDotConfig 驱动，
    /// 不在此处处理。
    /// </summary>
    [System.Serializable]
    public class FireLaserEffect : ISkillEffect
    {
        [Tooltip("激光类型配置")]
        public LaserTypeSO LaserType;

        [Tooltip("激光长度（世界单位）")]
        [Min(0.1f)]
        public float Length = 12f;

        [Tooltip("发射偏移（世界空间语义：(0,0.5)=正上方机头位置，Attached 模式自动转局部坐标）")]
        public Vector2 FireOffset;

        [Tooltip("激光生命周期覆盖（0 = 使用 LaserType.TotalDuration）")]
        [Min(0f)]
        public float LifetimeOverride = 0f;

        // ⚠ 序列化注意：Unity YAML 在值等于代码默认值时可能省略该字段。
        // 如果将来修改此默认值，所有未显式序列化的已有 asset 行为会静默改变。
        // 请保持默认值 = true 不变，若需 false 行为请在 Inspector 中逐个 asset 设置。
        [Tooltip("是否需要有效目标才能发射（默认 true：无目标不发射）")]
        public bool RequiresTarget = true;

        public bool Execute(SkillContext ctx)
        {
            if (LaserType == null)
            {
                Debug.LogWarning("[FireLaserEffect] LaserType 未设置，跳过发射。请在 SkillConfigSO 的 Effects 中为 FireLaserEffect 指定 LaserType。");
                return false;
            }

            // 目标检查：RequiresTarget=true 时，无目标不发射
            if (RequiresTarget && !ctx.HasTarget)
            {
                return false;
            }

            var ds = DanmakuSystem.Instance;
            if (ds == null)
            {
                Debug.LogWarning("[FireLaserEffect] DanmakuSystem.Instance 为 null，激光系统未初始化。请确保场景中存在 DanmakuSystem。");
                return false;
            }

            // 有 CasterTransform → Attached 模式（激光跟随飞机）
            if (ctx.CasterTransform != null)
            {
                // angleOffset：瞄准方向相对于 Transform Z rotation 的偏移
                float aimAngle = Mathf.Atan2(ctx.AimDirection.y, ctx.AimDirection.x);
                float transformAngle = ctx.CasterTransform.eulerAngles.z * Mathf.Deg2Rad;
                float angleOffset = aimAngle - transformAngle;

                // FireOffset 是世界空间语义（与普攻 AttackFireOffset 一致：(0,0.5)=机头上方）
                // AttachSourceRegistry 需要 Transform 局部空间偏移（TransformPoint 还原）
                // 转换：世界偏移 → 局部偏移
                Vector2 localOffset = (Vector2)ctx.CasterTransform.InverseTransformVector(
                    new Vector3(FireOffset.x, FireOffset.y, 0f));

                int index = ds.FireLaser(LaserType, ctx.CasterTransform, Length,
                    LifetimeOverride, localOffset, angleOffset, ctx.SourceTagId);
                return index >= 0;
            }

            // 无 CasterTransform → Detached 降级（测试模式/无视图场景）
            Vector2 pos = ctx.CastPosition + FireOffset;
            float angle = Mathf.Atan2(ctx.AimDirection.y, ctx.AimDirection.x);

            int detachedIndex = ds.FireLaser(LaserType, pos, angle, Length, LifetimeOverride,
                ctx.SourceTagId);
            return detachedIndex >= 0;
        }
    }
}
