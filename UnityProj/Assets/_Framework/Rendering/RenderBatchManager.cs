using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 渲染批次管理器——按 (RenderLayer, Texture) 分桶，每桶一个 Mesh + 一个 DrawCall。
    /// v2.3：支持多模板材质注册、Texture 基类、注册时排序。
    /// </summary>
    public class RenderBatchManager : IDisposable
    {
        public readonly struct BucketKey : IEquatable<BucketKey>
        {
            public readonly RenderLayer Layer;
            public readonly Texture Texture;

            public BucketKey(RenderLayer layer, Texture texture)
            {
                Layer = layer;
                Texture = texture;
            }

            public bool Equals(BucketKey other)
            {
                if (Layer != other.Layer)
                    return false;

                int thisTextureId = Texture != null ? Texture.GetInstanceID() : 0;
                int otherTextureId = other.Texture != null ? other.Texture.GetInstanceID() : 0;
                return thisTextureId == otherTextureId;
            }

            public override bool Equals(object obj) => obj is BucketKey other && Equals(other);

            public override int GetHashCode()
            {
                int texHash = Texture != null ? Texture.GetInstanceID() : 0;
                return ((int)Layer * 397) ^ texHash;
            }
        }

        public readonly struct BucketRegistration
        {
            public readonly BucketKey Key;
            public readonly Material TemplateMaterial;
            public readonly int SortingOrder;

            public BucketRegistration(BucketKey key, Material templateMaterial, int sortingOrder)
            {
                Key = key;
                TemplateMaterial = templateMaterial;
                SortingOrder = sortingOrder;
            }
        }

        public class RenderBucket
        {
            public BucketKey Key;
            public RenderVertex[] Vertices;
            public int QuadCount;
            public int MaxQuads;
            public Mesh Mesh;
            public Material Material;
            public int SortingOrder;


            public int AllocateQuad()
            {
                if (QuadCount >= MaxQuads) return -1;
                int baseVertex = QuadCount * 4;
                QuadCount++;
                return baseVertex;
            }
        }

        // IMPORTANT: Unity 会将非标准顺序的顶点属性强制重排为标准顺序。
        // 标准顺序为：Position → Normal → Tangent → Color → TexCoord0~7。
        // 此数组声明顺序必须遵循标准顺序，且必须与 RenderVertex 结构体字段顺序完全一致。
        // 违反此规则会导致 CPU 结构体与 GPU 内存布局不一致，表现为渲染错误或不可见。
        private static readonly VertexAttributeDescriptor[] VertexLayout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        private Dictionary<BucketKey, int> _bucketIndex;
        private List<RenderBucket> _buckets;
        private int _unknownBucketErrorCount;
        private int _dynamicBucketCreatedCount;
        private int _peakBucketCountThisFrame;
        private int _maxQuadsPerBucket;
        private int _maxBuckets;
        private bool _initialized;

        public int UnknownBucketErrorCount => _unknownBucketErrorCount;
        public int BucketCount => _buckets != null ? _buckets.Count : 0;
        public int DynamicBucketCreatedCount => _dynamicBucketCreatedCount;
        public int PeakBucketCountThisFrame => _peakBucketCountThisFrame;

        public void Initialize(IReadOnlyList<BucketRegistration> registrations, int maxQuadsPerBucket)
        {
            if (_initialized)
            {
                Debug.LogWarning("[RenderBatchManager] Already initialized. Call Dispose() first.");
                return;
            }

            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));
            if (maxQuadsPerBucket <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxQuadsPerBucket), "maxQuadsPerBucket 必须 > 0");

            _bucketIndex = new Dictionary<BucketKey, int>(registrations.Count);
            _buckets = new List<RenderBucket>(registrations.Count);
            _unknownBucketErrorCount = 0;
            _dynamicBucketCreatedCount = 0;
            _peakBucketCountThisFrame = registrations.Count;
            _maxQuadsPerBucket = maxQuadsPerBucket;
            _maxBuckets = Mathf.Max(64, registrations.Count == 0 ? 256 : registrations.Count * 4);

            for (int i = 0; i < registrations.Count; i++)
            {
                BucketRegistration registration = registrations[i];
                BucketKey key = registration.Key;

                if (key.Texture == null)
                {
                    Debug.LogWarning($"[RenderBatchManager] Skipping bucket with null texture (Layer={key.Layer}).");
                    continue;
                }

                if (registration.TemplateMaterial == null)
                {
                    Debug.LogWarning($"[RenderBatchManager] Template material is null for Texture={key.Texture.name}. Skipping.");
                    continue;
                }

                if (_bucketIndex.ContainsKey(key))
                    continue;

                RenderBucket bucket = CreateBucket(key, registration.TemplateMaterial, registration.SortingOrder, maxQuadsPerBucket);
                if (bucket == null)
                    continue;

                _bucketIndex[key] = _buckets.Count;
                _buckets.Add(bucket);
            }

            SortBucketsBySortingOrder();
            _initialized = true;
        }

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

        public bool TryGetOrCreateBucket(BucketKey key, Material templateMaterial, int sortingOrder, out RenderBucket bucket)
        {
            // 热路径：O(1) 字典命中——不走 TryGetBucket 避免副作用
            if (_bucketIndex != null && _bucketIndex.TryGetValue(key, out int idx))
            {
                bucket = _buckets[idx];
                return true;
            }

            // 冷路径：首次遇到新类型，动态建桶
            if (!_initialized || key.Texture == null || templateMaterial == null)
            {
                bucket = null;
                return false;
            }

            if (_buckets.Count >= _maxBuckets)
            {
                bucket = null;
                return false;
            }

            // 防御性兜底：若字典索引暂时不同步，线性扫描现有桶避免同 key 重复创建。
            // 这是冷路径，优先保证正确性而非极致性能。
            for (int i = 0; i < _buckets.Count; i++)
            {
                if (_buckets[i].Key.Equals(key))
                {
                    bucket = _buckets[i];
                    if (_bucketIndex != null)
                        _bucketIndex[key] = i;
                    return true;
                }
            }

            bucket = CreateBucket(key, templateMaterial, sortingOrder, _maxQuadsPerBucket);
            if (bucket == null)
                return false;

            _buckets.Add(bucket);
            _dynamicBucketCreatedCount++;

            // 排序并重建索引（SortBucketsBySortingOrder 内部调用 RebuildBucketIndex）。
            SortBucketsBySortingOrder();

            if (_buckets.Count > _peakBucketCountThisFrame)
                _peakBucketCountThisFrame = _buckets.Count;
            return true;
        }

        public void ResetAll()
        {
            _peakBucketCountThisFrame = BucketCount;
            for (int i = 0; i < _buckets.Count; i++)
            {
                _buckets[i].QuadCount = 0;
            }
        }

        public void UploadAndDrawAll()
        {
            int drawCalls = 0;
            int activeBatches = 0;

            for (int i = 0; i < _buckets.Count; i++)
            {
                RenderBucket bucket = _buckets[i];
                if (bucket.QuadCount == 0) continue;

                activeBatches++;

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
                drawCalls++;
            }

            RenderBatchManagerRuntimeStats.AccumulateBatch(drawCalls, activeBatches, _unknownBucketErrorCount);
        }

        public void Dispose()
        {
            if (_buckets != null)
            {
                for (int i = 0; i < _buckets.Count; i++)
                {
                    RenderBucket bucket = _buckets[i];
                    if (bucket.Mesh != null) UnityEngine.Object.Destroy(bucket.Mesh);
                    if (bucket.Material != null) UnityEngine.Object.Destroy(bucket.Material);
                }
            }

            _buckets = null;
            _bucketIndex = null;
            _dynamicBucketCreatedCount = 0;
            _peakBucketCountThisFrame = 0;
            _maxQuadsPerBucket = 0;
            _maxBuckets = 0;
            _initialized = false;
        }

        private void SortBucketsBySortingOrder()
        {
            if (_buckets == null || _buckets.Count <= 1)
                return;

            _buckets.Sort((a, b) => a.SortingOrder.CompareTo(b.SortingOrder));
            RebuildBucketIndex();
        }

        private void RebuildBucketIndex()
        {
            _bucketIndex.Clear();
            for (int i = 0; i < _buckets.Count; i++)
            {
                _bucketIndex[_buckets[i].Key] = i;
            }
        }

        private static RenderBucket CreateBucket(BucketKey key, Material templateMaterial, int sortingOrder, int maxQuadsPerBucket)
        {
            if (key.Texture == null || templateMaterial == null || maxQuadsPerBucket <= 0)
                return null;

            Material matInstance = new Material(templateMaterial)
            {
                name = $"BatchMat_{key.Layer}_{key.Texture.name} (Instance)",
            };

            // 关键修复：new Material(templateMaterial) 不会可靠保留 shader keyword。
            // Laser Atlas 依赖 _ATLASMODE_ON 变体；若 keyword 丢失，会错误走非 Atlas 分支导致不可见。
            matInstance.shaderKeywords = templateMaterial.shaderKeywords;
            matInstance.mainTexture = key.Texture;
            if (matInstance.HasProperty("_Color"))
                matInstance.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
            matInstance.renderQueue = 3000 + sortingOrder;

            int vertexCount = maxQuadsPerBucket * 4;
            int indexCount = maxQuadsPerBucket * 6;

            Mesh mesh = new Mesh
            {
                name = $"BatchMesh_{key.Layer}_{key.Texture.name}",
                indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            mesh.SetVertexBufferParams(vertexCount, VertexLayout);
            mesh.SetIndexBufferParams(indexCount, vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16);

            int[] indices = new int[indexCount];
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

            return new RenderBucket
            {
                Key = key,
                Vertices = new RenderVertex[vertexCount],
                QuadCount = 0,
                MaxQuads = maxQuadsPerBucket,
                Mesh = mesh,
                Material = matInstance,
                SortingOrder = sortingOrder,
            };
        }
    }
}
