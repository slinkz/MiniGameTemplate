using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 障碍物运行时数据。由 ObstaclePool 管理，AABB 碰撞检测。
    /// </summary>
    public struct ObstacleData
    {
        /// <summary>AABB 最小点</summary>
        public Vector2 Min;

        /// <summary>AABB 最大点</summary>
        public Vector2 Max;

        /// <summary>生命值（0=不可摧毁）</summary>
        public int HitPoints;

        /// <summary>阵营过滤（穿透自己阵营的弹丸）</summary>
        public byte Faction;

        /// <summary>当前阶段：0=未激活, 1=Active, 2=Destroyed</summary>
        public byte Phase;

        public byte _pad1;
        public byte _pad2;
    }

    /// <summary>障碍物生命阶段</summary>
    public enum ObstaclePhase : byte
    {
        /// <summary>未激活（空闲槽位）</summary>
        Inactive = 0,

        /// <summary>激活中</summary>
        Active = 1,

        /// <summary>已被摧毁</summary>
        Destroyed = 2,
    }
}
