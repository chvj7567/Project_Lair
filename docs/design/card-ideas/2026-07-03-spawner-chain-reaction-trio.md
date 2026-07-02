# Card Ideas — 2026-07-03 — 스포너 연쇄 반응: ring 위 스포너들이 서로를 깨우는 네트워크 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 스포너 연쇄 반응 (Spawner Chain Reaction) — ring 위에 배치된 6개 스포너를 독립 개체가 아닌 **네트워크 노드**로 바라보는 첫 제안. 한 스포너가 발사하면 다른 스포너가 반응하는 체인 구조.
- **목록**: VanguardEcho (선봉 메아리) / SwarmChain (군집 연쇄) / ResonancePulse (공명 파동)
- **기존 28장 + git log 20회차 + docs/design/card-ideas/ 34개 파일과의 중복 회피 확인됨**
  - 기존 28장: SpawnerHaste·Multiply 등 스포너 관련 카드 5장 존재하나, 모두 **개별 스포너 독립 동작** (쿨다운 배율, 출력 수 등). 스포너 간 상호 트리거 카드는 전무.
  - git log 20회차 검토:
    - 06-17 스포너 다양성 보상 (DiverseHaste·HarmonyHeal·CombinedDeploy): 6종 스포너 동시 유지 보상. 오늘 제안은 "종 유지"가 아닌 "발사 이벤트 연쇄".
    - 06-18 종 스포너 집중 가속 (ReaperOverflow·WraithTide·PlagueSpread): 개별 종 스포너 쿨다운 단축. 오늘 제안은 "자종 가속"이 아닌 "타종 트리거".
    - 07-01 스포너 아키텍처 (TwinSpawn·SacrificedSpawner·RelentlessCycle): 개별 스포너 구조 변경. 오늘 제안은 "네트워크 상호작용".
    - 나머지 17회차: 사망 메아리·처치 반향·밀도 압박·공간 압박·시간 탈진·생존 성장 등 — 모두 스포너 간 체인 미포함.
  - docs 폴더 34개 파일 전부 확인: `spawner-chain`, `cross-spawner`, `network-spawn` 계열 없음.
  - **중복 없음 ✅**

---

## 1. VanguardEcho — 선봉 메아리

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wisp·Wraith Spawner 가 몬스터를 스폰할 때마다, ring 상 대각(index +3) 위치의 Spawner 쿨다운을 즉시 **-2s** 단축.
  - 단, 단축 후 쿨다운이 0.5s 이하로 내려가지 않도록 최솟값 클램프.
  - **밸런스 근거**: 기본 스폰 주기 ~3s, 5분 런 기준 Tank 스포너 1개당 약 100회 발사. 대각 스포너 쿨다운 -2s × 100회 → 대각 스포너가 약 200s 분량의 쿨다운을 더 당겨 받음. 쿨다운 3s 기준 대각 스포너가 최대 ~67회 추가 발사 가능 (글로벌 캡 18 포화 시 자연 제한). Tank 스포너 2개(Wisp·Wraith) 보유 시 각각 독립 발동.
  - **수치 조정 포인트**: 쿨다운 단축량(2s) / 최솟값 클램프(0.5s) — BalanceConfig 인스펙터 노출.
  - 중첩 픽 시: 2픽 → 단축량 4s, 3픽 → 6s (클램프 동일).
- **구현 패턴**:
  - `VanguardEchoEffect.cs` — `IBattleContext.OnSpawnerFired` 이벤트 구독. EMonster.Wisp/Wraith 필터.
  - 발사 시: `IBattleContext.GetSpawnerAt(firedSpawner.RingIndex + 3)` → `spawner.ReduceCooldown(2f, minClamp: 0.5f)`.
  - Ring 대각 산출: `(ringIndex + 3) % 6` — 6개 스포너 기준 반대편.
  - `OnSpawnerFired` 이벤트 자체가 SpawnerHaste 의 구현에서 이미 스포너 접근 패턴 존재 — 재사용.
- **시너지 후크**:
  - `SpawnWraith` (Wraith 출력 +1) + VanguardEcho: Wraith 스포너가 자주 발사 → 대각 스포너 쿨다운 더 자주 단축.
  - `WispHpBoost` + VanguardEcho: Wisp 가 오래 살아 탱킹하는 동안 Wisp 스포너는 계속 발사 → 대각 체인 유지.
  - Tank Tier2 (Wisp·Wraith Power ×1.2) + VanguardEcho: 탱커가 강해진 동안 대각 스포너도 함께 가속 → Tank 빌드가 전체 ring 속도를 견인.
- **구현 비용 추정**: 3 (OnSpawnerFired 이벤트 + 인접 스포너 접근 패턴 신규 추가, SpawnerHaste 구현 기반 재사용)
- **중복 재검증**: SpawnerHaste (모든 스포너 주기 ×0.8 영구, 패시브)는 전체 균일 가속. VanguardEcho는 Tank 스포너 발사 이벤트를 트리거로 대각 스포너만 선택적 단축. 트리거 조건·대상 스포너 선택성·상호 의존 구조가 다름 ✓

---

## 2. SwarmChain — 군집 연쇄

- **카테고리**: 패시브 추가 (Swarm 축)
- **효과 모델**:
  - Phantom Spawner 가 Phantom 을 스폰할 때마다, 30% 확률로 ring 상 **인접(±1)** Spawner 1개(무작위)가 즉시 보너스 스폰 1마리 발사.
  - 보너스 스폰은 해당 인접 스포너 **고유 종** 을 소환 (Phantom 스포너면 Phantom, Reaper 스포너면 Reaper 등).
  - 글로벌 캡(18마리) 체크 — 캡 포화 시 보너스 스폰 불발.
  - **밸런스 근거**: Phantom 스포너 1개, 기본 주기 3s → 5분 약 100회 스폰. 30% 발동 → 기대값 약 30회 인접 체인. 인접 스포너 2개 중 무작위이므로 기대 15회씩. 추가 소환 총량 ~30마리 — 스폰 총 ~300회 기준 +10% 증가, 글로벌 캡으로 자연 조절.
  - 중첩 픽: 2픽 → 발동률 51% (1-0.7²), 3픽 → 65.7%.
- **구현 패턴**:
  - `SwarmChainEffect.cs` — `IBattleContext.OnSpawnerFired` 이벤트 구독. EMonster.Phantom 필터.
  - `Random.value < 0.30f` → `IBattleContext.GetAdjacentSpawners(firedSpawner.RingIndex)` (ring ±1, 최대 2개) 중 `Random.Range(0, 2)` 로 1개 선택.
  - 선택된 스포너에 대해 `CHMPool.Instance.Pop(selectedSpawner.MonsterPrefab, selectedSpawner.SpawnPoint)` — WallOfWisps/PhantomBirth(06-02) 의 즉시 소환 패턴 동일.
  - 글로벌 캡 가드: `IBattleContext.IsFieldCapReached()` 가 true 면 스킵.
- **시너지 후크**:
  - `FastBreeding` (Phantom 스포너 주기 ×0.6, Multiply) + SwarmChain: Phantom 발사 빈도 1.67배 → 체인 발동 빈도 동반 증가.
  - `SpawnPhantoms` (Phantom 스포너 출력 +1) + SwarmChain: 출력 +1 이면 각 Phantom 스폰마다 체인 기회 그대로 유지 — 총 Phantom 스폰 수 증가 → 기대 체인 수 증가.
  - Swarm Tier1 (Phantom·Wisp MoveSpeed ×1.3) + SwarmChain: 빠른 Phantom 이 죽지 않고 오래 싸우는 한편 체인으로 인접 종도 활성화 — Swarm 빌드 완성도 향상.
  - VanguardEcho (오늘 카드 1) + SwarmChain: Wisp 스포너가 대각 스포너를 당기고, 그 스포너가 Phantom 이면 SwarmChain 을 연발 — 두 체인이 ring 전체를 아우름.
- **구현 비용 추정**: 3 (OnSpawnerFired 이벤트는 VanguardEcho 와 공유 채널, 인접 인덱스 ±1 산출 신규, 즉시 소환은 기존 WallOfWisps 패턴)
- **중복 재검증**: Multiply/FastBreeding (Phantom 스포너 주기 영구 가속, Swarm A)는 Phantom 스포너만 가속. SwarmChain은 Phantom 발사 이벤트를 트리거로 **인접 타종 스포너** 소환. 효과 대상 스포너·소환 종·체인 방향이 다름 ✓

---

## 3. ResonancePulse — 공명 파동

- **카테고리**: 액티브 버프 / 와일드 (Dps 축)
- **효과 모델**:
  - 발동 즉시, **가장 최근 발사한 스포너** 를 기점으로 ring 을 따라 **시계 방향** 순서로 4개 Spawner 가 **0.5초 간격** 으로 순차 보너스 스폰 1마리씩 발사.
  - 총 4마리, 발동 후 1.5초 내에 ring 의 서로 다른 4개 방위에서 등장 — 영웅이 특정 방향으로 도망쳐도 다른 방향에서 추가로 나타남.
  - 각 스포너는 자신의 고유 종을 소환.
  - 글로벌 캡 적용 — 캡 포화 스포너는 건너뛰고 다음 스포너로.
  - **밸런스 근거**: WallOfWisps (Tank A, Wisp 4마리 즉시) 와 비교 — 동일 4마리 소환이지만 단일 종(Wisp) 고정 vs 4개 다른 종. 0.5s 지연 분산이 있어 즉발보다 회피 기회가 소폭 높으나, 사방 포위 압박은 더 강함. Dps 축 액티브는 현재 Frenzy(공속 +50%, 10s)·BloodThirst·MarkOfDeath(×1.5, 5s) 3장인데 모두 지속 버프 — 즉발 버스트 소환은 Dps 축에 새로운 플레이 질감.
  - **수치 조정 포인트**: 발사 간격(0.5s) / 발사 스포너 수(4) — BalanceConfig 인스펙터.
- **구현 패턴**:
  - `ResonancePulseEffect.cs` — `IBattleContext.GetMostRecentlyFiredSpawnerIndex()` 로 시작점 결정.
  - `for (int i = 0; i < 4; i++)` : `IBattleContext.GetSpawnerAt((startIndex + i) % 6)` → 각각 `i × 0.5s` 지연 후 Pop.
  - 지연 구현: `IronWillEffect` 의 `async/await WaitForSeconds` 패턴 (UnityEngine.Time 기반) 재사용.
  - 글로벌 캡 도달 스포너는 건너뛰고 `i` 계속 진행.
- **시너지 후크**:
  - `SpawnerHaste` (×0.8 영구) + ResonancePulse: 이미 가속된 스포너들이 체인 발사에도 참여 → 0.5s 지연 후 나오는 몬스터들이 더 빠르게 영웅을 추적.
  - `MarkOfDeath` (영웅 피해 ×1.5, 5s) + ResonancePulse: MarkOfDeath 먼저 걸고 1.5초 내 4방위 소환 → 영웅이 취약한 창에 사방 압박.
  - VanguardEcho·SwarmChain (오늘 카드 1·2) + ResonancePulse: 패시브 체인이 ring 을 항상 활성 상태로 유지하는 중에, 액티브로 즉발 파동을 날려 순간 압박.
- **구현 비용 추정**: 3 (GetMostRecentlyFiredSpawnerIndex 신규 API 1개 추가, 나머지는 IronWillEffect 지연 패턴 + CHMPool.Pop 기존 패턴)
- **중복 재검증**: WallOfWisps (Tank A, 4방위 Wisp 4마리 즉시 소환)는 영웅 위치 기준 4방위·단일 종(Wisp)·즉발. ResonancePulse는 스포너 ring 기준 시계방향 4개·다종·0.5s 간격 파동. 소환 기준점(영웅 vs 스포너 ring)·종 구성·타이밍 패턴 모두 다름 ✓

---

## 4. 공통 테마 고찰

### 왜 "스포너 연쇄 반응" 인가

기존 28장 + 34개 파일의 스포너 관련 카드를 분석하면 한 가지 공통점이 드러난다: **모든 카드가 스포너를 독립 개체로 취급한다.** SpawnerHaste 는 모든 스포너를 균일하게 가속하고, SpawnWisps 는 Wisp 스포너 출력만 늘리며, Multiply 는 Phantom 스포너만 가속한다. 스포너들이 서로 소통하거나 하나의 발사가 다른 발사를 유발하는 구조는 28장 어디에도, 34개 과거 제안 어디에도 없었다.

지속 스폰 모델(컨셉 §4.1)은 6개 스포너가 ring 위에 배치되어 독립 주기로 발사하는 구조다. 이 ring 구조는 본질적으로 네트워크 토폴로지를 내포하고 있다 — 대각·인접·순환이라는 위상 관계가 있다. 오늘 제안하는 3장은 이 잠재된 ring 구조를 전략적 레이어로 노출시킨다.

### QA 연계

최신 QA 리포트(2026-05-22)는 시뮬레이션 BLOCKED 상태로 픽률 데이터 없음. 설계 관점 분석:
- Dps 축 액티브 3장(Frenzy·BloodThirst·MarkOfDeath)이 모두 "지속 버프 / 지속 효과" — 즉발 스폰 버스트가 없어 "즉각 압박" 질감이 부족.
- Tank·Swarm 패시브에 스포너 네트워크 카드가 없어 "개별 종 강화" 외의 전략 레이어가 빈칸.
- ResonancePulse 는 Dps 액티브에 "즉발 버스트 소환" 질감을 채우고, VanguardEcho·SwarmChain 은 Tank·Swarm 패시브에 "ring-aware 전략" 레이어를 추가.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- VanguardEcho·SwarmChain 은 같은 `IBattleContext.OnSpawnerFired` 채널 + 스포너 ring 인덱스 API 를 공유하므로 **1회 구현 스프린트에 묶기** 권장 — ring 인덱스 추가 작업이 공통 선행 조건
- ResonancePulse 는 독립 스프린트 가능 (ring 인덱스 추가 후 바로 연계 가능)
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 여섯 개의 스포너(몬스터를 만들어내는 장치)는 각자 따로따로 일한다. 각자 주기에 맞춰 몬스터를 뿜어낼 뿐, 옆에 있는 스포너가 뭘 하는지 신경 쓰지 않는다. 그런데 만약 스포너들이 서로 연락을 주고받는다면 어떨까? 예를 들어 탱커 스포너가 몬스터를 내보내면, 맞은편 스포너도 "나도 빨리 내보내야지" 하고 반응하는 식이다. 오늘 제안하는 카드들은 이 여섯 스포너들이 하나의 팀처럼 움직이도록 해 주는 연쇄 반응 효과를 담고 있다. 그래서 오늘 제안하는 카드 3장은: 탱커 스포너가 쏘면 반대편 스포너가 빨라지는 "선봉 메아리", 팬텀 스포너가 쏘면 옆 스포너가 30% 확률로 따라 쏘는 "군집 연쇄", 그리고 플레이어가 직접 발동하면 스포너 여섯 개 중 네 개가 시계 방향으로 0.5초 간격씩 연달아 터지는 "공명 파동"입니다.
