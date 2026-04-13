using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 渲染批次管理器——按 (RenderLayer, Texture2D) 二元组分桶，每桶一个 Mesh + 一个 DrawCall。
    /// <para>
    /// 设计原则（ADR-015）：
    /// 1. 只允许初始化时按注册表预热桶，运行时禁止隐式建桶
    /// 2. 共享实现，不共享实例：Danmaku 和 VFX 各自持有各自的 RenderBatchManager 实例
    /// 3. 对未知贴图：开发期报错计数，运行时跳过渲染
    /// </para>
    /// </summary>
    public class RenderBatchManager : IDisposable
    {
        // ──── 桶标识 ────

        /// <summary>
        /// 渲染桶的唯一标识——(RenderLayer, Texture2D) 二元组。
        /// 结构体 Key + IEquatable 避免 Dictionary 查找时装箱。
        /// </summary>
        public readonly struct BucketKey : IEquatable<BucketKey>
        {
            public readonly RenderLayer Layer;
            public readonly Texture2D Texture;

            public BucketKey(RenderLayer layer, Texture2D texture)
            {
                Layer = layer;
                Texture = texture;
            }

            public bool Equals(BucketKey other)
            {
                return Layer == other.Layer && ReferenceEquals(Texture, other.Texture);
            }

            public override bool Equals(object obj) => obj is BucketKey other && Equals(other);

            public override int GetHashCode()
            {
                // Texture 的 InstanceID 作为 hash，避免 GetHashCode 虚调用
                int texHash = Texture != null ? Texture.GetInstanceID() : 0;
                return ((int)Layer * 397) ^ texHash;
            }
        }

        // ──── 渲染桶 ────

        /// <summary>
        /// 单个渲染桶——持有一个 Mesh、一个材质实例、一个顶点数组。
        /// </summary>
        public class RenderBucket
        {
            public RenderVertex[] Vertices;
            public int QuadCount;
            public int MaxQuads;
            public Mesh Mesh;
            public Material Material;
            public int SortingOrder;

            /// <summary>
            /// 分配一个 Quad 的顶点空间（4 顶点），返回 baseVertex 索引。
            /// 桶满时返回 -1。
            /// </summary>
            public int AllocateQuad()
            {
                if (QuadCount >= MaxQuads) return -1;
                int baseVertex = QuadCount * 4;
                QuadCount++;
                return baseVertex;
            }
        }

        // VertexAttributeDescriptor 缓存（与 BulletRenderer 一致，避免 GC）
        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        // 桶存储：Dictionary 用于 Key→Index 查找，数组用于遍历
        private Dictionary<BucketKey, int> _bucketIndex;
        private RenderBucket[] _buckets;
        private int _bucketCount;

        // 调试：未知桶错误计数
        private int _unknownBucketErrorCount;

        private bool _initialized;

        /// <summary>调试用：运行时请求了未知桶的次数</summary>
        public int UnknownBucketErrorCount => _unknownBucketErrorCount;

        /// <summary>当前桶数量</summary>
        public int BucketCount => _bucketCount;

        // ──── 初始化 ────

        /// <summary>
        /// 初始化：预热所有桶。必须在渲染循环开始前调用。
        /// </summary>
        /// <param name="keys">需要预热的所有桶标识</param>
        /// <param name="normalMaterial">Normal 层的模板材质</param>
        /// <param name="additiveMaterial">Additive 层的模板材质</param>
        /// <param name="maxQuadsPerBucket">每个桶的最大 Quad 数</param>
        /// <param name="sortingOrderProvider">根据 RenderLayer 返回 sortingOrder 的委托，null 时使用 RenderSortingOrder 默认值</param>
        public void Initialize(
            IReadOnlyList<BucketKey> keys,
            Material normalMaterial,
            Material additiveMaterial,
            int maxQuadsPerBucket,
            Func<RenderLayer, int> sortingOrderProvider = null)
        {
            if (_initialized)
            {
                Debug.LogWarning("[RenderBatchManager] Already initialized. Call Dispose() first.");
                return;
            }

            _bucketIndex = new Dictionary<BucketKey, int>(keys.Count);
            _buckets = new RenderBucket[keys.Count];
            _bucketCount = 0;
            _unknownBucketErrorCount = 0;

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                // 跳过 null 贴图
                if (key.Texture == null)
                {
                    Debug.LogWarning($"[RenderBatchManager] Skipping bucket with null texture (Layer={key.Layer}).");
                    continue;
                }

                // 跳过重复 key
                if (_bucketIndex.ContainsKey(key))
                    continue;

                // 选择模板材质
                Material templateMat = key.Layer == RenderLayer.Additive ? additiveMaterial : normalMaterial;
                if (templateMat == null)
                {
                    Debug.LogWarning($"[RenderBatchManager] Template material is null for Layer={key.Layer}, Texture={key.Texture.name}. Skipping.");
                    continue;
                }

                // 创建材质实例并绑定贴图
                var matInstance = new Material(templateMat)
                {
                    name = $"BatchMat_{key.Layer}_{key.Texture.name} (Instance)",
                    mainTexture = key.Texture,
                };

                // sortingOrder
                int sortingOrder = sortingOrderProvider != null
                    ? sortingOrderProvider(key.Layer)
                    : GetDefaultSortingOrder(key.Layer);

                // 创建 Mesh
                int vertexCount = maxQuadsPerBucket * 4;
                int indexCount = maxQuadsPerBucket * 6;

                var mesh = new Mesh
                {
                    name = $"BatchMesh_{key.Layer}_{key.Texture.name}",
                    indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                };

                mesh.SetVertexBufferParams(vertexCount, VertexLayout);
                mesh.SetIndexBufferParams(indexCount,
                    vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16);

                // 预填充 Quad 索引
                var indices = new int[indexCount];
                for (int q = 0; q < maxQuadsPerBucket; q++)
                {
                    int vi = q * 4;
                    int ii = q * 6;
                    indices[ii + 0] = vi + 0;
                    indices[ii + 1] = vi + 1;
                    indices[ii + 2] = vi + 2;
                    indices[ii + 3] = vi + 2;
                    indices[ii + 4] = vi + 3;
                    indices[ii + 5] = vi + 0;
                }
                mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);

                // 组装桶
                var bucket = new RenderBucket
                {
                    Vertices = new RenderVertex[vertexCount],
                    QuadCount = 0,
                    MaxQuads = maxQuadsPerBucket,
                    Mesh = mesh,
                    Material = matInstance,
                    SortingOrder = sortingOrder,
                };

                _bucketIndex[key] = _bucketCount;
                _buckets[_bucketCount] = bucket;
                _bucketCount++;
            }

            _initialized = true;
        }

        // ──── 运行时 API ────

        /// <summary>
        /// 尝试获取桶来写入 Quad。未知桶返回 false + 累加报错计数。
        /// </summary>
        public bool TryGetBucket(BucketKey key, out RenderBucket bucket)
        {
            if (_bucketIndex != null && _bucketIndex.TryGetValue(key, out int idx))
            {
                bucket = _buckets[idx];
                return true;
            }

            _unknownBucketErrorCount++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[RenderBatchManager] Unknown bucket: Layer={key.Layer}, Texture={(key.Texture != null ? key.Texture.name : "null")}. Error count: {_unknownBucketErrorCount}");
#endif
            bucket = null;
            return false;
        }

        /// <summary>
        /// 每帧开始时重置所有桶的 Quad 计数。
        /// </summary>
        public void ResetAll()
        {
            for (int i = 0; i < _bucketCount; i++)
            {
                _buckets[i].QuadCount = 0;
            }
        }

        /// <summary>
        /// 统一提交所有桶的 SetVertexBufferData + Graphics.DrawMesh。
        /// </summary>
        public void UploadAndDrawAll()
        {
            for (int i = 0; i < _bucketCount; i++)
            {
                var bucket = _buckets[i];
                if (bucket.QuadCount == 0) continue;

                int vertexCount = bucket.QuadCount * 4;
                int indexCount = bucket.QuadCount * 6;

                bucket.Mesh.SetVertexBufferData(bucket.Vertices, 0, 0, vertexCount, 0,
                    MeshUpdateFlags.DontValidateIndices |
                    MeshUpdateFlags.DontRecalculateBounds |
                    MeshUpdateFlags.DontNotifyMeshUsers);

                bucket.Mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles),
                    MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

                bucket.Mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 10f));

                Graphics.DrawMesh(bucket.Mesh, Matrix4x4.identity, bucket.Material, 0);
            }
        }

        /// <summary>
        /// 释放所有 Mesh 和材质实例资源。
        /// </summary>
        public void Dispose()
        {
            if (_buckets != null)
            {
                for (int i = 0; i < _bucketCount; i++)
                {
                    var bucket = _buckets[i];
                    if (bucket.Mesh != null) UnityEngine.Object.Destroy(bucket.Mesh);
                    if (bucket.Material != null) UnityEngine.Object.Destroy(bucket.Material);
                }
            }

            _buckets = null;
            _bucketIndex = null;
            _bucketCount = 0;
            _initialized = false;
        }

        // ──── 辅助 ────

        private static int GetDefaultSortingOrder(RenderLayer layer)
        {
            return layer switch
            {
                RenderLayer.Normal => RenderSortingOrder.BulletNormal,
                RenderLayer.Additive => RenderSortingOrder.BulletAdditive,
                _ => 0,
            };
        }
    }
}
