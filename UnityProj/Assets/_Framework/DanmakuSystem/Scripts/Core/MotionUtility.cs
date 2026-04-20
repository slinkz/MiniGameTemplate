using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 运动策略共享工具。
    /// </summary>
    public static class MotionUtility
    {
        public static float CalculateModifierSpeed(float elapsed, ref BulletModifier modifier)
        {
            if (elapsed < modifier.DelayEndTime)
            {
                return modifier.DelaySpeedScale;
            }

            if (elapsed < modifier.AccelEndTime)
            {
                float duration = modifier.AccelEndTime - modifier.DelayEndTime;
                if (duration <= Mathf.Epsilon)
                {
                    return 1f;
                }

                float t = (elapsed - modifier.DelayEndTime) / duration;
                return Mathf.Lerp(modifier.DelaySpeedScale, 1f, t);
            }

            return 1f;
        }
    }
}
