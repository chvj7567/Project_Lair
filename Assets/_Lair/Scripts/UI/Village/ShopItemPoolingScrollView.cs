using ChvjUnityInfra;

namespace Lair.UI
{
    //# 상점 품목 리스트 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class ShopItemPoolingScrollView : CHPoolingScrollView<ShopItemCell, ShopItemCellData>
    {
        //# 현재 풀에 떠있는 최상단 인덱스 — 구매 후 스크롤 위치 복원용. 빈 풀이면 0.
        public int FirstVisibleIndex => liPoolItem.Count > 0 ? liPoolItem.First.Value.index : 0;

        public override void InitItem(ShopItemCell item, ShopItemCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(ShopItemCell item)
        {
        }
    }
}
