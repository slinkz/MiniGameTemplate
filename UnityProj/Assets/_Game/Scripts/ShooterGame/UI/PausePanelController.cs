using System;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// Pause panel controller. Keep this aligned with SG_Popup/PausePanel.xml.
    /// </summary>
    public class PausePanelController : MonoBehaviour, IPausePanelController
    {
        private const int SORT_ORDER = 800;

        private SG_Popup.PausePanel _view;
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
                CreateView();

            Time.timeScale = 0f;
            _view.visible = true;
        }

        public void Hide()
        {
            Time.timeScale = 1f;
            if (_view != null)
                _view.visible = false;
        }

        private void CreateView()
        {
            _view = SG_Popup.PausePanel.CreateInstance();
            if (_view == null)
            {
                Debug.LogError("[PausePanelController] Required component missing: PausePanel");
                return;
            }

            GRoot.inst.AddChild(_view);
            _view.sortingOrder = SORT_ORDER;
            _view.MakeFullScreen();

            if (_view.btn_resume != null)
                _view.btn_resume.onClick.Add(OnResumeClicked);
            else
                Debug.LogError("[PausePanelController] Required button missing: btn_resume");

            if (_view.btn_quit != null)
                _view.btn_quit.onClick.Add(OnQuitClicked);
            else
                Debug.LogError("[PausePanelController] Required button missing: btn_quit");

            if (_view.mask != null)
                _view.mask.onClick.Add(OnResumeClicked);
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
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }
    }
}
