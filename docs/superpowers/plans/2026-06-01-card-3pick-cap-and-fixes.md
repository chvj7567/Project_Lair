# 카드 3픽 캡(전역) + GuardianRage 정합 + 정리 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Rule 01 준수**: 각 Task 의 마지막 "스테이징" 스텝은 `git add` 까지만 한다. **`git commit` 금지** — 커밋은 파이프라인 마무리에서 메인이 한글 메시지(안)과 함께 처리한다.

**Goal:** 같은 카드를 3번 픽하면 이후 3택 후보에서 전역 제외하는 메커니즘을 추가하고, GuardianRage 효과를 SO 설명과 정합시키며, 잔여 문서·주석 stale 2건을 정리한다.

**Architecture:** 신규 POCO `CardPickCounter`(ECardId→픽수)를 `BattleController`가 보유. 픽 기록 지점에서 +1, `CardDeck.Draw`에 제외 predicate 를 주입해 캡(3) 도달 카드를 후보에서 뺀다. UI 는 `CardView` 우상단 `CHText` 배지로 `N/3` 표시. GuardianRage 는 `MonsterBuffService` 한 case 수정.

**Tech Stack:** Unity 6 (6000.0.68f1), C#, namespace `Lair`, MVVM, ChvjPackage(`CHText`/`CHButton`), Unity Test Framework(NUnit) EditMode, 한글 테스트 메서드명. 코딩룰 Rule 00~04 (`//#` 주석 · `var` 금지 · `!` 금지 · 가드절 중괄호 없이 개행).

**참고 문서**: `docs/superpowers/specs/2026-06-01-card-3pick-cap-and-fixes-design.md`, `docs/design/card-renewal.md`.

---

## 파일 구조

| 파일 | 책임 | 작업 |
|---|---|---|
| `Assets/_Lair/Scripts/Card/CardPickCounter.cs` | 카드별 픽수 누적·캡 판정·리셋 | Create |
| `Assets/_Lair/Scripts/Card/CardDeck.cs` | Draw 에 제외 predicate 오버로드 | Modify |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | 카운터 보유·픽경로 +1·Draw 제외 주입·리셋·배지 카운트 공급 | Modify |
| `Assets/_Lair/Scripts/UI/CardView.cs` | `N/3` 배지 표시 | Modify |
| `Assets/_Lair/Scripts/UI/CardSelectionPopup.cs` | `CardSelectionArg.PickCountOf` 전달 | Modify |
| `Assets/_Lair/Scripts/Battle/MonsterBuffService.cs` | GuardianRage case `HpMaxScale*=2f` 제거 | Modify |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `Multiply`(82-83) stale 주석 정정 | Modify |
| `Assets/_Lair/Tests/EditMode/Card/CardPickCounterTests.cs` | 카운터 단위 테스트 | Create |
| `Assets/_Lair/Tests/EditMode/Card/CardDeckCapExclusionTests.cs` | Draw 제외 테스트 | Create |
| `Assets/_Lair/Tests/EditMode/Card/GuardianRageTargetingTests.cs` (기존) · `BerserkGuardianRageRegressionTests.cs` (기존) | GuardianRage HP 불변 회귀 갱신 | Modify |
| `docs/design/card-renewal.md` | §10 헤더 · §7 · §9.6 갱신 | Modify (game-designer) |

> **CardView 프리팹 주의 (Rule 04)**: `_countBadge` `CHText` 는 `CardSelectionPopup` 프리팹의 각 `CardView` 슬롯에 정적으로 박혀 있어야 한다. 코드 동적 생성 금지. 프리팹에 TMP_Text + CHText 컴포넌트를 추가하고 인스펙터로 `_countBadge` 연결 (gameplay-programmer 가 프리팹 작업 포함).

---

## Task 1: CardPickCounter POCO

**Files:**
- Create: `Assets/_Lair/Scripts/Card/CardPickCounter.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/CardPickCounterTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.Card
{
    //# CardPickCounter — 카드별 픽수 누적, 캡(3) 판정, 리셋.
    public class CardPickCounterTests
    {
        [Test]
        public void RecordPick_누적_GetCount_반영()
        {
            CardPickCounter c = new CardPickCounter();
            c.RecordPick(ECardId.WispHpBoost);
            c.RecordPick(ECardId.WispHpBoost);
            Assert.AreEqual(2, c.GetCount(ECardId.WispHpBoost));
            Assert.AreEqual(0, c.GetCount(ECardId.Frenzy));
        }

        [Test]
        public void IsCapped_3픽_도달시_true()
        {
            CardPickCounter c = new CardPickCounter();
            Assert.IsFalse(c.IsCapped(ECardId.Frenzy));
            c.RecordPick(ECardId.Frenzy);
            c.RecordPick(ECardId.Frenzy);
            Assert.IsFalse(c.IsCapped(ECardId.Frenzy), "2픽은 아직 미캡");
            c.RecordPick(ECardId.Frenzy);
            Assert.IsTrue(c.IsCapped(ECardId.Frenzy), "3픽 도달 시 캡");
        }

        [Test]
        public void Reset_모든_카운트_0()
        {
            CardPickCounter c = new CardPickCounter();
            c.RecordPick(ECardId.Slow);
            c.Reset();
            Assert.AreEqual(0, c.GetCount(ECardId.Slow));
            Assert.IsFalse(c.IsCapped(ECardId.Slow));
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — Unity Test Runner(EditMode) 또는 UnityMCP `editor_recompile` 후 실행. Expected: 컴파일 실패 `CardPickCounter 형식을 찾을 수 없음`.

- [ ] **Step 3: 구현**

```csharp
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 3픽 캡 (전역) — 카드별 픽수를 한 런 동안 누적. 캡 도달 카드는 CardDeck.Draw 에서 제외.
    //# BattleController 가 보유. BuildSynergyService.Reset 과 동일 시점에 Reset.
    public class CardPickCounter
    {
        //# 카드 1장당 실효 중첩 상한. 도달 시 이후 후보 풀에서 제외.
        public const int Cap = 3;

        private readonly Dictionary<ECardId, int> _counts = new Dictionary<ECardId, int>();

        public void RecordPick(ECardId id)
        {
            int prev;
            _counts.TryGetValue(id, out prev);
            _counts[id] = prev + 1;
        }

        public int GetCount(ECardId id)
        {
            int v;
            return _counts.TryGetValue(id, out v) ? v : 0;
        }

        public bool IsCapped(ECardId id) => GetCount(id) >= Cap;

        //# 라운드(=런) 시작 / Restart 시 호출.
        public void Reset()
        {
            _counts.Clear();
        }
    }
}
```

- [ ] **Step 4: 통과 확인** — EditMode 실행. Expected: `CardPickCounterTests` 3개 PASS.

- [ ] **Step 5: 스테이징** (Rule 01 — 커밋 금지)

```bash
git add Assets/_Lair/Scripts/Card/CardPickCounter.cs Assets/_Lair/Scripts/Card/CardPickCounter.cs.meta Assets/_Lair/Tests/EditMode/Card/CardPickCounterTests.cs Assets/_Lair/Tests/EditMode/Card/CardPickCounterTests.cs.meta
```

---

## Task 2: CardDeck.Draw 제외 predicate 오버로드

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/CardDeck.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/CardDeckCapExclusionTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    //# CardDeck.Draw(n, isExcluded) — 제외 predicate 가 true 인 카드는 후보에서 빠진다.
    public class CardDeckCapExclusionTests
    {
        private static List<CardData> NewPool(params ECardId[] ids)
        {
            List<CardData> list = new List<CardData>();
            foreach (ECardId id in ids)
                list.Add(FakeCardData.Create(id));
            return list;
        }

        [Test]
        public void Draw_제외카드는_후보에_안나온다()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow, ECardId.TimeStop);
            CardDeck deck = new CardDeck(pool, seed: 99);

            IReadOnlyList<CardData> drawn = deck.Draw(3, id => id == ECardId.Frenzy);

            foreach (CardData c in drawn)
                Assert.AreNotEqual(ECardId.Frenzy, c.Id, "제외 카드 Frenzy 가 후보에 있으면 안 됨");
        }

        [Test]
        public void Draw_제외후_적격_3장미만이면_가능한_만큼()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow);
            CardDeck deck = new CardDeck(pool, seed: 99);

            //# 3장 중 2장 제외 → 적격 1장만
            IReadOnlyList<CardData> drawn = deck.Draw(3, id => id == ECardId.Frenzy || id == ECardId.Slow);

            Assert.AreEqual(1, drawn.Count);
            Assert.AreEqual(ECardId.WispHpBoost, drawn[0].Id);
        }

        [Test]
        public void Draw_predicate_null이면_기존동작_전체후보()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow);
            CardDeck deck = new CardDeck(pool, seed: 99);

            IReadOnlyList<CardData> drawn = deck.Draw(3, null);

            Assert.AreEqual(3, drawn.Count);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — EditMode 실행. Expected: 컴파일 실패 `Draw(int, Func) 오버로드 없음`.

- [ ] **Step 3: 구현** — `CardDeck.cs` 의 기존 `Draw(int n)` 를 다음으로 교체/확장. 기존 `Draw(int n)` 시그니처는 보존(내부 위임).

```csharp
using System;
using System.Collections.Generic;

namespace Lair.Card
{
    //# 카드 풀에서 무작위 n장 드로우. POCO — 런타임에 BattleController 가 보유.
    public class CardDeck
    {
        private readonly List<CardData> _all;
        private readonly System.Random _rng;

        public CardDeck(IEnumerable<CardData> cards, int seed = 0)
        {
            _all = new List<CardData>(cards);
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        //# 무작위 n장 (중복 X). 풀 부족 시 가능한 만큼.
        public IReadOnlyList<CardData> Draw(int n) => Draw(n, null);

        //# isExcluded(id) == true 인 카드는 후보에서 제외 (3픽 캡). null 이면 전체 후보.
        //# 제외 후 적격 카드가 n 미만이면 가능한 만큼만 반환 (기존 graceful fallback).
        public IReadOnlyList<CardData> Draw(int n, Func<Lair.Data.ECardId, bool> isExcluded)
        {
            List<CardData> pool = new List<CardData>();
            for (int i = 0; i < _all.Count; ++i)
            {
                if (isExcluded != null && isExcluded(_all[i].Id))
                    continue;
                pool.Add(_all[i]);
            }

            int actual = System.Math.Min(n, pool.Count);
            List<CardData> result = new List<CardData>(actual);
            for (int i = 0; i < actual; ++i)
            {
                int idx = _rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
```

- [ ] **Step 4: 통과 확인** — EditMode 실행. Expected: `CardDeckCapExclusionTests` 3개 + 기존 `CardDeckTests` 3개 PASS.

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/Card/CardDeck.cs Assets/_Lair/Tests/EditMode/Card/CardDeckCapExclusionTests.cs Assets/_Lair/Tests/EditMode/Card/CardDeckCapExclusionTests.cs.meta
```

---

## Task 3: BattleController 와이어링 (카운터 보유·픽경로·Draw 제외·리셋)

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

> MonoBehaviour 통합 지점이라 EditMode 단위 테스트 비대상. 검증은 컴파일 + 기존 `BattleControllerEntryTests`/`BattleControllerCardScopeTests` 회귀 + PlayMode `CardFlowSmokeTest` 로 한다. Task 5 의 GuardianRage 와 함께 PlayMode 스모크에서 확인.

- [ ] **Step 1: 카운터 필드 추가** — `_synergy` 필드(74행) 근처에 추가.

```csharp
private BuildSynergyService _synergy;
private CardPickCounter _pickCounter;   //# 카드 3픽 캡 (전역)
```

- [ ] **Step 2: 초기화 + 리셋** — `_synergy = new BuildSynergyService(); _synergy.Reset();` (120-121행) 바로 아래에 추가.

```csharp
_synergy = new BuildSynergyService();
_synergy.Reset();
_pickCounter = new CardPickCounter();
_pickCounter.Reset();
```

- [ ] **Step 3: 픽 경로 +1 (sim 경로)** — 594행 `_recorder.RecordPick(picked.Id);` 아래에 추가.

```csharp
_recorder.RecordPick(picked.Id);
_pickCounter.RecordPick(picked.Id);   //# 3픽 캡 누적
```

- [ ] **Step 4: 픽 경로 +1 (실제 경로)** — 615행 `_recorder.RecordPick(card.Id);` 아래에 추가.

```csharp
_recorder.RecordPick(card.Id);
_pickCounter.RecordPick(card.Id);   //# 3픽 캡 누적
```

- [ ] **Step 5: Draw 에 제외 주입 (sim + 실제)** — 590행과 604행의 `deck.Draw(3)` 를 각각 다음으로 교체.

```csharp
//# 590행 (sim)
IReadOnlyList<CardData> simChoices = deck.Draw(3, id => _pickCounter.IsCapped(id));
//# 604행 (실제)
IReadOnlyList<CardData> choices = deck.Draw(3, id => _pickCounter.IsCapped(id));
```

- [ ] **Step 6: 배지 카운트 공급** — 607행 `CardSelectionArg` 생성에 `PickCountOf` 추가 (Task 4 에서 필드 정의).

```csharp
CardSelectionArg arg = new CardSelectionArg
{
    Choices = choices,
    PickCountOf = c => _pickCounter.GetCount(c.Id),   //# 배지 N/3 — 픽 전 누적값
    OnPicked = card =>
    {
        // ... 기존 OnPicked 본문 그대로 ...
    }
};
```

- [ ] **Step 7: 컴파일 + 회귀 확인** — UnityMCP `editor_recompile` → 에러 0. EditMode 전체 PASS (기존 BattleController 관련 테스트 깨지지 않음). Expected: 컴파일 성공, 회귀 PASS.

- [ ] **Step 8: 스테이징**

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs
```

---

## Task 4: CardView `N/3` 배지 + CardSelectionPopup 전달

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/CardView.cs`
- Modify: `Assets/_Lair/Scripts/UI/CardSelectionPopup.cs`
- Prefab: `CardSelectionPopup` 프리팹의 각 `CardView` 에 `_countBadge` CHText 추가 (Rule 03 §3 / Rule 04 — 정적 배치, 인스펙터 연결)

- [ ] **Step 1: CardView 에 배지 필드 + Bind 오버로드** — `card.Description` 표시 직후 배지 갱신.

```csharp
using System;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 단일 카드 표시 — 이름/설명/카테고리 색 테두리/픽 버튼/3픽 캡 배지.
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _descText;
        [SerializeField] private Image _border;
        [SerializeField] private CHButton _pickButton;
        //# 3픽 캡 — 이미 픽한 횟수 N (0 이면 숨김, 1~2 면 "N/3"). 3 도달 카드는 후보에 안 나옴.
        [SerializeField] private CHText _countBadge;

        public void Bind(CardData card, Action onClick) => Bind(card, onClick, 0);

        public void Bind(CardData card, Action onClick, int pickCount)
        {
            _nameText.SetText(card.DisplayName);
            _descText.SetText(card.Description);
            //# 테두리 색 — 카드 ID 기준 단일 출처 (CardBorderColors).
            _border.color = CardBorderColors.BorderColorOf(card.Id);
            _pickButton.OnClick(onClick);
            UpdateBadge(pickCount);
        }

        private void UpdateBadge(int pickCount)
        {
            if (_countBadge == null)
                return;

            if (pickCount <= 0)
            {
                _countBadge.gameObject.SetActive(false);
                return;
            }

            _countBadge.gameObject.SetActive(true);
            _countBadge.SetText(pickCount + "/" + Lair.Card.CardPickCounter.Cap);
        }
    }
}
```

- [ ] **Step 2: CardSelectionArg 에 PickCountOf + Popup 전달**

```csharp
public class CardSelectionArg : UIArg
{
    public IReadOnlyList<CardData> Choices;
    public Action<CardData> OnPicked;
    //# 3픽 캡 배지 — 카드별 현재 픽 누적수 공급 (null 이면 0 처리).
    public Func<CardData, int> PickCountOf;
}
```

`CardSelectionPopup.InitUI` 의 `_slots[i].Bind(card, ...)` 호출을 다음으로 교체:

```csharp
int pickCount = sa.PickCountOf != null ? sa.PickCountOf(card) : 0;
_slots[i].Bind(card, () =>
{
    sa.OnPicked?.Invoke(card);
    Close(reuse: false);
}, pickCount);
```

- [ ] **Step 3: 프리팹 작업** — `CardSelectionPopup` 프리팹 → 각 `CardView` 슬롯 자식에 `_countBadge` 용 GameObject(TextMeshProUGUI + CHText) 우상단 배치, `CardView._countBadge` 인스펙터 연결. (UnityMCP 또는 에디터 수동. Rule 04 — 동적 생성 금지.)

- [ ] **Step 4: 컴파일 확인** — `editor_recompile` 에러 0. (UI 바인딩은 PlayMode `CardFlowSmokeTest` 에서 NRE 없이 팝업 표시되는지 확인.)

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/UI/CardView.cs Assets/_Lair/Scripts/UI/CardSelectionPopup.cs
# 프리팹 변경분도 함께 (경로는 실제 prefab 위치)
git add Assets/_Lair/Art/UI/CardSelectionPopup.prefab
```

---

## Task 5: GuardianRage HP×2.0 제거 (SO 설명과 정합)

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/MonsterBuffService.cs:94-102`
- Modify: `Assets/_Lair/Tests/EditMode/Card/GuardianRageTargetingTests.cs` (기존) — HP 불변 단언으로 갱신
- Modify: `Assets/_Lair/Tests/EditMode/Card/BerserkGuardianRageRegressionTests.cs` (기존) — HP×2.0 기대 제거

- [ ] **Step 1: 기존 테스트 먼저 갱신(실패 유도)** — `GuardianRageTargetingTests` 에 HP 불변 단언 추가/수정. (정확한 기존 메서드명은 파일 확인 후 맞춤. 신규 케이스 예시:)

```csharp
[Test]
public void GuardianRage_HP는_변하지않고_받피만_절반()
{
    FakeHealth hp = new FakeHealth();   //# DamageTakenScale=1, HpMaxScale=1 초기값 가정
    //# MonsterBuffService 로 Wisp 에 GuardianRage 적용하는 기존 헬퍼 경로 사용
    //# (해당 테스트 파일의 기존 Apply 헬퍼 재사용)
    ApplyGuardianRageToWisp(hp);

    Assert.AreEqual(0.5f, hp.DamageTakenScale, 1e-4f, "받피 ×0.5");
    Assert.AreEqual(1f, hp.HpMaxScale, 1e-4f, "HP 배율 불변 (×2.0 제거됨)");
}
```

- [ ] **Step 2: 실패 확인** — EditMode 실행. Expected: `HpMaxScale` 단언 FAIL (현재 코드 ×2.0).

- [ ] **Step 3: 구현** — `MonsterBuffService.cs` GuardianRage case 수정.

```csharp
case EMonsterBuff.GuardianRage:
    //# 카드 리뉴얼 v0.6 → 2026-06-01 정합 — 적용 종 한정 {Wisp, Wraith}.
    //# 받는 데미지 ×0.5 만. (구 HP ×2.0 제거 — SO description "받는 데미지 -50%" 와 일치, spec 2026-06-01)
    if (hp != null)
        hp.DamageTakenScale *= 0.5f;
    break;
```

- [ ] **Step 4: 기존 회귀 테스트 갱신** — `BerserkGuardianRageRegressionTests.cs` 안에 HP×2.0(HpMaxScale==2) 를 기대하는 단언이 있으면 `==1f` 로 수정. 없으면 변경 불요.

- [ ] **Step 5: 통과 확인** — EditMode 실행. Expected: GuardianRage 관련 테스트 전부 PASS, 다른 테스트 회귀 없음.

- [ ] **Step 6: 스테이징**

```bash
git add Assets/_Lair/Scripts/Battle/MonsterBuffService.cs Assets/_Lair/Tests/EditMode/Card/GuardianRageTargetingTests.cs Assets/_Lair/Tests/EditMode/Card/BerserkGuardianRageRegressionTests.cs
```

---

## Task 6: 정리 (C) — CommonEnum 주석 + card-renewal 문서

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs:82-83`
- Modify: `docs/design/card-renewal.md` (§10 헤더 · §7 · §9.6)

- [ ] **Step 1: CommonEnum 주석 정정** — 82-83행 교체.

```csharp
//# 폐기 (카드 리뉴얼 v0.6 — SO/풀 ref 제거, enum 자리만 보존. 실제 효과는 FastBreedingEffect/"빠른 번식")
Multiply,                      //# (20) — Swarm A (실제 SO: Multiply.asset / FastBreedingEffect, 팬텀 스포너 주기 ×0.6)
```

또한 90-91행의 SwarmRush 관련 주석(`//# SwarmRush 는 별도 enum 값을 두지 않고...`)도 stale 이면 "SwarmRush 미구현 — Multiply 자리 그대로 사용" 으로 정정.

- [ ] **Step 2: card-renewal.md 문서 갱신 (game-designer 영역)**
  - §10 헤더에 한 줄: `> **구현 상태 (2026-06-01)**: 본 절의 표면·enum 은 이미 구현 완료 (CommonEnum.cs:92-94, BattleContext.cs:119-130). 이하는 명세 보존용.`
  - §7 중첩 정책: "전역 3픽 캡 도입 (2026-06-01) — 모든 카드는 같은 카드 3픽 시 후보에서 제외되어 4픽 이상 발생 불가. 곱연산/가산 누적표의 3픽이 실효 상한." 한 단락 추가.
  - §9.6 SpawnerHaste: "미구현" → "전역 3픽 캡(2026-06-01)으로 포섭 — SpawnerHaste 도 3픽 후 제외되어 ×0.512 가 상한. 단독 effect-cap 불요." 로 갱신.

- [ ] **Step 3: 컴파일 확인** — `editor_recompile` 에러 0.

- [ ] **Step 4: 스테이징**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs docs/design/card-renewal.md
```

---

## Task 7: 전체 검증 게이트

- [ ] **Step 1: EditMode 전체 실행** — UnityMCP 로 EditMode 스위트 실행. Expected: 신규(CardPickCounterTests, CardDeckCapExclusionTests) + 갱신(GuardianRage) + 기존 전부 PASS, 0 실패.
- [ ] **Step 2: PlayMode 스모크** — `CardFlowSmokeTest` 실행. Expected: 카드 팝업 표시·픽·적용 NRE 없이 통과 (배지·캡 경로 포함).
- [ ] **Step 3: 컴파일 경고/에러 0 확인** — `editor_read_log` 로 컴파일 에러·룰 위반(예: `var`/`!`) 없음 확인.
- [ ] **Step 4: 성공 기준(spec §4) 대조** — 1~7 각 항목을 통과 테스트와 매핑.

---

## Self-Review (작성자 체크)

- **Spec 커버리지**: (A) Task 1·2·3·4 / (B) Task 5 / (C) Task 6. spec §4 성공기준 1~7 모두 Task 매핑됨. ✓
- **Placeholder**: GuardianRage 기존 테스트 메서드명은 "파일 확인 후 맞춤"으로 표기 — gameplay-programmer/test-engineer 가 기존 파일의 실제 헬퍼명에 맞춰 단언만 갱신(신규 케이스 코드는 제공). 프리팹 경로 `Assets/_Lair/Art/UI/CardSelectionPopup.prefab` 는 실제 위치 확인 후 사용. ✓
- **타입 정합**: `CardPickCounter.Cap`/`RecordPick`/`GetCount`/`IsCapped`/`Reset` — Task 1 정의, Task 3·4 에서 동일 시그니처 사용. `CardDeck.Draw(int, Func<ECardId,bool>)` — Task 2 정의, Task 3 에서 동일 사용. `CardSelectionArg.PickCountOf (Func<CardData,int>)` — Task 4 정의, Task 3 에서 동일 사용. `CardView.Bind(card, onClick, int)` — Task 4 정의, Popup 에서 사용. ✓
