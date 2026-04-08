using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 圆形碰撞体。只读值类型，传参用 in 避免 12 bytes 拷贝。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct CircleHitbox
    {
        public readonly Vector2 Center;
        public readonly float Radius;

        public CircleHitbox(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>便捷构造——从 Vector3 取 XY 分量</summary>
        public CircleHitbox(Vector3 position, float radius)
        {
            Center = new Vector2(position.x, position.y);
            Radius = radius;
        }
    }
}
