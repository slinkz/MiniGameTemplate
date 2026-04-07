/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Example
{
    public partial class ClickCounterPanel : GComponent
    {
        public GGraph bg;
        public GGraph topBar;
        public GTextField txtTitle;
        public GTextField txtHighScore;
        public GGraph scoreCard;
        public GTextField txtScoreLabel;
        public GTextField txtScore;
        public GTextField txtTimerLabel;
        public GTextField txtTimer;
        public MenuIconButton btnTap;
        public GTextField txtHint;
        public MenuIconButton btnBack;
        public MenuIconButton btnRestart;
        public MenuIconButton btnShare;
        public const string URL = "ui://ex04cd05gen_01";

        public static ClickCounterPanel CreateInstance()
        {
            return (ClickCounterPanel)UIPackage.CreateObject("Example", "ClickCounterPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            topBar = (GGraph)GetChild("topBar");
            txtTitle = (GTextField)GetChild("txtTitle");
            txtHighScore = (GTextField)GetChild("txtHighScore");
            scoreCard = (GGraph)GetChild("scoreCard");
            txtScoreLabel = (GTextField)GetChild("txtScoreLabel");
            txtScore = (GTextField)GetChild("txtScore");
            txtTimerLabel = (GTextField)GetChild("txtTimerLabel");
            txtTimer = (GTextField)GetChild("txtTimer");
            btnTap = (MenuIconButton)GetChild("btnTap");
            txtHint = (GTextField)GetChild("txtHint");
            btnBack = (MenuIconButton)GetChild("btnBack");
            btnRestart = (MenuIconButton)GetChild("btnRestart");
            btnShare = (MenuIconButton)GetChild("btnShare");
        }
    }
}