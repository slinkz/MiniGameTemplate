namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 可选接口。由场景内 MonoBehaviour 实现（如 BattleFlowController）。
    /// ⚠️ ScriptableObject 禁止实现此接口（SO=纯数据原则）。（PK UA-008）
    /// 
    /// Navigator 在场景加载后通过场景根对象 GetComponentInChildren 查找实现者。
    /// 纯 UI 节点无需实现 — 面板自身的 OnOpen/OnClose 已覆盖。
    /// </summary>
    public interface IFlowHandler
    {
        /// <summary>导航器进入此节点时调用（首次 Push）。</summary>
        void OnFlowEnter(IFlowData data);

        /// <summary>导航器永久离开此节点时调用（节点被移出栈）。</summary>
        void OnFlowExit();
    }

    /// <summary>
    /// 可选挂起/恢复接口。场景节点（如 Battle）实现此接口管理暂停/恢复。
    /// Push 时 Navigator 调用 OnFlowSuspend()，Pop 返回时调用 OnFlowResume()。
    /// 纯 UI 节点通常无需实现 — CloseAllPanels 已充分"挂起"。（PK UA-007）
    /// </summary>
    public interface IFlowSuspendable
    {
        /// <summary>被新节点压入时调用（挂起：释放事件订阅、暂停逻辑）。</summary>
        void OnFlowSuspend();

        /// <summary>从上层节点 Pop 回来时调用（恢复：重新订阅事件、继续逻辑）。</summary>
        void OnFlowResume(IFlowData data);
    }
}
