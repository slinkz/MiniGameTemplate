using MiniGameTemplate.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 重量拖尾曲线池——保持独立 Mesh，但接入统一渲染统计（R3.2 / 方案 A）。
    /// 为设置 FLAG_HEAVY_TRAIL 的弹丸提供平滑曲线拖尾，与 Mesh 内 Ghost 残影互补。
    /// 渲染方式：Graphics.DrawMesh（与弹丸 RBM 走同一渲染路径，层次由调用顺序决定）。
    /// </summary>
    public class TrailPool
    {
        public const int MAX_TRAILS = 64;
        public const int MAX_POINTS_PER_TRAIL = 20;

        /// <summary>
        /// 最小采样距离——两个相邻点距离小于此值时跳过采样。
        /// 防止低速弹丸产生过密的点，导致 trail 总长度太短。
        /// </summary>
        public const float MIN_SAMPLE_DISTANCE = 0.15f;

        public int Capacity { get; }
        public int ActiveCount { get; private set; }

        private readonly TrailInstance[] _trails;
        private readonly int[] _freeSlots;
        private int _freeTop;

        private Mesh _mesh;
        private Material _material;
        private RenderVertex[] _vertices;
        private int[] _indices;
        private int _vertexCount;
        private int _indexCount;

        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        public TrailPool(int maxTrails = MAX_TRAILS)
        {
            Capacity = maxTrails;
            _trails = new TrailInstance[maxTrails];
            _freeSlots = new int[maxTrails];
            for (int i = 0; i < maxTrails; i++)
                _trails[i] = new TrailInstance();
        }

        public void Initialize(Material material)
        {
            if (material != null)
            {
                _material = new Material(material) { name = "Danmaku Trail (Instance)" };
                _material.mainTexture = Texture2D.whiteTexture;
                // renderQueue 与弹丸相同（3000 Transparent），
                // 层次由 Graphics.DrawMesh 的调用顺序控制：先调用 = 先画 = 在后面。
            }

            int maxVertices = Capacity * MAX_POINTS_PER_TRAIL * 2;
            int maxIndices = Capacity * (MAX_POINTS_PER_TRAIL - 1) * 6;

            _vertices = new RenderVertex[maxVertices];
            _indices = new int[maxIndices];

            _mesh = new Mesh
            {
                name = "TrailPool",
                indexFormat = IndexFormat.UInt32,
            };
            _mesh.SetVertexBufferParams(maxVertices, VertexLayout);
            _mesh.SetIndexBufferParams(maxIndices, IndexFormat.UInt32);

            for (int i = Capacity - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
        }

        public int Allocate(BulletTypeSO type)
        {
            if (_freeTop == 0)
                return -1;

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

        public void Free(int handle)
        {
            if (handle < 0 || handle >= Capacity)
                return;

            if (!_trails[handle].Active)
                return;

            _trails[handle].Active = false;
            _trails[handle].PointCount = 0;
            _freeSlots[_freeTop++] = handle;
            ActiveCount--;
        }

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

        public void AddPoint(int handle, Vector2 position)
        {
            if (handle < 0 || handle >= Capacity)
                return;

            ref var trail = ref _trails[handle];
            if (!trail.Active)
                return;

            // 第一个点直接加入
            if (trail.PointCount == 0)
            {
                trail.Points[0] = position;
                trail.PointCount = 1;
                return;
            }

            // 始终更新最后一个点为弹丸当前位置（trail 头部紧贴弹丸）
            trail.Points[trail.PointCount - 1] = position;

            // 检查和倒数第二个点的距离——距离不够就不新增采样点
            int prevIndex = trail.PointCount >= 2 ? trail.PointCount - 2 : 0;
            float dist = Vector2.Distance(position, trail.Points[prevIndex]);
            if (trail.PointCount >= 2 && dist < MIN_SAMPLE_DISTANCE)
                return;

            // 距离够了，新增一个采样点
            if (trail.PointCount < trail.MaxPoints)
            {
                trail.Points[trail.PointCount] = position;
                trail.PointCount++;
            }
            else
            {
                // 环形缓冲满了——移除最老的点
                System.Array.Copy(trail.Points, 1, trail.Points, 0, trail.MaxPoints - 1);
                trail.Points[trail.MaxPoints - 1] = position;
            }
        }

        public void Render()
        {
            _vertexCount = 0;
            _indexCount = 0;

            for (int i = 0; i < Capacity; i++)
            {
                ref var trail = ref _trails[i];
                if (!trail.Active || trail.PointCount < 2)
                    continue;

                BuildTrailMesh(trail);
            }

            if (_vertexCount == 0 || _material == null)
                return;

            _mesh.SetVertexBufferParams(_vertices.Length, VertexLayout);
            _mesh.SetIndexBufferParams(_indices.Length, IndexFormat.UInt32);

            _mesh.SetVertexBufferData(_vertices, 0, 0, _vertexCount, 0,
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);

            _mesh.SetIndexBufferData(_indices, 0, 0, _indexCount, MeshUpdateFlags.DontValidateIndices);
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, _indexCount, MeshTopology.Triangles),
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(100f, 100f, 1f));

            // 使用 Graphics.DrawMesh 与弹丸走同一渲染路径。
            // 层次由 UpdatePipeline 中的调用顺序决定：
            // 先调用 TrailPool.Render() → 先提交 → 先渲染 → 在弹丸后方。
            Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);
            RenderBatchManagerRuntimeStats.AccumulateBatch(1, 1, 0);
        }

        public void Dispose()
        {
            if (_mesh != null)
                Object.Destroy(_mesh);
            if (_material != null)
                Object.Destroy(_material);
        }

        private void BuildTrailMesh(TrailInstance trail)
        {
            int pointCount = trail.PointCount;

            for (int p = 0; p < pointCount; p++)
            {
                float t = (float)p / (pointCount - 1);
                float width = trail.Width;
                if (trail.WidthCurve != null && trail.WidthCurve.keys.Length > 0)
                    width *= trail.WidthCurve.Evaluate(t);
                else
                    width *= t;  // 默认宽度曲线：尾部(t=0)→0，头部(t=1)→全宽
                float halfWidth = width * 0.5f;

                Vector2 dir = p < pointCount - 1
                    ? (trail.Points[p + 1] - trail.Points[p]).normalized
                    : (trail.Points[p] - trail.Points[p - 1]).normalized;

                Vector2 normal = new Vector2(-dir.y, dir.x);
                Vector2 pos = trail.Points[p];

                // Gradient 评估：如果 Gradient 未配置或只有默认 key，
                // 使用默认的白色→透明淡出效果
                Color32 color;
                if (trail.ColorGradient != null && IsGradientCustomized(trail.ColorGradient))
                    color = trail.ColorGradient.Evaluate(t);
                else
                    color = DefaultTrailColor(t);

                if (_vertexCount + 2 > _vertices.Length)
                    return;

                _vertices[_vertexCount] = new RenderVertex
                {
                    Position = new Vector3(pos.x + normal.x * halfWidth, pos.y + normal.y * halfWidth, 0f),
                    Color = color,
                    UV = new Vector2(0f, t),
                };
                _vertices[_vertexCount + 1] = new RenderVertex
                {
                    Position = new Vector3(pos.x - normal.x * halfWidth, pos.y - normal.y * halfWidth, 0f),
                    Color = color,
                    UV = new Vector2(1f, t),
                };

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

        /// <summary>
        /// 判断 Gradient 是否经过用户自定义配置。
        /// Unity 默认 Gradient = 2 个 colorKey（白白）+ 2 个 alphaKey（1,1）。
        /// </summary>
        private static bool IsGradientCustomized(Gradient gradient)
        {
            var cKeys = gradient.colorKeys;
            var aKeys = gradient.alphaKeys;

            // 非默认结构 → 用户自定义
            if (cKeys.Length != 2 || aKeys.Length != 2)
                return true;

            // 检查是否全是默认白色
            for (int i = 0; i < cKeys.Length; i++)
            {
                var c = cKeys[i].color;
                if (c.r < 0.99f || c.g < 0.99f || c.b < 0.99f)
                    return true;
            }

            // 检查 alpha 是否全是 1.0
            for (int i = 0; i < aKeys.Length; i++)
            {
                if (aKeys[i].alpha < 0.99f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 默认拖尾颜色：头部白色全不透明 → 尾部白色全透明（淡出效果）。
        /// Points[0] = 最老（尾部），Points[末尾] = 最新（头部），
        /// 所以 t=0 对应尾部（透明），t=1 对应头部（不透明）。
        /// 当 BulletTypeSO.TrailColor 未自定义时使用。
        /// </summary>
        private static Color32 DefaultTrailColor(float t)
        {
            // DIAG: 亮青色高对比度验证（确认后改回白色）
            byte alpha = (byte)(255 * t);
            return new Color32(0, 255, 255, alpha);
        }

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
