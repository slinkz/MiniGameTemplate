using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光线段（折射后的一段）。多段首尾相连组成完整激光路径。
    /// </summary>
    public struct LaserSegment
    {
        /// <summary>线段起点</summary>
        public Vector2 Start;

        /// <summary>线段终点</summary>
        public Vector2 End;

        /// <summary>线段方向（归一化，缓存避免重算）</summary>
        public Vector2 Direction;

        /// <summary>线段长度</summary>
        public float Length;
    }
}
