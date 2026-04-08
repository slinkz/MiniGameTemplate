namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光数据容器。结构与 BulletWorld 一致（空闲栈 + 容量常量），容量 16。
    /// Segments 数组在池初始化时预分配（最大折射数 + 1），回收时保留引用避免 GC。
    /// </summary>
    public class LaserPool
    {
        public const int MAX_LASERS = 16;

        /// <summary>每条激光最大支持的折射段数</summary>
        public const int MAX_SEGMENTS_PER_LASER = 9; // MaxReflections(8) + 1

        public readonly LaserData[] Data = new LaserData[MAX_LASERS];

        /// <summary>精确活跃激光数</summary>
        public int ActiveCount { get; private set; }

        private readonly int[] _freeSlots = new int[MAX_LASERS];
        private int _freeTop;

        public LaserPool()
        {
            // 预分配 Segments 数组——所有槽位共享同尺寸数组，避免运行时 GC
            for (int i = 0; i < MAX_LASERS; i++)
                Data[i].Segments = new LaserSegment[MAX_SEGMENTS_PER_LASER];

            for (int i = MAX_LASERS - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
        }

        /// <summary>分配一个空闲槽位。返回索引，-1 表示池满。</summary>
        public int Allocate()
        {
            if (_freeTop == 0) return -1;
            ActiveCount++;
            return _freeSlots[--_freeTop];
        }

        /// <summary>归还槽位——保留 Segments 数组引用，仅清零值类型字段。</summary>
        public void Free(int index)
        {
            var segments = Data[index].Segments; // 保留预分配的数组
            Data[index] = default;
            Data[index].Segments = segments;     // 恢复引用
            _freeSlots[_freeTop++] = index;
            ActiveCount--;
        }

        /// <summary>清场——回收所有激光。</summary>
        public void FreeAll()
        {
            for (int i = 0; i < MAX_LASERS; i++)
            {
                var segments = Data[i].Segments;
                Data[i] = default;
                Data[i].Segments = segments;
            }
            _freeTop = 0;
            for (int i = MAX_LASERS - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
            ActiveCount = 0;
        }
    }
}
