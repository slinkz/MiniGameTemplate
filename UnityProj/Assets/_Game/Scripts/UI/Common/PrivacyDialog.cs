/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Common
{
    public partial class PrivacyDialog : GComponent
    {
        public GGraph bg;
        public GTextField txtTitle;
        public GGraph divider;
        public GTextField txtContent;
        public GTextField txtPrivacyLink;
        public CommonButton btnReject;
        public CommonButton btnAgree;
        public const string URL = "ui://cm01ab02gen_05";

        public static PrivacyDialog CreateInstance()
        {
            return (PrivacyDialog)UIPackage.CreateObject("Common", "PrivacyDialog");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            txtTitle = (GTextField)GetChild("txtTitle");
            divider = (GGraph)GetChild("divider");
            txtContent = (GTextField)GetChild("txtContent");
            txtPrivacyLink = (GTextField)GetChild("txtPrivacyLink");
            btnReject = (CommonButton)GetChild("btnReject");
            btnAgree = (CommonButton)GetChild("btnAgree");
        }
    }
}