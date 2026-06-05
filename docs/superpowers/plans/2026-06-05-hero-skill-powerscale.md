# 영웅 스킬 데미지에 PowerScale 적용 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **프로토타입 간소(start-develop-simple) 골격 plan** — 순수 메커니즘 결합(밸런스 수치 없음). 모든 결정은 spec 에서 락됨.

**Goal:** 영웅 스킬 데미지가 영웅의 공격력 배율(PowerScale)을 받아, 공격력 약화 디버프가 스킬에도 적용되게 한다.

**Architecture:** `HeroSkillContext` 가 영웅 `IAttacker` 를 1회 캐싱하고, `ApplyAll` 에서 스킬 `amount` 를 `Mathf.RoundToInt(amount * PowerScale)` 로 스케일해 데미지를 적용한다. PowerScale 은 적용 시점 live read 라 부착된 모든 디버프(HeroAttackDown/Weaken)가 자동 반영된다.

**Tech Stack:** Unity 6 / C# / Lair.Character. 테스트는 Unity Test Framework (EditMode — HeroSkillContext 는 POCO 라 Fake 로 검증 가능).

---

## 파일 구조

- **Modify** `Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs` — 생성자에서 영웅 `IAttacker` 캐싱, `ApplyAll` 에서 `amount` 를 PowerScale 로 스케일.
- (확인) `HeroSkillContext` 생성처 — 영웅 Transform 으로 생성되는 지점. IAttacker 캐싱이 그 흐름과 충돌 없는지 확인(생성자 시그니처 불변, 내부에서 `_hero.GetComponent<IAttacker>()`).
- **Test (Create/Modify)** `Assets/_Lair/Tests/EditMode/Character/HeroSkillContextPowerScaleTests.cs` — PowerScale 스케일/회귀/라운딩/fallback 검증. (기존 HeroSkillContext 테스트가 있으면 그 파일에 보강)

> HeroSkillContext 는 `CharacterRegistry.Monsters` 정적 리스트 + Transform/IHealth 인터페이스로 동작하는 POCO. 기존 스킬 테스트(있다면)의 Fake 패턴을 재사용한다.

---

## Task 1: HeroSkillContext 에 PowerScale 결합

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/HeroSkillContextPowerScaleTests.cs`

- [ ] **Step 1: 기존 스킬 테스트/생성 흐름 확인**
  - `HeroSkillContext` 의 생성처(`new HeroSkillContext(heroTransform)`)와 기존 테스트가 IHealth/IAttacker 를 어떻게 Fake 하는지 확인. `IAttacker.PowerScale`(get/set, `CommonInterface.cs:59`)를 구현한 Fake 가 필요.

- [ ] **Step 2: 실패 테스트 작성**
  - PowerScale 0.75 인 영웅으로 `DamageMonstersInRing`(또는 In Cone/Spheres) 시전 → 대상이 받은 데미지가 `RoundToInt(amount * 0.75)` 인지 검증. (Fake IHealth 가 받은 TakeDamage 인자 기록)
  - 회귀: PowerScale 1.0 → 데미지 == amount.
  - 라운딩 경계: amount 3, PowerScale 0.75 → 2.
  - IAttacker 부재(영웅에 미부착 Fake) → 배율 1 fallback, 데미지 == amount.

- [ ] **Step 3: 테스트 실패 확인**
  - Run: `Lair/Tests/Run EditMode Tests` (또는 러너) — 신규 케이스 FAIL(아직 스케일 미적용 → 데미지 == amount 라 0.75 케이스 실패).

- [ ] **Step 4: 구현**
  - `HeroSkillContext` 에 `private readonly IAttacker _heroAttacker;` 추가, 생성자에서 `_heroAttacker = _hero != null ? _hero.GetComponent<IAttacker>() : null;` 캐싱.
  - `ApplyAll(origin, amount, knockback)` 진입부에서 `float scale = _heroAttacker != null ? _heroAttacker.PowerScale : 1f;` 읽고 `int scaledAmount = Mathf.RoundToInt(amount * scale);`. 이후 `Apply(e, origin, scaledAmount, knockback)` 호출.
  - `Apply` 의 `TakeDamage(amount)` 가 scaledAmount 를 받게 됨. 넉백은 무변경.
  - 가드/스타일: `//#` 2줄, `var` 금지, `!` 금지, `== null` 사용.

- [ ] **Step 5: 테스트 통과 확인**
  - Run: EditMode 러너 — 신규 케이스 PASS, 기존 HeroSkillContext/스킬 테스트 회귀 0.

- [ ] **Step 6: 컴파일 + git add**

## Task 2: 디버프 연동 통합 테스트 + 마무리

**Files:**
- Test: `Assets/_Lair/Tests/EditMode/Character/HeroSkillContextPowerScaleTests.cs` (보강) 또는 기존 Aura 테스트 위치

- [ ] **Step 1: 디버프 경유 통합 테스트**
  - `HeroAttackDownAura.OnAttached` 로 영웅 IAttacker.PowerScale 을 0.75 로 만든 뒤 스킬 시전 → 데미지 75% 검증(디버프→스킬 경로 end-to-end).
  - `WeakenAura` 부착 시 감소, `OnDetached`(PowerScale 복원) 후 스킬 데미지 원복 검증.
  - (Fake IAttacker 의 PowerScale 을 Aura 가 곱하도록 — Aura 는 IAttacker 인터페이스만 의존하므로 Fake 로 구동 가능)

- [ ] **Step 2: 전체 EditMode/PlayMode 회귀 실행** — fail 이 기존 baseline 을 넘지 않는지 확인.

- [ ] **Step 3: 변경 요약 + Rule 01 커밋 메시지(안)** (메인 처리)

---

## Self-Review (spec 대비)

- **3-1 결합 지점(ApplyAll amount 스케일, RoundToInt)** → Task 1 Step4 ✅
- **3-2 PowerScale 주입(생성자 IAttacker 캐싱, 적용시점 live read)** → Task 1 Step4 ✅
- **3-3 불변/엣지(넉백 무변경 / PowerScale=1 회귀 / IAttacker null fallback / 라운딩)** → Task 1 Step2,4 ✅
- **범위 락(PowerScale 전체 → 모든 디버프 자동 반영)** → Task 2 Step1(HeroAttackDown·Weaken 경유 검증) ✅
- **테스트(스케일/회귀/라운딩/fallback/디버프 연동)** → Task 1 Step2 + Task 2 Step1 ✅
- 밸런스 수치 없음(순수 메커니즘) — 플레이스홀더 아님. 시그니처/파일경로 구체화됨.
