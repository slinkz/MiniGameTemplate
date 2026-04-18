using System;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 单个 Channel 的配置。
    /// </summary>
    [Serializable]
    public struct AtlasChannelConfig
    {
        [Tooltip("Atlas 页面尺寸（像素，正方形）")]
        public int AtlasSize;

        [Tooltip("子图之间的 Padding（像素）")]
        public int Padding;

        [Tooltip("最大页面数（超过则拒绝分配并报警告）")]
        public int MaxPages;

        public static AtlasChannelConfig Default => new AtlasChannelConfig
        {
            AtlasSize = 2048,
            Padding = 1,
            MaxPages = 4,
        };

        public static AtlasChannelConfig Small => new AtlasChannelConfig
        {
            AtlasSize = 1024,
            Padding = 1,
            MaxPages = 1,
        };

        public void Validate(string ownerName)
        {
            if (AtlasSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(AtlasSize), $"[{ownerName}] AtlasSize 必须 > 0");
            if (Padding < 0)
                throw new ArgumentOutOfRangeException(nameof(Padding), $"[{ownerName}] Padding 不能 < 0");
            if (MaxPages <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxPages), $"[{ownerName}] MaxPages 必须 > 0");
        }
    }
}
