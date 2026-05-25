#if UNITY_EDITOR
using UnityEngine;
using MiniGameTemplate.Entity;

namespace Game.ShooterGame
{
    /// <summary>
    /// 【编辑器专用】Battle 场景直跑调试启动器。
    /// 
    /// 职责：
    /// 1. 在编辑器直跑 Battle 场景时，注入调试用的技能/被动/关卡配置
    /// 2. 提供快捷强制胜利/失败/重试按钮
    /// 3. 运行时状态监视（Inspector 可视化）
    /// 
    /// 使用方式：
    /// - 挂在 Battle 场景的任意 GameObject 上（推荐挂在 BattleController 同对象或专用 DebugRoot）
    /// - Inspector 中拖入想测试的技能/被动 SO 资产
    /// - 勾选 Enable 开关
    /// - 直接 Play Battle 场景即可，不影响正常导航流程启动
    /// 
    /// 设计约束：
    /// - 整个文件 #if UNITY_EDITOR 包裹，打包时零开销
    /// - 仅在"直跑场景"（非 Flow 启动）时注入，正常游戏流程完全不受影响
    /// - 通过 BattleController.SetDebugLauncher() 注入，不侵入正常初始化逻辑
    /// </summary>
    [AddComponentMenu("ShooterGame/Debug/Battle Debug Launcher")]
    public class BattleDebugLauncher : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════
        // 配置区
        // ════════════════════════════════════════════════════════════════

        [Header("═══ 启用开关 ═══")]
        [Tooltip("总开关。关闭后本组件完全不生效")]
        [SerializeField] private bool _enabled = true;

        [Header("═══ 关卡选择 ═══")]
        [Tooltip("直跑场景时使用的关卡索引（0-based）。-1 = 使用 BattleController 默认值")]
        [SerializeField] private int _debugLevelIndex = 0;

        [Header("═══ 普攻配置（TDD-06）═══")]
        [Tooltip("普攻 SkillConfigSO 覆盖。null = 从 EntityConfigSO.NormalAttackSkill 兜底")]
        [SerializeField] private SkillConfigSO _debugNormalAttack;

        [Header("═══ 技能装备 ═══")]
        [Tooltip("勾选后使用下方技能列表覆盖默认装备")]
        [SerializeField] private bool _overrideSkills = true;

        [Tooltip("要装备的主动技能（最多 6 个，留空槽位会被忽略）")]
        [SerializeField] private SkillConfigSO[] _debugSkills;

        [Header("═══ 被动装备 ═══")]
        [Tooltip("勾选后使用下方被动列表覆盖默认装备")]
        [SerializeField] private bool _overridePassives = true;

        [Tooltip("要装备的被动技能（最多 3 个，留空槽位会被忽略）")]
        [SerializeField] private PassiveAbilitySO[] _debugPassives;

        [Header("═══ 运行时状态（只读）═══")]
        [SerializeField] private string _status = "Idle";

        // ════════════════════════════════════════════════════════════════
        // 引用
        // ════════════════════════════════════════════════════════════════

        private BattleController _battleController;

        // ════════════════════════════════════════════════════════════════
        // 生命周期
        // ════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (!_enabled)
            {
                _status = "Disabled";
                return;
            }

            _battleController = GetComponent<BattleController>();
            if (_battleController == null)
                _battleController = FindObjectOfType<BattleController>();

            if (_battleController == null)
            {
                _status = "ERROR: No BattleController found";
                Debug.LogError("[BattleDebugLauncher] BattleController not found in scene!");
                return;
            }

            // 注册自己到 BattleController
            _battleController.SetDebugLauncher(this);
            _status = "Registered — waiting for direct-run check";
        }

        // ════════════════════════════════════════════════════════════════
        // 公共接口（由 BattleController 在直跑路径调用）
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// 构建调试用的 BattleLevelData。
        /// 仅在直跑场景（非 Flow 启动）时被 BattleController 调用。
        /// </summary>
        /// <returns>包含调试配置的 BattleLevelData，null 表示不覆盖</returns>
        public BattleLevelData BuildDebugLevelData()
        {
            if (!_enabled) return null;

            var data = new BattleLevelData();

            // 关卡
            data.LevelIndex = _debugLevelIndex >= 0 ? _debugLevelIndex : 0;

            // 技能
            if (_overrideSkills && _debugSkills != null && _debugSkills.Length > 0)
            {
                // 过滤掉 null 槽位
                var validSkills = FilterNulls(_debugSkills);
                if (validSkills.Length > 0)
                    data.EquippedSkills = validSkills;
            }

            // 被动
            if (_overridePassives && _debugPassives != null && _debugPassives.Length > 0)
            {
                var validPassives = FilterNulls(_debugPassives);
                if (validPassives.Length > 0)
                    data.EquippedPassives = validPassives;
            }

            // 普攻（TDD-06 §2.12 PK-ET-002）
            if (_debugNormalAttack != null)
                data.NormalAttackConfig = _debugNormalAttack;
            else
                Debug.LogWarning("[BattleDebugLauncher] _debugNormalAttack 未配置，将从 EntityConfigSO 兜底");

            int skillCount = data.EquippedSkills != null ? data.EquippedSkills.Length : 0;
            int passiveCount = data.EquippedPassives != null ? data.EquippedPassives.Length : 0;
            _status = $"Injected: Level={data.LevelIndex}, Skills={skillCount}, Passives={passiveCount}";
            Debug.Log($"[BattleDebugLauncher] {_status}");

            return data;
        }

        // ════════════════════════════════════════════════════════════════
        // Context Menu 快捷操作
        // ════════════════════════════════════════════════════════════════

        [ContextMenu("Force Victory")]
        private void ForceVictory()
        {
            if (_battleController != null)
                _battleController.DebugForceVictory();
        }

        [ContextMenu("Force Defeat")]
        private void ForceDefeat()
        {
            if (_battleController != null)
                _battleController.DebugForceDefeat();
        }

        [ContextMenu("Retry Battle")]
        private void RetryBattle()
        {
            if (_battleController != null)
                _battleController.DebugRetryBattle();
        }

        // ════════════════════════════════════════════════════════════════
        // 工具方法
        // ════════════════════════════════════════════════════════════════

        private static T[] FilterNulls<T>(T[] source) where T : ScriptableObject
        {
            int count = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null) count++;
            }

            if (count == source.Length) return source;
            if (count == 0) return System.Array.Empty<T>();

            var result = new T[count];
            int idx = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    result[idx++] = source[i];
            }
            return result;
        }
    }
}
#endif
