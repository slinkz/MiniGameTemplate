using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸世界——预分配三层 SoA 数组 + 空闲槽位栈。
    /// 所有弹丸数据的唯一容器，零 GC。
    /// </summary>
    public class BulletWorld
    {
        public const int DEFAULT_MAX_BULLETS = 2048;

        /// <summary>热数据数组（运动/碰撞/生命周期）</summary>
        public readonly BulletCore[] Cores;

        /// <summary>冷数据数组（残影拖尾）</summary>
        public readonly BulletTrail[] Trails;

        /// <summary>修饰数据数组（延迟变速/追踪延迟）</summary>
        public readonly BulletModifier[] Modifiers;

        /// <summary>精确活跃弹丸数（Allocate +1 / Free -1）</summary>
        public int ActiveCount { get; private set; }

        /// <summary>数组容量（遍历上限）</summary>
        public int Capacity { get; }

        private readonly int[] _freeSlots;
        private int _freeTop;

        public BulletWorld(int capacity = DEFAULT_MAX_BULLETS)
        {
            Capacity = capacity;
            Cores = new BulletCore[capacity];
            Trails = new BulletTrail[capacity];
            Modifiers = new BulletModifier[capacity];
            _freeSlots = new int[capacity];

            // 倒序入栈，Allocate 时从 0 开始分配
            for (int i = capacity - 1; i >= 0; i--)
                _freeSlots[_freeTop++] = i;
        }

        /// <summary>
        /// 分配一个空闲槽位。返回索引，-1 表示池满。
        /// 调用者负责写入 Core/Trail/Modifier 数据并设置 FLAG_ACTIVE。
        /// </summary>
        public int Allocate()
        {
            if (_freeTop == 0) return -1;
            int index = _freeSlots[--_freeTop];
            ActiveCount++;

            // DEV-005 fix：初始化视觉动画默认值，避免 Renderer 端防御性分支
            ref var core = ref Cores[index];
            core.AnimScale = 1f;
            core.AnimAlpha = 1f;
            core.AnimColor = new Color32(255, 255, 255, 255);

            return index;
        }

        /// <summary>
        /// 归还槽位到空闲栈。清除 Flags 标记。
        /// </summary>
        public void Free(int index)
        {
            Cores[index].Flags = 0;
            _freeSlots[_freeTop++] = index;
            ActiveCount--;
        }

        /// <summary>
        /// 清场——回收所有弹丸。
        /// </summary>
        public void FreeAll()
        {
            _freeTop = 0;
            for (int i = Capacity - 1; i >= 0; i--)
            {
                Cores[i].Flags = 0;
                _freeSlots[_freeTop++] = i;
            }
            ActiveCount = 0;
        }
    }
}
