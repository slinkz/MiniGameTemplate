using FairyGUI;

namespace Game.UI
{
    /// <summary>
    /// FairyGUI exported binding part of LoadingPanel.
    /// Keep UI field bindings here so re-export will not overwrite business logic.
    /// </summary>
    public partial class LoadingPanel : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => "Common";
        protected override string ComponentName => "LoadingPanel";


        private GProgressBar _progressBar;
        private GTextField _txtPercent;
        private GTextField _txtHint;
        private GTextField _txtTitle;

        protected override void OnInit()
        {
            base.OnInit();
            _progressBar = ContentPane.GetChild("progressBar") as GProgressBar;
            _txtPercent = _progressBar?.GetChild("title")?.asTextField;
            _txtHint = ContentPane.GetChild("txtHint") as GTextField;
            _txtTitle = ContentPane.GetChild("txtTitle") as GTextField;
        }
    }
}
