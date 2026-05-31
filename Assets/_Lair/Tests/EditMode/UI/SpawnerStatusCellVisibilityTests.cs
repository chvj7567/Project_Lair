using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 색칩 visibility 회귀.
    //# v0.6.4 정책: IconRow 삭제로 redundancy 축이 줄어 색칩은 OutputCount 와 무관하게 항상 노출.
    public class SpawnerStatusCellVisibilityTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        //# 비활성 셀 GO + colorChip 자식만 채워둔 상태로 생성. Unity 의 OnEnable 자동 호출 회피용.
        private (SpawnerStatusCell cell, GameObject chipGo) CreateCellWithColorChip()
        {
            GameObject cellGo = new GameObject("Cell_UT");
            cellGo.SetActive(false);
            _spawned.Add(cellGo);

            SpawnerStatusCell cell = cellGo.AddComponent<SpawnerStatusCell>();

            GameObject chipGo = new GameObject("ColorChip");
            chipGo.transform.SetParent(cellGo.transform, false);
            Image chipImage = chipGo.AddComponent<Image>();

            FieldInfo fi = typeof(SpawnerStatusCell).GetField(
                "_colorChip", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "SpawnerStatusCell._colorChip 필드 존재 확인");
            fi.SetValue(cell, chipImage);

            return (cell, chipGo);
        }

        private static void InvokeOnEnable(SpawnerStatusCell cell)
        {
            MethodInfo mi = typeof(SpawnerStatusCell).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerStatusCell.OnEnable 메서드 존재 확인");
            mi.Invoke(cell, null);
        }

        private static BattleViewModel.SpawnerSnapshot MakeSnapshot(int outputCount, EMonster type = EMonster.Wisp)
        {
            return new BattleViewModel.SpawnerSnapshot
            {
                Index = 0,
                CurrentType = type,
                OutputCount = outputCount,
                AppliedBuffs = new List<BattleViewModel.AppliedBuff>(),
            };
        }

        //# ===== N=1 / N=2 / N=3 모두 활성 (v0.6.4 항상 노출 정책) =====

        [Test]
        public void RebindSnapshot_N1_색칩_활성()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsTrue(chipGo.activeSelf, "N=1 → 색칩 활성");
        }

        [Test]
        public void RebindSnapshot_N2_색칩_활성()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));
            Assert.IsTrue(chipGo.activeSelf, "N=2 → 색칩 활성 (v0.6.4 정책)");
        }

        [Test]
        public void RebindSnapshot_N3_색칩_활성()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 3));
            Assert.IsTrue(chipGo.activeSelf, "N=3 → 색칩 활성 (v0.6.4 정책)");
        }

        //# ===== 전이 — 모든 단계에서 활성 유지 =====

        [Test]
        public void RebindSnapshot_N2에서_N1_전이_활성_유지()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsTrue(chipGo.activeSelf, "N=2 → N=1 전이 후에도 활성 유지");
        }

        [Test]
        public void RebindSnapshot_N1에서_N2_전이_활성_유지()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));
            Assert.IsTrue(chipGo.activeSelf, "N=1 → N=2 전이 후에도 활성 유지 (v0.6.4 정책)");
        }

        //# ===== 풀 재사용 — OnEnable 이 항상 활성 시작점 보장 =====

        [Test]
        public void OnEnable_풀재사용시_색칩_활성_복원()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            //# 직전 풀 반환 — 외부에서 강제로 비활성 만들어 둔 상황.
            chipGo.SetActive(false);

            InvokeOnEnable(cell);

            Assert.IsTrue(chipGo.activeSelf,
                "OnEnable 이 직전 visibility 와 무관하게 색칩 활성 복원 (Rule 12)");
        }

        //# ===== null snapshot 방어 — 색칩 상태 유지 =====

        [Test]
        public void RebindSnapshot_null이면_NRE없이_색칩_상태_유지()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsTrue(chipGo.activeSelf, "사전조건: 활성 상태");

            Assert.DoesNotThrow(() => cell.RebindSnapshot(null), "null snapshot → early return, NRE 없음");

            Assert.IsTrue(chipGo.activeSelf, "null snapshot 은 색칩 visibility 를 건드리지 않음 (활성 유지)");
        }
    }
}
