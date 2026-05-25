/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Sortie
{
    public partial class PassiveCard : GComponent
    {
        public Controller selected;
        public GGraph card_bg;
        public GGraph border_selected;
        public GTextField text_name;
        public GTextField icon_check;
        public const string URL = "ui://sg06st01comp_02";

        public static PassiveCard CreateInstance()
        {
            return (PassiveCard)UIPackage.CreateObject("SG_Sortie", "PassiveCard");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            selected = GetController("selected");
            card_bg = (GGraph)GetChild("card_bg");
            border_selected = (GGraph)GetChild("border_selected");
            text_name = (GTextField)GetChild("text_name");
            icon_check = (GTextField)GetChild("icon_check");
        }
    }
}