namespace MiniGameTemplate.Navigation
{
    /// <summary>
    /// 所有导航数据的标记接口。
    /// 约束：
    /// - Data 必须是 class + 实现 ToString() 方便调试
    /// - 推荐标记 [Serializable]；如需 Editor 热重载恢复则必须 [Serializable]（PK ET-005）
    /// V2 可在此接口上扩展序列化能力（如 bool IsSerializable）而无 break change。（PK UA-002）
    /// </summary>
    public interface IFlowData { }
}
