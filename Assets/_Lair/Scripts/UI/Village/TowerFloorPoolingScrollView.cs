using ChvjUnityInfra;
using Lair.Village;

namespace Lair.UI
{
    //# 탑 층 사다리 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class TowerFloorPoolingScrollView : CHPoolingScrollView<TowerFloorCell, TowerFloorCellData>
    {
        public override void InitItem(TowerFloorCell item, TowerFloorCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(TowerFloorCell item)
        {
        }

        //# 현재 보이는 셀만 다시 그린다 — SetItemList 와 달리 스크롤 위치를 유지한다.
        public void RefreshVisible()
        {
            foreach (TowerFloorCell cell in ItemList)
            {
                if (cell != null)
                {
                    cell.Refresh();
                }
            }
        }
    }
}
