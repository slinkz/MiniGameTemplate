/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Common
{
    public partial class GlobalSpinner : GComponent
    {
        public GGraph overlay;
        public GGraph spinnerIcon;
        public GTextField txtHint;
        public Transition spin;
        public const string URL = "ui://cm01ab02gen_06";

        public static GlobalSpinner CreateInstance()
        {
            return (GlobalSpinner)UIPackage.CreateObject("Common", "GlobalSpinner");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            overlay = (GGraph)GetChild("overlay");
            spinnerIcon = (GGraph)GetChild("spinnerIcon");
            txtHint = (GTextField)GetChild("txtHint");
            spin = GetTransition("spin");
        }
    }
}