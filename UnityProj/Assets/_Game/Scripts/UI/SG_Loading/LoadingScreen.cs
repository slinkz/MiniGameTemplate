/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Loading
{
    public partial class LoadingScreen : GComponent
    {
        public GGraph bg;
        public GGraph logo_placeholder;
        public GTextField text_title;
        public LoadingBar bar;
        public GTextField text_loading;
        public const string URL = "ui://sg01ld02gen_01";

        public static LoadingScreen CreateInstance()
        {
            return (LoadingScreen)UIPackage.CreateObject("SG_Loading", "LoadingScreen");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            logo_placeholder = (GGraph)GetChild("logo_placeholder");
            text_title = (GTextField)GetChild("text_title");
            bar = (LoadingBar)GetChild("bar");
            text_loading = (GTextField)GetChild("text_loading");
        }
    }
}