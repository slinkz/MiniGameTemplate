using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 共享渲染顶点格式——交错布局（Interleaved），单次 SetVertexBufferData 上传。
    /// sizeof = 24 bytes。
    /// Danmaku / VFX / DamageNumber 等系统共用。
    ///
    /// 注意：字段顺序必须与 RenderBatchManager.VertexLayout 声明顺序完全一致，
    /// 且二者都必须遵循 Unity 的标准顶点属性排序：Position → Color → TexCoord0。
    /// Unity 在 SetVertexBufferParams 时会将非标准顺序强制重排为标准顺序，
    /// 导致 CPU 侧结构体与 GPU 侧实际内存布局不一致，表现为采样错误或不可见。
    /// 参见：Unity 文档 VertexAttributeDescriptor — "标准"属性声明顺序。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderVertex
    {
        /// <summary>顶点位置（世界坐标）</summary>
        public Vector3 Position;   // 12 bytes

        /// <summary>顶点颜色（Tint + Alpha 淡出）</summary>
        public Color32 Color;      // 4 bytes

        /// <summary>纹理坐标（图集 UV）</summary>
        public Vector2 UV;         // 8 bytes
    }
}
