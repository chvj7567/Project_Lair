using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# HUD 하위 컴포넌트 — 픽한 카드를 패시브/액티브 섹션에 아이콘으로 표시. BattleHud 가 Bind.
    //# 패널 루트 클릭 시 BuildModalPopup 으로 픽한 모든 카드 표시 (기획서 §2.6.3).
    public class BuildPanel : MonoBehaviour
    {
        //# 빌드 바 최소 슬롯 — 획득 전에도 빈 빗금 슬롯으로 채워 보여 주고, 획득이 최소치를 넘으면 늘어난다(기획서 §6.2-8).
        public const int MinPassiveSlots = 9;
        public const int MinActiveSlots = 5;

        //# 빈 슬롯 수 — 채워진 칸이 최소치 이상이면 0.
        public static int EmptySlotCount(int filled, int minSlots)
        {
            return Mathf.Max(0, minSlots - filled);
        }

        //# 획득 카드 뒤에 빈 슬롯 placeholder(Card=null)를 최소 슬롯 수까지 채운 리스트.
        public static List<BattleViewModel.BuildEntry> PadWithEmptySlots(List<BattleViewModel.BuildEntry> filled, int minSlots, bool isPassive)
        {
            List<BattleViewModel.BuildEntry> padded = new List<BattleViewModel.BuildEntry>(filled);
            int empty = EmptySlotCount(filled.Count, minSlots);
            for (int i = 0; i < empty; ++i)
            {
                padded.Add(new BattleViewModel.BuildEntry { Card = null, IsPassive = isPassive, Count = 0 });
            }
            return padded;
        }

        [SerializeField] private BuildIconPoolingScrollView _passiveScrollView;
        [SerializeField] private BuildIconPoolingScrollView _activeScrollView;
        //# 패널 루트 클릭 → BuildModalPopup 호출.
        [SerializeField] private CHButton _rootButton;

        //# 접기/펼치기(scene-2d-conversion §4.10) — 본문·접힌 탭 정적 자식. 접어도 앵커 위치는 그대로, 높이만 바뀐다.
        [SerializeField] private GameObject _body;
        [SerializeField] private GameObject _foldedTab;
        [SerializeField] private CHButton _collapseButton;
        [SerializeField] private CHButton _expandButton;
        //# 라벨 우측 "받은 픽 수/총 트리거 수" (본문 + 접힌 탭)
        [SerializeField] private CHText _passiveCountText;
        [SerializeField] private CHText _activeCountText;
        [SerializeField] private CHText _foldedPassiveText;
        [SerializeField] private CHText _foldedActiveText;
        [SerializeField] private float _expandedHeight = 239f;
        [SerializeField] private float _collapsedHeight = 31f;

        private HudLayoutViewModel _layout;
        private System.Action<bool> _layoutHandler;

        //# View 의도 API — 본문 ↔ 접힌 탭 즉시 전환. 펼칠 때 레이아웃을 강제 재계산한 뒤 목록을 1회 갱신(셀 0 크기 방지).
        public void SetCollapsed(bool collapsed)
        {
            if (_body != null)
                _body.SetActive(collapsed == false);
            if (_foldedTab != null)
                _foldedTab.SetActive(collapsed);
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, collapsed ? _collapsedHeight : _expandedHeight);
            }
            if (_rootButton != null)
            {
                _rootButton.Interactable = collapsed == false;
            }
            if (collapsed == false && _vm != null && isActiveAndEnabled)
            {
                RefreshWithLayout();
            }
        }

        private BattleViewModel _vm;
        //# 루트 버튼 listener 수명 관리.
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        //# BattleHud.Bind 가 호출 — VM 구독 + 초기 동기화.
        //# Refresh 첫 호출은 OnEnable 로 미룬다. BattleHud 가 CHMUI 로 띄워지는 UIBase 이고
        public void Bind(BattleViewModel vm, HudLayoutViewModel layout = null)
        {
            _vm = vm;
            vm.OnBuildChanged += Refresh;

            if (layout != null)
            {
                _layout = layout;
                _layoutHandler = SetCollapsed;
                layout.OnBuildCollapsedChanged += _layoutHandler;
                if (_collapseButton != null)
                    _collapseButton.OnClick(layout.ToggleBuild, _disposable);
                if (_expandButton != null)
                    _expandButton.OnClick(layout.ToggleBuild, _disposable);
                SetCollapsed(layout.IsBuildCollapsed);
            }

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
            if (_layout != null && _layoutHandler != null)
            {
                _layout.OnBuildCollapsedChanged -= _layoutHandler;
            }
            _layout = null;
            _layoutHandler = null;
            _vm = null;
            _disposable.Clear();
        }

        //# 라벨 수 = 받은 픽 수 / 총 트리거 수 (패시브 N/9 · 액티브 N/5)
        private void RefreshCounts()
        {
            string passive = $"{_vm.PassivePickCount}/{_vm.PassiveTriggerTotal}";
            string active = $"{_vm.ActivePickCount}/{_vm.ActiveTriggerTotal}";
            if (_passiveCountText != null) _passiveCountText.SetText(passive);
            if (_activeCountText != null) _activeCountText.SetText(active);
            if (_foldedPassiveText != null) _foldedPassiveText.SetText(passive);
            if (_foldedActiveText != null) _foldedActiveText.SetText(active);
        }

        //# vm.Build 재조회 후 필터·분할해 두 ScrollView 각각에 단일 호출로 push.
        //# CHPoolingScrollView 가 자체로 풀 인스턴스 생성·재바인딩 처리.
        private void Refresh()
        {
            if (_vm == null) return;

            RefreshCounts();
            //# 접힌 동안은 본문이 비활성 — 목록 갱신은 펼칠 때 SetCollapsed 가 수행
            if (_body != null && _body.activeInHierarchy == false) return;

            List<BattleViewModel.BuildEntry> passive = new List<BattleViewModel.BuildEntry>();
            List<BattleViewModel.BuildEntry> active  = new List<BattleViewModel.BuildEntry>();
            foreach (BattleViewModel.BuildEntry entry in _vm.Build)
            {
                if (entry == null || entry.Card == null) continue;

                if (entry.IsPassive) passive.Add(entry);
                else                 active.Add(entry);
            }

            if (_passiveScrollView != null) _passiveScrollView.SetItemList(PadWithEmptySlots(passive, MinPassiveSlots, true));
            if (_activeScrollView  != null) _activeScrollView.SetItemList(PadWithEmptySlots(active, MinActiveSlots, false));
        }
    }
}
