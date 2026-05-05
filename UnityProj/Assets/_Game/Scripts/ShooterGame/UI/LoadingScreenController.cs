using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// LoadingScreen Controller——加载 FairyGUI 包 + 显示进度条。
    /// TDD_04 §2
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private float _minDisplayTime = 1f;

        private GComponent _view;
        private GProgressBar _progressBar;

        public void Show()
        {
            _view = UIPackage.CreateObject("SG_Loading", "LoadingScreen").asCom;
            GRoot.inst.AddChild(_view);
            _view.MakeFullScreen();
            _progressBar = _view.GetChild("bar").asProgress;
        }

        public void SetProgress(float ratio)
        {
            if (_progressBar != null)
                _progressBar.value = ratio * 100;
        }

        public void Hide()
        {
            if (_view == null) return;
            _view.TweenFade(0f, 0.3f).OnComplete(() =>
            {
                _view.Dispose();
                _view = null;
            });
        }
    }
}
