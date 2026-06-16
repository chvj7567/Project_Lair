using ChvjUnityInfra;

namespace Lair.UI
{
    //# 랭킹 풀링 스크롤뷰 — InitItem 만 오버라이드(Rule 03 BuildModal 패턴).
    public class RankingPoolingScrollView : CHPoolingScrollView<RankingCell, RankingRowEntry>
    {
        public override void InitItem(RankingCell item, RankingRowEntry data, int index)
        {
            if (item == null || data == null)
                return;
            item.Bind(data);
        }

        public override void InitPoolingObject(RankingCell item) { }
    }
}
