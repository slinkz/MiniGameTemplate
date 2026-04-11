using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// VFX 渲染配置。
    /// 阶段 1 采用单图集方案：所有 VFXTypeSO 的 UV 都引用同一张 Atlas。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/VFX/Render Config")]
    public class VFXRenderConfig : ScriptableObject
    {
        [Header("材质")]
        public Material NormalMaterial;
        public Material AdditiveMaterial;

        [Header("贴图")]
        [Tooltip("阶段 1 共用图集。所有 VFXTypeSO 的 AtlasUV 都基于这张贴图。")]
        public Texture2D AtlasTexture;
    }
}
