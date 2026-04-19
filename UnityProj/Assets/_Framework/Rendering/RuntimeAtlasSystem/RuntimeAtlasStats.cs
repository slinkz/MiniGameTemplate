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
        public readonly int TotalRequests;
        public readonly int CacheHits;
        public readonly int BlitCount;
        public readonly int OverflowCount;
        public readonly int PendingRestoreChannels;

        public RuntimeAtlasStats(
            int[] pageCountPerChannel,
            int[] allocationCountPerChannel,
            float[] fillRatePerChannel,
            long totalMemoryBytes,
            int totalRequests,
            int cacheHits,
            int blitCount,
            int overflowCount,
            int pendingRestoreChannels)
        {
            PageCountPerChannel = pageCountPerChannel;
            AllocationCountPerChannel = allocationCountPerChannel;
            FillRatePerChannel = fillRatePerChannel;
            TotalMemoryBytes = totalMemoryBytes;
            TotalRequests = totalRequests;
            CacheHits = cacheHits;
            BlitCount = blitCount;
            OverflowCount = overflowCount;
            PendingRestoreChannels = pendingRestoreChannels;
        }

        public float CacheHitRate => TotalRequests > 0 ? (float)CacheHits / TotalRequests : 0f;
    }
}
