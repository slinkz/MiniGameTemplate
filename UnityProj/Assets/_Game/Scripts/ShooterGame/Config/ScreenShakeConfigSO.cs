using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// 屏幕震动配置 SO。
    /// TDD_02 §3.1：使用 Perlin Noise + AnimationCurve 衰减。
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/ScreenShakeConfig")]
    public class ScreenShakeConfigSO : ScriptableObject
    {
        [Header("飞机撞击敌机")]
        public float CollisionDuration = 0.15f;
        public float CollisionIntensity = 0.3f;
        public AnimationCurve CollisionDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("敌机突破底线")]
        public float BaseHitDuration = 0.3f;
        public float BaseHitIntensity = 0.6f;
        public AnimationCurve BaseHitDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }
}
