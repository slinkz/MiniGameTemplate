using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// Shelf Packing 算法（Best-Fit Shelf）。
    /// </summary>
    internal static class ShelfPacker
    {
        public static bool TryAllocate(AtlasPage page, int atlasSize, int width, int height, int padding, out RectInt pixelRect)
        {
            int paddedW = width + padding * 2;
            int paddedH = height + padding * 2;

            int bestShelfIndex = -1;
            int bestWaste = int.MaxValue;

            for (int i = 0; i < page.Shelves.Count; i++)
            {
                Shelf shelf = page.Shelves[i];
                if (shelf.Height < paddedH)
                    continue;

                if ((atlasSize - shelf.UsedWidth) < paddedW)
                    continue;

                int waste = shelf.Height - paddedH;
                if (waste < bestWaste)
                {
                    bestWaste = waste;
                    bestShelfIndex = i;
                }
            }

            if (bestShelfIndex >= 0)
            {
                Shelf shelf = page.Shelves[bestShelfIndex];
                pixelRect = new RectInt(shelf.UsedWidth + padding, shelf.Y + padding, width, height);
                shelf.UsedWidth += paddedW;
                page.Shelves[bestShelfIndex] = shelf;
                return true;
            }

            if (page.NextShelfY + paddedH <= atlasSize)
            {
                Shelf newShelf = new Shelf
                {
                    Y = page.NextShelfY,
                    Height = paddedH,
                    UsedWidth = paddedW,
                };

                page.Shelves.Add(newShelf);
                pixelRect = new RectInt(padding, page.NextShelfY + padding, width, height);
                page.NextShelfY += paddedH;
                return true;
            }

            pixelRect = default;
            return false;
        }
    }
}
