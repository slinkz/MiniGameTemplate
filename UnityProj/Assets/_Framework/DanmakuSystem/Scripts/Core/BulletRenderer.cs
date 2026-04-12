using MiniGameTemplate.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕 Mesh 渲染器——双 Mesh 方案（Normal + Additive），每帧单次顶点上传。
    /// 交错顶点格式 RenderVertex (24 bytes) + SetVertexBufferData。
    /// 索引缓冲仅初始化时设置（固定 Quad 拓扑），每帧只更新顶点数据。
    /// </summary>
    public class BulletRenderer
    {
        private Mesh _meshNormal;
        private Mesh _meshAdditive;

        private RenderVertex[] _verticesNormal;
        private RenderVertex[] _verticesAdditive;

        // 索引缓冲共享——所有弹丸都是 Quad
        private int[] _indicesNormal;
        private int[] _indicesAdditive;

        private int _normalQuadCount;
        private int _additiveQuadCount;

        private Material _materialNormal;
        private Material _materialAdditive;

        private int _maxBullets;

        /// <summary>上帧 Normal 层绘制的弹丸数</summary>
        public int NormalDrawCount => _normalQuadCount;

        /// <summary>上帧 Additive 层绘制的弹丸数</summary>
        public int AdditiveDrawCount => _additiveQuadCount;

        // VertexAttributeDescriptor 缓存（避免 GC）
        private static readonly VertexAttributeDescriptor[] VertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        /// <summary>
        /// 初始化渲染器——创建 Mesh、分配顶点缓冲。
        /// </summary>
        public void Initialize(DanmakuRenderConfig renderConfig, int maxBullets)
        {
            _maxBullets = maxBullets;

            // 创建材质实例并绑定 BulletAtlas 纹理到 _MainTex
            _materialNormal = renderConfig.BulletMaterial != null
                ? new Material(renderConfig.BulletMaterial) { name = "DanmakuBullet_Normal (Instance)" }
                : null;
            _materialAdditive = renderConfig.BulletAdditiveMaterial != null
                ? new Material(renderConfig.BulletAdditiveMaterial) { name = "DanmakuBullet_Additive (Instance)" }
                : null;

            if (renderConfig.BulletAtlas != null)
            {
                if (_materialNormal != null) _materialNormal.mainTexture = renderConfig.BulletAtlas;
                if (_materialAdditive != null) _materialAdditive.mainTexture = renderConfig.BulletAtlas;
            }

            // 每个弹丸 = 1 Quad = 4 顶点，最多 maxBullets + 残影
            int maxQuads = maxBullets * 4;  // 预留残影空间（每颗最多 3 残影）
            int vertexCount = maxQuads * 4;
            int indexCount = maxQuads * 6;

            _verticesNormal = new RenderVertex[vertexCount];
            _verticesAdditive = new RenderVertex[vertexCount];
            _indicesNormal = new int[indexCount];
            _indicesAdditive = new int[indexCount];

            // 预填充索引（0,1,2, 2,3,0 Quad 拓扑）
            FillQuadIndices(_indicesNormal, maxQuads);
            FillQuadIndices(_indicesAdditive, maxQuads);

            _meshNormal = CreateMesh("DanmakuMesh_Normal", vertexCount, indexCount);
            _meshAdditive = CreateMesh("DanmakuMesh_Additive", vertexCount, indexCount);

            // 设置索引缓冲（一次性，运行时不再更新）
            _meshNormal.SetIndices(_indicesNormal, MeshTopology.Triangles, 0, false);
            _meshAdditive.SetIndices(_indicesAdditive, MeshTopology.Triangles, 0, false);
        }

        /// <summary>
        /// 每帧由 DanmakuSystem.LateUpdate 调用——收集弹丸 → 填充顶点 → 上传 → DrawMesh。
        /// </summary>
        public void Rebuild(
            BulletWorld world,
            BulletTrail[] trails,
            DanmakuTypeRegistry registry)
        {
            _normalQuadCount = 0;
            _additiveQuadCount = 0;

            var cores = world.Cores;
            int capacity = world.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref var core = ref cores[i];
                if ((core.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;

                var bulletType = registry.BulletTypes[core.TypeIndex];
                bool isAdditive = bulletType.Layer == RenderLayer.Additive;

                // 受伤闪烁：FlashTimer > 0 时用 DamageFlashTint 替代 Tint
                ref var trail = ref trails[i];
                Color tint = bulletType.Tint;
                if (trail.FlashTimer > 0)
                {
                    tint = bulletType.DamageFlashTint;
                    trail.FlashTimer--;
                }

                // 爆炸帧动画：Exploding 阶段用 ExplosionAtlasUV + 帧偏移
                if (core.Phase == (byte)BulletPhase.Exploding
                    && bulletType.Explosion == ExplosionMode.MeshFrame
                    && bulletType.ExplosionFrameCount > 0)
                {
                    float frameDuration = 1f / 60f;  // 每帧时长（60fps）
                    int frame = Mathf.Clamp(
                        (int)(core.Elapsed / frameDuration),
                        0, bulletType.ExplosionFrameCount - 1);

                    Rect uv = bulletType.ExplosionAtlasUV;
                    float frameWidth = uv.width;
                    Rect frameUV = new Rect(
                        uv.x + frame * frameWidth, uv.y,
                        frameWidth, uv.height);

                    // 爆炸帧渐隐
                    float explosionAlpha = 1f - (float)frame / bulletType.ExplosionFrameCount;

                    WriteQuadUV(ref core, bulletType, isAdditive, explosionAlpha, tint, frameUV);
                }
                else
                {
                    // 写入主弹丸 Quad
                    WriteQuad(ref core, bulletType, isAdditive, 1f, tint);
                }

                // 残影 Quad（Ghost 模式——Exploding 阶段不画残影）
                if (core.Phase == (byte)BulletPhase.Active)
                {
                    if (trail.TrailLength >= 1)
                        WriteGhostQuad(trail.PrevPos1, bulletType, isAdditive, 0.6f);
                    if (trail.TrailLength >= 2)
                        WriteGhostQuad(trail.PrevPos2, bulletType, isAdditive, 0.3f);
                    if (trail.TrailLength >= 3)
                        WriteGhostQuad(trail.PrevPos3, bulletType, isAdditive, 0.15f);
                }
            }

            // 上传顶点数据
            UploadAndDraw(_meshNormal, _verticesNormal, _normalQuadCount, _materialNormal);
            UploadAndDraw(_meshAdditive, _verticesAdditive, _additiveQuadCount, _materialAdditive);
        }

        /// <summary>释放 Mesh 资源。</summary>
        public void Dispose()
        {
            if (_meshNormal != null) Object.Destroy(_meshNormal);
            if (_meshAdditive != null) Object.Destroy(_meshAdditive);
            if (_materialNormal != null) Object.Destroy(_materialNormal);
            if (_materialAdditive != null) Object.Destroy(_materialAdditive);
        }

        // ──── 内部方法 ────

        private void WriteQuad(ref BulletCore core, BulletTypeSO type, bool isAdditive, float alpha, Color tint)
        {
            RenderVertex[] verts;
            ref int quadCount = ref (isAdditive ? ref _additiveQuadCount : ref _normalQuadCount);

            verts = isAdditive ? _verticesAdditive : _verticesNormal;

            int baseVertex = quadCount * 4;
            if (baseVertex + 4 > verts.Length) return;  // 溢出保护

            float halfW = type.Size.x * 0.5f;
            float halfH = type.Size.y * 0.5f;

            // 旋转（如果启用 RotateToDirection）
            float cos = 1f, sin = 0f;
            if ((core.Flags & BulletCore.FLAG_ROTATE_TO_DIR) != 0)
            {
                float angle = Mathf.Atan2(core.Velocity.y, core.Velocity.x);
                cos = Mathf.Cos(angle);
                sin = Mathf.Sin(angle);
            }

            // 4 个角点（逆时针）
            WriteVertex(ref verts[baseVertex + 0], core.Position, -halfW, -halfH, cos, sin,
                type.AtlasUV.xMin, type.AtlasUV.yMin, tint, alpha);
            WriteVertex(ref verts[baseVertex + 1], core.Position, halfW, -halfH, cos, sin,
                type.AtlasUV.xMax, type.AtlasUV.yMin, tint, alpha);
            WriteVertex(ref verts[baseVertex + 2], core.Position, halfW, halfH, cos, sin,
                type.AtlasUV.xMax, type.AtlasUV.yMax, tint, alpha);
            WriteVertex(ref verts[baseVertex + 3], core.Position, -halfW, halfH, cos, sin,
                type.AtlasUV.xMin, type.AtlasUV.yMax, tint, alpha);

            quadCount++;
        }

        private void WriteQuadUV(ref BulletCore core, BulletTypeSO type, bool isAdditive, float alpha, Color tint, Rect uv)
        {
            RenderVertex[] verts;
            ref int quadCount = ref (isAdditive ? ref _additiveQuadCount : ref _normalQuadCount);

            verts = isAdditive ? _verticesAdditive : _verticesNormal;

            int baseVertex = quadCount * 4;
            if (baseVertex + 4 > verts.Length) return;

            float halfW = type.Size.x * 0.5f;
            float halfH = type.Size.y * 0.5f;

            // 爆炸帧不旋转
            WriteVertex(ref verts[baseVertex + 0], core.Position, -halfW, -halfH, 1, 0,
                uv.xMin, uv.yMin, tint, alpha);
            WriteVertex(ref verts[baseVertex + 1], core.Position, halfW, -halfH, 1, 0,
                uv.xMax, uv.yMin, tint, alpha);
            WriteVertex(ref verts[baseVertex + 2], core.Position, halfW, halfH, 1, 0,
                uv.xMax, uv.yMax, tint, alpha);
            WriteVertex(ref verts[baseVertex + 3], core.Position, -halfW, halfH, 1, 0,
                uv.xMin, uv.yMax, tint, alpha);

            quadCount++;
        }

        private void WriteGhostQuad(Vector2 position, BulletTypeSO type, bool isAdditive, float alpha)
        {
            RenderVertex[] verts;
            ref int quadCount = ref (isAdditive ? ref _additiveQuadCount : ref _normalQuadCount);

            verts = isAdditive ? _verticesAdditive : _verticesNormal;

            int baseVertex = quadCount * 4;
            if (baseVertex + 4 > verts.Length) return;

            float halfW = type.Size.x * 0.5f;
            float halfH = type.Size.y * 0.5f;

            // 残影不旋转
            WriteVertex(ref verts[baseVertex + 0], position, -halfW, -halfH, 1, 0,
                type.AtlasUV.xMin, type.AtlasUV.yMin, type.Tint, alpha);
            WriteVertex(ref verts[baseVertex + 1], position, halfW, -halfH, 1, 0,
                type.AtlasUV.xMax, type.AtlasUV.yMin, type.Tint, alpha);
            WriteVertex(ref verts[baseVertex + 2], position, halfW, halfH, 1, 0,
                type.AtlasUV.xMax, type.AtlasUV.yMax, type.Tint, alpha);
            WriteVertex(ref verts[baseVertex + 3], position, -halfW, halfH, 1, 0,
                type.AtlasUV.xMin, type.AtlasUV.yMax, type.Tint, alpha);

            quadCount++;
        }

        private static void WriteVertex(
            ref RenderVertex v,
            Vector2 center, float offsetX, float offsetY,
            float cos, float sin,
            float uvX, float uvY,
            Color tint, float alpha)
        {
            // 旋转偏移
            float rx = offsetX * cos - offsetY * sin;
            float ry = offsetX * sin + offsetY * cos;

            v.Position = new Vector3(center.x + rx, center.y + ry, 0f);
            v.UV = new Vector2(uvX, uvY);
            v.Color = new Color32(
                (byte)(tint.r * 255),
                (byte)(tint.g * 255),
                (byte)(tint.b * 255),
                (byte)(alpha * tint.a * 255));
        }

        private static void UploadAndDraw(Mesh mesh, RenderVertex[] vertices, int quadCount, Material material)
        {
            if (quadCount == 0 || material == null) return;

            int vertexCount = quadCount * 4;
            int indexCount = quadCount * 6;

            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0,
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);

            // 更新子网格范围（索引已预填充，只需告知绘制多少）
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles),
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

            // 设置足够大的 bounds 避免被剔除
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 1));

            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0);
        }

        private static Mesh CreateMesh(string name, int maxVertices, int maxIndices)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = maxVertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            // 设置顶点布局
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
