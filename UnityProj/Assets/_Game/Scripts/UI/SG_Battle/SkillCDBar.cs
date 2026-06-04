/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class SkillCDBar : GProgressBar
    {
        public GGraph bg;
        public const string URL = "ui://sg03bt04gen_07";

        public static SkillCDBar CreateInstance()
        {
            return (SkillCDBar)UIPackage.CreateObject("SG_Battle", "SkillCDBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
        }
    }
}