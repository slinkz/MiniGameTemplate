using FairyGUI;

namespace Game.UI
{
    /// <summary>
    /// FairyGUI exported binding part of MainMenuPanel.
    /// Keep UI field bindings here so re-export will not overwrite gameplay logic.
    /// </summary>
    public partial class MainMenuPanel : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => "MainMenu";
        protected override string ComponentName => "MainMenuPanel";


        private GTextField _txtNickname;
        private GTextField _txtGameTitle;
        private GTextField _txtVersion;
        private GButton _btnStart;
        private GButton _btnSettings;
        private GButton _btnRanking;
        private GButton _btnShare;

        protected override void OnInit()
        {
            base.OnInit();

            _txtNickname = ContentPane.GetChild("txtNickname") as GTextField;
            _txtGameTitle = ContentPane.GetChild("txtGameTitle") as GTextField;
            _txtVersion = ContentPane.GetChild("txtVersion") as GTextField;
            _btnStart = ContentPane.GetChild("btnStart") as GButton;
            _btnSettings = ContentPane.GetChild("btnSettings") as GButton;
            _btnRanking = ContentPane.GetChild("btnRanking") as GButton;
            _btnShare = ContentPane.GetChild("btnShare") as GButton;

            AddEvents();
        }
    }
}
