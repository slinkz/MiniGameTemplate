/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Common
{
    public partial class CommonButton : GButton
    {
        public GGraph bg_up;
        public GGraph bg_down;
        public GGraph bg_over;
        public const string URL = "ui://cm01ab02gen_04";

        public static CommonButton CreateInstance()
        {
            return (CommonButton)UIPackage.CreateObject("Common", "CommonButton");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg_up = (GGraph)GetChild("bg_up");
            bg_down = (GGraph)GetChild("bg_down");
            bg_over = (GGraph)GetChild("bg_over");
        }
    }
}