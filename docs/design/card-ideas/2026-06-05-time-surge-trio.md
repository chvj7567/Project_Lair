# Card Ideas — 2026-06-05 — 시간을 무기로 — 타이머 연동 압박 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 배틀 타이머를 카드 효과의 직접 입력(조건/스케일)으로 삼는 카드 3장 — "경과 시간" 누적 vs "잔여 시간" 활용이라는 상반된 두 축을 탐색
- **목록**: 시간의 저주 (CurseOfTime) / 파도 해방 (WaveRelease) / 시간 거래 (TimeTrade)
- 기존 28장 + git log 과거 8회차와의 중복 회피 확인됨
  - 기존 28장: 타이머 값을 효과 입력으로 쓰는 카드 전무. SpawnerHaste는 주기 영구 단축(타이머 구독 없음), TimeStop은 영웅 정지(타이머 소비 없음)
  - 과거 8회차(5/28~6/4): 전장 상태 감지(몬스터 수·HP%), 종간 시너지(공존 조건), 플레이그-독 생태계, 영구 낙인 액티브, 리퍼·헥스 심화, 죽음의 메아리, 위스프 벽 포위, Dps×Debuff 교차 사냥 — 어느 회차도 BattleClock 값을 카드 스케일/조건으로 사용하는 메커니즘 미제안 ✅

---

## 1. 시간의 저주 (CurseOfTime) — 가칭

- **카테고리**: 패시브 강화 / Swarm 축
- **효과 모델**:
  - 픽 후부터 30초마다(= 액티브 트리거와 동일 주기) 필드 전체 몬스터의 이동속도가 +4% 영구 누적된다.
  - 주요 시점별 누적치 (픽 시점 = 0s 기준):
    | 경과 시간 | 누적 이속 보너스 |
    |---|---|
    | 0:30 | +4% |
    | 1:00 | +8% |
    | 2:00 | +16% |
    | 3:00 | +24% |
    | 4:00 | +32% |
    | 5:00 | +40% (이론 최대) |
  - 평균 영웅 사망 2~4분 기준(컨셉 §8) 실질 누적 범위: **+16~+32%**
  - 밸런스 근거: PhantomMoveSpeedBoost(팬텀 이속 ×1.5 = +50% 즉시)와 비교 — 전 종(種) 대상이지만 2분이 지나야 +16%로 수렴. 픽 타이밍(早期 픽 = 누적 최대화)이 새로운 전략 결정 포인트. 늦게 픽할수록 효율이 급감하므로 자연스럽게 "HP 90% 선택지"에서 고려 대상이 됨.
- **구현 패턴**: ActiveTriggerService 30s 이벤트 구독 → 발화마다 MonsterBuffService 글로벌 이속 스케일 누적 적용.
  ```
  CurseOfTimeEffect.Apply →
    _accumulated = 0f
    ActiveTriggerService.OnTrigger += OnTimeMark   // 30s 이벤트 재사용

  OnTimeMark:
    _accumulated += 0.04f
    MonsterBuffService.SetGlobalMoveSpeedBonus(_accumulated)
    //# SetGlobalMoveSpeedBonus: 별도 영구 누적 버킷(기존 tick-reset 버킷과 분리)
  ```
  ActiveTriggerService.OnTrigger 이벤트가 미노출 상태면 ActiveTriggerService에 event Action OnTrigger 1줄 추가(~3줄 총 변경). 30s 타이머를 별도로 만들지 않고 재사용.
- **시너지 후크**:
  - **PhantomMoveSpeedBoost**: 팬텀 기본 이속 ×1.5 → CurseOfTime +16~32% 추가 → 중반부 팬텀 영웅 포위 도달 시간 급감
  - **SpawnerHaste**: 더 빠른 스폰 주기 + 점점 빠른 이동 → 후반부 "몬스터 홍수" 가속 시너지
  - **파도 해방(카드 2)**: 파도 해방으로 즉시 소환된 몬스터들이 이미 누적된 CurseOfTime 속도 보너스를 받고 등장
- **구현 비용 추정**: 3 (ActiveTriggerService 이벤트 노출 + 누적 변수 관리 + MonsterBuffService 영구 누적 버킷 신규. 기존 tick-reset 버킷과 병존 설계 필요)
- **중복 재검증**:
  - 기존 PhantomMoveSpeedBoost = 단일 종(팬텀), 고정 배율 ✅
  - 기존 SpawnerHaste = 스폰 주기 단축, 이동속도 무관 ✅
  - 과거 8회차 전부 — 타이머 구독 기반 강화 미제안 ✅

---

## 2. 파도 해방 (WaveRelease) — 가칭

- **카테고리**: 액티브 버프 / Swarm 축
- **효과 모델**:
  - 발동 즉시, 현재 배치된 **모든 스포너(최대 6개)**가 주기 타이머 상관없이 **각자 몬스터 1마리를 즉시 스폰**한다. 이후 각 스포너의 주기 타이머는 0으로 리셋(새 주기 시작).
  - 즉각 효과: 최대 6마리 즉시 소환(필드 글로벌 캡 18 제한 내 → 캡 초과분은 스킵).
  - 타이머 리셋 효과: 이후 다음 스폰이 "풀 주기 후" 발생. SpawnerHaste(×0.8) 활성 시 단축 주기 반영.
  - 부가 의미: 파도 → 소강 리듬. 즉시 소환 후 잠깐의 조용한 구간 → 다음 파도 시작. 전략적 타이밍에 쓰면 "대량 압박 → 일시 소강 → 재집결" 사이클 생성.
  - 밸런스 근거: WallOfWisps = 위스프 4마리, 영웅 4방 즉시(특정 종·특정 위치). WaveRelease = 배치된 전체 종, 링 위치에서 정상 수렴 → 생태계 그대로 유지하며 압박. 타이머 리셋으로 "폭발 후 소강"이 자연 발생해 자가 밸런싱.
- **구현 패턴**: IBattleContext를 통해 등록된 스포너 목록 순회 → ForceSpawnNow + ResetCycleTimer.
  ```
  WaveReleaseEffect.Apply →
    foreach spawner in ctx.GetAllSpawners():
      spawner.ForceSpawnNow()     //# 즉시 1마리 스폰 (캡 체크 포함)
      spawner.ResetCycleTimer()   //# 주기 타이머 0으로 리셋
  ```
  SpawnerController에 ForceSpawnNow() / ResetCycleTimer() 메서드 신규 추가 (~10줄). 기존 스폰 로직 내부 재사용. IBattleContext.GetAllSpawners() 미노출 시 인터페이스에 1줄 추가.
- **시너지 후크**:
  - **SpawnerHaste**: WaveRelease 후 타이머 리셋 → 이미 단축된(×0.8) 주기로 새 사이클 시작 → 다음 파도가 더 빨리 도달
  - **시간의 저주(카드 1)**: 파도 해방으로 소환된 모든 몬스터가 즉시 CurseOfTime 이속 누적 혜택 수령
  - **Swarm 축 Tier3 시너지(스포너 출력 +1)**: 각 스포너가 이미 +1 출력 상태면 ForceSpawnNow가 2마리 스폰 → 최대 12마리 즉시 소환
- **구현 비용 추정**: 3 (IBattleContext.GetAllSpawners() 노출 + SpawnerController.ForceSpawnNow / ResetCycleTimer 신규 메서드. 기존 스폰 로직 재사용으로 신규 시스템 없음)
- **중복 재검증**:
  - WallOfWisps = 위스프 종만, 영웅 위치 4방, 타이머 리셋 없음 ✅
  - Multiply/FastBreeding = 팬텀 스포너 주기 ×0.6 영구, 즉시 스폰 없음 ✅
  - SpawnX 계열 = 스포너 출력 영구 +1, 즉시 스폰 없음 ✅
  - 과거 8회차 — "전체 스포너 강제 즉시 발화 + 타이머 리셋" 메커니즘 미제안 ✅

---

## 3. 시간 거래 (TimeTrade) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**:
  - 발동 시점 **남은 전투 시간을 30초 단위로 환산한 값(time-units)** 이 버프 강도가 된다.
    - time-units = floor(remainingSeconds / 30)
  - 효과: 모든 몬스터 공격속도 **(time-units × 3%) 보너스**, 15초 지속.
  - 대표 사례:
    | 발동 시점 | 남은 시간 | time-units | 공격속도 보너스 |
    |---|---|---|---|
    | 0:30 | 270s | 9 | +27% |
    | 1:30 | 210s | 7 | +21% |
    | 2:30 | 150s | 5 | +15% |
    | 3:30 | 90s  | 3 | +9%  |
    | 4:30 | 30s  | 1 | +3%  |
  - 핵심 딜레마: **조기 사용 = 강함, 지연 사용 = 약함**. 기존 DespairFrenzy(영웅 HP 낮을수록 강함 → 늦게 사용 보상)의 역방향 인센티브 구조. 같은 "조건부 공속 버프"이지만 타이밍 전략이 정반대.
  - 밸런스 근거(컨셉 §8): 기준 카드 Frenzy = +50% 공속 / 10초, 고정. TimeTrade 최강 발동(0:30, +27%) < Frenzy(+50%)으로 절대 강도 미만. 그러나 15s 지속(Frenzy보다 5s 추가)과 "조기 사용 보상"이라는 전략 레이어가 차별점. 최강 발동 구간(초반 3개 선택지)에서만 의미 있게 강하고 중반 이후 급감 → 희소성 높은 초반 선택지를 TimeTrade에 할당할 가치가 있는지 플레이어가 판단.
- **구현 패턴**: IBattleContext.BattleClock.GetRemainingTime() 읽기 → 단위 계산 → MonsterBuffService 시한부 버프.
  ```csharp
  TimeTrade.Apply(IBattleContext ctx):
      float remaining = ctx.BattleClock.GetRemainingTime()
      int units = Mathf.FloorToInt(remaining / 30f)
      float bonus = 1f + units * 0.03f
      MonsterBuffService.AddTimedBuff(EMonsterBuff.AtkSpeed, bonus, 15f)
  ```
  BattleClock.cs 이미 존재(QA 리포트 `2026-05-22.md` §2 확인). IBattleContext에 `BattleClock GetRemainingTime()` 접근 경로가 미노출 상태면 인터페이스에 1줄 추가(최대 구현 비용 3).
- **시너지 후크**:
  - **시간의 저주(카드 1)** 와 병용 시 대조 전략: 저주는 "시간이 지날수록 강해지는" 패시브, 거래는 "시간이 지날수록 약해지는" 액티브 → 조기 구간(HP 90~70% 선택지)에서 TimeTrade 픽 → 이후 CurseOfTime 스택이 자연스럽게 보완
  - **ReaperAtkSpeed(기존 패시브)**: 리퍼 공속 쿨다운 이미 ×0.7 상태 + TimeTrade +21~27% → 초반 리퍼 burst DPS 극대화
  - **Dps 시너지 Tier2(Reaper·Hex Cooldown ×0.8)**: TimeTrade 조기 발동 시 공속 보너스가 Tier2 발동 전에 Reaper·Hex를 미리 강화
- **구현 비용 추정**: 2~3 (BattleClock 접근 경로 노출 여부에 따라. MonsterBuffService 기존 패턴 그대로 재사용)
- **중복 재검증**:
  - Frenzy = +50% 공속, 고정, 조건 없음 ✅
  - DespairFrenzy(6/4 루틴 제안) = 영웅 HP 비율 기반 공속 스케일링, 늦을수록 강함 ✅ (조건 축 + 인센티브 방향 모두 반대)
  - 과거 8회차 — 잔여 시간 기반 강도 스케일링 미제안 ✅

---

## 4. 공통 테마 고찰

세 카드는 **"BattleClock 값을 카드 효과의 직접 입력으로 사용"** 이라는 새로운 메커니즘 축을 공유한다:

| 카드 | 타이머 활용 방식 | 플레이어 결정 포인트 |
|---|---|---|
| 시간의 저주 | 경과 시간 × 4% 이속 누적 | 최대한 빨리 픽 → 오래 누적 |
| 파도 해방 | 발동 즉시 전 스포너 강제 발화 + 타이머 리셋 | 전략적 압박 타이밍 선택 |
| 시간 거래 | 잔여 시간 ÷ 30 = 공속 보너스 단위 | 조기 사용 = 강함, 지연 = 약화 |

**왜 오늘 이 테마인가:**
- 기존 28장은 "픽 즉시 고정 효과" 또는 "발동 후 일정 시간 유지 → 해제" 패턴이 전부. 전투 타이머(BattleClock)를 카드 효과 조건/스케일로 직접 연결하는 카드가 전무.
- QA 보고서(`2026-05-22.md`)가 BLOCKED 상태로 픽률 데이터 부재 → 구조 분석으로 대체: 9번의 액티브 선택에서 어느 시점에 카드를 쓰는지가 결과에 거의 영향을 미치지 않는다. 모든 액티브가 "발동 즉시 효과, 타이밍 무관" 구조이기 때문. TimeTrade는 이 구조를 깨는 최초의 "타이밍 의사결정 카드".
- 과거 8회차(5/28~6/4) 어느 회차도 BattleClock 값을 카드 스케일/조건으로 사용하는 아이디어 없음.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **구현 우선순위 제안**:
  1. TimeTrade(비용 2~3) — BattleClock.GetRemainingTime() 접근 확인 후 MonsterBuffService 기존 패턴 그대로. 가장 작은 변경으로 "타이밍 의사결정" 레이어 검증 가능
  2. WaveRelease(비용 3) — SpawnerController ForceSpawnNow/ResetCycleTimer API 추가. 이 API는 이후 다른 스포너 제어 카드의 기반 인프라가 됨
  3. CurseOfTime(비용 3) — ActiveTriggerService 이벤트 노출 + 영구 누적 버킷 관리. 마지막으로 추가해 앞 두 카드와 시너지 관계를 한꺼번에 QA

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임의 카드들은 쓰는 순간 효과가 나타나거나, 몇 초간 강해졌다 돌아오는 방식이라서 언제 쓰든 결과가 비슷합니다. 마치 알람 시계를 보지 않고 언제나 같은 버튼을 누르는 것처럼요. 오늘 제안하는 카드 3장은 전투 시계를 직접 읽어서 작동합니다 — 전투 내내 30초마다 조금씩 몬스터가 빨라지는 저주, 모든 스포너에서 동시에 몬스터 한 마리씩을 폭발적으로 쏟아낸 뒤 다시 준비하는 파도, 그리고 지금 쓸수록 강하고 늦게 쓸수록 약해져서 "지금 써야 해?" 라는 고민을 처음으로 만드는 시간 거래 카드입니다. 그래서 오늘 제안하는 카드 3장은: 30초마다 모든 몬스터 이동속도가 조금씩 영구히 빨라지는 '시간의 저주', 배치된 모든 스포너가 동시에 즉시 한 마리씩 쏟아내는 '파도 해방', 남은 시간이 많을수록 더 강한 공속 부스트를 받는 '시간 거래'입니다.
