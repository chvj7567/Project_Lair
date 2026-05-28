# Project Lair — 게임 설명서 (2026-05-28 KST)

> 자동 생성 (매주 월 07:01 KST) — Rule 01 자동화 예외 루틴이 생성/갱신.  
> 생성 기준: spec/design 문서 + UI 코드 종합. 코드 ↔ spec 모순 시 코드 진실.

---

## 0. 한 줄 컨셉

5분짜리 역방향 보스전 로그라이크. 플레이어는 던전 주인. 영웅 한 명이 자동으로 던전을 돌파해오고, 반지름 14유닛 원에 배치된 Spawner 6개에서 몬스터가 끊임없이 흘러나와 자동 전투한다. 영웅 HP 10%마다 패시브 카드(3택 1), 30초마다 액티브 카드(3택 1)를 골라 5분 안에 영웅을 처치하면 승리.

---

## 1. 게임 시작 — Battle 씬 진입

앱 실행 → 메인 메뉴 없이 **Battle.unity** 씬이 즉시 로드된다 (MVP 정책: 메인 메뉴·세팅 화면 없음).

씬 로드 직후 `BattleController.Start()` 가 다음 순서로 초기화를 수행한다:

1. Addressables 리소스 초기화 (`CHMResource.Init`)
2. UI 초기화 (`CHMUI.Init`, `CHMPool.Init`)
3. **BattleHud** 팝업 (`EUI.BattleHud`) — 상단 타이머·영웅 HP 바, 하단 스포너 상태 6셀 패널, 빌드 패널 등장
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
| HP | 4000 (BalanceConfig 기준) |
| 공격력 | 50 |
| 공격 쿨다운 | 1.0s |
| 공격 사거리 | 1.5 유닛 |
| 이동속도 | 3.0 |
| 비주얼 | 파랑 Capsule `#3B82F6` 스케일 1.0 |
| AI 행동 | 가장 가까운 살아있는 몬스터로 자동 이동 → 사거리 내 정지 → 자동 공격 반복 |

---

## 2. 자동 전투 진행

### 2.1 Spawner Ring

Spawner 6개가 반지름 **14.0 유닛** 원에 60° 간격으로 균등 배치되어 있다. 몬스터는 ring 바깥에서 중앙(영웅)으로 수렴한다.

| 인덱스 | 각도 | 위치 (x, z) | 초기 종 | 스폰 주기 | 초기 지연 |
|---|---|---|---|---|---|
| 0 | 0° | (14.0, 0.0) | Wisp | 9.0s | 0.0s |
| 1 | 60° | (7.0, 12.1) | Reaper | 12.0s | 0.5s |
| 2 | 120° | (-7.0, 12.1) | Phantom | 6.0s | 1.0s |
| 3 | 180° | (-14.0, 0.0) | Wisp | 9.0s | 1.5s |
| 4 | 240° | (-7.0, -12.1) | Wraith | 20.0s | 2.0s |
| 5 | 300° | (7.0, -12.1) | Hex | 15.0s | 2.5s |

**글로벌 몬스터 캡: 18마리.** 캡 초과 시 해당 Spawner가 해당 주기를 skip(백오프). 액티브 증식 카드(Multiply) 소환도 캡 초과분 truncate.

> Plague를 출력하는 Spawner는 시작 시 없음. SpawnPlagues·PlagueSlowBoost 카드는 풀에 존재하지만 Spawner가 없으면 no-op.

### 2.2 몬스터 스탯 (BalanceConfig 기준값)

| 종 | HP | Power | 이동속도 | Cooldown | Range | 비주얼 (메쉬·색·스케일) |
|---|---|---|---|---|---|---|
| Wisp | 200 | 5 | 1.0 | 1.0s | 1.5 | Sphere `#22C55E` ×0.6 |
| Wraith | 500 | 10 | 0.8 | 1.0s | 1.5 | Cube `#6B7280` ×1.2 |
| Reaper | 100 | 6 | 1.5 | 0.5s | 1.5 | Capsule `#EF4444` ×0.9 |
| Hex | 60 | 9 | 1.4 | 1.0s | 5.0 | Capsule `#EAB308` ×0.8 |
| Plague | 80 | 2 | 1.3 | 1.0s | 1.5 | Cube `#A855F7` ×0.5 (Y 0.3 납작) |
| Phantom | 30 | 2 | 2.4 | 1.0s | 1.5 | Sphere `#1F2937` ×0.3 |

### 2.3 HUD 표시

| 요소 | 위치 | 표시 내용 |
|---|---|---|
| 타이머 | 화면 상단 중앙 | 잔여 시간 `M:SS` — `CeilToInt(remain)` 올림 표시 (예: 잔여 270.0s → "4:30") |
| 영웅 HP 바 | 영웅 머리 위 | `Image.fillAmount` 빨강 `#DC2626`, 배경 `#374151` |
| 스포너 상태 패널 | 화면 하단 | Spawner 0→5 인덱스 순 가로 6셀 (§6.1 참조) |
| 빌드 패널 | 화면 하단 스포너 패널 위 | 픽한 카드 패시브/액티브 섹션 (§6.2 참조) |

### 2.4 종료 조건

| 조건 | 결과 |
|---|---|
| 영웅 HP ≤ 0 | **승리** — ResultPopup "승리" 표시 |
| 경과 시간 ≥ 300s | **패배** — ResultPopup "패배" 표시 |

---

## 3. 패시브 카드 트리거 — HP 임계점

### 3.1 트리거 시점

영웅 HP 비율이 아래 임계점 이하로 처음 떨어지는 순간 1회 발동. 최대 9회. 큰 데미지로 여러 임계점을 한 번에 통과해도 각각 순차 발동(큐에 enqueue 후 TryProcessNext).

| 임계점 | HP 4000 기준 절대값 |
|---|---|
| 90% | 3600 이하 |
| 80% | 3200 이하 |
| 70% | 2800 이하 |
| 60% | 2400 이하 |
| 50% | 2000 이하 |
| 40% | 1600 이하 |
| 30% | 1200 이하 |
| 20% | 800 이하 |
| 10% | 400 이하 |

### 3.2 카드 선택 UI (CardSelectionPopup)

- `PauseService.Pause()` → `Time.timeScale = 0` → 전투 일시정지
- 15장 풀(`CardPool_Passive`)에서 무작위 3택 (중복 없음, `CardDeck.Draw(3)`)
- 카드 클릭 → `card.Effect.Apply(_ctx)` 즉시 실행 → `CardSelectionPopup.Close(reuse:false)` → `PauseService.Resume()` → `Time.timeScale = 1`
- 큐에 항목이 남아있으면 다음 팝업을 즉시 열어 순차 처리

### 3.3 패시브 카드 15장

카드 UI 구성: 흰 배경 `#FFFFFF`, 검정 텍스트, 테두리 색이 카테고리를 구분.

**강화 (6장) — 테두리 초록 `#22C55E`**  
픽 즉시 해당 종의 영구 글로벌 버프 등록. 현재 필드 동일 종에도 즉시 소급 적용. 중첩 픽은 곱연산 누적.

| 카드 ID | 효과 | 배율 |
|---|---|---|
| WispHpBoost | Wisp 최대 HP 영구 버프 | ×1.5 (중첩 ×1.5^N) |
| WraithDamageBoost | Wraith 공격력(Power) 영구 버프 | ×1.5 |
| ReaperAtkSpeed | Reaper 공격 Cooldown 영구 단축 | ×0.7 |
| HexRangeBoost | Hex 사거리 영구 확장 | ×1.4 |
| PhantomMoveSpeedBoost | Phantom 이동속도 영구 버프 | ×1.5 |
| PlagueSlowBoost | Plague 둔화 계수 강화 (기준값 0.8, 1픽 → 0.6 = 영웅 이동속도 60%) | ×0.75 |

**추가 소환 (5장) — 테두리 파랑 `#3B82F6`**  
픽 즉시 해당 종을 출력하는 모든 Spawner의 동시 출력 수 +1 (영구 누적).

| 카드 ID | 효과 |
|---|---|
| SpawnWisps | Wisp 출력 Spawner 전부 동시 출력 +1 |
| SpawnWraith | Wraith 출력 Spawner 전부 동시 출력 +1 |
| SpawnReapers | Reaper 출력 Spawner 전부 동시 출력 +1 |
| SpawnPlagues | Plague 출력 Spawner 전부 동시 출력 +1 (초기 Spawner 없음 → no-op) |
| SpawnPhantoms | Phantom 출력 Spawner 전부 동시 출력 +1 |

**교체 (2장) — 테두리 주황 `#F97316`**  
픽 즉시 매칭 Spawner의 출력 종을 영구 변경. 이미 필드에 나온 몬스터는 그대로.

| 카드 ID | 효과 |
|---|---|
| ReplaceWispsToWraith | Wisp 출력 Spawner → Wraith 출력으로 전환 |
| ReplaceReapersToHexs | Reaper 출력 Spawner → Hex 출력으로 전환 |

**환경 (2장) — 테두리 보라 `#A855F7`**

| 카드 ID | 효과 | 지속 |
|---|---|---|
| HeroPoisonAura | 영웅 발밑에 독 장판 부착 (DPS 5, 1초마다 5 데미지) — 연두 반투명 디스크 `#84CC16 α=0.5` | 무제한 |
| HeroAttackDown | 영웅 공격력 감소 Aura 부착 | 무제한 |

---

## 4. 액티브 카드 트리거 — 시간 임계점

### 4.1 트리거 시점

경과 시간(`BattleClock.Elapsed`)이 아래 구간에 처음 도달할 때 1회씩 발동. 최대 9회.

| 경과 시간 | HUD 타이머 표시 (잔여) |
|---|---|
| 30s | 4:30 |
| 60s | 4:00 |
| 90s | 3:30 |
| 120s | 3:00 |
| 150s | 2:30 |
| 180s | 2:00 |
| 210s | 1:30 |
| 240s | 1:00 |
| 270s | 0:30 |

### 4.2 카드 선택 UI

- 패시브와 동일한 `CardSelectionPopup`, 동일한 `Time.timeScale = 0` 일시정지
- 10장 풀(`CardPool_Active`)에서 무작위 3택
- 패시브와 액티브가 같은 프레임에 큐에 쌓이면 **큐 순서대로 순차 처리** (한 번에 한 팝업)

### 4.3 액티브 카드 10장

**저주 (4장) — 테두리 빨강 `#EF4444` — 영웅 디버프**

| 카드 | 효과 | 지속 |
|---|---|---|
| Fear (공포) | 영웅 AI가 영웅을 도망 방향으로 강제 이동 | 3s |
| Bleed (출혈) | 영웅이 이동할 때마다 최대 HP의 2% 데미지 | 10s |
| Weaken (무력화) | 영웅 공격력 -50% | 10s |
| Slow (둔화) | 영웅 이동속도 -50% | 10s |

**버프 (4장) — 테두리 노랑 `#EAB308` — 몬스터 강화**

| 카드 | 효과 | 지속 |
|---|---|---|
| Berserk (광폭화) | 현재 필드 모든 몬스터 공격속도 +50% | 10s |
| Multiply (증식) | 현재 가장 많은 종의 몬스터 즉시 2배 소환 | 즉발 |
| BloodThirst (피의 갈증) | 몬스터가 처치 시 주변 몬스터 HP +30 | 30s |
| IronWill (강철의지) | 현재 필드 모든 몬스터 받는 데미지 -30% | 15s |

**와일드 (2장) — 테두리 회색 `#6B7280`**

| 카드 | 효과 | 지속 |
|---|---|---|
| TimeStop (시간 정지) | 영웅 완전 정지 (이동·공격 불가) | 5s |
| Frenzy (폭주) | 모든 몬스터 HP -50%, 데미지 +200% | 15s |

---

## 5. 상태 디버프 시각 표시 (6종)

영웅에 디버프 카드 효과가 적용되면 프리미티브 도형이 부착되어 영웅을 따라다닌다. `HeroAuraRunner.Update`가 매 프레임 `hero.position + Offset`으로 도형 위치를 갱신한다.

| 상태 ID | 원인 카드 | 메쉬 | 색 `#RRGGBB` | α | 스케일 | 영웅 기준 오프셋 |
|---|---|---|---|---|---|---|
| SlowStatus | Slow (둔화) | Sphere | `#0EA5E9` | 0.5 반투명 | 0.4 | (0, 0.05, 0) 발밑 |
| FearStatus | Fear (공포) | Cube | `#A855F7` | 1.0 불투명 | 0.3 | (0, 1.3, 0) 머리 위 |
| WeakenStatus | Weaken (무력화) | Cube | `#6B7280` | 1.0 불투명 | 0.3 | (-0.5, 0.6, 0) 왼쪽 옆 |
| AttackDownStatus | HeroAttackDown | Cube | `#7F1D1D` | 1.0 불투명 | 0.25 | (0.5, 0.6, 0) 오른쪽 옆 |
| TimeStopStatus | TimeStop | Sphere | `#E5E7EB` | 0.3 반투명 | 1.5 | (0, 0.5, 0) 영웅 감쌈 |
| BleedStatus | Bleed (출혈) | Sphere | `#DC2626` | 1.0 불투명 | 0.25 | (0.4, 0.05, 0) 발밑 옆 |

- 디버프 만료(Remain ≤ 0) 시 도형이 사라짐 — `Aura.OnDetached()` → `CHMPool.Push(Visual)`
- 영웅 사망·씬 종료 시 `HeroAuraRunner.OnDisable`에서 모든 슬롯의 Aura·Visual 전부 정리
- 독 장판(HeroPoisonAura)은 별도 — 연두 반투명 Cylinder `#84CC16 α=0.5`가 ground에 고정 (영웅 따라가지 않음)

---

## 6. 빌드 패널 및 스포너 상태 패널

### 6.1 스포너 상태 패널 (SpawnerStatusPanel)

화면 하단 가로 6셀. Spawner 인덱스 0→5 순서(ring 0°→300°).

**각 셀(SpawnerStatusCell) 구성 (폭 ≈46px):**

| 구역 | 요소 | 설명 |
|---|---|---|
| 상단 강화 아이콘 row | 좌: Enhance 슬롯 | 적용된 강화 카드 글자(H/D/S/R/M/P) + 종 색 원 배경 (~10px) |
| 상단 강화 아이콘 row | 우: Spawn 슬롯 | 적용된 추가소환 카드 글자(+) + 종 색 원 배경 |
| 본체 | 색칩 | 종 색 정사각형. 동시 출력 N≥2이면 숨겨짐(종명 폭 확보) |
| 본체 | 종명 (CHText) | Wisp / Wraith / Reaper / Hex / Plague / Phantom |
| 본체 | ×N (CHText, 노랑 `#FBBF24`) | 동시 출력 N≥2일 때만 표시 |
| 본체 | 진행 바 (`Image.fillAmount`) | Cool `#60A5FA` (Progress < 70%), Warm `#F97316` (Progress ≥ 70%) |
| 테두리 | 활성 테두리 | 툴팁 열릴 때 노랑 `#FBBF24`, 기본 투명 |

진행 바는 `ISpawnerProgress.Progress`를 매 프레임 폴링(`SpawnerStatusCell.Update`). VM 이벤트 우회.

**강화 아이콘 슬롯 글자·배경색 매핑:**

| 카드 ID | 슬롯 | 글자 | 배경색 (종 색) | 글자색 |
|---|---|---|---|---|
| WispHpBoost | Enhance | H | `#22C55E` 초록 | 검정 |
| WraithDamageBoost | Enhance | D | `#6B7280` 회색 | 검정 |
| ReaperAtkSpeed | Enhance | S | `#EF4444` 빨강 | 검정 |
| HexRangeBoost | Enhance | R | `#EAB308` 노랑 | 검정 |
| PhantomMoveSpeedBoost | Enhance | M | `#1F2937` 검정 | 흰색 |
| PlagueSlowBoost | Enhance | P | `#A855F7` 보라 | 검정 |
| SpawnWisps | Spawn | + | `#22C55E` 초록 | 검정 |
| SpawnWraith | Spawn | + | `#6B7280` 회색 | 검정 |
| SpawnReapers | Spawn | + | `#EF4444` 빨강 | 검정 |
| SpawnPlagues | Spawn | + | `#A855F7` 보라 | 검정 |
| SpawnPhantoms | Spawn | + | `#1F2937` 검정 | 흰색 |

중첩 픽(PickCount ≥ 2)은 아이콘 우측 하단에 `×N` 배지.

**셀 클릭 → SpawnerStatusTooltip (floating 툴팁):**

- 처음 클릭 → 셀 위 툴팁 열림 + 활성 테두리 노랑 표시
- 같은 셀 재클릭 → 툴팁 닫힘 + 테두리 원복
- 다른 셀 클릭 → 이전 툴팁 닫힘 + 새 셀 툴팁 열림

**SpawnerStatusTooltip 내용 (폭 201px, 셀 상단 +8px에 floating):**

- 헤더: `"Spawner #N — 종명 ×출력수"` (CHText)
- 강화 리스트: 적용된 강화 카드 목록 (CHPoolingScrollView `BuffLine`)
  - 예: `WispHpBoost ×2픽 — HP ×2.25 (200 → 450)`
- 적용 강화 없으면: `"적용된 강화 없음"` 한 줄
- 화면 좌우 clamp (4px 안전 마진)
- VM 이벤트 구독 중 — 툴팁 열린 동안 카드 픽이 발생하면 내용 자동 갱신

### 6.2 빌드 패널 (BuildPanel)

픽한 카드 중 **종 강화(Enhance + IsPassive) 6장을 제외**한 나머지를 패시브/액티브 섹션으로 나누어 `BuildIconPoolingScrollView`로 표시.

| 섹션 | 포함 카드 (종류) |
|---|---|
| 패시브 (좌측) | 추가소환 5장 + 교체 2장 + 환경 2장 |
| 액티브 (우측) | 저주 4장 + 버프 4장 + 와일드 2장 |

종 강화 6장은 스포너 상태 패널 셀의 아이콘 row에서 확인.

**셀(BuildIconCell) 구성:**
- 카드 아이콘 이미지 (`card.Icon`), PNG 누락 시 비활성 → 카테고리 색 프레임이 폴백
- 카테고리 색 프레임 (Enhance=초록, Spawn=파랑, Replace=주황, Environment=보라, 기타=회색)
- ×N 배지 (N≥2일 때만 표시, CHText)
- 셀 자체 클릭은 비활성 (raycastTarget=false) — 패널 루트 클릭이 모달을 담당

**패널 루트 클릭 → BuildModalPopup (빌드 상세 모달):**

- 화면 중앙 모달 + 배경 dim (`#000000 α=0.6`)
- 좌(패시브): 카테고리 그룹 순 정렬 (Enhance → Spawn → Replace → Environment), 강화 6장 **포함** 모두 표시
- 우(액티브): 픽 시간 순 (추가 정렬 없음)
- 각 카드: 이름 + 설명 + 중복 픽 ×N
- 닫기: 배경 dim 클릭 또는 우상단 X 버튼 (reuse:true)
- 게임 일시정지 없음 — 모달 열린 상태로 전투 계속 진행

---

## 7. 결과 화면 — ResultPopup

승리 또는 패배 결정 시 `BattleController.EndBattle(result)`가 호출되고:

1. `BattleClock.Stop()` — 타이머 논리 정지
2. 씬 내 모든 `AutoCombatAI.enabled = false` — 영웅·몬스터 전 행동 정지
3. `BattleViewModel.EndBattle(result)` 발행
4. `CHMUI.ShowUIAsync(EUI.ResultPopup, ResultPopupArg)` — 결과 팝업 표시

**팝업 구성:**

| 요소 | 승리 | 패배 |
|---|---|---|
| `_resultText` (CHText) | "승리" | "패배" |
| `_restartButton` (CHButton) | 클릭 → `SceneManager.LoadScene("Battle")` — 씬 재로드 | 동일 |

재시작 클릭 시 `BattleController`가 새로 초기화되어 새 판이 시작된다. `RunRecorder.FinishRun`이 종료 시점에 `Logs/lair_runs.jsonl`에 결과 레코드를 append.

---

## 8. (에디터 전용) 디버그 윈도우

메뉴: **Lair → Balance Window** (`Assets/_Lair/Editor/LairBalanceWindow.cs`)

Unity Inspector에서 플레이 중 치트를 실행하는 IMGUI EditorWindow.

### 8.1 치트 패널 (플레이 중에만 활성)

비-플레이 중에는 "플레이 모드에서만 사용 가능" 안내만 표시. `BattleController` 미발견 시 경고 표시.

| 버튼 / 컨트롤 | 동작 |
|---|---|
| 강제 패시브 트리거 (Button) | 패시브 카드 선택을 큐에 즉시 enqueue → TryProcessNext |
| 강제 액티브 트리거 (Button) | 액티브 카드 선택을 큐에 즉시 enqueue → TryProcessNext |
| ECardId 드롭다운 + 카드 즉시 적용 (Button) | 선택한 카드 효과를 팝업·일시정지 없이 즉시 `Effect.Apply(_ctx)` |
| 영웅 HP (IntField) + 적용 (Button) | 입력값으로 `TakeDamage` 또는 Heal 보정 |
| 영웅 즉사 (Button) | 현재 HP만큼 TakeDamage → Win 종료 |
| 전투 종료 — 승리 (Button) | `EndBattle(BattleResult.Win)` 직접 호출 |
| 전투 종료 — 패배 (Button) | `EndBattle(BattleResult.Lose)` 직접 호출 |

### 8.2 결과 히스토리 패널

- 파일: `Logs/lair_runs.jsonl` (프로젝트 루트, `.gitignore` 대상)
- 각 행: `result` (Win/Lose) / `deathTime` (경과초) / `picks` (ECardId 문자열 목록) / `survivingMonsters` (생존 몬스터 수)
- "히스토리 초기화" 버튼 → 파일 삭제

---

## 9. UI 인터랙션 매트릭스

| # | UI 요소 | 트리거 | 결과 | 일시정지 여부 |
|---|---|---|---|---|
| 1 | CardSelectionPopup — CardView 픽 버튼 (×3 중 1) | 클릭 | 카드 효과 즉시 Apply → 팝업 Close(reuse:false) → Resume(timeScale=1) | 팝업 열린 동안 timeScale=0 |
| 2 | ResultPopup — 재시작 버튼 | 클릭 | `SceneManager.LoadScene("Battle")` → 씬 재로드, 새 판 시작 | 전투 종료 후 |
| 3 | BuildPanel — 패널 루트 영역 | 클릭 | `CHMUI.ShowUI(EUI.BuildModalPopup, …)` — 빌드 상세 모달 열림 | 없음 (전투 계속) |
| 4 | BuildModalPopup — dim 배경 | 클릭 | `Close(reuse:true)` — 모달 닫힘 | 없음 |
| 5 | BuildModalPopup — X 버튼 | 클릭 | `Close(reuse:true)` — 모달 닫힘 | 없음 |
| 6 | SpawnerStatusCell — 닫힌 셀 | 클릭 | SpawnerStatusTooltip 열림 + 활성 테두리 `#FBBF24` 표시 | 없음 |
| 7 | SpawnerStatusCell — 열린 셀 | 재클릭 | SpawnerStatusTooltip 닫힘 + 테두리 투명 복원 | 없음 |
| 8 | SpawnerStatusCell — 다른 셀 | 클릭 | 이전 툴팁 닫힘 + 이전 테두리 복원 → 새 셀 툴팁 열림 + 새 테두리 활성 | 없음 |
| 9 | (에디터) Balance Window — 강제 패시브 트리거 | 버튼 | 카드 선택 큐 enqueue → cardSelectionPopup 열림 (timeScale=0) | timeScale=0 유발 |
| 10 | (에디터) Balance Window — 강제 액티브 트리거 | 버튼 | 카드 선택 큐 enqueue → cardSelectionPopup 열림 (timeScale=0) | timeScale=0 유발 |
| 11 | (에디터) Balance Window — 카드 즉시 적용 | 드롭다운 + 버튼 | 팝업·일시정지 없이 효과 즉시 Apply | 없음 |
| 12 | (에디터) Balance Window — 영웅 HP 설정 | IntField + 버튼 | 목표 HP로 TakeDamage or Heal 보정 | 없음 |
| 13 | (에디터) Balance Window — 영웅 즉사 | 버튼 | 현재 HP만큼 TakeDamage → Win 종료 | 없음 |
| 14 | (에디터) Balance Window — 전투 종료 승리/패배 | 버튼 (×2) | `EndBattle(Win or Lose)` 직접 호출 | 없음 |

---

## 10. 자동 정지 / 재개 흐름

### 10.1 timeScale 상태표

| 게임 상태 | `Time.timeScale` | 영향 |
|---|---|---|
| 전투 중 (정상) | 1.0 | 모든 Update·Tick 정상 동작 |
| 카드 선택 팝업 열림 | 0.0 | BattleClock 정지 / AutoCombatAI 이동·공격 정지 / SimpleMover 정지 / 데미지 발생 없음 |
| 카드 클릭 후 (큐 처리 중) | 0→1 (Resume) | 큐에 항목 남아있으면 즉시 다음 팝업 열어 0으로 재진입 |
| 전투 종료 | 1.0 유지 (`clock.Stop` 논리 정지) | `AutoCombatAI.enabled=false`로 행동 정지. timeScale은 변경 없음 |
| BuildModalPopup 열림 | 1.0 (변경 없음) | 전투 계속 진행 |
| SpawnerStatusTooltip 열림 | 1.0 (변경 없음) | 전투 계속 진행 |

### 10.2 PauseService 중첩 카운터

`PauseService._depth` 카운터로 중첩 일시정지를 관리.

- `Pause()` → `_depth++`, `_depth==1`이면 `Time.timeScale=0`
- `Resume()` → `_depth--`, `_depth==0`이면 `Time.timeScale=1`
- 패시브·액티브 트리거가 겹쳐 큐에 쌓여도 depth 관리로 안전. 큐 처리 루프가 한 번에 한 팝업씩 순차 처리.
- UI 입력 시스템(`CHButton.onClick`)은 `Time.timeScale`과 무관 — 일시정지 중에도 카드 클릭 정상 동작.

---

*spec ↔ 코드 모순 알림*:  
1. 영웅 HP — 컨셉서 §11.3 "1000", `continuous-spawn-round.md` §2 "4000". 코드(BalanceConfig) 기준 **4000** 채택.  
2. Plague 스탯 — 컨셉서 §11.3 "HP 50, DPS 5", 기능 기획서 §4 "HP 80, Power 2". 코드(BalanceConfig) 기준 **HP 80, Power 2** 채택.  
3. 환경 카드 15번 — 컨셉서 §11.3 "영웅 시야 절반 감소", 현재 코드/기획서 "HeroAttackDown". 코드 기준 **HeroAttackDown** 채택.
