namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光数据容器。结构与 BulletWorld 一致（空闲栈 + 容量常量），容量 16。
    /// </summary>
    public class LaserPool
    {
        public const int MAX_LASERS = 16;

        public readonly LaserData[] Data = new LaserData[MAX_LASERS];

        /// <summary>精确活跃激光数</summary>
        public int ActiveCount { get; private set; }

        private readonly int[] _freeSlots = new int[MAX_LASERS];
        private int _freeTop;

        public LaserPool()
        {
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

        /// <summary>归还槽位。</summary>
        public void Free(int index)
        {
            Data[index] = default;
            _freeSlots[_freeTop++] = index;
            ActiveCount--;
        }

        /// <summary>清场——回收所有激光。</summary>
        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = MAX_LASERS - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
            ActiveCount = 0;
        }
    }
}
