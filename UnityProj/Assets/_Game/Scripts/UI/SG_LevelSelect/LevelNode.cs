/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_LevelSelect
{
    public partial class LevelNode : GButton
    {
        public Controller state;
        public GGraph bg_cleared;
        public GGraph bg_available;
        public GGraph bg_locked;
        public StarDisplay star_group;
        public GGraph icon_lock;
        public GTextField text_play;
        public const string URL = "ui://sg02ls03gen_02";

        public static LevelNode CreateInstance()
        {
            return (LevelNode)UIPackage.CreateObject("SG_LevelSelect", "LevelNode");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            state = GetController("state");
            bg_cleared = (GGraph)GetChild("bg_cleared");
            bg_available = (GGraph)GetChild("bg_available");
            bg_locked = (GGraph)GetChild("bg_locked");
            star_group = (StarDisplay)GetChild("star_group");
            icon_lock = (GGraph)GetChild("icon_lock");
            text_play = (GTextField)GetChild("text_play");
        }
    }
}