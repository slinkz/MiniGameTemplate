using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 碰撞体形状类型。
    /// </summary>
    public enum HitboxShape : byte
    {
        Circle = 0,
        Rect = 1,
    }

    /// <summary>
    /// 统一碰撞体值类型（联合体设计）。
    /// 支持圆形和轴对齐矩形（AABB）两种形状。
    /// 
    /// 内存布局：20 bytes（Center 8 + Size 8 + Shape 1 + padding 3）
    /// CollisionSolver 每帧读取，保持小尺寸传参高效。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Hitbox
    {
        /// <summary>碰撞体中心点</summary>
        public readonly Vector2 Center;

        /// <summary>
        /// 形状参数：
        /// - Circle: X = Radius（Y 未用）
        /// - Rect: X = HalfWidth, Y = HalfHeight
        /// </summary>
        public readonly Vector2 HalfExtents;

        /// <summary>碰撞体形状</summary>
        public readonly HitboxShape Shape;

        // ──── 构造函数 ────

        /// <summary>圆形碰撞体</summary>
        public Hitbox(Vector2 center, float radius)
        {
            Center = center;
            HalfExtents = new Vector2(radius, 0f);
            Shape = HitboxShape.Circle;
        }

        /// <summary>矩形碰撞体（AABB，半宽 + 半高）</summary>
        public Hitbox(Vector2 center, float halfWidth, float halfHeight)
        {
            Center = center;
            HalfExtents = new Vector2(halfWidth, halfHeight);
            Shape = HitboxShape.Rect;
        }

        // ──── 便捷属性 ────

        /// <summary>圆形半径（仅 Circle 有效）</summary>
        public float Radius => HalfExtents.x;

        /// <summary>矩形半宽（仅 Rect 有效）</summary>
        public float HalfWidth => HalfExtents.x;

        /// <summary>矩形半高（仅 Rect 有效）</summary>
        public float HalfHeight => HalfExtents.y;

        // ──── 转换 ────

        /// <summary>从旧 CircleHitbox 隐式转换（向后兼容）</summary>
        public static implicit operator Hitbox(CircleHitbox circle)
        {
            return new Hitbox(circle.Center, circle.Radius);
        }

        /// <summary>转为旧 CircleHitbox（向后兼容，Rect 时返回外接圆）</summary>
        public CircleHitbox ToCircleHitbox()
        {
            if (Shape == HitboxShape.Circle)
                return new CircleHitbox(Center, Radius);
            // Rect → 外接圆（对角线半长）
            float r = Mathf.Sqrt(HalfExtents.x * HalfExtents.x + HalfExtents.y * HalfExtents.y);
            return new CircleHitbox(Center, r);
        }
    }
}
