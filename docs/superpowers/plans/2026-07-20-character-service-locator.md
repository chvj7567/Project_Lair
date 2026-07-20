# Character 서비스 로케이터 리팩터링 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. (start-develop-auto 파이프라인으로 진행 시 이 플랜은 game-designer·gameplay-programmer 의 입력으로 전달된다.)

**Goal:** 캐릭터 GameObject 당 하나의 `Character` 서비스 로케이터를 두고, 흩어진 `GetComponent<IXxx>()` 서비스 해석(내부 형제·외부 소비자·부모참조)을 전면 그 로케이터 경유로 전환한다. 게임 동작은 보존한다.

**Architecture:** `Character : MonoBehaviour` 를 캐릭터 루트에 부착. **Lazy** — `Get<T>()` 최초 호출 시 `GetComponent<T>()` 로 해석해 `Dictionary<Type, object>` 에 캐싱(non-null 만), 이후 O(1) 캐시 반환. `Awake` 등록/`DefaultExecutionOrder` 선행에 비의존(EditMode `Awake` 미실행·서비스 늦은 추가도 견딤). 제네릭 `Get<T>()`/`TryGet<T>` + 편의 타입 프로퍼티로 조회. 모든 소비자는 서비스를 `Character` 에서 1회 해석해 로컬 인터페이스 필드에 캐싱한 뒤 사용(현행 성능 유지).

**Tech Stack:** Unity 6 (6000.0.68f1), C#, Unity Test Framework(NUnit) EditMode/PlayMode, ChvjPackage 인프라.

## Global Constraints

- 코딩 룰 Rule 00~04 준수. 특히 Rule 02 §1(`//#` 주석)·§2(가드절)·§3(`var` 금지)·§4(`!` 금지)·§5(GetComponent 는 `Awake` 1회 캐싱).
- Rule 02 §5 god-object 방지 — `Character` 는 참조 보관·노출만. 게임 로직·상태 없음.
- Rule 04 §3 — 프리팹에 컴포넌트를 코드로 찍는 authoring 에디터 툴을 만든다면 실행 후 삭제(일회용). 상시 빌드 툴 아님.
- Rule 01 — 자동 커밋 금지. 아래 각 Task 의 "Commit" 단계는 **스테이징 + 커밋 메시지(안)** 까지만; 실제 `git commit` 은 사람이 수행. (파이프라인 route B 로 진행 시 최종 커밋은 파이프라인 6단계에서 일괄 처리.)
- namespace `Lair.Character`. 인터페이스 계약(`IHealth`/`IMover`/`IAttacker`/`IRotator`/`ITargetProvider`/`IAttackGate`/`IAnimatorSink`/`IDamageColorSink`) 변경 금지.
- 동작 보존 — 기존 EditMode/PlayMode 테스트가 전부 통과해야 함(주 회귀 안전망).
- 테스트 메서드명: 한글(`test_paths` 규약).

---

### Task 1: `Character` 서비스 로케이터 + 등록/조회 단위 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/Character/Character.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/CharacterServiceLocatorTests.cs`

**Interfaces:**
- Consumes: 기존 `Lair.Character` 인터페이스들(`IHealth` 등, `CommonInterface.cs`·`CommonInterface.HeroSkill.cs`).
- Produces:
  - `public T Get<T>() where T : class;` — **lazy 해석**, 미부착이면 null.
  - `public bool TryGet<T>(out T service) where T : class;`
  - `public IHealth Health { get; }` / `IMover Mover` / `IAttacker Attacker` / `IRotator Rotator` / `ITargetProvider TargetProvider` / `IAttackGate AttackGate` / `IDamageColorSink DamageColorSink` — 모두 내부적으로 `Get<T>()` 위임, 미부착 시 null.
  - **`IAnimatorSink` 는 노출 안 함** — POCO(`new AnimatorSink(...)`)라 `GetComponent<IAnimatorSink>` 는 항상 null, 소비자 0건.
  - 해석 시점: **lazy**(최초 `Get<T>()` 호출). `[DefaultExecutionOrder(-1000)]` 는 안전마진(load-bearing 아님).

- [ ] **Step 1: 실패 테스트 작성** — GameObject 에 `Health`(IHealth 구현) 부착 + `Character` 부착 후, `Get<IHealth>()` 와 `.Health` 가 그 인스턴스를 반환하고 미부착 서비스는 null 을 반환하는지, 그리고 **`Awake` 를 강제 실행하지 않아도 lazy 해석이 되는지** 검증.

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.EditMode.Character
{
    public class CharacterServiceLocatorTests
    {
        [Test]
        public void 부착된_서비스는_Get으로_조회되고_미부착은_null()
        {
            GameObject go = new GameObject("char");
            Health health = go.AddComponent<Health>();      //# IHealth 구현
            Lair.Character.Character character = go.AddComponent<Character>();

            //# lazy — Awake 강제 없이도 Get 호출 시점에 해석되어야 함(EditMode Awake 미실행 견딤)
            Assert.AreSame(health, character.Get<IHealth>());
            Assert.AreSame(health, (object)character.Health);
            Assert.IsNull(character.Get<IAttacker>());
            Assert.IsNull(character.Attacker);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryGet은_부착여부를_bool로_반환()
        {
            GameObject go = new GameObject("char");
            go.AddComponent<Health>();
            Lair.Character.Character character = go.AddComponent<Character>();

            Assert.IsTrue(character.TryGet(out IHealth h));
            Assert.IsNotNull(h);
            Assert.IsFalse(character.TryGet(out IAttacker a));
            Assert.IsNull(a);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void 서비스를_소비자보다_늦게_추가해도_lazy로_해석된다()
        {
            GameObject go = new GameObject("char");
            Lair.Character.Character character = go.AddComponent<Character>();
            //# Character 부착 후 서비스 추가 — eager Awake 방식이면 놓치지만 lazy 는 잡는다
            Health health = go.AddComponent<Health>();

            Assert.AreSame(health, character.Get<IHealth>());

            Object.DestroyImmediate(go);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — Unity Test Runner(EditMode) 또는 CLI. Expected: FAIL(`Character` 형 없음/컴파일 에러).

- [ ] **Step 3: `Character` 구현** — `Get<T>()` 가 최초 호출 시 `GetComponent<T>()` 로 해석해 캐싱(non-null 만), 이후 캐시 반환.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 캐릭터 GameObject 당 서비스 로케이터 — 흩어진 GetComponent<IXxx> 를 단일 진입점으로 총괄.
    //# 얇은 접근자만 (게임 로직·상태 없음, Rule 02 §5). lazy 해석이라 실행순서/Awake 선행에 비의존.
    [DefaultExecutionOrder(-1000)]
    public class Character : MonoBehaviour
    {
        //# 해석된 서비스 캐시 — non-null 만 저장. 미부착 서비스는 미캐싱(재호출 시 재해석).
        private readonly Dictionary<Type, object> _services = new();

        //# lazy — 최초 호출 시 GetComponent 로 해석·캐싱. 소비자 Awake 시점에 불려도 Character.Awake 선행 불필요.
        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object cached))
                return (T)cached;
            T service = GetComponent<T>();
            if (service != null)
                _services[typeof(T)] = service;
            return service;
        }

        public bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        public IHealth Health => Get<IHealth>();
        public IMover Mover => Get<IMover>();
        public IAttacker Attacker => Get<IAttacker>();
        public IRotator Rotator => Get<IRotator>();
        public ITargetProvider TargetProvider => Get<ITargetProvider>();
        public IAttackGate AttackGate => Get<IAttackGate>();
        public IDamageColorSink DamageColorSink => Get<IDamageColorSink>();
    }
}
```

> `IAnimatorSink` 프로퍼티/등록은 두지 않는다(POCO·소비자 0건). 제네릭 `Get<IAnimatorSink>()` 는 문법상 호출 가능하나 항상 null 이며 호출부가 없다.

- [ ] **Step 4: 테스트 통과 확인** — EditMode 실행. Expected: PASS(3개 테스트 — lazy·미부착 null·늦은 추가 포함).

- [ ] **Step 5: Commit(안)** — 스테이징 + 메시지(안). `# [refactor] - 캐릭터 서비스 접근을 총괄하는 Character 로케이터 추가`

---

### Task 2: 내부 형제 컴포넌트를 `Character` 경유로 전환

**Files (Modify):**
- `Assets/_Lair/Scripts/Character/AutoCombatAI.cs:66-74` (Awake 서비스 해석 5+1개)
- `Assets/_Lair/Scripts/Character/CharacterAnimationDriver.cs:33-37`
- `Assets/_Lair/Scripts/Character/HeroEntryDriver.cs:36-38`
- `Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs:26`
- `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs:31` (자기 `IHealth`) ← BLOCKER 1 추가분
- `Assets/_Lair/Scripts/Character/MonsterTargetProvider.cs:10` (자기 `IHealth`)
- `Assets/_Lair/Scripts/Character/HeroTargetProvider.cs:15` (자기 `IHealth`)
- `Assets/_Lair/Scripts/Character/PlagueSlowOnHit.cs:22` (자기 `IAttacker`) — 단, `:44` 의 `mb.GetComponent<IMover>()` 는 **대상 monster** 의 서비스 → Task 4 규칙(대상 Transform 라우팅) 적용
- `Assets/_Lair/Scripts/Character/MeleeAttacker.cs:117` 은 **대상**의 `IDamageColorSink` → Task 4 로 이관. (MeleeAttacker 자체가 소비하는 형제 서비스는 없음 — 이 파일은 Task 4 에서만 손댐)

> **주의** — `HeroAuraRunner` 는 `Assets/_Lair/Scripts/Battle/` 소속이지만 캐릭터(영웅) GameObject 에 붙는 형제 컴포넌트이므로 내부 형제 전환 규칙을 적용한다. asmdef 는 동일 `Lair` 라 참조 문제 없음.

**Interfaces:**
- Consumes: `Character.Get<T>()` (Task 1).
- Produces: 없음(내부 전환).

**전환 패턴** (모든 형제 컴포넌트 공통):

```csharp
//# (전) 형제 서비스를 각자 GetComponent
private void Awake()
{
    _mover = GetComponent<IMover>();
    _health = GetComponent<IHealth>();
    _attacker = GetComponent<IAttacker>();
    _targetProvider = GetComponent<ITargetProvider>();
    _rotator = GetComponent<IRotator>();
    _attackGate = GetComponent<IAttackGate>();
}

//# (후) Character 를 1회 잡고 서비스는 로케이터에서 해석
private void Awake()
{
    Character character = GetComponent<Character>();
    _mover = character.Get<IMover>();
    _health = character.Get<IHealth>();
    _attacker = character.Get<IAttacker>();
    _targetProvider = character.Get<ITargetProvider>();
    _rotator = character.Get<IRotator>();
    _attackGate = character.Get<IAttackGate>();   //# null=몬스터
}
```

> `Character` 는 lazy 라 소비자 `Awake` 에서 `character.Get<T>()` 를 부르는 시점에 해석된다 — `Character.Awake` 선행이 불필요(dictionary 는 첫 `Get` 때 채워짐). `[RequireComponent(typeof(Character))]` 가 형제 부착을 보장하므로 `GetComponent<Character>()` 는 non-null. 로컬 필드 캐싱 후 `Update` 경로는 불변 → 성능/동작 보존.

- [ ] **Step 1:** 위 8개 파일의 `Awake`(또는 서비스 해석부)를 전환 패턴으로 수정. `[RequireComponent]` 특성은 그대로 유지(형제 컴포넌트 존재 보장). 각 파일에 `[RequireComponent(typeof(Character))]` 를 추가해 로케이터 부착 누락을 컴파일/에디터 레벨에서 방지.
- [ ] **Step 2:** 컴파일 확인 — Unity 콘솔 에러 0.
- [ ] **Step 3:** 기존 관련 EditMode/PlayMode 테스트 실행(`HeroSkillRunner*`, `HeroAnimationDriver*`, `AutoCombatAI` 계열) — Expected: PASS(동작 보존). 테스트 GameObject 에 `Character` 미부착으로 NRE 나면 Task 5 의 테스트 픽스처 보강으로 처리.
- [ ] **Step 4: Commit(안)** — `# [refactor] - 캐릭터 내부 컴포넌트가 서비스를 Character 로케이터에서 해석`

---

### Task 3: 부모참조 컴포넌트를 `Character` 경유로 전환

**Files (Modify):**
- `Assets/_Lair/Scripts/Character/MonsterHpBar.cs:17,30` (`GetComponentInParent<IHealth>()`)
- `Assets/_Lair/Scripts/Character/CharacterAttackStrikeRelay.cs:19` (`GetComponentInParent<IAttackGate>()`)

**전환 패턴:**

```csharp
//# (전)
_health = GetComponentInParent<IHealth>();
//# (후) — 부모 루트의 Character 를 잡아 로케이터로 해석
Character character = GetComponentInParent<Character>();
_health = character != null ? character.Get<IHealth>() : null;
```

- [ ] **Step 1:** 두 파일의 부모참조 해석부를 전환. `MonsterHpBar` 는 `:30` 의 지연 재해석(null 시 재시도) 로직도 동일 패턴으로 유지.
- [ ] **Step 2:** 컴파일 확인.
- [ ] **Step 3:** 관련 PlayMode 테스트(`HeroAnimationTimingSyncPlayTests` — relay→AttackGate 경로) 실행. Expected: PASS.
- [ ] **Step 4: Commit(안)** — `# [refactor] - HP바·공격릴레이가 부모 Character 로케이터로 서비스 해석`

---

### Task 4: 외부 카드/시너지/스킬 소비자를 `Character` 경유로 전환

**Files (Modify):**
- `Assets/_Lair/Scripts/Card/Effects/WeakenEffect.cs:18` (대상 hero `IAttacker`)
- `Assets/_Lair/Scripts/Card/Effects/TimeStopEffect.cs:17-18` (대상 hero `IMover`+`IAttacker`)
- `Assets/_Lair/Scripts/Card/Effects/HeroAttackDownEffect.cs:15` (대상 hero `IAttacker`)
- `Assets/_Lair/Scripts/Card/Synergy/DebuffSynergyTier2.cs:16` (대상 hero `IAttacker`)
- `Assets/_Lair/Scripts/Card/Auras/PoisonAura.cs:88` (대상 `IDamageColorSink`)
- `Assets/_Lair/Scripts/Card/Auras/EternalBleedAura.cs:50` (대상 `IDamageColorSink`)
- `Assets/_Lair/Scripts/Card/Auras/BleedAura.cs:45` (대상 `IDamageColorSink`) ← BLOCKER 1 추가분
- `Assets/_Lair/Scripts/Battle/BattleContext.cs:59` (`GetHeroMover()` — 영웅 `IMover`) ← BLOCKER 1 추가분
- `Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs:21` (hero `IAttacker`) + `:118` (대상 `IDamageColorSink`)
- `Assets/_Lair/Scripts/Character/MeleeAttacker.cs:117` (대상 `IDamageColorSink`)
- `Assets/_Lair/Scripts/Character/PlagueSlowOnHit.cs:44` (대상 monster `IMover` via `mb`)

**전환 패턴** (대상 Transform/컴포넌트에서 서비스 해석):

```csharp
//# (전)
IAttacker atk = heroT.GetComponent<IAttacker>();
//# (후)
Character character = heroT.GetComponent<Character>();
IAttacker atk = character != null ? character.Get<IAttacker>() : null;

//# DamageColorSink 스탬프(전)
tc.GetComponent<IDamageColorSink>()?.StampDamageColor(color);
//# (후)
tc.GetComponent<Character>()?.Get<IDamageColorSink>()?.StampDamageColor(color);
```

> 이들은 런타임 중(Awake 이후) 해석하므로 실행순서 무관. 대상에 `Character` 미부착이면 null → 기존 `?.`/null 체크 계약과 동일(동작 보존). `PlagueSlowOnHit:44` 의 `mb` 는 대상 monster 컴포넌트이므로 `mb.GetComponent<Character>()?.Get<IMover>()`.

- [ ] **Step 1:** 위 파일 목록의 모든 지점을 전환 패턴으로 수정. `?.` 널 안전 체인을 유지해 미부착 대상에서 기존과 동일하게 no-op 되도록 한다. `BattleContext.GetHeroMover()` 는 `return e.Transform.GetComponent<Character>()?.Get<IMover>();` 형태로, `BleedAura` 는 `PoisonAura`/`EternalBleedAura` 와 동일 패턴으로 전환.
- [ ] **Step 2:** 컴파일 확인.
- [ ] **Step 3:** 관련 테스트(`HeroSkillContextPowerScale*`, `HeroSkillContextGeometryPlayTests`, 카드 효과 EditMode 계열) 실행. Expected: PASS.
- [ ] **Step 4: Commit(안)** — `# [refactor] - 카드·시너지·스킬이 대상 Character 로케이터로 서비스 접근`

---

### Task 5: 프리팹 7종에 `Character` 부착 + 역방향 검증 + 테스트 픽스처 점검

**Files:**
- Modify(프리팹): `Assets/_Lair/Art/Characters/Knight.prefab`, `Reaper.prefab`, `Wraith.prefab`, `Phantom.prefab`, `Hex.prefab`, `Wisp.prefab`, `Plague.prefab` — 각 루트에 `Character` 컴포넌트 1개 추가.
- Modify(테스트 픽스처, 필요 시): 아래 열거 파일 중 `Character` 자동부착(RequireComponent)으로 해결되지 않는 케이스만 개별 보강.

**작업 방식** (Rule 04 §3):
- 프리팹에 컴포넌트를 코드로 일괄 추가하려면 일회용 authoring 에디터 툴(`Lair/Build/AttachCharacterLocator` 등)을 작성해 7개 프리팹에 `Character` 를 부착·저장한 뒤 **툴을 삭제**한다. 또는 프리팹 소수라 에디터에서 수동 부착도 가능(이 경우 툴 없음).
- 부착 후 각 프리팹의 컴포넌트 목록에 `Character` 가 1개만 있는지 확인.

**역방향 안전망 (BLOCKER 2 대응)** — 서비스 인터페이스를 구현한 GameObject 는 반드시 `Character` 를 가져야 외부 `?.` 체인이 조용히 no-op 로 새지 않는다.
- 서비스 구현체(`Health`/`MeleeAttacker`/`SimpleMover`/`SimpleRotator`/`HeroAttackGate`/`HeroTargetProvider`/`MonsterTargetProvider`/`DamageFeedback` 등)가 전부 7개 캐릭터 프리팹 소속인지 확인 → 프리팹 7종 부착으로 커버 완료임을 검증.

**영향 테스트 전면 점검 (과소산정 금지)** — 캐릭터 GO 를 `AddComponent` 로 수동 조립하는 테스트 약 20개 파일:
`AutoCombatAIRotationTests`, `AutoCombatAIHysteresisTests`, `HeroEntryDriverPlayTests`, `CenterPullPlayTests`, `PhysicsAndFleeTests`, `HeroAnimationTimingSyncPlayTests`, `HeroAnimationDriverTests`, `HeroAuraRunner*`(6종), `HeroSkillRunner*`(3종), `HeroSkillContextPowerScale*`/`HeroSkillContextGeometryPlayTests`, `B3ActiveEffectTests`, `ContinuousSpawnIntegrationTest` 등.
- 원칙: Task 2·3 에서 소비자에 `[RequireComponent(typeof(Character))]` 를 붙였으므로 `AddComponent<소비자>()` 시 `Character` 가 **자동 부착**된다. `Character` 는 lazy 라 EditMode 에서 `Awake` 미실행이어도 `Get<T>()` 가 해석 → 대부분 픽스처는 **무수정 통과** 예상.
- 예외 점검: 서비스를 소비자보다 **늦게** `AddComponent` 하는 테스트도 lazy 라 안전하지만, 소비자를 `AddComponent` 하지 않고 서비스만 조립해 외부 경로로 접근하는 테스트(예: 카드 효과가 hero Transform 만 세워 접근)는 그 hero GO 에 `Character` 부착이 필요 → 해당 테스트에 `AddComponent<Character>()` 추가.

- [ ] **Step 1:** 7개 프리팹 루트에 `Character` 부착·저장.
- [ ] **Step 2:** (툴 사용 시) authoring 에디터 툴 삭제(Rule 04 §3).
- [ ] **Step 3:** 역방향 검증 — 서비스 구현체가 모두 7개 프리팹 소속임을 확인(다른 GO 에 붙는 서비스 구현체가 있으면 그 프리팹에도 `Character` 추가).
- [ ] **Step 4:** 위 열거 테스트를 전량 실행. RequireComponent 자동부착으로 성립하지 않는 개별 케이스만 `AddComponent<Character>()` 로 픽스처 보강.
- [ ] **Step 5:** 전체 EditMode + PlayMode 스위트 실행. Expected: **전부 PASS**(회귀 안전망).
- [ ] **Step 6:** Battle.unity 씬에 사전 배치된 캐릭터 인스턴스가 있으면 `Character` 반영 확인.
- [ ] **Step 7: Commit(안)** — `# [asset] - 캐릭터 프리팹 7종에 Character 로케이터 부착`

---

### Task 6: 전면 전환 검증 (잔여 GetComponent 스윕 + 회귀)

**Files:** 없음(검증 전용).

- [ ] **Step 1:** 서비스 인터페이스 직접 해석 잔재 스윕 — 프로덕션 코드에 `GetComponent(InChildren|InParent)?<I(Mover|Health|Attacker|Rotator|TargetProvider|AttackGate|AnimatorSink|DamageColorSink)>` 패턴이 남아있지 않은지 grep. `Character.cs` 의 `Get<T>()` 는 제네릭 `GetComponent<T>()` 라 이 패턴(구체 인터페이스명)에 안 걸린다.

```
Run: rg 'GetComponent(InChildren|InParent)?<I(Mover|Health|Attacker|Rotator|TargetProvider|AttackGate|AnimatorSink|DamageColorSink)>' Assets/_Lair/Scripts
Expected: 매치 0
```

- [ ] **Step 2:** 전체 테스트 스위트 재실행 — EditMode + PlayMode 전부 PASS.
- [ ] **Step 3: Commit(안)** — (변경 없으면 생략). 파이프라인 route B 면 6단계에서 전체 일괄 스테이징 + 커밋 메시지(안).

---

## Self-Review

**Spec coverage:**
- §3.1 `Character` 로케이터(lazy·IAnimatorSink 제외) → Task 1. ✅
- §3.2 소비자 전면 전환(내부/외부/부모) → Task 2·3·4. ✅ (2026-07-20 grep 34개 매치 라인 전부 매핑 — BLOCKER 1 의 `HeroAuraRunner`(Task 2)·`BleedAura`·`BattleContext`(Task 4) 포함. Task 6 grep 이 0 을 반환하도록 완결.)
- §3.3 lazy 해석 계약(실행순서 비의존) → Task 1 구현/테스트 + Task 2 주석. ✅
- §4 리스크(null 허용·lazy 회귀제거·테스트 전면열거·역방향 안전망) → Task 1 테스트, Task 5 Step3·4, Task 6. ✅
- §5 프리팹 7종 부착 + 역방향 검증 → Task 5. ✅
- §5 로케이터 단위 테스트 → Task 1(3 케이스). ✅

**Placeholder scan:** 전환 패턴은 대표 코드 블록 1회 + 대상 지점 파일:라인 전량 열거(동일 기계적 변환의 반복 코드는 DRY 상 패턴+사이트 목록으로 대체). "적절히 처리" 류 없음. ✅

**Type consistency:** `Get<T>()`/`TryGet<T>`/타입 프로퍼티 이름이 Task 1 정의와 Task 2~4 사용에서 일치. `IAnimatorSink` 는 노출 목록·프로퍼티에서 일관 제외. `IDamageColorSink`·`IAttackGate` 등 인터페이스명 spec 과 일치. ✅
