/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class PickupNotification : GComponent
    {
        public GGraph bg;
        public GTextField text;
        public const string URL = "ui://sg03bt04gen_12";

        public static PickupNotification CreateInstance()
        {
            return (PickupNotification)UIPackage.CreateObject("SG_Battle", "PickupNotification");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            bg = (GGraph)GetChild("bg");
            text = (GTextField)GetChild("text");
        }
    }
}