using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// Atlas 通道枚举——不同业务域的 Atlas 物理隔离。
    /// </summary>
    public enum AtlasChannel : byte
    {
        Bullet = 0,
        VFX = 1,
        DamageText = 2,
        Laser = 3,
        Trail = 4,
        Character = 5,
    }
}
