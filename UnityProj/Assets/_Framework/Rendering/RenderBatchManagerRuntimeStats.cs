namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// Shared runtime stats snapshot for debug HUD and acceptance checks.
    /// <para>
    /// 使用协议（每帧）：
    /// 1. 帧开始前调 <see cref="BeginFrame"/> 归零帧内累加器
    /// 2. 每个 RenderBatchManager 实例调 <see cref="AccumulateBatch"/> 累加本批次统计
    /// 3. 帧结束后调 <see cref="EndFrame"/> 写入 Last / Peak / Average
    /// </para>
    /// </summary>
    public static class RenderBatchManagerRuntimeStats
    {
        // ── 对外只读快照（Debug HUD 读取） ──
        public static int LastSubmittedDrawCalls { get; internal set; }
        public static int LastActiveBatchCount { get; internal set; }
        public static int LastUnknownBucketErrorCount { get; internal set; }
        public static int PeakSubmittedDrawCalls { get; internal set; }
        public static int PeakActiveBatchCount { get; internal set; }
        public static float AverageSubmittedDrawCalls { get; internal set; }
        public static float AverageActiveBatchCount { get; internal set; }
        public static int SampleCount { get; internal set; }

        // ── 帧内累加器（仅内部使用） ──
        private static int _frameDrawCalls;
        private static int _frameActiveBatches;
        private static int _frameUnknownBucketErrors;

        /// <summary>
        /// 帧开始时调用——归零帧内累加器。
        /// </summary>
        public static void BeginFrame()
        {
            _frameDrawCalls = 0;
            _frameActiveBatches = 0;
            _frameUnknownBucketErrors = 0;
        }

        /// <summary>
        /// 每个 RenderBatchManager 实例在 UploadAndDrawAll 后调用——累加本批次统计。
        /// </summary>
        public static void AccumulateBatch(int drawCalls, int activeBatchCount, int unknownBucketErrorCount)
        {
            _frameDrawCalls += drawCalls;
            _frameActiveBatches += activeBatchCount;
            _frameUnknownBucketErrors += unknownBucketErrorCount;
        }

        /// <summary>
        /// 帧结束时调用——将帧内累加结果写入 Last / Peak / Average。
        /// </summary>
        public static void EndFrame()
        {
            LastSubmittedDrawCalls = _frameDrawCalls;
            LastActiveBatchCount = _frameActiveBatches;
            LastUnknownBucketErrorCount = _frameUnknownBucketErrors;

            if (_frameDrawCalls > PeakSubmittedDrawCalls)
                PeakSubmittedDrawCalls = _frameDrawCalls;

            if (_frameActiveBatches > PeakActiveBatchCount)
                PeakActiveBatchCount = _frameActiveBatches;

            SampleCount++;
            AverageSubmittedDrawCalls += (_frameDrawCalls - AverageSubmittedDrawCalls) / SampleCount;
            AverageActiveBatchCount += (_frameActiveBatches - AverageActiveBatchCount) / SampleCount;
        }

        /// <summary>
        /// [向后兼容] 单次调用版——等价于 BeginFrame + AccumulateBatch + EndFrame。
        /// 仅用于只有单个 RenderBatchManager 的场景（如 VFX 系统）。
        /// </summary>
        public static void RecordFrame(int drawCalls, int activeBatchCount, int unknownBucketErrorCount)
        {
            BeginFrame();
            AccumulateBatch(drawCalls, activeBatchCount, unknownBucketErrorCount);
            EndFrame();
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
            _frameDrawCalls = 0;
            _frameActiveBatches = 0;
            _frameUnknownBucketErrors = 0;
        }
    }
}
