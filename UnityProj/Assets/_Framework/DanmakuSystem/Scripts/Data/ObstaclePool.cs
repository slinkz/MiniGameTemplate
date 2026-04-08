namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 障碍物数据容器。AABB 碰撞，容量 64。
    /// </summary>
    public class ObstaclePool
    {
        public const int MAX_OBSTACLES = 64;

        public readonly ObstacleData[] Data = new ObstacleData[MAX_OBSTACLES];

        /// <summary>精确活跃障碍物数</summary>
        public int ActiveCount { get; private set; }

        private readonly int[] _freeSlots = new int[MAX_OBSTACLES];
        private int _freeTop;

        public ObstaclePool()
        {
            for (int i = MAX_OBSTACLES - 1; i >= 0; i--)
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

        /// <summary>清场——回收所有障碍物。</summary>
        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = MAX_OBSTACLES - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
            ActiveCount = 0;
        }
    }
}
