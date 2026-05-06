using System;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 暂停面板 Controller。TDD_04 §5
    /// </summary>
    public class PausePanelController : MonoBehaviour, IPausePanelController
    {
        private GComponent _view;
        private Action _onResume;
        private Action _onQuit;

        public void BindEvents(Action onResume, Action onQuit)
        {
            _onResume = onResume;
            _onQuit = onQuit;
        }

        public void Show()
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("SG_Popup", "PausePanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_resume").asButton.onClick.Add(OnResumeClicked);
                _view.GetChild("btn_quit").asButton.onClick.Add(OnQuitClicked);
            }

            Time.timeScale = 0f;
            _view.visible = true;
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            if (_view != null)
                _view.visible = false;
        }

        private void OnResumeClicked()
        {
            Hide();
            _onResume?.Invoke();
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
            _onQuit?.Invoke();
        }

        private void OnDestroy()
        {
            // 场景卸载时清理 FairyGUI view（挂在 DontDestroyOnLoad 的 GRoot 上）
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }
    }
}
