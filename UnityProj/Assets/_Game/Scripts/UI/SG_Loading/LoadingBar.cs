/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Loading
{
    public partial class LoadingBar : GProgressBar
    {
        public GGraph bg;
        public const string URL = "ui://sg01ld02gen_02";

        public static LoadingBar CreateInstance()
        {
            return (LoadingBar)UIPackage.CreateObject("SG_Loading", "LoadingBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
        }
    }
}