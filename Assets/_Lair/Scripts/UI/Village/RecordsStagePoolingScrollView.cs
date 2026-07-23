using ChvjUnityInfra;

namespace Lair.UI
{
    //# 기록 스테이지 리스트 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class RecordsStagePoolingScrollView : CHPoolingScrollView<RecordsStageCell, RecordsStageCellData>
    {
        public override void InitItem(RecordsStageCell item, RecordsStageCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(RecordsStageCell item)
        {
        }
    }
}
