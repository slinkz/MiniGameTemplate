using UnityEngine;
using MiniGameTemplate.Navigation;

namespace Game.ShooterGame
{
    /// <summary>
    /// ShooterGame 的 FlowNode SO 引用持有者。
    /// 通过 Resources.Load 在首次访问时加载 SO 资产。
    /// 
    /// SO 路径约定：Resources/Navigation/Node_*.asset
    /// （Phase 2.1 创建的 SO 资产放在 Assets/_Game/Resources/Navigation/）
    /// </summary>
    public static class SG_FlowNodes
    {
        private static FlowNodeSO _nodeLevelSelect;
        private static FlowNodeSO _nodeBattle;

        /// <summary>选关界面节点。</summary>
        public static FlowNodeSO NodeLevelSelect
        {
            get
            {
                if (_nodeLevelSelect == null)
                    _nodeLevelSelect = Resources.Load<FlowNodeSO>("Navigation/Node_LevelSelect");
                return _nodeLevelSelect;
            }
        }

        /// <summary>战斗节点。</summary>
        public static FlowNodeSO NodeBattle
        {
            get
            {
                if (_nodeBattle == null)
                    _nodeBattle = Resources.Load<FlowNodeSO>("Navigation/Node_Battle");
                return _nodeBattle;
            }
        }

        /// <summary>清理缓存（Domain Reload 时自动清理）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _nodeLevelSelect = null;
            _nodeBattle = null;
        }
    }
}
