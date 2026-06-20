# Card Ideas — 2026-06-21 — 영웅 공격·방어 이중 잠식: 전투 능력 3중 약화 세트

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 영웅 전투 능력 3중 약화 — Debuff 축 기존 7장이 "공격력 약화(HeroAttackDown/Weaken)"와 "이동·행동 제어(Fear/Slow/TimeStop/Bleed/HeroPoisonAura)"에 집중된 반면, "방어력 약화(받는 피해 배율 증가)"와 "공격속도 저하(공격 쿨다운 영구 증가)"는 기존 28장 + 과거 23회차 어디에도 직접 카드화된 전례가 없는 공백. 오늘 3장은 이 두 축을 영구 패시브 2장 + 임시 복합 액티브 1장 형태로 채운다.
- **목록**: FleshWound (살점 노출 — 방어력 약화 영구 패시브) / HeavyBlade (무거운 검 — 공격속도 저하 영구 패시브) / SenseShatter (감각 분쇄 — 복합 20초 저주 액티브)
- **기존 28장 + git log 과거 23회차와의 중복 회피 확인됨**
  - 기존 28장: HeroAttackDown(공격력 영구 ×0.75), Weaken(공격력 임시 ×0.5), MarkOfDeath(받는 피해 임시 ×1.5, 5s) — 영구 "받는 피해 배율" 카드 없음. 영구 "공격 쿨다운 배율" 카드 없음 ✅
  - 과거 23회차 전부:
    - 5/28 전장 상태 감지: 픽 시점 스냅샷 스케일(필드 종류수·HP%) — 영웅 스탯 직접 약화 아님 ✅
    - 5/31 낙인 트리오: 임시 효과 + 영구 낙인 이중 구조 — 낙인은 "픽 행위마다 쌓이는 스택". FleshWound/HeavyBlade는 단일 영구 배율 ✅
    - 6/15 공격 반격 패널티: 영웅 공격/처치 이벤트 기반 누적 스택(ExhaustionCurse 등) — 이벤트 트리거 누적. FleshWound/HeavyBlade는 픽 즉시 영구 적용, 이벤트 없음 ✅
    - 6/20 저체력 포식자(BloodScent 등): HP 비율 분기 조건부 몬스터 강화 — 몬스터 stat 강화. FleshWound는 영웅 방어 약화 ✅
    - 나머지 18회차: Tank/Swarm/Dps 축 강화, 스포너 조작, 온데스 트리거, 타이머/밀도 임계 등 — 영웅 공격 쿨다운·받는 피해 직접 영구 약화와 무관 ✅

---

## 1. FleshWound — 살점 노출

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영웅이 받는 모든 피해 × 1.15 영구 적용 ("방어력 약화" 효과).
  - 영웅 HP 1000 기준: 순수 물리 DPS가 영웅에게 15% 더 들어감.
    - 예: Reaper DPS 40 → 실효 DPS 46 / Wraith DPS 20 → 23 / Phantom DPS 5 → 5.75.
    - 전 종 동시 DPS 추산 ~120 × 1.15 = **~138** — 영웅 유효 체력이 1000/1.15 ≈ **870**으로 단축.
  - 중첩 픽: 2픽 ×1.15² ≈ ×1.32 / 3픽 ×1.15³ ≈ ×1.52.
  - **밸런스 근거(컨셉 §8)**: 기준 사망 구간 2~4분. 1픽 FleshWound로 사망 기대 시간 약 13% 단축. HeroAttackDown(영웅 공격력 ×0.75 → 처치 속도 -25%)과 "거의 동등 강도" 대칭 설계. IronWill(몬스터 받는 피해 ×0.7, 15s)의 영웅 방향 반전 개념이나 영구 적용으로 장기 임팩트 높음.
- **구현 패턴**:
  - `IBattleContext` 를 통해 `IHeroState.AddModifier(EStat.DamageReceive, 1.15f, permanent: true)` 호출.
  - 영웅이 피해를 받을 때 `DamageHandler.ApplyToHero(raw) → raw * hero.DamageReceiveMultiplier`.
  - `HeroAttackDownEffect` 가 `IHeroState.AddModifier(EStat.AttackPower, 0.75f)` 를 이미 사용하는 구조 재사용. `EStat.DamageReceive` 추가와 DamageHandler 적용부 3~5줄만 신규.
- **시너지 후크**:
  - **MarkOfDeath(×1.5, 5s) + FleshWound(×1.15 영구)**: 중첩 시 ×1.725 → "방어 약화 심화 빌드"
  - **Bleed(이동 시 HP -2%) + FleshWound**: 몬스터 직격 + 이동 시 이중 체력 소모로 복합 압박
  - **WispHpBoost + FleshWound**: 위스프가 오래 살아있는 동안 FleshWound 누적 피해 극대화 — Tank×Debuff 크로스 시너지
- **구현 비용 추정**: 2 (IHeroState에 DamageReceive stat 필드 ~5줄, DamageHandler 적용부 ~3줄. 신규 시스템 없음.)
- **중복 재검증**: 기존 28장 + 23회차 어디에도 "영웅 받는 피해 영구 배율 증가" 카드 없음. MarkOfDeath는 임시(5s) + Dps 축 — FleshWound는 영구 + Debuff 축 ✅

---

## 2. HeavyBlade — 무거운 검

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영웅의 공격 쿨다운(공격 사이 간격) × 1.25 영구 적용 (공격 빈도 -20%).
  - 기본 쿨다운 1.0s → **1.25s**. 영웅 실효 DPS = 공격력 50 / 1.25 = **DPS 40** (기본 50 → -20%).
  - 중첩 픽: 2픽 × 1.56 쿨다운 → DPS ≈ 32 / 3픽 × 1.95 쿨다운 → DPS ≈ 26.
  - **밸런스 근거(컨셉 §8)**: HeroAttackDown 1픽(DPS -25%) vs HeavyBlade 1픽(DPS -20%) — 비슷한 단일 임팩트, stat 타겟이 다름. HeavyBlade는 HeroAttackDown과 중첩 시 가산(DPS = 50 × 0.75 / 1.25 = **30**, 기본 대비 40% 감소) — Debuff 빌드 심화 핵심 카드.
- **구현 패턴**:
  - `IHeroState.AddModifier(EStat.AttackCooldown, 1.25f, permanent: true)`.
  - 영웅 AttackBehavior 내 `_nextAttackTime = lastAttackTime + baseCooldown * hero.AttackCooldownMultiplier`.
  - `ReaperAtkSpeedEffect`(Reaper 쿨다운 ×0.7)의 영웅 대상 역방향 적용 패턴 재사용. IHeroState에 `AttackCooldown` stat 필드 추가.
- **시너지 후크**:
  - **HeroAttackDown(×0.75) + HeavyBlade(쿨다운 ×1.25)**: 영웅 실효 DPS = 50 × 0.75 / 1.25 = **30** — 기본의 60% 감소. "영웅 무장 해제" Debuff 빌드 완성형
  - **SenseShatter(쿨다운 ×1.4, 20s) + HeavyBlade(영구 ×1.25)**: 중첩 시 쿨다운 ×1.75 → 영웅 1.75초마다 1회 공격으로 극한 둔화
  - **Plague 계열 전체(PlagueSlowBoost + HeroPoisonAura + HeavyBlade)**: 영웅이 독장판에서 느리게 이동하며 느리게 공격 → 완전 무력화 빌드
- **구현 비용 추정**: 2 (FleshWound와 동일 IHeroState 인터페이스 수정 PR에 함께. AttackCooldown stat ~5줄 + AttackBehavior 적용부 ~3줄.)
- **중복 재검증**: 기존 28장 + 23회차 어디에도 "영웅 공격 쿨다운 영구 증가" 카드 없음. Weaken은 공격력(Power) stat 임시 감소 — 다른 stat + 임시 ✅. 6/19 SpawnerCycleRush는 스포너 주기 가속(몬스터 측) — FleshWound/HeavyBlade는 영웅 측 ✅

---

## 3. SenseShatter — 감각 분쇄

- **카테고리**: 액티브 저주 (Debuff 축)
- **효과 모델**:
  - 발동 즉시 **20초간** 영웅에게 "감각 분쇄" 상태 적용:
    - (1) 공격 쿨다운 × 1.4 (공격 빈도 -28%)
    - (2) 받는 피해 × 1.1 동시 적용
  - 기존 Weaken("공격력 ×0.5, 10s" — 단일 stat 단기 강한 효과)와 비교: SenseShatter는 두 stat 동시 중기(20s) 적용. 단일 stat 피크는 낮으나 이중 압박.
  - **밸런스 근거(컨셉 §8)**: 20s = 액티브 주기 30s의 67%. 이 구간 영웅 실효 DPS ≈ 50/1.4 ≈ 36, 받는 피해 ×1.1 상시. Fear(3s 강한 제어) / TimeStop(5s 정지) 대비 길지만 약한 복합 효과 — 지속 압박형 저주.
  - **FleshWound + SenseShatter 중첩**: 받는 피해 ×1.15 × ×1.10 = ×1.265 (20s간).
  - **HeavyBlade + SenseShatter 중첩**: 쿨다운 ×1.25 × ×1.40 = ×1.75 (20s간).
- **구현 패턴**:
  - `IHeroState.AddTemporaryModifier(EStat.AttackCooldown, 1.4f, duration: 20f)` + `IHeroState.AddTemporaryModifier(EStat.DamageReceive, 1.1f, duration: 20f)`.
  - FleshWound/HeavyBlade가 사용하는 `AddModifier` 의 임시 버전 오버로드 — `duration` 파라미터 추가로 자동 만료. Weaken/Slow의 임시 적용 패턴 참조.
  - 세 카드를 한 PR에 구현 시 IHeroState에 두 stat + 임시 오버로드를 일괄 추가 → 총 구현 비용 공유로 절감.
- **시너지 후크**:
  - **FleshWound + SenseShatter → MarkOfDeath(5s ×1.5)** 순 연결: 20s 지속 ×1.265 + 5s 스파이크 ×1.5×1.1 = ×1.65 → "받는 피해 에스컬레이션 콤보"
  - **HeavyBlade + SenseShatter**: 영웅 쿨다운 ×1.75 → DPS ≈ 29. Debuff Tier2(HeroAttackDown 자동 등록, Debuff 축 5픽 시) 이후 SenseShatter 발동 시 클라이맥스 압박
  - **Plague + Bleed + SenseShatter**: 독장판 5 DPS(영구) + 이동 시 HP -2% + SenseShatter 20s 복합 — Debuff 풀빌드 "최후 저주"
- **구현 비용 추정**: 2 (FleshWound·HeavyBlade 구현 완료 후 AddTemporaryModifier 오버로드만 추가. 세 카드 묶음 한 PR 총 비용 2~3.)
- **중복 재검증**: Weaken(공격력 stat ×0.5, 10s), Slow(이속 + 몬스터 이속 이중, 10s) — stat 종류와 조합이 완전히 다름 ✅. 6/15 ExhaustionCurse(공격마다 공격력 스택 감소)는 이벤트 누적. SenseShatter는 단순 timed 배율 ✅

---

## 4. 공통 테마 고찰

오늘 3장은 **"Debuff 축이 지금까지 직접 건드리지 않은 영웅 전투 스탯(방어 취약도·공격 빈도)의 첫 카드화"** 라는 공통 이유로 묶인다:

| 카드 | 타겟 스탯 | 적용 방식 | Debuff 축 기존 공백 |
|---|---|---|---|
| FleshWound | 영웅 받는 피해 배율 | 영구 ×1.15 | 0장 — 기존 없음 |
| HeavyBlade | 영웅 공격 쿨다운 배율 | 영구 ×1.25 | 0장 — 기존 없음 |
| SenseShatter | 위 두 스탯 동시 | 임시 20s × 1.4/×1.1 | 영구 버전 FleshWound·HeavyBlade 선행 |

세 카드가 함께 채택되면 다음 "Debuff 완전 빌드" 구조가 완성된다:

> [영구 공격력 약화] HeroAttackDown(×0.75) + HeavyBlade(쿨다운 ×1.25)  
> + [영구 방어 약화] FleshWound(받는 피해 ×1.15)  
> + [임시 복합 저주] SenseShatter(20s 쿨다운 ×1.4 + 받는 피해 ×1.1)  
> = 영웅 실효 DPS ≈ 24, 받는 피해 ×1.265(20s)

이 완성형 빌드에서 영웅 DPS는 기본(50)의 절반도 안 되고, 몬스터 피해는 26% 이상 증폭된다.

**왜 오늘 이 테마인가:**
- QA 리포트(2026-05-22)가 BLOCKED — 데이터 없이 설계 분석으로 공백 도출.
- 현재 Debuff 축 7장 분포:
  - 공격력 약화: HeroAttackDown(영구), Weaken(임시) → **2장**
  - 이동·행동 제어: Fear / Slow / TimeStop / Bleed / HeroPoisonAura → **5장**
  - 방어력 약화(받는 피해 배율): **0장**
  - 공격속도 약화(쿨다운 증가): **0장**
- v0.2 풀 확장(Debuff 축 ~10장 목표)에서 이 공백은 반드시 채워야 할 "축 설계 대칭 요소". 오늘 3장이 그 초안.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **구현 전제 조건**: `IHeroState` 에 `EStat.DamageReceive` · `EStat.AttackCooldown` stat 추가 필요. FleshWound + HeavyBlade를 먼저 영구 수정자로 구현, SenseShatter는 임시 오버로드로 후속.
- **구현 우선순위 제안**:
  1. FleshWound + HeavyBlade: 동일 `IHeroState` 수정 PR로 묶음 (비용 2)
  2. SenseShatter: 위 완료 후 `AddTemporaryModifier` 오버로드만 추가 (비용 1)
- v0.2 진입 전까지 backlog 보관
- **대칭 확장 제안**: FleshWound(방어 약화)가 채택되면 Tank 축 "영웅 받는 피해 감소" 카드(= IronWill의 영웅 방향 역전)와 대칭 완성 — 다음 회차 제안 후보.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 영웅을 약하게 만드는 카드들은 "힘을 낮추거나(공격력)" "몸을 못 움직이게 하는(공포·느려짐·멈춤·출혈)" 두 방향이었어요. 그런데 아직 아무도 제안하지 않은 게 있어요. 바로 "영웅이 맞을 때 더 많이 아프게 하기(방어 약화)"와 "공격 속도 자체를 느리게 하기(공격 쿨다운 증가)"예요. 마치 갑옷에 구멍이 뚫리고 팔이 무거운 검을 들어 느려진 영웅처럼, 싸울수록 불리해지는 새로운 방식입니다. 그래서 오늘 제안하는 카드 3장은: 영웅이 몬스터에게 맞을 때 항상 15% 더 많이 피해를 받는 '살점 노출(FleshWound)', 영웅이 공격할 때마다 평소보다 25% 더 오래 기다려야 하는 '무거운 검(HeavyBlade)', 그리고 20초 동안 이 두 가지를 동시에 걸어버리는 '감각 분쇄(SenseShatter)'입니다.
