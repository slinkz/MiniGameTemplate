/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_LevelSelect
{
    public partial class StarDisplay : GComponent
    {
        public Controller stars;
        public GGraph star1_off;
        public GGraph star1_on;
        public GGraph star2_off;
        public GGraph star2_on;
        public GGraph star3_off;
        public GGraph star3_on;
        public const string URL = "ui://sg02ls03gen_10";

        public static StarDisplay CreateInstance()
        {
            return (StarDisplay)UIPackage.CreateObject("SG_LevelSelect", "StarDisplay");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            stars = GetController("stars");
            star1_off = (GGraph)GetChild("star1_off");
            star1_on = (GGraph)GetChild("star1_on");
            star2_off = (GGraph)GetChild("star2_off");
            star2_on = (GGraph)GetChild("star2_on");
            star3_off = (GGraph)GetChild("star3_off");
            star3_on = (GGraph)GetChild("star3_on");
        }
    }
}