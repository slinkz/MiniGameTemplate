/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_LevelSelect
{
    public partial class BackButton : GButton
    {
        public GGraph bg_up;
        public GGraph bg_down;
        public GGraph bg_over;
        public GTextField icon_arrow;
        public const string URL = "ui://sg02ls03gen_03";

        public static BackButton CreateInstance()
        {
            return (BackButton)UIPackage.CreateObject("SG_LevelSelect", "BackButton");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg_up = (GGraph)GetChild("bg_up");
            bg_down = (GGraph)GetChild("bg_down");
            bg_over = (GGraph)GetChild("bg_over");
            icon_arrow = (GTextField)GetChild("icon_arrow");
        }
    }
}