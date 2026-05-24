using System;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 胜利面板 Controller。TDD_04 §6
    /// </summary>
    public class VictoryPanelController : MonoBehaviour, IVictoryPanelController
    {
        private GComponent _view;
        private Action _onConfirm;

        public void BindEvents(Action onConfirm)
        {
            _onConfirm = onConfirm;
        }

        public void Show(BattleResultData result)
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("SG_Popup", "VictoryPanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_confirm").asButton.onClick.Add(OnConfirmClicked);
            }

            // 填充数据（从 BattleResultData 快照读取，不依赖 SO 变量）
            _view.GetChild("text_kills").asTextField.text = $"击杀数：{result.TotalKills}";

            int hpPercent = result.BaseHpMax > 0
                ? Mathf.RoundToInt((float)result.BaseHpRemaining / result.BaseHpMax * 100)
                : 0;
            _view.GetChild("text_hp").asTextField.text = $"剩余血量：{hpPercent}%";

            // 星级显示（通过 graph 组件的 controller 控制亮暗）
            var starGroup = _view.GetChild("star_group")?.asCom;
            if (starGroup != null)
            {
                var ctrl = starGroup.GetController("stars");
                if (ctrl != null)
                    ctrl.selectedIndex = Mathf.Clamp(result.Stars, 0, 3);
            }

            _view.visible = true;
        }

        private void OnConfirmClicked()
        {
            _view.visible = false;
            _onConfirm?.Invoke();
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }
    }
}
