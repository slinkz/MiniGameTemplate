/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Common
{
    public partial class ConfirmDialog : GComponent
    {
        public GGraph bg;
        public GTextField txtTitle;
        public GGraph divider;
        public GTextField txtContent;
        public CommonButton btnCancel;
        public CommonButton btnConfirm;
        public const string URL = "ui://cm01ab02gen_02";

        public static ConfirmDialog CreateInstance()
        {
            return (ConfirmDialog)UIPackage.CreateObject("Common", "ConfirmDialog");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            txtTitle = (GTextField)GetChild("txtTitle");
            divider = (GGraph)GetChild("divider");
            txtContent = (GTextField)GetChild("txtContent");
            btnCancel = (CommonButton)GetChild("btnCancel");
            btnConfirm = (CommonButton)GetChild("btnConfirm");
        }
    }
}