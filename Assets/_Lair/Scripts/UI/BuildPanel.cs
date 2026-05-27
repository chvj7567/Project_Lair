using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# HUD 하위 컴포넌트 — 픽한 카드를 패시브/액티브 섹션에 아이콘으로 표시. BattleHud 가 Bind.
    //# 종 강화 패시브 6장 (Enhance + IsPassive) 은 셀 표시에서 제외 — 스포너 상태 셀에 노출되므로.
    //# 패널 루트 클릭 시 BuildModalPopup 으로 픽한 모든 카드(강화 포함) 표시 (기획서 §2.6.3).
    //# v0.8 — Rule 11 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙 완전 적용.
    //# 기존 `_cells` Dict + `CHMPool.Pop` 분기 + `_cellPrefab` 직렬화 제거, 두 섹션을 BuildIconPoolingScrollView 로 통일.
    public class BuildPanel : MonoBehaviour
    {
        [SerializeField] private BuildIconPoolingScrollView _passiveScrollView;
        [SerializeField] private BuildIconPoolingScrollView _activeScrollView;
        //# 패널 루트 클릭 → BuildModalPopup 호출.
        [SerializeField] private CHButton _rootButton;

        private BattleViewModel _vm;
        //# 루트 버튼 listener 수명 관리.
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        //# BattleHud.Bind 가 호출 — VM 구독 + 초기 동기화.
        public void Bind(BattleViewModel vm)
        {
            _vm = vm;
            vm.OnBuildChanged += Refresh;
            Refresh();

            //# 루트 클릭 → BuildModalPopup. CHMUI 가 단일 인스턴스 caching 으로 재사용.
            if (_rootButton != null)
            {
                _rootButton.OnClick(() =>
                {
                    if (_vm == null) return;
                    CHMUI.Instance.ShowUI(EUI.BuildModalPopup, new BuildModalPopupArg { ViewModel = _vm });
                }, _disposable);
            }
        }

        //# BattleHud.closeDisposable 가 호출 — 구독 해제 + 리스너 정리.
        public void Unbind()
        {
            if (_vm != null) _vm.OnBuildChanged -= Refresh;
            _vm = null;
            _disposable.Clear();
        }

        //# vm.Build 재조회 후 필터·분할해 두 ScrollView 각각에 단일 호출로 push.
        //# CHPoolingScrollView 가 자체로 풀 인스턴스 생성·재바인딩 처리.
        private void Refresh()
        {
            if (_vm == null) return;

            var passive = new List<BattleViewModel.BuildEntry>();
            var active  = new List<BattleViewModel.BuildEntry>();
            foreach (var entry in _vm.Build)
            {
                if (entry == null || entry.Card == null) continue;

                //# 종 강화 패시브 6장 (Enhance + IsPassive) 제외 (기획서 §2.6.1).
                //# 액티브 4장(Berserk/BloodThirst/Frenzy/IronWill) 도 _category 가 Enhance 로 직렬화돼 있지만
                //# IsPassive = false 라 통과한다.
                if (entry.Card.Category == ECardCategory.Enhance && entry.IsPassive) continue;

                if (entry.IsPassive) passive.Add(entry);
                else                 active.Add(entry);
            }

            if (_passiveScrollView != null) _passiveScrollView.SetItemList(passive);
            if (_activeScrollView  != null) _activeScrollView.SetItemList(active);
        }
    }
}
