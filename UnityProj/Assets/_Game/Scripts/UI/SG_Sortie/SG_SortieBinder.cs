/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;

namespace SG_Sortie
{
    public class SG_SortieBinder
    {
        public static void BindAll()
        {
            UIObjectFactory.SetPackageItemExtension(SkillCard.URL, typeof(SkillCard));
            UIObjectFactory.SetPackageItemExtension(PassiveCard.URL, typeof(PassiveCard));
            UIObjectFactory.SetPackageItemExtension(BtnSortie.URL, typeof(BtnSortie));
            UIObjectFactory.SetPackageItemExtension(SortieBottomSheet.URL, typeof(SortieBottomSheet));
        }
    }
}