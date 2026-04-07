/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace MainMenu
{
    public partial class MenuIconButton : GButton
    {
        public GGraph bg_up;
        public GGraph bg_down;
        public GGraph bg_over;
        public const string URL = "ui://mm03ef04gen_02";

        public static MenuIconButton CreateInstance()
        {
            return (MenuIconButton)UIPackage.CreateObject("MainMenu", "MenuIconButton");
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