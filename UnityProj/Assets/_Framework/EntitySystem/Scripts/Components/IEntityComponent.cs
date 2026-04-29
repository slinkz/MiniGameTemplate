namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 组件基接口。所有 Entity 组件必须实现此接口。
    /// 
    /// 生命周期：
    /// - Init(owner)：从池取出时调用，组件通过 owner 间接获取配置
    /// - Reset()：归还池时调用，清理运行时数据但保留对象本体
    /// - SetActive(bool)：激活/休眠切换
    /// </summary>
    public interface IEntityComponent
    {
        /// <summary>组件是否激活</summary>
        bool IsActive { get; }

        /// <summary>组件类型枚举（用于 Entity 内部数组索引，O(1) 查找）</summary>
        ComponentType Type { get; }

        /// <summary>
        /// 初始化（从池取出时调用）。
        /// 组件通过 owner 间接获取配置：owner.Config 提供配置数据。
        /// </summary>
        void Init(Entity owner);

        /// <summary>重置（归还池时调用，清运行时数据保留对象）</summary>
        void Reset();

        /// <summary>激活/休眠切换。休眠后从 TickList 移除，不响应事件，零开销。</summary>
        void SetActive(bool active);
    }
}
