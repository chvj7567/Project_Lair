using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 색칩 visibility 토글 회귀 (기획서 §2.2.2 v0.5).
    //#
    //# 대상 로직 (SpawnerStatusCell.cs):
    //#  - RebindSnapshot 안의 `_colorChip.gameObject.SetActive(snapshot.OutputCount < 2)`
    //#    (N=1 → 노출, N≥2 → 숨김. 종명 가용 폭 회복용.)
    //#  - OnEnable 의 `_colorChip.gameObject.SetActive(true)` 풀 재사용 visibility 회복.
    //#
    //# 본 테스트는 SpawnerStatusCell 인스턴스 라이프사이클(EditMode 한정 — Canvas 없이도 Image
    //# 컴포넌트의 .color / .gameObject.activeSelf 만 검증 가능)을 다룬다.
    //# 정적 매핑 회귀는 SpawnerStatusCellTests / SpawnerStatusCellMappingTests 가 담당.
    public class SpawnerStatusCellVisibilityTests
    {
        //# 정리 대상 — TearDown 에서 DestroyImmediate.
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        //# 비활성 셀 GO + colorChip 자식(Image) 만 채워둔 상태로 생성.
        //# Cell 본체는 SetActive(false) 로 만들어 Unity 의 OnEnable 자동 호출을 막고
        //# 테스트가 reflection 으로 명시 호출한다 (BattleViewModelSpawnerSnapshotTests 패턴).
        //#
        //# 다른 SerializeField (_speciesText, _countText, _iconRow, _border 등) 는
        //# null 로 유지 — RebindSnapshot / OnEnable 모두 null 체크가 있어 안전.
        private (SpawnerStatusCell cell, GameObject chipGo) CreateCellWithColorChip()
        {
            GameObject cellGo = new GameObject("Cell_UT");
            cellGo.SetActive(false);
            _spawned.Add(cellGo);

            SpawnerStatusCell cell = cellGo.AddComponent<SpawnerStatusCell>();

            //# 색칩은 셀의 자식 GO 에 Image 컴포넌트 — production 프리팹 구조와 동일.
            GameObject chipGo = new GameObject("ColorChip");
            chipGo.transform.SetParent(cellGo.transform, false);
            Image chipImage = chipGo.AddComponent<Image>();

            //# private SerializeField _colorChip 주입.
            FieldInfo fi = typeof(SpawnerStatusCell).GetField(
                "_colorChip", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "SpawnerStatusCell._colorChip 필드 존재 확인");
            fi.SetValue(cell, chipImage);

            return (cell, chipGo);
        }

        //# OnEnable 명시 호출 — Cell GO 가 inactive 라 Unity 가 자동 호출하지 않음.
        private static void InvokeOnEnable(SpawnerStatusCell cell)
        {
            MethodInfo mi = typeof(SpawnerStatusCell).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerStatusCell.OnEnable 메서드 존재 확인");
            mi.Invoke(cell, null);
        }

        //# 표준 스냅샷 생성 헬퍼 — Index/CurrentType 고정, OutputCount 만 가변.
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

        //# ===== 케이스 1 — N=1 → 색칩 활성 =====

        [Test]
        public void RebindSnapshot_N이_1이면_색칩_활성()
        {
            //# Arrange — OnEnable 로 visibility=true 초기화.
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);

            //# Act — N=1 스냅샷 적용.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));

            //# Assert — `OutputCount < 2` → true → SetActive(true).
            Assert.IsTrue(chipGo.activeSelf, "N=1 일 때 색칩 활성 (종명 가용 폭 32px — §2.2.2 v0.5)");
        }

        //# ===== 케이스 2 — N=2 → 색칩 비활성 =====

        [Test]
        public void RebindSnapshot_N이_2이면_색칩_비활성()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);

            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));

            //# `OutputCount < 2` → false → SetActive(false). 종 색은 아이콘 row 배경 + 디스크 본체로 보전.
            Assert.IsFalse(chipGo.activeSelf, "N=2 일 때 색칩 숨김 (종명 가용 폭 회복 — §2.2.2 v0.5)");
        }

        //# ===== 케이스 3 — N=2 → N=1 전이 (색칩 복원) =====

        [Test]
        public void RebindSnapshot_N이_2에서_1로_전이시_색칩_복원()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);

            //# 1단계 — N=2 로 숨김.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));
            Assert.IsFalse(chipGo.activeSelf, "사전조건: N=2 에서 색칩 숨김");

            //# 2단계 — N=1 로 전이. ReplaceOutput 등으로 동시 출력이 1로 줄어드는 시나리오.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));

            Assert.IsTrue(chipGo.activeSelf,
                "N=2 → N=1 전이 시 색칩이 false → true 로 복원 (RebindSnapshot 단방향 갱신 — §2.2.2 v0.5)");
        }

        //# ===== 케이스 4 — N=1 → N=2 전이 (색칩 숨김) =====

        [Test]
        public void RebindSnapshot_N이_1에서_2로_전이시_색칩_숨김()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);

            //# 1단계 — N=1 로 노출 (OnEnable 의 기본값 그대로 — 검증 차원에서 명시 호출).
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsTrue(chipGo.activeSelf, "사전조건: N=1 에서 색칩 노출");

            //# 2단계 — IncrementOutput 으로 N=2 가 된 시나리오.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));

            Assert.IsFalse(chipGo.activeSelf,
                "N=1 → N=2 전이 시 색칩이 true → false 로 토글 (§2.2.2 v0.5)");
        }

        //# ===== 케이스 5 — 풀 재사용 안전 (OnEnable visibility 회복) =====

        //# 직전 셀이 N=2 로 숨김 상태로 풀에 반환됐어도, 재Pop 시 OnEnable 이 색칩을 다시 활성화해야 함.
        //# (Rule 12 — OnEnable / OnDisable 상태 리셋. RebindSnapshot 직전에 visibility 가 true 시작점이어야
        //# 첫 스냅샷이 어떤 N 이든 결정성 있게 토글됨.)
        [Test]
        public void OnEnable_직전_숨김상태에서_색칩_활성_복원()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();

            //# Arrange — 풀 반환 직전 상황 시뮬레이션. RebindSnapshot(N=2) 로 색칩이 비활성으로 박힘.
            //# (OnEnable 호출 없이 직접 Rebind 만 — 풀 반환 직전 마지막 갱신을 모사.)
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));
            Assert.IsFalse(chipGo.activeSelf, "사전조건: 풀 반환 직전 색칩 숨김 상태");

            //# Act — 풀에서 Pop 직후 Unity 가 OnEnable 자동 호출하는 시점을 reflection 으로 재현.
            InvokeOnEnable(cell);

            //# Assert — 색칩이 visibility=true 시작점으로 회복 (line 58 `_colorChip.gameObject.SetActive(true)`).
            Assert.IsTrue(chipGo.activeSelf,
                "OnEnable 이 직전 visibility 상태와 무관하게 색칩 활성 복원 (풀 재사용 — Rule 12)");
        }

        //# 풀 재사용 + 첫 Bind 가 N=1 이면 활성 유지 — Pop 직후 평범한 N=1 셀로 시작하는 케이스.
        [Test]
        public void 풀재사용_OnEnable_후_N1_Rebind시_색칩_활성_유지()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            //# 직전 풀 반환 — N=2 숨김 잔재.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));

            //# Pop — OnEnable 자동 호출 시점.
            InvokeOnEnable(cell);
            //# 첫 Rebind — N=1.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));

            Assert.IsTrue(chipGo.activeSelf, "OnEnable + N=1 첫 Rebind 후 색칩 활성");
        }

        //# 풀 재사용 + 첫 Bind 가 N=2 이면 즉시 숨김 — Pop 직후 이미 다중 출력인 셀로 시작하는 케이스.
        [Test]
        public void 풀재사용_OnEnable_후_N2_Rebind시_색칩_즉시_숨김()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            //# 직전 풀 반환 — N=1 노출 잔재.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsTrue(chipGo.activeSelf, "사전조건: 직전 노출");

            //# Pop — OnEnable.
            InvokeOnEnable(cell);
            Assert.IsTrue(chipGo.activeSelf, "OnEnable 직후 visibility=true 시작점");
            //# 첫 Rebind — N=2.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));

            Assert.IsFalse(chipGo.activeSelf,
                "OnEnable 의 true 시작점에서 N=2 Rebind 가 즉시 false 로 토글 (잔재 영향 없음)");
        }

        //# ===== 케이스 6 — null snapshot 방어 =====

        //# RebindSnapshot(null) 은 early return (line 80) — 색칩 visibility 가 직전 값 그대로 유지되어야.
        [Test]
        public void RebindSnapshot_null이면_NRE없이_색칩_상태_유지_숨김()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            //# 직전: N=2 로 숨김.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 2));
            Assert.IsFalse(chipGo.activeSelf, "사전조건: 직전 숨김 상태");

            //# Act + Assert — NRE 없이 통과.
            Assert.DoesNotThrow(() => cell.RebindSnapshot(null),
                "RebindSnapshot(null) 은 early return (line 80) — NRE 발생 X");

            //# 이전 상태 유지 — early return 이므로 색칩 SetActive 분기에 진입하지 않음.
            Assert.IsFalse(chipGo.activeSelf,
                "null snapshot 은 색칩 visibility 를 건드리지 않음 (직전 숨김 상태 유지)");
        }

        //# 대칭 — 직전이 노출 상태였으면 null 후에도 노출 유지.
        [Test]
        public void RebindSnapshot_null이면_색칩_상태_유지_노출()
        {
            (SpawnerStatusCell cell, GameObject chipGo) = CreateCellWithColorChip();
            InvokeOnEnable(cell);
            //# 직전: N=1 로 노출.
            cell.RebindSnapshot(MakeSnapshot(outputCount: 1));
            Assert.IsTrue(chipGo.activeSelf, "사전조건: 직전 노출 상태");

            Assert.DoesNotThrow(() => cell.RebindSnapshot(null));

            Assert.IsTrue(chipGo.activeSelf,
                "null snapshot 은 색칩 visibility 를 건드리지 않음 (직전 노출 상태 유지)");
        }
    }
}
