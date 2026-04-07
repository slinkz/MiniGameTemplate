/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Common
{
    public partial class LoadingPanel : GComponent
    {
        public GGraph bg;
        public GTextField txtTitle;
        public GTextField txtSubtitle;
        public CommonProgressBar progressBar;
        public GTextField txtHint;
        public const string URL = "ui://cm01ab02gen_01";

        public static LoadingPanel CreateInstance()
        {
            return (LoadingPanel)UIPackage.CreateObject("Common", "LoadingPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            txtTitle = (GTextField)GetChild("txtTitle");
            txtSubtitle = (GTextField)GetChild("txtSubtitle");
            progressBar = (CommonProgressBar)GetChild("progressBar");
            txtHint = (GTextField)GetChild("txtHint");
        }
    }
}