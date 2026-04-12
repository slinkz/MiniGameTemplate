using UnityEngine;
using UnityEngine.Rendering;
using MiniGameTemplate.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光 Mesh 渲染器——每帧将所有活跃激光重建为 Quad 条带并提交 DrawMesh。
    /// <para>
    /// 每条激光的每段 (LaserSegment) 生成一个 Quad（4 顶点 / 6 索引）：
    /// - 宽度由 LaserTypeSO.WidthProfile 沿线段长度驱动
    /// - UV.x 0→1 横跨宽度（中心=0.5，Shader 用于 Core/Glow 渐变）
    /// - UV.y 沿长度方向映射（Shader 用于纹理滚动）
    /// - 顶点 Color = 插值(CoreColor, EdgeColor) × Phase alpha
    /// </para>
    /// </summary>
    public class LaserRenderer
    {
        /// <summary>每条激光最多 9 段折射，每段 1 Quad = 4 顶点</summary>
        private const int MAX_QUADS = LaserPool.MAX_LASERS * LaserPool.MAX_SEGMENTS_PER_LASER;
        private const int MAX_VERTICES = MAX_QUADS * 4;
        private const int MAX_INDICES = MAX_QUADS * 6;

        private Mesh _mesh;
        private Material _material;
        private RenderVertex[] _vertices;
        private int[] _indices;
        private int _quadCount;

        /// <summary>上帧绘制的 Quad 数（调试用）</summary>
        public int DrawCount => _quadCount;

        // VertexAttributeDescriptor 缓存（与 BulletRenderer 一致）
        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        /// <summary>
        /// 初始化渲染器——创建材质实例、Mesh、预填充索引。
        /// </summary>
        public void Initialize(DanmakuRenderConfig renderConfig)
        {
            // 创建材质实例
            if (renderConfig.LaserMaterial != null)
            {
                _material = new Material(renderConfig.LaserMaterial)
                {
                    name = "DanmakuLaser (Instance)"
                };
            }
            else
            {
                Debug.LogWarning("[LaserRenderer] RenderConfig.LaserMaterial 为 null，激光将不可见。");
            }

            _vertices = new RenderVertex[MAX_VERTICES];
            _indices = new int[MAX_INDICES];

            // 预填充 Quad 索引（0,1,2 / 2,3,0）
            for (int q = 0; q < MAX_QUADS; q++)
            {
                int vi = q * 4;
                int ii = q * 6;
                _indices[ii + 0] = vi + 0;
                _indices[ii + 1] = vi + 1;
                _indices[ii + 2] = vi + 2;
                _indices[ii + 3] = vi + 2;
                _indices[ii + 4] = vi + 3;
                _indices[ii + 5] = vi + 0;
            }

            _mesh = new Mesh
            {
                name = "DanmakuMesh_Laser",
                indexFormat = MAX_VERTICES > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            _mesh.SetVertexBufferParams(MAX_VERTICES, VertexLayout);
            _mesh.SetIndexBufferParams(MAX_INDICES,
                MAX_VERTICES > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16);
            _mesh.SetIndices(_indices, MeshTopology.Triangles, 0, false);
        }

        /// <summary>
        /// 每帧由 DanmakuSystem.LateUpdate 调用——重建激光 Mesh 并 DrawMesh。
        /// </summary>
        public void Rebuild(LaserPool pool, DanmakuTypeRegistry registry)
        {
            _quadCount = 0;

            if (_material == null) return;
            if (pool.ActiveCount == 0) return;

            for (int i = 0; i < LaserPool.MAX_LASERS; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase == 0) continue;
                if (laser.SegmentCount == 0) continue;

                var type = registry.LaserTypes[laser.LaserTypeIndex];

                // Phase alpha：Charging 闪烁，Firing 全亮，Fading 渐隐
                float alpha = GetPhaseAlpha(ref laser, type);
                if (alpha <= 0f) continue;

                // 累计 UV.y 偏移（多段折射时 UV 连续）和长度比例（WidthProfile 驱动）
                float uvYAccum = 0f;
                float lengthAccum = 0f;
                float totalLength = laser.VisualLength > 0f ? laser.VisualLength : laser.Length;

                for (int s = 0; s < laser.SegmentCount; s++)
                {
                    ref var seg = ref laser.Segments[s];
                    if (seg.Length <= 0.0001f) continue;

                    WriteSegmentQuad(ref seg, type, laser.Width, alpha,
                        ref uvYAccum, ref lengthAccum, totalLength);
                }
            }

            // 上传并绘制
            if (_quadCount == 0) return;

            int vertexCount = _quadCount * 4;
            int indexCount = _quadCount * 6;

            _mesh.SetVertexBufferData(_vertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles),
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 1));

            Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);
        }

        /// <summary>释放 Mesh 和材质实例。</summary>
        public void Dispose()
        {
            if (_mesh != null) Object.Destroy(_mesh);
            if (_material != null) Object.Destroy(_material);
        }

        // ──── 内部方法 ────

        /// <summary>
        /// 根据激光 Phase 计算整体 alpha。
        /// Charging: 闪烁（0.3~0.8 正弦）
        /// Firing: 1.0
        /// Fading: 线性衰减到 0
        /// </summary>
        private static float GetPhaseAlpha(ref LaserData laser, LaserTypeSO type)
        {
            switch (laser.Phase)
            {
                case 1: // Charging — 闪烁
                    return 0.3f + 0.5f * (0.5f + 0.5f * Mathf.Sin(laser.Elapsed * 20f));

                case 2: // Firing — 全亮
                    return 1f;

                case 3: // Fading — 线性衰减
                    float fadeStart = type.ChargeDuration + type.FiringDuration;
                    float fadeProgress = (laser.Elapsed - fadeStart) / type.FadeDuration;
                    return Mathf.Clamp01(1f - fadeProgress);

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 将单个线段写为一个 Quad（4 顶点）。
        /// Quad 沿线段方向展开，宽度垂直于线段方向。
        /// </summary>
        private void WriteSegmentQuad(
            ref LaserSegment seg,
            LaserTypeSO type,
            float width,
            float alpha,
            ref float uvYAccum,
            ref float lengthAccum,
            float totalLength)
        {
            if (_quadCount >= MAX_QUADS) return;

            float halfW = width * 0.5f;

            // 垂直方向（线段方向逆时针旋转 90°）
            Vector2 perp = new Vector2(-seg.Direction.y, seg.Direction.x);

            // WidthProfile 沿长度驱动：用段端点在总长度中的归一化位置采样
            float startWidthScale = 1f;
            float endWidthScale = 1f;
            if (type.WidthProfile != null && type.WidthProfile.length > 0 && totalLength > 0f)
            {
                float startNorm = Mathf.Clamp01(lengthAccum / totalLength);
                float endNorm = Mathf.Clamp01((lengthAccum + seg.Length) / totalLength);
                startWidthScale = type.WidthProfile.Evaluate(startNorm);
                endWidthScale = type.WidthProfile.Evaluate(endNorm);
            }

            float startHalfW = halfW * startWidthScale;
            float endHalfW = halfW * endWidthScale;

            // 4 个顶点：起点左/右，终点左/右
            Vector2 startLeft = seg.Start + perp * startHalfW;
            Vector2 startRight = seg.Start - perp * startHalfW;
            Vector2 endLeft = seg.End + perp * endHalfW;
            Vector2 endRight = seg.End - perp * endHalfW;

            // UV 映射
            float uvYEnd = uvYAccum + seg.Length;

            // 顶点颜色：传入 CoreColor × alpha，Shader 内部做 Core/Glow 混合
            Color32 color = new Color32(
                (byte)(type.CoreColor.r * 255),
                (byte)(type.CoreColor.g * 255),
                (byte)(type.CoreColor.b * 255),
                (byte)(alpha * 255));

            int baseV = _quadCount * 4;

            // 左下（起点左，UV.x=0）
            _vertices[baseV + 0] = new RenderVertex
            {
                Position = new Vector3(startLeft.x, startLeft.y, 0f),
                Color = color,
                UV = new Vector2(0f, uvYAccum),
            };

            // 右下（起点右，UV.x=1）
            _vertices[baseV + 1] = new RenderVertex
            {
                Position = new Vector3(startRight.x, startRight.y, 0f),
                Color = color,
                UV = new Vector2(1f, uvYAccum),
            };

            // 右上（终点右，UV.x=1）
            _vertices[baseV + 2] = new RenderVertex
            {
                Position = new Vector3(endRight.x, endRight.y, 0f),
                Color = color,
                UV = new Vector2(1f, uvYEnd),
            };

            // 左上（终点左，UV.x=0）
            _vertices[baseV + 3] = new RenderVertex
            {
                Position = new Vector3(endLeft.x, endLeft.y, 0f),
                Color = color,
                UV = new Vector2(0f, uvYEnd),
            };

            _quadCount++;
            uvYAccum = uvYEnd;
            lengthAccum += seg.Length;
        }
    }
}
