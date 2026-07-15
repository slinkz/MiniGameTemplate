using System;
using UnityEngine;
using FairyGUI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// Victory result panel controller. Keep this aligned with SG_Popup/VictoryPanel.xml.
    /// </summary>
    public class VictoryPanelController : MonoBehaviour, IVictoryPanelController
    {
        private const float PANEL_SLIDE_DURATION = 0.3f;

        private SG_Popup.VictoryPanel _view;
        private Action _onReturnToSelect;

        public void BindEvents(Action onReturnToSelect)
        {
            _onReturnToSelect = onReturnToSelect;
        }

        public void Show(BattleResultData result)
        {
            EnsureView();
            PopulateData(result);
            StartShowAnimation();
        }

        private void EnsureView()
        {
            if (_view != null) return;

            _view = SG_Popup.VictoryPanel.CreateInstance();
            GRoot.inst.AddChild(_view);
            _view.MakeFullScreen();
            _view.sortingOrder = 100;

            if (_view.btn_confirm != null)
                _view.btn_confirm.onClick.Add(OnReturnClicked);
            else
                Debug.LogError("[VictoryPanelController] Required button missing: btn_confirm");
        }

        private void PopulateData(BattleResultData result)
        {
            if (_view.text_kills != null)
                _view.text_kills.text = $"击杀：{result.TotalKills}";

            if (_view.text_hp != null)
            {
                int hpPercent = result.BaseHpMax > 0
                    ? Mathf.RoundToInt((float)result.BaseHpRemaining / result.BaseHpMax * 100f)
                    : 0;
                _view.text_hp.text = $"剩余血量：{hpPercent}%";
            }

            if (_view.star_group != null && _view.star_group.stars != null)
                _view.star_group.stars.selectedIndex = Mathf.Clamp(result.Stars, 0, 3);
        }

        private void StartShowAnimation()
        {
            _view.visible = true;
            _view.alpha = 0f;
            _view.y = Screen.height * 0.1f;

            _view.TweenFade(1f, PANEL_SLIDE_DURATION).SetIgnoreEngineTimeScale(true);
            _view.TweenMoveY(0f, PANEL_SLIDE_DURATION)
                .SetEase(EaseType.CubicOut)
                .SetIgnoreEngineTimeScale(true);
        }

        private void OnReturnClicked()
        {
            HideAndDispose();
            _onReturnToSelect?.Invoke();
        }

        private void HideAndDispose()
        {
            if (_view != null)
                _view.visible = false;
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
