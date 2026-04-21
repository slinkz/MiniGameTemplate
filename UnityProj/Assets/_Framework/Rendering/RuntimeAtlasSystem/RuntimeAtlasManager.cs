using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// RuntimeAtlas 核心入口。
    /// R1：补齐配置驱动初始化、WarmUp 结果输出、Stats 快照与 RT Lost 标记恢复。
    /// R4.4A：懒建页——Channel 初始化时不创建 Page 0，延迟到首次 Allocate 时按需创建。
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
            public int TotalRequests;
            public int OverflowCount;
            public bool PendingRestore;
        }

        private readonly Dictionary<AtlasChannel, AtlasChannelState> _channels = new Dictionary<AtlasChannel, AtlasChannelState>(8);
        private RuntimeAtlasConfig _config;
        private bool _initialized;

        public bool IsInitialized => _initialized;
        public RuntimeAtlasConfig Config => _config;

        public void Initialize(RuntimeAtlasConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.Validate();
            Reset();
            _config = config;

            Array channels = Enum.GetValues(typeof(AtlasChannel));
            for (int i = 0; i < channels.Length; i++)
            {
                AtlasChannel channel = (AtlasChannel)channels.GetValue(i);
                InitChannel(channel, config.GetChannelConfig(channel));
            }

            _initialized = true;
        }

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

            // R4.4A：懒建页——Page 0 延迟到首次 Allocate 时创建，节省未使用 Channel 的 RT 内存
            _channels[channel] = state;
        }

        public AtlasAllocation Allocate(AtlasChannel channel, Texture2D source)
        {
            if (source == null)
                return AtlasAllocation.Invalid;

            AtlasChannelState state = GetRequiredState(channel);
            state.TotalRequests++;

            int instanceId = source.GetInstanceID();
            if (state.AllocationCache.TryGetValue(instanceId, out AtlasAllocation cached) && cached.Valid)
            {
                state.CacheHits++;
                return cached;
            }

            if (!CanFitInAtlas(source, state.Config))
            {
                Debug.LogWarning($"[RuntimeAtlas] 纹理 {source.name} ({source.width}x{source.height}) 超过 Channel={channel} 的 AtlasSize={state.Config.AtlasSize}，回退独立贴图。");
                state.OverflowCount++;
                return AtlasAllocation.Invalid;
            }

            AtlasAllocation allocation = TryAllocateInternal(channel, state, source);
            if (allocation.Valid)
                return allocation;

            state.OverflowCount++;
            return AtlasAllocation.Invalid;
        }

        public RenderTexture GetAtlasTexture(AtlasChannel channel, int pageIndex)
        {
            if (!_channels.TryGetValue(channel, out AtlasChannelState state))
                return null;

            if (pageIndex < 0 || pageIndex >= state.Pages.Count)
                return null;

            return state.Pages[pageIndex].Texture;
        }

        public AtlasAllocation TryGetAllocation(AtlasChannel channel, Texture2D source)
        {
            if (source == null)
                return AtlasAllocation.Invalid;

            if (!_channels.TryGetValue(channel, out AtlasChannelState state))
                return AtlasAllocation.Invalid;

            return state.AllocationCache.TryGetValue(source.GetInstanceID(), out AtlasAllocation allocation)
                ? allocation
                : AtlasAllocation.Invalid;
        }

        public int GetPageCount(AtlasChannel channel)
        {
            return _channels.TryGetValue(channel, out AtlasChannelState state)
                ? state.Pages.Count
                : 0;
        }

        public int WarmUp(AtlasChannel channel, IReadOnlyList<Texture2D> sources)
        {
            if (sources == null || sources.Count == 0)
                return 0;

            AtlasChannelState state = GetRequiredState(channel);
            int blitBefore = state.BlitCount;

            for (int i = 0; i < sources.Count; i++)
            {
                Allocate(channel, sources[i]);
            }

            return state.BlitCount - blitBefore;
        }

        public void HandleRTLost()
        {
            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                AtlasChannelState state = pair.Value;
                state.PendingRestore = true;

                for (int i = 0; i < state.Pages.Count; i++)
                {
                    state.Pages[i].IsDirty = true;
                }
            }
        }

        public void RestoreDirtyPages()
        {
            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                AtlasChannel channel = pair.Key;
                AtlasChannelState state = pair.Value;
                if (!state.PendingRestore)
                    continue;

                RebuildChannel(channel, state);
            }
        }

        public void RestoreDirtyPages(int maxTexturesPerChannel)
        {
            if (maxTexturesPerChannel <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTexturesPerChannel), "maxTexturesPerChannel 必须 > 0");

            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                AtlasChannel channel = pair.Key;
                AtlasChannelState state = pair.Value;
                if (!state.PendingRestore)
                    continue;

                RebuildChannel(channel, state, maxTexturesPerChannel);
            }
        }

        public RuntimeAtlasStats GetStats()
        {
            int channelCount = Enum.GetValues(typeof(AtlasChannel)).Length;
            int[] pageCount = new int[channelCount];
            int[] allocationCount = new int[channelCount];
            float[] fillRate = new float[channelCount];

            long totalMemoryBytes = 0;
            int totalRequests = 0;
            int cacheHits = 0;
            int blitCount = 0;
            int overflowCount = 0;
            int pendingRestoreChannels = 0;

            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                int index = (int)pair.Key;
                AtlasChannelState state = pair.Value;
                pageCount[index] = state.Pages.Count;
                allocationCount[index] = state.AllocationCache.Count;
                totalRequests += state.TotalRequests;
                cacheHits += state.CacheHits;
                blitCount += state.BlitCount;
                overflowCount += state.OverflowCount;
                if (state.PendingRestore)
                    pendingRestoreChannels++;

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

                // R4.4A (PI-003): Pages.Count=0 时 totalPixels=0，fillRate=0（语义精确）
                long totalPixels = (long)state.Config.AtlasSize * state.Config.AtlasSize * state.Pages.Count;
                fillRate[index] = totalPixels > 0 ? (float)usedPixels / totalPixels : 0f;
                totalMemoryBytes += (long)state.Config.AtlasSize * state.Config.AtlasSize * 4L * state.Pages.Count;
            }

            return new RuntimeAtlasStats(pageCount, allocationCount, fillRate, totalMemoryBytes, totalRequests, cacheHits, blitCount, overflowCount, pendingRestoreChannels);
        }

        public void Reset()
        {
            foreach (KeyValuePair<AtlasChannel, AtlasChannelState> pair in _channels)
            {
                ReleaseState(pair.Value);
            }

            _channels.Clear();
            _initialized = false;
        }

        public void Dispose()
        {
            Reset();
            _config = null;
            AtlasBlit.Dispose();
        }

        private AtlasAllocation TryAllocateInternal(AtlasChannel channel, AtlasChannelState state, Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            AtlasChannelConfig config = state.Config;

            // R4.4A：懒建页——首次分配时创建 Page 0
            if (state.Pages.Count == 0)
            {
                state.Pages.Add(CreatePage(config.AtlasSize, channel, 0));
            }

            for (int pageIndex = 0; pageIndex < state.Pages.Count; pageIndex++)
            {
                if (!ShelfPacker.TryAllocate(state.Pages[pageIndex], config.AtlasSize, width, height, config.Padding, out RectInt pixelRect))
                    continue;

                return CommitAllocation(state, source, pageIndex, pixelRect, config.AtlasSize);
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

            return CommitAllocation(state, source, newPageIndex, newPixelRect, config.AtlasSize);
        }

        private AtlasAllocation CommitAllocation(AtlasChannelState state, Texture2D source, int pageIndex, RectInt pixelRect, int atlasSize)
        {
            AtlasPage page = state.Pages[pageIndex];
            if (!AtlasBlit.Blit(source, page.Texture, pixelRect))
                return AtlasAllocation.Invalid;

            page.IsDirty = false;
            state.BlitCount++;

            AtlasAllocation allocation = new AtlasAllocation(pageIndex, ToUvRect(pixelRect, atlasSize));
            int instanceId = source.GetInstanceID();
            state.AllocationCache[instanceId] = allocation;
            state.SourceTextures[instanceId] = source;
            return allocation;
        }

        private void RebuildChannel(AtlasChannel channel, AtlasChannelState state)
        {
            RebuildChannel(channel, state, int.MaxValue);
        }

        private void RebuildChannel(AtlasChannel channel, AtlasChannelState state, int maxTexturesToRestore)
        {
            AtlasChannelConfig config = state.Config;
            List<Texture2D> sources = new List<Texture2D>(state.SourceTextures.Values);
            int totalRequests = state.TotalRequests;
            int cacheHits = state.CacheHits;
            int overflowCount = state.OverflowCount;

            ReleasePages(state);
            state.AllocationCache.Clear();
            state.SourceTextures.Clear();
            state.BlitCount = 0;
            state.PendingRestore = false;
            state.TotalRequests = totalRequests;
            state.CacheHits = cacheHits;
            state.OverflowCount = overflowCount;

            int restoreCount = Math.Min(maxTexturesToRestore, sources.Count);
            for (int i = 0; i < restoreCount; i++)
            {
                Texture2D source = sources[i];
                if (source == null)
                    continue;

                TryAllocateInternal(channel, state, source);
            }

            for (int i = restoreCount; i < sources.Count; i++)
            {
                Texture2D source = sources[i];
                if (source == null)
                    continue;

                state.SourceTextures[source.GetInstanceID()] = source;
                state.PendingRestore = true;
            }
        }

        private AtlasChannelState GetRequiredState(AtlasChannel channel)
        {
            if (!_channels.TryGetValue(channel, out AtlasChannelState state))
                throw new InvalidOperationException($"[RuntimeAtlas] Channel {channel} 尚未初始化");

            return state;
        }

        private static bool CanFitInAtlas(Texture2D source, AtlasChannelConfig config)
        {
            int paddedWidth = source.width + config.Padding * 2;
            int paddedHeight = source.height + config.Padding * 2;
            return paddedWidth <= config.AtlasSize && paddedHeight <= config.AtlasSize;
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
            ReleasePages(state);
            state.AllocationCache.Clear();
            state.SourceTextures.Clear();
            state.BlitCount = 0;
            state.CacheHits = 0;
            state.TotalRequests = 0;
            state.OverflowCount = 0;
            state.PendingRestore = false;
        }

        private static void ReleasePages(AtlasChannelState state)
        {
            for (int i = 0; i < state.Pages.Count; i++)
            {
                state.Pages[i].Dispose();
            }

            state.Pages.Clear();
        }
    }
}
