namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// EntityManager 全局访问点（Editor 工具 + 游戏层查询用）。
    /// 由 EntitySystemBootstrap.Awake() 注册，OnDestroy() 注销。
    /// 非 Singleton 模式——不阻止多实例（测试/分屏场景预留）。
    /// 
    /// v2.6 WF-004：避免全局 static 引用导致的生命周期问题，
    /// 通过显式注册/注销保证引用始终指向当前场景的实例。
    /// </summary>
    public static class EntityManagerAccessor
    {
        /// <summary>当前场景的 EntityManager 实例</summary>
        public static EntityManager Instance { get; internal set; }

        /// <summary>当前场景的 EntityViewBridge 实例（Phase 1.9）</summary>
        public static EntityViewBridge ViewBridge { get; internal set; }

        /// <summary>当前场景的 EntitySpawner 实例（Phase 1.10）</summary>
        public static EntitySpawner Spawner { get; internal set; }
    }
}
