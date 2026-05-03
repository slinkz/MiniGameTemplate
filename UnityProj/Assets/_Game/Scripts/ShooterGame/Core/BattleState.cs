namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗状态枚举——BattleController 核心状态机驱动。
    /// TDD_02 §1.1
    /// </summary>
    public enum BattleState : byte
    {
        None = 0,
        Intro,      // 飞机进场动画，不 Tick Spawner、不检测碰撞、不响应输入
        Playing,    // 正常战斗
        Victory,    // 0.5s 静默 → 胜利界面
        Defeat,     // 基地爆炸 → 失败界面
    }
}
