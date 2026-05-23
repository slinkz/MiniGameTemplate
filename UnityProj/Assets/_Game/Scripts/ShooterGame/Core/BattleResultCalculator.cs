namespace Game.ShooterGame
{
    /// <summary>
    /// 星级计算——纯静态工具类，零依赖，便于单元测试。
    /// TDD_04 §S4.4
    /// 
    /// 星级标准（GDD §星级评价标准）：
    ///   ⭐⭐⭐ = HP ≥ 80%（碾压）
    ///   ⭐⭐   = HP ≥ 50%（打得不错）
    ///   ⭐     = HP > 0（险过）
    ///   0      = HP = 0（失败）
    /// </summary>
    public static class BattleResultCalculator
    {
        private const float THRESHOLD_3_STAR = 0.80f;
        private const float THRESHOLD_2_STAR = 0.50f;

        /// <summary>
        /// 计算星级。
        /// </summary>
        /// <param name="currentHp">基地当前 HP</param>
        /// <param name="maxHp">基地最大 HP</param>
        /// <returns>0~3 星</returns>
        public static int CalcStars(int currentHp, int maxHp)
        {
            if (maxHp <= 0 || currentHp <= 0) return 0;

            float ratio = (float)currentHp / maxHp;
            if (ratio >= THRESHOLD_3_STAR) return 3;
            if (ratio >= THRESHOLD_2_STAR) return 2;
            return 1;
        }
    }
}
