# 스포너 상태 UI 구현 계획서

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** World-space 진행 바를 제거하고 화면 하단 6셀 가로 패널로 스포너 상태(종/×N/진행/강화)를 통합하며, BuildPanel 클릭 시 모달로 픽한 모든 카드를 표시한다.

**Architecture:** 데이터(`Spawner` + `BattleController`) → ViewModel(`BattleViewModel.SpawnerSnapshot`) → View(`SpawnerStatusPanel` 6 × `SpawnerStatusCell`) 단방향. 강화 카드의 source 추적은 `BattleController.ApplyCardEffect(card)` 진입점이 `_currentCardScope` 를 잠시 보관해 `RegisterMonsterTypeBuff` 가 self-key 로 사용하는 방식. Progress 만 셀 측 매 프레임 폴링(이벤트 우회), 나머지(종/카운트/강화)는 VM 이벤트 푸시.

**Tech Stack:** Unity 2022.3+ / C# / MVVM / NUnit (EditMode·PlayMode) / `ChvjUnityInfra` (CHMPool · CHText · CHButton · CHMUI · CHMResource · Addressables)

**설계서:** `docs/superpowers/specs/2026-05-27-spawner-status-ui-design.md`

**구현 참조 단일 진실:** 본 계획의 각 Task 가 만들/고칠 파일의 **전체 본문**은 이미 동일 브랜치에 staging 되어 있다 (스펙과 함께 staged). 계획서는 TDD 흐름·시그니처·핵심 골격을 명시하고, 200줄 이상 가는 구현은 staging diff 를 단일 진실로 본다. 다른 환경에서 재현이 필요하면 staging diff 를 그대로 옮긴다.

**건드리지 말 것 (스펙 §5.4 유지):**
- `Assets/_Lair/Scripts/Battle/SpawnerBody.cs` — 디스크 색상 틴트 (Replace 카드 시각 피드백) 그대로 유지
- `Spawner.Progress` / `ISpawnerProgress` 인터페이스 자체 — World 대신 셀이 폴링할 뿐 계약은 동일

**룰 주의 (CLAUDE.md):**
- Rule 01 — `git commit` 직접 실행 금지. 각 Task 끝에서 **관련 파일 `git add` + 커밋 메시지(안)** 까지만.
- Rule 02 — 모든 신규 단일 라인 주석은 `//#` 접두어.
- Rule 05 — MVVM 단방향 (Spawner/BattleController → VM → View).
- Rule 07/11/12 — ChvjPackage 기준. `CHText`/`CHButton`/`CHMPool` 사용.
- Rule 08 — Enum 키 = 에셋 파일명 (대소문자 일치).
- Rule 09/10/13 — `CommonEnum.cs`, `CommonInterface.cs`, `UIArg` 페어 단일 파일.
- Rule 14 — 신규 프리팹은 `Assets/_Lair/Art/UI/`.

**테스트 실행:** Unity Test Runner — `editor_execute_menu` 로 `Lair/Tests/Run EditMode Tests` (PlayMode 는 `Run PlayMode Tests`) 실행 후 `Library/lair-test-result.json` 의 `"done": true` 폴링. 코드 수정 후 재컴파일 완료 대기 필수. 신규 `.cs` 는 `editor_refresh_assets` 로 임포트.

---

## 파일 구조

| 파일 | 책임 | MS |
|---|---|---|
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `EUI.BuildModalPopup`, `EUI.SpawnerStatusTooltip` 추가 | M1 수정 |
| `Assets/_Lair/Scripts/Battle/CommonInterface.cs` | `ISpawnerOutputProvider` 에 `OutputCount` + `OnOutputCountChanged` | M1 수정 |
| `Assets/_Lair/Scripts/Battle/Spawner.cs` | `OutputCount`/`OnOutputCountChanged` 구현 + `IncrementOutput` 시 발행 | M1 수정 |
| `Assets/_Lair/Scripts/Battle/StatMultiplier.cs` | `Get(EMonsterStatKind)` read API 추가 | M1 수정 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | `_typeModifierPicks`/`_currentCardScope`/`ApplyCardEffect`/`TrackCardPick`/`GetAppliedBuffs`/`OnTypeModifierChanged`/`Balance` | M2 수정 |
| `Assets/_Lair/Scripts/UI/BattleViewModel.cs` | `AppliedBuff`·`SpawnerSnapshot`·`AttachSpawners`/`DetachSpawners`·`OnSpawnerSnapshotChanged` | M2 수정 |
| `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs` | 1셀 — 아이콘 row + 색칩 + 종명 + ×N + 진행 바 + 활성 테두리 | M3 신규 |
| `Assets/_Lair/Scripts/UI/SpawnerStatusPanel.cs` | 6셀 컨테이너 + 툴팁 토글 상태 | M3 신규 |
| `Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs` | 셀 위 floating 툴팁 + `SpawnerStatusTooltipArg` 페어 | M3 신규 |
| `Assets/_Lair/Scripts/UI/BuildModalPopup.cs` | 화면 중앙 모달 + `BuildModalPopupArg` + `BuildModalCardCell` | M3 신규 |
| `Assets/_Lair/Scripts/UI/BuildPanel.cs` | 종 강화 필터 + 루트 `CHButton` → 모달 호출 | M3 수정 |
| `Assets/_Lair/Scripts/UI/BattleHud.cs` | `SpawnerStatusPanel` Bind + `BattleHudArg` 에 `Spawners`/`Balance` | M3 수정 |
| `Assets/_Lair/Editor/LairSpawnerStatusUIBuilder.cs` | 신규 프리팹 4종 절차적 빌드 | M5 신규 |
| `Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs` | 진행 바 빌드 스텝 제거 (디스크 본체만 유지) | M5 수정 |
| `Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs` | 삭제 | M5 제거 |
| `Assets/_Lair/Tests/EditMode/Battle/SpawnerCooldownBarTests.cs` | 삭제 | M5 제거 |
| `Assets/_Lair/Tests/EditMode/Battle/SpawnerOutputCountTests.cs` | Spawner.OutputCount 단위 테스트 | M1 신규 |
| `Assets/_Lair/Tests/EditMode/Battle/SpawnerOutputEventTests.cs` | 이벤트 경계 회귀 테스트 | M1 신규 |
| `Assets/_Lair/Tests/EditMode/Battle/BattleControllerCardScopeTests.cs` | `_currentCardScope`·`TrackCardPick` 동작 검증 | M2 신규 |
| `Assets/_Lair/Tests/EditMode/Battle/BattleViewModelSpawnerSnapshotTests.cs` | `AttachSpawners`·이벤트 누수·중복 attach 검증 | M2 신규 |
| `Assets/_Lair/Tests/EditMode/Card/CardEffectSignatureRegressionTests.cs` | `ICardEffect.Apply(IBattleContext)` 시그니처 회귀 | M2 신규 |
| `Assets/_Lair/Tests/EditMode/Helpers/FakeCardEffect.cs` | 테스트 헬퍼 — Apply 본문 주입 | M2 신규 |
| `Assets/_Lair/Tests/EditMode/UI/SpawnerStatusCellTests.cs` | 셀 색상·×N·아이콘 row 검증 | M3 신규 |
| `Assets/_Lair/Tests/EditMode/UI/SpawnerStatusCellMappingTests.cs` | `SpeciesColor`/`IconLetterFor` 매핑 표 검증 | M3 신규 |
| `Assets/_Lair/Tests/EditMode/UI/SpawnerStatusTooltipFormatTests.cs` | 강화 줄 포맷 (스탯별 6분기) | M3 신규 |
| `Assets/_Lair/Tests/EditMode/UI/BuildPanelFilterTests.cs` | 종 강화 6장 제외 필터 | M3 신규 |
| `Assets/_Lair/Tests/EditMode/UI/BuildModalPopupTests.cs` | 패시브 그룹 정렬·액티브 순서 | M3 신규 |
| `Assets/_Lair/Tests/PlayMode/SpawnerStatusUIPlayTests.cs` | Battle 씬 통합 — VM.Spawners 6개 | M6 신규 |

---

## 마일스톤 M1 — Data 계층 (Spawner / Enum / Interface)

### Task 1: `EUI` 에 신규 키 2개 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`

- [ ] **Step 1: `EUI` enum 에 `BuildModalPopup`, `SpawnerStatusTooltip` 추가**

기존 `EUI` 정의의 `CardSelectionPopup` 다음에 두 값을 append 한다. 순서 변경 금지 — 기존 값 인덱스 보존.

```csharp
    //# CHMUI.ShowUI 로 UI 프리팹 로드.
    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# B1 신규
        BuildModalPopup,       //# 스포너 상태 UI — BuildPanel 클릭 시 화면 중앙 모달
        SpawnerStatusTooltip,  //# 스포너 상태 UI — 셀 클릭 시 셀 위 floating 툴팁
    }
```

- [ ] **Step 2: 컴파일 확인**

`editor_recompile` 후 `editor_read_log` — 에러 0 확인. Rule 08 보장은 M5 의 프리팹 빌더에서 (파일명 == Enum 값명).

- [ ] **Step 3: git add**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs
```

커밋 메시지(안):
```
# [feat] - EUI 에 BuildModalPopup·SpawnerStatusTooltip 추가
```

---

### Task 2: `ISpawnerOutputProvider` 확장

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/CommonInterface.cs`

- [ ] **Step 1: 인터페이스에 `OutputCount` 와 `OnOutputCountChanged` 추가**

`ISpawnerOutputProvider` 가 이미 `CurrentType` + `OnOutputTypeChanged` 만 노출. 동시 출력 수도 셀이 읽어야 하므로 같은 인터페이스에 추가 (Rule 10 — 단일 도메인 통합).

```csharp
    //# Rule 10 — Spawner 출력 종(EMonster) 변경 이벤트 + 동시 출력 수 노출 계약.
    //# SpawnerBody 가 GetComponentInParent<ISpawnerOutputProvider>() 로 구독 (Rule 06).
    //# Spawner.cs 가 구현. ReplaceOutput 호출 시 + OnEnable 시 OnOutputTypeChanged 발행.
    //# IncrementOutput 호출 시 OnOutputCountChanged 발행 (OnEnable 시점에는 발행 안 함 — VM 폴링).
    public interface ISpawnerOutputProvider
    {
        //# 현재 출력 중인 몬스터 종.
        EMonster CurrentType { get; }

        //# 동시 출력 수 — 기본 1, 추가소환 카드(IncrementSpawnerOutput)로 +1.
        int OutputCount { get; }

        //# ReplaceOutput 호출 시 또는 초기화(OnEnable) 시 발행.
        event System.Action<EMonster> OnOutputTypeChanged;

        //# IncrementOutput 호출 시 발행. OnEnable 시점엔 발행 안 함 (VM 이 초기값을 직접 폴링).
        event System.Action<int> OnOutputCountChanged;
    }
```

- [ ] **Step 2: 컴파일 — `Spawner.cs` 가 새 멤버 미구현 상태라 에러 예상**

이 시점엔 컴파일 실패가 정상. Task 3 가 구현체를 채운다.

- [ ] **Step 3: git add (Task 3 끝에서 함께 add)** — 단일 커밋으로 묶음.

---

### Task 3: `Spawner` 에 `OutputCount` + `OnOutputCountChanged` 구현

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/Spawner.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/SpawnerOutputCountTests.cs` (신규)
- Test: `Assets/_Lair/Tests/EditMode/Battle/SpawnerOutputEventTests.cs` (신규)

- [ ] **Step 1: 실패 테스트 — `SpawnerOutputCountTests.cs`**

`Assets/_Lair/Tests/EditMode/Battle/` 에 새 파일 생성. 기존 `SpawnerTests.cs` 패턴을 따른다 (reflection 으로 private 필드 주입).

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 스포너 상태 UI — 영역 B (Spawner.OutputCount / IncrementOutput).
    public class SpawnerOutputCountTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private Spawner Create()
        {
            var go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            var sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", EMonster.Wisp);
            SetPrivate(sp, "_spawnPeriod", 9f);
            SetPrivate(sp, "_initialDelay", 0f);
            typeof(Spawner).GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sp, null);
            return sp;
        }

        private static void SetPrivate(object t, string f, object v)
            => t.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, v);

        [Test]
        public void 초기_OutputCount_1()
        {
            var sp = Create();
            Assert.AreEqual(1, sp.OutputCount);
        }

        [Test]
        public void IncrementOutput_1회_호출시_OutputCount_2()
        {
            var sp = Create();
            sp.IncrementOutput();
            Assert.AreEqual(2, sp.OutputCount);
        }

        [Test]
        public void IncrementOutput_N회_호출시_OutputCount_1plusN()
        {
            var sp = Create();
            for (int i = 0; i < 4; ++i) sp.IncrementOutput();
            Assert.AreEqual(5, sp.OutputCount);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 컴파일 실패 확인**

`Spawner.OutputCount` / `IncrementOutput` 미존재. 정상.

- [ ] **Step 3: `Spawner.cs` 에 필드/프로퍼티/이벤트 추가**

`Spawner.cs` 의 기존 `_currentType` 필드 다음에 `_outputCount` 필드 + `OutputCount` 프로퍼티 + `OnOutputCountChanged` 이벤트 + `IncrementOutput` 메서드를 추가한다. `OnEnable` 에서 `_outputCount = 1` 리셋, 단 `OnOutputCountChanged` 는 발행하지 않는다 (VM 이 직접 폴링).

```csharp
        //# 동시 출력 수 — 기본 1, 추가소환 카드(IncrementSpawnerOutput)로 +1. Spawner 슬롯에 영구 귀속.
        private int _outputCount = 1;

        //# ISpawnerOutputProvider — 동시 출력 수. VM 이 AttachSpawners 시점에 직접 폴링.
        public int OutputCount => _outputCount;

        //# ISpawnerOutputProvider 구현 — VM 이 IncrementOutput 발생 시 구독해 셀 갱신.
        //# OnEnable 시점엔 발행 안 함 — VM 의 AttachSpawners 가 OutputCount 를 직접 폴링한다.
        public event System.Action<int> OnOutputCountChanged;

        //# 추가소환 카드 — 동시 출력 +1 (Spawner 슬롯에 영구 귀속, §3.2).
        //# 호출 시 OnOutputCountChanged 발행 — VM 셀이 ×N 갱신.
        public void IncrementOutput()
        {
            _outputCount++;
            OnOutputCountChanged?.Invoke(_outputCount);
        }
```

`OnEnable` 본문에 `_outputCount = 1;` 한 줄 삽입 (기존 `_currentType = _outputType;` 다음 줄).

- [ ] **Step 4: 테스트 재실행 — 통과 확인**

```
editor_execute_menu("Lair/Tests/Run EditMode Tests")
```

Expected: 3개 신규 케이스 PASS. 기존 `SpawnerTests` 도 통과 유지.

- [ ] **Step 5: 이벤트 회귀 테스트 추가 — `SpawnerOutputEventTests.cs`**

`Spawner.OnOutputCountChanged` 가 모든 경계 케이스에서 정확히 발행되는지 검증.

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 스포너 상태 UI — 영역 B 보강 (OnOutputCountChanged / OnOutputTypeChanged 이벤트 경계).
    public class SpawnerOutputEventTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private Spawner CreateSpawner(EMonster type = EMonster.Wisp)
        {
            var go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            var sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", type);
            SetPrivate(sp, "_spawnPeriod", 9f);
            SetPrivate(sp, "_initialDelay", 0f);
            InvokeOnEnable(sp);
            return sp;
        }

        private static void SetPrivate(object t, string f, object v)
            => t.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, v);

        private static void InvokeOnEnable(Spawner sp)
            => typeof(Spawner).GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sp, null);

        //# OnEnable 시 OnOutputCountChanged 미발행 (VM 이 직접 폴링하기 위한 계약).
        [Test]
        public void OnEnable시_OutputCountChanged_미발행()
        {
            var sp = CreateSpawner();
            int calls = 0;
            sp.OnOutputCountChanged += _ => calls++;
            InvokeOnEnable(sp);   //# 재호출.
            Assert.AreEqual(0, calls);
        }

        //# ReplaceOutput 은 OutputCount 를 안 바꾸므로 OnOutputCountChanged 미발행.
        [Test]
        public void ReplaceOutput시_OutputCountChanged_미발행()
        {
            var sp = CreateSpawner();
            int calls = 0;
            sp.OnOutputCountChanged += _ => calls++;
            sp.ReplaceOutput(EMonster.Plague);
            Assert.AreEqual(0, calls);
        }

        //# IncrementOutput N 회 호출 — 정확히 N 회 발행, 인자 단조 증가 2..N+1.
        [Test]
        public void IncrementOutput_N회_정확히_N회_발행_단조증가()
        {
            var sp = CreateSpawner();
            var received = new List<int>();
            sp.OnOutputCountChanged += v => received.Add(v);
            for (int i = 0; i < 4; ++i) sp.IncrementOutput();
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, received);
        }

        //# 다중 구독자 — 모두에게 동일 인자.
        [Test]
        public void 다중구독자_모두에게_동일_인자_전달()
        {
            var sp = CreateSpawner();
            int a = 0, b = 0;
            sp.OnOutputCountChanged += v => a = v;
            sp.OnOutputCountChanged += v => b = v;
            sp.IncrementOutput();
            Assert.AreEqual(2, a);
            Assert.AreEqual(2, b);
        }

        //# 구독 해제 후 미호출 — leak 방지.
        [Test]
        public void 구독해제후_IncrementOutput_핸들러_미호출()
        {
            var sp = CreateSpawner();
            int calls = 0;
            System.Action<int> handler = _ => calls++;
            sp.OnOutputCountChanged += handler;
            sp.OnOutputCountChanged -= handler;
            sp.IncrementOutput();
            Assert.AreEqual(0, calls);
        }
    }
}
```

- [ ] **Step 6: 테스트 실행 — 5케이스 PASS 확인**

- [ ] **Step 7: git add (Task 1·2·3 묶음)**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Scripts/Battle/CommonInterface.cs Assets/_Lair/Scripts/Battle/Spawner.cs Assets/_Lair/Tests/EditMode/Battle/SpawnerOutputCountTests.cs Assets/_Lair/Tests/EditMode/Battle/SpawnerOutputEventTests.cs
```

커밋 메시지(안):
```
# [feat] - Spawner OutputCount + OnOutputCountChanged 노출
```

---

### Task 4: `StatMultiplier.Get(stat)` read API 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/StatMultiplier.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/StatMultiplierTests.cs` (기존 파일 보강)

- [ ] **Step 1: 실패 테스트 추가**

기존 `StatMultiplierTests.cs` 에 Get 검증 케이스 append.

```csharp
        [Test]
        public void Get_각_스탯_종류에_해당_필드_값_반환()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Hp, 1.5f);
            m.Multiply(EMonsterStatKind.Cooldown, 0.7f);
            Assert.AreEqual(1.5f, m.Get(EMonsterStatKind.Hp), 0.0001f);
            Assert.AreEqual(0.7f, m.Get(EMonsterStatKind.Cooldown), 0.0001f);
            Assert.AreEqual(1f,  m.Get(EMonsterStatKind.Power), 0.0001f, "미설정 스탯은 항등 1");
        }
```

- [ ] **Step 2: `StatMultiplier` 에 `Get` 메서드 추가**

`Multiply` 메서드 바로 아래에 추가.

```csharp
        //# 지정 스탯 종류의 현재 누적 배율 조회 — 툴팁(AppliedBuff.AggregateMultiplier) 갱신 등 외부 read 용.
        public float Get(EMonsterStatKind stat)
        {
            switch (stat)
            {
                case EMonsterStatKind.Hp:         return HpMul;
                case EMonsterStatKind.Power:      return PowerMul;
                case EMonsterStatKind.Cooldown:   return CooldownMul;
                case EMonsterStatKind.Range:      return RangeMul;
                case EMonsterStatKind.MoveSpeed:  return MoveSpeedMul;
                case EMonsterStatKind.SlowFactor: return SlowFactorMul;
                default:                          return 1f;
            }
        }
```

- [ ] **Step 3: 테스트 PASS**

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Scripts/Battle/StatMultiplier.cs Assets/_Lair/Tests/EditMode/Battle/StatMultiplierTests.cs
```

커밋 메시지(안):
```
# [refactor] - StatMultiplier.Get read API 추가
```

---

## 마일스톤 M2 — Card Source 추적 + ViewModel

### Task 5: 테스트 헬퍼 `FakeCardEffect` + `FakeCardData` 보강

**Files:**
- Create: `Assets/_Lair/Tests/EditMode/Helpers/FakeCardEffect.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Helpers/FakeCardData.cs`

- [ ] **Step 1: `FakeCardEffect.cs` — Apply 본문 주입식 더블 생성**

```csharp
using System;
using Lair.Card;

namespace Lair.Tests.Helpers
{
    //# 테스트 더블 — Apply 본문을 람다로 주입. PoolingScrollView 등 외부 의존 없이 IBattleContext 동작 검증.
    public class FakeCardEffect : ICardEffect
    {
        public Action<IBattleContext> OnApply;
        public int ApplyCount;

        public void Apply(IBattleContext ctx)
        {
            ApplyCount++;
            OnApply?.Invoke(ctx);
        }
    }
}
```

- [ ] **Step 2: `FakeCardData.cs` 에 `Create(ECardId id, ICardEffect effect = null)` 오버로드 추가**

기존 시그니처가 어떤지 먼저 확인 (Read). 효과 인자를 받지 않으면 SerializeReference 필드를 reflection 으로 주입하는 헬퍼를 추가.

```csharp
        //# 효과 주입 가능한 오버로드 — 스포너 상태 UI 추적 테스트 (TrackCardPick 등) 용.
        public static Lair.Card.CardData Create(Lair.Data.ECardId id, Lair.Card.ICardEffect effect)
        {
            var card = Create(id);   //# 기존 헬퍼 — _id 직렬화까지.
            var fi = typeof(Lair.Card.CardData).GetField("_effect",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fi?.SetValue(card, effect);
            return card;
        }
```

- [ ] **Step 3: 컴파일 확인 — 신규 헬퍼 임포트**

`editor_refresh_assets` 후 `editor_recompile`. 헬퍼는 다음 Task 가 첫 소비자.

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Tests/EditMode/Helpers/FakeCardEffect.cs Assets/_Lair/Tests/EditMode/Helpers/FakeCardData.cs
```

커밋 메시지(안):
```
# [test] - FakeCardEffect + FakeCardData 효과 주입 오버로드
```

---

### Task 6: `BattleController` 카드 source 추적 (BLOCKER 4 결정)

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/BattleControllerCardScopeTests.cs` (신규)

설계 결정 (스펙 §10 권장안): `ICardEffect.Apply(IBattleContext)` 시그니처는 변경하지 않고, `BattleController.ApplyCardEffect(card)` 가 카드 호출 직전에 `_currentCardScope` 를 잠시 저장. `RegisterMonsterTypeBuff` 가 발행 시점에 `_currentCardScope` 가 non-null 이면 source 로 사용.

- [ ] **Step 1: 실패 테스트 추가 — `BattleControllerCardScopeTests.cs`**

`Assets/_Lair/Tests/EditMode/Battle/BattleControllerCardScopeTests.cs` 신규 — 비활성 GameObject 에 BattleController 를 붙여 `Start(async void)` 회피하고 `_ctx` 만 reflection 으로 주입한다. 전체 본문은 staging 된 `BattleControllerCardScopeTests.cs` 파일을 그대로 사용 — 핵심 케이스 목록:

  - `ApplyCardEffect_진입시_currentCardScope에_카드_저장된다`
  - `ApplyCardEffect_복귀후_currentCardScope_null로_복원`
  - `ApplyCardEffect_Apply가_예외던져도_finally로_scope_복원`
  - `ApplyCardEffect_card_null이면_noop_예외없음`
  - `ApplyCardEffect_card_Effect_null이면_noop`
  - `ApplyCardEffect_ctx_null이면_noop_예외없음`
  - `강화_카드_1픽시_AppliedBuffs에_PickCount_1로_기록`
  - `강화_카드_중첩_2픽시_PickCount_2_AggregateMultiplier_2점25`
  - `다른_종_강화는_dict_키_분리_서로_영향없음`
  - `GetAppliedBuffs_미픽_종은_빈_배열_반환`
  - `RegisterMonsterTypeBuff_scope_없이_호출시_추적_미누적_배율은_정상`
  - `RegisterMonsterTypeBuff_호출시_OnTypeModifierChanged_발행`
  - `ApplyCardEffect_경유시도_OnTypeModifierChanged_정상_발행`
  - `TrackCardPick_AggregateMultiplier가_StatMultiplier_Get으로_동기화`

(전체 코드는 staging 된 `Assets/_Lair/Tests/EditMode/Battle/BattleControllerCardScopeTests.cs` 참조 — 약 320 줄. 본 계획서는 검증 의도만 명시.)

- [ ] **Step 2: 테스트 실행 — 컴파일 실패 확인**

`BattleController.ApplyCardEffect` / `GetAppliedBuffs` / `OnTypeModifierChanged` / `_currentCardScope` / `_typeModifierPicks` / `BattleViewModel.AppliedBuff` 미존재. 정상.

- [ ] **Step 3: `BattleViewModel` 에 `AppliedBuff` 클래스 정의 (Task 7 의 일부지만 컴파일 의존 때문에 먼저)**

`BattleViewModel.cs` 의 기존 `BuildEntry` 다음에 `AppliedBuff` + `SpawnerSnapshot` 두 클래스를 추가. 본문은 staging 된 `BattleViewModel.cs:23-39` 그대로:

```csharp
        //# 스포너 셀 1개에 적용된 강화 카드 1픽 — 툴팁의 강화 줄 + 셀 상단 아이콘 row 의 source.
        //# Rule 10 의 동일 도메인 단일 파일 정신 + 기존 BuildEntry 와 같은 파일에 정의 (기획서 §4.3).
        public class AppliedBuff
        {
            public CardData Source;                  //# 어느 카드인지 (Wisp~Phantom 강화 6장 중 1)
            public int PickCount;                    //# 중첩 픽 횟수 (×N 배지 출처)
            public EMonsterStatKind Stat;            //# 어느 스탯
            public float AggregateMultiplier;        //# 곱연산 누적 결과 (툴팁 ×배율 표시 출처)
        }

        //# 스포너 1개의 표시용 스냅샷 — 이벤트 발행 시점에 재계산해 View 에 푸시.
        //# Progress 는 스냅샷에 안 들어감 (View 측 매 프레임 ISpawnerProgress.Progress 폴링).
        public class SpawnerSnapshot
        {
            public int Index;                                  //# 0~5 ring 인덱스
            public EMonster CurrentType;
            public int OutputCount;
            public IReadOnlyList<AppliedBuff> AppliedBuffs;    //# 현 출력 종에 적용된 강화 카드 픽 누적
        }
```

- [ ] **Step 4: `BattleController` 에 source 추적 필드 + 진입점 추가**

`_typeModifiers` 다음 줄에 `_typeModifierPicks`, `_currentCardScope`, `OnTypeModifierChanged` 추가:

```csharp
        //# 스포너 상태 UI — 종별 적용된 강화 카드 픽 누적 (툴팁 본문 + 셀 상단 아이콘 row 의 source).
        //# Source 추적은 ApplyCardEffect(card) 가 _currentCardScope 에 카드를 저장한 동안
        //# RegisterMonsterTypeBuff 가 호출되는 패턴으로만 갱신된다 (기획서 §4.2).
        private readonly Dictionary<EMonster, List<BattleViewModel.AppliedBuff>> _typeModifierPicks = new();

        //# 스포너 상태 UI — 카드 효과 적용 진입점이 임시로 저장하는 현재 픽 카드. RegisterMonsterTypeBuff 가 source 로 읽는다.
        private CardData _currentCardScope;

        //# 스포너 상태 UI — VM 이 구독. 종 단위 강화가 갱신되면 해당 종 출력 스포너 셀 모두 재계산.
        public event System.Action<EMonster> OnTypeModifierChanged;
```

`BalanceConfig Balance => _balance;` public 노출 (툴팁용):

```csharp
        //# 스포너 상태 UI — 툴팁이 base 스탯(Hp/Power/Range/Cooldown/MoveSpeed)을 읽기 위해 노출.
        //# Plague SlowFactor 의 base 는 코드 상수 Lair.Character.PlagueSlowOnHit.BaseSlowFactor 사용 (§2.5.5).
        public BalanceConfig Balance => _balance;
```

- [ ] **Step 5: `ApplyCardEffect` / `TrackCardPick` / `GetAppliedBuffs` 메서드 추가**

기존 `RegisterMonsterTypeBuff` 끝에 source 추적 호출 + 이벤트 발행 한 줄씩 추가:

```csharp
            //# 카드 source 가 있으면 픽 누적 추적 (직접 호출 / 시뮬레이션 외 경로는 _currentCardScope null).
            if (_currentCardScope != null)
                TrackCardPick(type, stat, _currentCardScope);

            //# VM 셀이 동일 종 출력 스포너를 모두 갱신하도록 broadcast.
            OnTypeModifierChanged?.Invoke(type);
```

새 메서드 3개:

```csharp
        //# 스포너 상태 UI — 카드 효과 적용의 단일 진입점 (기획서 §4.2 BLOCKER 4 결정).
        //# 3개 기존 호출지점(card.Effect.Apply(_ctx))을 이 메서드로 치환해 source 를 잠시 보관한다.
        //# ICardEffect / IBattleContext / 25개 효과 클래스 시그니처는 일체 변경하지 않는다.
        public void ApplyCardEffect(CardData card)
        {
            if (card?.Effect == null || _ctx == null) return;
            _currentCardScope = card;
            try { card.Effect.Apply(_ctx); }
            finally { _currentCardScope = null; }
        }

        //# 스포너 상태 UI — _typeModifierPicks 에 픽 누적. 동일 source 면 PickCount++,
        //# 신규 source 면 add. 동일 종·동일 Stat 의 누적 배율은 _typeModifiers 의 Get(stat) 으로 일괄 갱신.
        private void TrackCardPick(EMonster type, EMonsterStatKind stat, CardData source)
        {
            if (_typeModifierPicks.TryGetValue(type, out var list) == false)
                _typeModifierPicks[type] = list = new List<BattleViewModel.AppliedBuff>();

            var existing = list.Find(b => b.Source == source);
            if (existing != null)
                existing.PickCount++;
            else
                list.Add(new BattleViewModel.AppliedBuff
                {
                    Source = source,
                    PickCount = 1,
                    Stat = stat,
                    AggregateMultiplier = 1f,
                });

            //# 동일 종·동일 Stat 의 엔트리들에 누적 배율을 일괄 동기화 (종 1 ↔ 카드 1 매핑이지만
            //# 향후 1↔다 매핑 확장에 대비해 list 순회로 갱신).
            if (_typeModifiers.TryGetValue(type, out var mul))
            {
                foreach (var b in list)
                    if (b.Stat == stat) b.AggregateMultiplier = mul.Get(stat);
            }
        }

        //# 스포너 상태 UI — VM 이 SpawnerSnapshot 채울 때 사용. 없는 종이면 빈 array.
        public IReadOnlyList<BattleViewModel.AppliedBuff> GetAppliedBuffs(EMonster type)
            => _typeModifierPicks.TryGetValue(type, out var list)
                ? (IReadOnlyList<BattleViewModel.AppliedBuff>)list
                : System.Array.Empty<BattleViewModel.AppliedBuff>();
```

- [ ] **Step 6: 기존 `card.Effect.Apply(_ctx)` 호출 3곳을 `ApplyCardEffect(card)` 로 치환**

`BattleController.cs` 내에서 `card.Effect.Apply(_ctx)` 검색해 3곳 치환 (TryProcessNext 본문의 카드 픽 콜백 2곳 + DebugApplyCard). 각 위치는 staging diff 참조 — 시뮬레이션 분기, 실제 픽 콜백, `DebugApplyCard`.

```csharp
                        //# 스포너 상태 UI — source 추적용 단일 진입점 (기획서 §4.2).
                        ApplyCardEffect(picked);
```

- [ ] **Step 7: 테스트 실행 — 14 케이스 PASS 확인**

`editor_execute_menu("Lair/Tests/Run EditMode Tests")` 후 `BattleControllerCardScopeTests` 전 케이스 통과 확인.

- [ ] **Step 8: git add**

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs Assets/_Lair/Scripts/UI/BattleViewModel.cs Assets/_Lair/Tests/EditMode/Battle/BattleControllerCardScopeTests.cs
```

커밋 메시지(안):
```
# [feat] - BattleController 카드 source 추적 + AppliedBuff/SpawnerSnapshot
```

---

### Task 7: `BattleViewModel.AttachSpawners` 통합

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BattleViewModel.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/BattleViewModelSpawnerSnapshotTests.cs` (신규)

- [ ] **Step 1: 실패 테스트 — `BattleViewModelSpawnerSnapshotTests.cs`**

핵심 케이스 (전체 본문은 staging 된 같은 파일):

  - `AttachSpawners_6개_초기_스냅샷_채워진다`
  - `AttachSpawners_초기_OutputCount_폴링값_반영`
  - `AttachSpawners_null_인자시_noop_예외없음`
  - `AttachSpawners_배열에_null_있어도_정상_채워짐`
  - `IncrementOutput_VM_해당_인덱스_스냅샷_갱신_이벤트_발행`
  - `ReplaceOutput_VM_해당_인덱스_CurrentType_갱신`
  - `ReplaceOutput_후_AppliedBuffs는_새_종_기준`
  - `강화_카드_픽시_동일_종_모든_인덱스_스냅샷_갱신`
  - `다른_종_강화_픽시_미매칭_인덱스_이벤트_미발행`
  - `같은_종_2픽_연속_각_매칭_인덱스_2회씩_통지`
  - `DetachSpawners_이후_Spawner_이벤트_미수신`
  - `DetachSpawners_이후_OnTypeModifierChanged_미수신`
  - `DetachSpawners_이후_Spawners_컬렉션_비워짐`
  - `DetachSpawners_두번_호출시_예외없음`
  - `AttachSpawners_중복_호출시_이전_구독_해제후_새로_attach`
  - `Spawners_프로퍼티는_IReadOnlyList_타입`

- [ ] **Step 2: `BattleViewModel` 필드 추가**

`_build` 필드 옆에 캐시·구독 핸들 필드 추가:

```csharp
        private readonly List<SpawnerSnapshot> _spawnerSnapshots = new();

        //# AttachSpawners 가 보관 — Detach 시 동일 인스턴스로 unsubscribe.
        private IReadOnlyList<Spawner> _attachedSpawners;
        private BattleController _attachedController;
        //# Spawner 별로 등록한 핸들러 캐시 — Detach 시 정확히 해제 (대응 인덱스 클로저).
        private Action<EMonster>[] _outputTypeHandlers;
        private Action<int>[] _outputCountHandlers;
        private Action<EMonster> _typeModifierHandler;

        //# 스포너 스냅샷 단독 갱신 — 6개 중 변경된 1개 인덱스만 알린다.
        public event Action<int> OnSpawnerSnapshotChanged;

        //# 스포너 스냅샷 — 인덱스 0~5, AttachSpawners 이후에만 유효.
        public IReadOnlyList<SpawnerSnapshot> Spawners => _spawnerSnapshots;
```

- [ ] **Step 3: `AttachSpawners` / `DetachSpawners` / 핸들러들 추가**

`AddPick` 메서드 다음에 추가. 본문은 staging 된 `BattleViewModel.cs:117-213` 그대로 — 6 Spawner + BattleController 를 받아 초기 스냅샷을 폴링으로 채우고, 인덱스별 클로저로 `OnOutputTypeChanged` / `OnOutputCountChanged` 구독, 컨트롤러 `OnTypeModifierChanged` 구독. `DetachSpawners` 는 모든 구독 정확히 해제. 멱등 보장 (`AttachSpawners` 중복 호출 시 내부에서 `DetachSpawners` 먼저). `BuildSnapshot` private 메서드가 `controller.GetAppliedBuffs(sp.CurrentType)` 결과를 스냅샷에 담는다.

- [ ] **Step 4: 테스트 실행 — 16 케이스 PASS**

- [ ] **Step 5: 회귀 — `ICardEffect.Apply(IBattleContext)` 시그니처 보존 확인**

`Assets/_Lair/Tests/EditMode/Card/CardEffectSignatureRegressionTests.cs` 신규 — reflection 으로 시그니처 검증 (Task 6 의 결정이 시그니처를 안 바꾸는지 회귀).

```csharp
using System.Reflection;
using NUnit.Framework;
using Lair.Card;

namespace Lair.Tests.Card
{
    //# 스포너 상태 UI BLOCKER 4 — ICardEffect.Apply 시그니처가 변하지 않았음을 회귀로 박제.
    public class CardEffectSignatureRegressionTests
    {
        [Test]
        public void ICardEffect_Apply는_IBattleContext_단일_인자()
        {
            var mi = typeof(ICardEffect).GetMethod("Apply");
            Assert.IsNotNull(mi, "Apply 메서드 존재");
            var ps = mi.GetParameters();
            Assert.AreEqual(1, ps.Length, "인자 1개");
            Assert.AreEqual(typeof(IBattleContext), ps[0].ParameterType, "인자 타입 = IBattleContext");
            Assert.AreEqual(typeof(void), mi.ReturnType, "반환 void");
        }
    }
}
```

- [ ] **Step 6: git add**

```bash
git add Assets/_Lair/Scripts/UI/BattleViewModel.cs Assets/_Lair/Tests/EditMode/Battle/BattleViewModelSpawnerSnapshotTests.cs Assets/_Lair/Tests/EditMode/Card/CardEffectSignatureRegressionTests.cs
```

커밋 메시지(안):
```
# [feat] - BattleViewModel AttachSpawners + SpawnerSnapshot 이벤트 푸시
```

---

## 마일스톤 M3 — UI 신규 4종

### Task 8: `SpawnerStatusCell.cs`

**Files:**
- Create: `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs`
- Test: `Assets/_Lair/Tests/EditMode/UI/SpawnerStatusCellMappingTests.cs`
- Test: `Assets/_Lair/Tests/EditMode/UI/SpawnerStatusCellTests.cs`

- [ ] **Step 1: 매핑 테스트 추가 — `SpawnerStatusCellMappingTests.cs`**

`SpeciesColor` / `SpeciesName` / `IconLetterFor` 가 기획서 §2.3.3 / §2.4 표대로 매핑되는지 검증.

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 셀 매핑 표 (기획서 §2.3.3 · §2.4) 회귀.
    public class SpawnerStatusCellMappingTests
    {
        [TestCase(EMonster.Wisp,    "Wisp")]
        [TestCase(EMonster.Wraith,  "Wraith")]
        [TestCase(EMonster.Reaper,  "Reaper")]
        [TestCase(EMonster.Hex,     "Hex")]
        [TestCase(EMonster.Plague,  "Plague")]
        [TestCase(EMonster.Phantom, "Phantom")]
        public void SpeciesName_종_영문풀네임_매핑(EMonster t, string expected)
            => Assert.AreEqual(expected, SpawnerStatusCell.SpeciesName(t));

        [TestCase(ECardId.WispHpBoost,           'H')]
        [TestCase(ECardId.WraithDamageBoost,     'D')]
        [TestCase(ECardId.ReaperAtkSpeed,        'S')]
        [TestCase(ECardId.HexRangeBoost,         'R')]
        [TestCase(ECardId.PhantomMoveSpeedBoost, 'M')]
        [TestCase(ECardId.PlagueSlowBoost,       'P')]
        public void IconLetterFor_강화_카드_글자_매핑(ECardId id, char expected)
        {
            var info = SpawnerStatusCell.IconLetterFor(id);
            Assert.AreEqual(expected, info.letter);
        }

        //# Phantom 만 흰 글자, 나머지 종은 검은 글자 (가독성 §2.3.3).
        [Test]
        public void IconLetterFor_Phantom은_흰_글자_나머지는_검정()
        {
            Assert.AreEqual(Color.white, SpawnerStatusCell.IconLetterFor(ECardId.PhantomMoveSpeedBoost).fgColor);
            Assert.AreEqual(Color.black, SpawnerStatusCell.IconLetterFor(ECardId.WispHpBoost).fgColor);
        }

        //# 강화 카드가 아닌 ID 는 letter == ' ' (방어 — UI 가 row 숨김).
        [Test]
        public void IconLetterFor_강화카드_아니면_공백_글자()
        {
            var info = SpawnerStatusCell.IconLetterFor(ECardId.Berserk);
            Assert.AreEqual(' ', info.letter);
        }
    }
}
```

- [ ] **Step 2: 셀 동작 테스트 — `SpawnerStatusCellTests.cs`**

`Bind(snapshot, progress, onClick)` 호출 후 색칩 색, 종명, ×N 표시, 진행 바 fillAmount 색상 전환 (Cool ↔ Warm) 검증. UI Image / TMP_Text 가 필요해 `[UnityTest]` 또는 `GameObject` 동적 생성. (staging 된 `SpawnerStatusCellTests.cs` 그대로.)

- [ ] **Step 3: `SpawnerStatusCell.cs` 구현**

본문은 staging 된 `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs` 그대로 — 약 200 줄. 요지:

- `[SerializeField]` 11개 — `_border`/`_colorChip`/`_speciesText`/`_countText`/`_progressFill`/`_button`/`_iconRow`/`_iconCircle`/`_iconLetter`/`_iconBadge`. UI 컴포넌트는 `CHText` · `CHButton` (Rule 11).
- `OnEnable` — 풀 재사용 대비 상태 리셋 (`_disposable.Clear`, `_countText`/`_iconRow` 숨김, `_progressFill.fillAmount = 0`, 활성 테두리 off) (Rule 12).
- `Bind(snapshot, progress, onClick)` — `_progressSource` 저장, `RebindSnapshot(snap)` 호출, `_button.OnClick(() => onClick(idx), _disposable)`.
- `RebindSnapshot(snap)` — 색칩 색 / 종명 / ×N 표시 (count ≥ 2) / `RebindIconRow`.
- `RebindIconRow` — `snap.AppliedBuffs` 가 비면 row 숨김, 있으면 첫 buff 의 `Source.Id` 로 `IconLetterFor` 매핑 적용. 배지 (`PickCount ≥ 2`).
- `Update` — `_progressSource.Progress` 매 프레임 폴링 → `fillAmount` + Cool/Warm 색 전환 (threshold 0.7).
- `SetActiveBorder(bool)` — 패널이 활성 테두리 토글.
- `static SpeciesColor` / `SpeciesName` / `IconLetterFor` — 매핑 함수 (Test 가 직접 호출 가능하도록 public).

색상 상수는 `public static readonly Color`:
- `CoolColor` = `#60A5FA`, `WarmColor` = `#F97316`, `BarBackgroundColor` = `#374151`
- `ActiveBorderColor` = `#FBBF24`, `InactiveBorderColor` = `(0,0,0,0)`
- `CountTextColor` = `#FBBF24`
- `WarmThreshold` = `0.7f`

- [ ] **Step 4: 테스트 실행 — 셀 매핑/동작 케이스 PASS**

- [ ] **Step 5: git add**

```bash
git add Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs Assets/_Lair/Tests/EditMode/UI/SpawnerStatusCellMappingTests.cs Assets/_Lair/Tests/EditMode/UI/SpawnerStatusCellTests.cs
```

커밋 메시지(안):
```
# [feat] - SpawnerStatusCell 신규 (색칩/×N/진행바/강화 row)
```

---

### Task 9: `SpawnerStatusTooltip.cs` (Rule 13 — UIArg 페어)

**Files:**
- Create: `Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs`
- Test: `Assets/_Lair/Tests/EditMode/UI/SpawnerStatusTooltipFormatTests.cs`

- [ ] **Step 1: 포맷 테스트 — `SpawnerStatusTooltipFormatTests.cs`**

`FormatBuffLine` 또는 동등 static 헬퍼로 각 `EMonsterStatKind` 분기 (Hp/Power/Range/MoveSpeed/Cooldown/SlowFactor) 가 올바른 한글 라벨 + base→result 변환을 출력하는지 검증. Cooldown 은 역수(공격속도) 표시 확인.

- [ ] **Step 2: `SpawnerStatusTooltip.cs` 구현 — UIArg 페어**

같은 파일 상단에 `SpawnerStatusTooltipArg : UIArg` (Rule 13). 필드: `int SpawnerIndex`, `BattleViewModel ViewModel`, `RectTransform AnchorCell`, `Action<int> OnClosed`, `BalanceConfig Balance`.

본 클래스 `SpawnerStatusTooltip : UIBase`. `[SerializeField]` 3개: `_root`/`_headerText`/`_buffText`. `InitUI(arg)`:
1. arg 캐스팅 + `_vm = _arg.ViewModel`.
2. `RefreshContent()` (헤더 + 강화 줄) + `PositionAboveAnchor()`.
3. `_vm.OnSpawnerSnapshotChanged += HandleSnapshotChanged` 구독 → `closeDisposable.Add` 로 해제 예약.
4. `_arg.OnClosed` 가 있으면 `closeDisposable.Add(() => onClosed(closedIndex))` — closedIndex 캡처해 stale 콜백 self-ignore 가능.

`PositionAboveAnchor` — 셀 RectTransform 의 월드 좌상단을 캔버스 로컬로 변환 후 `_root.anchoredPosition = (clampedX, localY + 8)`. 좌우 clamp (halfWidth + 4px margin).

`FormatBuffLine` — 6 스탯 분기. Plague SlowFactor 만 `PlagueSlowOnHit.BaseSlowFactor` 상수, 나머지는 `arg.Balance.GetMonster(type)` 의 해당 필드. Cooldown 은 cd → aspeed 역수 표시.

본문 전체는 staging 된 `Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs` 그대로.

- [ ] **Step 3: 테스트 PASS**

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs Assets/_Lair/Tests/EditMode/UI/SpawnerStatusTooltipFormatTests.cs
```

커밋 메시지(안):
```
# [feat] - SpawnerStatusTooltip 신규 (셀 위 floating, 강화 줄 포맷)
```

---

### Task 10: `SpawnerStatusPanel.cs`

**Files:**
- Create: `Assets/_Lair/Scripts/UI/SpawnerStatusPanel.cs`

- [ ] **Step 1: `SpawnerStatusPanel.cs` 구현**

본문은 staging 된 `Assets/_Lair/Scripts/UI/SpawnerStatusPanel.cs` 그대로. 핵심:

- `[SerializeField] Transform _container` + `GameObject _cellPrefab`.
- `Bind(vm, IReadOnlyList<Spawner> spawners, BalanceConfig balance)` — VM 구독 + `RebuildAll()`.
- `RebuildAll` — `vm.Spawners` 순회. 각 인덱스에 `CHMPool.Pop(_cellPrefab, _container)` (Rule 12) → `SpawnerStatusCell.Bind(snap, spawners[i] as ISpawnerProgress, HandleCellClicked)`.
- `Unbind` — VM 구독 해제 + 열려있던 툴팁 닫기 + 셀들 `CHMPool.Push`.
- `HandleSnapshotChanged(int index)` — 해당 셀 `RebindSnapshot(vm.Spawners[index])`.
- `HandleCellClicked(int index)` — 같은 셀 재클릭 토글 / 다른 셀 전환. `CHMUI.Instance.ShowUI(EUI.SpawnerStatusTooltip, arg, ui => _openTooltipInstance = ui)`. `arg.OnClosed = HandleTooltipClosed`, `arg.SpawnerIndex = index`.
- `HandleTooltipClosed(int closedIndex)` — `closedIndex != _openCellIndex` 면 stale 콜백 무시 (advisor BLOCKER). 일치하면 활성 테두리 원복 + `_openCellIndex = -1`.
- `CloseTooltip()` — `_openTooltipInstance.Close(reuse: true)` 또는 `CHMUI.CloseUI` fallback.

- [ ] **Step 2: 컴파일 — 에러 0**

- [ ] **Step 3: git add**

```bash
git add Assets/_Lair/Scripts/UI/SpawnerStatusPanel.cs
```

커밋 메시지(안):
```
# [feat] - SpawnerStatusPanel 신규 (6셀 + 툴팁 토글)
```

---

### Task 11: `BuildModalPopup.cs` + `BuildModalCardCell`

**Files:**
- Create: `Assets/_Lair/Scripts/UI/BuildModalPopup.cs`
- Test: `Assets/_Lair/Tests/EditMode/UI/BuildModalPopupTests.cs`

- [ ] **Step 1: 테스트 — `BuildModalPopupTests.cs`**

`Build(vm)` 호출 시 패시브/액티브 분리 + 카테고리 그룹 정렬 (Enhance → Spawn → Replace → Environment) 검증. (staging 된 같은 파일.)

- [ ] **Step 2: `BuildModalPopup.cs` 구현 — Rule 13 페어**

같은 파일 상단에 `BuildModalPopupArg : UIArg { BattleViewModel ViewModel; }`.

본 클래스 `BuildModalPopup : UIBase`. `[SerializeField]` 7개: `_dimButton`/`_closeButton`/`_passiveContent`/`_activeContent`/`_cellPrefab`/`_passiveEmptyText`/`_activeEmptyText`.

- `InitUI` — `Build(vm)` + dim/close 버튼 `OnClick(() => Close(reuse: true), closeDisposable)`.
- `Close(reuse)` — `_spawnedCells` 의 모든 GameObject 를 `CHMPool.Push` 반환 (Rule 12).
- `Build(vm)` — `vm.Build` 를 IsPassive 로 분리, 패시브는 `CategoryOrder` 로 안정 정렬 (Enhance=0, Spawn=1, Replace=2, Environment=3), 액티브는 픽 순서 유지.
- `FillSection` — `CHMPool.Pop(_cellPrefab, content)` → `BuildModalCardCell.Bind(card, count)`.

같은 파일에 `BuildModalCardCell : MonoBehaviour` 도 정의 (Rule 13 정신 — 관련 코드 응집). `[SerializeField] Image _frame; CHText _nameText; CHText _countText; CHText _descText;`. `Bind(card, count)` — `_frame.color = CardView.CategoryColor(card.Category)`, `_nameText.SetText(DisplayName)`, `_descText.SetText(Description)`, `_countText` 는 `count >= 2` 일 때만 표시.

- [ ] **Step 3: 테스트 PASS**

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Scripts/UI/BuildModalPopup.cs Assets/_Lair/Tests/EditMode/UI/BuildModalPopupTests.cs
```

커밋 메시지(안):
```
# [feat] - BuildModalPopup 신규 (픽한 모든 카드 모달)
```

---

### Task 12: `BuildPanel` 수정 — 종 강화 필터 + 루트 클릭 → 모달

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BuildPanel.cs`
- Test: `Assets/_Lair/Tests/EditMode/UI/BuildPanelFilterTests.cs`

- [ ] **Step 1: 필터 회귀 테스트 — `BuildPanelFilterTests.cs`**

`Refresh` 가 `Enhance + IsPassive` 카드를 셀로 만들지 않는지 검증. 액티브 4장(Berserk/BloodThirst/Frenzy/IronWill) 은 `Enhance` 카테고리지만 IsPassive=false 라 통과해야 한다.

- [ ] **Step 2: `BuildPanel.cs` 수정**

- `_rootButton` `[SerializeField] CHButton` 추가 — 패널 루트 클릭 (자식 셀의 클릭 콜백은 제거, 셀에 버튼 두면 propagation 안 됨).
- `Refresh` 의 entry 순회에 필터 한 줄:

```csharp
                //# 종 강화 패시브 6장 (Enhance + IsPassive) 제외 (기획서 §2.6.1, design-reviewer BLOCKER 1).
                //# 액티브 4장(Berserk/BloodThirst/Frenzy/IronWill) 도 _category 가 Enhance 로 직렬화돼 있지만
                //# IsPassive = false 라 통과한다.
                if (entry.Card.Category == ECardCategory.Enhance && entry.IsPassive) continue;
```

- `Bind(vm)` 에 루트 버튼 핸들러:

```csharp
            //# 루트 클릭 → BuildModalPopup. CHMUI 가 단일 인스턴스 caching 으로 재사용.
            if (_rootButton != null)
            {
                _rootButton.OnClick(() =>
                {
                    if (_vm == null) return;
                    CHMUI.Instance.ShowUI(EUI.BuildModalPopup, new BuildModalPopupArg { ViewModel = _vm });
                }, _disposable);
            }
```

- 자식 셀 클릭 콜백 제거: `cell.Bind(entry.Card, null)` (두 번째 인자 null — 셀이 클릭 라이센스 안 가짐).

- 기존 `_detailRoot` / `ShowDetail` / `_detailShown` 모두 제거.

- [ ] **Step 3: 테스트 PASS — 필터 케이스 + 기존 회귀**

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Scripts/UI/BuildPanel.cs Assets/_Lair/Tests/EditMode/UI/BuildPanelFilterTests.cs
```

커밋 메시지(안):
```
# [refactor] - BuildPanel 종 강화 필터 + 루트 클릭 → 모달
```

---

### Task 13: `BattleHud` — `SpawnerStatusPanel` 바인딩 + `BattleHudArg` 확장

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/BattleHud.cs`

- [ ] **Step 1: `BattleHudArg` 확장**

상단의 `BattleHudArg` 에 두 필드 추가:

```csharp
    public class BattleHudArg : UIArg
    {
        public BattleViewModel ViewModel;
        //# 스포너 상태 UI — 진행 바 폴링용 ISpawnerProgress 6개.
        public IReadOnlyList<Spawner> Spawners;
        //# 스포너 상태 UI — 툴팁이 base 스탯을 읽기 위한 단일 진실.
        public BalanceConfig Balance;
    }
```

- [ ] **Step 2: `BattleHud` 에 `_spawnerStatusPanel` 직렬 필드 + Bind 후크 추가**

```csharp
        //# 스포너 상태 UI — 화면 하단 6셀 패널 (기획서 §2.1).
        [SerializeField] private SpawnerStatusPanel _spawnerStatusPanel;
```

`Bind(ba)` 본문에 (BuildPanel Bind 다음 위치):

```csharp
            //# 스포너 상태 패널 바인딩 (Close 시 자동 해제)
            if (_spawnerStatusPanel != null)
            {
                _spawnerStatusPanel.Bind(vm, ba.Spawners, ba.Balance);
                closeDisposable.Add(() => _spawnerStatusPanel.Unbind());
            }
```

`InitUI` 시그니처는 그대로 `arg is BattleHudArg ba` 캐스팅 후 `Bind(ba)` 호출 — `ba` 객체를 통째로 넘겨 `Spawners`/`Balance` 까지 전달.

- [ ] **Step 3: 컴파일 — 에러 0**

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Scripts/UI/BattleHud.cs
```

커밋 메시지(안):
```
# [refactor] - BattleHud SpawnerStatusPanel 바인딩 + Args 확장
```

---

## 마일스톤 M4 — BattleController 통합

### Task 14: `BattleController.BindSpawners` 가 VM.AttachSpawners + HUD args 주입 + OnDestroy 정리

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: `Start` 의 `ShowUIAsync(EUI.BattleHud, ...)` 호출 인자 확장**

```csharp
            //# 3. HUD 표시 — 스포너 상태 UI 가 진행 바 폴링·툴팁 base 스탯 표시에 필요한 Spawners·Balance 함께 주입.
            await CHMUI.Instance.ShowUIAsync(EUI.BattleHud,
                new BattleHudArg { ViewModel = _vm, Spawners = _spawners, Balance = _balance });
```

- [ ] **Step 2: `BindSpawners` 가 끝에 VM.AttachSpawners 호출**

```csharp
        //# 지속 스폰 — 씬의 Spawner 들에 호스트 주입. 이후 Update 가 각자 주기 틱.
        //# 스포너 상태 UI — Spawner 6개 + 본 컨트롤러를 VM 에 묶어 SpawnerSnapshot 통합 노출.
        private void BindSpawners()
        {
            if (_spawners == null) return;
            foreach (var sp in _spawners)
                if (sp != null) sp.Bind(this);
            //# VM 이 초기 스냅샷 폴링 + 이벤트 구독을 시작. Detach 는 OnDestroy 에서.
            _vm?.AttachSpawners(_spawners, this);
        }
```

- [ ] **Step 3: `OnDestroy` 에 `_vm.DetachSpawners()` 추가**

```csharp
        //# 정적 이벤트 구독 해제 — 씬 재시작 시 누수 방지.
        //# 스포너 상태 UI — VM 이 Spawner / 본 컨트롤러 이벤트를 구독했으므로 함께 해제.
        private void OnDestroy()
        {
            DespawnOnDeath.MonsterDied -= HandleMonsterDied;
            _vm?.DetachSpawners();
        }
```

- [ ] **Step 4: 컴파일 + 기존 회귀 테스트 PASS**

- [ ] **Step 5: git add**

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs
```

커밋 메시지(안):
```
# [feat] - BattleController VM.AttachSpawners + HUD args + Detach
```

---

## 마일스톤 M5 — 마이그레이션 (제거 + 빌더)

### Task 15: `SpawnerCooldownBar` 제거

**Files:**
- Delete: `Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs` (+ .meta)
- Delete: `Assets/_Lair/Tests/EditMode/Battle/SpawnerCooldownBarTests.cs` (+ .meta)

- [ ] **Step 1: 두 파일 + .meta 삭제**

`SpawnerStatusCell.Update` 가 동일 역할(progress 폴링 + Cool/Warm 색 전환)을 화면 공간에서 수행하므로 World-space 진행 바는 폐기.

- [ ] **Step 2: 컴파일 — `SpawnerCooldownBar` 참조 잔존 확인 (다른 곳에서 import 했으면 정리)**

Grep `SpawnerCooldownBar` 로 잔존 0 확인.

- [ ] **Step 3: git add (삭제는 `git rm` 이지만 unity 의 .meta 관계상 `git add -A <path>` 도 가능)**

```bash
git add Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs.meta Assets/_Lair/Tests/EditMode/Battle/SpawnerCooldownBarTests.cs Assets/_Lair/Tests/EditMode/Battle/SpawnerCooldownBarTests.cs.meta
```

커밋 메시지(안):
```
# [refactor] - SpawnerCooldownBar 제거 (BattleHud 6셀로 이전)
```

---

### Task 16: `LairSpawnerStatusUIBuilder.cs` 신규 + `LairSpawnerVisualBuilder` 진행 바 제거

**Files:**
- Create: `Assets/_Lair/Editor/LairSpawnerStatusUIBuilder.cs`
- Modify: `Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs`
- Modify: `Assets/_Lair/Editor/LairUIPrefabBuilder.cs`

- [ ] **Step 1: `LairSpawnerStatusUIBuilder.cs` — 메뉴 `Lair/Setup/Spawner Status UI`**

본문은 staging 된 `Assets/_Lair/Editor/LairSpawnerStatusUIBuilder.cs` 그대로. 핵심 책임:

- `BuildAll` → 4 프리팹 생성:
  - `SpawnerStatusCell.prefab` — `Image`(배경) + 자식 `_border` + `_colorChip` + `_speciesText` (CHText) + `_countText` + `_progressFill` (Image FilledRadial360 or Horizontal) + `_button` (CHButton) + `_iconRow` + `_iconCircle` + `_iconLetter` + `_iconBadge`. `CHPoolable` 부착 (Rule 12).
  - `SpawnerStatusPanel.prefab` — `HorizontalLayoutGroup` 컨테이너 + `SpawnerStatusPanel` 컴포넌트. `_cellPrefab` 필드에 위 셀 프리팹 연결.
  - `SpawnerStatusTooltip.prefab` — `UIBase` 자식, `_root` `_headerText` `_buffText`. Addressables 그룹 `Resource`, 라벨 `Resource` 로 등록 (CHMUI 가 `EUI.SpawnerStatusTooltip` 키로 로드).
  - `BuildModalPopup.prefab` — dim button + close button + 패시브/액티브 ScrollRect + `BuildModalCardCell.prefab` 자식 셀. Addressables 등록.
- 색상 상수는 클래스 내 `static readonly Color` 로 명시. 셀 폭 46 / 높이 56 / 간격 6.
- 파일명 = Enum 값명 (Rule 08).
- 위치 = `Assets/_Lair/Art/UI/` (Rule 14).

- [ ] **Step 2: `LairSpawnerVisualBuilder` 의 진행 바 빌드 스텝 제거**

기존 빌더가 World-space `CooldownBarWrapper` 자식 + `SpawnerCooldownBar` 부착 스텝을 가지고 있으면 삭제. 디스크 본체(`SpawnerBody`) + 머티리얼 + tint 스텝만 남긴다.

- [ ] **Step 3: `LairUIPrefabBuilder` — `BattleHud.prefab` 빌드 시 `SpawnerStatusPanel` 자식으로 nest**

기존 BattleHud 빌더에서 마지막에 `SpawnerStatusPanel.prefab` 을 instantiate 해서 HUD 하단 자식으로 두고 `_spawnerStatusPanel` 직렬 필드 연결.

- [ ] **Step 4: 메뉴 실행 — 4 프리팹 생성 확인**

`editor_execute_menu("Lair/Setup/Spawner Status UI")` 실행 → `Assets/_Lair/Art/UI/` 에 4 .prefab + .meta 생성. Addressables `SpawnerStatusTooltip` / `BuildModalPopup` 라벨 등록 확인.

`editor_execute_menu("Lair/Setup/UI Prefabs")` 재실행 → `BattleHud.prefab` 에 `SpawnerStatusPanel` nested 확인.

- [ ] **Step 5: git add (빌더 + 생성된 프리팹 + AddressableAssetSettings)**

```bash
git add Assets/_Lair/Editor/LairSpawnerStatusUIBuilder.cs Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs Assets/_Lair/Editor/LairUIPrefabBuilder.cs Assets/_Lair/Art/UI/SpawnerStatusCell.prefab Assets/_Lair/Art/UI/SpawnerStatusCell.prefab.meta Assets/_Lair/Art/UI/SpawnerStatusPanel.prefab Assets/_Lair/Art/UI/SpawnerStatusPanel.prefab.meta Assets/_Lair/Art/UI/SpawnerStatusTooltip.prefab Assets/_Lair/Art/UI/SpawnerStatusTooltip.prefab.meta Assets/_Lair/Art/UI/BuildModalPopup.prefab Assets/_Lair/Art/UI/BuildModalPopup.prefab.meta Assets/_Lair/Art/UI/BuildModalCardCell.prefab Assets/_Lair/Art/UI/BuildModalCardCell.prefab.meta Assets/_Lair/Art/UI/BattleHud.prefab Assets/AddressableAssetsData/AddressableAssetSettings.asset Assets/AddressableAssetsData/AssetGroups/Resource.asset
```

커밋 메시지(안):
```
# [asset] - 스포너 상태 UI 프리팹 4종 + Addressable 등록 + BattleHud 통합
```

---

### Task 17: `Battle.unity` 씬 — Spawner 자식 `CooldownBarWrapper` 제거

**Files:**
- Modify: `Assets/_Lair/Scenes/Battle.unity`

- [ ] **Step 1: 씬을 Unity 에서 연 상태로 6 Spawner GameObject 의 `CooldownBarWrapper` 자식 삭제**

UnityMCP `editor_open_scene` → `scene_hierarchy` → 각 Spawner 의 `CooldownBarWrapper` 노드를 `game_object_destroy`.

- [ ] **Step 2: `SpawnerBody` 자식은 그대로 유지** (Replace 카드 시각 피드백)

- [ ] **Step 3: 씬 저장**

`editor_save_scene`

- [ ] **Step 4: git add**

```bash
git add Assets/_Lair/Scenes/Battle.unity
```

커밋 메시지(안):
```
# [asset] - Battle 씬 CooldownBarWrapper 제거
```

---

## 마일스톤 M6 — PlayMode 통합 검증

### Task 18: PlayMode 통합 — Battle 씬 로드 후 VM.Spawners 6개

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/SpawnerStatusUIPlayTests.cs`

- [ ] **Step 1: PlayMode 테스트 작성**

본문은 staging 된 `Assets/_Lair/Tests/PlayMode/SpawnerStatusUIPlayTests.cs` 그대로 — `SceneManager.LoadSceneAsync("Battle")` → `BattleController` 찾을 때까지 5초 대기 → `DebugAutoPicker` 로 카드 팝업 hang 방지 → 3초 대기 (Start 비동기) → `SpawnerStatusPanel` reflection 으로 `_vm` 꺼내 `vm.Spawners.Count == 6` + 각 인덱스 `Index` 정합 + 초기 `OutputCount == 1` 확인.

- [ ] **Step 2: PlayMode 실행**

```
editor_execute_menu("Lair/Tests/Run PlayMode Tests")
```

폴링: `Library/lair-test-result.json` `"done": true` 까지. Expected: 신규 케이스 PASS.

- [ ] **Step 3: 회귀 — 기존 PlayMode 케이스도 통과 확인**

`AutoCombatAIRotationTests`, `SimpleRotatorPlayTests` 등 기존 케이스가 그대로 통과해야 한다.

- [ ] **Step 4: 게임 실행 — 수동 smoke (선택)**

`sim_play` → 5분 자동 전투. 셀 6개 화면 하단 표시, 진행 바 Cool→Warm 전환, 카드 픽 시 강화 row 갱신, 셀 클릭 시 툴팁 toggle, BuildPanel 클릭 시 모달 표시 — 정성 검증.

- [ ] **Step 5: git add**

```bash
git add Assets/_Lair/Tests/PlayMode/SpawnerStatusUIPlayTests.cs Assets/_Lair/Tests/PlayMode/SpawnerStatusUIPlayTests.cs.meta
```

커밋 메시지(안):
```
# [test] - SpawnerStatusUI PlayMode 통합 검증
```

---

## 전체 회귀 체크 (Task 18 완료 후)

- [ ] **EditMode 전 케이스 PASS**

```
editor_execute_menu("Lair/Tests/Run EditMode Tests")
```

신규 추가 케이스 (대략):
- SpawnerOutputCountTests · SpawnerOutputEventTests
- StatMultiplierTests (Get 보강)
- BattleControllerCardScopeTests
- BattleViewModelSpawnerSnapshotTests
- CardEffectSignatureRegressionTests
- SpawnerStatusCellMappingTests · SpawnerStatusCellTests
- SpawnerStatusTooltipFormatTests
- BuildPanelFilterTests
- BuildModalPopupTests

총 ~60 개 신규 케이스. 기존 케이스 0 회귀.

- [ ] **PlayMode PASS**

`SpawnerStatusUIPlayTests` + 기존 `AutoCombatAIRotationTests` 등.

- [ ] **수동 smoke (Battle 씬 5분 자동 전투)**

- 6셀 화면 하단 가운데 정렬 표시
- 진행 바 Cool→Warm 전환 (threshold 0.7)
- 카드 픽 시 동일 종 출력 셀들이 상단 강화 row 일제 갱신
- 셀 클릭 시 셀 위 툴팁 표시, 다른 셀 클릭 시 전환, 같은 셀 재클릭 시 닫힘
- BuildPanel 클릭 시 화면 중앙 모달 표시, 강화 카드 포함 모든 픽 표시
- World-space 진행 바 없음 (디스크 본체만 남음)

---

## 룰 위반 자체 체크 (Rule 별)

- [ ] **Rule 01** — `git commit` 직접 실행 0회. 모든 Task 가 `git add` + 커밋 메시지(안)까지만.
- [ ] **Rule 02** — 신규 추가 단일 라인 주석 100% `//#` 접두어.
- [ ] **Rule 05** — Spawner/BattleController → VM → View 단방향. View 가 Model 직접 참조 0건.
- [ ] **Rule 07** — `CHMResource`/`CHMUI`/`CHMPool`/`CHText`/`CHButton` 사용. 신규 함수 없음 (게임 코드 종속 X).
- [ ] **Rule 08** — `BuildModalPopup.prefab`, `SpawnerStatusTooltip.prefab` 파일명 = Enum 값명.
- [ ] **Rule 09** — `EUI` 신규 값 2개 모두 `CommonEnum.cs` 에 통합.
- [ ] **Rule 10** — `ISpawnerOutputProvider` 확장은 `Battle/CommonInterface.cs` 안에서. 신규 인터페이스 분리 없음.
- [ ] **Rule 11** — UI 컴포넌트 100% `CHText`/`CHButton` 래퍼. Legacy Text · 단일 Button 직접 사용 0건.
- [ ] **Rule 12** — `CHMPool.Pop/Push` 만. `Instantiate`/`CreatePrimitive` 직접 호출 0건. 셀 풀 재사용은 `OnEnable` state reset.
- [ ] **Rule 13** — `SpawnerStatusTooltipArg`/`BuildModalPopupArg`/`BattleHudArg` 모두 페어 `UIBase` 와 같은 파일.
- [ ] **Rule 14** — 신규 프리팹 4종 모두 `Assets/_Lair/Art/UI/` 하위.

---

## 검증 가설 (스펙 §11)

본 변경이 사용자 경험을 개선했는지 5분 한 판 플레이로 확인:

1. 카드 픽 화면에서 "어느 스포너에 무엇이 적용됐는지" 즉시 보고 결정한다 (시너지 가시성)
2. 6 스포너 상태를 화면 하단 한 줄로 동시 비교한다 (공간 분산 ↓)
3. BuildPanel 모달로 픽한 모든 카드 효과를 빠르게 복기한다

검증 실패 시 후속 작업:
- 셀 정렬 옵션 (종 그룹화) 도입
- 모달 열림 시 일시정지 결합
- 디스크 본체 색상 강조 강화

---

## 변경 이력

- **v0.1 (2026-05-27)**: 초안. M1 Data → M2 VM → M3 UI → M4 통합 → M5 마이그레이션 → M6 PlayMode 의 6 마일스톤 18 Task. ICardEffect 시그니처 보존 (BLOCKER 4 권장안 — `ApplyCardEffect` 진입점 + `_currentCardScope`).
