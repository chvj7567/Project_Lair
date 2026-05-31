using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

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

        //# OnEnable / OnBuildChanged 양쪽이 참조하므로 필드로 보관.
        private BattleViewModel _vm;

        public override void InitUI(UIArg arg)
        {
            //# Build 첫 호출은 OnEnable 또는 (prefab active 케이스의) InitUI 끝 분기가 담당.
            //# 이유: CHMUI.ActivateUI 는 InitUI → SetActive(true) 순서다. prefab 이 inactive 로 저장된
            //# 경우엔 InitUI 시점 GameObject 가 비활성이고, 이때 SetItemList 를 부르면 viewport.rect 가
            //# layout 미산정 상태(0)라 CHPoolingScrollView 의 _poolItemCount / Content 크기 계산이
            //# 0 기준으로 굳어버린다. 그 결과 다음 픽(첫 픽)으로 SetItemList 가 재호출돼도 굳은 풀 크기·
            //# Content 크기 때문에 첫 카드가 화면에 표시되지 않는 버그가 발생 → OnEnable 가 처리.
            //# prefab 이 active 로 저장된 경우엔 SetActive(true) 가 no-op 라 OnEnable 이 발화하지 않으므로
            //# InitUI 끝의 isActiveAndEnabled 분기가 BuildAndLayout 을 직접 호출한다.
            if (arg is BuildModalPopupArg ma && ma.ViewModel != null)
            {
                _vm = ma.ViewModel;
                //# 팝업 열려있는 동안 카드 픽 시 자동 갱신.
                System.Action refresh = HandleBuildChanged;
                _vm.OnBuildChanged += refresh;
                BattleViewModel vmRef = _vm;
                closeDisposable.Add(() => vmRef.OnBuildChanged -= refresh);
                //# Close(reuse: true) 시 _vm 가 그대로 남아있으면 다음 ShowUI 때 잘못된 vm 으로
                //# Build 가 돌 수 있으므로 close 직후 null 화. closeDisposable 가 Init() 마다 새로 생성되므로
                //# 이 람다는 이번 lifecycle 종료 시점에만 실행된다.
                closeDisposable.Add(() => _vm = null);
            }

            //# 배경 dim 클릭 / X 버튼 클릭 → 닫힘.
            if (_dimButton != null)
                _dimButton.OnClick(() => Close(reuse: true), closeDisposable);
            if (_closeButton != null)
                _closeButton.OnClick(() => Close(reuse: true), closeDisposable);

            //# prefab 이 active 상태로 저장된 경우 (BuildModalPopup.prefab 의 root m_IsActive: 1) 보강.
            //# CHMUI.ActivateUI 는 InitUI → SetActive(true) 순서인데, prefab 이 이미 active 면
            //# Object.Instantiate 직후 OnEnable 이 1회 발화 (이때 _vm null 가드로 skip) → SetParent →
            //# Init → InitUI → SetActive(true) 가 no-op (이미 active) → OnEnable 재발화 없음.
            //# 결과로 BuildAndLayout 가 한 번도 호출되지 않아 첫 열림에서 누적 카드가 빈 화면으로 표시됨.
            //# 이 시점 GameObject 는 이미 active + parent (CHMUI root) 도 active 라
            //# isActiveAndEnabled == true → 직접 호출해도 layout 산정·Build 정상 동작.
            //# (BuildPanel / SpawnerStatusTooltip 의 동일 분기와 같은 패턴.)
            if (isActiveAndEnabled)
            {
                BuildAndLayout();
            }
        }

        //# 활성화 직후 layout 산정 + 최초 Build. 재오픈(cached UI 재사용) 시에도 매번 호출돼
        //# Pause 동안 누적된 픽이 그대로 반영된다.
        private void OnEnable()
        {
            if (_vm == null)
                return;
            BuildAndLayout();
        }

        //# 모달 루트 RectTransform 강제 산정 후 Build. InitUI(prefab active 케이스)·OnEnable 양쪽에서 호출.
        //# 모달 루트 한 번 ForceRebuildLayoutImmediate 면 viewport 까지 chain 으로 산정됨 — CHPoolingScrollView
        //# 가 viewport.rect 기준으로 풀 크기·Content 크기를 정상 계산한다.
        private void BuildAndLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Build(_vm);
        }

        //# OnBuildChanged 람다 분리 — 매번 새 람다 캡처 회피 + close 시 unsubscribe 확실히.
        private void HandleBuildChanged()
        {
            if (_vm == null)
                return;
            Build(_vm);
        }

        private void Build(BattleViewModel vm)
        {
            IReadOnlyList<BattleViewModel.BuildEntry> entries = vm.Build;
            //# 패시브 / 액티브 분리.
            List<BattleViewModel.BuildEntry> passive = new List<BattleViewModel.BuildEntry>();
            List<BattleViewModel.BuildEntry> active  = new List<BattleViewModel.BuildEntry>();
            if (entries != null)
            {
                foreach (BattleViewModel.BuildEntry e in entries)
                {
                    if (e == null || e.Card == null) continue;
                    if (e.IsPassive) passive.Add(e);
                    else             active.Add(e);
                }
            }

            //# 패시브 — 카테고리 그룹화 (Tank → Dps → Debuff → Swarm), 그룹 내 픽 시간 순.
            //# 카드 리뉴얼 v0.6 — 구 카테고리 → EBuildAxis 자리 치환 (Phase 1 임시).
            passive.Sort((a, b) =>
            {
                int oa = CategoryOrder(a.Card.Axis);
                int ob = CategoryOrder(b.Card.Axis);
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

        //# 카드 리뉴얼 v0.6 — 구 카테고리 → EBuildAxis 자리 치환 (Phase 1 임시).
        private static int CategoryOrder(EBuildAxis c) => c switch
        {
            EBuildAxis.Tank   => 0,
            EBuildAxis.Dps    => 1,
            EBuildAxis.Debuff => 2,
            EBuildAxis.Swarm  => 3,
            _                 => 99,
        };
    }
}
