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
        [Header("Bullet Channel")]
        public AtlasChannelConfig Bullet = AtlasChannelConfig.Default;

        [Header("VFX Channel")]
        public AtlasChannelConfig VFX = AtlasChannelConfig.Default;

        [Header("DamageText Channel")]
        public AtlasChannelConfig DamageText = AtlasChannelConfig.Small;

        [Header("Laser Channel")]
        public AtlasChannelConfig Laser = AtlasChannelConfig.Small;

        [Header("Trail Channel")]
        public AtlasChannelConfig Trail = AtlasChannelConfig.Small;

        [Header("Character Channel")]
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

        public void Validate()
        {
            Bullet.Validate(nameof(Bullet));
            VFX.Validate(nameof(VFX));
            DamageText.Validate(nameof(DamageText));
            Laser.Validate(nameof(Laser));
            Trail.Validate(nameof(Trail));
            Character.Validate(nameof(Character));
        }
    }
}
