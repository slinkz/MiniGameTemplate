using MiniGameTemplate.Navigation;
using UnityEngine;

namespace Game.ShooterGame
{
    /// <summary>
    /// Battle 场景导航入口适配器。
    /// 单一职责：接收 AppFlow 导航数据，将启动上下文注入 BattleController 并显式触发战斗初始化。
    /// 这确保了时序正确——不依赖 Start() 与 OnFlowEnter 的执行先后。
    /// </summary>
    public class BattleFlowHandler : MonoBehaviour, IFlowHandler
    {
        [SerializeField] private BattleController _battleController;

        public void OnFlowEnter(IFlowData data)
        {
            if (_battleController == null)
            {
                Debug.LogError("[BattleFlowHandler] BattleController is null.");
                return;
            }

            if (data is BattleLevelData battleData)
            {
                // V2 Sprint 2: 传递完整数据（含装备技能/被动）
                _battleController.SetLaunchContext(battleData);
            }
            else
            {
                _battleController.SetLaunchContext((int?)null);
            }

            // 显式触发战斗初始化，消除 Start() 时序竞争
            _battleController.StartBattle();
        }

        public void OnFlowExit()
        {
        }
    }
}
