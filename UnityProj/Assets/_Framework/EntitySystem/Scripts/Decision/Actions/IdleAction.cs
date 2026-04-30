namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI Action: 原地不动。无状态。
    /// </summary>
    public sealed class IdleAction : IAIAction
    {
        public void Enter(Entity owner) { }

        public DecisionCommand Execute(Entity owner, float dt)
        {
            return DecisionCommand.Idle;
        }

        public void Exit(Entity owner) { }
    }
}
