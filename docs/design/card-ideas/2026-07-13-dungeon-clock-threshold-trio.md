# Card Ideas — 2026-07-13 — 던전의 시계: 타이머 임계점 자동 발화 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- 테마: "던전의 시계 (Dungeon Clock)" — 5분 타이머의 특정 경과 시각(1분·2분30초·4분)에 자동 발동하는 패시브 3종. 카드를 픽하는 순간 "예약된 강화 분기점"이 생기며, 세 장을 모두 보유하면 런의 흐름이 자동으로 3단계 클라이맥스로 구성된다.
- 목록: EarlyRush / MidnightSurge / FinalCountdown
- 기존 25장 + git log 과거 회차(총 45회차)와의 중복 회피 확인됨

---

## 1. EarlyRush (얼리 러시) — 가칭

- **카테고리**: 패시브 / 환경
- **효과 모델**:
  - 이 카드를 픽한 순간부터 IBattleContext.ElapsedTime 감시 시작.
  - 게임 시작 후 **60초 경과 시 1회 자동 발동**: 모든 스포너 동시 출력 +1 (20s 한시적, 이후 원복).
  - 발동 타이밍: 영웅이 첫 번째 패시브 선택지(HP 90%)를 막 처리한 직후 시점. 방심한 초반에 갑작스러운 물량 스파이크.
  - 수치 근거: 20s 동안 Spawner 6개 각 +1 → 평균 6~12마리 추가 필드 유입(스폰 주기 2~3s 기준). 캡 초과분은 자연 백오프로 조절되므로 캡을 벗어나지 않음.
- **구현 패턴**: `EarlyRushEffect` — `IBattleContext.ElapsedTime` 폴링(또는 OnTimeReached 이벤트 훅) → 60초 도달 시 모든 SpawnerController 에 임시 OutputDelta=+1 주입 → 20초 뒤 원복 (코루틴 또는 MonsterBuffService 타임드 버프 패턴).
- **시너지 후크**:
  - `SpawnReapers` / `SpawnPhantoms` / `SpawnPlagues` 와 중첩 시, EarlyRush 발동 구간 동안 해당 스포너가 2배 출력으로 동작 → 초반 딜러/군중 스파이크.
  - Swarm Tier1 (Phantom·Wisp 이속 ×1.3) 시너지와 맞물리면 60초 기습 무리가 영웅에게 빠르게 수렴.
- **구현 비용 추정**: 2 (ElapsedTime 이벤트 또는 폴링 + 임시 output delta 로직 — SpawnerController 에 AddOutputModifier(float delta, float duration) 메서드 하나 추가 필요)
- **중복 재검증**: `time-surge-trio`(06-05)는 시간 기반 "서지(가속)" 이나 slug 패턴·수치 불명, `event-burst-spawn-trio`(06-26)는 게임 이벤트(HP 트리거/액티브 트리거) 연동 즉발 스폰. 본 카드는 "런 타이머 절대 시각 60s" 라는 단발 임계점을 조건으로 하며, 두 회차와 개념이 다름.

---

## 2. MidnightSurge (한밤의 파도) — 가칭

- **카테고리**: 패시브 / 환경
- **효과 모델**:
  - 게임 시작 후 **150초(2분 30초) 경과 시 1회 자동 발동**: Reaper 스포너 발생 위치 근처에서 Reaper 3마리, Phantom 스포너 발생 위치 근처에서 Phantom 3마리 즉시 소환 (합계 6마리 버스트).
  - 소환된 6마리 전원에게 이동속도 ×1.3 (10s) 임시 버프.
  - 발동 타이밍: 중반 전환점(영웅이 패시브 3~4번, 액티브 3~4번 선택을 마친 시점). 빌드가 어느 정도 갖춰진 상태에서 예상치 못한 파상공격.
  - 수치 근거: 6마리 즉시 추가 → 필드 점유율 급상승 (글로벌 캡 18 대비 최대 33% 순간 추가). 캡 도달 시 스포너 자연 백오프로 대기열 형성, 속도 버프 10s 안에 영웅 도달 및 전투 참여 가능.
- **구현 패턴**: `MidnightSurgeEffect` — ElapsedTime 150s 임계 감시 → CHMPool.Pop(ReaperPrefab, spawnPosition, count=3) × 스포너 위치 순회 + MonsterBuffService.AddTemporaryBuff(moveSpeedMultiplier=1.3, duration=10f). 글로벌 캡 적용(CHMPool 또는 SpawnerController 의 캡 체크 통과 시에만 소환).
- **시너지 후크**:
  - `ReaperAtkSpeed` 패시브와 중첩: 소환된 Reaper 3마리가 이미 공속 버프를 받아 150초 파도가 고화력으로 착지.
  - `PhantomMoveSpeedBoost` 패시브와 중첩: Phantom 이속 글로벌 ×1.5 위에 추가 ×1.3 = 사실상 ×1.95, 한 순간에 빠른 팬텀 3마리가 쇄도.
  - Dps+Swarm 복합 빌드의 "중반 임계점 보상" 역할.
- **구현 비용 추정**: 2 (ElapsedTime 감시 + CHMPool 다중 Pop + MonsterBuffService — 기존 SpawnPlagues/SpawnPhantoms 효과 클래스 패턴 재활용)
- **중복 재검증**: `spawner-chain-reaction-trio`(07-03)는 스포너 간 상호 트리거(ring 위 체이닝). `event-burst-spawn-trio`(06-26)는 게임 이벤트 연동. MidnightSurge는 절대 시각 150s 단발 자동 발화 + 특정 두 종 버스트 소환 조합이므로 중복 없음.

---

## 3. FinalCountdown (피날레 카운트다운) — 가칭

- **카테고리**: 패시브 / 환경
- **효과 모델**:
  - 게임 시작 후 **240초(4분) 경과 시 1회 자동 발동**: 글로벌 캡 +6 영구 증가 (18→24, 혹은 현재 캡 기준 +6) + 모든 스포너 스폰 주기 ×0.6 (60s, 이후 원복).
  - "마지막 1분 총공세" — 런의 클라이맥스에서 가장 많은 몬스터가 가장 빠르게 쏟아지며, 빌드가 완성된 던전이 폭발적으로 가동됨.
  - 수치 근거: 캡 +6(33% 증가) + 스폰 주기 ×0.6 = 사실상 스폰 밀도 ×1.67. 기존 SpawnerHaste(전체 ×0.8, 영구)보다 1분 한정이나 훨씬 강한 가속. 4분 이전에 영웅을 처치했다면 발동 없이 런 종료 → "버티기" 빌드에만 의미 있는 카드.
- **구현 패턴**: `FinalCountdownEffect` — ElapsedTime 240s 임계 → GlobalCapManager.AddPermanentCapBonus(+6) + SpawnerController 전체 순회하여 SpawnIntervalModifier ×0.6 주입 → 코루틴으로 60s 후 interval modifier 원복, cap bonus는 영구 유지.
- **시너지 후크**:
  - `SpawnerHaste` 패시브(×0.8 영구)와 중첩: 4분 이후 스폰 주기 = 기존 × 0.8 × 0.6 = ×0.48 (기본의 절반도 안 되는 속도). 최후 1분 완전 포화.
  - Tank 축 Tier3 시너지(글로벌 캡 +6 자체가 Tier3 발동 조건)와 중복 주의 — FinalCountdown이 캡 +6을 부여하면 Tank Tier3가 이미 조건 충족됐을 수 있음. 이 경우 이중 캡 확장 → 의도된 시너지.
  - `WallOfWisps` 액티브(즉시 소환 4마리)와 4분 직후 병용 시 극한 포화 연출.
- **구현 비용 추정**: 3 (GlobalCapManager 캡 델타 API 필요 + SpawnerController interval modifier + 코루틴 원복. SpawnerHaste와 modifier 누적 처리 설계 주의)
- **중복 재검증**: `attrition-over-time-trio`(06-29)는 시간 경과마다 영웅 약화 스택 누적(TimeCurse/ExhaustionMark). `spawn-architecture-trio`(07-02)는 스포너 희생·캡 교체 구조적 변형. FinalCountdown은 고정 4분 임계점 단발 발화 + 캡 영구 확장 + 스폰 주기 1분 한시 가속 조합으로 중복 없음.

---

## 4. 공통 테마 고찰

세 장의 공통 기둥: **"IBattleContext.ElapsedTime 절대 임계점 자동 발화"**. 기존 카드들은 대부분 "영웅 HP%, 스포너 캡 조건, 필드 개체 수, 플레이어 액티브 선택" 등을 트리거로 썼으나, "경과 시간 특정 지점 도달"을 단독 트리거로 쓰는 카드는 MVP 28장 및 45회차 비축분에 없다.

오늘 이 테마를 고른 이유:
1. **운영 감성 공백**: 던전 주인이 "타이머 보고 압박감을 느끼는" 순간이 현재는 '시간 초과 = 패배' 뿐이다. 세 장을 모두 픽하면 "60초, 150초, 240초가 되는 순간 던전이 자동으로 각성한다"는 새 긴장 축이 생김.
2. **기존 45회차 비축분 공백**: git log + 파일 목록 전수 검토 결과 "타이머 절대 임계점 단발 자동 발화" 테마를 정면으로 다룬 회차 없음.
3. **IBattleContext 확장 자연스러움**: ElapsedTime은 이미 타이머 UI에서 사용 중인 값이므로 `OnTimeReached(float seconds, Action callback)` 이벤트 하나 추가로 세 장 모두 구현 가능.

세 장이 만드는 런 내 분기점:
```
0:00 ── (픽) ── 1:00 [EarlyRush: 물량 기습] ── 2:30 [MidnightSurge: 파상공격] ── 4:00 [FinalCountdown: 총공세] ── 5:00
```

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- IBattleContext 에 `OnTimeReached(float seconds, Action callback)` 또는 `ElapsedTime` 폴링 패턴 중 하나를 game-designer + gameplay-programmer 합의로 결정 필요
- FinalCountdown의 GlobalCapManager 캡 델타 API 는 `spawn-architecture-trio`(07-02)의 SacrificedSpawner 구현 시 이미 GlobalCapManager 논의가 있었으므로 연동 검토 권장
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서는 던전이 처음부터 끝까지 같은 속도로 돌아갑니다. 영웅이 강해서 잘 버티면 5분을 다 채워 던전이 지는데, 이때 플레이어가 할 수 있는 게 별로 없어요. 오늘 제안하는 카드 세 장은 5분짜리 타이머에 "깜짝 부스터"를 예약하는 카드들입니다. 1분이 되면 갑자기 몬스터가 우르르 쏟아지고, 2분30초에는 강한 몬스터 6마리가 한꺼번에 뛰쳐나오고, 4분이 되는 순간에는 던전 전체가 마지막 1분 동안 폭발적으로 가동됩니다. 마치 보스가 체력이 낮아질수록 갑자기 강해지듯이, 영웅이 살아남을수록 던전이 점점 각성하는 긴장감을 만드는 거예요. 그래서 오늘 제안하는 카드 3장은: EarlyRush(1분 기습 물량), MidnightSurge(2분30초 파상공격), FinalCountdown(4분 마지막 총공세) 입니다.
