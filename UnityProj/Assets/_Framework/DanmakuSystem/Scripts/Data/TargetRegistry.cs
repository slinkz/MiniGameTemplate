namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 碰撞目标注册表——预分配 16 槽位，管理所有 <see cref="ICollisionTarget"/> 实例。
    /// CollisionSolver 每帧遍历本注册表进行弹丸 vs 目标碰撞检测。
    /// </summary>
    public class TargetRegistry
    {
        /// <summary>最大注册目标数</summary>
        public const int MAX_TARGETS = 16;

        private readonly ICollisionTarget[] _targets = new ICollisionTarget[MAX_TARGETS];
        private int _count;

        /// <summary>当前注册数量</summary>
        public int Count => _count;

        /// <summary>内部数组（CollisionSolver 直接遍历用）</summary>
        public ICollisionTarget[] Targets => _targets;

        /// <summary>
        /// 注册一个碰撞目标。
        /// </summary>
        /// <returns>分配的槽位索引（0~15），-1 表示已满</returns>
        public int Register(ICollisionTarget target)
        {
            if (target == null) return -1;

            // 检查重复
            for (int i = 0; i < MAX_TARGETS; i++)
            {
                if (_targets[i] == target) return i;
            }

            // 找空槽
            for (int i = 0; i < MAX_TARGETS; i++)
            {
                if (_targets[i] == null)
                {
                    _targets[i] = target;
                    _count++;
                    return i;
                }
            }

            return -1; // 满了
        }

        /// <summary>
        /// 注销一个碰撞目标。
        /// </summary>
        public void Unregister(ICollisionTarget target)
        {
            if (target == null) return;

            for (int i = 0; i < MAX_TARGETS; i++)
            {
                if (_targets[i] == target)
                {
                    _targets[i] = null;
                    _count--;
                    return;
                }
            }
        }

        /// <summary>清空全部注册。</summary>
        public void FreeAll()
        {
            for (int i = 0; i < MAX_TARGETS; i++)
                _targets[i] = null;
            _count = 0;
        }
    }
}
