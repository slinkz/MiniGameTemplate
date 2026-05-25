/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Sortie
{
    public partial class SortieBottomSheet : GComponent
    {
        public GGraph mask;
        public GGraph panel_bg;
        public GGraph drag_bar;
        public GTextField text_level;
        public GTextField label_skills;
        public GList list_skills;
        public GTextField label_passives;
        public GList list_passives;
        public BtnSortie btn_sortie;
        public const string URL = "ui://sg06st01main_01";

        public static SortieBottomSheet CreateInstance()
        {
            return (SortieBottomSheet)UIPackage.CreateObject("SG_Sortie", "SortieBottomSheet");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            mask = (GGraph)GetChild("mask");
            panel_bg = (GGraph)GetChild("panel_bg");
            drag_bar = (GGraph)GetChild("drag_bar");
            text_level = (GTextField)GetChild("text_level");
            label_skills = (GTextField)GetChild("label_skills");
            list_skills = (GList)GetChild("list_skills");
            label_passives = (GTextField)GetChild("label_passives");
            list_passives = (GList)GetChild("list_passives");
            btn_sortie = (BtnSortie)GetChild("btn_sortie");
        }
    }
}