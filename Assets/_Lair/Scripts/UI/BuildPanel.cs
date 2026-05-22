using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;

namespace Lair.UI
{
    //# HUD 하위 컴포넌트 — 픽한 카드를 패시브/액티브 섹션에 아이콘으로 표시. BattleHud 가 Bind.
    public class BuildPanel : MonoBehaviour
    {
        [SerializeField] private Transform _passiveContainer;
        [SerializeField] private Transform _activeContainer;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _detailRoot;
        [SerializeField] private CHText _detailName;
        [SerializeField] private CHText _detailDesc;

        private BattleViewModel _vm;
        private readonly Dictionary<CardData, BuildIconCell> _cells = new();
        private CardData _detailShown;

        //# BattleHud.Bind 가 호출 — VM 구독 + 초기 동기화.
        public void Bind(BattleViewModel vm)
        {
            _vm = vm;
            if (_detailRoot != null) _detailRoot.SetActive(false);
            vm.OnBuildChanged += Refresh;
            Refresh();
        }

        //# BattleHud.closeDisposable 가 호출 — 구독 해제 + 셀 참조 정리.
        public void Unbind()
        {
            if (_vm != null) _vm.OnBuildChanged -= Refresh;
            _cells.Clear();
        }

        //# vm.Build 재조회 후 셀 정합 — 신규 카드는 셀 생성, 기존은 카운트 갱신.
        private void Refresh()
        {
            if (_vm == null) return;
            foreach (var entry in _vm.Build)
            {
                if (_cells.TryGetValue(entry.Card, out var cell) == false)
                {
                    var parent = entry.IsPassive ? _passiveContainer : _activeContainer;
                    var poolable = CHMPool.Instance.Pop(_cellPrefab, parent);
                    cell = poolable.GetComponent<BuildIconCell>();
                    var captured = entry.Card;
                    cell.Bind(captured, () => ShowDetail(captured));
                    _cells[entry.Card] = cell;
                }
                cell.SetCount(entry.Count);
            }
        }

        //# 셀 클릭 — 같은 카드 재클릭 시 닫힘, 아니면 해당 카드 상세 표시.
        private void ShowDetail(CardData card)
        {
            if (_detailRoot == null || card == null) return;
            if (_detailShown == card && _detailRoot.activeSelf)
            {
                _detailRoot.SetActive(false);
                _detailShown = null;
                return;
            }
            _detailShown = card;
            if (_detailName != null) _detailName.SetText(card.DisplayName);
            if (_detailDesc != null) _detailDesc.SetText(card.Description);
            _detailRoot.SetActive(true);
        }
    }
}
