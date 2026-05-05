/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace SG_Battle
{
    public partial class FloatingText : GComponent
    {
        public GTextField text;
        public const string URL = "ui://sg03bt04gen_02";

        public static FloatingText CreateInstance()
        {
            return (FloatingText)UIPackage.CreateObject("SG_Battle", "FloatingText");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            text = (GTextField)GetChild("text");
        }
    }
}