using MiniGameTemplate.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// Sprite Sheet 特效合批渲染器。
    /// 采用双 Mesh（Normal + Additive），每帧重建顶点并提交。
    /// </summary>
    public class VFXBatchRenderer
    {
        private Mesh _meshNormal;
        private Mesh _meshAdditive;
        private RenderVertex[] _verticesNormal;
        private RenderVertex[] _verticesAdditive;
        private int[] _indicesNormal;
        private int[] _indicesAdditive;
        private Material _materialNormal;
        private Material _materialAdditive;
        private readonly MaterialPropertyBlock _normalPropertyBlock = new();
        private readonly MaterialPropertyBlock _additivePropertyBlock = new();
        private int _normalQuadCount;
        private int _additiveQuadCount;

        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        public void Initialize(VFXRenderConfig renderConfig, int maxInstances)
        {
            int vertexCount = maxInstances * 4;
            int indexCount = maxInstances * 6;

            _verticesNormal = new RenderVertex[vertexCount];
            _verticesAdditive = new RenderVertex[vertexCount];
            _indicesNormal = new int[indexCount];
            _indicesAdditive = new int[indexCount];

            FillQuadIndices(_indicesNormal, maxInstances);
            FillQuadIndices(_indicesAdditive, maxInstances);

            _meshNormal = CreateMesh("VFXMesh_Normal", vertexCount, indexCount);
            _meshAdditive = CreateMesh("VFXMesh_Additive", vertexCount, indexCount);
            _meshNormal.SetIndices(_indicesNormal, MeshTopology.Triangles, 0, false);
            _meshAdditive.SetIndices(_indicesAdditive, MeshTopology.Triangles, 0, false);

            _materialNormal = renderConfig != null && renderConfig.NormalMaterial != null
                ? new Material(renderConfig.NormalMaterial) { name = "VFX_Normal (Instance)" }
                : null;
            _materialAdditive = renderConfig != null && renderConfig.AdditiveMaterial != null
                ? new Material(renderConfig.AdditiveMaterial) { name = "VFX_Additive (Instance)" }
                : null;

            if (renderConfig != null && renderConfig.AtlasTexture != null)
            {
                if (_materialNormal != null)
                    _materialNormal.mainTexture = renderConfig.AtlasTexture;
                if (_materialAdditive != null)
                    _materialAdditive.mainTexture = renderConfig.AtlasTexture;
            }
        }

        public void Rebuild(VFXPool pool, VFXTypeRegistrySO registry)
        {
            _normalQuadCount = 0;
            _additiveQuadCount = 0;

            if (pool == null || registry == null)
                return;

            var instances = pool.Instances;
            for (int i = 0; i < pool.Capacity; i++)
            {
                ref var instance = ref instances[i];
                if (!instance.IsActive)
                    continue;

                if (!registry.TryGet(instance.TypeIndex, out var type))
                    continue;

                WriteQuad(ref instance, type);
            }

            UploadAndDraw(_meshNormal, _verticesNormal, _normalQuadCount, _materialNormal, _normalPropertyBlock);
            UploadAndDraw(_meshAdditive, _verticesAdditive, _additiveQuadCount, _materialAdditive, _additivePropertyBlock);
        }

        public void Dispose()
        {
            if (_meshNormal != null) Object.Destroy(_meshNormal);
            if (_meshAdditive != null) Object.Destroy(_meshAdditive);
            if (_materialNormal != null) Object.Destroy(_materialNormal);
            if (_materialAdditive != null) Object.Destroy(_materialAdditive);
        }

        private void WriteQuad(ref VFXInstance instance, VFXTypeSO type)
        {
            bool isAdditive = type.Layer == RenderLayer.Additive;
            var vertices = isAdditive ? _verticesAdditive : _verticesNormal;
            ref int quadCount = ref (isAdditive ? ref _additiveQuadCount : ref _normalQuadCount);


            int baseVertex = quadCount * 4;
            if (baseVertex + 4 > vertices.Length)
                return;

            Rect frameUV = GetFrameUV(type, instance.CurrentFrame);
            float halfW = type.Size.x * instance.Scale * 0.5f;
            float halfH = type.Size.y * instance.Scale * 0.5f;
            float radians = type.RotateWithInstance ? instance.RotationDegrees * Mathf.Deg2Rad : 0f;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            WriteVertex(ref vertices[baseVertex + 0], instance.Position, -halfW, -halfH, cos, sin, frameUV.xMin, frameUV.yMin, instance.Color);
            WriteVertex(ref vertices[baseVertex + 1], instance.Position, halfW, -halfH, cos, sin, frameUV.xMax, frameUV.yMin, instance.Color);
            WriteVertex(ref vertices[baseVertex + 2], instance.Position, halfW, halfH, cos, sin, frameUV.xMax, frameUV.yMax, instance.Color);
            WriteVertex(ref vertices[baseVertex + 3], instance.Position, -halfW, halfH, cos, sin, frameUV.xMin, frameUV.yMax, instance.Color);

            quadCount++;
        }

        private static Rect GetFrameUV(VFXTypeSO type, int frame)
        {
            int columns = Mathf.Max(1, type.Columns);
            int rows = Mathf.Max(1, type.Rows);
            int clampedFrame = Mathf.Clamp(frame, 0, type.MaxFrameCount - 1);
            int x = clampedFrame % columns;
            int y = clampedFrame / columns;

            float frameWidth = type.AtlasUV.width / columns;
            float frameHeight = type.AtlasUV.height / rows;

            return new Rect(
                type.AtlasUV.x + x * frameWidth,
                type.AtlasUV.y + y * frameHeight,
                frameWidth,
                frameHeight);
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

        private static void UploadAndDraw(Mesh mesh, RenderVertex[] vertices, int quadCount,
            Material material, MaterialPropertyBlock propertyBlock)
        {
            if (mesh == null || material == null || quadCount == 0)
                return;

            int vertexCount = quadCount * 4;
            int indexCount = quadCount * 6;

            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);

            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles),
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 10f));

            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, null, 0, propertyBlock);
        }

        private static Mesh CreateMesh(string name, int maxVertices, int maxIndices)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = maxVertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            mesh.SetVertexBufferParams(maxVertices, VertexLayout);
            mesh.SetIndexBufferParams(maxIndices,
                maxVertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16);
            return mesh;
        }

        private static void FillQuadIndices(int[] indices, int maxQuads)
        {
            for (int q = 0; q < maxQuads; q++)
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
        }
    }
}
