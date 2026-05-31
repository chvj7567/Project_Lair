# Project Lair — 게임 설명서 (2026-05-31 KST)

> 자동 생성 (매주 월 07:01 KST) — Rule 01 자동화 예외 루틴이 생성/갱신.
> 생성 기준: spec/design 문서 + UI 코드 종합. 코드 ↔ spec 모순 시 코드 진실.

---

## 0. 한 줄 컨셉

5분짜리 역방향 보스전 로그라이크. 플레이어는 던전 주인. 영웅 한 명이 자동으로 던전을 돌파해오고, 반지름 14유닛 원에 배치된 Spawner 6개에서 몬스터가 끊임없이 흘러나와 자동 전투한다. 영웅 HP 10%마다 패시브 카드(3택 1), 30초마다 액티브 카드(3택 1)를 골라 **4가지 빌드 축(Tank/Dps/Debuff/Swarm)** 을 쌓아 5분 안에 영웅을 처치하면 승리.

---

## 1. 게임 시작 — Battle 씬 진입

앱 실행 → 메인 메뉴 없이 **Battle.unity** 씬이 즉시 로드된다 (MVP 정책: 메인 메뉴·세팅 화면 없음).

씬 로드 직후 `BattleController.Start()` 가 다음 순서로 초기화를 수행한다:

1. Addressables 리소스 초기화 (`CHMResource.Init`)
2. UI 초기화 (`CHMUI.Init`, `CHMPool.Init`)
3. **BattleHud** 팝업 (`EUI.BattleHud`) — 상단 타이머·영웅 HP 바, 좌측 빌드 시너지 패널, 하단 스포너 상태 6셀 패널, 하단 빌드 패널 등장
4. 영웅(Knight) 스폰 — 던전 중앙 (0, 0, 0)
5. Spawner 6개 작동 시작 — 각자 초기 지연 후 고정 주기로 몬스터 흘려보내기 시작
6. 5분 카운트다운 타이머 시작

### 1.1 카메라

| 속성 | 값 |
|---|---|
| Position | (0, 12, -8) |
| Rotation | (50°, 0°, 0°) |
| Projection | Perspective, FOV 60 |
| 배경색 | `#1F2937` (짙은 회색) |

### 1.2 영웅 (기사 Knight)

| 항목 | 값 |
|---|---|
| 시작 위치 | (0, 0, 0) — 던전 중앙 |
| HP | 4000 (BalanceConfig.asset 기준) |
| 공격력 | 50 |
| 공격 쿨다운 | 1.0s |
| 공격 사거리 | 1.5 유닛 |
| 이동속도 | 3.0 |
| 비주얼 | 파랑 Capsule `#3B82F6` 스케일 1.0 |
| AI 행동 | 가장 가까운 살아있는 몬스터로 자동 이동 → 사거리 내 정지 → 자동 공격 반복 |

### 1.3 Spawner 6개 — Ring 배치 (v0.6 기준)

반지름 **14.0 유닛** 원에 60° 간격 균등 배치.

| # | 각도 | 위치 (x, z) | 초기 종 | 스폰 주기 | 초기 지연 |
|---|---|---|---|---|---|
| 0 | 0° | (14.0, 0.0) | Wisp | 9.0s | 0.0s |
| 1 | 60° | (7.0, 12.124) | Reaper | 12.0s | 0.5s |
| 2 | 120° | (-7.0, 12.124) | Phantom | 6.0s | 1.0s |
| 3 | 180° | (-14.0, 0.0) | **Plague** | 10.0s | 1.5s |
| 4 | 240° | (-7.0, -12.124) | Wraith | 20.0s | 2.0s |
| 5 | 300° | (7.0, -12.124) | Hex | 15.0s | 2.5s |

> Spawner #3 (180°) 는 v0.6에서 Wisp → **Plague** 로 전환됐다. Debuff 빌드 축 작동의 전제 조건.

**필드 글로벌 캡**: 동시 존재 몬스터 최대 **18마리**. 캡 초과 시 해당 Spawner 는 해당 주기를 skip.

### 1.4 몬스터 6종 기본 스탯 (BalanceConfig.asset 기준)

| 종 | HP | Power | MoveSpeed | Cooldown | Range | 비주얼 |
|---|---|---|---|---|---|---|
| Wisp | 200 | 5 | 1.0 | 1.0s | 1.5 | 초록 Sphere `#22C55E` 스케일 0.6 |
| Wraith | 500 | 10 | 0.8 | 1.0s | 1.5 | 회색 Cube `#6B7280` 스케일 1.2 |
| Reaper | 100 | 6 | 1.5 | 0.5s | 1.5 | 빨강 Capsule `#EF4444` 스케일 0.9 |
| Hex | 60 | 9 | 1.4 | 1.0s | 5.0 | 노랑 Capsule `#EAB308` 스케일 0.8 |
| Plague | 80 | 2 | 1.3 | 1.0s | 1.5 | 보라 Cube `#A855F7` Y-scale 0.3 |
| Phantom | 30 | 2 | 2.4 | 1.0s | 1.5 | 검정 Sphere `#1F2937` 스케일 0.3 |

---

## 2. 자동 전투 진행

전투는 플레이어 입력 없이 진행된다. 영웅과 몬스터 모두 `AutoCombatAI` 컴포넌트가 제어한다.

**AI 루프 (매 프레임):**
1. `IsAlive == false` 이면 즉시 return (사망 후 AI 정지)
2. 가장 가까운 살아있는 적을 `CharacterRegistry` 에서 탐색
3. 적이 없으면 `Stop()`
4. 거리 ≤ 사거리 이면 정지 + 쿨다운 확인 후 공격 (`TryAttack`)
5. 거리 > 사거리 이면 `MoveTo(적 위치)`

**카운트다운 타이머:** `BattleClock` 이 `Time.deltaTime` 누적 → HUD 표시는 `ceil` 기준 (예: elapsed=30.001s 시 HUD "4:30" 유지).

**종료 조건:**
- **승리**: 영웅 HP 0 (`Health.OnDied` → `EndBattle(Win)`)
- **패배**: 5:00 도달 (`BattleClock.OnTimeUp` → `EndBattle(Lose)`)

종료 시: 모든 `AutoCombatAI.enabled = false` → `ResultPopup` 표시.

---

## 3. 패시브 카드 트리거 — HP 임계점

`PassiveTriggerService` 가 영웅 HP 비율을 매 `Health.OnChanged` 시점에 검사.

| 임계점 | 트리거 인덱스 |
|---|---|
| HP 90% 이하 | 0 |
| HP 80% 이하 | 1 |
| HP 70% 이하 | 2 |
| HP 60% 이하 | 3 |
| HP 50% 이하 | 4 |
| HP 40% 이하 | 5 |
| HP 30% 이하 | 6 |
| HP 20% 이하 | 7 |
| HP 10% 이하 | 8 |

**발동 횟수**: 라운드 당 최대 9회. 각 임계점은 1회만 발동 (재발동 없음).

**큰 데미지로 여러 임계점 동시 통과 시**: `TriggerQueue` 에 순차 enqueue → 차례로 처리.

**처리 흐름:**
1. `_queue.Enqueue(Passive, index)` → `TryProcessNext()` 호출
2. `PauseService.Pause()` → `Time.timeScale = 0f` (게임 일시정지)
3. `CardDeck.Draw(3)` — 패시브 풀 28장 중 랜덤 3장 (중복 없음, 시드 기반)
4. `CHMUI.ShowUIAsync(EUI.CardSelectionPopup, arg)` → **카드 선택 팝업** 표시
5. 플레이어가 카드 1장 클릭 → `card.Effect.Apply(_ctx)` 실행 + `_vm.AddPick(card, isPassive:true)`
6. `PauseService.Resume()` → `Time.timeScale = 1f` (게임 재개)
7. 큐에 남은 트리거가 있으면 즉시 다음 처리

**우선순위**: 패시브와 액티브가 동시 트리거되면 **패시브 먼저** 처리.

---

## 4. 액티브 카드 트리거 — 시간 임계점

`ActiveTriggerService` 가 `BattleClock.Elapsed` 를 구독해 경과 초를 검사.

| 트리거 시각 | 남은 시간 (HUD 기준) |
|---|---|
| 0:30 (30s 경과) | 4:30 |
| 1:00 (60s 경과) | 4:00 |
| 1:30 (90s 경과) | 3:30 |
| 2:00 (120s 경과) | 3:00 |
| 2:30 (150s 경과) | 2:30 |
| 3:00 (180s 경과) | 2:00 |
| 3:30 (210s 경과) | 1:30 |
| 4:00 (240s 경과) | 1:00 |
| 4:30 (270s 경과) | 0:30 |

**발동 횟수**: 라운드 당 최대 9회.

**처리 흐름**: 패시브와 동일 (`TriggerQueue.Source.Active` 로 enqueue). `CardDeck.Draw(3)` 은 **액티브 풀** 12장에서 뽑음.

**일시정지 중 시스템 동작:**

| 시스템 | timeScale=0 결과 |
|---|---|
| BattleClock | Elapsed 누적 정지 |
| AutoCombatAI | 이동·공격 정지 |
| SimpleMover | 이동 정지 |
| HeroAuraRunner | Tick 정지 (Aura 효과 시간 소비 정지) |
| CHButton.OnClick | 정상 발화 (입력 시스템은 unscaled) |
| CHMUI 팝업 | 정상 표시 (EditorApplication.Update 기반) |

---

## 5. 상태 디버프 시각 표시 (6종)

영웅에 디버프 Aura 가 부착되면 `HeroAuraRunner` 가 `IStatusVisual` 을 구현한 오브젝트를 CHMPool 에서 Pop해 영웅 위치 + Offset 으로 매 프레임 추적. 만료 시 자동 Push.

같은 Aura 재부착 시: `IDistinctHeroAura` 가 아닌 경우 **Remain 연장** (새 인스턴스 무시). 영구(duration < 0) Aura 는 만료 없음.

| 디버프 | EVisual | 메쉬 | 색상 (RGBA) | 스케일 | Offset (영웅 기준) | 대응 효과 |
|---|---|---|---|---|---|---|
| 둔화 | SlowStatus | Sphere | `#0EA5E9` α=0.5 | 0.4 | (0, 0.05, 0) 발밑 | Slow 카드 / Plague 공격 시 |
| 공포 | FearStatus | Cube | `#A855F7` α=1.0 | 0.3 | (0, 1.3, 0) 머리 위 | Fear 카드 |
| 약화 (일시 공격력↓) | WeakenStatus | Cube | `#6B7280` α=1.0 | 0.3 | (-0.5, 0.6, 0) 왼쪽 | Weaken 카드 |
| 공격력 영구 하락 | AttackDownStatus | Cube | `#7F1D1D` α=1.0 | 0.25 | (0.5, 0.6, 0) 오른쪽 | HeroAttackDown 카드 |
| 시간 정지 | TimeStopStatus | Sphere | `#E5E7EB` α=0.3 | 1.5 | (0, 0.5, 0) 영웅 감쌈 | TimeStop 카드 |
| 출혈 | BleedStatus | Sphere | `#DC2626` α=1.0 | 0.25 | (0.4, 0.05, 0) 발밑 옆 | Bleed 카드 |

반투명(α<1.0)은 URP Lit Transparent Surface. 출혈은 영웅 본체 색상 변경이 아닌 **부착물** (HitFlash 충돌 방지).

독 장판(`HeroPoisonAura`): Plane/Cylinder Y=0.05, 연두 반투명 `#84CC16 α=0.5`. 영웅 발에 부착되어 이동 시 따라다님 (`IStatusVisual` 미구현, 자체 관리).

---

## 6. 카드 시스템 (v0.6 리뉴얼)

### 6.1 4축 빌드 정의

| 축 | 키 색 | 테두리 색 코드 | 핵심 몬스터 | 빌드 슬로건 |
|---|---|---|---|---|
| **Tank** | 초록 | `#22C55E` | Wisp · Wraith | "영웅을 묶어 둔다" |
| **Dps** | 빨강 | `#EF4444` | Reaper · Hex | "빠르게 깎는다" |
| **Debuff** | 보라 | `#A855F7` | Plague + 액티브 저주 | "갉아내고 무력화한다" |
| **Swarm** | 검정 | `#1F2937` | Phantom (+Plague 보조) | "머릿수로 압도한다" |

색 선택 근거: 축 색 = 핵심 몬스터 비주얼 색 그대로 → "내가 픽한 카드 색 = 내가 키우는 몬스터 색" 직관 매핑.

### 6.2 카드 28장 마스터 (P=패시브, A=액티브)

**Tank 축 (P4 + A3)**

| ECardId | T | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|---|
| WispHpBoost | P | 끈질긴 위스프 | Wisp 글로벌 HP ×1.5 (현재 필드 소급 적용) | 곱연산 (2픽=×2.25) |
| WraithDamageBoost | P | 망령의 압박 | Wraith 글로벌 HP ×1.5 | 곱연산 |
| SpawnWraith | P | 더 많은 망령 | Wraith 스포너 동시 출력 +1 (영구) | 가산 |
| ReplaceWispsToWraith | P | 망령으로 진화 | Wisp 스포너 → Wraith 출력 영구 변경 | 멱등 (이미 Wraith 면 no-op) |
| IronWill | A | 강철 의지 | 모든 몬스터 받는 데미지 ×0.7, 15s | 지속시간 누적 (잔여+15s) |
| WallOfWisps | A | 위스프 장벽 | 영웅 주변 4방위에 Wisp 즉시 4마리 소환 (캡 적용) | 가산 (2픽=8마리) |
| Berserk(→GuardianRage) | A | 수호자의 분노 | Wisp·Wraith HP ×2.0 + 받는 데미지 ×0.5, 15s | 지속시간 누적 |

**Dps 축 (P4 + A3)**

| ECardId | T | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|---|
| ReaperAtkSpeed | P | 신속한 사신 | Reaper 공격 쿨다운 ×0.7 | 곱연산 |
| HexRangeBoost | P | 저주의 시야 | Hex 사거리 ×1.4 | 곱연산 |
| SpawnReapers | P | 사신 떼거리 | Reaper 스포너 동시 출력 +1 (영구) | 가산 |
| ReplaceReapersToHex | P | 헥스로 진화 | Reaper 스포너 → Hex 출력 영구 변경 | 멱등 |
| Frenzy | A | 광폭화 | 모든 몬스터 공격속도 +50%, 10s | 지속시간 누적 |
| BloodThirst | A | 피의 갈증 | 처치 시 주변 몬스터 HP +30 회복, 30s | 지속시간 누적 |
| MarkOfDeath | A | 죽음의 표식 | 영웅이 받는 데미지 ×1.5, 5s | 지속시간 누적 |

**Debuff 축 (P4 + A3)**

| ECardId | T | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|---|
| PlagueSlowBoost | P | 역병의 손길 | Plague SlowFactor ×0.75 (둔화 강화, 기준 0.8 → 0.6) | 곱연산 |
| SpawnPlagues | P | 역병 증식 | Plague 스포너 동시 출력 +1 (영구) | 가산 |
| HeroPoisonAura | P | 독장판 | 영웅 발밑 독장판 5 DPS, 5s 지속 (영웅 이동 시 따라감) | 지속시간 누적 |
| HeroAttackDown | P | 약화의 저주 | 영웅 공격력 영구 ×0.75 | 곱연산 (2픽=×0.5625) |
| Fear | A | 공포 | 영웅 3s 도주 | 지속시간 누적 |
| Bleed | A | 출혈 | 영웅 이동 시 1s당 HP -2%, 10s | 지속시간 누적 |
| Weaken | A | 무력화 | 영웅 공격력 ×0.5, 10s | 지속시간 누적 |

**Swarm 축 (P4 + A3)**

| ECardId | T | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|---|
| PhantomMoveSpeedBoost | P | 환령의 발걸음 | Phantom 이동속도 ×1.5 | 곱연산 |
| SpawnPhantoms | P | 환령 떼 | Phantom 스포너 동시 출력 +1 (영구) | 가산 |
| SpawnWisps | P | 위스프 떼 | Wisp 스포너 동시 출력 +1 (영구) | 가산 |
| SpawnerHaste | P | 던전 박동 | 모든 스포너 스폰 주기 ×0.8 (영구) | 곱연산 (2픽=×0.64) |
| TimeStop | A | 시간 정지 | 영웅 5s 정지 | 지속시간 누적 |
| Multiply(→SwarmRush) | A | 스웜 러시 | Phantom 즉시 6마리 영웅 근처 소환 (캡 18 truncate) | 가산 (2픽=12마리) |
| Slow | A | 던전의 점성 | 영웅 이속 ×0.5 + 모든 몬스터 이속 ×1.3, 10s | 지속시간 누적 |

### 6.3 2-Layer 시너지

**Layer 1 — 빌드 시너지 (축 카드 누적 픽 수 기준)**

같은 카드 K번 픽도 카운트에 포함. 임계 도달 시 즉시 1회 발화. 이전 Tier 효과는 유지 + 새 Tier 효과 추가.

| 축 | Tier 1 (3장 도달) | Tier 2 (5장 도달) | Tier 3 (7장 도달) |
|---|---|---|---|
| Tank | Wisp·Wraith HP ×1.3 (글로벌 영구) | Wisp·Wraith Power ×1.2 (글로벌 영구) | 필드 캡 +6 (18→24, 영구) |
| Dps | Reaper·Hex Power ×1.3 | Reaper·Hex Cooldown ×0.8 (공속 +25%) | Reaper·Hex Range ×1.3 |
| Debuff | Plague SlowFactor ×0.8 추가 | HeroAttackDown 자동 등록 (영웅 공격력 ×0.85 영구) | 영구 출혈 — 영웅 이동 시 1s당 HP -1%, 라운드 끝까지 |
| Swarm | Phantom·Wisp MoveSpeed ×1.3 | 모든 스포너 주기 ×0.85 (영구) | 모든 스포너 동시 출력 +1 (영구) |

**Layer 2 — 카드 중첩 (같은 카드 K번 픽)**

- 패시브 강화 카드 (HP/Power/속도 배율): 곱연산 누적 (`WispHpBoost` 2픽 = ×1.5² = ×2.25)
- 패시브 추가/교체 카드: 가산 누적 또는 멱등
- 액티브 카드 (지속시간 있음): 진행 중 재픽 시 잔여 시간 + duration 연장
- 와일드 소환 카드: 소환 수 가산 (캡 truncate)

---

## 7. 빌드 패널

HUD 하단에 항상 표시. 픽한 카드를 **패시브 섹션** / **액티브 섹션** 으로 분리해 `CHPoolingScrollView` 로 표시.

### 7.1 BuildPanel (하단 아이콘 미니 패널)

- 패시브 픽: 왼쪽 가로 스크롤 영역 (`BuildIconPoolingScrollView`)
- 액티브 픽: 오른쪽 가로 스크롤 영역 (`BuildIconPoolingScrollView`)
- 각 셀(`BuildIconCell`): 아이콘(`card.Icon`) + 4축 색 프레임 (`CardBorderColors.BorderColorOf(card.Id)`) + ×N 배지 (N≥2 시 표시)
  - 아이콘 PNG 누락 시 4축 색 프레임이 폴백
  - N=1: 배지 숨김 / N≥2: `×N` 텍스트 노출
  - 셀은 raycastTarget=false → 패널 루트 클릭이 셀에 가로막히지 않음
- **패널 루트 클릭 (CHButton)** → `CHMUI.ShowUI(EUI.BuildModalPopup)` 호출

### 7.2 BuildModalPopup (화면 중앙 모달)

- 좌(패시브): Tank→Dps→Debuff→Swarm 순 그룹화, 그룹 내 픽 시간 순 (`EBuildAxis` 기준 정렬)
- 우(액티브): 픽 시간 순 (추가 정렬 없음)
- 빈 섹션: `_passiveEmptyText` / `_activeEmptyText` 라벨 표시
- 팝업 오픈 중 카드 픽 발생 시 `OnBuildChanged` → 자동 갱신
- 닫기: 배경 dim(`#000 α=0.6`) 클릭 (`_dimButton`) 또는 우상단 X 버튼 (`_closeButton`)
- 일시정지 없음: 모달이 열려있어도 전투 진행 (MVP 정책)

### 7.3 BuildSynergyPanel (좌측 4축 시너지 패널 — v0.6 신규)

HUD 좌측에 항상 표시. 4행 각각이 한 축을 나타냄. `OnBuildChanged` 구독 후 매 픽마다 자동 갱신.

| 행 | 라벨 | 색 |
|---|---|---|
| 0 | TANK | 초록 `#22C55E` |
| 1 | DPS | 빨강 `#EF4444` |
| 2 | DEBUFF | 보라 `#A855F7` |
| 3 | SWARM | 검정 `#1F2937` |

각 행 표시 내용: 현재 픽 카운트 / 다음 임계 수치 (`NextThreshold`; -1이면 7+ 달성) / 현재 활성 Tier (0·1·2·3). 임계를 새로 넘으면 `JustCrossed=true` → 시각 펄스 효과. 인터랙션 없음 (표시 전용).

---

## 8. 스포너 상태 패널

HUD 하단 가로 6셀 (인덱스 0→5, 왼쪽→오른쪽 = ring 0°→300° 순서 고정).

### 8.1 각 셀 내용 (SpawnerStatusCell)

| 요소 | 설명 |
|---|---|
| 색칩 (정사각형) | 현재 출력 종 색상 (컨셉 §11.4 매핑) |
| 종명 텍스트 (CHText) | "Wisp" / "Wraith" / "Reaper" / "Hex" / "Plague" / "Phantom" |
| ×N 배지 (CHText) | 동시 출력 수. N≥2 일 때만 노란 `#FBBF24` 텍스트 표시 |
| 진행 바 (Image fillAmount) | 스폰 쿨다운 진행률 0~1. 0~69%: Cool `#60A5FA`, 70~100%: Warm `#F97316` |
| 테두리 (Image) | 기본 투명 (툴팁 폐지로 활성화 없음) |

**진행 바 폴링**: `SpawnerStatusCell.Update()` 에서 매 프레임 `ISpawnerProgress.Progress` 직접 읽음 (VM 이벤트 우회).

**셀 클릭**: v0.6.4에서 툴팁 폐지. 셀 클릭 동작 없음 (`onClick = null`).

**스냅샷 갱신 트리거 (이벤트 기반):**
- `Spawner.OnOutputTypeChanged` → 해당 인덱스 셀 재바인딩
- `Spawner.OnOutputCountChanged` → 해당 인덱스 셀 재바인딩
- `BattleController.OnTypeModifierChanged(EMonster)` → 동일 종 출력 셀 전체 재바인딩

---

## 9. 결과 화면 — ResultPopup

`EndBattle` 호출 시 `CHMUI.ShowUIAsync(EUI.ResultPopup, arg)` 로 표시.

| 요소 | 동작 |
|---|---|
| `_resultText` (CHText) | 승리 시 "승리", 패배 시 "패배" |
| `_restartButton` (CHButton) | 클릭 시 `SceneManager.LoadScene("Battle")` — Battle 씬 재로드 |

재시작 후 새 판: BalanceConfig 수치로 스탯 재적용, 픽 이력 초기화, 타이머 0:00 재시작.

---

## 10. (에디터 전용) LairBalanceWindow 디버그 윈도우

메뉴 `Lair/Balance Window` 로 열기. 플레이 중에만 치트 패널 활성. 비플레이 시 "플레이 모드에서만 사용 가능" 안내 + 히스토리 패널만 표시.

### 10.1 치트 패널 (플레이 모드 한정)

| 버튼 / 컨트롤 | 동작 |
|---|---|
| [강제 패시브 트리거] | 패시브 카드 선택 큐에 즉시 enqueue → `TryProcessNext` |
| [강제 액티브 트리거] | 액티브 카드 선택 큐에 즉시 enqueue → `TryProcessNext` |
| [ECardId 드롭다운] + [카드 즉시 적용] | 선택한 카드 효과를 팝업 없이 즉시 `Apply(_ctx)` |
| [영웅 HP 정수 필드] + [적용] | 목표 HP 로 TakeDamage 또는 Heal 보정 |
| [영웅 즉사] | 현재 영웅 HP 만큼 데미지 → 승리 종료 |
| [전투 종료 — 승리] | `DebugEndBattle(Win)` |
| [전투 종료 — 패배] | `DebugEndBattle(Lose)` |

### 10.2 결과 히스토리 패널

| 요소 | 내용 |
|---|---|
| 직전 판 강조 | 결과 / 사망 시각 / 픽 수 / 생존 몬스터 수 |
| 히스토리 스크롤 뷰 | 전체 누적 판 목록 (최신→과거 역순), 최대 높이 200px |
| [새로고침] | `Logs/lair_runs.jsonl` 재로드 |
| [초기화] | 파일 삭제 후 빈 리스트 |

**로그 파일**: `Logs/lair_runs.jsonl` (프로젝트 루트 기준, `.gitignore` 추적 제외). 한 판 종료 시 JSON Lines 한 줄 append.

```json
{"FinishedAt":"2026-05-31T07:01:00Z","Result":"Win","DeathTime":83.5,"Picks":["WispHpBoost","Fear","Frenzy"],"SurvivingMonsters":12}
```

---

## 11. UI 인터랙션 매트릭스

| # | UI 요소 | 컴포넌트 | 트리거 | 동작 |
|---|---|---|---|---|
| 1 | 카드 선택 팝업 — 카드 1 | `CardView._pickButton` (CHButton) | 클릭 | 해당 카드 효과 Apply + 빌드 카운트 누적 + 팝업 Close + 게임 재개 |
| 2 | 카드 선택 팝업 — 카드 2 | `CardView._pickButton` (CHButton) | 클릭 | 동일 |
| 3 | 카드 선택 팝업 — 카드 3 | `CardView._pickButton` (CHButton) | 클릭 | 동일 |
| 4 | 결과 팝업 — 재시작 | `ResultPopup._restartButton` (CHButton) | 클릭 | `SceneManager.LoadScene("Battle")` |
| 5 | 빌드 패널 루트 | `BuildPanel._rootButton` (CHButton) | 클릭 | `CHMUI.ShowUI(EUI.BuildModalPopup)` |
| 6 | 빌드 모달 — 배경 dim | `BuildModalPopup._dimButton` (CHButton) | 클릭 | `Close(reuse: true)` |
| 7 | 빌드 모달 — X 버튼 | `BuildModalPopup._closeButton` (CHButton) | 클릭 | `Close(reuse: true)` |
| 8 | [에디터] 강제 패시브 트리거 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugForcePassiveTrigger()` |
| 9 | [에디터] 강제 액티브 트리거 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugForceActiveTrigger()` |
| 10 | [에디터] 카드 즉시 적용 | `LairBalanceWindow` EnumPopup + Button | 드롭다운 선택 + 클릭 | `bc.DebugApplyCard(ECardId)` |
| 11 | [에디터] 영웅 HP 설정 | `LairBalanceWindow` IntField + Button | 값 입력 + 클릭 | `bc.DebugSetHeroHp(int)` |
| 12 | [에디터] 영웅 즉사 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugKillHero()` |
| 13 | [에디터] 전투 종료 승리 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugEndBattle(Win)` |
| 14 | [에디터] 전투 종료 패배 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugEndBattle(Lose)` |
| 15 | [에디터] 히스토리 새로고침 | `LairBalanceWindow` GUILayout.Button | 클릭 | `ReloadHistory()` |
| 16 | [에디터] 히스토리 초기화 | `LairBalanceWindow` GUILayout.Button | 클릭 | `ClearHistory()` (파일 삭제) |

**비인터랙션 요소 (표시 전용)**: BattleHud 타이머, 영웅 HP 바, BuildSynergyPanel 4행, SpawnerStatusPanel 6셀 (툴팁 폐지 후 클릭 없음), 상태 visual 6종 부착물.

---

## 12. 자동 정지 / 재개 흐름

`PauseService` 가 중첩 depth 카운터로 관리.

| 상태 | Time.timeScale | 발생 원인 |
|---|---|---|
| 정상 진행 | 1.0f | 초기값, `Resume()` 시 depth가 0이 된 순간 |
| 카드 선택 일시정지 | 0.0f | `TryProcessNext` → `Pause()` (depth=1) |
| 중첩 일시정지 | 0.0f | 카드 선택 중 추가 트리거 발생 시 depth 누적 (depth>1 이어도 timeScale 0 유지) |
| 전투 종료 후 | 0.0f (AI 비활성) | `EndBattle` → `BattleClock.Stop()` + 모든 `AutoCombatAI.enabled = false` |
| 재시작 | — | `SceneManager.LoadScene("Battle")` → 씬 전체 재초기화 |

중첩 규칙: `Pause()` 시 depth++. depth가 1이 된 시점에만 timeScale=0. `Resume()` 시 depth--. depth가 0이 된 시점에만 timeScale=1. `ForcePause()` 는 depth=int.MaxValue/2 (강제 고정).

---

## 13. 쉬운 설명 (비개발자 요약)

Project Lair는 "내가 던전 주인이 되어 침입하는 영웅을 막는" 게임이다. 영웅은 혼자서 알아서 싸우고, 나는 5분 동안 카드를 골라 몬스터들을 점점 강하게 만들면 된다. 영웅의 HP가 10% 줄어들 때마다 카드 3장 중 하나를 고를 수 있고, 30초마다도 한 번씩 카드를 고를 수 있어서 총 최대 18번의 선택 기회가 생긴다. 이번 주 가장 큰 변화는 **카드가 4가지 '컬러'(초록=탱커, 빨강=딜러, 보라=디버프, 검정=스웜)로 재편됐다는 것** — 같은 색 카드를 3장·5장·7장 모을수록 강력한 보너스(시너지)가 즉시 터져 나온다. 즉, 이번 매뉴얼의 포인트는: 카드 28장이 전면 개편되어 어떤 색을 모을지 전략이 생겼고, 화면 왼쪽에 생긴 시너지 패널이 내 빌드 진행 상태를 한눈에 보여준다.
