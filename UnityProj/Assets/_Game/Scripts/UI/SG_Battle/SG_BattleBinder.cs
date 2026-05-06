/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;

namespace SG_Battle
{
    public class SG_BattleBinder
    {
        public static void BindAll()
        {
            UIObjectFactory.SetPackageItemExtension(BattleHUD.URL, typeof(BattleHUD));
            UIObjectFactory.SetPackageItemExtension(FloatingText.URL, typeof(FloatingText));
            UIObjectFactory.SetPackageItemExtension(Joystick.URL, typeof(Joystick));
            UIObjectFactory.SetPackageItemExtension(HPBar.URL, typeof(HPBar));
            UIObjectFactory.SetPackageItemExtension(PauseButton.URL, typeof(PauseButton));
        }
    }
}