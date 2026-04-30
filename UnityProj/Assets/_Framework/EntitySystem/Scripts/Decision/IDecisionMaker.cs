namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 决策接口——ControlComponent 和 AIComponent 均实现此接口。
    /// 每帧产生 DecisionCommand 驱动 Movement 和 Attack。
    /// BC-07.1: Control 和 AI 统一接口。
    /// </summary>
    public interface IDecisionMaker
    {
        /// <summary>获取当前帧的决策指令</summary>
        DecisionCommand GetDecision();
    }
}
