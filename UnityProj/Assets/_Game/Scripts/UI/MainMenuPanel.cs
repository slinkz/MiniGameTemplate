using FairyGUI;
using MiniGameTemplate.Events;
using MiniGameTemplate.Platform;

namespace Game.UI
{
    /// <summary>
    /// Data passed to MainMenuPanel when opening.
    /// </summary>
    public class MainMenuPanelData
    {
        public GameEvent StartGameEvent;
        public IWeChatBridge WeChatBridge;
    }

    /// <summary>
    /// Main menu / lobby panel — the player's hub after loading completes.
    /// Displays player info, start button, and utility shortcuts.
    /// </summary>
    public class MainMenuPanel : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => MiniGameTemplate.UI.UIConstants.PKG_MAIN_MENU;
        protected override string ComponentName => MiniGameTemplate.UI.UIConstants.COMP_MAIN_MENU_PANEL;
        protected override int SortOrder => MiniGameTemplate.UI.UIConstants.LAYER_NORMAL;

        private GTextField _txtNickname;
        private GButton _btnStart;
        private GButton _btnSettings;
        private GButton _btnRanking;
        private GButton _btnShare;

        private GameEvent _startGameEvent;
        private IWeChatBridge _weChatBridge;

        protected override void OnInit()
        {
            base.OnInit();
            _txtNickname = ContentPane.GetChild("txtNickname") as GTextField;
            _btnStart = ContentPane.GetChild("btnStart") as GButton;
            _btnSettings = ContentPane.GetChild("btnSettings") as GButton;
            _btnRanking = ContentPane.GetChild("btnRanking") as GButton;
            _btnShare = ContentPane.GetChild("btnShare") as GButton;

            if (_btnStart != null) _btnStart.onClick.Add(OnStartClicked);
            if (_btnSettings != null) _btnSettings.onClick.Add(OnSettingsClicked);
            if (_btnRanking != null) _btnRanking.onClick.Add(OnRankingClicked);
            if (_btnShare != null) _btnShare.onClick.Add(OnShareClicked);
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            // Unpack dependencies from data — ensures they are available before RefreshPlayerInfo
            var menuData = data as MainMenuPanelData;
            if (menuData != null)
            {
                _startGameEvent = menuData.StartGameEvent;
                _weChatBridge = menuData.WeChatBridge;
            }

            RefreshPlayerInfo();
        }

        protected override void OnClose()
        {
            _startGameEvent = null;
            _weChatBridge = null;
            base.OnClose();
        }

        private void RefreshPlayerInfo()
        {
            if (_weChatBridge == null || _txtNickname == null) return;

            var userInfo = _weChatBridge.GetUserInfo();
            if (userInfo != null)
            {
                _txtNickname.text = userInfo.Nickname;
            }
        }

        private void OnStartClicked()
        {
            _startGameEvent?.Raise();
        }

        private void OnSettingsClicked()
        {
            // Placeholder — open settings panel when implemented
            MiniGameTemplate.Utils.GameLog.Log("[MainMenuPanel] Settings button clicked (not yet implemented).");
        }

        private void OnRankingClicked()
        {
            _weChatBridge?.ShowRankingPanel();
        }

        private void OnShareClicked()
        {
            _weChatBridge?.Share("来和我一起玩吧！", "", "");
        }
    }
}
