namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// RuntimeAtlas 统计信息。
    /// </summary>
    public readonly struct RuntimeAtlasStats
    {
        public readonly int[] PageCountPerChannel;
        public readonly int[] AllocationCountPerChannel;
        public readonly float[] FillRatePerChannel;
        public readonly long TotalMemoryBytes;
        public readonly int TotalAllocations;
        public readonly int CacheHits;
        public readonly int BlitCount;

        public RuntimeAtlasStats(
            int[] pageCountPerChannel,
            int[] allocationCountPerChannel,
            float[] fillRatePerChannel,
            long totalMemoryBytes,
            int totalAllocations,
            int cacheHits,
            int blitCount)
        {
            PageCountPerChannel = pageCountPerChannel;
            AllocationCountPerChannel = allocationCountPerChannel;
            FillRatePerChannel = fillRatePerChannel;
            TotalMemoryBytes = totalMemoryBytes;
            TotalAllocations = totalAllocations;
            CacheHits = cacheHits;
            BlitCount = blitCount;
        }
    }
}
