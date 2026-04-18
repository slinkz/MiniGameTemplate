using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// RuntimeAtlas 核心入口。
    /// </summary>
    public sealed class RuntimeAtlasManager : IDisposable
    {
        private sealed class AtlasChannelState
        {
            public AtlasChannelConfig Config;
            public readonly List<AtlasPage> Pages = new List<AtlasPage>(4);
            public readonly Dictionary<int, AtlasAllocation> AllocationCache = new Dictionary<int, AtlasAllocation>(128);
            public readonly Dictionary<int, Texture2D> SourceTextures = new Dictionary<int, Texture2D>(128);
            public int BlitCount;
            public int CacheHits;
            public int TotalAllocations;
        }

        private readonly Dictionary<AtlasChannel, AtlasChannelState> _channels = new Dictionary<AtlasChannel, AtlasChannelState>(8);

        public void InitChannel(AtlasChannel channel, AtlasChannelConfig config)
        {
            config.Validate(channel.ToString());

            if (_channels.TryGetValue(channel, out AtlasChannelState existing))
            {
                ReleaseState(existing);
            }

            AtlasChannelState state = new AtlasChannelState
            {
                Config = config,
            };

            state.Pages.Add(CreatePage(config.AtlasSize, channel, 0));
            _channels[channel] = state;
        }

        public AtlasAllocation Allocate(AtlasChannel channel, Texture2D source)
        {
            if (source == null)
                return AtlasAllocation.Invalid;

            if (!_channels.TryGetValue(channel, out AtlasChannelState state))
                throw new InvalidOperationException($"[RuntimeAtlas] Channel {channel} 尚未初始化");

            int instanceId = source.GetInstanceID();
            state.TotalAllocations++;

            if (state.AllocationCache.TryGetValue(instanceId, out AtlasAllocation cached) && cached.Valid)
            {
                state.CacheHits++;
                return cached;
            }

            int width = source.width;
            int height = source.height;
            AtlasChannelConfig config = state.Config;

            for (int pageIndex = 0; pageIndex < state.Pages.Count; pageIndex++)
            {
                if (ShelfPacker.TryAllocate(state.Pages[pageIndex], config.AtlasSize, width, height, config.Padding, out RectInt pixelRect))
                {
                    if (!AtlasBlit.Blit(source, state.Pages[pageIndex].Texture, pixelRect))
                        return AtlasAllocation.Invalid;

                    state.Pages[pageIndex].IsDirty = false;
                    state.BlitCount++;

                    AtlasAllocation allocation = new AtlasAllocation(pageIndex, ToUvRect(pixelRect, config.AtlasSize));
                    state.AllocationCache[instanceId] = allocation;
                    state.SourceTextures[instanceId] = source;
                    return allocation;
                }
            }

            if (state.Pages.Count >= config.MaxPages)
            {
                Debug.LogWarning($"[RuntimeAtlas] Channel {channel} 已达到 MaxPages={config.MaxPages}，拒绝分配 {source.name}");
                return AtlasAllocation.Invalid;
            }

            int newPageIndex = state.Pages.Count;
            AtlasPage newPage = CreatePage(config.AtlasSize, channel, newPageIndex);
            state.Pages.Add(newPage);

            if (!ShelfPacker.TryAllocate(newPage, config.AtlasSize, width, height, config.Padding, out RectInt newPixelRect))
            {
                Debug.LogError($"[RuntimeAtlas] 新页面仍无法容纳纹理 {source.name} ({width}x{height})");
                return AtlasAllocation.Invalid;
            }

            if (!AtlasBlit.Blit(source, newPage.Texture, newPixelRect))
                return AtlasAllocation.Invalid;

            state.BlitCount++;

            AtlasAllocation newAllocation = new AtlasAllocation(newPageIndex, ToUvRect(newPixelRect, config.AtlasSize));
            state.AllocationCache[instanceId] = newAllocation;
            state.SourceTextures[instanceId] = source;
            return newAllocation;
        }

        public RenderTexture GetAtlasTexture(AtlasChannel channel, int pageIndex)
        {
            if (!_channels.TryGetValue(channel, out AtlasChannelState state))
                return null;

            if (pageIndex < 0 || pageIndex >= state.Pages.Count)
                return null;

            return state.Pages[pageIndex].Texture;
        }

        public void WarmUp(AtlasChannel channel, IReadOnlyList<Texture2D> sources)
        {
            if (sources == null)
                return;

            for (int i = 0; i < sources.Count; i++)
            {
                Allocate(channel, sources[i]);
            }
        }

        public void HandleRTLost()
        {
            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                AtlasChannel channel = pair.Key;
                AtlasChannelState state = pair.Value;
                AtlasChannelConfig config = state.Config;

                List<Texture2D> sources = new List<Texture2D>(state.SourceTextures.Values);
                ReleaseState(state);
                state.Pages.Add(CreatePage(config.AtlasSize, channel, 0));
                state.AllocationCache.Clear();
                state.SourceTextures.Clear();
                state.BlitCount = 0;
                state.CacheHits = 0;
                state.TotalAllocations = 0;

                for (int i = 0; i < sources.Count; i++)
                {
                    if (sources[i] != null)
                        Allocate(channel, sources[i]);
                }
            }
        }

        public RuntimeAtlasStats GetStats()
        {
            int channelCount = Enum.GetValues(typeof(AtlasChannel)).Length;
            int[] pageCount = new int[channelCount];
            int[] allocationCount = new int[channelCount];
            float[] fillRate = new float[channelCount];

            long totalMemoryBytes = 0;
            int totalAllocations = 0;
            int cacheHits = 0;
            int blitCount = 0;

            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                int index = (int)pair.Key;
                AtlasChannelState state = pair.Value;
                pageCount[index] = state.Pages.Count;
                allocationCount[index] = state.AllocationCache.Count;
                totalAllocations += state.TotalAllocations;
                cacheHits += state.CacheHits;
                blitCount += state.BlitCount;

                long usedPixels = 0;
                for (int i = 0; i < state.Pages.Count; i++)
                {
                    AtlasPage page = state.Pages[i];
                    for (int s = 0; s < page.Shelves.Count; s++)
                    {
                        Shelf shelf = page.Shelves[s];
                        usedPixels += (long)shelf.Height * shelf.UsedWidth;
                    }
                }

                long totalPixels = (long)state.Config.AtlasSize * state.Config.AtlasSize * Math.Max(1, state.Pages.Count);
                fillRate[index] = totalPixels > 0 ? (float)usedPixels / totalPixels : 0f;
                totalMemoryBytes += (long)state.Config.AtlasSize * state.Config.AtlasSize * 4L * state.Pages.Count;
            }

            return new RuntimeAtlasStats(pageCount, allocationCount, fillRate, totalMemoryBytes, totalAllocations, cacheHits, blitCount);
        }

        public void Reset()
        {
            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                ReleaseState(pair.Value);
            }

            _channels.Clear();
        }

        public void Dispose()
        {
            Reset();
            AtlasBlit.Dispose();
        }

        private static Rect ToUvRect(RectInt pixelRect, int atlasSize)
        {
            float inv = 1f / atlasSize;
            return new Rect(pixelRect.x * inv, pixelRect.y * inv, pixelRect.width * inv, pixelRect.height * inv);
        }

        private static AtlasPage CreatePage(int atlasSize, AtlasChannel channel, int pageIndex)
        {
            RenderTexture rt = new RenderTexture(atlasSize, atlasSize, 0, RenderTextureFormat.ARGB32)
            {
                name = $"RuntimeAtlas_{channel}_{pageIndex}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
            };
            rt.Create();
            return new AtlasPage(rt);
        }

        private static void ReleaseState(AtlasChannelState state)
        {
            for (int i = 0; i < state.Pages.Count; i++)
            {
                state.Pages[i].Dispose();
            }

            state.Pages.Clear();
            state.AllocationCache.Clear();
            state.SourceTextures.Clear();
        }
    }
}
