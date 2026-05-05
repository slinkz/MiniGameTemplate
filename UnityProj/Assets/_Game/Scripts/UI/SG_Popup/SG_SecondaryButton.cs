/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Popup
{
    public partial class SG_SecondaryButton : GButton
    {
        public GGraph bg_up;
        public GGraph bg_down;
        public GGraph bg_over;
        public const string URL = "ui://sg04pp05gen_05";

        public static SG_SecondaryButton CreateInstance()
        {
            return (SG_SecondaryButton)UIPackage.CreateObject("SG_Popup", "SG_SecondaryButton");
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