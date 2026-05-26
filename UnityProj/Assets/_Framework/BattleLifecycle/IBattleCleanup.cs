namespace MiniGameTemplate.Battle
{
    /// <summary>
    /// 战斗退场清理接口。
    /// 实现者自行注册到 BattleLifecycleEvent SO，退场时自动回调。
    /// TDD-07 §3.1
    /// </summary>
    /// <remarks>
    /// WX-001: 不使用 C# 8 DIM（Default Interface Methods），
    /// 避免 IL2CPP/WebGL 下的 AOT 兼容性风险。所有实现者显式声明 CleanupOrder。
    /// </remarks>
    public interface IBattleCleanup
    {
        /// <summary>清理优先级（数值越小越先执行）。</summary>
        int CleanupOrder { get; }

        /// <summary>执行退场清理。</summary>
        void OnBattleCleanup();
    }
}
