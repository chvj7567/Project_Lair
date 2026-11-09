using ChvjUnityInfra;

namespace Lair.UI
{
    //# 도감 몬스터 매트릭스 — 6종족이 하나의 풀링 스크롤뷰를 공유(Rule 03 §3 3-class). 종족 전환 시 SetItemList 로 다시 채운다.
    public class CodexMatrixPoolingScrollView : CHPoolingScrollView<CodexMonsterCell, CodexMonsterCellData>
    {
        public override void InitItem(CodexMonsterCell item, CodexMonsterCellData data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(CodexMonsterCell item)
        {
        }
    }
}
