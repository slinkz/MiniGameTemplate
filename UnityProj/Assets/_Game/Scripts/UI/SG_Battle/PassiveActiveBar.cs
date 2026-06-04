/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class PassiveActiveBar : GProgressBar
    {
        public GGraph bg;
        public const string URL = "ui://sg03bt04gen_09";

        public static PassiveActiveBar CreateInstance()
        {
            return (PassiveActiveBar)UIPackage.CreateObject("SG_Battle", "PassiveActiveBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
        }
    }
}