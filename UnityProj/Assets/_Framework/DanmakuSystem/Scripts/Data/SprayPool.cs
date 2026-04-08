namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 喷雾数据容器。结构同 LaserPool，容量 8。
    /// </summary>
    public class SprayPool
    {
        public const int MAX_SPRAYS = 8;

        public readonly SprayData[] Data = new SprayData[MAX_SPRAYS];

        /// <summary>精确活跃喷雾数</summary>
        public int ActiveCount { get; private set; }

        private readonly int[] _freeSlots = new int[MAX_SPRAYS];
        private int _freeTop;

        public SprayPool()
        {
            for (int i = MAX_SPRAYS - 1; i >= 0; i--)
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

        /// <summary>清场——回收所有喷雾。</summary>
        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = MAX_SPRAYS - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
            ActiveCount = 0;
        }
    }
}
