using MiniGameTemplate.Rendering;
using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// VFX Sprite Sheet 合批渲染器——通过 RenderBatchManager 按 Texture 分桶渲染（ADR-029 v2：统一 Normal）。
    /// 支持每个 VFXTypeSO 引用独立贴图。
    /// </summary>
    public class VFXBatchRenderer
    {
        private RenderBatchManager _batchManager;
        private RuntimeAtlasManager _runtimeAtlas;
        private Texture2D _fallbackAtlas; // 旧资产 SourceTexture 为空时的 fallback
        private int _totalQuadCount;

        /// <summary>上帧绘制的 VFX 总 Quad 数</summary>
        public int TotalDrawCount => _totalQuadCount;

        /// <summary>
        /// 初始化渲染器——从 VFXTypeRegistrySO 收集所有 Texture 预热桶。
        /// </summary>
        public void Initialize(VFXRenderConfig renderConfig, VFXTypeRegistrySO registry, int maxQuadsPerBucket)
        {
            _batchManager = new RenderBatchManager();
            _fallbackAtlas = renderConfig != null ? renderConfig.AtlasTexture : null;
            _runtimeAtlas = null;

            if (renderConfig != null && renderConfig.RuntimeAtlasConfig != null)
            {
                _runtimeAtlas = new RuntimeAtlasManager();
                _runtimeAtlas.Initialize(renderConfig.RuntimeAtlasConfig);
            }

            Material mat = renderConfig != null ? renderConfig.NormalMaterial : null;
            var registrations = new System.Collections.Generic.List<RenderBatchManager.BucketRegistration>();

            if (registry != null)
            {
                for (int i = 0; i < registry.Count; i++)
                {
                    if (!registry.TryGet((ushort)i, out var vfxType)) continue;

                    var binding = RuntimeAtlasBindingResolver.ResolveVFX(_runtimeAtlas, _fallbackAtlas, vfxType);
                    if (!binding.IsValid) continue;

                    var key = new RenderBatchManager.BucketKey(RenderLayer.Normal, binding.Texture);
                    bool exists = false;
                    for (int j = 0; j < registrations.Count; j++)
                    {
                        if (registrations[j].Key.Equals(key))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                        registrations.Add(new RenderBatchManager.BucketRegistration(key, mat, RenderSortingOrder.VFX));
                }
            }

            // 兼容：如果没有任何桶被收集但全局 Atlas 存在，至少建一个 fallback 桶
            if (registrations.Count == 0 && _fallbackAtlas != null)
            {
                registrations.Add(new RenderBatchManager.BucketRegistration(
                    new RenderBatchManager.BucketKey(RenderLayer.Normal, _fallbackAtlas),
                    mat,
                    RenderSortingOrder.VFX));
            }

            _batchManager.Initialize(registrations, maxQuadsPerBucket);
        }

        /// <summary>
        /// 每帧由 VFX 系统调用——遍历活跃实例，填充顶点，统一提交。
        /// </summary>
        public void Rebuild(VFXPool pool, VFXTypeRegistrySO registry)
        {
            _batchManager.ResetAll();
            _totalQuadCount = 0;

            if (pool == null || registry == null)
            {
                _batchManager.UploadAndDrawAll();
                return;
            }

            var instances = pool.Instances;
            for (int i = 0; i < pool.Capacity; i++)
            {
                ref var instance = ref instances[i];
                if (!instance.IsActive) continue;
                if (!registry.TryGet(instance.TypeIndex, out var type)) continue;

                var binding = RuntimeAtlasBindingResolver.ResolveVFX(_runtimeAtlas, _fallbackAtlas, type);
                if (!binding.IsValid) continue;

                Texture texture = binding.Texture;
                Rect baseUV = binding.UVRect;

                var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, texture);
                if (!_batchManager.TryGetBucket(bucketKey, out var bucket)) continue;

                WriteQuad(bucket, ref instance, type, baseUV);
            }

            _batchManager.UploadAndDrawAll();
        }

        /// <summary>获取 RuntimeAtlas 统计快照（无 Atlas 时返回 null）。</summary>
        public RuntimeAtlasStats? GetAtlasStats()
        {
            return _runtimeAtlas != null && _runtimeAtlas.IsInitialized
                ? _runtimeAtlas.GetStats()
                : (RuntimeAtlasStats?)null;
        }

        /// <summary>释放 BatchManager 资源。</summary>
        public void Dispose()
        {
            _batchManager?.Dispose();
            _runtimeAtlas?.Dispose();
            _runtimeAtlas = null;
        }

        // ──── Quad 写入 ────

        private void WriteQuad(RenderBatchManager.RenderBucket bucket, ref VFXInstance instance, VFXTypeSO type, Rect baseUV)
        {
            int baseVertex = bucket.AllocateQuad();
            if (baseVertex < 0) return;

            _totalQuadCount++;

            Rect frameUV = type.GetFrameUV(instance.CurrentFrame, baseUV);
            float halfW = type.Size.x * instance.Scale * 0.5f;
            float halfH = type.Size.y * instance.Scale * 0.5f;
            float radians = type.RotateWithInstance ? instance.RotationDegrees * Mathf.Deg2Rad : 0f;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            var verts = bucket.Vertices;
            WriteVertex(ref verts[baseVertex + 0], instance.Position, -halfW, -halfH, cos, sin, frameUV.xMin, frameUV.yMin, instance.Color);
            WriteVertex(ref verts[baseVertex + 1], instance.Position, halfW, -halfH, cos, sin, frameUV.xMax, frameUV.yMin, instance.Color);
            WriteVertex(ref verts[baseVertex + 2], instance.Position, halfW, halfH, cos, sin, frameUV.xMax, frameUV.yMax, instance.Color);
            WriteVertex(ref verts[baseVertex + 3], instance.Position, -halfW, halfH, cos, sin, frameUV.xMin, frameUV.yMax, instance.Color);
        }

        private static void WriteVertex(ref RenderVertex vertex, Vector3 center,
            float offsetX, float offsetY, float cos, float sin,
            float uvX, float uvY, Color32 color)
        {
            float rx = offsetX * cos - offsetY * sin;
            float ry = offsetX * sin + offsetY * cos;

            vertex.Position = new Vector3(center.x + rx, center.y + ry, center.z);
            vertex.UV = new Vector2(uvX, uvY);
            vertex.Color = color;
        }

    }
}
