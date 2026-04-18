using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕渲染配置——材质、贴图引用。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Danmaku/Config/Render")]
    public class DanmakuRenderConfig : ScriptableObject
    {
        [Header("材质")]
        [Tooltip("弹丸 Alpha Blend 材质")]
        public Material BulletMaterial;

        [Tooltip("激光材质")]
        public Material LaserMaterial;

        [Header("贴图")]
        [Tooltip("弹丸图集（规则网格布局）")]
        public Texture2D BulletAtlas;

        [Tooltip("数字精灵图集（0-9 飘字用）")]
        public Texture2D NumberAtlas;
    }
}
