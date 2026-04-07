using FairyGUI;

namespace Game.UI
{
    /// <summary>
    /// FairyGUI exported binding part of ClickCounterPanel.
    /// Keep UI field bindings here so re-export will not overwrite gameplay logic.
    /// </summary>
    public partial class ClickCounterPanel : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => "Example";
        protected override string ComponentName => "ClickCounterPanel";


        private GTextField _txtTitle;
        private GTextField _txtHighScore;
        private GTextField _txtScore;
        private GTextField _txtTimer;
        private GTextField _txtHint;

        private GButton _btnTap;
        private GButton _btnBack;
        private GButton _btnRestart;
        private GButton _btnShare;

        protected override void OnInit()
        {
            base.OnInit();

            _txtTitle = ContentPane.GetChild("txtTitle") as GTextField;
            _txtHighScore = ContentPane.GetChild("txtHighScore") as GTextField;
            _txtScore = ContentPane.GetChild("txtScore") as GTextField;
            _txtTimer = ContentPane.GetChild("txtTimer") as GTextField;
            _txtHint = ContentPane.GetChild("txtHint") as GTextField;

            _btnTap = ContentPane.GetChild("btnTap") as GButton;
            _btnBack = ContentPane.GetChild("btnBack") as GButton;
            _btnRestart = ContentPane.GetChild("btnRestart") as GButton;
            _btnShare = ContentPane.GetChild("btnShare") as GButton;

            AddEvents();
        }
    }
}

