using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 运动策略委托签名。
    /// 使用 delegate 而非 interface 避免装箱（零 GC 约束）。
    /// </summary>
    /// <param name="core">弹丸热数据（ref 可读写）</param>
    /// <param name="modifier">弹丸修饰数据（ref 可读写）</param>
    /// <param name="type">弹丸类型 SO</param>
    /// <param name="playerPos">玩家位置（追踪弹用）</param>
    /// <param name="dt">弹幕 deltaTime</param>
    public delegate void MotionStrategy(
        ref BulletCore core,
        ref BulletModifier modifier,
        BulletTypeSO type,
        Vector2 playerPos,
        float dt);

    /// <summary>
    /// 运动策略注册表——受控 static 注册表，按 MotionType 枚举索引。
    /// 初始化时注册所有内置策略。不做运行时开放注册、不做反射注册。
    /// </summary>
    public static class MotionRegistry
    {
        /// <summary>支持的最大运动类型数</summary>
        private const int MAX_MOTION_TYPES = 8;

        private static readonly MotionStrategy[] _strategies = new MotionStrategy[MAX_MOTION_TYPES];
        private static bool _initialized;

        /// <summary>
        /// 初始化注册表——注册所有内置策略。
        /// 在 DanmakuSystem 初始化前调用一次。
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _strategies[(int)MotionType.Default] = DefaultMotionStrategy.Execute;
            _strategies[(int)MotionType.SineWave] = SineWaveMotionStrategy.Execute;
            _strategies[(int)MotionType.Spiral] = SpiralMotionStrategy.Execute;

            _initialized = true;
        }

        /// <summary>
        /// 根据 MotionType 获取对应的运动策略。
        /// </summary>
        public static MotionStrategy Get(MotionType motionType)
        {
            int index = (int)motionType;
            if (index >= 0 && index < MAX_MOTION_TYPES && _strategies[index] != null)
                return _strategies[index];

            // fallback 到默认策略
            return _strategies[(int)MotionType.Default];
        }
    }
}
