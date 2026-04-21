using MiniGameTemplate.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 重量拖尾曲线池——保持独立 Mesh，但接入统一渲染统计（R3.2 / 方案 A）。
    /// 为设置 FLAG_HEAVY_TRAIL 的弹丸提供平滑曲线拖尾，与 Mesh 内 Ghost 残影互补。
    /// 渲染方式：Graphics.DrawMesh（与弹丸 RBM 走同一渲染路径，层次由调用顺序决定）。
    /// Trail Phase T2: RuntimeAtlas 集成——纹理化拖尾 + whiteTexture fallback 统一入 Atlas。
    /// PI-001: 接收共享 RuntimeAtlasManager。
    /// PI-004: RT Lost 恢复路径补全。
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

        // Trail Phase T1.2: RuntimeAtlas 支持
        private RuntimeAtlasManager _runtimeAtlas;
        private Rect _whiteTextureUV;  // whiteTexture 在 Atlas 中的 UV
        private RenderTexture _atlasRT; // Atlas 贴图引用（替代 whiteTexture）

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

        /// <summary>
        /// PI-001: 接收共享 RuntimeAtlasManager。
        /// 策略 A: whiteTexture 也 Blit 到 Atlas 保持 1 DC。
        /// </summary>
        public void Initialize(Material material, RuntimeAtlasManager sharedAtlas = null)
        {
            _runtimeAtlas = sharedAtlas;

            if (material != null)
            {
                _material = new Material(material) { name = "Danmaku Trail (Instance)" };

                if (_runtimeAtlas != null && _runtimeAtlas.IsInitialized)
                {
                    // 策略 A: 分配 whiteTexture 到 Atlas 作为无纹理 Trail 的 fallback
                    var whiteAlloc = _runtimeAtlas.Allocate(AtlasChannel.Trail, Texture2D.whiteTexture);
                    if (whiteAlloc.Valid)
                    {
                        _atlasRT = _runtimeAtlas.GetAtlasTexture(AtlasChannel.Trail, whiteAlloc.PageIndex);
                        _whiteTextureUV = whiteAlloc.UVRect;
                        _material.mainTexture = _atlasRT;
                    }
                    else
                    {
                        _material.mainTexture = Texture2D.whiteTexture;
                        _whiteTextureUV = new Rect(0, 0, 1, 1);
                    }
                }
                else
                {
                    _material.mainTexture = Texture2D.whiteTexture;
                    _whiteTextureUV = new Rect(0, 0, 1, 1);
                }
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

        /// <summary>
        /// T2.2: 分配 Trail 并预解算纹理 UV。
        /// </summary>
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

            // T2.2：解算纹理 UV
            if (type.TrailTexture != null && _runtimeAtlas != null && _runtimeAtlas.IsInitialized)
            {
                var alloc = _runtimeAtlas.Allocate(AtlasChannel.Trail, type.TrailTexture);
                if (alloc.Valid)
                {
                    trail.TextureUVRect = alloc.UVRect;
                    // 确保 material 纹理指向 Atlas RT
                    if (_atlasRT == null)
                    {
                        _atlasRT = _runtimeAtlas.GetAtlasTexture(AtlasChannel.Trail, alloc.PageIndex);
                        if (_atlasRT != null)
                            _material.mainTexture = _atlasRT;
                    }
                }
                else
                {
                    trail.TextureUVRect = _whiteTextureUV; // 溢出 fallback
                }
            }
            else
            {
                trail.TextureUVRect = _whiteTextureUV; // 无纹理 fallback
            }

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

        /// <summary>
        /// PI-004: 渲染帧——含 RT Lost 检测 + 恢复尝试。
        /// </summary>
        public void Render()
        {
            // PI-004: RT Lost 检测 + 恢复尝试
            if (_atlasRT != null && !_atlasRT.IsCreated())
            {
                // RT Lost：先回退到 whiteTexture 保证本帧可渲染
                _material.mainTexture = Texture2D.whiteTexture;
                _atlasRT = null;
            }
            else if (_atlasRT == null && _runtimeAtlas != null && _runtimeAtlas.IsInitialized)
            {
                // 尝试恢复：检查 Atlas 是否已被 RestoreDirtyPages 重建
                var testAlloc = _runtimeAtlas.TryGetAllocation(AtlasChannel.Trail, Texture2D.whiteTexture);
                if (testAlloc.Valid)
                {
                    _atlasRT = _runtimeAtlas.GetAtlasTexture(AtlasChannel.Trail, testAlloc.PageIndex);
                    if (_atlasRT != null && _atlasRT.IsCreated())
                    {
                        _material.mainTexture = _atlasRT;
                    }
                    else
                    {
                        _atlasRT = null; // 仍未恢复
                    }
                }
            }

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

            Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);
            RenderBatchManagerRuntimeStats.AccumulateBatch(1, 1, 0);
        }

        /// <summary>PI-001: 共享 Atlas 由 DanmakuSystem 统一 Dispose。</summary>
        public void Dispose()
        {
            if (_mesh != null)
                Object.Destroy(_mesh);
            if (_material != null)
                Object.Destroy(_material);
            _runtimeAtlas = null;
            _atlasRT = null;
        }

        /// <summary>
        /// T2.4: BuildTrailMesh UV Remap——UV 映射到 Atlas 子区域。
        /// </summary>
        private void BuildTrailMesh(TrailInstance trail)
        {
            int pointCount = trail.PointCount;
            Rect uvRect = trail.TextureUVRect;

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

                Color32 color;
                if (trail.ColorGradient != null && IsGradientCustomized(trail.ColorGradient))
                    color = trail.ColorGradient.Evaluate(t);
                else
                    color = DefaultTrailColor(t);

                if (_vertexCount + 2 > _vertices.Length)
                    return;

                // T2.4: UV Remap 到 Atlas 子区域
                float u0 = uvRect.x;
                float u1 = uvRect.x + uvRect.width;
                float v = uvRect.y + t * uvRect.height;

                _vertices[_vertexCount] = new RenderVertex
                {
                    Position = new Vector3(pos.x + normal.x * halfWidth, pos.y + normal.y * halfWidth, 0f),
                    Color = color,
                    UV = new Vector2(u0, v),
                };
                _vertices[_vertexCount + 1] = new RenderVertex
                {
                    Position = new Vector3(pos.x - normal.x * halfWidth, pos.y - normal.y * halfWidth, 0f),
                    Color = color,
                    UV = new Vector2(u1, v),
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
        /// </summary>
        private static bool IsGradientCustomized(Gradient gradient)
        {
            var cKeys = gradient.colorKeys;
            var aKeys = gradient.alphaKeys;

            if (cKeys.Length != 2 || aKeys.Length != 2)
                return true;

            for (int i = 0; i < cKeys.Length; i++)
            {
                var c = cKeys[i].color;
                if (c.r < 0.99f || c.g < 0.99f || c.b < 0.99f)
                    return true;
            }

            for (int i = 0; i < aKeys.Length; i++)
            {
                if (aKeys[i].alpha < 0.99f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 默认拖尾颜色：头部白色全不透明 → 尾部白色全透明。
        /// </summary>
        private static Color32 DefaultTrailColor(float t)
        {
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
            public Rect TextureUVRect;  // T2.3: Atlas 中的 UV 子区域
            public readonly Vector2[] Points = new Vector2[MAX_POINTS_PER_TRAIL];
        }
    }
}
