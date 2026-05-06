/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class PauseButton : GButton
    {
        public GGraph bg_up;
        public GGraph bg_down;
        public GGraph bg_over;
        public const string URL = "ui://sg03bt04gen_06";

        public static PauseButton CreateInstance()
        {
            return (PauseButton)UIPackage.CreateObject("SG_Battle", "PauseButton");
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