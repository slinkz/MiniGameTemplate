namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI Action 执行器接口——支持多帧有状态执行。
    /// 每帧由 AIComponent 通过 ConditionActionTableStrategy 调用 Execute()。
    /// Action 内部维护自身状态（如 Patrol 的目标巡逻点、等待计时）。
    /// v2.4 新增（GD-R4-010）。
    /// </summary>
    public interface IAIAction
    {
        /// <summary>进入此 Action 时调用（初始化内部状态）</summary>
        void Enter(Entity owner);

        /// <summary>每帧执行，返回移动/攻击指令</summary>
        DecisionCommand Execute(Entity owner, float dt);

        /// <summary>退出此 Action 时调用（清理内部状态）</summary>
        void Exit(Entity owner);
    }
}
