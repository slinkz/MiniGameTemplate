/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Popup
{
    public partial class VictoryPanel : GComponent
    {
        public GGraph mask;
        public GGraph panel_bg;
        public GTextField text_victory;
        public GGraph divider1;
        public GTextField text_kills;
        public GTextField text_hp;
        public GTextField text_stars;
        public GGraph divider2;
        public SG_PopupButton btn_confirm;
        public const string URL = "ui://sg04pp05gen_02";

        public static VictoryPanel CreateInstance()
        {
            return (VictoryPanel)UIPackage.CreateObject("SG_Popup", "VictoryPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            mask = (GGraph)GetChild("mask");
            panel_bg = (GGraph)GetChild("panel_bg");
            text_victory = (GTextField)GetChild("text_victory");
            divider1 = (GGraph)GetChild("divider1");
            text_kills = (GTextField)GetChild("text_kills");
            text_hp = (GTextField)GetChild("text_hp");
            text_stars = (GTextField)GetChild("text_stars");
            divider2 = (GGraph)GetChild("divider2");
            btn_confirm = (SG_PopupButton)GetChild("btn_confirm");
        }
    }
}