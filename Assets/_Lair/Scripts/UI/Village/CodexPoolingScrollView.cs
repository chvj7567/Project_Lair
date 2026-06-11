using ChvjUnityInfra;

namespace Lair.UI
{
    //# 도감 그리드 — CHPoolingScrollView 3-class 구조 (Rule 03 §3).
    public class CodexPoolingScrollView : CHPoolingScrollView<CodexCell, CodexCellData>
    {
        public override void InitItem(CodexCell item, CodexCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(CodexCell item)
        {
        }
    }
}
