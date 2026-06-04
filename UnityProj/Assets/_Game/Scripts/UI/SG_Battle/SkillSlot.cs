/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class SkillSlot : GComponent
    {
        public Controller state;
        public GGraph bg;
        public GGraph border;
        public SkillCDBar cd_bar;
        public const string URL = "ui://sg03bt04gen_10";

        public static SkillSlot CreateInstance()
        {
            return (SkillSlot)UIPackage.CreateObject("SG_Battle", "SkillSlot");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            state = GetController("state");
            bg = (GGraph)GetChild("bg");
            border = (GGraph)GetChild("border");
            cd_bar = (SkillCDBar)GetChild("cd_bar");
        }
    }
}