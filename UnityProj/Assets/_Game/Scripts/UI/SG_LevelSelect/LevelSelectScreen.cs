/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_LevelSelect
{
    public partial class LevelSelectScreen : GComponent
    {
        public GGraph bg;
        public BackButton btn_back;
        public GTextField text_title;
        public GGraph path_line;
        public LevelNode node_1;
        public GGraph conn_1_2;
        public LevelNode node_2;
        public GGraph conn_2_3;
        public LevelNode node_3;
        public GGraph conn_3_4;
        public LevelNode node_4;
        public GGraph conn_4_5;
        public LevelNode node_5;
        public const string URL = "ui://sg02ls03gen_01";

        public static LevelSelectScreen CreateInstance()
        {
            return (LevelSelectScreen)UIPackage.CreateObject("SG_LevelSelect", "LevelSelectScreen");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            btn_back = (BackButton)GetChild("btn_back");
            text_title = (GTextField)GetChild("text_title");
            path_line = (GGraph)GetChild("path_line");
            node_1 = (LevelNode)GetChild("node_1");
            conn_1_2 = (GGraph)GetChild("conn_1_2");
            node_2 = (LevelNode)GetChild("node_2");
            conn_2_3 = (GGraph)GetChild("conn_2_3");
            node_3 = (LevelNode)GetChild("node_3");
            conn_3_4 = (GGraph)GetChild("conn_3_4");
            node_4 = (LevelNode)GetChild("node_4");
            conn_4_5 = (GGraph)GetChild("conn_4_5");
            node_5 = (LevelNode)GetChild("node_5");
        }
    }
}