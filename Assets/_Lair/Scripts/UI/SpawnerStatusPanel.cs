using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# 6셀 컨테이너 — BattleHud 자식. VM.Spawners 구독 + OnSpawnerSnapshotChanged.
    //# 셀 풀링은 CHMPool (Rule 12) — BuildPanel 의 BuildIconCell 패턴 그대로.
    //# 셀 클릭 → SpawnerStatusTooltip toggle 은 본 패널이 _openCellIndex 상태로 결정 (advisor §2).
    public class SpawnerStatusPanel : MonoBehaviour
    {
        [SerializeField] private Transform _container;       //# 셀들이 배치될 부모 (HorizontalLayoutGroup)
        [SerializeField] private GameObject _cellPrefab;     //# SpawnerStatusCell.prefab — 풀링 대상

        private BattleViewModel _vm;
        //# Spawner 6개 — 셀 Progress 폴링용 ISpawnerProgress 참조 (기획서 §4.6).
        //# BattleHud.Bind 가 BattleController 로부터 받아 주입.
        private IReadOnlyList<Spawner> _spawners;
        //# 툴팁이 base 스탯을 표시하기 위한 BalanceConfig (Rule 03 인터페이스/주입 우선).
        private Data.BalanceConfig _balance;

        private readonly List<SpawnerStatusCell> _cells = new();
        //# 현재 툴팁이 열린 셀 인덱스. -1 이면 닫힘.
        private int _openCellIndex = -1;
        //# 현재 열려있는 툴팁 UIBase 인스턴스 — Close() 직접 호출용 (closeDisposable.Clear 보장).
        private UIBase _openTooltipInstance;

        //# BattleHud.Bind 가 호출. spawners / balance 가 null 이면 진행 바·툴팁 base 스탯이 비활성 (graceful).
        public void Bind(BattleViewModel vm, IReadOnlyList<Spawner> spawners, Data.BalanceConfig balance)
        {
            _vm = vm;
            _spawners = spawners;
            _balance = balance;
            if (vm == null) return;

            RebuildAll();

            vm.OnSpawnerSnapshotChanged += HandleSnapshotChanged;
        }

        //# BattleHud.closeDisposable 가 호출 — 구독 해제 + 셀 반환 + 툴팁 닫기.
        public void Unbind()
        {
            if (_vm != null) _vm.OnSpawnerSnapshotChanged -= HandleSnapshotChanged;
            _vm = null;

            //# 열려있던 툴팁 닫기 — closeDisposable.Clear 가 OnClosed 콜백을 발화하므로 instance/index 정리.
            if (_openTooltipInstance != null)
            {
                _openTooltipInstance.Close(reuse: true);
                _openTooltipInstance = null;
            }
            _openCellIndex = -1;

            //# 셀 반환 (CHMPool.Push) — 다음 씬 진입 시 재사용.
            foreach (SpawnerStatusCell cell in _cells)
            {
                if (cell == null) continue;
                CHPoolable poolable = cell.GetComponent<CHPoolable>();
                if (poolable != null) CHMPool.Instance.Push(poolable);
                else                  Destroy(cell.gameObject);
            }
            _cells.Clear();
        }

        //# 6개 셀을 새로 생성·바인딩.
        private void RebuildAll()
        {
            if (_vm == null || _container == null || _cellPrefab == null) return;
            IReadOnlyList<BattleViewModel.SpawnerSnapshot> snapshots = _vm.Spawners;
            if (snapshots == null) return;

            for (int i = 0; i < snapshots.Count; ++i)
            {
                BattleViewModel.SpawnerSnapshot snap = snapshots[i];
                if (snap == null)
                {
                    //# 인덱스 정합 유지 — null slot. HandleSnapshotChanged 가 index 그대로 사용 가능.
                    _cells.Add(null);
                    continue;
                }

                CHPoolable poolable = CHMPool.Instance.Pop(_cellPrefab, _container);
                if (poolable == null)
                {
                    _cells.Add(null);
                    continue;
                }
                SpawnerStatusCell cell = poolable.GetComponent<SpawnerStatusCell>();
                if (cell == null)
                {
                    _cells.Add(null);
                    continue;
                }

                //# Progress 폴링용 ISpawnerProgress 는 직참된 _spawners 에서 인덱스로 매칭.
                ISpawnerProgress progress = null;
                if (_spawners != null && i < _spawners.Count)
                    progress = _spawners[i];

                cell.Bind(snap, progress, HandleCellClicked);
                _cells.Add(cell);
            }
        }

        //# 스냅샷 1개 갱신 — 셀이 같은 인덱스에 있으면 RebindSnapshot.
        private void HandleSnapshotChanged(int index)
        {
            if (_vm == null) return;
            if (index < 0 || index >= _cells.Count) return;
            SpawnerStatusCell cell = _cells[index];
            if (cell == null) return;
            IReadOnlyList<BattleViewModel.SpawnerSnapshot> snapshots = _vm.Spawners;
            if (snapshots == null || index >= snapshots.Count) return;
            cell.RebindSnapshot(snapshots[index]);
        }

        //# 셀 클릭 — 같은 셀 재클릭이면 닫힘, 다른 셀이면 닫고 새로 열림 (기획서 §2.5.6).
        private void HandleCellClicked(int index)
        {
            if (index < 0 || index >= _cells.Count) return;

            if (_openCellIndex == index)
            {
                //# 같은 셀 재클릭 — 닫고 테두리 원복.
                CloseTooltip();
                return;
            }

            //# 다른 셀 클릭 — 이전 셀 비활성 + 새 셀 활성 + 툴팁 갱신.
            if (_openCellIndex >= 0 && _openCellIndex < _cells.Count && _cells[_openCellIndex] != null)
                _cells[_openCellIndex].SetActiveBorder(false);

            _openCellIndex = index;
            if (_cells[index] == null) return;
            _cells[index].SetActiveBorder(true);

            //# 툴팁 표시 — CHMUI 캐싱 재사용. anchorCell 로 셀 RectTransform 전달.
            //# 콜백으로 UIBase 인스턴스를 받아두어 Close() 직접 호출 가능 (closeDisposable.Clear 보장).
            RectTransform rt = _cells[index].transform as RectTransform;
            CHMUI.Instance.ShowUI(EUI.SpawnerStatusTooltip, new SpawnerStatusTooltipArg
            {
                SpawnerIndex = index,
                ViewModel    = _vm,
                AnchorCell   = rt,
                OnClosed     = HandleTooltipClosed,
                Balance      = _balance,
            }, ui => _openTooltipInstance = ui);
        }

        //# 툴팁이 닫힐 때 호출 (closeDisposable.Clear 의 일부) — 활성 테두리 원복 + 캐시 정리.
        //# 닫힌 셀 인덱스를 받아 stale 콜백을 self-ignore (advisor BLOCKER).
        //# 셀 전환 시 CHMUI.ShowUI 가 동기적으로 이전 disposable 을 Clear 하면서 이 콜백이 호출되는데,
        //# 그 시점 _openCellIndex 는 이미 새 셀(B) 로 바뀌어 있어 closedIndex(A) 와 다르다 → 무시.
        private void HandleTooltipClosed(int closedIndex)
        {
            if (_openCellIndex != closedIndex)
            {
                //# 셀 전환 race — 이전 셀의 cleanup 콜백, 무시.
                return;
            }
            if (_openCellIndex >= 0 && _openCellIndex < _cells.Count && _cells[_openCellIndex] != null)
                _cells[_openCellIndex].SetActiveBorder(false);
            _openCellIndex = -1;
            _openTooltipInstance = null;
        }

        //# 패널 측에서 명시적으로 툴팁 닫기.
        //# UIBase.Close 직접 호출 — closeDisposable.Clear 까지 정상 수행 (advisor lower-priority 1).
        //# Close 내부에서 OnClosed 콜백이 호출돼 _openCellIndex / SetActiveBorder 가 정리된다.
        private void CloseTooltip()
        {
            if (_openTooltipInstance != null)
            {
                _openTooltipInstance.Close(reuse: true);
            }
            else
            {
                //# fallback — 인스턴스 참조가 없으면 CHMUI 통해 닫기 (직접 정리도 함께).
                CHMUI.Instance.CloseUI(EUI.SpawnerStatusTooltip, reuse: true);
                if (_openCellIndex >= 0 && _openCellIndex < _cells.Count && _cells[_openCellIndex] != null)
                    _cells[_openCellIndex].SetActiveBorder(false);
                _openCellIndex = -1;
            }
        }
    }
}
