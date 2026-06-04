/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class PassiveSlot : GComponent
    {
        public Controller state;
        public GGraph bg;
        public PassiveCDBar cd_progress;
        public PassiveActiveBar active_progress;
        public const string URL = "ui://sg03bt04gen_11";

        public static PassiveSlot CreateInstance()
        {
            return (PassiveSlot)UIPackage.CreateObject("SG_Battle", "PassiveSlot");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            state = GetController("state");
            bg = (GGraph)GetChild("bg");
            cd_progress = (PassiveCDBar)GetChild("cd_progress");
            active_progress = (PassiveActiveBar)GetChild("active_progress");
        }
    }
}