using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Battle;
using UnityEngine;

namespace Lair.UI
{
    //# 6셀 컨테이너 — BattleHud 자식. VM.Spawners 구독 + OnSpawnerSnapshotChanged.
    //# Tooltip 제거 (v0.6.4) — 셀 클릭 동작 없음, 진행 바·종명·×N 만 표시.
    public class SpawnerStatusPanel : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private GameObject _cellPrefab;

        private BattleViewModel _vm;
        private IReadOnlyList<Spawner> _spawners;

        private readonly List<SpawnerStatusCell> _cells = new();

        public void Bind(BattleViewModel vm, IReadOnlyList<Spawner> spawners)
        {
            _vm = vm;
            _spawners = spawners;
            if (vm == null) return;

            RebuildAll();
            vm.OnSpawnerSnapshotChanged += HandleSnapshotChanged;
        }

        public void Unbind()
        {
            if (_vm != null) _vm.OnSpawnerSnapshotChanged -= HandleSnapshotChanged;
            _vm = null;

            foreach (SpawnerStatusCell cell in _cells)
            {
                if (cell == null) continue;
                CHPoolable poolable = cell.GetComponent<CHPoolable>();
                if (poolable != null) CHMPool.Instance.Push(poolable);
                else                  Destroy(cell.gameObject);
            }
            _cells.Clear();
        }

        private void RebuildAll()
        {
            if (_vm == null || _container == null || _cellPrefab == null) return;
            IReadOnlyList<BattleViewModel.SpawnerSnapshot> snapshots = _vm.Spawners;
            if (snapshots == null) return;

            for (int i = 0; i < snapshots.Count; ++i)
            {
                BattleViewModel.SpawnerSnapshot snap = snapshots[i];
                if (snap == null) { _cells.Add(null); continue; }

                CHPoolable poolable = CHMPool.Instance.Pop(_cellPrefab, _container);
                if (poolable == null) { _cells.Add(null); continue; }
                SpawnerStatusCell cell = poolable.GetComponent<SpawnerStatusCell>();
                if (cell == null) { _cells.Add(null); continue; }

                ISpawnerProgress progress = null;
                if (_spawners != null && i < _spawners.Count) progress = _spawners[i];

                //# onClick null — Tooltip 폐기로 셀 클릭 동작 없음.
                cell.Bind(snap, progress, null);
                _cells.Add(cell);
            }
        }

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
    }
}
