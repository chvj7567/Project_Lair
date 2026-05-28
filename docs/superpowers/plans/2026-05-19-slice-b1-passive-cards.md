# Slice B1 — HP% 패시브 카드 시스템 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** HP 10% 임계점 트리거 + 3택 1 카드 선택 + 7장 카드 효과 시스템을 Slice A 의 자동전투 위에 통합.

**Architecture:** Time.timeScale=0 일시정지 + TriggerQueue 큐 + CardData(SO) 와 ICardEffect(Strategy) + IBattleContext 인터페이스 (Rule 06 자연 도입) + CHMUI 의 CardSelectionPopup.

**Tech Stack:** Unity 6 (6000.0.68f1), URP 17.0.4, C# 9, NUnit, Addressables 2.8.1, ScriptableObject + SerializeReference, com.chvj.unityinfra.

**참고 설계서:** `docs/superpowers/specs/2026-05-19-slice-b1-passive-cards-design.md`

---

## 프로젝트 룰 적용 — 매 태스크

- **Rule 01**: 모든 "Commit" 스텝은 *스테이지 + 한글 커밋 메시지(안) 출력*. `git commit` 실행 금지.
- **Rule 02**: 모든 신규 주석 `//#` 접두어.
- **Rule 03/06**: 인터페이스/이벤트 우선, 종속성 최소화.
- **Rule 05**: UI 만 풀세트 MVVM.
- **Rule 07**: ChvjPackage 기존 API 만 사용.
- **Rule 08**: 에셋 파일명 = Enum 값명 정확 일치.
- **Rule 09**: 공용 Enum 은 `CommonEnum.cs` 에 추가 (`Scripts/Data/CommonEnum.cs`).
- **Rule 10**: 공용 Interface 는 도메인별 `CommonInterface.cs` 에 통합.
- **Rule 11**: 텍스트는 `CHText`+`TMP_Text`, 버튼은 `Button`+`CHButton`.

## 테스트 실행 방법

EditMode/PlayMode 자동 실행:
- `mcp__UnityMCP__editor_invoke_method` → `Lair.EditorTools.LairTestRunner.RunEditModeTests` / `RunPlayModeTests`
- 결과 파일: `Library/lair-test-result.json` (EditMode) / `Library/lair-test-result-playmode.json` (PlayMode)
- `done: true` 폴링 후 `pass`/`fail` 확인

## 작업 디렉토리 / 브랜치

- 작업 디렉토리: `D:\Project_Lair`
- 브랜치: `feature/slice-b` (현재 체크아웃됨)
- Unity 에디터 열린 상태 가정 — 파일 작성 후 자동 컴파일

---

# B1-M1 — POCO 로직 TDD (~1일)

## Task M1.1: CommonEnum.cs 확장

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`

- [ ] **Step 1: EUI 에 CardSelectionPopup, 신규 enum 4개 추가**

기존 파일 끝에 추가 (EUI 는 enum 값 추가, 그 외는 신규):

```csharp
namespace Lair.Data
{
    //# 기존 ...

    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# B1 신규
    }

    //# B1 신규 — 데이터 SO 로드 키 (예: CardPool)
    public enum EData
    {
        CardPool_Passive,
    }

    //# B1 신규 — 카드 카테고리
    public enum ECardCategory
    {
        Enhance,        //# 강화
        Spawn,          //# 추가 소환
        Replace,        //# 교체
        Environment,    //# 환경 (영웅 디버프)
    }

    //# B1 신규 — 7장 카드 식별자
    public enum ECardId
    {
        SlimeHpBoost,
        GolemDamageBoost,
        OrcAtkSpeed,
        SpawnSlimes,
        SpawnGolem,
        ReplaceSlimesToGolem,
        HeroPoisonAura,
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`mcp__UnityMCP__editor_refresh_assets` → `editor_wait_ready` → `editor_read_log` (Error level) → 에러 0.

---

## Task M1.2: Card/CommonInterface.cs 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Card/CommonInterface.cs`

- [ ] **Step 1: 파일 생성 + 3개 인터페이스 작성**

```csharp
using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 효과 Strategy. SerializeReference 로 CardData 에 직렬화.
    public interface ICardEffect
    {
        void Apply(IBattleContext ctx);
    }

    //# 카드 효과 ↔ BattleController 표면 (Rule 06).
    public interface IBattleContext
    {
        IEnumerable<IHealth> GetMonsters(EMonster? filter = null);
        IHealth GetHero();
        Transform GetHeroTransform();

        //# 동적 스폰 — "슬라임 3마리 소환" 같은 카드용
        void SpawnMonster(EMonster key, Vector3 nearHero);

        //# 환경 카드 (예: 독 장판) — duration < 0 이면 무제한
        void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f);

        float DeltaTime { get; }
    }

    //# 영웅에 붙는 일시/영구 효과
    public interface IHeroAura
    {
        void OnAttached(IHealth hero);
        void Tick(IHealth hero, float dt);
        void OnDetached(IHealth hero);
    }
}
```

- [ ] **Step 2: 컴파일 확인**

콘솔 에러 0.

---

## Task M1.3: PauseService TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/PauseService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/PauseServiceTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Battle/PauseServiceTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    public class PauseServiceTests
    {
        [SetUp]
        public void Setup() { Time.timeScale = 1f; }

        [TearDown]
        public void TearDown() { Time.timeScale = 1f; }

        [Test]
        public void Pause_시_timeScale_0_Resume_시_1()
        {
            var ps = new PauseService();
            ps.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.IsTrue(ps.IsPaused);

            ps.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void 중첩_Pause_Resume_시_depth_관리()
        {
            var ps = new PauseService();
            ps.Pause();
            ps.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            ps.Resume();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f, "depth 2 → 1, 아직 pause 상태");
            Assert.IsTrue(ps.IsPaused);

            ps.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void ForcePause_즉시_정지_depth_무시()
        {
            var ps = new PauseService();
            ps.ForcePause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.IsTrue(ps.IsPaused);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Test Runner → EditMode → Run All → `PauseServiceTests` 3개 컴파일 실패.

- [ ] **Step 3: PauseService 구현**

Create `Assets/_Lair/Scripts/Battle/PauseService.cs`:
```csharp
using UnityEngine;

namespace Lair.Battle
{
    //# Time.timeScale 으로 게임 정지. 중첩 호출 안전 (depth 카운터).
    //# UI 입력/애니메이션은 unscaledDeltaTime 사용 시 계속 작동.
    public class PauseService
    {
        private int _depth;
        public bool IsPaused => _depth > 0;

        public void Pause()
        {
            _depth++;
            if (_depth == 1) Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (_depth == 0) return;
            _depth--;
            if (_depth == 0) Time.timeScale = 1f;
        }

        //# Slice A 의 EndBattle 같은 강제 정지 — depth 무시
        public void ForcePause() { _depth = int.MaxValue / 2; Time.timeScale = 0f; }
    }
}
```

- [ ] **Step 4: 통과 확인**

`mcp__UnityMCP__editor_invoke_method` `Lair.EditorTools.LairTestRunner.RunEditModeTests` → 3개 PASS.

---

## Task M1.4: PassiveTriggerService TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/PassiveTriggerService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/PassiveTriggerServiceTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Battle/PassiveTriggerServiceTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Battle;
using Lair.Tests.Helpers;

namespace Lair.Tests.Battle
{
    public class PassiveTriggerServiceTests
    {
        [Test]
        public void HP_90퍼_통과_시_OnTriggered_0_발행()
        {
            var hp = new FakeHealth();
            hp.SetMax(1000);
            var svc = new PassiveTriggerService(hp);
            var fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(100);   //# 1000 → 900 (90%)

            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }

        [Test]
        public void 여러_임계점_동시_통과_시_순차_발동()
        {
            var hp = new FakeHealth();
            hp.SetMax(1000);
            var svc = new PassiveTriggerService(hp);
            var fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(500);   //# 1000 → 500 (50%) — 90/80/70/60/50% 통과

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, fired);
        }

        [Test]
        public void 한_임계점_1회만_발동()
        {
            var hp = new FakeHealth();
            hp.SetMax(1000);
            var svc = new PassiveTriggerService(hp);
            var fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(50);    //# 950
            hp.TakeDamage(50);    //# 900 — 90% 도달
            hp.TakeDamage(50);    //# 850 — 여전히 90% 이하지만 재발동 X

            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Test Runner Run All → 3개 실패.

- [ ] **Step 3: PassiveTriggerService 구현**

Create `Assets/_Lair/Scripts/Battle/PassiveTriggerService.cs`:
```csharp
using System;
using Lair.Character;

namespace Lair.Battle
{
    //# Hero IHealth 의 OnChanged 를 구독해 HP 10% 임계점 통과 1회 감지.
    //# 큰 데미지로 여러 임계점 한 번에 통과해도 각각 순차 발동.
    public class PassiveTriggerService : IDisposable
    {
        //# 90%, 80%, ..., 10% — 총 9개
        private static readonly float[] Thresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        private readonly bool[] _fired = new bool[Thresholds.Length];
        private readonly IHealth _hero;

        public event Action<int> OnTriggered;   //# 0=90%, 1=80%, ..., 8=10%

        public PassiveTriggerService(IHealth hero)
        {
            _hero = hero;
            _hero.OnChanged += HandleChanged;
        }

        public void Dispose()
        {
            if (_hero != null) _hero.OnChanged -= HandleChanged;
        }

        private void HandleChanged(int current, int max)
        {
            if (max <= 0) return;
            float ratio = (float)current / max;
            for (int i = 0; i < Thresholds.Length; ++i)
            {
                if (_fired[i]) continue;
                if (ratio <= Thresholds[i])
                {
                    _fired[i] = true;
                    OnTriggered?.Invoke(i);
                }
            }
        }
    }
}
```

- [ ] **Step 4: 통과 확인**

EditMode 자동 실행 → 6개 누적 PASS (PauseService 3 + Passive 3).

---

## Task M1.5: TriggerQueue TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/TriggerQueue.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/TriggerQueueTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Battle/TriggerQueueTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    public class TriggerQueueTests
    {
        [Test]
        public void Enqueue_후_Dequeue_순서_FIFO()
        {
            var q = new TriggerQueue();
            q.Enqueue(TriggerQueue.Source.Passive, 0);
            q.Enqueue(TriggerQueue.Source.Passive, 1);

            Assert.AreEqual(2, q.Count);

            Assert.IsTrue(q.TryDequeue(out var e1));
            Assert.AreEqual(TriggerQueue.Source.Passive, e1.SourceType);
            Assert.AreEqual(0, e1.Index);

            Assert.IsTrue(q.TryDequeue(out var e2));
            Assert.AreEqual(1, e2.Index);
        }

        [Test]
        public void 빈_큐_TryDequeue_false()
        {
            var q = new TriggerQueue();
            Assert.IsFalse(q.TryDequeue(out _));
            Assert.AreEqual(0, q.Count);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

- [ ] **Step 3: TriggerQueue 구현**

Create `Assets/_Lair/Scripts/Battle/TriggerQueue.cs`:
```csharp
using System.Collections.Generic;

namespace Lair.Battle
{
    //# 트리거된 카드 선택 이벤트 큐. BattleController 가 한 건씩 순차 처리.
    public class TriggerQueue
    {
        public enum Source { Passive, Active }

        public readonly struct Entry
        {
            public readonly Source SourceType;
            public readonly int Index;
            public Entry(Source s, int i) { SourceType = s; Index = i; }
        }

        private readonly Queue<Entry> _q = new();
        public int Count => _q.Count;

        public void Enqueue(Source s, int i) => _q.Enqueue(new Entry(s, i));
        public bool TryDequeue(out Entry e) => _q.TryDequeue(out e);
    }
}
```

- [ ] **Step 4: 통과 확인**

8개 누적 PASS.

---

## Task M1.6: CardData / CardPool ScriptableObject 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Card/CardData.cs`
- Create: `Assets/_Lair/Scripts/Card/CardPool.cs`

- [ ] **Step 1: CardData 작성**

Create `Assets/_Lair/Scripts/Card/CardData.cs`:
```csharp
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 데이터 정의 — Effect 는 SerializeReference 로 polymorphic 직렬화.
    [CreateAssetMenu(fileName = "Card_", menuName = "Lair/Card", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private ECardId _id;
        [SerializeField] private ECardCategory _category;
        [SerializeField] private string _displayName;
        [TextArea] [SerializeField] private string _description;
        [SerializeReference] private ICardEffect _effect;

        public ECardId Id => _id;
        public ECardCategory Category => _category;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ICardEffect Effect => _effect;
    }
}
```

- [ ] **Step 2: CardPool 작성**

Create `Assets/_Lair/Scripts/Card/CardPool.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 풀 — 한 슬라이스의 모든 카드 묶음. CHMResource 로 로드.
    [CreateAssetMenu(fileName = "CardPool_", menuName = "Lair/Card Pool")]
    public class CardPool : ScriptableObject
    {
        [SerializeField] private List<CardData> _cards = new();
        public IReadOnlyList<CardData> Cards => _cards;
    }
}
```

- [ ] **Step 3: 컴파일 확인**

---

## Task M1.7: CardDeck TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Card/CardDeck.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/CardDeckTests.cs`
- Test helper: `Assets/_Lair/Tests/EditMode/Helpers/FakeCardData.cs`

- [ ] **Step 1: 테스트 헬퍼 작성**

Create `Assets/_Lair/Tests/EditMode/Helpers/FakeCardData.cs`:
```csharp
using Lair.Card;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# 테스트용 CardData 생성기 — SerializedObject 우회.
    public static class FakeCardData
    {
        public static CardData Create(ECardId id, ECardCategory category = ECardCategory.Enhance)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            //# reflection 으로 private SerializeField 주입 (테스트 한정 허용)
            var t = typeof(CardData);
            t.GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, id);
            t.GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, category);
            t.GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, id.ToString());
            return card;
        }
    }
}
```

- [ ] **Step 2: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Card/CardDeckTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using UnityEngine;

namespace Lair.Tests.Card
{
    public class CardDeckTests
    {
        private static List<CardData> NewPool(int count)
        {
            var list = new List<CardData>();
            for (int i = 0; i < count; ++i)
            {
                //# ECardId 의 정의된 값을 순환
                list.Add(FakeCardData.Create((ECardId)(i % System.Enum.GetValues(typeof(ECardId)).Length)));
            }
            return list;
        }

        [TearDown]
        public void TearDown()
        {
            //# ScriptableObject 정리
        }

        [Test]
        public void Draw_3_카드_3장_중복_없음()
        {
            var pool = NewPool(7);
            var deck = new CardDeck(pool, seed: 1234);

            var drawn = deck.Draw(3);

            Assert.AreEqual(3, drawn.Count);
            var set = new HashSet<CardData>(drawn);
            Assert.AreEqual(3, set.Count, "중복 카드 없음");
        }

        [Test]
        public void Draw_풀_부족_시_가능한_만큼()
        {
            var pool = NewPool(2);
            var deck = new CardDeck(pool, seed: 1234);

            var drawn = deck.Draw(3);

            Assert.AreEqual(2, drawn.Count);
        }

        [Test]
        public void Seed_고정_시_Reproducibility()
        {
            var pool = NewPool(7);
            var d1 = new CardDeck(pool, seed: 42);
            var d2 = new CardDeck(pool, seed: 42);

            var a = d1.Draw(3);
            var b = d2.Draw(3);

            for (int i = 0; i < 3; ++i)
                Assert.AreSame(a[i], b[i]);
        }
    }
}
```

- [ ] **Step 3: 실패 확인**

- [ ] **Step 4: CardDeck 구현**

Create `Assets/_Lair/Scripts/Card/CardDeck.cs`:
```csharp
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
        public IReadOnlyList<CardData> Draw(int n)
        {
            var pool = new List<CardData>(_all);
            int actual = System.Math.Min(n, pool.Count);
            var result = new List<CardData>(actual);
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

- [ ] **Step 5: 통과 확인**

11개 누적 PASS.

---

## Task M1.8: PoisonAura TDD

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Auras/PoisonAura.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/PoisonAuraTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/Card/PoisonAuraTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Card;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    public class PoisonAuraTests
    {
        [Test]
        public void Tick_1초마다_데미지_1회()
        {
            var hp = new FakeHealth();
            hp.SetMax(100);
            var aura = new PoisonAura(dps: 5f);
            aura.OnAttached(hp);

            //# 0.5초 → 데미지 없음
            aura.Tick(hp, 0.5f);
            Assert.AreEqual(0, hp.DamageCallCount);

            //# +0.6초 → 누적 1.1초, 1회 데미지
            aura.Tick(hp, 0.6f);
            Assert.AreEqual(1, hp.DamageCallCount);
            Assert.AreEqual(5, hp.LastDamage);

            //# +2초 → 누적 0.1+2=2.1, 2회 추가 데미지 (총 3회)
            aura.Tick(hp, 2.0f);
            Assert.AreEqual(3, hp.DamageCallCount);
        }

        [Test]
        public void OnAttached_시_초기화()
        {
            var hp = new FakeHealth();
            hp.SetMax(100);
            var aura = new PoisonAura(dps: 5f);
            aura.OnAttached(hp);
            aura.OnDetached(hp);

            //# Detach 후 Tick 호출은 무관 (호출되지 않을 것이지만 안전성)
            Assert.AreEqual(0, hp.DamageCallCount);
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

- [ ] **Step 3: PoisonAura 구현**

Create `Assets/_Lair/Scripts/Card/Auras/PoisonAura.cs`:
```csharp
using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅에 부착되어 1초마다 _dps 데미지를 가하는 환경 효과.
    [Serializable]
    public class PoisonAura : IHeroAura
    {
        private readonly float _dps;
        private float _tickAccumulator;

        public PoisonAura(float dps) { _dps = dps; }

        public void OnAttached(IHealth hero) { _tickAccumulator = 0f; }

        public void Tick(IHealth hero, float dt)
        {
            if (hero == null) return;
            _tickAccumulator += dt;
            while (_tickAccumulator >= 1f)
            {
                _tickAccumulator -= 1f;
                hero.TakeDamage((int)_dps);
            }
        }

        public void OnDetached(IHealth hero) { }
    }
}
```

- [ ] **Step 4: 통과 확인**

13개 누적 PASS.

---

## Task M1.9: M1 검증 + 스테이지

- [ ] **Step 1: 전체 EditMode 실행**

`LairTestRunner.RunEditModeTests` → `Library/lair-test-result.json` 에서:
- 기존 Slice A: 24 PASS
- B1-M1 신규: 13 PASS (PauseService 3 + PassiveTrigger 3 + TriggerQueue 2 + CardDeck 3 + PoisonAura 2)
- **합계 ≥ 37 PASS, FAIL 0**

(LairTestRunner 의 leaf 카운트가 추가 노드 포함할 수 있음 — fail=0 이 중요)

- [ ] **Step 2: 스테이지**

```powershell
git add Assets/_Lair/Scripts/Data/CommonEnum.cs `
        Assets/_Lair/Scripts/Card `
        Assets/_Lair/Scripts/Battle/PauseService.cs `
        Assets/_Lair/Scripts/Battle/PassiveTriggerService.cs `
        Assets/_Lair/Scripts/Battle/TriggerQueue.cs `
        Assets/_Lair/Tests/EditMode
```

- [ ] **Step 3: 커밋 메시지(안)**

```
# [feat] - B1-M1 POCO 로직 TDD (Card 데이터/PauseService/PassiveTriggerService/TriggerQueue/PoisonAura)
```

**M1 검증 게이트**: EditMode 누적 ~37 PASS, FAIL 0.

---

# B1-M2 — 효과 클래스 + Aura 인프라 + BattleContext (~1일)

## Task M2.1: MonsterTag 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Character/MonsterTag.cs`

- [ ] **Step 1: MonsterTag 구현**

Create `Assets/_Lair/Scripts/Character/MonsterTag.cs`:
```csharp
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 프리팹에 부착되어 EMonster 값을 직렬화.
    //# BattleContext.GetMonsters(filter) 가 이를 통해 슬라임/골렘/오크 구분.
    public class MonsterTag : MonoBehaviour
    {
        [SerializeField] private EMonster _key;
        public EMonster Key => _key;

        //# 빌더 또는 런타임 동적 설정
        public void Configure(EMonster k) => _key = k;
    }
}
```

- [ ] **Step 2: 컴파일 확인**

---

## Task M2.2: HeroAuraRunner 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs`

- [ ] **Step 1: HeroAuraRunner 구현**

Create `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs`:
```csharp
using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 영웅 GameObject 에 부착되어 여러 IHeroAura 를 매 frame Tick.
    //# Attach(aura, duration) — duration < 0 이면 무제한.
    [RequireComponent(typeof(Health))]
    public class HeroAuraRunner : MonoBehaviour
    {
        private class Slot
        {
            public IHeroAura Aura;
            public float Remain;
            public bool Indefinite;
        }

        private readonly List<Slot> _slots = new();
        private IHealth _hero;

        private void Awake() => _hero = GetComponent<IHealth>();

        public void Attach(IHeroAura aura, float duration)
        {
            if (aura == null) return;
            aura.OnAttached(_hero);
            _slots.Add(new Slot { Aura = aura, Remain = duration, Indefinite = duration < 0f });
        }

        private void Update()
        {
            if (_hero == null) return;
            for (int i = _slots.Count - 1; i >= 0; --i)
            {
                var s = _slots[i];
                s.Aura.Tick(_hero, Time.deltaTime);
                if (!s.Indefinite)
                {
                    s.Remain -= Time.deltaTime;
                    if (s.Remain <= 0f)
                    {
                        s.Aura.OnDetached(_hero);
                        _slots.RemoveAt(i);
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

---

## Task M2.3: BattleContext 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/BattleContext.cs`

- [ ] **Step 1: BattleContext 구현**

Create `Assets/_Lair/Scripts/Battle/BattleContext.cs`:
```csharp
using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# IBattleContext 의 구체 구현. BattleController 가 보유.
    //# 카드 효과 클래스가 이 인터페이스만 통해 부모 기능 사용 (Rule 06).
    public class BattleContext : IBattleContext
    {
        private readonly BattleController _owner;
        public float DeltaTime => Time.deltaTime;

        public BattleContext(BattleController owner) => _owner = owner;

        public IEnumerable<IHealth> GetMonsters(EMonster? filter = null)
        {
            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Health == null || !e.Health.IsAlive) continue;
                if (filter.HasValue)
                {
                    var tag = e.Transform != null ? e.Transform.GetComponent<MonsterTag>() : null;
                    if (tag == null || tag.Key != filter.Value) continue;
                }
                yield return e.Health;
            }
        }

        public IHealth GetHero()
        {
            foreach (var e in CharacterRegistry.Heroes)
                if (e?.Health != null && e.Health.IsAlive) return e.Health;
            return null;
        }

        public Transform GetHeroTransform()
        {
            foreach (var e in CharacterRegistry.Heroes)
                if (e?.Transform != null) return e.Transform;
            return null;
        }

        public void SpawnMonster(EMonster key, Vector3 nearHero)
        {
            _owner.SpawnMonsterRuntime(key, nearHero);
        }

        public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f)
        {
            var heroT = GetHeroTransform();
            if (heroT == null) return;
            var runner = heroT.GetComponent<HeroAuraRunner>()
                      ?? heroT.gameObject.AddComponent<HeroAuraRunner>();
            runner.Attach(aura, durationSeconds);
        }
    }
}
```

- [ ] **Step 2: 컴파일 — BattleController 의 `SpawnMonsterRuntime` 메서드가 아직 없어 에러 예상**

확인 후 다음 Task 에서 BattleController 에 추가 (M4 에서 통합).

임시 우회: BattleContext.SpawnMonster 의 본문을 `Debug.LogWarning("[BattleContext] SpawnMonster not implemented yet");` 로. M4 에서 BattleController.SpawnMonsterRuntime 추가 후 본문 복원.

Modified `BattleContext.cs` (임시):
```csharp
        public void SpawnMonster(EMonster key, Vector3 nearHero)
        {
            //# M4 BattleController.SpawnMonsterRuntime 추가 후 호출. 그 전엔 경고만.
            Debug.LogWarning($"[BattleContext] SpawnMonster({key}) — not implemented until M4");
        }
```

- [ ] **Step 3: 컴파일 확인**

---

## Task M2.4: 효과 7개 작성

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/SlimeHpBoostEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/GolemDamageBoostEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/OrcAtkSpeedEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/SpawnSlimesEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/SpawnGolemEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/ReplaceSlimesToGolemEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/HeroPoisonAuraEffect.cs`

각 효과는 `[Serializable]` + `ICardEffect`. 코드 형식은 동일 — 매개변수만 다름.

- [ ] **Step 1: SlimeHpBoostEffect**

Create `Assets/_Lair/Scripts/Card/Effects/SlimeHpBoostEffect.cs`:
```csharp
using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 슬라임 Max HP × _hpMul (Current 비율 유지).
    [Serializable]
    public class SlimeHpBoostEffect : ICardEffect
    {
        [SerializeField] private float _hpMul = 1.5f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Slime))
            {
                int newMax = Mathf.Max(1, (int)(hp.Max * _hpMul));
                hp.SetMax(newMax, resetCurrent: false);
            }
        }
    }
}
```

- [ ] **Step 2: GolemDamageBoostEffect**

Create `Assets/_Lair/Scripts/Card/Effects/GolemDamageBoostEffect.cs`:
```csharp
using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 골렘의 MeleeAttacker.Power × _mul.
    [Serializable]
    public class GolemDamageBoostEffect : ICardEffect
    {
        [SerializeField] private float _mul = 1.5f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Golem))
            {
                //# IHealth 에서 MonoBehaviour cast → MeleeAttacker 조회
                if (hp is MonoBehaviour mb)
                {
                    var attacker = mb.GetComponent<MeleeAttacker>();
                    if (attacker != null)
                    {
                        attacker.Configure(attacker.Range, attacker.Cooldown,
                                           Mathf.Max(1, (int)(attacker.Power * _mul)));
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 3: OrcAtkSpeedEffect**

Create `Assets/_Lair/Scripts/Card/Effects/OrcAtkSpeedEffect.cs`:
```csharp
using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 오크의 MeleeAttacker.Cooldown × _cdMul (낮을수록 빠른 공격).
    [Serializable]
    public class OrcAtkSpeedEffect : ICardEffect
    {
        [SerializeField] private float _cdMul = 0.7f;

        public void Apply(IBattleContext ctx)
        {
            foreach (var hp in ctx.GetMonsters(EMonster.Orc))
            {
                if (hp is MonoBehaviour mb)
                {
                    var attacker = mb.GetComponent<MeleeAttacker>();
                    if (attacker != null)
                    {
                        attacker.Configure(attacker.Range,
                                           Mathf.Max(0.05f, attacker.Cooldown * _cdMul),
                                           attacker.Power);
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 4: SpawnSlimesEffect**

Create `Assets/_Lair/Scripts/Card/Effects/SpawnSlimesEffect.cs`:
```csharp
using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 근처에 슬라임 _count 마리 스폰.
    [Serializable]
    public class SpawnSlimesEffect : ICardEffect
    {
        [SerializeField] private int _count = 3;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            for (int i = 0; i < _count; ++i)
                ctx.SpawnMonster(EMonster.Slime, heroT.position);
        }
    }
}
```

- [ ] **Step 5: SpawnGolemEffect**

Create `Assets/_Lair/Scripts/Card/Effects/SpawnGolemEffect.cs`:
```csharp
using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 근처에 골렘 _count 마리 스폰.
    [Serializable]
    public class SpawnGolemEffect : ICardEffect
    {
        [SerializeField] private int _count = 1;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            for (int i = 0; i < _count; ++i)
                ctx.SpawnMonster(EMonster.Golem, heroT.position);
        }
    }
}
```

- [ ] **Step 6: ReplaceSlimesToGolemEffect**

Create `Assets/_Lair/Scripts/Card/Effects/ReplaceSlimesToGolemEffect.cs`:
```csharp
using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 슬라임 전부 제거 → 위치 평균에 골렘 1마리.
    [Serializable]
    public class ReplaceSlimesToGolemEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            var positions = new List<Vector3>();
            foreach (var hp in ctx.GetMonsters(EMonster.Slime))
            {
                if (hp is MonoBehaviour mb)
                {
                    positions.Add(mb.transform.position);
                    //# 슬라임에 즉시 큰 데미지 → DespawnOnDeath 가 Destroy
                    hp.TakeDamage(int.MaxValue / 2);
                }
            }

            if (positions.Count == 0) return;

            Vector3 avg = Vector3.zero;
            foreach (var p in positions) avg += p;
            avg /= positions.Count;

            ctx.SpawnMonster(EMonster.Golem, avg);
        }
    }
}
```

- [ ] **Step 7: HeroPoisonAuraEffect**

Create `Assets/_Lair/Scripts/Card/Effects/HeroPoisonAuraEffect.cs`:
```csharp
using System;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅에 PoisonAura(DPS=_dps) 무제한 부착.
    [Serializable]
    public class HeroPoisonAuraEffect : ICardEffect
    {
        [SerializeField] private float _dps = 5f;

        public void Apply(IBattleContext ctx)
        {
            ctx.ApplyHeroAura(new PoisonAura(_dps), durationSeconds: -1f);
        }
    }
}
```

- [ ] **Step 8: 컴파일 확인 + 회귀 검증**

`mcp__UnityMCP__editor_refresh_assets` → 컴파일 에러 0 확인.
`LairTestRunner.RunEditModeTests` → 누적 PASS 유지 (≥37), FAIL 0.

---

## Task M2.5: M2 검증 + 스테이지

- [ ] **Step 1: 회귀 테스트**

EditMode 자동 실행 — 누적 PASS 유지, FAIL 0.

- [ ] **Step 2: 스테이지**

```powershell
git add Assets/_Lair/Scripts/Character/MonsterTag.cs `
        Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs `
        Assets/_Lair/Scripts/Battle/BattleContext.cs `
        Assets/_Lair/Scripts/Card/Effects
```

- [ ] **Step 3: 커밋 메시지(안)**

```
# [feat] - B1-M2 카드 효과 7개 + HeroAuraRunner + MonsterTag + BattleContext
```

**M2 검증 게이트**: 컴파일 에러 0, EditMode 회귀 FAIL 0.

---

# B1-M3 — 카드 SO 7장 + CardPool + CardSelectionPopup 프리팹 자동 생성 (~1일)

## Task M3.1: CardSelectionArg + CardSelectionPopup + CardView 작성

**Files:**
- Create: `Assets/_Lair/Scripts/UI/CardSelectionArg.cs`
- Create: `Assets/_Lair/Scripts/UI/CardSelectionPopup.cs`
- Create: `Assets/_Lair/Scripts/UI/CardView.cs`

- [ ] **Step 1: CardSelectionArg**

Create `Assets/_Lair/Scripts/UI/CardSelectionArg.cs`:
```csharp
using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;

namespace Lair.UI
{
    //# CardSelectionPopup 의 UIArg — 카드 3장 + 선택 콜백.
    public class CardSelectionArg : UIArg
    {
        public IReadOnlyList<CardData> Choices;
        public Action<CardData> OnPicked;
    }
}
```

- [ ] **Step 2: CardView**

Create `Assets/_Lair/Scripts/UI/CardView.cs`:
```csharp
using System;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 단일 카드 표시 — 이름/설명/카테고리 색 테두리/픽 버튼.
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _descText;
        [SerializeField] private Image _border;
        [SerializeField] private CHButton _pickButton;

        public void Bind(CardData card, Action onClick)
        {
            _nameText.SetText(card.DisplayName);
            _descText.SetText(card.Description);
            _border.color = CategoryColor(card.Category);
            _pickButton.OnClick(onClick);
        }

        private static Color CategoryColor(ECardCategory c) => c switch
        {
            ECardCategory.Enhance     => new Color(0.13f, 0.77f, 0.37f),
            ECardCategory.Spawn       => new Color(0.23f, 0.51f, 0.96f),
            ECardCategory.Replace     => new Color(0.96f, 0.62f, 0.23f),
            ECardCategory.Environment => new Color(0.66f, 0.33f, 0.96f),
            _                         => Color.gray,
        };
    }
}
```

- [ ] **Step 3: CardSelectionPopup**

Create `Assets/_Lair/Scripts/UI/CardSelectionPopup.cs`:
```csharp
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# CHMUI 로 띄워지는 카드 선택 팝업. 3장 표시 → 1장 선택 → OnPicked → Close.
    public class CardSelectionPopup : UIBase
    {
        [SerializeField] private CardView[] _slots = new CardView[3];

        public override void InitUI(UIArg arg)
        {
            if (arg is not CardSelectionArg sa) return;

            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i] == null) continue;

                if (i < sa.Choices.Count)
                {
                    var card = sa.Choices[i];
                    _slots[i].gameObject.SetActive(true);
                    _slots[i].Bind(card, () =>
                    {
                        sa.OnPicked?.Invoke(card);
                        Close(reuse: true);
                    });
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
```

- [ ] **Step 4: 컴파일 확인**

---

## Task M3.2: LairCardPrefabBuilder 작성 (Editor)

**Files:**
- Create: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

- [ ] **Step 1: 빌더 작성**

Create `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`:
```csharp
using System.Collections.Generic;
using System.IO;
using Lair.Card;
using Lair.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# B1 — 카드 SO 7장 + CardPool_Passive SO 자동 생성 + Addressables 등록.
    //# SerializeReference 의 ICardEffect 슬롯 주입은 managedReferenceValue 사용.
    public static class LairCardPrefabBuilder
    {
        public const string CardDir = "Assets/_Lair/Data/Cards/Cards";
        public const string PoolDir = "Assets/_Lair/Data/Cards";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";

        public class Spec
        {
            public ECardId Id;
            public ECardCategory Category;
            public string DisplayName;
            public string Description;
            public System.Func<ICardEffect> EffectFactory;
        }

        public static readonly Spec[] AllSpecs = new[]
        {
            new Spec { Id = ECardId.SlimeHpBoost, Category = ECardCategory.Enhance,
                       DisplayName = "끈질긴 슬라임", Description = "모든 슬라임 HP +50%",
                       EffectFactory = () => new SlimeHpBoostEffect() },
            new Spec { Id = ECardId.GolemDamageBoost, Category = ECardCategory.Enhance,
                       DisplayName = "강철 골렘", Description = "모든 골렘 데미지 +50%",
                       EffectFactory = () => new GolemDamageBoostEffect() },
            new Spec { Id = ECardId.OrcAtkSpeed, Category = ECardCategory.Enhance,
                       DisplayName = "광폭 오크", Description = "모든 오크 공격속도 +30%",
                       EffectFactory = () => new OrcAtkSpeedEffect() },
            new Spec { Id = ECardId.SpawnSlimes, Category = ECardCategory.Spawn,
                       DisplayName = "슬라임 소환", Description = "영웅 근처에 슬라임 3마리",
                       EffectFactory = () => new SpawnSlimesEffect() },
            new Spec { Id = ECardId.SpawnGolem, Category = ECardCategory.Spawn,
                       DisplayName = "골렘 소환", Description = "영웅 근처에 골렘 1마리",
                       EffectFactory = () => new SpawnGolemEffect() },
            new Spec { Id = ECardId.ReplaceSlimesToGolem, Category = ECardCategory.Replace,
                       DisplayName = "융합", Description = "모든 슬라임 → 골렘 1마리",
                       EffectFactory = () => new ReplaceSlimesToGolemEffect() },
            new Spec { Id = ECardId.HeroPoisonAura, Category = ECardCategory.Environment,
                       DisplayName = "독 안개", Description = "영웅 발 밑에 독 장판 (DPS 5)",
                       EffectFactory = () => new HeroPoisonAuraEffect() },
        };

        [MenuItem("Lair/Setup/B1 - Build Card Assets")]
        public static void BuildAllCards()
        {
            EnsureDir(CardDir);
            EnsureDir(PoolDir);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                LairSetup.EnsureAddressablesSetup();
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }
            var group = settings.FindGroup(ResourceGroup);

            var createdCards = new List<CardData>();

            //# 1) 카드 7장
            foreach (var spec in AllSpecs)
            {
                var card = ScriptableObject.CreateInstance<CardData>();
                var so = new SerializedObject(card);
                so.FindProperty("_id").enumValueIndex       = (int)spec.Id;
                so.FindProperty("_category").enumValueIndex = (int)spec.Category;
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description;
                so.FindProperty("_effect").managedReferenceValue = spec.EffectFactory();
                so.ApplyModifiedPropertiesWithoutUndo();

                string path = $"{CardDir}/{spec.Id}.asset";
                AssetDatabase.CreateAsset(card, path);
                RegisterAddressable(settings, group, path, spec.Id.ToString());

                createdCards.Add(card);
                Debug.Log($"[LairCardPrefabBuilder] CardData 생성: {spec.Id}");
            }

            //# 2) CardPool_Passive 생성 + 7장 List 채우기
            var pool = ScriptableObject.CreateInstance<CardPool>();
            var poolSo = new SerializedObject(pool);
            var listProp = poolSo.FindProperty("_cards");
            listProp.arraySize = createdCards.Count;
            for (int i = 0; i < createdCards.Count; ++i)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = createdCards[i];
            }
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            string poolPath = $"{PoolDir}/{EData.CardPool_Passive}.asset";
            AssetDatabase.CreateAsset(pool, poolPath);
            RegisterAddressable(settings, group, poolPath, EData.CardPool_Passive.ToString());
            Debug.Log("[LairCardPrefabBuilder] CardPool_Passive 생성 + 7장 등록");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RegisterAddressable(AddressableAssetSettings settings,
            AddressableAssetGroup group, string assetPath, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 + 실행**

`mcp__UnityMCP__editor_refresh_assets` → `editor_wait_ready` → 에러 0.
`mcp__UnityMCP__editor_invoke_method` `Lair.EditorTools.LairCardPrefabBuilder.BuildAllCards`.
로그 확인 — 7개 CardData + 1개 CardPool 생성 메시지.

- [ ] **Step 3: 파일 확인**

Bash `ls Assets/_Lair/Data/Cards/Cards` → 7개 `.asset` 파일.
Bash `ls Assets/_Lair/Data/Cards` → `CardPool_Passive.asset`.

---

## Task M3.3: LairUIPrefabBuilder 에 CardSelectionPopup 추가

**Files:**
- Modify: `Assets/_Lair/Editor/LairUIPrefabBuilder.cs`

- [ ] **Step 1: BuildCardSelectionPopup 메서드 추가**

`LairUIPrefabBuilder.cs` 의 `BuildAllUIPrefabs` 안에 호출 추가:
```csharp
BuildBattleHud(settings, group);
BuildResultPopup(settings, group);
BuildCardSelectionPopup(settings, group);   //# B1 추가
```

같은 파일에 새 메서드 추가 (`BuildResultPopup` 뒤):
```csharp
//# ---------- CardSelectionPopup 프리팹 ----------
private static void BuildCardSelectionPopup(AddressableAssetSettings settings, AddressableAssetGroup group)
{
    const string PrefabName = "CardSelectionPopup";

    //# 루트
    var root = new GameObject(PrefabName, typeof(RectTransform));
    SetFullStretch((RectTransform)root.transform);
    var popup = root.AddComponent<Lair.UI.CardSelectionPopup>();

    //# Dim
    var dimGo = new GameObject("Dim", typeof(RectTransform));
    dimGo.transform.SetParent(root.transform, false);
    SetFullStretch((RectTransform)dimGo.transform);
    var dimImg = dimGo.AddComponent<Image>();
    dimImg.sprite = GetUISprite();
    dimImg.type = Image.Type.Sliced;
    dimImg.color = new Color(0f, 0f, 0f, 0.65f);

    //# Title
    var titleGo = new GameObject("Title", typeof(RectTransform));
    titleGo.transform.SetParent(root.transform, false);
    var titleRt = (RectTransform)titleGo.transform;
    titleRt.anchorMin = new Vector2(0.5f, 1f);
    titleRt.anchorMax = new Vector2(0.5f, 1f);
    titleRt.pivot     = new Vector2(0.5f, 1f);
    titleRt.anchoredPosition = new Vector2(0f, -60f);
    titleRt.sizeDelta = new Vector2(600f, 80f);
    var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
    titleTmp.text = "카드 선택";
    titleTmp.font = TMP_Settings.defaultFontAsset;
    titleTmp.fontSize = 48f;
    titleTmp.alignment = TextAlignmentOptions.Center;
    titleTmp.color = Color.white;
    titleGo.AddComponent<CHText>();

    //# CardsLayout — 3 slot 가로 배치
    var layoutGo = new GameObject("CardsLayout", typeof(RectTransform));
    layoutGo.transform.SetParent(root.transform, false);
    var layoutRt = (RectTransform)layoutGo.transform;
    layoutRt.anchorMin = new Vector2(0.5f, 0.5f);
    layoutRt.anchorMax = new Vector2(0.5f, 0.5f);
    layoutRt.pivot     = new Vector2(0.5f, 0.5f);
    layoutRt.anchoredPosition = Vector2.zero;
    layoutRt.sizeDelta = new Vector2(1080f, 460f);
    var hlg = layoutGo.AddComponent<HorizontalLayoutGroup>();
    hlg.spacing = 40;
    hlg.childAlignment = TextAnchor.MiddleCenter;
    hlg.childForceExpandWidth = false;
    hlg.childForceExpandHeight = false;
    hlg.childControlWidth = false;
    hlg.childControlHeight = false;

    //# 3개 CardView slot
    var slots = new Lair.UI.CardView[3];
    for (int i = 0; i < 3; ++i)
    {
        slots[i] = BuildCardViewSlot(layoutGo.transform, i);
    }

    //# SerializedObject 주입 — _slots 배열
    var so = new SerializedObject(popup);
    var slotsProp = so.FindProperty("_slots");
    slotsProp.arraySize = 3;
    for (int i = 0; i < 3; ++i)
        slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
    so.ApplyModifiedPropertiesWithoutUndo();

    SaveAndRegisterPrefab(root, PrefabName, settings, group);
}

private static Lair.UI.CardView BuildCardViewSlot(Transform parent, int index)
{
    var slot = new GameObject($"CardView_{index}", typeof(RectTransform));
    slot.transform.SetParent(parent, false);
    var slotRt = (RectTransform)slot.transform;
    slotRt.sizeDelta = new Vector2(320f, 420f);

    //# Border (full stretch, 카테고리 색 — 런타임에 CardView 가 변경)
    var borderGo = new GameObject("Border", typeof(RectTransform));
    borderGo.transform.SetParent(slot.transform, false);
    SetFullStretch((RectTransform)borderGo.transform);
    var borderImg = borderGo.AddComponent<Image>();
    borderImg.sprite = GetUISprite();
    borderImg.type = Image.Type.Sliced;
    borderImg.color = Color.gray;

    //# 카드 내부 흰 배경 (border 안쪽 약간 작게)
    var bgGo = new GameObject("Bg", typeof(RectTransform));
    bgGo.transform.SetParent(slot.transform, false);
    var bgRt = (RectTransform)bgGo.transform;
    bgRt.anchorMin = Vector2.zero;
    bgRt.anchorMax = Vector2.one;
    bgRt.offsetMin = new Vector2(8f, 8f);
    bgRt.offsetMax = new Vector2(-8f, -8f);
    var bgImg = bgGo.AddComponent<Image>();
    bgImg.sprite = GetUISprite();
    bgImg.type = Image.Type.Sliced;
    bgImg.color = Color.white;

    //# NameText
    var nameGo = new GameObject("NameText", typeof(RectTransform));
    nameGo.transform.SetParent(slot.transform, false);
    var nameRt = (RectTransform)nameGo.transform;
    nameRt.anchorMin = new Vector2(0f, 1f);
    nameRt.anchorMax = new Vector2(1f, 1f);
    nameRt.pivot     = new Vector2(0.5f, 1f);
    nameRt.anchoredPosition = new Vector2(0f, -30f);
    nameRt.sizeDelta = new Vector2(0f, 60f);
    var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
    nameTmp.text = "Name";
    nameTmp.font = TMP_Settings.defaultFontAsset;
    nameTmp.fontSize = 32f;
    nameTmp.alignment = TextAlignmentOptions.Center;
    nameTmp.color = Color.black;
    var nameText = nameGo.AddComponent<CHText>();

    //# DescText
    var descGo = new GameObject("DescText", typeof(RectTransform));
    descGo.transform.SetParent(slot.transform, false);
    var descRt = (RectTransform)descGo.transform;
    descRt.anchorMin = new Vector2(0f, 0f);
    descRt.anchorMax = new Vector2(1f, 0.7f);
    descRt.offsetMin = new Vector2(20f, 20f);
    descRt.offsetMax = new Vector2(-20f, -20f);
    var descTmp = descGo.AddComponent<TextMeshProUGUI>();
    descTmp.text = "Description";
    descTmp.font = TMP_Settings.defaultFontAsset;
    descTmp.fontSize = 22f;
    descTmp.alignment = TextAlignmentOptions.TopLeft;
    descTmp.color = Color.black;
    var descText = descGo.AddComponent<CHText>();

    //# PickButton (full stretch over the whole card)
    var btnGo = new GameObject("PickButton", typeof(RectTransform));
    btnGo.transform.SetParent(slot.transform, false);
    SetFullStretch((RectTransform)btnGo.transform);
    var btnImg = btnGo.AddComponent<Image>();
    btnImg.sprite = GetUISprite();
    btnImg.type = Image.Type.Sliced;
    btnImg.color = new Color(1, 1, 1, 0.001f);   //# 거의 투명, raycast 만 받음
    var btn = btnGo.AddComponent<Button>();
    btn.targetGraphic = btnImg;
    var chBtn = btnGo.AddComponent<CHButton>();

    //# CardView 컴포넌트
    var cv = slot.AddComponent<Lair.UI.CardView>();
    var cvSo = new SerializedObject(cv);
    SetObjectField(cvSo, "_nameText", nameText);
    SetObjectField(cvSo, "_descText", descText);
    SetObjectField(cvSo, "_border", borderImg);
    SetObjectField(cvSo, "_pickButton", chBtn);
    cvSo.ApplyModifiedPropertiesWithoutUndo();

    return cv;
}
```

- [ ] **Step 2: 컴파일 확인 + 실행**

`mcp__UnityMCP__editor_refresh_assets` → 에러 0.
`mcp__UnityMCP__editor_invoke_method` `Lair.EditorTools.LairUIPrefabBuilder.BuildAllUIPrefabs`.
로그에서 "CardSelectionPopup" 빌드 메시지 확인.

- [ ] **Step 3: 프리팹 파일 확인**

Bash `ls Assets/_Lair/Prefabs/UI` → `CardSelectionPopup.prefab` 존재.

---

## Task M3.4: M3 검증 + 스테이지

- [ ] **Step 1: 회귀 테스트**

EditMode 자동 실행 — 누적 PASS 유지.

- [ ] **Step 2: 스테이지**

```powershell
git add Assets/_Lair/Scripts/UI/CardSelectionArg.cs `
        Assets/_Lair/Scripts/UI/CardSelectionPopup.cs `
        Assets/_Lair/Scripts/UI/CardView.cs `
        Assets/_Lair/Editor/LairCardPrefabBuilder.cs `
        Assets/_Lair/Editor/LairUIPrefabBuilder.cs `
        Assets/_Lair/Data `
        Assets/_Lair/Prefabs/UI `
        Assets/AddressableAssetsData
```

- [ ] **Step 3: 커밋 메시지(안)**

```
# [feat] - B1-M3 카드 SO 7장 + CardPool + CardSelectionPopup 프리팹 자동 생성
```

**M3 검증 게이트**: 7 CardData + 1 CardPool + CardSelectionPopup.prefab 생성, Addressables 등록, EditMode FAIL 0.

**🛑 사용자 검토 포인트**: Unity Game View 에서 CardSelectionPopup 프리팹을 임시 띄워(예: Editor 에서 BattleHud 대신) 시각 확인. 또는 M4 끝에서 통합 검증.

---

# B1-M4 — BattleController 통합 + 캐릭터 재빌드 (~1일)

## Task M4.1: LairCharacterPrefabBuilder 에 MonsterTag 추가

**Files:**
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs`

- [ ] **Step 1: 캐릭터 빌드 시 MonsterTag 부착**

`LairCharacterPrefabBuilder.BuildOne` 내부의 컴포넌트 부착 부분에 추가:

기존:
```csharp
if (spec.IsHero) go.AddComponent<HeroTargetProvider>();
else             go.AddComponent<MonsterTargetProvider>();
go.AddComponent<AutoCombatAI>();
```

변경 후:
```csharp
if (spec.IsHero)
{
    go.AddComponent<HeroTargetProvider>();
}
else
{
    go.AddComponent<MonsterTargetProvider>();
    //# B1 — MonsterTag 부착 + EMonster Key 주입
    var tag = go.AddComponent<MonsterTag>();
    if (System.Enum.TryParse<EMonster>(spec.Name, out var key))
        tag.Configure(key);
}
go.AddComponent<AutoCombatAI>();
```

`using Lair.Data;` 가 파일 상단에 없다면 추가.

- [ ] **Step 2: 컴파일 + 재빌드**

`mcp__UnityMCP__editor_refresh_assets` → 에러 0.
`mcp__UnityMCP__editor_invoke_method` `Lair.EditorTools.LairCharacterPrefabBuilder.BuildAllCharacterPrefabs`.
로그: 4개 캐릭터 재빌드.

- [ ] **Step 3: 프리팹 확인**

Bash `grep -A 3 "MonsterTag" Assets/_Lair/Prefabs/Characters/Slime.prefab | head -10` — `m_Script` 가 MonsterTag 의 GUID 인지 확인.

---

## Task M4.2: BattleController 에 SpawnMonsterRuntime + B1 통합

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: 신규 필드 + Start 확장**

기존 BattleController 의 필드 영역 끝에 추가:
```csharp
//# B1 신규
private PauseService _pause;
private PassiveTriggerService _passiveTriggers;
private TriggerQueue _queue;
private CardDeck _passiveDeck;
private IBattleContext _ctx;
private bool _processingQueue;
```

상단 using 추가:
```csharp
using Lair.Card;
```

`Start()` 의 `_clock.Start();` 직전에 다음 추가:

```csharp
//# B1 신규 — 일시정지 / 트리거 / 카드 풀
_pause = new PauseService();
_queue = new TriggerQueue();
if (_heroHealth != null)
{
    _passiveTriggers = new PassiveTriggerService(_heroHealth);
    _passiveTriggers.OnTriggered += idx =>
    {
        _queue.Enqueue(TriggerQueue.Source.Passive, idx);
        TryProcessNext();
    };
}
_ctx = new BattleContext(this);

var pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
if (pool != null) _passiveDeck = new CardDeck(pool.Cards);
```

- [ ] **Step 2: TryProcessNext 메서드 추가**

`EndBattle` 메서드 뒤에 추가:

```csharp
//# B1 — 큐 비울 때까지 카드 선택 팝업 순차 처리
private async void TryProcessNext()
{
    if (_processingQueue) return;
    if (_queue.Count == 0) return;
    if (_model.Result != BattleResult.None) return;
    if (_passiveDeck == null) return;

    _processingQueue = true;

    while (_queue.TryDequeue(out var entry))
    {
        if (_model.Result != BattleResult.None) break;

        _pause.Pause();
        var choices = _passiveDeck.Draw(3);
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

        var arg = new CardSelectionArg
        {
            Choices = choices,
            OnPicked = card =>
            {
                if (card?.Effect != null && _ctx != null) card.Effect.Apply(_ctx);
                tcs.TrySetResult(true);
            }
        };
        await CHMUI.Instance.ShowUIAsync(EUI.CardSelectionPopup, arg);
        await tcs.Task;
        _pause.Resume();
    }

    _processingQueue = false;
}

//# B1 — BattleContext.SpawnMonster 가 호출하는 런타임 스폰
public async void SpawnMonsterRuntime(Lair.Data.EMonster key, Vector3 nearHero)
{
    var prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
    if (prefab == null) return;
    var p = CHMPool.Instance.Pop(prefab, transform);
    if (p == null) return;

    var offset = UnityEngine.Random.insideUnitSphere * 2.5f;
    offset.y = 0f;
    p.transform.position = nearHero + offset;
}
```

`using Lair.UI;` 가 상단에 없으면 추가 (CardSelectionArg 사용).

- [ ] **Step 3: BattleContext 의 임시 SpawnMonster 본문 복원**

`BattleContext.cs` 의 `SpawnMonster`:
```csharp
public void SpawnMonster(EMonster key, Vector3 nearHero)
{
    _owner.SpawnMonsterRuntime(key, nearHero);
}
```

`Debug.LogWarning` 제거.

- [ ] **Step 4: 컴파일 확인 + 회귀 검증**

`mcp__UnityMCP__editor_refresh_assets` → 에러 0.
`LairTestRunner.RunEditModeTests` → 누적 PASS 유지, FAIL 0.

---

## Task M4.3: 자동 시뮬 검증

- [ ] **Step 1: sim_play + 자동 트리거 발동**

`mcp__UnityMCP__sim_play`.
3초 대기.
`mcp__UnityMCP__editor_invoke_method` `Lair.EditorTools.LairDebugProbe.LogAllHealth` — Knight HP 확인.

- [ ] **Step 2: Knight HP 강제 감소 → 90% 임계점 트리거**

`LairDebugProbe` 에 `DamageHero` 메서드 임시 추가 (또는 KillAllHeroes 활용 약화 버전). 또는 시뮬에서 자연 데미지로 90% 까지 기다림.

가장 빠른 방법 — `Lair/Debug` 메뉴에 `DamageHeroBy100` 추가:

Edit `Assets/_Lair/Editor/LairDebugProbe.cs`, KillAllHeroes 위에 추가:
```csharp
[MenuItem("Lair/Debug/Damage Hero 100")]
public static void DamageHero100()
{
    var snapshot = CharacterRegistry.Heroes.ToArray();
    foreach (var e in snapshot)
        if (e?.Health != null) e.Health.TakeDamage(100);
    Debug.Log("[Probe] Hero 에 100 데미지");
}
```

- [ ] **Step 3: 재컴파일 후 호출**

`editor_refresh_assets` → wait → `editor_invoke_method` `Lair.EditorTools.LairDebugProbe.DamageHero100`.
1초 대기 후 `editor_read_log` 로 CardSelectionPopup 표시 확인.

- [ ] **Step 4: 자동 카드 선택 시뮬**

`LairDebugProbe` 에 `AutoPickFirstCard` 추가:
```csharp
[MenuItem("Lair/Debug/Auto Pick First Card")]
public static void AutoPickFirstCard()
{
    //# 활성 CardSelectionPopup 의 첫 슬롯 클릭
    var popup = Object.FindFirstObjectByType<Lair.UI.CardSelectionPopup>();
    if (popup == null) { Debug.LogWarning("[Probe] CardSelectionPopup 없음"); return; }
    var slot = popup.GetComponentsInChildren<Lair.UI.CardView>(includeInactive: false)[0];
    var btn = slot.GetComponentInChildren<UnityEngine.UI.Button>();
    btn.onClick.Invoke();
    Debug.Log("[Probe] 첫 카드 자동 클릭");
}
```

`editor_refresh_assets` → wait → `AutoPickFirstCard` 호출 → 로그에서 카드 효과 적용 확인.

- [ ] **Step 5: sim_stop**

`mcp__UnityMCP__sim_stop`.

---

## Task M4.4: M4 검증 + 스테이지

- [ ] **Step 1: 스테이지**

```powershell
git add Assets/_Lair/Scripts/Battle/BattleController.cs `
        Assets/_Lair/Scripts/Battle/BattleContext.cs `
        Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs `
        Assets/_Lair/Editor/LairDebugProbe.cs `
        Assets/_Lair/Prefabs/Characters
```

- [ ] **Step 2: 커밋 메시지(안)**

```
# [feat] - B1-M4 BattleController 큐 루프 통합 + 캐릭터 프리팹 재빌드 (MonsterTag)
```

**M4 검증 게이트**: 자동 시뮬에서 트리거 → 일시정지 → 카드 표시 → 자동 클릭 → 효과 적용 → 재개. EditMode FAIL 0.

**🛑 사용자 검토 포인트**: Unity Game View 에서 직접 Play → Knight 가 자연 데미지 받아 90% 도달 시 카드 선택 화면 자동 표시 확인.

---

# B1-M5 — PlayMode smoke + 수동 체크리스트 (~1일)

## Task M5.1: PlayMode CardFlowSmokeTest 작성

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/CardFlowSmokeTest.cs`

- [ ] **Step 1: 테스트 작성**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.UI;

namespace Lair.Tests.PlayMode
{
    public class CardFlowSmokeTest
    {
        [UnityTest]
        public IEnumerator HP_90퍼_트리거시_CardSelectionPopup_자동표시()
        {
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# 1) BattleController.Start 가 비동기 — 영웅 스폰 + HUD 표시까지 대기 (~3초)
            float elapsed = 0f;
            while (elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }

            Assert.Greater(CharacterRegistry.Heroes.Count, 0, "Hero 스폰 확인");

            //# 2) Hero 에게 강제 데미지 (90% 임계점 통과)
            foreach (var e in CharacterRegistry.Heroes)
            {
                if (e?.Health != null)
                    e.Health.TakeDamage(e.Health.Max / 5);   //# 20% 데미지 → 80% 도달
            }
            yield return null;
            yield return null;

            //# 3) 1초 대기 (CHMUI.ShowUIAsync 비동기 완료)
            elapsed = 0f;
            while (elapsed < 1.5f) { elapsed += Time.unscaledDeltaTime; yield return null; }

            //# 4) CardSelectionPopup 이 씬에 활성화돼 있어야
            var popup = Object.FindFirstObjectByType<CardSelectionPopup>();
            Assert.IsNotNull(popup, "CardSelectionPopup 자동 표시 확인");

            //# 5) Time.timeScale 이 0 (Pause 작동)
            Assert.AreEqual(0f, Time.timeScale, 0.01f, "PauseService 작동 확인");

            //# 정리
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
```

- [ ] **Step 2: 실행**

`LairTestRunner.RunPlayModeTests` → `Library/lair-test-result-playmode.json` 에서:
- Slice A: 1 PASS
- B1 신규: 1 PASS
- **합계 2 PASS, FAIL 0**

---

## Task M5.2: 수동 체크리스트 9개 자동 검증 (가능한 항목)

- [ ] **Step 1: 수동 체크리스트를 사용자에게 안내**

다음 11개 항목을 사용자가 Unity Game View 에서 직접 확인:

1. Knight HP 90% 도달 시 CardSelectionPopup 자동 표시 + 시간 정지
2. 카드 3장 모두 다른 카드
3. 카드 클릭 시 효과 적용 + 팝업 닫힘 + 시간 재개
4. HP 80%, 70%, ..., 10% 까지 9회 전부 트리거
5. 큰 데미지로 여러 임계점 동시 통과 시 순차 처리
6. 강화 카드 동작 (슬라임 HP +50% 등 — 시각 확인은 HitFlash 가 그대로 유지되는지)
7. 스폰 카드 동작 (영웅 근처 슬라임 3마리 생성)
8. 교체 카드 동작 (슬라임 사라지고 골렘 1마리 생성)
9. 환경 카드 동작 (영웅 HP 가 1초마다 5 감소 — [Combat] 로그)
10. 영웅 사망 시 ResultPopup 정상 (Slice A 회귀)
11. 5분 도달 시 ResultPopup 정상 (Slice A 회귀)

---

## Task M5.3: 최종 검증 + 스테이지

- [ ] **Step 1: 전체 테스트 실행**

- EditMode: `LairTestRunner.RunEditModeTests` → 누적 PASS 확인 (~37 이상)
- PlayMode: `LairTestRunner.RunPlayModeTests` → 2 PASS

- [ ] **Step 2: 스테이지**

```powershell
git add Assets/_Lair/Tests/PlayMode
```

- [ ] **Step 3: 커밋 메시지(안)**

```
# [test] - B1-M5 PlayMode CardFlow smoke + 수동 체크리스트 11개
```

**M5 검증 게이트**: 자동 테스트 합계 ≥ 39 PASS / FAIL 0, 수동 11개 ✓.

**🛑 사용자 검토 포인트**: 검증 가설 평가 — "HP 임계점 트리거 + 3택 1 선택지가 빌드업의 재미를 만드는가". 한 판 풀 플레이 후 사용자 판단.

---

# 자가 검토

## 1. 스펙 커버리지

| 설계서 섹션 | 구현 태스크 |
|---|---|
| §0 목적/범위 | (개요로 인용) |
| §1 룰 매핑 | 모든 태스크 |
| §2 아키텍처 + 데이터 흐름 | M4.2 (BattleController.TryProcessNext) |
| §3.1 CommonEnum 추가 | M1.1 |
| §3.2 Card/CommonInterface | M1.2 |
| §4 카드 데이터 모델 | M1.6 (CardData/CardPool), M1.7 (CardDeck) |
| §5.1 PauseService | M1.3 |
| §5.2 PassiveTriggerService | M1.4 |
| §5.3 TriggerQueue | M1.5 |
| §5.4 일시정지 동작 | M4.2 통합으로 검증 |
| §6.1 MonsterTag | M2.1 + M4.1 (프리팹 부착) |
| §6.2 BattleContext | M2.3 + M4.2 (SpawnMonster 본문 복원) |
| §6.3 HeroAuraRunner | M2.2 |
| §6.4 PoisonAura | M1.8 |
| §7 CardSelectionPopup UI | M3.1 (스크립트), M3.3 (프리팹) |
| §8 BattleController 통합 | M4.2 |
| §9 자동화 | M3.2 (LairCardPrefabBuilder), M3.3 (LairUIPrefabBuilder), M4.1 (LairCharacterPrefabBuilder) |
| §10 테스트 전략 | M1.* TDD, M5.1 PlayMode, M5.2 수동 |
| §11 마일스톤 | M1~M5 |
| §12 위험/가정 | M4.2 의 LoadAsync null 가드 + Effect null 가드 |

→ **스펙 커버리지 OK**.

## 2. 플레이스홀더 스캔
- "TBD" / "TODO" / "fill in" 없음 ✅
- 모든 코드 스텝에 실제 코드 포함 ✅
- 모든 명령어 정확 ✅

## 3. 타입 일관성
- `ICardEffect.Apply(IBattleContext)` 시그니처 — 7개 효과 + CardData 일치 ✅
- `IBattleContext.GetMonsters/GetHero/SpawnMonster/ApplyHeroAura` — BattleContext 구현 일치 ✅
- `IHeroAura.OnAttached/Tick/OnDetached` — HeroAuraRunner/PoisonAura 일치 ✅
- `CardData._effect` SerializeReference — LairCardPrefabBuilder 의 `managedReferenceValue` 일치 ✅
- `EUI.CardSelectionPopup`, `EData.CardPool_Passive`, `ECardId.*`, `ECardCategory.*` — Enum 정의 + 사용처 일치 ✅
- `MonsterTag.Configure(EMonster)` — LairCharacterPrefabBuilder 호출 일치 ✅
- `BattleController.SpawnMonsterRuntime(EMonster, Vector3)` — BattleContext.SpawnMonster 호출 일치 ✅

→ **타입 일관성 OK**.

---

## 변경 이력
- **v0.1 (2026-05-19)**: 초안 — 설계서 v0.1 기반 5 마일스톤 / ~22 태스크 / ~95 step.
