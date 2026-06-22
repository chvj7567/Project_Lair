# Card Ideas — 2026-06-23 — 선도자 오라: 필드 생존 조건부 연동 강화

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- 테마: **선도자 오라 (Presence Aura)** — 특정 몬스터가 필드에 살아있는 동안 다른 몬스터(혹은 영웅)에게 조건부 버프/디버프를 걸어, 영웅의 타게팅 딜레마를 유발하는 카드 3종
- 목록: WraithCommand (레이스의 통솔) / PhantomRipple (팬텀 파문) / PlagueMiasma (역병의 독기)
- 기존 28장 + git log 과거 25회차와의 중복 회피 확인됨
  - 2026-05-29 `cross-species-synergy-trio` (빌드 픽 카운트 기반 종간 강화) 와의 차이: 오늘 제안은 **실시간 필드 생존 수**를 조건으로 하는 동적 토글 버프. 픽 이후 영구 적용이 아니라 활성/해제가 반복됨 — 메커니즘 레이어가 다름.
  - 2026-06-07 `wraith-phantom-awakening` (레이스·팬텀 고유 스탯 강화) 와의 차이: 오늘은 A종 생존이 B종을 강화하는 **교차 조건부** 구조. 단일 종 스탯 버프가 아님.

---

## 1. WraithCommand — "레이스의 통솔" (가칭)

- **카테고리**: 패시브 강화
- **효과 모델**:
  - 레이스(Wraith)가 필드에 **1마리 이상** 생존 중인 동안, 위스프(Wisp) 전체 HP ×1.25 동적 적용 (조건 ON)
  - 레이스가 전멸하면 즉시 해제 (조건 OFF)
  - 중첩 픽 2회: ×1.25² ≒ ×1.56 (레이스 생존 조건 계속 유지)
  - 수치 근거: WispHpBoost(무조건 ×1.5)보다 낮은 ×1.25로 설정 → 조건부라 실질 기대값이 낮아 밸런싱 §8 범위 이탈 없음. 레이스 생존율 ≒ 60~70% 가정 시 기대 배율 ×1.15~1.17
- **구현 패턴**:
  - `ICardEffect.Apply` 시 영구 오라 리스너를 등록. OnUpdate 또는 `IBattleContext.GetFieldCount(EMonster.Wraith) > 0` 폴링으로 상태를 감지
  - 조건 참 → `MonsterBuffService.ApplyGlobalBuff(EMonster.Wisp, StatType.Hp, ×1.25)` 호출
  - 조건 거짓 → `MonsterBuffService.RemoveBuff(...)` 해제
  - `IHeroAura` 대신 `IPresenceAura` 인터페이스 신설 권장 (A종 생존 → B종 버프 패턴 재사용 고려)
- **시너지 후크**:
  - WispHpBoost(×1.5 영구) + WraithCommand(×1.25 조건부) = 레이스 생존 중 위스프 HP ×1.875 → Tank Tier1·2 시너지와 결합 시 위스프가 사실상 불사에 가까워짐
  - SpawnWraith(레이스 동시 출력 +1) → 레이스 필드 생존율 증가 → WraithCommand 조건 유지율 상승 → 자연스러운 빌드 연계
  - 영웅 AI: 기본 "가장 가까운 몬스터 우선" → 레이스와 위스프가 섞여 있을 때 어느 것을 먼저 잡아야 하는지 고민 발생. **타게팅 딜레마 연출이 핵심 설계 의도**
- **구현 비용 추정**: 3 (IBattleContext.GetFieldCount API 기존 구현 여부에 따라 2~3. 없으면 신규 추가 필요)
- **중복 재검증**: WispHpBoost는 무조건 영구 글로벌. WraithCommand는 레이스 생존을 전제로 한 **조건부 동적 토글**이라 완전히 다른 레이어. 기존 25회차 어디에도 "특정 종 필드 생존 → 다른 종 실시간 버프 ON/OFF" 패턴은 없음.

---

## 2. PhantomRipple — "팬텀 파문" (가칭)

- **카테고리**: 패시브 강화
- **효과 모델**:
  - 필드의 팬텀(Phantom) 수가 **3마리 이상**일 때, 모든 몬스터 이동속도 ×1.2 동적 적용 (조건 ON)
  - 3마리 미만이면 즉시 해제 (조건 OFF)
  - 임계치 3은 기본 Phantom 스포너 동시 출력 1 × 스폰 주기 기준 필드 평균 수와 대응 — SpawnPhantoms 픽 후 출력이 2로 늘면 임계 달성이 훨씬 쉬워짐
  - 중첩 픽 2회: ×1.44
  - 수치 근거: 이동속도 ×1.2는 SpawnerHaste(주기 ×0.8) 보다 약하고 Slow 카드(몬스터 이속 ×1.3)보다 낮음. 전체 적용이지만 조건부 + 팬텀 수 관리 비용이 있어 균형 수준
- **구현 패턴**:
  - `IBattleContext.GetFieldCount(EMonster.Phantom) >= 3` 폴링
  - 조건 참 → `MonsterBuffService.ApplyGlobalBuff(EMonster.All, StatType.MoveSpeed, ×1.2)`
  - 팬텀 수 감소로 조건 거짓 → 해제
  - 기존 `SlowEffect`(10s 한시)와 달리 지속성이 팬텀 필드 상태에 묶임 → 영구와 한시의 중간 성격
- **시너지 후크**:
  - SpawnPhantoms(팬텀 출력 +1) → 임계 3 달성 용이 → PhantomRipple 상시 활성화
  - PhantomMoveSpeedBoost(팬텀 이속 ×1.5) → 팬텀이 빠르게 움직여 영웅 주변을 맴돌아 죽기 어려움 → 팬텀 필드 유지율 상승 → PhantomRipple 안정화
  - SpawnerHaste(모든 스포너 주기 ×0.8) + PhantomRipple = 전체 몬스터 이속+스폰속도 동반 상승 — Swarm 빌드 최종 형태
  - Swarm Tier1(팬텀·위스프 이속 ×1.3) + PhantomRipple 이속 ×1.2 = 팬텀 이속 최대 ×1.56
- **구현 비용 추정**: 3 (GetFieldCount API 재사용 가능하면 2)
- **중복 재검증**: density-tide-pressure(06-06)는 스폰 밀도/파도 흐름 카드이고 이속 조건부 토글이 아님. spawner-cycle-rush(06-18)는 스포너 주기 가속 특화. PhantomRipple은 "팬텀 생존 수 임계 → 전체 이속 실시간 ON/OFF" 완전히 다른 구조.

---

## 3. PlagueMiasma — "역병의 독기" (가칭)

- **카테고리**: 패시브 환경
- **효과 모델**:
  - 플레이그(Plague)가 필드에 **1마리 이상** 생존 중인 동안, 영웅 HP -3/s 독기 오라 자동 적용 (조건 ON)
  - 플레이그 전멸 시 해제 (조건 OFF)
  - 중첩 픽 N회: -3 × N /s 추가 누적
  - 수치 근거: 영웅 HP 1000 기준 -3/s = 전투 300초 동안 최대 900 피해(이론치). 플레이그 생존율 ≒ 50~70%라면 기대 총 피해 450~630. 몬스터 DPS에 더해 충분히 치명적이지만 단독으로 즉사시키는 수준은 아님 → §8 밸런싱 2~4분 사망 유지
  - HeroPoisonAura(기존 패시브: 영웅 발 밑 독장판 5DPS, 5s 한시, 이동 시 따라다님)와 독립 레이어 — 중첩 가능
- **구현 패턴**:
  - `IHeroAura` 또는 별도 `IPresenceAura<Plague>` 패턴
  - 조건 참 → `HeroStatusService.RegisterTickDamage(id: "PlagueMiasma", dps: 3)` 등록
  - 조건 거짓 → `HeroStatusService.RemoveTickDamage(id: "PlagueMiasma")` 해제
  - 기존 `HeroPoisonAuraEffect` 와 유사한 구조로 구현 비용 낮음
- **시너지 후크**:
  - SpawnPlagues(플레이그 출력 +1) → 플레이그 필드 생존율 증가 → PlagueMiasma 조건 유지율 상승
  - PlagueSlowBoost(Plague 슬로우 ×0.75) + PlagueMiasma(-3/s) = **플레이그 삼위일체** (영웅 둔화 + 독기 지속 데미지 + 영웅 이동 어려움 → 도주도 불가)
  - Fear(액티브: 영웅 3s 도주) → 도주 중에도 PlagueMiasma 틱 → Debuff 빌드의 연쇄 피해 극대화
  - 영웅 AI 딜레마: 플레이그를 먼저 죽이러 가면 Swarm/Tank 몬스터에 포위 → 플레이그를 냅두면 독기 누적 → 타게팅 선택의 핵심 압박
- **구현 비용 추정**: 2 (HeroPoisonAuraEffect 패턴 그대로 재사용. 조건부 등록/해제만 추가)
- **중복 재검증**: HeroPoisonAura(기존 28장)은 조건 없이 항상 발동하는 독장판. PlagueMiasma는 플레이그 생존을 전제로 한 조건부 흡혈 오라. plague-poison-chain(06-30)은 플레이그 슬로우+독 연쇄 콤보 카드였고, PlagueMiasma와는 메커니즘 차이가 있음. monster-vitality-resilience(06-22) 는 몬스터 자체 생존력 강화이고 PlagueMiasma는 플레이그 생존 조건부 영웅 디버프.

---

## 4. 공통 테마 고찰

**"선도자 오라 (Presence Aura)"** — 세 카드가 공유하는 구조:
> **[특정 종 A가 필드에 살아있는 동안] → [B 유닛 또는 영웅에게 버프/디버프 ON]**

**왜 지금 이 테마인가?**

1. **기존 25회차 공백 확인**: 지금까지 제안된 카드들은 ①빌드 픽 카운트 기반(cross-species-synergy), ②처치 시 발동(kill-echo), ③HP% 조건부(wounded-hero-punisher), ④주기 가속(spawner-cycle-rush) 등이었음. **"특정 몬스터의 필드 생존 여부를 실시간 조건으로 삼는"** 패턴은 25회 중 단 한 번도 없었음.

2. **영웅 AI와의 상호작용**: 영웅은 "가장 가까운 몬스터 자동 공격" AI를 가짐. 선도자 오라 카드가 있으면 영웅이 레이스를 먼저 잡아야 할지(→위스프 강화 해제) vs 위스프를 먼저 잡아야 할지(→레이스 생존 유지) 사이의 딜레마가 생김. 영웅의 단순 AI가 **자연스럽게 비효율 동선**을 만드는 설계.

3. **QA 데이터 부재 → 공백 추론**: 현재 QA 리포트가 blocked 상태(2026-05-22)라 픽률 데이터가 없음. 하지만 기존 카드 구조를 보면 "플레이그 활용 빌드"가 슬로우(PlagueSlowBoost)와 스폰 증가(SpawnPlagues)에 집중되어 있음. PlagueMiasma는 플레이그 생존 유지 자체를 전략 목표로 만들어 "플레이그 축 Debuff 빌드"에 새로운 최종 목표를 부여함.

4. **구현 비용 효율**: 세 카드 모두 기존 `IBattleContext`, `MonsterBuffService`, `HeroStatusService` 패턴 안에서 `GetFieldCount` API 1개 추가(또는 재사용)로 구현 가능. 신규 시스템 설계 없이 기존 인프라 활용 → 게임플레이 깊이 대비 구현 비용 낮음.

---

## 5. 채택 흐름 제안

- 채택 시 `/start-develop` 호출, 이 문서를 game-designer 입력으로 전달
  - 우선 수치 확정(WraithCommand ×1.25 / PhantomRipple ×1.2 임계치 / PlagueMiasma -3/s) game-designer 검토 필요
  - `IPresenceAura` 인터페이스 신설 여부 → gameplay-programmer 와 협의
- v0.2 진입 전까지 `docs/design/card-ideas/` backlog 보관
- 세 카드 중 PlagueMiasma 단독 채택도 가능 (구현 비용 2, Debuff 축 즉시 채움)

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 영웅은 자동으로 가장 가까운 몬스터를 공격합니다. 지금까지는 "어떤 몬스터를 강하게 키울까"가 주된 고민이었다면, 오늘 제안하는 카드들은 **"어떤 몬스터를 살아있게 유지하느냐"** 가 다른 몬스터의 강함을 결정하는 구조입니다. 예를 들어 레이스가 필드에 서 있는 동안 위스프가 더 두꺼워지는 카드가 있다면, 영웅은 "레이스를 먼저 잡자니 위스프가 공격받고, 위스프를 먼저 잡자니 레이스 때문에 HP가 깎이는" 딜레마에 빠지게 됩니다. 마치 보스 전에 부하를 먼저 죽여야 할지 보스를 직접 때려야 할지 고민하는 것처럼요. 그래서 오늘 제안하는 카드 3장은: 레이스가 살아있으면 위스프가 강해지는 **레이스의 통솔**, 팬텀 떼가 몰려있으면 모든 몬스터가 빨라지는 **팬텀 파문**, 플레이그가 살아있는 동안 영웅이 계속 독기를 마시는 **역병의 독기**입니다.
