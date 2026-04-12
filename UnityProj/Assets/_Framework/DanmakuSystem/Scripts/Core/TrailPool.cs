using UnityEngine;
using UnityEngine.Rendering;
using MiniGameTemplate.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 重量拖尾曲线池——管理独立 Trail 渲染（LineStrip 或 TriangleStrip）。
    /// 为设置 FLAG_HEAVY_TRAIL 的弹丸提供平滑曲线拖尾，与 Mesh 内 Ghost 残影互补。
    /// </summary>
    public class TrailPool
    {
        /// <summary>最大同时拖尾数（通常远少于弹丸数，16-64）</summary>
        public const int MAX_TRAILS = 64;

        /// <summary>每条拖尾最大点数</summary>
        public const int MAX_POINTS_PER_TRAIL = 20;

        /// <summary>当前实例的容量（由构造参数决定）</summary>
        public int Capacity { get; }

        private readonly TrailInstance[] _trails;
        private readonly int[] _freeSlots;
        private int _freeTop;

        public TrailPool(int maxTrails = MAX_TRAILS)
        {
            Capacity = maxTrails;
            _trails = new TrailInstance[maxTrails];
            _freeSlots = new int[maxTrails];
            for (int i = 0; i < maxTrails; i++)
                _trails[i] = new TrailInstance();
        }

        // 渲染
        private Mesh _mesh;
        private Material _material;
        private RenderVertex[] _vertices;
        private int[] _indices;
        private int _vertexCount;
        private int _indexCount;

        public int ActiveCount { get; private set; }

        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        public void Initialize(Material material)
        {
            // 创建独立材质实例——拖尾使用纯白纹理，仅靠顶点颜色着色
            if (material != null)
            {
                _material = new Material(material) { name = "DanmakuTrail (Instance)" };
                _material.mainTexture = Texture2D.whiteTexture;
            }

            // 每条拖尾 = (MAX_POINTS_PER_TRAIL-1) 段 × 2 三角形 × 3 顶点
            // 简化：每条拖尾 MAX_POINTS_PER_TRAIL × 2 顶点（TriangleStrip 展开为 TriangleList）
            int maxVertices = Capacity * MAX_POINTS_PER_TRAIL * 2;
            int maxIndices = Capacity * (MAX_POINTS_PER_TRAIL - 1) * 6;

            _vertices = new RenderVertex[maxVertices];
            _indices = new int[maxIndices];

            _mesh = new Mesh
            {
                name = "TrailPool",
                indexFormat = maxVertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };
            _mesh.SetVertexBufferParams(maxVertices, VertexLayout);
            _mesh.SetIndexBufferParams(maxIndices,
                maxVertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16);

            // 初始化空闲栈
            for (int i = Capacity - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
        }

        /// <summary>
        /// 为弹丸分配一条拖尾。返回 handle（索引），-1 = 池满。
        /// </summary>
        public int Allocate(BulletTypeSO type)
        {
            if (_freeTop == 0) return -1;
            int slot = _freeSlots[--_freeTop];
            ActiveCount++;

            ref var trail = ref _trails[slot];
            trail.Active = true;
            trail.PointCount = 0;
            trail.MaxPoints = Mathf.Min(type.TrailPointCount, MAX_POINTS_PER_TRAIL);
            trail.Width = type.TrailWidth;
            trail.WidthCurve = type.TrailWidthCurve;
            trail.ColorGradient = type.TrailColor;
            return slot;
        }

        /// <summary>归还拖尾。</summary>
        public void Free(int handle)
        {
            if (handle < 0 || handle >= Capacity) return;
            _trails[handle].Active = false;
            _trails[handle].PointCount = 0;
            _freeSlots[_freeTop++] = handle;
            ActiveCount--;
        }

        /// <summary>清场。</summary>
        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = Capacity - 1; i >= 0; i--)
            {
                _trails[i].Active = false;
                _trails[i].PointCount = 0;
                _freeSlots[_freeTop++] = i;
            }
            ActiveCount = 0;
        }

        /// <summary>
        /// 追加一个点到拖尾。
        /// </summary>
        public void AddPoint(int handle, Vector2 position)
        {
            if (handle < 0 || handle >= Capacity) return;
            ref var trail = ref _trails[handle];
            if (!trail.Active) return;

            // 环形写入
            if (trail.PointCount < trail.MaxPoints)
            {
                trail.Points[trail.PointCount] = position;
                trail.PointCount++;
            }
            else
            {
                // 前移一位
                System.Array.Copy(trail.Points, 1, trail.Points, 0, trail.MaxPoints - 1);
                trail.Points[trail.MaxPoints - 1] = position;
            }
        }

        /// <summary>
        /// 每帧渲染所有活跃拖尾。由 DanmakuSystem.LateUpdate 调用。
        /// </summary>
        public void Render()
        {
            _vertexCount = 0;
            _indexCount = 0;

            for (int i = 0; i < Capacity; i++)
            {
                ref var trail = ref _trails[i];
                if (!trail.Active || trail.PointCount < 2) continue;

                BuildTrailMesh(trail);
            }

            if (_vertexCount == 0 || _material == null) return;

            _mesh.SetVertexBufferData(_vertices, 0, 0, _vertexCount, 0,
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);

            _mesh.SetIndexBufferData(_indices, 0, 0, _indexCount,
                MeshUpdateFlags.DontValidateIndices);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, _indexCount, MeshTopology.Triangles),
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 1));
            Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);
        }

        public void Dispose()
        {
            if (_mesh != null) Object.Destroy(_mesh);
            if (_material != null) Object.Destroy(_material);
        }

        // ──── 内部 ────

        private void BuildTrailMesh(TrailInstance trail)
        {
            int pointCount = trail.PointCount;

            for (int p = 0; p < pointCount; p++)
            {
                float t = (float)p / (pointCount - 1);  // 0..1 沿拖尾

                // 宽度
                float width = trail.Width;
                if (trail.WidthCurve != null && trail.WidthCurve.keys.Length > 0)
                    width *= trail.WidthCurve.Evaluate(t);
                float halfWidth = width * 0.5f;

                // 法线方向（垂直于行进方向）
                Vector2 dir;
                if (p < pointCount - 1)
                    dir = (trail.Points[p + 1] - trail.Points[p]).normalized;
                else
                    dir = (trail.Points[p] - trail.Points[p - 1]).normalized;

                Vector2 normal = new Vector2(-dir.y, dir.x);
                Vector2 pos = trail.Points[p];

                // 颜色
                Color32 color = new Color32(255, 255, 255, 255);
                if (trail.ColorGradient != null)
                {
                    Color c = trail.ColorGradient.Evaluate(t);
                    color = c;
                }

                // 两个顶点（左/右）
                if (_vertexCount + 2 > _vertices.Length) return;

                _vertices[_vertexCount] = new RenderVertex
                {
                    Position = new Vector3(pos.x + normal.x * halfWidth, pos.y + normal.y * halfWidth, 0),
                    UV = new Vector2(0, t),
                    Color = color,
                };
                _vertices[_vertexCount + 1] = new RenderVertex
                {
                    Position = new Vector3(pos.x - normal.x * halfWidth, pos.y - normal.y * halfWidth, 0),
                    UV = new Vector2(1, t),
                    Color = color,
                };

                // 索引（从第二个点开始构成 Quad）
                if (p > 0 && _indexCount + 6 <= _indices.Length)
                {
                    int prev = _vertexCount - 2;
                    int curr = _vertexCount;

                    _indices[_indexCount + 0] = prev;
                    _indices[_indexCount + 1] = prev + 1;
                    _indices[_indexCount + 2] = curr;

                    _indices[_indexCount + 3] = curr;
                    _indices[_indexCount + 4] = prev + 1;
                    _indices[_indexCount + 5] = curr + 1;

                    _indexCount += 6;
                }

                _vertexCount += 2;
            }
        }

        /// <summary>单条拖尾实例数据。使用 class 以便持有引用类型字段。</summary>
        private class TrailInstance
        {
            public bool Active;
            public int PointCount;
            public int MaxPoints;
            public float Width;
            public AnimationCurve WidthCurve;
            public Gradient ColorGradient;
            public readonly Vector2[] Points = new Vector2[MAX_POINTS_PER_TRAIL];
        }
    }
}
