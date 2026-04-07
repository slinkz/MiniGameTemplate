using FairyGUI;

namespace Game.UI
{
    /// <summary>
    /// FairyGUI exported binding part of GlobalSpinner.
    /// Keep UI field bindings here so re-export will not overwrite business logic.
    /// </summary>
    public partial class GlobalSpinner : MiniGameTemplate.UI.UIBase
    {
        protected override string PackageName => "Common";
        protected override string ComponentName => "GlobalSpinner";


        private GTextField _txtHint;

        protected override void OnInit()
        {
            base.OnInit();
            _txtHint = ContentPane.GetChild("txtHint") as GTextField;
        }
    }
}
