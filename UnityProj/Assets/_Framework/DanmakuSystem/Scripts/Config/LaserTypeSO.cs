using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光类型配置——视觉、宽度曲线、阶段时长、伤害。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Laser Type")]
    public class LaserTypeSO : ScriptableObject
    {
        [Header("视觉")]
        [Tooltip("激光纹理（UV 横向滚动）")]
        public Texture2D LaserTexture;

        [Tooltip("UV 滚动速度")]
        public float UVScrollSpeed = 2f;

        public Color CoreColor = Color.white;
        public Color EdgeColor = Color.cyan;

        [Tooltip("沿长度的宽度曲线（中间粗两头细）")]
        public AnimationCurve WidthProfile;

        [Header("宽度生命周期曲线")]
        [Tooltip("横轴=归一化时间(0-1), 纵轴=宽度比例(0-1, 1=MaxWidth)")]
        public AnimationCurve WidthOverLifetime = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("阶段时长")]
        [Tooltip("蓄力阶段（细线闪烁，不造成伤害）")]
        public float ChargeDuration = 0.5f;

        [Tooltip("发射阶段（全宽光柱，造成伤害）")]
        public float FiringDuration = 2f;

        [Tooltip("消散阶段")]
        public float FadeDuration = 0.3f;

        [Header("伤害")]
        public float DamagePerTick = 10f;

        [Tooltip("伤害间隔（秒），0.1s = 每秒 10 次")]
        public float TickInterval = 0.1f;

        [Header("碰撞")]
        public float MaxWidth = 0.8f;

        /// <summary>DanmakuTypeRegistry 分配的运行时索引</summary>
        [HideInInspector]
        public byte RuntimeIndex;

        /// <summary>总时长（charge + fire + fade）</summary>
        public float TotalDuration => ChargeDuration + FiringDuration + FadeDuration;
    }
}
