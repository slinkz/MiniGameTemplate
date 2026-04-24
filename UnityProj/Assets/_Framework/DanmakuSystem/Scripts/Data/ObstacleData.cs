using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 障碍物运行时数据。由 ObstaclePool 管理，OBB 碰撞检测。
    /// 结构体布局：36 bytes（最大字段对齐 4 bytes，无额外 padding）。
    /// </summary>
    public struct ObstacleData
    {
        /// <summary>OBB 中心（世界坐标）</summary>
        public Vector2 Center;

        /// <summary>半尺寸（局部空间宽高的一半）</summary>
        public Vector2 HalfExtents;

        /// <summary>旋转角度（弧度，逆时针为正）</summary>
        public float RotationRad;

        /// <summary>预计算 sin(RotationRad)</summary>
        public float Sin;

        /// <summary>预计算 cos(RotationRad)</summary>
        public float Cos;

        /// <summary>生命值（0=不可摧毁）</summary>
        public int HitPoints;

        /// <summary>阵营过滤（穿透自己阵营的弹丸）</summary>
        public byte Faction;

        /// <summary>当前阶段：0=未激活, 1=Active, 2=Destroyed</summary>
        public byte Phase;

        /// <summary>1=圆形障碍物，0=矩形/OBB 障碍物</summary>
        public byte Shape;
        public byte _pad1;

    }

    public enum ObstacleShape : byte
    {
        Box = 0,
        Circle = 1,
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
