using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class BuildModalPopupArg : UIArg
    {
        public BattleViewModel ViewModel;
    }

    //# 화면 중앙 모달 — 픽한 모든 카드 표시. 좌(패시브) : 우(액티브) 50:50.
    //# 패시브 섹션은 카테고리 그룹(Enhance→Spawn→Replace→Environment), 액티브는 픽 시간 순 (기획서 §2.7.4).
    //# v0.8 — Rule 11 완전 적용. 각 섹션이 BuildModalCardPoolingScrollView 1세트.
    //# 기존 `_spawnedCells` List + `CHMPool.Push` 루프 + `_cellPrefab` 직렬화 + nested BuildModalCardCell 모두 제거.
    public class BuildModalPopup : UIBase
    {
        [SerializeField] private CHButton _dimButton;                                //# 전체 화면 dim (#000 α=0.6) CHButton — 클릭 시 닫힘
        [SerializeField] private CHButton _closeButton;                              //# 우상단 X
        [SerializeField] private BuildModalCardPoolingScrollView _passiveScrollView; //# 좌 섹션 CHPoolingScrollView
        [SerializeField] private BuildModalCardPoolingScrollView _activeScrollView;  //# 우 섹션 CHPoolingScrollView
        [SerializeField] private CHText _passiveEmptyText;                           //# 빈 상태 라벨 (패시브)
        [SerializeField] private CHText _activeEmptyText;                            //# 빈 상태 라벨 (액티브)

        public override void InitUI(UIArg arg)
        {
            if (arg is BuildModalPopupArg ma && ma.ViewModel != null)
            {
                var vm = ma.ViewModel;
                Build(vm);
                //# 팝업 열려있는 동안 카드 픽 시 자동 갱신.
                System.Action refresh = () => Build(vm);
                vm.OnBuildChanged += refresh;
                closeDisposable.Add(() => vm.OnBuildChanged -= refresh);
            }

            //# 배경 dim 클릭 / X 버튼 클릭 → 닫힘.
            if (_dimButton != null)
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);
        }

        private void Build(BattleViewModel vm)
        {
            var entries = vm.Build;
            //# 패시브 / 액티브 분리.
            var passive = new List<BattleViewModel.BuildEntry>();
            var active  = new List<BattleViewModel.BuildEntry>();
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e == null || e.Card == null) continue;
                    if (e.IsPassive) passive.Add(e);
                    else             active.Add(e);
                }
            }

            //# 패시브 — 카테고리 그룹화 (Enhance → Spawn → Replace → Environment), 그룹 내 픽 시간 순.
            passive.Sort((a, b) =>
            {
                int oa = CategoryOrder(a.Card.Category);
                int ob = CategoryOrder(b.Card.Category);
                return oa.CompareTo(ob);
            });

            //# 액티브 — 픽 시간 순 (리스트 추가 순서 그대로 유지). 별도 정렬 안 함.

            //# 빈 상태 라벨.
            if (_passiveEmptyText != null) _passiveEmptyText.gameObject.SetActive(passive.Count == 0);
            if (_activeEmptyText  != null) _activeEmptyText.gameObject.SetActive(active.Count == 0);

            //# CHPoolingScrollView 가 자체로 풀 인스턴스 생성·재바인딩 처리 — 단일 호출.
            if (_passiveScrollView != null) _passiveScrollView.SetItemList(passive);
            if (_activeScrollView  != null) _activeScrollView.SetItemList(active);
        }

        private static int CategoryOrder(ECardCategory c) => c switch
        {
            ECardCategory.Enhance     => 0,
            ECardCategory.Spawn       => 1,
            ECardCategory.Replace     => 2,
            ECardCategory.Environment => 3,
            _                         => 99,
        };
    }
}
