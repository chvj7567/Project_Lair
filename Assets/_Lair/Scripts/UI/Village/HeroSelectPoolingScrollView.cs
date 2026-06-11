using ChvjUnityInfra;

namespace Lair.UI
{
    //# 영웅 선택 그리드 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class HeroSelectPoolingScrollView : CHPoolingScrollView<HeroSelectCell, HeroSelectCellData>
    {
        public override void InitItem(HeroSelectCell item, HeroSelectCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(HeroSelectCell item)
        {
        }
    }
}
