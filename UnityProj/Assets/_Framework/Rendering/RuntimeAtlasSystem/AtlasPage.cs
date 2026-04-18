using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// 单张 Atlas 页面的状态。
    /// </summary>
    internal sealed class AtlasPage : IDisposable
    {
        public RenderTexture Texture;
        public readonly List<Shelf> Shelves = new List<Shelf>(8);
        public int NextShelfY;
        public bool IsDirty;

        public AtlasPage(RenderTexture texture)
        {
            Texture = texture;
        }

        public void Dispose()
        {
            if (Texture != null)
            {
                Texture.Release();
                UnityEngine.Object.Destroy(Texture);
                Texture = null;
            }

            Shelves.Clear();
            NextShelfY = 0;
            IsDirty = false;
        }
    }
}
