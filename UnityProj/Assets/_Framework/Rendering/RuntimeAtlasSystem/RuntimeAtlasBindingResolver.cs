using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// R2：运行时纹理解算辅助层。
    /// 负责把 SourceTexture / AtlasBinding / RuntimeAtlas 三套来源收敛成统一结果，
    /// 让业务渲染器只关心“拿到哪张贴图、用哪段 UV”。
    /// </summary>
    public static class RuntimeAtlasBindingResolver
    {
        public readonly struct ResolvedTextureBinding
        {
            public readonly Texture Texture;
            public readonly Rect UVRect;
            public readonly bool UsesRuntimeAtlas;

            public ResolvedTextureBinding(Texture texture, Rect uvRect, bool usesRuntimeAtlas)
            {
                Texture = texture;
                UVRect = uvRect;
                UsesRuntimeAtlas = usesRuntimeAtlas;
            }

            public bool IsValid => Texture != null;
        }

        public static ResolvedTextureBinding ResolveBullet(RuntimeAtlasManager atlasManager, Texture2D fallbackTexture, MiniGameTemplate.Danmaku.BulletTypeSO type)
        {
            if (type == null)
                return default;

            return ResolveCommon(atlasManager, AtlasChannel.Bullet, type.SourceTexture, type.AtlasBinding, type.UVRect, fallbackTexture);
        }

        public static ResolvedTextureBinding ResolveVFX(RuntimeAtlasManager atlasManager, Texture2D fallbackTexture, MiniGameTemplate.VFX.VFXTypeSO type)
        {
            if (type == null)
                return default;

            return ResolveCommon(atlasManager, AtlasChannel.VFX, type.SourceTexture, type.AtlasBinding, type.UVRect, fallbackTexture);
        }

        private static ResolvedTextureBinding ResolveCommon(RuntimeAtlasManager atlasManager,
            AtlasChannel channel,
            Texture2D sourceTexture,
            AtlasMappingSO atlasBinding,
            Rect sourceUV,
            Texture2D fallbackTexture)
        {
            if (atlasManager != null && atlasManager.IsInitialized && sourceTexture != null)
            {
                AtlasAllocation allocation = atlasManager.Allocate(channel, sourceTexture);
                if (allocation.Valid)
                {
                    RenderTexture atlasTexture = atlasManager.GetAtlasTexture(channel, allocation.PageIndex);
                    if (atlasTexture != null)
                        return new ResolvedTextureBinding(atlasTexture, RemapUv(sourceUV, allocation.UVRect), true);
                }
            }

            if (atlasBinding != null && atlasBinding.AtlasTexture != null && sourceTexture != null && atlasBinding.TryFindEntry(sourceTexture, out var entry))
                return new ResolvedTextureBinding(atlasBinding.AtlasTexture, entry.UVRect, false);

            if (sourceTexture != null)
                return new ResolvedTextureBinding(sourceTexture, sourceUV, false);

            if (fallbackTexture != null)
                return new ResolvedTextureBinding(fallbackTexture, new Rect(0f, 0f, 1f, 1f), false);

            return default;
        }

        private static Rect RemapUv(Rect localUv, Rect atlasUv)
        {
            return new Rect(
                atlasUv.x + localUv.x * atlasUv.width,
                atlasUv.y + localUv.y * atlasUv.height,
                localUv.width * atlasUv.width,
                localUv.height * atlasUv.height);
        }
    }
}
