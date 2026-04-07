using FairyGUI;

namespace Game.UI
{
    /// <summary>
    /// FairyGUI exported binding part of PrivacyDialog.
    /// Keep UI field bindings here so re-export will not overwrite business logic.
    /// </summary>
    public partial class PrivacyDialog : MiniGameTemplate.UI.UIDialogBase
    {
        protected override string PackageName => "Common";
        protected override string ComponentName => "PrivacyDialog";


        private GButton _btnAgree;
        private GButton _btnReject;
        private GTextField _txtPrivacyLink;

        protected override void OnInit()
        {
            base.OnInit();
            _btnAgree = ContentPane.GetChild("btnAgree") as GButton;
            _btnReject = ContentPane.GetChild("btnReject") as GButton;
            _txtPrivacyLink = ContentPane.GetChild("txtPrivacyLink") as GTextField;

            AddEvents();
        }
    }
}
