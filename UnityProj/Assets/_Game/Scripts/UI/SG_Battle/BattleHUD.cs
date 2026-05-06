/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class BattleHUD : GComponent
    {
        public GGraph bg;
        public PauseButton btn_pause;
        public GTextField text_wave;
        public HPBar hp_bar;
        public GTextField text_hp_pct;
        public GGraph red_flash;
        public const string URL = "ui://sg03bt04gen_01";

        public static BattleHUD CreateInstance()
        {
            return (BattleHUD)UIPackage.CreateObject("SG_Battle", "BattleHUD");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            btn_pause = (PauseButton)GetChild("btn_pause");
            text_wave = (GTextField)GetChild("text_wave");
            hp_bar = (HPBar)GetChild("hp_bar");
            text_hp_pct = (GTextField)GetChild("text_hp_pct");
            red_flash = (GGraph)GetChild("red_flash");
        }
    }
}