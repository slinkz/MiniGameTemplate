using System;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// RuntimeAtlas 全局配置 SO。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Rendering/Runtime Atlas Config")]
    public sealed class RuntimeAtlasConfig : ScriptableObject
    {
        public AtlasChannelConfig Bullet = AtlasChannelConfig.Default;
        public AtlasChannelConfig VFX = AtlasChannelConfig.Default;
        public AtlasChannelConfig DamageText = AtlasChannelConfig.Small;
        public AtlasChannelConfig Laser = AtlasChannelConfig.Small;
        public AtlasChannelConfig Trail = AtlasChannelConfig.Small;
        public AtlasChannelConfig Character = AtlasChannelConfig.Default;

        public AtlasChannelConfig GetChannelConfig(AtlasChannel channel)
        {
            switch (channel)
            {
                case AtlasChannel.Bullet: return Bullet;
                case AtlasChannel.VFX: return VFX;
                case AtlasChannel.DamageText: return DamageText;
                case AtlasChannel.Laser: return Laser;
                case AtlasChannel.Trail: return Trail;
                case AtlasChannel.Character: return Character;
                default: throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }
    }
}
