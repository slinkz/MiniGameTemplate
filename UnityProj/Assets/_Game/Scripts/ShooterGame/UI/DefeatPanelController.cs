using System;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 失败面板 Controller。TDD_04 §7
    /// </summary>
    public class DefeatPanelController : MonoBehaviour, IDefeatPanelController
    {
        [SerializeField] private IntVariable _killCount;
        [SerializeField] private IntVariable _totalEnemyCount;

        private GComponent _view;
        private Action _onRetry;
        private Action _onQuit;

        // 鼓励文案池
        private static readonly string[] ENCOURAGE_TEXTS =
        {
            "再来一次！", "差一点！", "你能行！", "这次更近了！"
        };

        public void BindEvents(Action onRetry, Action onQuit)
        {
            _onRetry = onRetry;
            _onQuit = onQuit;
        }

        public void Show()
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("SG_Popup", "DefeatPanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_retry").asButton.onClick.Add(OnRetryClicked);
                _view.GetChild("btn_quit").asButton.onClick.Add(OnQuitClicked);
            }

            // 填充数据
            _view.GetChild("text_progress").asTextField.text =
                $"消灭了 {_killCount.Value}/{_totalEnemyCount.Value} 架";
            _view.GetChild("text_encourage").asTextField.text =
                ENCOURAGE_TEXTS[UnityEngine.Random.Range(0, ENCOURAGE_TEXTS.Length)];

            _view.visible = true;
        }

        private void OnRetryClicked()
        {
            _view.visible = false;
            _onRetry?.Invoke();
        }

        private void OnQuitClicked()
        {
            _view.visible = false;
            _onQuit?.Invoke();
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
