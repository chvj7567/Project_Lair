using ChvjUnityInfra;

namespace Lair.UI
{
    //# Rule 03 §3 — CHPoolingScrollView 상속. InitItem/InitPoolingObject 만 오버라이드.
    public class SynergyModalCardPoolingScrollView
        : CHPoolingScrollView<SynergyModalCell, SynergyModalCellData>
    {
        public override void InitItem(SynergyModalCell item, SynergyModalCellData data, int index)
        {
            if (item == null || data == null) return;
            item.Bind(data);
        }

        //# 풀 인스턴스 1회 초기화. Bind 가 매 InitItem 마다 표시 상태를 재결정하므로 추가 작업 불요.
        public override void InitPoolingObject(SynergyModalCell item)
        {
        }
    }
}
