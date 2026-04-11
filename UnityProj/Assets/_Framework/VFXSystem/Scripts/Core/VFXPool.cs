using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// 预分配 Sprite Sheet VFX 槽位池。
    /// 只负责分配/回收，不负责渲染。
    /// </summary>
    public class VFXPool
    {
        public const int DEFAULT_CAPACITY = 64;

        public readonly VFXInstance[] Instances;
        public int Capacity { get; }
        public int ActiveCount { get; private set; }

        private readonly int[] _freeSlots;
        private int _freeTop;

        public VFXPool(int capacity = DEFAULT_CAPACITY)
        {
            Capacity = Mathf.Max(1, capacity);
            Instances = new VFXInstance[Capacity];
            _freeSlots = new int[Capacity];

            for (int i = Capacity - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
        }

        public int Allocate()
        {
            if (_freeTop == 0)
                return -1;

            ActiveCount++;
            return _freeSlots[--_freeTop];
        }

        public void Free(int index)
        {
            Instances[index].Flags = 0;
            _freeSlots[_freeTop++] = index;
            ActiveCount--;
        }

        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = Capacity - 1; i >= 0; i--)
            {
                Instances[i].Flags = 0;
                _freeSlots[_freeTop++] = i;
            }

            ActiveCount = 0;
        }
    }
}
