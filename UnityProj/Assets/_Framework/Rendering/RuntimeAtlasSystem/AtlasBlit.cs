using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 将源纹理 Blit 到 Atlas RT 的指定像素区域。
    /// 使用 CommandBuffer + viewport 方式，兼容 WebGL 2.0。
    /// </summary>
    internal static class AtlasBlit
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private static Material _blitMaterial;
        private static CommandBuffer _commandBuffer;
        private static Mesh _fullscreenQuad;

        public static bool Blit(Texture source, RenderTexture atlasRT, RectInt destPixelRect)
        {
            if (source == null || atlasRT == null)
                return false;

            if (!EnsureResources())
                return false;

            _blitMaterial.SetTexture(MainTexId, source);

            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(atlasRT);
            _commandBuffer.SetViewport(new Rect(destPixelRect.x, destPixelRect.y, destPixelRect.width, destPixelRect.height));
            _commandBuffer.DrawMesh(_fullscreenQuad, Matrix4x4.identity, _blitMaterial, 0, 0);
            Graphics.ExecuteCommandBuffer(_commandBuffer);
            return true;
        }

        public static void Dispose()
        {
            if (_commandBuffer != null)
            {
                _commandBuffer.Release();
                _commandBuffer = null;
            }

            if (_blitMaterial != null)
            {
                Object.Destroy(_blitMaterial);
                _blitMaterial = null;
            }

            if (_fullscreenQuad != null)
            {
                Object.Destroy(_fullscreenQuad);
                _fullscreenQuad = null;
            }
        }

        private static bool EnsureResources()
        {
            if (_blitMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/RuntimeAtlasBlit");
                if (shader == null)
                {
                    Debug.LogError("[RuntimeAtlas] Missing shader: Hidden/RuntimeAtlasBlit");
                    return false;
                }

                _blitMaterial = new Material(shader)
                {
                    name = "RuntimeAtlasBlit (Instance)",
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }

            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer
                {
                    name = "RuntimeAtlasBlit",
                };
            }

            if (_fullscreenQuad == null)
            {
                _fullscreenQuad = BuildFullscreenQuad();
            }

            return _blitMaterial != null && _commandBuffer != null && _fullscreenQuad != null;
        }

        private static Mesh BuildFullscreenQuad()
        {
            Mesh mesh = new Mesh { name = "RuntimeAtlasBlitQuad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3( 1f, -1f, 0f),
                new Vector3( 1f,  1f, 0f),
                new Vector3(-1f,  1f, 0f),
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 2, 3, 0 }, 0, false);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
