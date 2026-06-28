# Project Lair — 게임 매뉴얼

> 생성일: 2026-06-28 / 단계: v0.3 / 코드 진실 우선 — spec 과 충돌 시 구현 코드 기준

---

## 1. 한 줄 컨셉

**5분짜리 역방향 보스전 로그라이크.** 플레이어는 던전 주인(영주). 기사 영웅 한 명이 자동으로 던전을 돌파해 오고, 플레이어가 배치한 6종 몬스터 무리가 자동 전투한다. HP 트리거·시간 트리거마다 카드(3택 1)를 골라 덱을 쌓고, 5분 안에 영웅을 처치하면 승리.

---

## 2. 게임 시작 — 씬 흐름

```
앱 시작 → Loading → Village (마을 허브) → 출격 → Battle (5분 런)
              ↑                                        │
              └── 결과 팝업 (소울 +N · XP +N 보상 요약) ──┘
```

### Loading 씬

`LoadingHud` 컴포넌트가 씬에 직접 배치됨 (CHMUI 미사용).

| 요소 | 내용 |
|---|---|
| 진행률 바 | `fillAmount` 0→1 |
| 퍼센트 텍스트 | `0%` ~ `100%` (RoundToInt) |
| 설명 텍스트 | 단계별 로딩 메시지 |

### Village 씬 (마을 허브 — v0.2+)

마을이 사실상 시작 화면을 겸한다 (별도 메인 메뉴 없음).

| 위치 | 요소 |
|---|---|
| 상단 바 좌측 | 소울(영혼석) 보유량 |
| 상단 바 중앙 | 마을 이름 |
| 상단 바 우측 | 영주 Lv + XP 게이지 |
| 좌측 세로 메뉴 | 상점 · 도감 · 기록 |
| 우측 세로 메뉴 | 영웅 · 퀘스트(도전과제) · 영주성 |
| 하단 중앙 | **출격** 버튼 (대형) |
| 3D 중앙 | 선택된 영웅 스켈레톤 모델 (`Skeleton_idle` 루프 재생) |

**메타 저장 (`MetaProfile`)**: 소울 잔액 · 상점 레벨 · 영주 Lv/XP · 도전과제 플래그 · 도감/통계를 로컬 JSON (`Application.persistentDataPath`) 에 저장.  
저장 시점: 보상 정산 직후 + 메타 변경 직후. v0.3 에서 클라우드 백업/복원 추가.

---

## 3. 자동 전투 (Battle 씬)

### 카메라

| 항목 | 값 |
|---|---|
| Position | (0, 12, -8) |
| Rotation | (50, 0, 0) |
| FOV | 60 |
| Clear Color | #1F2937 |

### 기본 구조

- **영웅 (Knight)**: HP 1000. 자동 전진·공격. 플레이어가 직접 조작하지 않는다.
- **스포너 6기**: 영웅 주변 링 형태 고정 배치. 각자 한 종(種)씩 일정 주기로 몬스터 소환.
- **전투**: 소환된 몬스터들이 자동으로 영웅을 공격. 영웅도 자동 반격.
- **필드 캡**: 동시 활성 몬스터 기본 18마리. Tank Tier3 시너지 달성 시 24마리로 확장.

### 승패 조건

| 조건 | 결과 |
|---|---|
| 5분(300초) 내 영웅 HP 0 | **승리** |
| 5분 경과, 영웅 생존 | **패배** |

### 타이머 표시 형식

`M:SS` 형식 (`Mathf.CeilToInt` 올림 적용). 예: 남은 시간 4.1초 → `0:05` 표시.

### 몬스터 6종

| 종 | 색 코드 | 시각 형태 | 주 축 |
|---|---|---|---|
| Wisp | #22C55E (초록) | 구체 scale 0.6 | Tank / Swarm |
| Wraith | #6B7280 (회색) | 정육면체 scale 1.2 | Tank |
| Reaper | #EF4444 (빨강) | 캡슐 scale 0.9 | Dps |
| Hex | #EAB308 (노랑) | 캡슐 scale 0.8 | Dps |
| Plague | #A855F7 (보라) | 정육면체 scale 0.5 (Y 0.3) | Debuff |
| Phantom | #1F2937 (검정) | 구체 scale 0.3 | Swarm |

### 스포너 상태 패널 — 하단 6셀

각 셀의 표시 요소:

| 요소 | 내용 |
|---|---|
| 종 아이콘 (중앙) | 종 스프라이트. 없으면 숨김 (테두리 색으로 폴백) |
| 테두리 색 | 종 대표색 프레임 |
| 종명 | 영문 (`Wisp` / `Wraith` / `Reaper` / `Hex` / `Plague` / `Phantom`) |
| ×N 배지 | 동시 출력 2마리 이상일 때만 노출, 색 #FBBF24 (노랑) |
| 진행 바 Fill | Cool #60A5FA ↔ Warm #F97316 (진행률 ≥70% 구간), 배경 #374151 |
| 남은 시간 | 소수 첫째 자리 초 (`2.5s`), InvariantCulture 고정 |

셀 클릭: 비활성 (v0.6.4 Tooltip 제거).

---

## 4. 패시브 카드 (16장)

**트리거 조건**: 영웅 HP 가 `[90%, 80%, 70%, 60%, 50%, 40%, 30%, 20%, 10%]` 임계값을 **하향 돌파**할 때마다 3택 1 팝업 발동. 최대 9회.

동시 트리거(패시브+액티브 겹침) → `TriggerQueue` 에 쌓여 FIFO 순서대로 처리.

### Tank 축 패시브 — Wisp(초록) / Wraith(회색)

| ECardId | 효과 요약 |
|---|---|
| WispHpBoost | Wisp HP 상시 증가 |
| WraithDamageBoost | Wraith HP 상시 증가 (v0.6 효과 리뉴얼) |
| SpawnWraith | Wraith 스포너 1기 추가 |
| ReplaceWispsToWraith | Wisp·Wraith 공격력 ×1.3 영구 (코드: `WispWraithPowerBoostEffect`) |

### Dps 축 패시브 — Reaper(빨강) / Hex(노랑)

| ECardId | 효과 요약 |
|---|---|
| ReaperAtkSpeed | Reaper 공격 속도 증가 |
| HexRangeBoost | Hex 사거리 증가 |
| SpawnReapers | Reaper 스포너 1기 추가 |
| ReplaceReapersToHex | Reaper·Hex 공격력 ×1.3 영구 (코드: `ReaperHexPowerBoostEffect`) |

### Debuff 축 패시브 — Plague(보라)

| ECardId | 효과 요약 |
|---|---|
| PlagueSlowBoost | Plague 의 둔화 효과 강화 |
| SpawnPlagues | Plague 스포너 1기 추가 |
| HeroPoisonAura | 영웅에 독 오라 부착 (지속 피해) |
| HeroAttackDown | 영웅 공격력 영구 감소 |

### Swarm 축 패시브 — Phantom(검정) / Wisp(초록)

| ECardId | 효과 요약 |
|---|---|
| PhantomMoveSpeedBoost | Phantom 이동 속도 증가 |
| SpawnWisps | Wisp 스포너 1기 추가 (v0.6 Tank→Swarm 축 이동) |
| SpawnPhantoms | Phantom 스포너 1기 추가 |
| SpawnerHaste | 모든 스포너 주기 단축 |

---

## 5. 액티브 카드 (12장)

**트리거 조건**: 경과 시간 `[30, 60, 90, 120, 150, 180, 210, 240, 270]`초마다 3택 1 팝업 발동. 최대 9회.

패시브와 **동일한 `TriggerQueue` + `CardSelectionPopup`** 사용. 패시브와 겹치면 큐에 함께 적재.

### Tank 축 액티브

| ECardId | 효과 요약 |
|---|---|
| IronWill | 아군 피해 감소 (방어 강화) |
| Berserk | 아군 받는 피해 ×0.5 (코드: `GuardianRageEffect` — HP×2.0 효과 제거됨) |
| WallOfWisps | Wisp·Wraith 받는 피해 ×0.75 영구 (코드: `ToughHideEffect`) |

### Dps 축 액티브

| ECardId | 효과 요약 |
|---|---|
| Frenzy | Dps 계열 전투력 일시 강화 |
| BloodThirst | 공격력 상승 효과 (v0.6 Swarm→Dps 축 이동) |
| MarkOfDeath | 영웅에 표식 부착 → 받는 피해 증폭 |

### Debuff 축 액티브

| ECardId | 효과 요약 |
|---|---|
| Fear | 영웅에 `FearStatus` 부착 (공포/이탈, 시각: 보라 큐브 머리 위) |
| Bleed | 영웅에 `BleedStatus` 부착 (이동 시 HP 감소, 시각: 빨강 구체) |
| Weaken | 영웅에 `WeakenStatus` 부착 (방어력 감소, 시각: 회색 큐브) |

### Swarm 축 액티브

| ECardId | 효과 요약 |
|---|---|
| Slow | 영웅 이동 속도 감소 (v0.6 Debuff→Swarm 축 이동) |
| Multiply | Phantom 스포너 주기 ×0.6 영구 (코드: `FastBreedingEffect`) |
| TimeStop | 영웅에 `TimeStopStatus` 부착 — 일시 행동 불능 (시각: 반투명 흰 구체) |

---

## 6. 상태 디버프 (Status Visuals)

영웅에 부착되는 상태 이상. `HeroAuraRunner` 가 매 프레임 위치 추적. `IStatusVisual` 인터페이스 구현.

| EVisual 키 | 형태 | 색 코드 | 알파 | 스케일 | 오프셋 (x, y, z) | 효과 |
|---|---|---|---|---|---|---|
| PoisonAura | Plane | #84CC16 | 0.5 | — | — | 지속 독 피해 |
| SlowStatus | Sphere | #0EA5E9 | 0.5 | 0.4 | (0, 0.05, 0) | 이동 속도 감소 |
| FearStatus | Cube | #A855F7 | 1.0 | 0.3 | (0, 1.3, 0) | 공포 / 이탈 |
| WeakenStatus | Cube | #6B7280 | 1.0 | 0.3 | (−0.5, 0.6, 0) | 방어력 감소 |
| AttackDownStatus | Cube | #7F1D1D | 1.0 | 0.25 | (0.5, 0.6, 0) | 공격력 감소 |
| TimeStopStatus | Sphere | #E5E7EB | 0.3 | 1.5 | (0, 0.5, 0) | 행동 불능 |
| BleedStatus | Sphere | #DC2626 | 1.0 | 0.25 | (0.4, 0.05, 0) | 이동 시 HP −1%/s |

---

## 7. 빌드 패널 / 시너지

### BattleHud 구성

| 요소 | 위치 | 역할 |
|---|---|---|
| Timer (CHText) | 상단 | 남은 시간 `M:SS` (ceil) |
| HpBarView | 상단 | 영웅 현재 HP 비율 + 상태 아이콘 |
| BuildPanel | 좌측 | 픽한 카드 아이콘 — 패시브(위) / 액티브(아래) |
| BuildSynergyPanel | 좌측 | 4축 시너지 현황 |
| SpawnerStatusPanel | 하단 | 6셀 스포너 상태 |

### BuildPanel 아이콘 매핑

| 카드 ECardId | 글자 | 배경색 |
|---|---|---|
| WispHpBoost | `H` | #22C55E (Wisp 초록) |
| WraithDamageBoost | `D` | #6B7280 (Wraith 회색) |
| ReaperAtkSpeed | `S` | #EF4444 (Reaper 빨강) |
| HexRangeBoost | `R` | #EAB308 (Hex 노랑) |
| PhantomMoveSpeedBoost | `M` | #1F2937 (Phantom 검정) |
| PlagueSlowBoost | `P` | #A855F7 (Plague 보라) |
| SpawnWisps | `+` | #22C55E |
| SpawnWraith | `+` | #6B7280 |
| SpawnReapers | `+` | #EF4444 |
| SpawnPlagues | `+` | #A855F7 |
| SpawnPhantoms | `+` | #1F2937 |
| 그 외 | ` ` (공백) | gray |

패널 루트 클릭 → **BuildModalPopup** 팝업.

### BuildSynergyPanel

- 4축 각 1행: 축 아이콘 + 이름 + `현재수/다음임계` + 활성 Tier 마커(1~3개)
- 임계 새로 돌파 시 `JustCrossed` 펄스 효과

**축 색상 및 레이블**:

| 축 | 색 코드 | 레이블 |
|---|---|---|
| Tank | #22C55E | TANK |
| Dps | #EF4444 | DPS |
| Debuff | #A855F7 | DEBUFF |
| Swarm | #1F2937 | SWARM |

패널 루트 클릭 → **SynergyModalPopup** 팝업.

### 축 시너지 효과표 — 임계 3 / 5 / 7장 (코드: `SynergyModalPopup.TierDesc`)

| 축 | Tier 1 (3장) | Tier 2 (5장) | Tier 3 (7장) |
|---|---|---|---|
| TANK | Wisp·Wraith HP ×1.3 | Wisp·Wraith Power ×1.2 | 필드 캡 +6 (18→24) |
| DPS | Reaper·Hex Power ×1.3 | Reaper·Hex 공속 +25% | Reaper·Hex Range ×1.3 |
| DEBUFF | Plague 둔화 ×0.8 | 영웅 공격력 ×0.85 | 출혈 영구 — 이동 시 1s당 HP −1% |
| SWARM | Phantom·Wisp 이동속도 ×1.3 | 모든 스포너 주기 ×0.85 | 모든 스포너 동시 출력 +1 |

### BuildModalPopup

- 패시브(좌 50%) / 액티브(우 50%) 2분할 레이아웃
- 패시브: Tank → Dps → Debuff → Swarm 축 순 정렬
- 액티브: 픽 시간 순 (추가 정렬 없음)
- 빈 섹션 = "빈 상태" 라벨 표시
- 배경 dim 클릭 / X 버튼 → 닫힘 (`reuse: true`)

### SynergyModalPopup

- 임계 도달 축만 표시 (활성 티어 0개면 빈 상태 텍스트)
- 각 축: 헤더 행 `AXIS (N장)` + 효과 행 `Tier1 … / Tier2 … / Tier3 …`
- 배경 dim 클릭 / X 버튼 → 닫힘 (`reuse: true`)

### CardSelectionPopup (카드 선택 팝업)

| 항목 | 내용 |
|---|---|
| 팝업 오픈 시 | `EAudio.CardSelect` 사운드 재생 |
| 슬롯 수 | 3개 (`CardView`). 미사용 슬롯 `SetActive(false)` |
| 각 슬롯 표시 | 이름 + 설명 + 축 색 테두리 + 아트 이미지 (null이면 숨김) + 픽 카운트 배지 |
| 픽 카운트 배지 | `N/3` 형식 — 0번이면 숨김, 1번이면 `1/3`, 2번이면 `2/3` 표시 |
| 픽 선택 후 | 팝업 닫힘 (`reuse: false`, 매번 새 인스턴스) |

### 3-pick 캡

동일 카드 최대 3회 픽 가능. 3회 도달 카드는 이후 선택지에서 완전히 제외.

---

## 8. 결과 화면 (ResultPopup)

### 표시 요소

| 요소 | 표시 조건 | 포맷 |
|---|---|---|
| 결과 텍스트 | 항상 | `"승리"` / `"패배"` |
| 보상 블록 | `HasMeta == true` | `보상  소울 +N · XP +N` |
| 영주 레벨 업 줄 | `LordLevel != 0` | `영주 레벨 업!  Lv N  +N 소울` (보상 0이면 `+N 소울` 생략) |
| 도전과제 달성 줄 | 달성 1건 이상 | `도전과제 달성!  [이름]  +N 소울` (최대 3줄) |
| 초과 도전과제 | 3건 초과 | `외 N건 달성` |

### 버튼

| 버튼 | 씬 전환 | 비고 |
|---|---|---|
| 다시 도전 | `EScene.Battle` 즉시 로드 | 마을 미경유 — MetaSession 메모리 유지, 메타 보너스 동일 적용 |
| 마을로 | `EScene.Village` 로드 | |

중복 클릭 방지: `_sceneLoadRequested` 플래그 — 첫 클릭만 통과, 이후 클릭 무시.

---

## 9. 에디터 전용 (LairBalanceWindow)

메뉴: `Lair → Balance Debug` (EditorWindow, IMGUI). **플레이 모드에서만 동작**.

### 치트 6종

| 치트 | 동작 |
|---|---|
| Force Passive Trigger | 다음 패시브 임계 즉시 발동 → CardSelectionPopup 표시 |
| Force Active Trigger | 다음 액티브 임계 즉시 발동 → CardSelectionPopup 표시 |
| Apply Card (ECardId 선택) | 특정 카드 효과 즉시 덱에 적용 |
| Set Hero HP (int 입력) | 영웅 HP 임의 설정 |
| Kill Hero | 영웅 즉사 (패배 트리거) |
| End Battle (Win / Lose 토글) | 전투 즉시 종료 |

### 런 히스토리

`Logs/lair_runs.jsonl` 파일에서 과거 런 기록 불러와 표시.

`RunRecord` 필드:

| 필드 | 내용 |
|---|---|
| FinishedAt | 종료 타임스탬프 |
| Result | Win / Lose |
| DeathTime | 영웅 사망 경과 초 |
| Picks | 픽한 카드 ID 문자열 목록 |
| SurvivingMonsters | 생존 몬스터 수 |

---

## 10. UI 인터랙션 매트릭스

### 팝업 오픈/닫기 요약

| UI | 오픈 트리거 | 오픈 방법 | 닫기 | reuse |
|---|---|---|---|---|
| BattleHud | Battle 씬 시작 | `CHMUI.ShowUI(EUI.BattleHud, BattleHudArg)` | 씬 전환 | true |
| CardSelectionPopup | 패시브/액티브 임계 돌파 | `TriggerQueue` → `CHMUI.ShowUI(EUI.CardSelectionPopup, CardSelectionArg)` | 카드 선택 | false |
| BuildModalPopup | BuildPanel 루트 클릭 | `CHMUI.ShowUI(EUI.BuildModalPopup, BuildModalPopupArg)` | dim 또는 X 클릭 | true |
| SynergyModalPopup | BuildSynergyPanel 루트 클릭 | `CHMUI.ShowUI(EUI.SynergyModalPopup, SynergyModalPopupArg)` | dim 또는 X 클릭 | true |
| ResultPopup | 전투 종료 이벤트 | `CHMUI.ShowUI(EUI.ResultPopup, ResultPopupArg)` | 버튼 클릭 → 씬 전환 | — |
| LoadingHud | Loading 씬 직접 배치 | 씬 내 정적 배치 (CHMUI 미사용) | 씬 전환 | — |

### 이벤트 구독 흐름

| 이벤트 | 발화자 | 구독자 | 동작 |
|---|---|---|---|
| `OnTimerChanged` | `BattleViewModel` | `BattleHud` | 타이머 텍스트 갱신 (M:SS ceil) |
| `OnHeroHpValuesChanged` | `BattleViewModel` | `BattleHud` | HpBarView 갱신 |
| `OnStatusIconAdded` | `BattleViewModel` | `BattleHud` → `HpBarView` | 상태 아이콘 추가 |
| `OnStatusIconRemoved` | `BattleViewModel` | `BattleHud` → `HpBarView` | 상태 아이콘 제거 |
| `OnBattleEnded` | `BattleViewModel` | `BattleHud` | ResultPopup 표시 |
| `OnBuildChanged` | `BattleViewModel` | `BuildPanel` | 패시브/액티브 아이콘 ScrollView 갱신 |
| `OnBuildChanged` | `BattleViewModel` | `BuildSynergyPanel` | 4축 시너지 행 갱신 + JustCrossed 펄스 |
| `OnBuildChanged` | `BattleViewModel` | `BuildModalPopup` (열려있을 때) | 모달 내용 실시간 반영 |
| `OnBuildChanged` | `BattleViewModel` | `SynergyModalPopup` (열려있을 때) | 모달 내용 실시간 반영 |
| `OnSpawnerSnapshotChanged(index)` | `BattleViewModel` | `SpawnerStatusPanel` | 해당 인덱스 셀 스냅샷 교체 |

### 주요 가드 / 동기화 포인트

| 상황 | 처리 방식 |
|---|---|
| ResultPopup 버튼 중복 클릭 | `_sceneLoadRequested` 플래그 — 첫 클릭만 씬 로드 요청 통과 |
| 패시브+액티브 동시 트리거 | `TriggerQueue` FIFO — 한 번에 팝업 1개씩 순서 처리 |
| PauseService 중첩 | depth 카운터 — `Pause()` 시 +1, `Resume()` 시 −1; depth>0 이면 `Time.timeScale=0` |
| BuildPanel OnEnable | `LayoutRebuilder.ForceRebuildLayoutImmediate` 선행 → viewport 0 고착 방지 |
| BuildModalPopup / SynergyModalPopup OnEnable | `ForceRebuildLayoutImmediate` → `Build()` 순서로 재오픈 시 최신 픽 반영 |
| SpawnerStatusCell Update | `ISpawnerProgress.Progress` 매 프레임 폴링 — 진행 바·남은 초 표시 |

### UIArg 전달 인자 요약

| UIBase | UIArg 주요 필드 |
|---|---|
| `BattleHud` | `ViewModel`, `Spawners` (IReadOnlyList\<Spawner\>), `Balance` (BalanceConfig), `CardIcons` (IReadOnlyDictionary\<ECardId, Sprite\>) |
| `CardSelectionPopup` | `Choices` (IReadOnlyList\<CardData\>), `OnPicked` (Action\<CardData\>), `PickCountOf` (Func\<CardData, int\>) |
| `BuildModalPopup` | `ViewModel` (BattleViewModel) |
| `SynergyModalPopup` | `ViewModel` (BattleViewModel) |
| `ResultPopup` | `Result` (BattleResult), `HasMeta` (bool), `SoulsGained`, `XpGained`, `LordLevel`, `LordRewardSouls`, `NewlyAchieved` (List\<AchievementDef\>) |

---

## 11. 쉬운 설명 — 처음 하는 사람을 위한 가이드

### 이 게임의 목표

당신은 **던전의 주인(영주)** 입니다. 용감한 기사 영웅 한 명이 당신의 던전을 쳐들어옵니다. **5분 안에 영웅의 체력을 0으로 만들면 승리** 입니다.

### 기본 흐름

1. **마을 → 출격**: 앱을 켜면 마을 화면이 뜹니다. 하단 「출격」 버튼을 눌러 전투를 시작하세요.
2. **전투 자동 진행**: 영웅과 몬스터들은 자동으로 싸웁니다. 조이스틱을 움직일 필요가 없습니다.
3. **카드 선택이 핵심**: 영웅이 체력을 잃거나 시간이 지날수록 **카드 3장 중 1장을 고릅니다**. 어떤 카드를 고르느냐가 전략의 전부입니다.

### 카드를 어떻게 골라야 할까?

- **같은 축(계열) 카드를 집중적으로 모으세요**: 3장 → 5장 → 7장마다 강력한 시너지 효과가 발동됩니다.
  - **TANK** (Wisp 초록 + Wraith 회색): 아군 체력·방어력 강화
  - **DPS** (Reaper 빨강 + Hex 노랑): 아군 공격력·속도·사거리 강화
  - **DEBUFF** (Plague 보라): 영웅을 약화·둔화
  - **SWARM** (Phantom 검정 + Wisp 초록): 몬스터를 더 빠르게, 더 많이 소환
- **같은 카드를 반복 픽**: 동일 카드는 최대 3번까지 고를 수 있고, 누적 효과가 쌓입니다.

### 화면 보는 법

| 위치 | 표시 내용 | 의미 |
|---|---|---|
| 상단 타이머 | `4:59` → `0:00` | 남은 시간. 0이 되기 전에 영웅을 처치해야 합니다 |
| 상단 HP 바 | 채워진 정도 | 영웅의 현재 체력. 줄어들수록 유리합니다 |
| 하단 6개 칸 | 종 아이콘 + 진행 바 | 지금 소환된 몬스터 종류와 다음 소환까지 남은 시간 (파랑 → 주황이면 곧 소환) |
| 좌측 아이콘 열 | 카드 기호 | 지금까지 고른 카드들. **클릭하면 전체 목록** 팝업 |
| 좌측 하단 행 | TANK / DPS / DEBUFF / SWARM | 각 축 카드 수 + 시너지 단계. **클릭하면 시너지 상세** 팝업 |

### 결과 화면

- **승리/패배** 표시 후 획득한 **소울(영혼석)** 과 **경험치(XP)** 가 표시됩니다.
- **다시 도전**: 마을을 거치지 않고 즉시 재도전. 메타 업그레이드 보너스는 그대로 유지됩니다.
- **마을로**: 마을로 돌아가 소울로 영구 업그레이드를 구매하거나 영주 레벨 보상을 확인합니다.

### 마을에서 할 수 있는 것

| 메뉴 | 내용 |
|---|---|
| 상점 | 소울로 영구 업그레이드 구매 (모든 런에 누적 적용) |
| 영주성 | 런을 거듭하며 올라가는 영주 레벨 보상 확인 |
| 퀘스트 | 도전과제 달성 시 추가 소울 지급 |
| 도감 / 기록 | 만난 몬스터·사용한 카드 기록, 통계(총 런 수·승률·최단 클리어 등) 확인 |

### 패배해도 괜찮은 이유

패배해도 영웅의 체력을 깎은 비율에 비례해 소울을 일부 받습니다. **소울은 항상 모입니다**. 상점에서 영구 업그레이드를 쌓으면 이후 런이 더 유리해집니다.
