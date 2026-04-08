using MiniGameTemplate.Audio;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕发射模式——策划核心配置。定义一次弹幕发射的所有参数。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Bullet Pattern")]
    public class BulletPatternSO : ScriptableObject
    {
        [Header("弹幕类型")]
        public BulletTypeSO BulletType;

        [Header("发射参数")]
        [Tooltip("单次弹幕数量")]
        public int Count = 12;

        [Tooltip("散布角（360=全方位）")]
        public float SpreadAngle = 360f;

        [Tooltip("起始角偏移")]
        public float StartAngle = 0f;

        [Tooltip("每次发射的角度递增（旋转弹幕用）")]
        public float AnglePerShot = 0f;

        [Header("运动")]
        [Tooltip("弹丸速度（世界单位/秒）。安全上限约 12")]
        [Range(0.1f, 20f)]
        public float Speed = 5f;

        [Tooltip("速度随生命周期的曲线（横轴 0-1 = 生命百分比，纵轴 = 速度倍率）")]
        public AnimationCurve SpeedOverLifetime = AnimationCurve.Constant(0, 1, 1);

        [Tooltip("弹丸最大存活时间")]
        public float Lifetime = 5f;

        [Header("延迟变速")]
        [Tooltip("发射后静止/减速的等待时长（秒），0=无延迟")]
        public float DelayBeforeAccel = 0f;

        [Tooltip("等待期间的速度倍率（0=完全静止, 0.5=半速）")]
        [Range(0f, 1f)]
        public float DelaySpeedScale = 0f;

        [Tooltip("等待结束后的加速持续时间（秒），0=瞬间变速")]
        public float AccelDuration = 0.3f;

        [Header("追踪")]
        public bool IsHoming;

        [Tooltip("追踪转向速度（度/秒）")]
        public float HomingStrength = 2f;

        [Tooltip("追踪延迟：发射后多久才开始追踪（秒）")]
        public float HomingDelay = 0f;

        [Header("连射")]
        [Tooltip("连射次数")]
        public int BurstCount = 1;

        [Tooltip("连射间隔（秒）")]
        public float BurstInterval = 0.05f;

        [Header("音效")]
        public AudioClipSO FireSFX;
    }
}
