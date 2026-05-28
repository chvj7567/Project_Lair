# Slice B2 — 30초 액티브 카드 시스템 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** B1 인프라(Queue/Pause/CardSelectionPopup) 를 재사용해 30초 액티브 트리거 + 5장 액티브 카드를 합류시킨다.

**Architecture:** `ActiveTriggerService` 가 `BattleClock.OnTick` 을 구독 → 9개 임계점(30/60/.../270초) 통과 1회 감지 → `TriggerQueue.Enqueue(Active, idx)` → 기존 `TryProcessNext()` 루프 재사용. 효과 5종(즉발/오라).

**Tech Stack:** Unity 6 (6000.0.68f1) / URP 17.0.4 / TDD POCO / ChvjPackage(CHMResource·CHMUI·CHMPool)

---

## 파일 구조

- Create: `Assets/_Lair/Scripts/Battle/ActiveTriggerService.cs`
- Create: `Assets/_Lair/Tests/EditMode/Battle/ActiveTriggerServiceTests.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/MonsterAoeDamageEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/HeroSlowEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/HeroSilenceEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/InstantSpawnGolemEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/InstantSpawnSlimesEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Auras/SlowAura.cs`
- Create: `Assets/_Lair/Scripts/Card/Auras/SilenceAura.cs`
- Create: `Assets/_Lair/Tests/EditMode/Card/SlowAuraTests.cs`
- Create: `Assets/_Lair/Tests/EditMode/Card/SilenceAuraTests.cs`
- Create: `Assets/_Lair/Tests/PlayMode/ActiveCardFlowSmokeTest.cs`
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (ECardId 5건 + EData.CardPool_Active 추가)
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs` (IBattleContext.GetHeroMover 추가)
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs` (IAttacker.Enabled 추가)
- Modify: `Assets/_Lair/Scripts/Character/MeleeAttacker.cs` (Enabled 프로퍼티 구현)
- Modify: `Assets/_Lair/Scripts/Battle/BattleContext.cs` (GetHeroMover 구현)
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (ActiveTriggerService + _activeDeck 통합)
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` (액티브 5장 + CardPool_Active 빌드 메뉴)

---

## Task 1: ActiveTriggerService (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/ActiveTriggerService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/ActiveTriggerServiceTests.cs`

- [ ] **Step 1: 실패하는 테스트 작성 (5개)**

```csharp
using System.Collections.Generic;
using Lair.Battle;
using NUnit.Framework;

namespace Lair.Tests.EditMode.Battle
{
    public class ActiveTriggerServiceTests
    {
        private static (BattleClock clock, ActiveTriggerService svc, List<int> fired) Setup()
        {
            var clock = new BattleClock(300f);
            clock.Start();
            var svc = new ActiveTriggerService(clock);
            var fired = new List<int>();
            svc.OnTriggered += idx => fired.Add(idx);
            return (clock, svc, fired);
        }

        [Test]
        public void Tick_Below30s_DoesNotFire()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(29.9f);
            Assert.AreEqual(0, fired.Count);
        }

        [Test]
        public void Tick_Reach30s_FiresIndex0()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(30f);
            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }

        [Test]
        public void Tick_Reach30sTwice_FiresOnlyOnce()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(30f);
            clock.Tick(0.1f);
            Assert.AreEqual(1, fired.Count);
        }

        [Test]
        public void Tick_BigDelta_FiresAllPassedThresholds()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(95f);    //# 30, 60, 90 통과
            Assert.AreEqual(3, fired.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, fired);
        }

        [Test]
        public void Dispose_StopsFiring()
        {
            var (clock, svc, fired) = Setup();
            svc.Dispose();
            clock.Tick(30f);
            Assert.AreEqual(0, fired.Count);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → 컴파일 실패 확인**

Run: `Lair/Tests/Run EditMode` 메뉴 또는 LairTestRunner
Expected: ActiveTriggerService 미정의 → 컴파일 에러

- [ ] **Step 3: 최소 구현**

```csharp
using System;

namespace Lair.Battle
{
    //# BattleClock.OnTick 구독 → 30초 단위 9개 임계점 1회 발동.
    public class ActiveTriggerService : IDisposable
    {
        private static readonly float[] Thresholds = { 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f };

        private readonly bool[] _fired = new bool[Thresholds.Length];
        private readonly BattleClock _clock;

        public event Action<int> OnTriggered;

        public ActiveTriggerService(BattleClock clock)
        {
            _clock = clock;
            if (_clock != null) _clock.OnTick += HandleTick;
        }

        public void Dispose()
        {
            if (_clock != null) _clock.OnTick -= HandleTick;
        }

        private void HandleTick(float elapsed)
        {
            for (int i = 0; i < Thresholds.Length; ++i)
            {
                if (_fired[i]) continue;
                if (elapsed >= Thresholds[i])
                {
                    _fired[i] = true;
                    OnTriggered?.Invoke(i);
                }
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 5개 PASS 확인**

Run: EditMode 테스트
Expected: 5/5 PASS

- [ ] **Step 5: 커밋 (사용자에게 메시지만 제안)**

```
# [feat] - B2 ActiveTriggerService (30초 임계점 트리거)
```

---

## Task 2: IAttacker.Enabled / IBattleContext.GetHeroMover 확장

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Character/MeleeAttacker.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleContext.cs`

- [ ] **Step 1: IAttacker 에 Enabled 추가**

`Assets/_Lair/Scripts/Character/CommonInterface.cs` 의 `IAttacker` 에:
```csharp
public interface IAttacker
{
    //# 기존 멤버 유지 + 침묵 효과용 토글
    bool Enabled { get; set; }
    // ... 기존 메서드
}
```

- [ ] **Step 2: MeleeAttacker.Enabled 프로퍼티 구현**

`MeleeAttacker.cs` 에:
```csharp
public bool Enabled
{
    get => enabled;
    set => enabled = value;
}
```
MonoBehaviour 의 `enabled` 와 매핑 — Update 가 호출 안 됨 → 공격 정지.

- [ ] **Step 3: IBattleContext 에 GetHeroMover 추가**

`Card/CommonInterface.cs` 의 `IBattleContext` 에:
```csharp
IMover GetHeroMover();
```

- [ ] **Step 4: BattleContext.GetHeroMover 구현**

`Battle/BattleContext.cs` 에:
```csharp
public IMover GetHeroMover()
{
    foreach (var e in CharacterRegistry.Heroes)
        if (e?.Transform != null) return e.Transform.GetComponent<IMover>();
    return null;
}
```

- [ ] **Step 5: EditMode 전체 PASS 확인 + 커밋 제안**

```
# [refactor] - B2 IAttacker.Enabled / IBattleContext.GetHeroMover 추가
```

---

## Task 3: SlowAura / SilenceAura + 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Auras/SlowAura.cs`
- Create: `Assets/_Lair/Scripts/Card/Auras/SilenceAura.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/SlowAuraTests.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/SilenceAuraTests.cs`

- [ ] **Step 1: SlowAura 테스트 작성 (실패)**

```csharp
//# 헬퍼 IMover 더블 — FakeHealth 와 유사 패턴
public class FakeMover : IMover { public float Speed { get; set; } public void MoveTo(UnityEngine.Vector3 _){} public void Stop(){} }

[Test]
public void OnAttached_HalvesSpeed_OnDetachedRestores()
{
    var hero = new FakeHealth(100);
    var mover = new FakeMover { Speed = 5f };
    var aura = new SlowAura(mover, slowFactor: 0.6f);
    aura.OnAttached(hero);
    Assert.AreEqual(3f, mover.Speed, 0.001f);
    aura.OnDetached(hero);
    Assert.AreEqual(5f, mover.Speed, 0.001f);
}
```

(`FakeMover` 는 `Helpers/FakeMover.cs` 에 추가 — 이미 있으면 재사용)

- [ ] **Step 2: SlowAura 구현**

```csharp
using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 IMover.Speed 를 slowFactor 배로 일시 변경. OnDetached 시 복원.
    [Serializable]
    public class SlowAura : IHeroAura
    {
        private readonly IMover _mover;
        private readonly float _factor;
        private float _backup;

        public SlowAura(IMover mover, float slowFactor = 0.6f)
        {
            _mover = mover;
            _factor = slowFactor;
        }

        public void OnAttached(IHealth hero)
        {
            if (_mover == null) return;
            _backup = _mover.Speed;
            _mover.Speed = _backup * _factor;
        }

        public void Tick(IHealth hero, float dt) { /* 시간 경과는 Runner 가 관리 */ }

        public void OnDetached(IHealth hero)
        {
            if (_mover != null) _mover.Speed = _backup;
        }
    }
}
```

- [ ] **Step 3: SilenceAura 테스트 작성 (실패)**

```csharp
public class FakeAttacker : IAttacker { public bool Enabled { get; set; } = true; /* 기타 멤버 */ }

[Test]
public void OnAttached_DisablesAttacker_OnDetachedRestores()
{
    var hero = new FakeHealth(100);
    var atk = new FakeAttacker();
    var aura = new SilenceAura(atk);
    aura.OnAttached(hero);
    Assert.IsFalse(atk.Enabled);
    aura.OnDetached(hero);
    Assert.IsTrue(atk.Enabled);
}
```

- [ ] **Step 4: SilenceAura 구현**

```csharp
using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 IAttacker.Enabled = false. OnDetached 시 true 복원.
    [Serializable]
    public class SilenceAura : IHeroAura
    {
        private readonly IAttacker _attacker;
        private bool _backup;

        public SilenceAura(IAttacker attacker) { _attacker = attacker; }

        public void OnAttached(IHealth hero)
        {
            if (_attacker == null) return;
            _backup = _attacker.Enabled;
            _attacker.Enabled = false;
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            if (_attacker != null) _attacker.Enabled = _backup;
        }
    }
}
```

- [ ] **Step 5: 테스트 PASS + 커밋 제안**

```
# [feat] - B2 SlowAura / SilenceAura (영웅 디버프 일시 적용)
```

---

## Task 4: 액티브 효과 5종

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/MonsterAoeDamageEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/HeroSlowEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/HeroSilenceEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/InstantSpawnGolemEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/InstantSpawnSlimesEffect.cs`

- [ ] **Step 1: MonsterAoeDamageEffect**

```csharp
using System;
using UnityEngine;

namespace Lair.Card
{
    //# 살아있는 모든 몬스터에 _damage 즉발 데미지.
    [Serializable]
    public class MonsterAoeDamageEffect : ICardEffect
    {
        [SerializeField] private int _damage = 50;

        public void Apply(IBattleContext ctx)
        {
            foreach (var m in ctx.GetMonsters())
                m.TakeDamage(_damage);
        }
    }
}
```

- [ ] **Step 2: HeroSlowEffect**

```csharp
using System;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 이동속도 _factor 로 _duration 초간 감소.
    [Serializable]
    public class HeroSlowEffect : ICardEffect
    {
        [SerializeField] private float _factor = 0.6f;
        [SerializeField] private float _duration = 5f;

        public void Apply(IBattleContext ctx)
        {
            var mover = ctx.GetHeroMover();
            if (mover == null) return;
            ctx.ApplyHeroAura(new SlowAura(mover, _factor), _duration);
        }
    }
}
```

- [ ] **Step 3: HeroSilenceEffect**

```csharp
using System;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 공격 _duration 초간 정지.
    [Serializable]
    public class HeroSilenceEffect : ICardEffect
    {
        [SerializeField] private float _duration = 5f;

        public void Apply(IBattleContext ctx)
        {
            var heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            var atk = heroT.GetComponent<Lair.Character.IAttacker>();
            if (atk == null) return;
            ctx.ApplyHeroAura(new SilenceAura(atk), _duration);
        }
    }
}
```

- [ ] **Step 4: InstantSpawnGolemEffect / InstantSpawnSlimesEffect**

```csharp
//# InstantSpawnGolemEffect
[Serializable]
public class InstantSpawnGolemEffect : ICardEffect
{
    public void Apply(IBattleContext ctx)
    {
        var heroT = ctx.GetHeroTransform();
        if (heroT == null) return;
        ctx.SpawnMonster(Lair.Data.EMonster.Golem, heroT.position);
    }
}

//# InstantSpawnSlimesEffect
[Serializable]
public class InstantSpawnSlimesEffect : ICardEffect
{
    [SerializeField] private int _count = 3;
    public void Apply(IBattleContext ctx)
    {
        var heroT = ctx.GetHeroTransform();
        if (heroT == null) return;
        for (int i = 0; i < _count; ++i)
            ctx.SpawnMonster(Lair.Data.EMonster.Slime, heroT.position);
    }
}
```

- [ ] **Step 5: 컴파일 OK + 커밋 제안**

```
# [feat] - B2 액티브 효과 5종 (AOE/슬로우/침묵/즉시소환)
```

---

## Task 5: CommonEnum 갱신

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`

- [ ] **Step 1: ECardId 에 5건 추가**

```csharp
public enum ECardId
{
    //# 기존 B1 7장 유지
    SlimeHpBoost, GolemDamageBoost, OrcAtkSpeed,
    SpawnSlimes, SpawnGolem,
    ReplaceSlimesToGolem,
    HeroPoisonAura,

    //# B2 액티브 5장
    MonsterAoeDamage,
    HeroSlow,
    HeroSilence,
    InstantSpawnGolem,
    InstantSpawnSlimes,
}
```

- [ ] **Step 2: EData 에 CardPool_Active 추가**

```csharp
public enum EData
{
    CardPool_Passive,
    CardPool_Active,
}
```

- [ ] **Step 3: 컴파일 OK + 커밋 제안**

```
# [feat] - B2 ECardId 5건 + EData.CardPool_Active enum 추가
```

---

## Task 6: LairCardPrefabBuilder 확장 — 액티브 카드 + 풀

**Files:**
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

- [ ] **Step 1: BuildActiveCards 메서드 추가**

기존 `BuildPassiveCards()` 와 동일 패턴.
- `Assets/_Lair/Data/Cards/Active/` 폴더에 5개 `CardData.asset` 생성
- 각 `_effect` 에 `managedReferenceValue` 로 효과 인스턴스 주입
- `Assets/_Lair/Data/CardPool_Active.asset` 생성 + 5장 참조
- Addressables 등록 (Resource 라벨)

```csharp
[MenuItem("Lair/Setup/B2 - Build Active Cards")]
public static void BuildActiveCards()
{
    EnsureDir("Assets/_Lair/Data/Cards/Active");
    var settings = GetAddressableSettings();
    var group = settings.FindGroup("Resource");

    BuildCard(ECardId.MonsterAoeDamage, ECardCategory.Environment,
        "전체 데미지", "모든 몬스터에 50 데미지",
        new MonsterAoeDamageEffect(), settings, group);
    BuildCard(ECardId.HeroSlow, ECardCategory.Environment,
        "영웅 둔화", "영웅 이동속도 40% 감소 5초",
        new HeroSlowEffect(), settings, group);
    BuildCard(ECardId.HeroSilence, ECardCategory.Environment,
        "영웅 침묵", "영웅 공격 5초 정지",
        new HeroSilenceEffect(), settings, group);
    BuildCard(ECardId.InstantSpawnGolem, ECardCategory.Spawn,
        "골렘 소환", "골렘 1마리 즉시 소환",
        new InstantSpawnGolemEffect(), settings, group);
    BuildCard(ECardId.InstantSpawnSlimes, ECardCategory.Spawn,
        "슬라임 떼", "슬라임 3마리 즉시 소환",
        new InstantSpawnSlimesEffect(), settings, group);

    BuildCardPool(EData.CardPool_Active, new[] {
        ECardId.MonsterAoeDamage, ECardId.HeroSlow, ECardId.HeroSilence,
        ECardId.InstantSpawnGolem, ECardId.InstantSpawnSlimes,
    }, "Active", settings, group);

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
}
```

(필요 시 기존 BuildPassiveCards 의 공통 로직 추출 — `BuildCard`, `BuildCardPool` 헬퍼)

- [ ] **Step 2: 메뉴 실행 → 5장 + 풀 생성 확인**

Unity 에디터에서 `Lair/Setup/B2 - Build Active Cards` 실행. Project 창에서:
- `Assets/_Lair/Data/Cards/Active/*.asset` 5개
- `Assets/_Lair/Data/CardPool_Active.asset`

- [ ] **Step 3: 커밋 제안**

```
# [feat] - B2 액티브 카드 SO 5장 + CardPool_Active 자동 빌더
```

---

## Task 7: BattleController 통합

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: 필드 + Start() 통합**

```csharp
private ActiveTriggerService _activeTriggers;
private CardDeck _activeDeck;
```

Start() 내 BattleClock 시작 이후 또는 시작 직후에:
```csharp
_activeTriggers = new ActiveTriggerService(_clock);
_activeTriggers.OnTriggered += idx =>
{
    _queue.Enqueue(TriggerQueue.Source.Active, idx);
    TryProcessNext();
};

var activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
if (activePool != null) _activeDeck = new CardDeck(activePool.Cards);
```

- [ ] **Step 2: TryProcessNext() 에서 deck 선택 분기**

```csharp
while (_queue.TryDequeue(out var entry))
{
    if (_model.Result != BattleResult.None) break;

    var deck = entry.SourceType == TriggerQueue.Source.Passive ? _passiveDeck : _activeDeck;
    if (deck == null) continue;

    _pause.Pause();
    var choices = deck.Draw(3);
    // ... 기존 로직 동일
}
```

기존 `_passiveDeck.Draw(3)` 한 곳을 `deck.Draw(3)` 로 변경.

- [ ] **Step 3: EndBattle() 에 Dispose 추가**

```csharp
_activeTriggers?.Dispose();
```

(현재 `_passiveTriggers?.Dispose()` 가 있다면 같은 위치에 — 없다면 둘 다 추가)

- [ ] **Step 4: 컴파일 OK + 커밋 제안**

```
# [feat] - B2 BattleController 액티브 트리거 / 액티브 덱 통합
```

---

## Task 8: PlayMode 스모크 테스트

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/ActiveCardFlowSmokeTest.cs`

- [ ] **Step 1: 테스트 작성**

기존 `CardFlowSmokeTest.cs` 패턴 참고:
- Battle 씬 로드
- BattleClock 가 30초 도달할 때까지 시뮬레이트 (sim_set_speed 활용 또는 Time.timeScale 조작)
- CardSelectionPopup 등장 확인
- 첫 카드 선택 → 효과 발동 → Resume
- Assert: 액티브 카드 1회 이상 처리됨

(전체 시나리오는 사용자 수동 검증 — PlayMode 테스트는 30초 트리거가 적어도 한 번 발동되는지만 검증)

- [ ] **Step 2: 테스트 실행 → PASS 확인**

Run: PlayMode 테스트

- [ ] **Step 3: 커밋 제안**

```
# [test] - B2 액티브 카드 플로우 PlayMode 스모크
```

---

## Task 9: 사용자 수동 검증 + 머지

- [ ] **Step 1: Unity 에디터에서 한 판 플레이**

- 30초마다 카드 선택 팝업 등장 확인 (최대 9회)
- 5장 카드 각각 화면에서 효과 확인
- 패시브 트리거(HP 90%) 와 액티브 트리거(30초) 동시 발생 시 둘 다 처리
- 영웅 사망 후 새 판 시작 시 슬로우/침묵 상태 잔존 X

- [ ] **Step 2: 사용자 OK 시 머지 권고 — main 으로 PR**

(직접 머지는 사용자가 결정)

---

## 자기 검토 (Self-Review)

**Spec 커버리지:**
- §3.1 ActiveTriggerService → Task 1 ✓
- §3.2 효과 5종 → Task 3/4 ✓
- §3.3 카드 풀 SO → Task 6 ✓
- §3.4 Editor 빌더 → Task 6 ✓
- §3.5 BattleController 통합 → Task 7 ✓
- §4 동시 트리거 처리 → 기존 TryProcessNext 가 가드 — 검증은 Task 9 수동

**플레이스홀더 스캔:** TBD/TODO 없음 ✓

**타입 일관성:** `IAttacker.Enabled` (Task 2) ↔ SilenceAura.OnAttached (Task 3) ↔ HeroSilenceEffect (Task 4) ✓
