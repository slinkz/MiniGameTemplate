using FairyGUI;

namespace Game.UI
{
    /// <summary>
    /// FairyGUI exported binding part of ConfirmDialog.
    /// Keep UI field bindings here so re-export will not overwrite business logic.
    /// </summary>
    public partial class ConfirmDialog : MiniGameTemplate.UI.UIDialogBase
    {
        protected override string PackageName => "Common";
        protected override string ComponentName => "ConfirmDialog";


        private GTextField _txtTitle;
        private GTextField _txtContent;
        private GButton _btnConfirm;
        private GButton _btnCancel;

        protected override void OnInit()
        {
            base.OnInit();
            _txtTitle = ContentPane.GetChild("txtTitle") as GTextField;
            _txtContent = ContentPane.GetChild("txtContent") as GTextField;
            _btnConfirm = ContentPane.GetChild("btnConfirm") as GButton;
            _btnCancel = ContentPane.GetChild("btnCancel") as GButton;

            AddEvents();
        }
    }
}
