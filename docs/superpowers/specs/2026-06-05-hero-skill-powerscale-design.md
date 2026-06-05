# 영웅 스킬 데미지에 공격력 배율(PowerScale) 적용 — 설계 (spec)

- 작성일: 2026-06-05
- 흐름: start-develop-simple (프로토타입 간소 — design-reviewer / code-reviewer / qa-simulator 생략, test-engineer 유지)
- 대상 시스템: `Assets/_Lair/Scripts/Character/Skills/HeroSkillContext.cs` (+ 영웅 IAttacker 참조)

## 1. 의도 / 범위

현재 영웅 공격력 약화(HeroAttackDown 등 PowerScale 디버프)는 **근접 자동공격에만** 적용되고 영웅 스킬 데미지(Nova/OrbitBlade/Dash 등)에는 적용되지 않는다. 스킬 데미지도 영웅의 공격력 배율(PowerScale)을 받도록 결합한다.

- **결정 락(범위)**: 스킬은 영웅의 **PowerScale 전체**를 곱한다. PowerScale 은 영웅 `MeleeAttacker` 의 단일 공유 배율이므로, 이를 깎는 모든 디버프(HeroAttackDown -25%, Weaken 일시 약화, 향후 추가될 공격 버프/디버프)가 자동으로 스킬에도 일관 적용된다. 디버프별 allowlist 를 두지 않는다(YAGNI).

범위 밖: 근접 데미지 계산 변경 없음(이미 PowerScale 적용 중), 넉백 변경 없음, 밸런스 시뮬(qa-simulator) 본 흐름 미포함, 신규 카드/스탯 없음.

## 2. 현재 구조 (조사 완료)

- `HeroAttackDownAura.OnAttached` → `_attacker.PowerScale *= 0.75`. `WeakenAura` 도 PowerScale 을 일시 곱함.
- `PowerScale` 을 읽는 유일한 지점: `MeleeAttacker.cs:117` — `target.TakeDamage(Mathf.RoundToInt(_power * PowerScale))` (근접 데미지).
- 스킬 데미지: `HeroSkillContext.ApplyAll(origin, amount, knockback)` → `Apply` → `e.Health.TakeDamage(amount)`. `amount` 는 스킬 설정값 그대로, **PowerScale 미참조**.
- `HeroSkillContext` 는 생성자에서 영웅 `Transform _hero` 만 보유.

## 3. 설계

### 3-1. 결합 지점
- `HeroSkillContext.ApplyAll` 진입 시 영웅 PowerScale 을 1회 읽어, `amount` 를 스케일한다: `int scaledAmount = Mathf.RoundToInt(amount * powerScale)`. 이후 `Apply` 는 `scaledAmount` 로 `TakeDamage`.
- 한 스킬 시전의 모든 대상이 동일 배율을 받는다(ApplyAll 단위 1회 계산).
- 근접(`MeleeAttacker.cs:117`)과 동일한 `Mathf.RoundToInt` 라운딩으로 일관성 유지.

### 3-2. PowerScale 주입
- `HeroSkillContext` 가 영웅의 `IAttacker`(MeleeAttacker)를 생성자에서 1회 캐싱한다(`_hero.GetComponent<IAttacker>()` — 기존 코드의 영웅 컴포넌트 1회성 참조 패턴과 일관).
- **데미지 적용 시점(ApplyAll)에 `.PowerScale` 을 live 로 읽는다** — 디버프는 시간에 따라 부착/해제되므로 시전 순간 값이 정확.
- 정확한 주입 방식(생성자 캐싱 vs 프로퍼티/Func 주입)은 plan/구현에서 기존 생성 흐름에 맞춰 확정. 단 "적용 시점 live read" 는 유지.

### 3-3. 불변 사항 / 엣지
- **넉백 무변경** — 데미지 amount 만 스케일.
- **PowerScale = 1**(디버프 없음): `RoundToInt(amount * 1) = amount` → 기존과 완전 동일(하위호환·회귀 안전).
- **IAttacker null**: 이론상 영웅엔 항상 존재하나, null 이면 배율 1 fallback(amount 그대로).
- **라운딩**: 작은 데미지 × 0.75 (예: 1→1, 2→2, 3→2, 4→3) — 근접과 동일 거동. 0 으로 소멸하는 케이스는 근접과 동일하게 허용.
- **누적 디버프**: HeroAttackDown 중복/Weaken 동시 등 PowerScale 곱연산 누적은 PowerScale 값 자체에 이미 반영 → 스킬은 그 값을 읽기만 하므로 자동 반영.

## 4. 결정 락 (Locked)

- 스킬은 영웅 **PowerScale 전체**를 곱한다(디버프별 allowlist 없음).
- 결합 지점: `HeroSkillContext.ApplyAll` 에서 amount 스케일, `Mathf.RoundToInt` 라운딩.
- PowerScale 은 **적용 시점 live read**.
- 넉백·근접 계산 무변경. PowerScale=1 이면 기존과 동일.

## 5. 테스트 (test-engineer)

- PowerScale 0.75 → 스킬 데미지가 75%(RoundToInt)로 감소.
- PowerScale 1.0 → 스킬 데미지 불변(회귀 고정).
- HeroAttackDown 부착 후 스킬 시전 → 데미지 감소.
- WeakenAura 부착 시 감소, 해제(OnDetached, PowerScale 복원) 후 원복.
- RoundToInt 경계값(예: 1·2·3 × 0.75) 검증.
- IAttacker 부재 시 배율 1 fallback(amount 그대로) — 가드 검증.

## 6. 영향 / 안전

- 영웅 스킬 DPS 가 공격력 디버프만큼 감소 → 5분 처치 밸런스에 영향. 본 흐름은 qa-simulator 미포함이므로, 구현 후 밸런스 의심 시 별도 qa-simulator 호출 권장.
- 몬스터/근접 데미지 경로 무변경. 코딩 룰(Rule 00~04)·MVP 범위(§8) 준수.
