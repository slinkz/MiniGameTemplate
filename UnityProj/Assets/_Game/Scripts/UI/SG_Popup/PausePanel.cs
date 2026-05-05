/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Popup
{
    public partial class PausePanel : GComponent
    {
        public GGraph mask;
        public GGraph panel_bg;
        public GTextField text_title;
        public SG_PopupButton btn_resume;
        public SG_SecondaryButton btn_quit;
        public const string URL = "ui://sg04pp05gen_01";

        public static PausePanel CreateInstance()
        {
            return (PausePanel)UIPackage.CreateObject("SG_Popup", "PausePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            mask = (GGraph)GetChild("mask");
            panel_bg = (GGraph)GetChild("panel_bg");
            text_title = (GTextField)GetChild("text_title");
            btn_resume = (SG_PopupButton)GetChild("btn_resume");
            btn_quit = (SG_SecondaryButton)GetChild("btn_quit");
        }
    }
}