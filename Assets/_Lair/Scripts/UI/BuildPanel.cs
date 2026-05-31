using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# HUD 하위 컴포넌트 — 픽한 카드를 패시브/액티브 섹션에 아이콘으로 표시. BattleHud 가 Bind.
    //# 패널 루트 클릭 시 BuildModalPopup 으로 픽한 모든 카드 표시 (기획서 §2.6.3).
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
        //# Refresh 첫 호출은 OnEnable 로 미룬다. BattleHud 가 CHMUI 로 띄워지는 UIBase 이고
        //# ActivateUI 는 InitUI → SetActive(true) 순서라 Bind 시점엔 BuildPanel 의 GameObject 가
        //# 비활성 + layout 미산정 상태. 이때 CHPoolingScrollView.SetItemList 를 부르면 viewport.rect = 0
        //# 기준으로 _poolItemCount / Content 크기가 굳어 다음 픽에 첫 카드가 안 보이는 버그가 생긴다.
        //# (BuildModalPopup 과 동일 root cause.)
        public void Bind(BattleViewModel vm)
        {
            _vm = vm;
            vm.OnBuildChanged += Refresh;

            //# 루트 클릭 → BuildModalPopup. CHMUI 가 단일 인스턴스 caching 으로 재사용.
            if (_rootButton != null)
            {
                _rootButton.OnClick(() =>
                {
                    if (_vm == null) return;
                    CHMUI.Instance.ShowUI(EUI.BuildModalPopup, new BuildModalPopupArg { ViewModel = _vm });
                }, _disposable);
            }

            //# 이미 활성 상태면 즉시 Refresh (재바인딩 케이스). 그렇지 않으면 OnEnable 이 처리.
            if (isActiveAndEnabled)
            {
                RefreshWithLayout();
            }
        }

        //# 활성화 직후 layout 산정 + 동기화. BattleHud 가 SetActive(true) 되며 호출됨.
        private void OnEnable()
        {
            if (_vm == null)
                return;
            RefreshWithLayout();
        }

        //# 첫 SetItemList 가 viewport rect 0 으로 굳어지는 것을 막기 위해 ForceRebuildLayoutImmediate 선행.
        private void RefreshWithLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            Refresh();
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

            List<BattleViewModel.BuildEntry> passive = new List<BattleViewModel.BuildEntry>();
            List<BattleViewModel.BuildEntry> active  = new List<BattleViewModel.BuildEntry>();
            foreach (BattleViewModel.BuildEntry entry in _vm.Build)
            {
                if (entry == null || entry.Card == null) continue;

                if (entry.IsPassive) passive.Add(entry);
                else                 active.Add(entry);
            }

            if (_passiveScrollView != null) _passiveScrollView.SetItemList(passive);
            if (_activeScrollView  != null) _activeScrollView.SetItemList(active);
        }
    }
}
