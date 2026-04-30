namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI 决策策略接口——AIComponent 内部通过此接口实现策略可替换。
    /// BC-07.3: 默认实现为 ConditionActionTableStrategy。
    /// </summary>
    public interface IDecisionStrategy
    {
        /// <summary>初始化策略（绑定 owner）</summary>
        void Init(Entity owner);

        /// <summary>每帧评估并返回决策指令</summary>
        DecisionCommand Evaluate(float dt);

        /// <summary>重置策略状态</summary>
        void Reset();
    }
}
