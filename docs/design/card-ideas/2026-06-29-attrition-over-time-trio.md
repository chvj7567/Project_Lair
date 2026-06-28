# Card Ideas — 2026-06-29 — 런 경과 시간 누적 탈진 패시브 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요
- 테마: 런 경과 시간을 조건으로 하는 영웅 탈진 누적 패시브 — "5분이 흐를수록 던전이 영웅을 자동으로 갉아먹는다"
- 목록: TimeCurse (시간의 저주) / ExhaustionMark (탈진 표식) / ClockCurse (시계 저주)
- 기존 28장 + git log 과거 17회차 (2026-06-09 ~ 2026-06-27) + 파일 기반 30개 과거 회차 전부와의 중복 회피 확인됨. 이속·공속·출혈 카드는 기존에 즉시 적용 또는 일회성 임시 효과로 존재하지만, 순수 **시간 주기 누적 영구 패시브**는 이번이 첫 제안.

---

## 1. TimeCurse — 시간의 저주
- **카테고리**: 패시브 — 환경 (영웅 이속 영구 누적 감소)
- **효과 모델**: 픽 직후부터 **30초마다 1회** 영웅 이동속도 영구 ×0.96 적용. 픽 시점부터 발동하므로 런 5분 기준 최대 10회(0, 30, 60 … 270초 후) 발동 가능 → 0.96^10 ≈ ×0.664 (이속 -33.6%). 중간 픽(HP 50%)이라면 약 4~5회 = -17% ~ -19%. BattleClock이 0 기준이면 픽 직후 첫 발동이 즉시인지 30초 후인지는 구현 시 결정.
- **구현 패턴**: `TimeBasedMoveDebuffEffect : ICardEffect`. `Apply` 시 `IBattleContext.BattleClock.RegisterPeriodicCallback(interval: 30f, OnTick)`. `OnTick` → `IHeroAura.ApplyMovementSpeedMult(0.96f)` (BleedEffect·SlowEffect와 동일 IHeroAura API 사용, Duration = float.PositiveInfinity). 스포너 구조 변경 없음.
- **시너지 후크**: Slow(액티브, 이속 ×0.5, 10s) + TimeCurse(누적 -33.6%) 동시 = 영웅이 극단적으로 굼뜸. PhantomMoveSpeedBoost(팬텀 ×1.5)와 묶으면 추격 격차 극대화 (Swarm 축 조합). 초반 픽일수록 이득이 크므로 HP 90~80% 구간 픽 우선순위 카드.
- **구현 비용 추정**: 2 (RegisterPeriodicCallback API가 BattleClock에 이미 있으면 1; 없으면 콜백 등록 패턴 신규 추가 필요 — 약 10줄)
- **중복 재검증**: Slow(액티브, 임시 10s), TimeStop(5초 정지)은 일회성 임시 효과. hero-combat-cripple(2026-06-21, FleshWound·HeavyBlade·SenseShatter)은 즉시 1회 적용 영구 약화. 이건 **시간 주기 반복 누적** 패턴 — 메커니즘 레벨에서 완전히 구분됨.

---

## 2. ExhaustionMark — 탈진 표식
- **카테고리**: 패시브 — 환경 (영웅 공격력 영구 누적 감소)
- **효과 모델**: 픽 후 **90초마다 1회** 영웅 공격력 영구 ×0.95 적용. 5분 런에서 최대 3회(90s / 180s / 270s) 발동 → 0.95^3 ≈ ×0.857 (공격력 -14.3%). 개별 효과는 HeroAttackDown(즉시 ×0.75)보다 약하지만, 기존 HeroAttackDown과 **곱연산으로 중첩** 가능 → 동시 보유 시 총 ×0.75 × 0.857 ≈ ×0.643.
- **구현 패턴**: `TimeBasedAtkDownEffect : ICardEffect`. Apply → `IBattleContext.BattleClock.RegisterPeriodicCallback(90f, OnTick, maxTicks: 3)`. OnTick → `IHeroAura.ApplyAttackMult(0.95f)` (HeroAttackDownEffect와 동일 API — 배율 곱연산 누적). 3회 발동 후 콜백 자동 해제.
- **시너지 후크**: HeroAttackDown(즉시 ×0.75) + ExhaustionMark(3회 ×0.857) = 총 공격력 ×0.643 → Debuff 축 "완전 무장 해제" 빌드의 마무리 패시브. HP 80~70% 이전에 픽해야 3회 전부 확보 가능 — 타이밍 판단이 필요한 카드.
- **구현 비용 추정**: 2 (동일 PeriodicCallback 패턴 + HeroAttackDownEffect의 ApplyAttackMult API 재사용)
- **중복 재검증**: HeroAttackDown(패시브, 즉시 ×0.75)은 1회 적용 후 종료. hero-combat-cripple(2026-06-21)의 HeavyBlade(공속 영구 약화)·SenseShatter(방어 약화)도 즉시 1회. ExhaustionMark는 90초 간격 3회 분산 적용으로 후반 가중치가 높아지는 구조 — 동일 API, 다른 트리거 패턴.

---

## 3. ClockCurse — 시계 저주
- **카테고리**: 패시브 — 환경 (영웅 이동 중 출혈 자동 누적)
- **효과 모델**: 픽 후 **60초마다 1회** 영웅에게 출혈 스택 1개 등록 (이동 시 HP -1%, 지속 30초). 각 스택은 독립 인스턴스 — 동시에 살아있는 스택 수가 중첩 합산됨. 60초 스택은 90초에 만료되므로 120초 시점에 60초 스택은 소멸하고 120초 스택만 남음. 실질 동시 중첩 최대 = **2스택 (60s~90s 구간, 180s~210s 구간 등)** = 이동 시 HP -2%. 액티브 Bleed(이동 시 -2%, 10s)와 동시 보유 시 최대 -4%.
- **구현 패턴**: `TimedBleedStackEffect : ICardEffect`. Apply → `IBattleContext.BattleClock.RegisterPeriodicCallback(60f, OnTick)`. OnTick → `IHeroAura.RegisterBleed(damagePerMove: 0.01f, duration: 30f)` (BleedEffect와 동일 API — 중첩 인스턴스 추가 방식). BattleClock 종료(5분) 시 미완료 스택 자동 해제.
- **시너지 후크**: Bleed(액티브, -2%/이동, 10s) + ClockCurse(-1~2%/이동 지속) = 이동 시 HP -3~4% → 영웅이 움직이기를 꺼리는 딜레마(움직이면 피해, 안 움직이면 팬텀 포위). Slow(이속 ×0.5) + 이 카드 = 이동 자체가 고통. Swarm 축(PhantomMoveSpeedBoost) + Debuff 축(Bleed/ClockCurse) 교차 시너지 핵심 카드.
- **구현 비용 추정**: 3 (PeriodicCallback + IHeroAura 중첩 Bleed 인스턴스 관리 + 30초 만료 타이머 트래킹 — BleedEffect 기반이지만 중첩 인스턴스 생애 주기 관리 필요)
- **중복 재검증**: Bleed(액티브, 즉시 1회 10s)는 기존 카드. curse-companion-trio(2026-06-25)의 "출혈 동반 증폭"은 기존 Bleed 발동 시 추가 효과를 주는 것. 이건 Bleed 자체를 시간 주기로 자동 등록하는 패시브 — 트리거 방식이 전혀 다름.

---

## 4. 공통 테마 고찰
세 카드는 모두 **런 경과 시간을 트리거로 하는 영구 누적 영웅 약화 패시브** 라는 메커니즘 패턴을 공유한다.

**이 테마를 오늘 고른 이유**:
기존 28장과 과거 30개 루틴 출력을 전수 분석한 결과, 트리거 패턴은 다음 네 가지로 분류됐다:
- HP% 임계 통과 (패시브 트리거 자체 — 영웅 HP 10%마다)
- 처치/공격 이벤트 기반 (BloodThirst, kill-echo 계열)
- 즉시 발동 1회 영구 적용 (HeroAttackDown, WispHpBoost 등 대부분)
- 임시 시간 한정 효과 (IronWill, Frenzy, TimeStop 등)

**순수 런 경과 시간(초) 자체를 조건으로 하는 누적 영구 패시브** 패턴은 어디에도 없었다.

QA 리포트(2026-05-22)는 시뮬레이션 BLOCKED 상태라 픽률 데이터가 없으나, 컨셉 §8 기준(영웅 2~4분 사망)으로 추정 시:
- 2분 클리어: TimeCurse 4회 발동(-14.5%), ExhaustionMark 1회 발동(-5%), ClockCurse 2회 등록
- 4분 클리어: TimeCurse 8회 발동(-28%), ExhaustionMark 2회 발동(-9.8%), ClockCurse 4회 등록 (최대 2중첩)

4분 클리어 기준으로 세 카드 모두 '초반에 선택할수록 이득이 크다'는 픽 타이밍 전략 레이어가 생기는 효과가 있다.

---

## 5. 채택 흐름 제안
- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- 축 귀속 권장: TimeCurse → Swarm 축 또는 신규 "환경" 서브카테고리, ExhaustionMark → Debuff 축 심화 패시브, ClockCurse → Debuff 축 (Bleed 시너지 조합 카드)
- 세 카드 공통으로 `BattleClock.RegisterPeriodicCallback` API 의존 — gameplay-programmer가 BattleClock에 해당 API가 없으면 신규 추가 필요 (추정 구현 비용 1)

---

## 6. 쉬운 설명 (비개발자 요약)

지금까지 제안한 카드들은 대부분 "고르는 순간 바로 효과가 나타나거나, 특정 상황(몬스터 처치, 영웅 HP 감소)이 되면 효과가 발동"하는 방식이었다. 오늘은 그것과 완전히 다른 카드들이다 — **그냥 시간이 흘러가기만 해도 영웅이 점점 약해지는** 카드들이다. 마치 오래 싸울수록 지쳐가는 영웅처럼, 30초가 지날 때마다 발이 느려지고, 90초마다 주먹이 약해지고, 60초마다 자동으로 출혈이 시작된다. 영웅 입장에서는 "빨리 끝내야 한다"는 압박이 생기고, 던전 주인 입장에서는 "시간만 끌면 이긴다"는 전략이 생긴다. 그래서 오늘 제안하는 카드 3장은: 시간이라는 무기로 영웅을 소진시키는 **시간의 저주**, **탈진 표식**, **시계 저주**.
