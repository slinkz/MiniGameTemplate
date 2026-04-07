/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Common
{
    public partial class CommonProgressBar : GProgressBar
    {
        public GGraph bg;
        public const string URL = "ui://cm01ab02gen_03";

        public static CommonProgressBar CreateInstance()
        {
            return (CommonProgressBar)UIPackage.CreateObject("Common", "CommonProgressBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
        }
    }
}