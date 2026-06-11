using ChvjUnityInfra;

namespace Lair.UI
{
    //# 영주 보상 트랙 리스트 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class LordRewardPoolingScrollView : CHPoolingScrollView<LordRewardCell, LordRewardCellData>
    {
        public override void InitItem(LordRewardCell item, LordRewardCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(LordRewardCell item)
        {
        }
    }
}
