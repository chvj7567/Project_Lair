using ChvjUnityInfra;

namespace Lair.UI
{
    //# 도전과제 리스트 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class QuestPoolingScrollView : CHPoolingScrollView<QuestCell, QuestCellData>
    {
        public override void InitItem(QuestCell item, QuestCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(QuestCell item)
        {
        }
    }
}
