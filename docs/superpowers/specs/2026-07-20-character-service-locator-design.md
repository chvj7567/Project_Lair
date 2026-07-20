# Character 서비스 로케이터 리팩터링 — Design Spec

- 날짜: 2026-07-20
- 상태: 확정 (사용자 승인)
- 유형: **동작 보존 리팩터링** (게임플레이 수치·페이싱 불변 — qa-simulator 불필요)

## 1. 배경 / 문제

캐릭터 GameObject(영웅 Knight 14개, 몬스터 6종 13개, Plague 14개)에 단일책임 MonoBehaviour 가 12~14개씩 붙어 있다. 각 컴포넌트는 형제 서비스를 `Awake` 에서 `GetComponent<IXxx>()` 로 개별 해석하고, 외부 카드/스킬은 대상 `Transform` 에서 `GetComponent<IXxx>()` 로 서비스를 꺼낸다.

- 서비스 해석 지점이 코드 전역에 흩어져 있음 (내부 형제 · 외부 소비자 · 부모참조 3계층).
- 한 캐릭터의 "무슨 서비스가 있는가"를 한눈에 볼 단일 진입점이 없음.

현재 아키텍처 자체는 인터페이스 주도(Rule 02 §5·§7 준수)로 이미 깔끔하다. 이번 작업은 **결합도를 더 낮추는 게 아니라, 서비스 접근을 단일 진입점(`Character`)으로 총괄**하는 것이다.

## 2. 목표 / 비목표

**목표**
- 캐릭터 GameObject 당 하나의 `Character` 서비스 로케이터를 두고, 모든 서비스 접근을 그것을 경유하도록 **전면 전환**.
- 흩어진 `GetComponent<IXxx>()` (내부 형제 + 외부 소비자 + 부모참조)를 `Character` 조회로 대체.

**비목표 (YAGNI)**
- 컴포넌트 개수 감소 — 하지 않는다. 기존 12~14개 MonoBehaviour 는 그대로 유지 (사용자가 퍼사드 방식 선택 시 확인).
- 게임플레이 동작·수치·페이싱 변경 — 없음. 순수 리팩터링.
- 인터페이스 계약(`IHealth`/`IMover`/…) 변경 — 없음.
- 서비스의 동적 등록/해제(런타임 add/remove) — 필요 없음. 캐릭터 구성은 프리팹 고정.

## 3. 설계

### 3.1 `Character` — 서비스 로케이터

위치: `Assets/_Lair/Scripts/Character/Character.cs`, namespace `Lair.Character`.

- 캐릭터 루트에 부착하는 `MonoBehaviour`.
- **Lazy 해석 방식** — `Get<T>()` 최초 호출 시 `GetComponent<T>()` 로 해석해 `Dictionary<Type, object>` 에 캐싱(non-null 만 저장), 이후 호출은 캐시 반환. **없으면 null 반환**(재호출 시 재해석 — 미부착 서비스는 캐시 안 함).
- Lazy 이므로 **실행순서/`Awake` 선행에 의존하지 않는다** — 소비자가 자기 `Awake` 에서 `Character.Get<T>()` 를 호출해도, `Character.Awake` 가 먼저 돌 필요가 없다(호출 시점에 해석). EditMode 에서 `Awake` 가 자동 실행되지 않아도 동작. (그래도 `[DefaultExecutionOrder(-1000)]` 는 무해하게 유지 — load-bearing 아님, 명시적 문서화용.)
- **null 허용** — 영웅/몬스터 서비스셋 차이 수용(몬스터는 `IAttackGate`·`HeroSkill` 계열 없음). 미부착 서비스는 `Get<T>()` 가 null.
- **얇은 접근자/코디네이터만.** 게임 로직·상태 없음. 참조 보관·노출이 전부 (Rule 02 §5 god-object 방지).
- 무상태 참조 캐시이므로 풀링 `OnEnable` 리셋 불필요(참조는 프리팹 고정, 풀 재사용해도 동일 컴포넌트).

노출 서비스 인터페이스(현행 계약 그대로):
`IHealth` · `IMover` · `IAttacker` · `IRotator` · `ITargetProvider` · `IAttackGate` · `IDamageColorSink`

> **`IAnimatorSink` 는 로케이터 범위 밖.** `AnimatorSink` 는 MonoBehaviour 가 아닌 POCO(`new AnimatorSink(animator, ...)` 로 `CharacterAnimationDriver` 가 생성)라 `GetComponent<IAnimatorSink>` 는 항상 null 이고, 프로덕션에 `GetComponent<IAnimatorSink>` 소비자가 0건이다. 따라서 등록/프로퍼티 대상에서 제외한다(제네릭 `Get<T>()` 는 임의 인터페이스를 받지만 애니메이터 sink 를 여기서 꺼내는 코드는 없다).

조회 API:
```csharp
public T Get<T>() where T : class;              //# lazy 해석, 없으면 null
public bool TryGet<T>(out T service) where T : class;
//# 편의 타입 프로퍼티 — 내부적으로 Get<T> 위임
public IHealth Health { get; }
public IMover Mover { get; }
public IAttacker Attacker { get; }
public IRotator Rotator { get; }
public ITargetProvider TargetProvider { get; }
public IAttackGate AttackGate { get; }          //# 몬스터는 null
public IDamageColorSink DamageColorSink { get; }
```

### 3.2 소비자 전면 전환

**소비 패턴 원칙**: 소비자는 `Character` 에서 서비스를 **1회 해석해 로컬 인터페이스 필드에 캐싱**한 뒤(현재와 동일하게 `Awake`/`OnEnable`), `Update`/이벤트 경로에서는 캐싱된 필드를 쓴다. 매 프레임 로케이터 조회 없음 → 성능 현행 유지 (Rule 02 §5).

> **소비자가 `Character` 를 얻는 방법** — 내부 형제는 자기 GameObject 에서 `[RequireComponent(typeof(Character))]` 로 부착을 강제하고 `GetComponent<Character>()` 로 잡으므로 **항상 non-null** → 무가드 사용 안전. 외부 소비자는 임의 대상 `Transform` 에서 `GetComponent<Character>()` 하므로 **null 가능** → `?.`/null 가드 필수. 이 비대칭은 의도된 것이며 계약으로 못박는다(내부=RequireComponent 보장, 외부=널 안전 체인).

1. **내부 형제 컴포넌트** — 각자 `Character` 를 `Awake` 에서 `GetComponent<Character>()` 1회로 잡고, 필요한 서비스를 `Character.Get<IXxx>()` 로 해석해 로컬 캐싱.
   - 대상: `AutoCombatAI`, `CharacterAnimationDriver`, `HeroSkillRunner`, `HeroEntryDriver`, `MonsterTargetProvider`, `HeroTargetProvider`, `PlagueSlowOnHit`(자기 `IAttacker`), **`HeroAuraRunner`**(자기 `IHealth`).
   - `AutoCombatAI` 는 **전투 구동 역할 유지** — 서비스만 `Character` 경유로 읽고 오케스트레이션 로직은 불변(중복 생성 금지).

2. **외부 카드/시너지/스킬 (대상 `Transform`/컴포넌트에서 서비스 해석)** — 대상에서 `GetComponent<Character>()` 를 잡아 `Get<IXxx>()`/타입 프로퍼티로 접근. 대상에 `Character` 미부착이면 null → 기존 `?.`/null 체크와 동일 no-op.
   - 대상: `WeakenEffect`, `TimeStopEffect`(`IMover`+`IAttacker`), `HeroAttackDownEffect`, `DebuffSynergyTier2`(`IAttacker`), `PoisonAura`·`EternalBleedAura`·**`BleedAura`**(대상 `IDamageColorSink`), `HeroSkillContext`(`_hero` 의 `IAttacker` + 대상 `IDamageColorSink`), `MeleeAttacker`(대상 `IDamageColorSink` 스탬프), `PlagueSlowOnHit`(대상 monster `IMover`), **`BattleContext.GetHeroMover()`**(영웅 `IMover`).

3. **부모참조 컴포넌트** — `GetComponentInParent<IXxx>()` 를 `GetComponentInParent<Character>()` 경유로 라우팅.
   - 대상: `MonsterHpBar`(`IHealth`), `CharacterAttackStrikeRelay`(`IAttackGate`).

> 전환 후에도 각 컴포넌트가 여전히 존재하고 인터페이스를 구현한다. 로케이터는 "그 서비스를 어디서 꺼내는가"만 단일화한다.
>
> **전면 전환 근거** — `Assets/_Lair/Scripts` 에서 위 8개 서비스 인터페이스를 `GetComponent(InChildren|InParent)?<I...>` 로 해석하는 프로덕션 지점을 grep 으로 전량 열거해 위 1~3 에 매핑했다(2026-07-20 기준 34개 매치 라인). `IAnimatorSink` 는 소비자 0건이라 전환 사이트 없음.

### 3.3 초기화 계약 (lazy)

- **`Character` 는 `Awake` 등록을 하지 않는다.** 서비스는 소비자가 최초로 `Get<T>()` 를 부르는 시점에 `GetComponent<T>()` 로 해석·캐싱된다(non-null 만). 따라서 `Character.Awake` 가 소비자보다 먼저 돌 필요가 없다 — 초기화 순서 의존이 없다.
- 내부 형제 소비자가 자기 `Awake` 에서 `Character.Get<T>()` 를 불러도, 그 호출 시점에 해석되므로 안전(로케이터 dictionary 는 그 순간 채워짐).
- 외부 소비자(카드/스킬)는 런타임 중 해석하므로 당연히 순서 무관.
- `[DefaultExecutionOrder(-1000)]` 는 안전마진일 뿐 load-bearing 아님(제거해도 동작 동일). 캐릭터 프리팹 구성이 고정이라 캐시 무효화/재해석 문제 없음.

## 4. 리스크 / 검증

- **동작 보존**: 인터페이스 계약·기존 컴포넌트 로직 불변. **기존 EditMode/PlayMode 테스트가 그대로 통과**해야 함(주 회귀 안전망).
- **null 허용**: 몬스터에 없는 서비스(`IAttackGate` 등)는 `Get<T>()` 가 null. 소비자는 현재도 null 체크 중이므로 계약 동일.
- **실행순서 회귀 — lazy 로 근본 제거**: `Get<T>()` 가 호출 시점에 `GetComponent<T>()` 로 해석하므로 `Character.Awake` 선행이 불필요. 소비자 `Awake`(자동/수동 무관)에서 호출해도 그 시점에 해석된다. `DefaultExecutionOrder` 는 안전마진일 뿐 load-bearing 아님.
- **테스트 영향 (전면 열거 — 과소산정 금지)**: 캐릭터 GameObject 를 `AddComponent` 로 **수동 조립**하는 테스트가 광범위하다(약 20개 파일·40+ AddComponent 지점 — `AutoCombatAIRotationTests`, `AutoCombatAIHysteresisTests`, `HeroEntryDriverPlayTests`, `CenterPullPlayTests`, `PhysicsAndFleeTests`, `HeroAnimationTimingSyncPlayTests`, `HeroAuraRunner*` 계열, `HeroSkillRunner*` 계열, `HeroSkillContext*` 계열, `HeroAnimationDriverTests`, `B3ActiveEffectTests`, `ContinuousSpawnIntegrationTest` 등). 리스크 완화:
  - 소비자에 `[RequireComponent(typeof(Character))]` 를 붙이면 `AddComponent<소비자>()` 시 `Character` 가 **자동 부착**된다 → 대부분의 픽스처가 수정 없이 성립.
  - `Character` 는 lazy 이므로 EditMode 에서 `Awake` 가 자동 실행되지 않아도 `Get<T>()` 가 정상 해석 → reviewer 가 우려한 "빈 `_services` 로 인한 조용한 null" 이 발생하지 않는다.
  - 단, 테스트가 `IAnimatorSink` 를 `Character` 로 꺼내려 하거나 서비스 컴포넌트를 소비자보다 **늦게** 추가하는 케이스는 test-engineer 단계에서 개별 확인. 이 전면 열거를 plan Task 5 에 픽스처 점검 항목으로 명시한다.
- **역방향 안전망**: 서비스 인터페이스를 구현한 GameObject 는 반드시 `Character` 를 가져야 외부 `?.` 체인이 no-op 로 새지 않는다. 서비스 구현체(`Health`/`MeleeAttacker`/`Simple*`/`DamageFeedback`/`HeroAttackGate`/`*TargetProvider` 등)가 전부 7개 캐릭터 프리팹 소속임을 확인하고, 프리팹 7종에 `Character` 부착을 보장한다(plan Task 5 역방향 체크).

## 5. 범위 / 산출물

- 신규: `Character.cs` (서비스 로케이터).
- 수정: 내부 형제 컴포넌트 8종 + 외부 소비자 7종 + 부모참조 2종의 서비스 해석부.
- 프리팹: 7개 캐릭터 프리팹(Knight + 몬스터 6종)에 `Character` 컴포넌트 1개씩 추가 + `DefaultExecutionOrder` 확인.
- 테스트: 기존 테스트 회귀 통과 + `Character` 로케이터 등록/조회/null-허용 단위 테스트 신규.

## 6. 미해결 / 후속

- 없음. 후속 컴포넌트 수 감소(POCO 통합)는 이번 범위 밖 — 필요 시 별도 spec.
