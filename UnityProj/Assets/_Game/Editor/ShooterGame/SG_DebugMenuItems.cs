using UnityEngine;
using UnityEditor;
using MiniGameTemplate.Data;
using MiniGameTemplate.Danmaku;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// ShooterGame 调试快捷菜单（仅 Play Mode 可用）。
    /// AT-007: 菜单路径常量化，执行方法和 Validate 使用同一 const。
    /// TDD: SG_TOOLS_TDD_02 §2.1
    /// </summary>
    public static class SG_DebugMenuItems
    {
        private const string MENU_ROOT = "Tools/SG/Debug/";

        private const string MENU_RETRY = MENU_ROOT + "重试当前关卡 %&R";
        private const string MENU_VICTORY = MENU_ROOT + "直接胜利";
        private const string MENU_DEFEAT = MENU_ROOT + "直接失败";
        private const string MENU_SKIP_WAVE = MENU_ROOT + "跳到下一波";
        private const string MENU_SET_HP = MENU_ROOT + "设置基地HP为50%";

        // ── 执行方法 ──

        /// <summary>强制重试当前关卡</summary>
        [MenuItem(MENU_RETRY)]
        private static void ForceRetry()
        {
            var bc = Object.FindObjectOfType<BattleController>();
            if (bc == null) { Debug.LogWarning("[SG Debug] 未找到 BattleController"); return; }
            bc.DebugRetryBattle();
        }

        /// <summary>直接判定胜利</summary>
        [MenuItem(MENU_VICTORY)]
        private static void ForceVictory()
        {
            var bc = Object.FindObjectOfType<BattleController>();
            if (bc == null) { Debug.LogWarning("[SG Debug] 未找到 BattleController"); return; }
            bc.DebugForceVictory();
        }

        /// <summary>直接判定失败</summary>
        [MenuItem(MENU_DEFEAT)]
        private static void ForceDefeat()
        {
            var bc = Object.FindObjectOfType<BattleController>();
            if (bc == null) { Debug.LogWarning("[SG Debug] 未找到 BattleController"); return; }
            bc.DebugForceDefeat();
        }

        /// <summary>
        /// 跳到下一波（秒杀场上全部敌方 Entity，利用框架 AllCleared 机制自动推进）。
        /// AT-001: 走 DamageDealer 正式管线（重入保护 + PendingDespawn 安全检查）。
        /// </summary>
        [MenuItem(MENU_SKIP_WAVE)]
        private static void SkipToNextWave()
        {
            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null) { Debug.LogWarning("[SG Debug] EntityManager 未初始化"); return; }

            var entities = mgr.ActiveEntities;
            int killed = 0;
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                var entity = entities[i];
                if (entity.Camp == EnumCamp.Enemy && !entity.IsPendingDespawn)
                {
                    var ctx = new DamageContext
                    {
                        BaseDamage = 99999,
                        HitType = MiniGameTemplate.Entity.CollisionEventType.ContactHit,
                    };
                    DamageDealer.DealDamageToEntity(entity, ctx);
                    killed++;
                }
            }
            Debug.Log($"[SG Debug] 已秒杀 {killed} 架敌机，等待下一波推进");
        }

        /// <summary>
        /// 设置基地 HP 为 50%。
        /// 必须同时修改 HealthComponent（真实 HP）和 FloatVariable SO（HUD 显示比率），
        /// 否则只改 SO 不改实际 HP → HUD 虽然更新了但下次 Tick 又会被覆盖回去。
        /// </summary>
        [MenuItem(MENU_SET_HP)]
        private static void SetBaseHP50()
        {
            // 1. 找到基地 Entity（Camp=Player 且持有 HealthComponent 且 ConfigSO.name 含 "Base"）
            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null) { Debug.LogWarning("[SG Debug] EntityManager 未初始化"); return; }

            HealthComponent baseHealth = null;
            var entities = mgr.ActiveEntities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.Camp == EnumCamp.Player && !entity.IsPendingDespawn)
                {
                    var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
                    if (health != null && entity.ConfigSO != null
                        && entity.ConfigSO.name.Contains("Base"))
                    {
                        baseHealth = health;
                        break;
                    }
                }
            }

            if (baseHealth == null)
            {
                Debug.LogWarning("[SG Debug] 未找到基地 Entity");
                return;
            }

            // 2. 修改 HealthComponent 的真实 HP
            int targetHp = Mathf.RoundToInt(baseHealth.MaxHp * 0.5f);
            baseHealth.SetHp(targetHp);

            // 3. 同步 FloatVariable SO（驱动 HUD 更新）
            var baseHPVar = SG_EditorUtility.FindSOByName<FloatVariable>("SG_BaseHP");
            if (baseHPVar != null)
            {
                baseHPVar.SetValue(baseHealth.HpRatio);
            }

            Debug.Log($"[SG Debug] 基地 HP 已设为 50% → {baseHealth.CurrentHp}/{baseHealth.MaxHp}");
        }

        // ── Validate（仅 Play Mode 可用）──

        [MenuItem(MENU_RETRY, true)]
        private static bool ValidateRetry() => Application.isPlaying;

        [MenuItem(MENU_VICTORY, true)]
        private static bool ValidateVictory() => Application.isPlaying;

        [MenuItem(MENU_DEFEAT, true)]
        private static bool ValidateDefeat() => Application.isPlaying;

        [MenuItem(MENU_SKIP_WAVE, true)]
        private static bool ValidateSkipWave() => Application.isPlaying;

        [MenuItem(MENU_SET_HP, true)]
        private static bool ValidateSetHP() => Application.isPlaying;
    }
}
