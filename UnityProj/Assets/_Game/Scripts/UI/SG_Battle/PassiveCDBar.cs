/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class PassiveCDBar : GProgressBar
    {
        public GGraph bg;
        public const string URL = "ui://sg03bt04gen_08";

        public static PassiveCDBar CreateInstance()
        {
            return (PassiveCDBar)UIPackage.CreateObject("SG_Battle", "PassiveCDBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
        }
    }
}