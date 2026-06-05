# Card Ideas — 2026-06-06 — 필드를 꽉 채울수록 강해진다: 군단 밀도 압박 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 군단 밀도 압박 — 전장의 몬스터 총 마릿수(글로벌 캡 대비 현재 생존 수)를 실시간 트리거로 삼는 카드 3장. "캡 포화 = 전체 강화", "극소 = Tank 역전", "공석 절반 즉시 채우기"라는 서로 다른 밀도 조건을 탐색.
- **목록**: 밀집 군단 (DensityTide) / 최후의 항전 (LastStand) / 군단 집결 (RallyTheTroops)
- **기존 28장 + git log 과거 9회차와의 중복 회피 확인됨**
  - 기존 28장: 총 생존 몬스터 수를 트리거로 사용하는 카드 전무. SpawnerHaste(주기 단축), SpawnX 계열(출력 +1)은 수를 늘리는 수단이지, 수 자체를 조건으로 발동하지 않음.
  - 과거 9회차 전부:
    - 5/28 전장 상태 감지: **픽 시점 1회 스냅샷** → 영구 고정 스케일 (실시간 감시 X)
    - 5/29 종간 연계: 특정 **종 조합** 공존 조건. 총 수 무관
    - 5/30 ~ 6/02: Plague 사망/낙인/딜러 심화/OnDeath — 수(數) 임계 무관
    - 6/03 위스프 벽: 위스프 밀도 → **영웅 디버프**. 총 수가 아닌 특정 종 근접 밀도
    - 6/04 Dps×Debuff: 영웅 상태 조건. 몬스터 수 무관
    - 6/05 타이머 연동: BattleClock 값. 몬스터 수 무관
  - 오늘 3장: **실시간 총 생존 수 임계치(≥12 / ≤5 / 공석 기반)를 조건으로 ON/OFF 또는 비례 발동** — 어느 과거 회차와도 메커니즘 축이 다름 ✅

---

## 1. 밀집 군단 (DensityTide) — 가칭

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**:
  - 영구 조건부 패시브. 매 tick 현재 생존 몬스터 수가 **12마리 이상**(기본 글로벌 캡 18의 67%)이면 모든 몬스터 이동속도 **+20%** + 피해 **+15%**.
  - 조건 해제(≤11마리) → 즉시 무효. 조건 재충족(≥12마리) → 다음 tick에 즉시 재활성.
  - **Tank Tier3(캡 +6 → 24)와 자동 시너지**: 캡이 24로 확장되면 임계 12마리 = 캡의 50% — 달성 쉬워짐.
  - 수치 근거 (컨셉 §8):
    - Frenzy(공속 +50%, 10s, 전체): 단기 최강 버프. DensityTide는 이속·피해 동시이지만 조건부 영구.
    - 이속 +20%는 PhantomMoveSpeedBoost(팬텀 ×1.5) 대비 전종·조건부. 기대 가동률 60~70%이면 기대 DPS 증가 +15% × 0.65 ≈ **+10% 실질**.
    - 12마리 이상 유지는 SpawnPhantoms + SpawnWisps + SpawnerHaste 조합 빌드로 달성 가능 — Swarm 전략 보상.
- **구현 패턴**:
  ```
  DensityTideEffect.Apply(IBattleContext ctx):
      //# IBattleContext에 GetAliveMonsterCount() 신규 노출 필요 (~3줄)
      MonsterBuffService.Tick 콜백에 조건 추가:
          if ctx.GetAliveMonsterCount() >= 12:
              SetConditionalGlobalBuff(moveSpeed: 1.2f, power: 1.15f)  //# 매 tick base 리셋 → 재적용
          else:
              ClearConditionalGlobalBuff()
  ```
  WispLink.cs의 조건부 ON/OFF 패턴 완전 재사용. 종 필터 없이 전체 적용만 다름.
- **시너지 후크**:
  - **SpawnPhantoms + SpawnWisps**: 스폰 수 증가 → 12마리 유지 시간 증가 → 가동률 상승
  - **SpawnerHaste**: 빠른 재보충 → 영웅이 처치해도 12마리 임계 복귀 시간 단축
  - **Tank Tier3 시너지(캡 +6 → 24)**: 캡 확장으로 임계 달성 조건 완화
  - **RallyTheTroops(카드 3)**: 재집결로 12마리 즉시 복구 → DensityTide 즉시 재활성
- **구현 비용 추정**: 2 (IBattleContext.GetAliveMonsterCount() 1~3줄 추가 + WispLink 조건부 패턴 재사용. 신규 시스템 없음)
- **중복 재검증**:
  - 5/28(전장 상태 감지): 픽 시점 1회 스냅샷 → 고정 배율 영구. DensityTide는 실시간 임계 ON/OFF → 전략 의미가 "픽 타이밍 보상"이 아닌 "지속 유지 보상"으로 다름 ✅
  - 5/29(종간 연계): 특정 종 공존 조건(Tank AND Dealer 등). DensityTide는 종 무관 총 수 조건 ✅
  - 기존 28장: 총 마릿수 기반 글로벌 버프 없음 ✅

---

## 2. 최후의 항전 (LastStand) — 가칭

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - 영구 조건부 패시브. 매 tick 생존 몬스터 수가 **5마리 이하**일 때, 위스프와 레이스의 피해 **+50%** + 받는 데미지 **×0.75**.
  - 조건 해제(≥6마리) → 즉시 무효. 조건 충족(≤5마리) → 다음 tick에 즉시 재활성.
  - 컨셉: "소수 정예 역전 카드". 영웅이 많은 몬스터를 처치해 전장이 비어갈 때, 살아남은 Tank 몬스터들이 극도로 강해지는 역전 메커니즘. 패배 직전 상황이 오히려 위협적이 됨.
  - 수치 근거 (컨셉 §8):
    - 5마리 이하 상황: 전투 전체의 약 15~25% 구간 추산 (영웅이 DPS 50으로 지속 처치 중인 구간).
    - Wisp DPS 10 × 1.5 = 15, 실효 HP 200/0.75 = 267. Wraith DPS 20 × 1.5 = 30, 실효 HP 500/0.75 = 667.
    - 소수 상황 총 Tank DPS (예: Wisp 2 + Wraith 1): 15×2 + 30 = 60 vs 기본 30. 영웅 DPS 50 기준, 이 구간에서도 6초 내 영웅 100HP 손실 가능.
    - WraithDamageBoost(HP×1.5 무조건 영구) 대비: LastStand는 조건부이나 +50% 피해 + 방어 동시 → 조건 달성 시 더 강력.
- **구현 패턴**:
  ```
  LastStandEffect.Apply(IBattleContext ctx):
      MonsterBuffService.Tick 콜백에 조건 추가:
          if ctx.GetAliveMonsterCount() <= 5:
              SetConditionalBuff(new[]{EMonster.Wisp, EMonster.Wraith},
                  power: 1.5f, damageTaken: 0.75f)
          else:
              ClearConditionalBuff()
  ```
  DensityTide와 **GetAliveMonsterCount() API 공유** (동반 1회 추가로 두 카드 모두 구현). WispLink.cs 조건부 패턴 재사용.
- **시너지 후크**:
  - **WispHpBoost + LastStand**: Wisp HP ×1.5 + 받는 데미지 ×0.75 → 실효 HP ×1.5/0.75 = **×2.0**
  - **IronWill(15s, 모든 몬스터 받는 데미지 ×0.7)**: ≤5마리 상황에 IronWill 발동 시 중복 → Wisp 받는 데미지 ×0.7 × 0.75 = **×0.525**
  - **RallyTheTroops(카드 3)**: LastStand로 Tank가 강해진 타이밍에 RallyTheTroops로 재집결 → 12마리 달성 시 DensityTide도 활성 → **3카드 연계 콤보 루트**
- **구현 비용 추정**: 2 (DensityTide와 GetAliveMonsterCount() 공유, 조건 방향·대상 종만 다름. 신규 시스템 없음)
- **중복 재검증**:
  - 5/28(전장 상태 감지): 스냅샷 고정 배율. LastStand는 실시간 ≤5 임계 ✅
  - 6/03(위스프 벽): 위스프 마릿수/근접 거리 → 영웅 이속·공격력 감소. LastStand는 총 마릿수 → Tank 자체 강화(자기 버프), 영웅 디버프 없음 ✅
  - 기존 HeroAttackDown(영구 영웅 공격력 ×0.75): 조건 없는 영웅 디버프. LastStand는 조건부 Tank 강화 ✅

---

## 3. 군단 집결 (RallyTheTroops) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**:
  - 발동 즉시: floor((글로벌 캡 − 현재 생존 몬스터 수) ÷ 2)마리를 전체 스포너에서 균등 즉시 스폰.
  - 상황별 발동 효과:
    | 발동 시 현재 수 | 캡 18 기준 공석 | 즉시 스폰 수 | 발동 후 총수 |
    |---|---|---|---|
    | 4마리 (LastStand 활성 중) | 14 | 7 | 11 |
    | 8마리 (중간) | 10 | 5 | 13 |
    | 14마리 (거의 포화) | 4 | 2 | 16 |
    | 17마리 (포화 직전) | 1 | 0 | 17 |
  - 캡 확장 후(24) 동일 발동 효과 확대: 현재 4마리일 때 즉시 10마리 추가 → 총 14마리.
  - 컨셉: "군단 동원령". 빈 자리가 많을수록 폭발적. 영웅이 많은 몬스터를 쓸어담은 직후 발동 시 최대 효과 → "영웅이 강하게 몰아칠 때가 오히려 위협적" 역전 타이밍 카드.
  - 수치 근거 (컨셉 §8):
    - WallOfWisps(위스프 4마리 즉시): 고정 4마리, 위스프 종. RallyTheTroops는 전 종, 상황 비례.
    - 최강 발동(현재 0마리, 캡 18): 9마리 즉시 → 평균 DPS 17 × 9 = 153. 영웅 1000HP 기준 ~6.5초 분.
    - 6/05 WaveRelease(전 스포너 즉시 발사 + 타이머 리셋): 타이머 연동, 공석 무관, 스포너 1마리씩. RallyTheTroops: 공석 기반, 타이머 영향 없음, 균등 분배 → 효과 범위와 메커니즘이 다름.
- **구현 패턴**:
  ```csharp
  RallyTheTroopsEffect.Apply(IBattleContext ctx):
      int count   = ctx.GetAliveMonsterCount()  //# DensityTide/LastStand와 공유
      int cap     = ctx.GetGlobalCap()           //# IBattleContext 신규 노출 (~2줄)
      int spawn   = Mathf.FloorToInt((cap - count) / 2f)
      //# 각 스포너에 균등 배분 (나머지는 인덱스 순 배분)
      IReadOnlyList<ISpawner> spawners = ctx.GetAllSpawners()  //# WaveRelease 제안 API 재사용
      int perSpawner = spawn / spawners.Count
      int remainder  = spawn % spawners.Count
      for (int i = 0; i < spawners.Count; i++):
          int extra = (i < remainder) ? 1 : 0
          for (int j = 0; j < perSpawner + extra; j++):
              spawners[i].ForceSpawnNow()  //# WaveRelease 제안 API 재사용
  ```
  IBattleContext.GetGlobalCap() 신규 노출 (약 2줄). GetAliveMonsterCount() / GetAllSpawners() / ForceSpawnNow()는 DensityTide·LastStand·WaveRelease(6/05 제안) 구현과 공유 가능.
- **시너지 후크**:
  - **LastStand(카드 2) → RallyTheTroops → DensityTide(카드 1)**: ≤5마리 LastStand 활성 → 전투 버팀 → RallyTheTroops(최대 7마리 즉시) → ≥12마리 DensityTide 활성. 세 카드를 한 턴에 클리어하는 "3단계 역전" 콤보.
  - **SpawnerHaste + RallyTheTroops**: 빠른 자연 보충 + 즉시 재집결 → 글로벌 캡 거의 상시 유지
  - **Swarm 시너지 Tier3(모든 스포너 출력 +1)**: ForceSpawnNow 호출 시 Tier3 적용 중이면 2마리씩 → 즉시 스폰 효과 2배
- **구현 비용 추정**: 2~3 (IBattleContext.GetGlobalCap() 신규, GetAliveMonsterCount() 공유, GetAllSpawners() + ForceSpawnNow()를 6/05 WaveRelease 제안과 공유하면 2, 없으면 추가 구현 포함 3)
- **중복 재검증**:
  - WaveRelease(6/05 루틴): 전 스포너 즉시 1회 발사 + 타이머 리셋. 공석과 무관. RallyTheTroops는 공석 절반 비례 스폰, 타이머 미영향 ✅
  - WallOfWisps(기존): 위스프 종 고정 4마리. 영웅 4방위 고정 위치. RallyTheTroops는 전 종, 링 위치 정상 스폰 ✅
  - SpawnX 계열(기존 패시브): 스포너 출력 영구 +1. RallyTheTroops는 일회성 즉시 스폰 ✅

---

## 4. 공통 테마 고찰

세 카드는 **"현재 필드 생존 몬스터 총 수(N)"를 카드 효과의 실시간 입력(조건/스케일)으로 사용**이라는 동일 메커니즘 축을 공유한다:

| 카드 | 임계 방향 | 발동 조건 | 전략적 의미 |
|---|---|---|---|
| DensityTide | 포화 (≥12) | 수가 충분할 때 강해짐 | 많은 몬스터 유지 → Swarm 전략 보상 |
| LastStand | 고갈 (≤5) | 수가 줄어들 때 강해짐 | 최후 Tank들의 역전 → 위기 반전 |
| RallyTheTroops | 공석 비례 | 수가 적을수록 더 강한 즉시 스폰 | 고갈→포화 전환 → 3카드 연계 완성 |

세 카드를 모두 채택하면 다음 게임플레이 리듬이 생긴다:
> 필드 포화(DensityTide 활성) → 영웅이 몬스터를 처치해 수 감소 → LastStand 활성 → Tank로 버팀 → RallyTheTroops 발동으로 재포화 → DensityTide 재활성 → 반복

이는 현재 28장에 없는 **"밀물과 썰물"** 페이싱 패턴으로, 플레이어가 몬스터 수를 의식적으로 관리하는 새로운 전략 레이어를 추가한다.

**왜 오늘 이 테마인가:**
- QA 리포트(2026-05-22.md)가 BLOCKED 상태 → 구조 분석으로 대체. 현재 28장은 스폰 수를 늘리는 수단(SpawnX, SpawnerHaste)과 각 종의 능력치를 강화하는 수단은 풍부하지만, "얼마나 많은 몬스터가 살아있느냐"에 반응하는 카드는 전무.
- 플레이어가 SpawnPhantoms + SpawnWisps를 픽한 후, 실제 필드에 많은 몬스터가 살아있을 때와 영웅이 쓸어버린 직후 빈 필드일 때 **체감 강도 차이가 없다**. 밀도를 유지하는 행위 자체에 의미를 부여하는 카드가 없기 때문.
- 과거 9회차 어느 회차도 총 마릿수 실시간 임계를 트리거로 사용하는 아이디어 없음 ✅

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **IBattleContext 공유 API 일괄 추가 제안**: GetAliveMonsterCount() + GetGlobalCap()은 세 카드 모두 사용. 구현 시 한 번에 추가하면 비용 절감.
- **구현 우선순위 제안**:
  1. DensityTide(비용 2) + LastStand(비용 2): 동일 API 공유, 한 PR에 두 카드 같이. 가장 작은 변경으로 "밀도 기반 게임플레이" 레이어 검증.
  2. RallyTheTroops(비용 2~3): WaveRelease(6/05 제안) ForceSpawnNow API 먼저 구현 후 이 카드를 추가하면 비용 2. 6/05와 6/06 제안을 묶어서 처리 추천.
- 세 카드 채택 시 축 분포: DensityTide(Swarm) + LastStand(Tank) + RallyTheTroops(Wild) → v0.2 Pool에서 Swarm·Tank 축 패시브·액티브 균형 보완.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 몬스터들을 많이 보충해도, 영웅이 빠르게 처치하고 나면 그냥 텅 비어버립니다. "많은 몬스터가 지금 살아있다"는 사실 자체에는 아무런 보너스가 없거든요. 오늘 제안하는 카드 3장은 "현재 전장에 몬스터가 얼마나 있느냐"를 보고 반응합니다. 마치 축구처럼, 선수가 많을 때는 팀 전체가 더 강해지고, 선수들이 다 빠져나가면 마지막 남은 선수들이 필사적으로 버티고, 그 틈에 팀을 다시 가득 채우는 전략이에요. 비유하자면 "썰물처럼 몬스터가 줄어들다가, 파도처럼 한꺼번에 밀려오는" 리듬을 만드는 카드들입니다. 그래서 오늘 제안하는 카드 3장은: 몬스터가 12마리 이상 살아있을 때 모두 더 빨라지고 강해지는 '밀집 군단(DensityTide)', 반대로 몬스터가 5마리 이하로 줄어들 때 남은 위스프·레이스가 거꾸로 더 강해지는 '최후의 항전(LastStand)', 그리고 빈 자리만큼 즉시 몬스터를 채워 넣는 '군단 집결(RallyTheTroops)'입니다.
