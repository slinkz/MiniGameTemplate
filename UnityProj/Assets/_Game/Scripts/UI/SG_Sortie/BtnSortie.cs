/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Sortie
{
    public partial class BtnSortie : GButton
    {
        public GGraph bg_up;
        public GGraph bg_down;
        public GGraph bg_over;
        public const string URL = "ui://sg06st01comp_03";

        public static BtnSortie CreateInstance()
        {
            return (BtnSortie)UIPackage.CreateObject("SG_Sortie", "BtnSortie");
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