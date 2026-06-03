# 원형 스포너 배치 (Circular Spawner Arranger) Implementation Plan

> **For agentic workers:** 이 plan 은 `start-develop-simple` 파이프라인(game-designer → gameplay-programmer → test-engineer)의 골격 가이드다. TDD 5단계를 강제하지 않으며 verification 은 가볍게 — 컴파일 통과 + 순수 헬퍼 EditMode 테스트 통과 수준.

**Goal:** 중앙 기준으로 스포너를 반지름·몬스터 리스트대로 원형 균등 배치하는 런타임 컴포넌트 + 에디터 Rebuild 버튼을 만든다.

**Architecture:** 런타임 `CircularSpawnerArranger`(설정 + 순수 각도 헬퍼) + Editor asmdef 의 커스텀 인스펙터(`CreatePrimitive`/생성 로직). 색상 테이블은 기존 `LairSpawnerVisualBuilder` 와 공유. Rebuild 가 `BattleController._spawners` 를 재와이어링.

**Tech Stack:** Unity 6 / C# / `Lair.Battle` namespace / Editor asmdef / NUnit EditMode

**Spec:** `docs/superpowers/specs/2026-06-03-circular-spawner-arranger-design.md`

---

## File Structure

| 파일 | 책임 | asmdef |
|---|---|---|
| `Assets/_Lair/Scripts/Battle/CircularSpawnerArranger.cs` (Create) | 설정(`_radius`/`_monsters`/`_startAngleDeg`) + 순수 정적 각도 헬퍼 | Lair |
| `Assets/_Lair/Editor/SpawnerColorPalette.cs` (Create) | `EMonster→색상 hex` 테이블 + 머티리얼 생성/로드 공유 헬퍼 (기존 빌더에서 추출) | Editor |
| `Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs` (Modify) | 추출된 `SpawnerColorPalette` 를 참조하도록 변경 (색상표 중복 제거) | Editor |
| `Assets/_Lair/Editor/CircularSpawnerArrangerEditor.cs` (Create) | 커스텀 인스펙터 "Rebuild" — 스포너 생성/배치/색상/BattleController 재와이어링 | Editor |
| `Assets/_Lair/Tests/EditMode/Battle/CircularSpawnerArrangerTests.cs` (Create) | 순수 헬퍼 EditMode 테스트 | Lair.Tests.EditMode |

---

## Task 1: game-designer — 기획서 박제

**Files:** Create `docs/design/circular-spawner-arranger.md`

- [ ] 프로토타입 기획서 작성 — 반지름 기본값(13), 시작각 기본(90°=+Z), 색상 테이블(기존 6종 hex 재사용), 각도 분배 규칙(360/N), 엣지(0/1/중복), 프로토타입 한계(HUD 6셀 가정·주기 기본 9초)를 spec §2~§6 기준으로 확정. 시너지 컬럼·정밀 밸런스 생략 허용.

---

## Task 2: 순수 각도 헬퍼 + 컴포넌트 골격

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/CircularSpawnerArranger.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/CircularSpawnerArrangerTests.cs`

- [ ] **컴포넌트 + 순수 헬퍼 작성**

```csharp
using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 중앙(transform.position) 기준 원형 스포너 배치 설정. 실제 생성은 에디터(CircularSpawnerArrangerEditor).
    public class CircularSpawnerArranger : MonoBehaviour
    {
        [SerializeField] private float _radius = 13f;
        [SerializeField] private List<EMonster> _monsters = new List<EMonster>();
        [SerializeField] private float _startAngleDeg = 90f;

        public float Radius => _radius;
        public IReadOnlyList<EMonster> Monsters => _monsters;
        public float StartAngleDeg => _startAngleDeg;

        //# N개 균등 분배 각 간격. count<=0 이면 0.
        public static float AngleStep(int count) => count <= 0 ? 0f : 360f / count;

        //# 탑다운 평면(XZ) 원주 위 좌표. +Z 가 angleDeg=90 기준 (cos→x, sin→z).
        public static Vector3 PositionOnCircle(Vector3 center, float radius, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(center.x + radius * Mathf.Cos(rad), center.y, center.z + radius * Mathf.Sin(rad));
        }

        //# count 개 균등 배치 좌표. startDeg 부터 360/count 씩. count<=0 이면 빈 배열.
        public static Vector3[] ComputePositions(Vector3 center, float radius, int count, float startDeg)
        {
            if (count <= 0) return new Vector3[0];
            Vector3[] result = new Vector3[count];
            float step = AngleStep(count);
            for (int i = 0; i < count; ++i)
                result[i] = PositionOnCircle(center, radius, startDeg + step * i);
            return result;
        }
    }
}
```

- [ ] **EditMode 테스트 작성** (한글 메서드명 — `test_method_naming: korean`)

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.EditMode.Battle
{
    public class CircularSpawnerArrangerTests
    {
        [Test]
        public void 두개일때_각간격은_180도()
            => Assert.AreEqual(180f, CircularSpawnerArranger.AngleStep(2), 1e-4f);

        [Test]
        public void 세개일때_각간격은_120도()
            => Assert.AreEqual(120f, CircularSpawnerArranger.AngleStep(3), 1e-4f);

        [Test]
        public void 네개일때_각간격은_90도()
            => Assert.AreEqual(90f, CircularSpawnerArranger.AngleStep(4), 1e-4f);

        [Test]
        public void 개수가_0이하면_각간격_0()
        {
            Assert.AreEqual(0f, CircularSpawnerArranger.AngleStep(0));
            Assert.AreEqual(0f, CircularSpawnerArranger.AngleStep(-3));
        }

        [Test]
        public void 모든점은_center에서_radius거리()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 5, 90f);
            Assert.AreEqual(5, pts.Length);
            foreach (Vector3 p in pts)
                Assert.AreEqual(13f, new Vector3(p.x, 0f, p.z).magnitude, 1e-3f);
        }

        [Test]
        public void 인접점_각거리_균등()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 10f, 3, 90f);
            float a01 = Vector3.Angle(pts[0], pts[1]);
            float a12 = Vector3.Angle(pts[1], pts[2]);
            Assert.AreEqual(a01, a12, 1e-2f);
            Assert.AreEqual(120f, a01, 1e-2f);
        }

        [Test]
        public void 개수0이면_빈배열()
            => Assert.AreEqual(0, CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 0, 90f).Length);
    }
}
```

- [ ] **검증:** EditMode 테스트 실행 → 7건 PASS (LairTestRunner 또는 Unity Test Runner)

---

## Task 3: 색상 팔레트 공유 헬퍼 추출

**Files:**
- Create: `Assets/_Lair/Editor/SpawnerColorPalette.cs`
- Modify: `Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs`

- [ ] `LairSpawnerVisualBuilder` 의 `SpawnerColorTable`(EMonster→hex 6종) + `EnsureSpawnerMaterials()` 를 `SpawnerColorPalette` static class 로 이동. 두 빌더가 같은 테이블·머티리얼을 공유. 동작 동일(idempotent, `Art/Materials/Mat_Spawner_{type}.mat`).
- [ ] `LairSpawnerVisualBuilder` 가 추출본을 호출하도록 수정. 색상표 중복 정의 0건 확인.
- [ ] **검증:** 컴파일 통과 + 기존 `Lair/Setup/S1 - Attach Spawner Visuals` 메뉴 동작 회귀 없음(색상 동일).

---

## Task 4: 커스텀 인스펙터 Rebuild

**Files:** Create `Assets/_Lair/Editor/CircularSpawnerArrangerEditor.cs`

- [ ] `[CustomEditor(typeof(CircularSpawnerArranger))]` 작성. 기본 인스펙터 + "Rebuild" 버튼. Rebuild 동작:
  1. 관리 스포너 자식 전부 `DestroyImmediate` (전면 교체, idempotent)
  2. `arranger.Monsters` 순회 — i 마다:
     - `Spawner` GameObject 생성(`Spawner_{type}_{i}`) + `Spawner` 컴포넌트
     - `_outputType = type` (SerializedObject)
     - `transform.position = CircularSpawnerArranger.ComputePositions(arranger.transform.position, Radius, count, StartAngleDeg)[i]`
     - `SpawnerBody` Cylinder 디스크 자식 부착 — `SpawnerColorPalette` 머티리얼 사용 (기존 `LairSpawnerVisualBuilder.EnsureSpawnerBody` 패턴 재사용: `_renderer`/`_materials` 주입)
  3. `BattleController._spawners` 를 새 배열로 재와이어링 (SerializedObject, `FindFirstObjectByType<BattleController>`)
  4. `EditorSceneManager.MarkSceneDirty` + 저장
- [ ] GameObject 생성/`CreatePrimitive` 가 **Editor asmdef 안에만** 있는지 확인 (Rule 03 §4).
- [ ] **검증:** 컴파일 통과. 씬에서 빈 GameObject 에 `CircularSpawnerArranger` 부착 → `_monsters` 에 2~3종 + radius 입력 → Rebuild → 스포너가 원형 균등 배치 + 각 색상 표시 + `BattleController._spawners` 갱신 확인. (gameplay-programmer 스모크)

---

## Task 5: 마무리

- [ ] 변경 파일 요약 + 한글 커밋 메시지(안) 제시 (Rule 01 — 자동 커밋 금지, `git add` 까지). 신규 .cs 는 `.meta` 동반 스테이징.

---

## Self-Review

- **Spec coverage:** §3.1 컴포넌트→Task2, §3.2 에디터→Task4, §3.3 색상 공유→Task3, §4 각도→Task2 헬퍼, §5 엣지→Task2 테스트, §7 테스트→Task2, BattleController 재와이어링→Task4 step3. 기획서→Task1. 갭 없음.
- **Placeholder scan:** 코드 블록 모두 실제 구현/시그니처 포함. Task4 의 SpawnerBody 부착은 기존 `LairSpawnerVisualBuilder.EnsureSpawnerBody` 패턴 명시 참조.
- **Type consistency:** `AngleStep`/`PositionOnCircle`/`ComputePositions`/`Monsters`/`Radius`/`StartAngleDeg` Task2 정의와 Task4 사용 일치. `SpawnerColorPalette` Task3 정의 Task4 사용 일치.
