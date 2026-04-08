using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕世界配置——容量、世界边界、碰撞网格、无敌帧。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/World")]
    public class DanmakuWorldConfig : ScriptableObject
    {
        [Header("容量")]
        public int MaxBullets = 2048;
        public int MaxLasers = 16;
        public int MaxSprays = 8;

        [Header("世界边界")]
        [Tooltip("弹幕活动区域（屏幕边缘判定用）")]
        public Rect WorldBounds = new(-6, -10, 12, 20);

        [Header("碰撞网格")]
        [Tooltip("均匀网格分区列数")]
        public int GridCellsX = 12;

        [Tooltip("均匀网格分区行数")]
        public int GridCellsY = 20;

        [Header("无敌帧")]
        [Tooltip("受击后无敌时长（秒）。0=关闭。使用真实时间，不受弹幕 TimeScale 影响")]
        public float InvincibleDuration = 0f;
    }
}
