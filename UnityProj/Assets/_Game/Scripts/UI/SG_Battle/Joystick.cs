/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class Joystick : GComponent
    {
        public GGraph base_circle;
        public GGraph stick;
        public const string URL = "ui://sg03bt04gen_03";

        public static Joystick CreateInstance()
        {
            return (Joystick)UIPackage.CreateObject("SG_Battle", "Joystick");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            base_circle = (GGraph)GetChild("base_circle");
            stick = (GGraph)GetChild("stick");
        }
    }
}