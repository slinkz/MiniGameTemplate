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
        [SerializeField] private IntVariable _killCount;
        [SerializeField] private FloatVariable _baseHP;

        private GComponent _view;
        private Action _onConfirm;

        public void BindEvents(Action onConfirm)
        {
            _onConfirm = onConfirm;
        }

        public void Show()
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("SG_Popup", "VictoryPanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_confirm").asButton.onClick.Add(OnConfirmClicked);
            }

            // 填充数据
            _view.GetChild("text_kills").asTextField.text = $"击杀数：{_killCount.Value}";
            _view.GetChild("text_hp").asTextField.text = $"剩余血量：{Mathf.RoundToInt(_baseHP.Value * 100)}%";

            _view.visible = true;
        }

        private void OnConfirmClicked()
        {
            _view.visible = false;
            _onConfirm?.Invoke();
        }
    }
}
