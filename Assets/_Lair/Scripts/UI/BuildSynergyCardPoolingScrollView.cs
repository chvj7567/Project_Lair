using ChvjUnityInfra;

namespace Lair.UI
{
    //# BuildSynergyPanel 의 4축 시너지 셀 리스트 — CHPoolingScrollView<BuildSynergyCell, BuildSynergyCellData>.
    //# Rule 11 v0.8 — BuildModalCardPoolingScrollView / BuildIconPoolingScrollView 와 동일 패턴.
    public class BuildSynergyCardPoolingScrollView
        : CHPoolingScrollView<BuildSynergyCell, BuildSynergyCellData>
    {
        public override void InitItem(BuildSynergyCell item, BuildSynergyCellData data, int index)
        {
            if (item == null || data == null) return;
            item.Bind(data);
            if (data.JustCrossed) item.Pulse();
        }

        //# 풀 인스턴스 1회 초기화. BuildSynergyCell 의 OnEnable 이 코루틴/배경 알파 reset.
        public override void InitPoolingObject(BuildSynergyCell item)
        {
        }
    }
}
