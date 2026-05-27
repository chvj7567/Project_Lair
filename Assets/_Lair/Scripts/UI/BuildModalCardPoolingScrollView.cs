using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# 모달 좌/우 섹션의 카드 리스트 — CHPoolingScrollView<BuildModalCardCell, BuildEntry>.
    //# Rule 11 v0.8 — `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙 완전 적용.
    //# 기존 `CHMPool.Pop` + `_spawnedCells` List 수동 풀링 폐기, 본 컴포넌트의 자체 풀링 사용.
    public class BuildModalCardPoolingScrollView
        : CHPoolingScrollView<BuildModalCardCell, BattleViewModel.BuildEntry>
    {
        public override void InitItem(BuildModalCardCell item, BattleViewModel.BuildEntry data, int index)
        {
            if (item == null || data == null) return;
            item.Bind(data);
        }

        //# 풀 인스턴스 1회 초기화. BuildModalCardCell 의 OnEnable 이 ×N 텍스트를 reset 하므로 추가 작업 불요.
        public override void InitPoolingObject(BuildModalCardCell item)
        {
        }
    }
}
