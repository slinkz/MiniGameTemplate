using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕渲染顶点格式——交错布局（Interleaved），单次 SetVertexBufferData 上传。
    /// sizeof = 24 bytes。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DanmakuVertex
    {
        /// <summary>顶点位置（世界坐标）</summary>
        public Vector3 Position;   // 12 bytes

        /// <summary>顶点颜色（Tint + Alpha 淡出）</summary>
        public Color32 Color;      // 4 bytes

        /// <summary>纹理坐标（图集 UV）</summary>
        public Vector2 UV;         // 8 bytes
    }
}
