# Card Ideas — 2026-06-26 — 이벤트 연동 즉발 스폰: 게임 순간에 반응하는 폭발 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 이벤트 연동 즉발 스폰 (Event-Burst Spawn) — 기존 스폰 카드들은 "영구 출력 +1" 또는 "스폰 주기 단축"으로 지속적 물량을 늘린다. 이번 3장은 그와 달리 특정 **게임 이벤트가 발생하는 순간** 스포너에서 즉시 1회 추가 스폰을 발동시킨다. 이벤트 소스는 각각 ① 카드 발동(액티브 즉발), ② 패시브 트리거 발화(영웅 HP 경계 통과), ③ 액티브 카드 픽 완료. 기존 스폰 패턴과 직교하는 메커니즘.
- **목록**: 조류 해방 (TidalForce) / HP 급락 보강 (HpSurgeSpawn) / 액티브 공명 (ActiveEcho)
- **기존 28장 + git log 조회 과거 16회차 (2026-06-08 ~ 2026-06-25) + 폴더 내 이전 파일 (2026-05-28 ~ 2026-06-07) 전부와의 중복 회피 확인됨**
  - **기존 28장**: SpawnX 계열(특정 종 스포너 +1 영구), SpawnerHaste(전체 주기 ×0.8), Multiply(Phantom 주기 ×0.6), WallOfWisps(Wisp 4마리 즉시 소환)는 모두 "지속 생산 라인 변경"임. "이벤트 발생 시 한 번 즉발"은 없음 ✅
  - **2026-06-13 전술 배치 (SwarmRush·PlagueCloud·ReaperStrike)**: 각 종을 즉시 단수 소환하는 "단일 종 즉발 소환" 계열. TidalForce는 "전체 스포너 동시"이며 HpSurgeSpawn·ActiveEcho는 "게임 이벤트 트리거" — 메커니즘 이중으로 다름 ✅
  - **2026-06-05 시간 급등 (CurseOfTime)**: 30초마다 이속 누적 → "타이머 주기 누적 스케일" 이고 스폰이 아님 ✅
  - **2026-06-08 도주 처벌 / 2026-06-09 킬 메아리**: 영웅 행동(도주·킬) 조건부 강화. 오늘 카드는 "덱 이벤트(패시브 트리거, 액티브 픽)"에 연동 — 트리거 소스가 다름 ✅
  - **2026-06-06 밀도 파도 압박**: 파일 읽지 않았으나 slug("density-tide-pressure") 상 밀도/수량 기반 압박 계열. 오늘 카드는 현재 필드 밀도와 무관하게 이벤트 시 발동 — 조건 소스 다름 ✅

---

## 1. 조류 해방 (TidalForce) — 가칭

- **카테고리**: 액티브 버프 (Swarm 축)
- **효과 모델**:
  - 즉발. 이 카드를 픽하는 순간, **현재 활성 상태인 모든 스포너(최대 6개)에서 동시에 1회 추가 스폰** 발동.
  - 스폰은 각 스포너의 현재 동시 출력(base 1 + SpawnX 카드 픽 횟수)만큼 즉시 방출.
  - 예: Wraith 스포너 출력 2 + Phantom 스포너 출력 3 + 나머지 4개 스포너 출력 1씩 = 9마리 즉시 추가.
  - **수치 근거** (컨셉 §8 2~4분 사망 기준):
    - 기본 구성: 스포너 6개 × 출력 1 = 즉시 6마리 보충. 필드 캡(18)에 여유가 있을 때 최대 효과.
    - SpawnX 카드를 2장 픽했다면(출력 합계 +2) 즉시 8마리 방출 가능.
    - WallOfWisps 비교: Wisp 4마리(단일 종) vs TidalForce: 모든 종 비율대로 최대 6~10마리.
    - 30초 타이밍에 픽하면 영웅이 포위망을 뚫으려는 순간에 갑작스러운 머릿수 증원 → 탈출 차단 효과.
  - **지속 효과 없음** — 즉발 후 스폰 주기는 원래대로 재개.
- **구현 패턴**:
  ```
  TidalForceEffect.Apply →
    IBattleContext.ForceAllSpawnersEmit()
    //# 각 Spawner의 현재 simultaneousOutput 만큼 즉시 스폰
    //# SpawnerService 또는 SpawnerController에 EmitNow() 메서드 1개 추가
  ```
  - 기존 스포너 구조(`continuous-spawn-round.md` §3 SpawnerController)에 `EmitNow(int count)` 1개 추가로 구현.
  - WallOfWisps의 "Wisp 4마리 즉시 소환" 패턴을 전체 스포너로 일반화한 구조.
- **시너지 후크**:
  - SpawnPhantoms + SpawnWisps + **TidalForce**: 팬텀과 위스프 출력이 올라간 상태에서 TidalForce = 즉시 대군 방출. Swarm 빌드의 전술 폭발기.
  - SpawnerHaste + **TidalForce**: SpawnerHaste로 평소 스폰 주기가 빠른데, TidalForce로 즉발 폭발 추가 → "흐르는 물에 순간 파도".
  - BloodThirst(처치 시 HP 회복)와 조합: TidalForce로 즉시 많은 몬스터가 들어오면 처치 기회가 많아지고, 처치된 몬스터만큼 주변 몬스터 회복 → 적을 많이 부를수록 남은 몬스터가 단단해짐.
- **구현 비용 추정**: 2 (SpawnerController에 EmitNow() 메서드 추가 + TidalForceEffect에서 호출. WallOfWisps 패턴의 전체화)
- **중복 재검증**: 기존 28장 + 16회차 파일명·git log 전부 검토. "전체 스포너 동시 즉발 방출" 개념은 어디에도 없음. 6/13의 SwarmRush·PlagueCloud·ReaperStrike는 단일 종 즉발 소환. 완전 신규 ✅

---

## 2. HP 급락 보강 (HpSurgeSpawn) — 가칭

- **카테고리**: 패시브 추가 (Tank 축)
- **효과 모델**:
  - 픽 후부터, **영웅 HP 10% 경계가 통과될 때마다** (패시브 트리거와 동일 타이밍) Wisp·Wraith 스포너에서 각각 즉시 1마리씩 추가 스폰 (Wisp 1 + Wraith 1 = 총 2마리, 캡 여유 내에서).
  - 선택창 팝업이 뜨기 직전(TryProcessNext 내 카드 Draw 직후)에 자동으로 발동.
  - **총 발동 횟수**: 최대 9번(HP 90%~10% 9단계) → 최대 18마리(Wisp 9 + Wraith 9) 추가.
  - **수치 근거**:
    - 영웅이 HP 10% 경계를 빠르게 잃는 고DPS 순간(예: Frenzy나 MarkOfDeath 중첩)에 자동으로 Tank 보충 → "공세가 강해질수록 방어선이 자동 보강"되는 반응적 설계.
    - 위스프(HP 200, DPS 10)와 레이스(HP 500, DPS 20) 각 1마리 = 즉각적인 진로 방해 강화.
    - 평균 15~17초마다 1회 패시브 트리거 → HpSurgeSpawn 1회 발동 = 약 15~17초마다 Tank 2마리 보충. 자연 스폰 주기와 독립적인 "비상 보충" 역할.
  - **비교**: SpawnWraith(레이스 스포너 영구 +1)는 스폰 주기마다 계속 추가. HpSurgeSpawn은 HP 경계에서만 1회씩 → 런 초반 영웅이 빠르게 깎이면 더 빈번하게 발동.
- **구현 패턴**:
  ```
  HpSurgeSpawnEffect.Apply →
    IBattleContext.OnPassiveTriggerFired += OnHpThresholdCrossed

  OnHpThresholdCrossed():
    ctx.GetSpawner(EMonster.Wisp)?.EmitNow(1)
    ctx.GetSpawner(EMonster.Wraith)?.EmitNow(1)
  ```
  - `OnPassiveTriggerFired` 이벤트는 `PassiveTriggerService`의 HP% 경계 통과 시점에 추가. TidalForce와 공유 가능한 `EmitNow()` 패턴.
  - 카드 선택 큐(`TryProcessNext`) 흐름을 건드리지 않음 — 이벤트 구독이므로 패시브 팝업 타이밍에 영향 없음.
- **시너지 후크**:
  - WispHpBoost + **HpSurgeSpawn**: 스폰될 때마다 HP가 이미 강화된 위스프가 추가 투입 → 위스프 물량 극대화.
  - ReplaceWispsToWraith + **HpSurgeSpawn**: 위스프 스포너가 레이스로 교체되어 있다면 HpSurgeSpawn의 Wisp 부분이 실질적으로 추가 레이스로 작동 (스포너 출력 종 적용 후 스폰).
  - GuardianRage(Wisp·Wraith HP×2 + 받는데미지×0.5, 15s)와 연계: GuardianRage 활성 중에 HP 경계를 지나면 그 강화된 레이스/위스프가 즉시 2마리 더 추가 → 무적 수비대 폭발.
- **구현 비용 추정**: 3 (PassiveTriggerService에 OnPassiveTriggerFired 이벤트 추가 + HpSurgeSpawnEffect 구독. TidalForce의 EmitNow() 패턴 재사용)
- **중복 재검증**: 기존 28장에 "패시브 트리거 타이밍에 연동된 스폰" 없음. 과거 16회차: 6/18 "spawner-cycle-rush-trio" (ReaperOverflow·WraithTide·PlagueSpread — 종별 스포너 주기 가속)는 주기 단축, HP 이벤트 연동 아님. 6/23 "presence-aura-leader-trio"는 필드 생존 조건부 연동이며 스폰이 아님. 완전 신규 ✅

---

## 3. 액티브 공명 (ActiveEcho) — 가칭

- **카테고리**: 패시브 환경 (Debuff 축)
- **효과 모델**:
  - 픽 후부터, **30초 액티브 카드가 선택될 때마다** Plague 스포너에서 즉시 1마리 추가 스폰.
  - 트리거 조건: `ActiveTriggerService`의 카드 픽 완료(OnActivePicked) 이벤트 — 어떤 종류의 액티브 카드를 골라도 공명 발동.
  - **총 발동 횟수**: 최대 9번(액티브 9회) → 최대 9마리 추가 Plague.
  - **수치 근거**:
    - Plague(HP 50, DPS 5, 둔화 적용)가 액티브 카드 픽마다 1마리씩 추가 투입.
    - SpawnPlagues(스포너 +1)와 비교: SpawnPlagues는 매 스폰 주기마다 추가(지속 물량 증가), ActiveEcho는 액티브 픽 타이밍에만 발동(이벤트 타이밍 집중).
    - 액티브 카드 픽 시점 = 게임 흐름상 30초 단위 전략 결정 순간. 이때 Plague가 1마리 자동 추가 → 전략 실행 직후 독/둔화 압박 강화.
    - 9번 발동 기준 Plague 9마리 누적 = SpawnPlagues 약 2~3회 픽 분량을 보조 없이 확보.
    - Bleed(이동 시 HP -2%, 10s) + PlagueSlowBoost + **ActiveEcho**: 액티브로 Bleed를 픽하는 순간 Plague 1마리 추가 → 독+출혈+둔화 삼중 Debuff 동시 개막.
  - 이 카드를 픽하는 것 자체도 "액티브 픽"에 해당하나, Apply 완료 후 이벤트 구독이 등록되므로 픽 자신은 발동 안 함(apply-then-subscribe 순서).
- **구현 패턴**:
  ```
  ActiveEchoEffect.Apply →
    IBattleContext.OnActivePicked += OnActiveCardPicked

  OnActiveCardPicked(CardData _):
    ctx.GetSpawner(EMonster.Plague)?.EmitNow(1)
  ```
  - `OnActivePicked` 이벤트는 `ActiveTriggerService`의 카드 픽 완료 시점에 추가 (HpSurgeSpawn의 PassiveTrigger 패턴을 Active로 미러링).
  - `EmitNow()` 패턴 동일 — TidalForce, HpSurgeSpawn과 3개 카드가 같은 인프라 공유.
- **시너지 후크**:
  - PlagueSlowBoost + SpawnPlagues + **ActiveEcho**: Debuff 축 Tier1 달성 시 Plague SlowFactor ×0.75 적용 + 추가 스폰 2개 소스(SpawnPlagues 주기·ActiveEcho 이벤트) → Plague가 빠르게 축적되며 강한 둔화 압박.
  - Fear(영웅 3s 도주) + **ActiveEcho**: Fear를 픽하면 Plague 1마리 즉시 추가 → 도주하는 영웅 경로에 Plague가 갑작스럽게 나타나 독/둔화 재적용.
  - Bleed(이동 시 HP -2%, 10s) + **ActiveEcho**: Bleed를 픽하면 Plague 1마리 + 이동 시 독/출혈 이중 피해 시작 → 둘 다 "이동할수록 손해" 콤보.
- **구현 비용 추정**: 3 (ActiveTriggerService에 OnActivePicked 이벤트 추가 + ActiveEchoEffect 구독. HpSurgeSpawn과 대칭 구현. EmitNow 공유)
- **중복 재검증**: 기존 28장에 "액티브 픽 타이밍에 연동된 스폰" 없음. 6/25 "curse-companion-trio" (FearCharge·WoundedFrenzy·WeakenedPrey)는 저주 상태 지속 중 효과이지 픽 타이밍 트리거가 아님. 6/05 "time-surge-trio"는 타이머 주기 기반. 완전 신규 ✅

---

## 4. 공통 테마 고찰

세 카드는 모두 "기존 스폰 주기와 독립된 이벤트 버스트 스폰"이라는 메커니즘으로 묶인다.

**기존 스폰 공간 구조:**
```
지속 스폰 라인 →  SpawnX(출력+1), SpawnerHaste(주기×0.8), Multiply(Phantom주기×0.6)
즉발 소환    →  WallOfWisps(Wisp 4마리), 6/13 SwarmRush·PlagueCloud·ReaperStrike (각 단일 종)
```

**오늘 3장이 채우는 공간:**
```
이벤트 버스트 스폰 → TidalForce(액티브 발동 시 전체 스포너 동시), HpSurgeSpawn(HP% 경계 시 Tank 2마리), ActiveEcho(액티브 픽 시 Plague 1마리)
```

**왜 지금 이 테마인가?**
QA 리포트(2026-05-22)는 시뮬레이션 미실행(BLOCKED) 상태라 픽률 데이터가 없다. 하지만 현재 카드 구조를 보면 "상황 반응형" 스폰이 완전히 비어 있다. 영웅이 빠르게 깎이는 고DPS 순간(패시브 트리거 급증)이나 플레이어가 강한 액티브를 선택하는 결정적 순간에 즉각 전장 판도를 바꾸는 반응적 카드가 없다. 오늘 3장은 이 공백을 정확히 채운다.

**구현 인프라 공유:**
세 카드 모두 `EmitNow(int count)` 하나를 공유 → SpawnerController에 1개 메서드만 추가하면 3장 모두 구현 가능. 이벤트 훅(OnPassiveTriggerFired, OnActivePicked)도 대칭 구조 → 코드 중복 최소화.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력: 이 문서 + `docs/design/continuous-spawn-round.md` §3 (SpawnerController 구조) + `docs/design/card-renewal.md` §3 (카드 마스터 표) 동시 전달
- 구현 순서 제안: TidalForce → HpSurgeSpawn → ActiveEcho (의존성 없이 독립 구현 가능)
- v0.2 진입 전까지 backlog 보관. 단, SpawnerController.EmitNow() 인프라는 TidalForce 구현 시 일괄 추가하여 나머지 2장이 무료로 재사용.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 몬스터들은 일정한 속도로 꾸준히 나온다. 카드를 골라서 그 속도를 빠르게 하거나 특정 몬스터를 더 많이 나오게 할 수 있다. 하지만 지금까지 없었던 것이 있다: "결정적인 순간에 갑자기 터지는" 몬스터.

예를 들어, 영웅의 체력이 딱 절반으로 떨어지는 순간 "지원군"이 갑자기 쏟아져 나온다면 어떨까? 또는 플레이어가 강력한 저주 카드를 선택하는 그 순간, 독 몬스터가 한 마리 자동으로 추가 투입된다면? 영웅은 자기가 강해지려는 바로 그 타이밍에 더 강한 압박을 받게 된다.

그래서 오늘 제안하는 카드 3장은: 버튼 한 번으로 모든 스포너를 동시에 폭발시키는 "조류 해방", 영웅 체력이 꺾일 때마다 Tank 몬스터 2마리가 자동 투입되는 "HP 급락 보강", 그리고 플레이어가 액티브 카드를 고를 때마다 독 몬스터가 1마리씩 조용히 추가되는 "액티브 공명"이다.
