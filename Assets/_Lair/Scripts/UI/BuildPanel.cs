using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# HUD 하위 컴포넌트 — 픽한 카드를 패시브/액티브 섹션에 아이콘으로 표시. BattleHud 가 Bind.
    //# 종 강화 패시브 6장 (Enhance + IsPassive) 은 셀 표시에서 제외 — 스포너 상태 셀에 노출되므로.
    //# 패널 루트 클릭 시 BuildModalPopup 으로 픽한 모든 카드(강화 포함) 표시 (기획서 §2.6.3).
    public class BuildPanel : MonoBehaviour
    {
        [SerializeField] private Transform _passiveContainer;
        [SerializeField] private Transform _activeContainer;
        [SerializeField] private GameObject _cellPrefab;
        //# 패널 루트 클릭 → BuildModalPopup 호출.
        [SerializeField] private CHButton _rootButton;

        private BattleViewModel _vm;
        private readonly Dictionary<CardData, BuildIconCell> _cells = new();
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

        //# BattleHud.closeDisposable 가 호출 — 구독 해제 + 셀 참조 정리.
        public void Unbind()
        {
            if (_vm != null) _vm.OnBuildChanged -= Refresh;
            _vm = null;
            _disposable.Clear();
            _cells.Clear();
        }

        //# vm.Build 재조회 후 셀 정합 — 신규 카드는 셀 생성, 기존은 카운트 갱신.
        private void Refresh()
        {
            if (_vm == null) return;
            foreach (var entry in _vm.Build)
            {
                //# 종 강화 패시브 6장 (Enhance + IsPassive) 제외 (기획서 §2.6.1, design-reviewer BLOCKER 1).
                //# 액티브 4장(Berserk/BloodThirst/Frenzy/IronWill) 도 _category 가 Enhance 로 직렬화돼 있지만
                //# IsPassive = false 라 통과한다.
                if (entry.Card.Category == ECardCategory.Enhance && entry.IsPassive) continue;

                if (_cells.TryGetValue(entry.Card, out var cell) == false)
                {
                    var parent = entry.IsPassive ? _passiveContainer : _activeContainer;
                    var poolable = CHMPool.Instance.Pop(_cellPrefab, parent);
                    cell = poolable.GetComponent<BuildIconCell>();
                    //# 자식 셀 클릭 콜백 제거 — 패널 루트가 모달을 띄움 (기획서 §2.6.2).
                    cell.Bind(entry.Card, null);
                    _cells[entry.Card] = cell;
                }
                cell.SetCount(entry.Count);
            }
        }
    }
}
