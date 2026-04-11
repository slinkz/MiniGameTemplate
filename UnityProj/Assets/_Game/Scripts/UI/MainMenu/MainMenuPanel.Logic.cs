using MiniGameTemplate.Core;
using MiniGameTemplate.Events;
using MiniGameTemplate.Platform;
using MiniGameTemplate.Timing;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    /// <summary>
    /// Data passed to MainMenuPanel when opening.
    /// </summary>
    public class MainMenuPanelData
    {
        public GameEvent StartGameEvent;
        public IWeChatBridge WeChatBridge;
        public bool EnableBannerAd = true;
    }

    /// <summary>
    /// Main menu / lobby panel — the player's hub after loading completes.
    /// Displays player info, start button, and utility shortcuts.
    ///
    /// If StartGameEvent is not configured in Boot scene, this panel runs a built-in
    /// ClickCounter fallback mode so the template remains playable out of the box.
    /// </summary>
    public partial class MainMenuPanel : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "MainMenu";

        private IWeChatBridge _weChatBridge;
        private bool _enableBannerAd = true;

        public void OnOpen(object data)
        {
            // Bind button events (only in OnOpen — never re-bind)
            if (btnClickGame != null) btnClickGame.onClick.Add(OnClickGameClicked);
            if (btnDanmakuDemo != null) btnDanmakuDemo.onClick.Add(OnDanmukuDemoClicked);
            if (btnVFXDemo != null) btnVFXDemo.onClick.Add(OnVFXDemoClicked);
            if (btnSettings != null) btnSettings.onClick.Add(OnSettingsClicked);
            if (btnRanking != null) btnRanking.onClick.Add(OnRankingClicked);
            if (btnShare != null) btnShare.onClick.Add(OnShareClicked);

            ApplyData(data);
        }

        public void OnClose()
        {
            if (_enableBannerAd)
                _weChatBridge?.HideBannerAd();

            _weChatBridge = null;
        }

        public void OnRefresh(object data)
        {
            // Only update data — do NOT re-bind events
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            var menuData = data as MainMenuPanelData;

            if (menuData != null)
            {
                _weChatBridge = menuData.WeChatBridge;
                _enableBannerAd = menuData.EnableBannerAd;
            }
            else
            {
                _enableBannerAd = true;
            }

            EnterMenuState();
        }

        private void OnClickGameClicked()
        {
            UIManager.Instance.ClosePanel<MainMenuPanel>();
            SceneManager.LoadScene("ClickGame");

        }

        private void OnDanmukuDemoClicked()
        {
            UIManager.Instance.ClosePanel<MainMenuPanel>();
            SceneManager.LoadScene("DanmakuDemo");
        }

        private void OnVFXDemoClicked()
        {
            UIManager.Instance.ClosePanel<MainMenuPanel>();
            SceneManager.LoadScene("VFXDemo");
        }

        private void OnSettingsClicked()
        {
            GameLog.Log("[MainMenuPanel] Settings button clicked (not yet implemented).");
        }

        private void OnRankingClicked()
        {
            _weChatBridge?.ShowRankingPanel();
        }

        private void OnShareClicked()
        {
            _weChatBridge?.Share("来和我一起玩吧！", "", "");
        }

        private void EnterMenuState()
        {
            if (_enableBannerAd)
                _weChatBridge?.ShowBannerAd();
        }
    }
}
