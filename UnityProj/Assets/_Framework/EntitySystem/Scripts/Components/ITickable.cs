namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 需要每帧驱动的组件。
    /// 实现此接口的组件会被 Entity 按 TickOrder 升序调度 Tick(dt)。
    /// 休眠（IsActive=false）时不调用 Tick。
    /// </summary>
    public interface ITickable
    {
        /// <summary>Tick 排序优先级（升序执行，数字越小越先）</summary>
        int TickOrder { get; }

        /// <summary>每帧更新</summary>
        void Tick(float dt);
    }

    /// <summary>
    /// Tick 优先级常量。各组件使用此常量确保执行顺序稳定。
    /// </summary>
    public static class TickOrders
    {
        public const int Decision  = 100;  // ControlComponent / AIComponent
        public const int Attack    = 150;  // AttackComponent
        public const int AutoAim   = 200;  // AutoAimComponent
        public const int Health    = 250;  // HealthComponent（P2.4：无敌帧/HitStop 计时）
        public const int Movement  = 300;  // MovementComponent
        public const int Animation = 400;  // AnimationComponent
    }
}
