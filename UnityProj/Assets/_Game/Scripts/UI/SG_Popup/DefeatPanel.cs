/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Popup
{
    public partial class DefeatPanel : GComponent
    {
        public GGraph mask;
        public GGraph panel_bg;
        public GTextField text_defeat;
        public GGraph divider;
        public GTextField text_progress;
        public GTextField text_encourage;
        public SG_PopupButton btn_retry;
        public SG_SecondaryButton btn_quit;
        public const string URL = "ui://sg04pp05gen_03";

        public static DefeatPanel CreateInstance()
        {
            return (DefeatPanel)UIPackage.CreateObject("SG_Popup", "DefeatPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            mask = (GGraph)GetChild("mask");
            panel_bg = (GGraph)GetChild("panel_bg");
            text_defeat = (GTextField)GetChild("text_defeat");
            divider = (GGraph)GetChild("divider");
            text_progress = (GTextField)GetChild("text_progress");
            text_encourage = (GTextField)GetChild("text_encourage");
            btn_retry = (SG_PopupButton)GetChild("btn_retry");
            btn_quit = (SG_SecondaryButton)GetChild("btn_quit");
        }
    }
}