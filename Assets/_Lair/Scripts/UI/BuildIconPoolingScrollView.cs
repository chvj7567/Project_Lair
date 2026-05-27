using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# BuildPanel 의 PassiveSection / ActiveSection 카드 아이콘 리스트 —
    //# CHPoolingScrollView<BuildIconCell, BuildEntry>.
    //# Rule 11 v0.8 — `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙 완전 적용.
    //# 기존 `_cells` Dict + `CHMPool.Pop` 수동 풀링 폐기, 본 컴포넌트의 자체 풀링 사용.
    public class BuildIconPoolingScrollView
        : CHPoolingScrollView<BuildIconCell, BattleViewModel.BuildEntry>
    {
        public override void InitItem(BuildIconCell item, BattleViewModel.BuildEntry data, int index)
        {
            if (item == null || data == null) return;
            //# 패널 루트가 모달을 띄움 (기획서 §2.6.2) — 자식 셀은 onClick null.
            item.Bind(data.Card, null);
            item.SetCount(data.Count);
        }

        //# 풀 인스턴스 1회 초기화. BuildIconCell 의 OnEnable 이 상태를 reset 하므로 추가 작업 불요.
        public override void InitPoolingObject(BuildIconCell item)
        {
        }
    }
}
