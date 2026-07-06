# Card Ideas — 2026-07-07 — 한 종 집중 투자 보상 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 단일 종 집중 포화 보상 — 특정 종 몬스터를 필드에 N마리 이상 동시 생존시키면 그 종이 더 강해지는 "집중 투자 달성 보상" 카드 3장. 총 몬스터 수가 아닌 **종별 생존 수**를 독립 조건으로 사용하는 점이 핵심.
- **목록**: 위스프 대홍수 (WispFlood) / 역병 나선 (InfestationSpiral) / 유령의 파도 (GhostTide)
- **기존 28장 + git log 과거 21회차와의 중복 회피 확인됨**
  - 기존 28장: 종별 글로벌 생존 수를 조건으로 발화하는 카드 전무. SpawnX 계열(스포너 출력+1)은 수를 늘리는 수단이지 수 자체를 조건으로 삼지 않음.
  - 과거 21회차 핵심 비교:
    - 2026-06-06 DensityTide: **총 전체 수** ≥12 → 전종 이속+데미지 버프. 오늘: **특정 종만** ≥N → 해당 종 전용 능력치 강화. 메커니즘 축이 다름 ✅
    - 2026-06-03 WispContactSlow: 영웅 **반경 0.8u 이내** 위스프 수 → 영웅 이속 디버프. 오늘 WispFlood: **필드 전체** 위스프 수 → 위스프 HP 자기 버프. 조건 범위·효과 방향 다름 ✅
    - 2026-06-19 ReaperOverflow·WraithTide·PlagueSpread: 특정 종 스포너 **주기 가속**. 오늘: 특정 종 필드 생존 수 달성 시 **능력치 강화**. 다름 ✅
    - 나머지 회차: 영웅 HP 조건, 시간 조건, 몬스터 HP 임계, 피격 횟수, 공간/거리, 스포너 간 연쇄 등 — 종별 생존 수 조건 없음 ✅

---

## 1. 위스프 대홍수 (WispFlood) — 가칭

- **카테고리**: 패시브 강화 (Tank 축, 위스프 특화)
- **효과 모델**:
  - 필드 전체에 살아있는 위스프가 **6마리 이상** 동시 존재하는 순간 1회 발화: 위스프 전체 HP **+200** 글로벌 영구 적용.
  - 이후 위스프가 6마리 미만으로 줄어도 버프 유지 (1회 발화 고정 — DensityTide의 실시간 ON/OFF와 구별).
  - 수치 근거 (컨셉 §8):
    - 위스프 기본 HP 200. +200 적용 시 HP 400 (×2.0). WispHpBoost(무조건 ×1.5=HP 300)보다 결과는 강하지만 "6마리 동시 유지" 조건 달성 후 발화.
    - WispFlood 단독: HP 400. WispHpBoost + WispFlood: HP 200×1.5+200 = 500. GuardianRage 추가 시 HP ×2.0 = 800~1000 수준 — Tank Tier3 시너지(캡 +6 → 24)로 6마리 조건 완화.
    - SpawnWisps(출력+1) + SpawnerHaste(주기×0.8 영구): 2카드 조합으로 필드 위스프 6마리 도달 속도 크게 단축. 조건 달성 후에는 Tank 전략의 보상.
- **구현 패턴**: IBattleContext에서 `GetAliveMonsterCount(EMonster.Wisp)` (종 필터 추가, DensityTide 제안 API의 오버로드) 구독 또는 패시브 틱 폴링 → 6 이상 시 WispHpBoostEffect 유사 패턴으로 MonsterBuffService 등록. 1회 발화 플래그로 중복 방지.
- **시너지 후크**:
  - **SpawnWisps + SpawnerHaste**: 위스프 공급 증가 + 주기 단축 → 조건 달성 속도 증가
  - **WispHpBoost**: 중첩 시 최대 HP 500 (×1.5 + +200 순서 의존, gameplay-programmer 확인 필요)
  - **GuardianRage(A, Tank)**: WispFlood 발화 후 GuardianRage 발동 시 HP ×2.0 → 순간 HP 800~1000
  - **Tank Tier3(캡 +6 → 24)**: 필드 캡 확장 → 6마리 유지 자체가 더 쉬워짐
- **구현 비용 추정**: 2 (IBattleContext.GetAliveMonsterCount(EMonster) 오버로드 추가 ~3줄 + WispHpBoostEffect 패턴 재사용 + 1회 발화 플래그)
- **중복 재검증**: WispHpBoost는 무조건 무한 적용, WispFlood는 "필드 6마리 동시 유지" 1회 발화. DensityTide(총 수 전종 이속·데미지)·WispContactSlow(영웅 반경 내 위스프 수)와 조건 범위·효과 방향 모두 다름 ✅

---

## 2. 역병 나선 (InfestationSpiral) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축, 플레이그 특화)
- **효과 모델**:
  - 필드 전체에 살아있는 플레이그가 **4마리 이상** 동시 존재하는 순간 1회 발화: 플레이그 SlowFactor 추가 **×0.9** 글로벌 영구 적용.
  - 발화 조건 달성 이후 플레이그 수 감소해도 버프 유지.
  - 곱연산 예시:
    | 보유 카드 | 영웅 둔화 배수 | 영웅 이동속도 |
    |---|---|---|
    | 기본(플레이그 공격) | ×0.80 | -20% |
    | + PlagueSlowBoost | ×0.80 × 0.75 = ×0.60 | -40% |
    | + InfestationSpiral | ×0.80 × 0.75 × 0.90 = ×0.54 | -46% |
    | + Debuff Tier1(×0.80) | ×0.80 × 0.75 × 0.90 × 0.80 = ×0.43 | -57% |
  - 수치 근거 (컨셉 §8): 영웅 이동속도 -46%(InfestationSpiral 발화 후)는 SlowEffect(×0.5 = -50%)에 근접하지만, 이 수치는 플레이그 4마리 유지 + PlagueSlowBoost 픽 두 조건이 모두 충족되어야 달성됨. 조건 없이는 -40%까지만.
- **구현 패턴**: IBattleContext.GetAliveMonsterCount(EMonster.Plague) ≥ 4 확인 → PlagueSlowBoostEffect 유사 패턴으로 SlowFactor에 ×0.9 곱연산 MonsterBuffService 등록. WispFlood와 API 공유 (종 필터 오버로드 재사용).
- **시너지 후크**:
  - **PlagueSlowBoost**: 핵심 선행 카드. 이 둘이 같이 있을 때 비로소 -40% → -46% 달성
  - **SpawnPlagues**: 플레이그 스포너 출력+1 → 4마리 조건 빠르게 달성
  - **Debuff Tier1(SlowFactor×0.8 추가)**: 3장 픽 시 자동 발화 → InfestationSpiral와 곱산하면 -57%까지 확장
  - **Bleed(A) / Fear(A) / Weaken(A)**: 느려진 영웅에게 추가 저주 적중률 간접 상승 (영웅이 덜 피하게 됨)
- **구현 비용 추정**: 2 (PlagueSlowBoostEffect 재사용 + GetAliveMonsterCount(EMonster.Plague) WispFlood와 API 공유)
- **중복 재검증**: PlagueSlowBoost는 무조건 적용, InfestationSpiral은 "플레이그 4마리 유지" 조건 달성 1회 발화. DensityTide(총 수 조건)·WispContactSlow(위스프 근접 조건)와 종류·효과 완전히 다름 ✅

---

## 3. 유령의 파도 (GhostTide) — 가칭

- **카테고리**: 액티브 버프 (Swarm 축, 팬텀 특화)
- **효과 모델**:
  - 발동 시점 필드 전체 팬텀 생존 수를 읽어 **팬텀 수 × 1.5초 (최대 12초)** 동안 영웅 이동속도 ×0.5.
  - 팬텀 수별 지속 시간:
    | 팬텀 수 | 지속 시간 | 비교 |
    |---|---|---|
    | 0마리 | 0초 (무효화) | |
    | 2마리 | 3초 | |
    | 4마리 | 6초 | TimeStop (5s 정지)보다 약함 |
    | 6마리 | 9초 | |
    | 8마리 이상 | 12초 (캡) | 기존 Slow (10s)보다 길음 |
  - 수치 근거 (컨셉 §8): 기존 Slow(A) = 고정 10s, 이속×0.5. GhostTide는 팬텀 8마리 이상일 때만 12초로 기존보다 강함. 팬텀 4마리 이하면 기존 Slow보다 약하므로 팬텀 집중 투자가 없으면 불리한 교환.
- **구현 패턴**: SlowEffect 재사용. 발동 시 IBattleContext.GetAliveMonsterCount(EMonster.Phantom) 조회 → duration = Mathf.Clamp(count × 1.5f, 0f, 12f). WispFlood·InfestationSpiral과 API 공유.
- **시너지 후크**:
  - **SpawnPhantoms + PhantomMoveSpeedBoost**: 팬텀 수 증가 → GhostTide 지속 시간 증가 + 빠른 팬텀이 슬로우된 영웅을 더 효과적으로 포위
  - **Swarm Tier1(팬텀·위스프 이속 ×1.3)**: 팬텀 수 유지 × 이속 강화 → GhostTide가 길수록 더 많은 팬텀이 영웅에 도달
  - **TimeStop(A, Swarm)와 교대**: TimeStop(5s 완전 정지) → GhostTide(12s 이속 반감) 연속 발동 시 최대 17s 봉쇄
  - **Fear(A, Debuff)와 조합**: GhostTide로 먼저 느리게 한 뒤 Fear(3s 도주)로 방향 강제 → 팬텀 포위 진형으로 몰아넣기
- **구현 비용 추정**: 2 (SlowEffect 재사용 + 발동 시 GetAliveMonsterCount(Phantom) 조회로 duration 계산. 신규 시스템 없음)
- **중복 재검증**: 기존 Slow는 고정 10초, GhostTide는 팬텀 수 비례 가변 지속(0~12초). TeamStop은 완전 정지. 팬텀 수가 적으면 오히려 불리해지는 리스크·리워드 구조 — 과거 21회차 어디에도 팬텀 수 비례 가변 지속 시간 없음 ✅

---

## 4. 공통 테마 고찰

세 카드는 **"특정 종 몬스터의 필드 전체 생존 수(N)"를 조건으로 삼아 해당 종의 전략 투자에 보상을 제공**하는 동일 메커니즘 축을 공유한다:

| 카드 | 대상 종 | 조건 | 발화 방식 | 효과 방향 |
|---|---|---|---|---|
| WispFlood | 위스프 | 6마리 이상 | 1회 발화 | 위스프 자기 HP 강화 |
| InfestationSpiral | 플레이그 | 4마리 이상 | 1회 발화 | 플레이그 둔화 심화 |
| GhostTide | 팬텀 | 발동 시 실시간 조회 | 발동 즉시 | 영웅 이속 감소 (비례 지속) |

**왜 오늘 이 테마인가:**
- QA 리포트(2026-05-22.md)가 BLOCKED 상태라 픽률 데이터 없음. 하지만 카드 리스트 분석으로 공백 확인:
  - 위스프: WispHpBoost(무조건 강화), SpawnWisps(수 증가) → "위스프를 많이 유지할수록 더 강해진다"는 보상 없음. WispFlood가 채움.
  - 플레이그: PlagueSlowBoost(무조건 강화), SpawnPlagues(수 증가) → "플레이그 4마리 이상 유지 빌드"에 대한 보상 없음. InfestationSpiral이 채움.
  - 팬텀: PhantomMoveSpeedBoost(무조건 강화), SpawnPhantoms(수 증가) → 팬텀 다수를 유지했을 때 액티브 보상 없음. GhostTide가 채움.
- 과거 06-06 DensityTide는 **총 수** 임계 보상. 오늘 카드들은 **종별 수** 임계 보상 → 집중 투자(하나의 종을 전문화) vs 분산 투자(전체 물량 유지)라는 서로 다른 전략 레이어를 제공해 둘이 경쟁하지 않고 보완 관계 형성.
- 세 카드 모두 IBattleContext.GetAliveMonsterCount(EMonster) 오버로드 1개 추가로 구현 가능 → 추가 API 비용 최소화.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **구현 일괄 제안**: 세 카드가 GetAliveMonsterCount(EMonster) 오버로드를 공유하므로, 구현 시 한 PR에 묶어 처리하면 API 추가 비용을 1회로 압축 가능.
- **우선순위 제안**:
  1. GhostTide(비용 2, SlowEffect 재사용): 기존 Slow의 "팬텀 투자 버전"이라 코드 최소 변경.
  2. WispFlood + InfestationSpiral(비용 2씩, API 공유): GetAliveMonsterCount 오버로드 1회 추가로 두 카드 동시 구현.
- **축 분포**: WispFlood(Tank P) + InfestationSpiral(Debuff P) + GhostTide(Swarm A) → v0.2 Pool에서 Tank/Debuff/Swarm 각 1장씩 추가, 세 축 균형 보완.

---

## 6. 쉬운 설명 (비개발자 요약)

게임에서 위스프를 많이 소환하든, 플레이그를 잔뜩 풀어놓든, 팬텀을 수십 마리 기르든, 지금까지는 단순히 "수가 많으면 더 귀찮은 정도"였습니다. 특정 몬스터를 열심히 키웠는데 "그만큼 더 강해진다"는 보상이 없던 거예요. 오늘 제안하는 카드 3장은 "한 종류를 집중해서 키우면 그 종이 더 강해진다"는 전문화 보상입니다. 마치 한 포지션 선수를 전문 훈련시키면 그 포지션이 월등히 강해지는 것처럼, 위스프 6마리를 유지하면 위스프 전체 체력이 크게 오르고, 플레이그 4마리 이상이 살아있으면 적이 훨씬 느리게 기어다니고, 팬텀이 많을수록 적을 더 오래 느리게 만들 수 있습니다. 그래서 오늘 제안하는 카드 3장은: 위스프 6마리를 필드에 채우면 위스프 전체 체력이 2배가 되는 '위스프 대홍수(WispFlood)', 플레이그 4마리 이상을 유지하면 적의 이동 둔화가 한층 더 깊어지는 '역병 나선(InfestationSpiral)', 그리고 팬텀이 많을수록 영웅을 최대 12초 느리게 만드는 '유령의 파도(GhostTide)'입니다.
