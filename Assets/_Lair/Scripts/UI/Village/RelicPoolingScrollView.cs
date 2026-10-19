using ChvjUnityInfra;
using Lair.Village;

namespace Lair.UI
{
    //# 유물 도감 그리드 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class RelicPoolingScrollView : CHPoolingScrollView<RelicCell, RelicCellData>
    {
        public override void InitItem(RelicCell item, RelicCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(RelicCell item)
        {
        }
    }
}
