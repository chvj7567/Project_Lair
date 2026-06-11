using ChvjUnityInfra;

namespace Lair.UI
{
    //# 상점 품목 리스트 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class ShopItemPoolingScrollView : CHPoolingScrollView<ShopItemCell, ShopItemCellData>
    {
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
