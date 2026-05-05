/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class HPBar : GProgressBar
    {
        public GGraph bg;
        public const string URL = "ui://sg03bt04gen_05";

        public static HPBar CreateInstance()
        {
            return (HPBar)UIPackage.CreateObject("SG_Battle", "HPBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
        }
    }
}