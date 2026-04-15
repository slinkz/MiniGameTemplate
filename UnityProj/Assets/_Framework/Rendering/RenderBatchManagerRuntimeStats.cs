namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// Shared runtime stats snapshot for debug HUD and acceptance checks.
    /// </summary>
    public static class RenderBatchManagerRuntimeStats
    {
        public static int LastSubmittedDrawCalls { get; internal set; }
        public static int LastActiveBatchCount { get; internal set; }
        public static int LastUnknownBucketErrorCount { get; internal set; }
        public static int PeakSubmittedDrawCalls { get; internal set; }
        public static int PeakActiveBatchCount { get; internal set; }
        public static float AverageSubmittedDrawCalls { get; internal set; }
        public static float AverageActiveBatchCount { get; internal set; }
        public static int SampleCount { get; internal set; }

        public static void RecordFrame(int drawCalls, int activeBatchCount, int unknownBucketErrorCount)
        {
            LastSubmittedDrawCalls = drawCalls;
            LastActiveBatchCount = activeBatchCount;
            LastUnknownBucketErrorCount = unknownBucketErrorCount;

            if (drawCalls > PeakSubmittedDrawCalls)
                PeakSubmittedDrawCalls = drawCalls;

            if (activeBatchCount > PeakActiveBatchCount)
                PeakActiveBatchCount = activeBatchCount;

            SampleCount++;
            AverageSubmittedDrawCalls += (drawCalls - AverageSubmittedDrawCalls) / SampleCount;
            AverageActiveBatchCount += (activeBatchCount - AverageActiveBatchCount) / SampleCount;
        }

        public static void Reset()
        {
            LastSubmittedDrawCalls = 0;
            LastActiveBatchCount = 0;
            LastUnknownBucketErrorCount = 0;
            PeakSubmittedDrawCalls = 0;
            PeakActiveBatchCount = 0;
            AverageSubmittedDrawCalls = 0f;
            AverageActiveBatchCount = 0f;
            SampleCount = 0;
        }
    }
}
