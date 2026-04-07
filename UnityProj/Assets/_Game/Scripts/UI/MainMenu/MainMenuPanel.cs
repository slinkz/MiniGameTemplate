/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace MainMenu
{
    public partial class MainMenuPanel : GComponent
    {
        public GGraph bg;
        public GGraph avatar;
        public GTextField txtNickname;
        public GTextField txtGameTitle;
        public GTextField txtVersion;
        public MenuIconButton btnStart;
        public MenuIconButton btnSettings;
        public MenuIconButton btnRanking;
        public MenuIconButton btnShare;
        public const string URL = "ui://mm03ef04gen_01";

        public static MainMenuPanel CreateInstance()
        {
            return (MainMenuPanel)UIPackage.CreateObject("MainMenu", "MainMenuPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            avatar = (GGraph)GetChild("avatar");
            txtNickname = (GTextField)GetChild("txtNickname");
            txtGameTitle = (GTextField)GetChild("txtGameTitle");
            txtVersion = (GTextField)GetChild("txtVersion");
            btnStart = (MenuIconButton)GetChild("btnStart");
            btnSettings = (MenuIconButton)GetChild("btnSettings");
            btnRanking = (MenuIconButton)GetChild("btnRanking");
            btnShare = (MenuIconButton)GetChild("btnShare");
        }
    }
}