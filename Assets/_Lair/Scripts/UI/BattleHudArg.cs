using ChvjUnityInfra;

namespace Lair.UI
{
    //# CHMUI.ShowUI 호출 시 BattleHud 에 ViewModel 주입용.
    public class BattleHudArg : UIArg
    {
        public BattleViewModel ViewModel;
    }
}
