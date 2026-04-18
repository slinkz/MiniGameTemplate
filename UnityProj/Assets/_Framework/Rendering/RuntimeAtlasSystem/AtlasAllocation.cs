using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// Atlas 分配结果——业务层拿到这个就够了。
    /// </summary>
    public readonly struct AtlasAllocation
    {
        public readonly int PageIndex;
        public readonly Rect UVRect;
        public readonly bool Valid;

        public AtlasAllocation(int pageIndex, Rect uvRect)
        {
            PageIndex = pageIndex;
            UVRect = uvRect;
            Valid = true;
        }

        public static AtlasAllocation Invalid => default;
    }
}
