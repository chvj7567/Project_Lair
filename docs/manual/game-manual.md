# Project Lair — 게임 설명서 (2026-06-07 KST)

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
| HP | 4,000 (BalanceConfig.asset 기준) |
| 공격력 | 50 |
| 공격 쿨다운 | 1.0s |
| 공격 사거리 | 1.5 유닛 |
| 이동속도 | 3.0 |
| 비주얼 | 파랑 Capsule `#3B82F6` 스케일 1.0 |
| AI 행동 | 가장 가까운 살아있는 몬스터로 자동 이동 → 사거리 내 정지 → 자동 공격 반복 |

### 1.3 Spawner 6개 — Ring 배치 (v0.6 기준)

반지름 **14.0 유닛** 원에 60° 간격 균등 배치.

| # | 각도 | 위치 (x, z) | 종 | 스폰 주기 | 초기 지연 |
|---|---|---|---|---|---|
| 1 | 0° | (14.0, 0.0) | Wisp | 9.0s | 0.0s |
| 2 | 60° | (7.0, 12.124) | Reaper | 12.0s | 0.5s |
| 3 | 120° | (-7.0, 12.124) | Phantom | 6.0s | 1.0s |
| 4 | 180° | (-14.0, 0.0) | **Plague** | **10.0s** | 1.5s |
| 5 | 240° | (-7.0, -12.124) | Wraith | 20.0s | 2.0s |
| 6 | 300° | (7.0, -12.124) | Hex | 15.0s | 2.5s |

> Spawner #4 (180°) 는 v0.6에서 Wisp → **Plague** 로 전환됐다 (`card-renewal.md §5`). Debuff 빌드 축 작동의 전제 조건 — 둔화(Plague 공격) + PlagueSlowBoost 카드 시너지.

**필드 글로벌 캡**: 동시 존재 몬스터 최대 **18마리**. 캡 초과 시 해당 Spawner 는 해당 주기를 skip.

### 1.4 몬스터 6종 기본 스탯 (BalanceConfig.asset 기준)

| 종 | HP | Power | MoveSpeed | Cooldown | Range | 비주얼 |
|---|---|---|---|---|---|---|
| Wisp | 200 | 5 | 1.0 | 1.0s | 1.5 | 초록 Sphere `#22C55E` 스케일 0.6 |
| Wraith | 500 | 10 | 0.8 | 1.0s | 1.5 | 회색 Cube `#6B7280` 스케일 1.2 |
| Reaper | 100 | 6 | 1.5 | 0.5s | 1.5 | 빨강 Capsule `#EF4444` 스케일 0.9 |
| Hex | 60 | 9 | 1.4 | 1.0s | 5.0 | 노랑 Capsule `#EAB308` 스케일 0.8 |
| Plague | 80 | 2 | 1.3 | 1.0s | 1.5 | 보라 Cube `#A855F7` Y-scale 0.3 (납작 큐브) |
| Phantom | 30 | 2 | 2.4 | 1.0s | 1.5 | 검정 Sphere `#1F2937` 스케일 0.3 |

---

## 2. 자동 전투 진행

전투는 플레이어 입력 없이 진행된다. 영웅과 몬스터 모두 `AutoCombatAI` 컴포넌트가 제어한다.

**AI 루프 (매 프레임):**
1. `IsAlive == false` 이면 즉시 return
2. 가장 가까운 살아있는 적을 `CharacterRegistry` 에서 탐색
3. 적이 없으면 `Stop()`
4. 거리 ≤ 사거리 이면 정지 + 쿨다운 확인 후 공격 (`TryAttack`)
5. 거리 > 사거리 이면 `MoveTo(적 위치)`

**카운트다운 타이머:** `BattleClock` 이 `Time.deltaTime` 누적 → HUD 표시는 `ceil` 기준 (예: elapsed=30.001s 시 HUD "4:30" 유지).

**종료 조건:**
- **승리**: 영웅 HP 0 (`Health.OnDied` → `EndBattle(Win)`)
- **패배**: 5:00 도달 (`BattleClock.OnTimeUp` → `EndBattle(Lose)`)

종료 시: 모든 `AutoCombatAI.enabled = false` → `ResultPopup` 표시.

### 2.1 영웅 자동 스킬 — HP 임계점 해금

영웅 HP가 임계점 이하로 내려가면 **스킬 해금 컷신**이 재생되고 해당 스킬이 자동 활성화된다.

| 해금 HP | 스킬명 | 형태 | 피해 | 쿨다운 | 부가 |
|---|---|---|---|---|---|
| **85%** | DashStrike | ±35° 부채꼴, 길이 7.0 | 80 | 3.0s | 넉백 2.0, 집결반경 8.0 |
| **65%** | AoeNova | 원형 반경 3.5 | 100 | 7.0s | 넉백 3.0 |
| **45%** | OrbitingBlade | 구체 3개 공전(반경 1.4, 180°/s) | 15/틱 (0.3s 간격) | 상시 | 구체 반경 0.9 |

> **스펙 vs 코드**: 기획 초안은 90%/60%/30% 였으나 `HeroSkillLoadout.asset` 기준 **85%/65%/45%** 가 실제 적용값.

**스킬 해금 컷신 흐름 (SkillUnlockCutscene):**

```
PauseService.Pause() → timeScale=0
 ↓
카메라 셰이크 (0.4s, 진폭 0.3 유닛, 선형 감쇠)
 ↓
SkillUnlockBannerView 재생 (unscaledDeltaTime):
  슬라이드 인  (왼쪽 밖→중앙) : 0.35s
  홀드 (중앙 유지)            : 1.20s
  슬라이드 아웃 (중앙→오른쪽 밖): 0.35s
  ─────────────────────────────
  합계                        : 1.90s
 ↓
PauseService.Resume() → timeScale=1
```

배너 위치: `anchoredPosition.y = -280` (하단 1/3, 1280×720 기준). 밴드 실폭이 1300px 초과 시 자동 보정.

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
3. `CardDeck.Draw(3)` — 패시브 풀 16장 중 랜덤 3장 (중복 없음, 시드 기반)
4. `CHMUI.ShowUIAsync(EUI.CardSelectionPopup, arg)` → **카드 선택 팝업** 표시
5. 플레이어가 카드 1장 클릭 → `card.Effect.Apply(_ctx)` 실행 + `_vm.AddPick(card, isPassive:true)`
6. `PauseService.Resume()` → `Time.timeScale = 1f` (게임 재개)
7. 큐에 남은 트리거가 있으면 즉시 다음 처리

**우선순위**: 패시브와 액티브가 동시 트리거되면 **패시브 먼저** 처리.

### 3.1 카드 선택 팝업 (CardSelectionPopup)

- 배경 딤 (전체 스트레치)
- 빌드 카운트 바 (4축 × 현재 픽 수 / 다음 임계) — 패널 상단
- 카드 슬롯 3개 가로 배치 (`CardView × 3`)

**카드 한 장 (CardView) 구성:**

| 요소 | 설명 |
|---|---|
| 아트 이미지 | 카드 일러스트 (null 이면 숨김) |
| 테두리 색 | 축 색상 (`CardBorderColors.BorderColorOf(card.Id)`) |
| 축 헤더 | 축 이름 + 색 사각형 (`Tank ■`, `Dps ■`, ...) |
| 카드명 | CHText (한글 displayName) |
| 설명 | CHText (한글 효과 설명) |
| N/3 배지 | 현재 픽 누적 수 (0이면 숨김, **3픽 후 후보 제외**) |
| 선택 버튼 | CHButton — 클릭 시 효과 Apply + 팝업 Close(reuse:false) |

**팝업 오픈 사운드**: `EAudio.CardSelect` (InitUI 시점 1회).

**3픽 캡 (전역, 2026-06-01)**: 모든 카드는 같은 카드 3픽 시 후보에서 영구 제외 (4픽 불가).

### 3.2 패시브 카드 16장 (2026-06-01 에셋 기준)

**Tank 축 P4 (녹색 `#22C55E` — "영웅을 묶어 둔다")**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `WispHpBoost` | 끈질긴 위스프 | Wisp 글로벌 HP ×1.5 (필드 소급 포함) | 곱연산 (2픽=×2.25, 3픽=×3.375) |
| `WraithDamageBoost` | 망령의 압박 | Wraith 글로벌 HP ×1.5 | 곱연산 |
| `SpawnWraith` | 더 많은 망령 | Wraith Spawner 동시 출력 +1 (영구) | 가산 (2픽=+2, 캡 18) |
| `ReplaceWispsToWraith` | 공포의 군세 | Wisp·Wraith 데미지 **+30%** (영구, `WispWraithPowerBoostEffect`) | 곱연산 |

> **스펙 vs 코드**: ReplaceWispsToWraith 원안 = "Wisp 스포너→Wraith 교체 (멱등)" → **현행 = Power ×1.3 강화.** 종 교체 카드 아님.

**Dps 축 P4 (빨강 `#EF4444` — "빠르게 깎는다")**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `ReaperAtkSpeed` | 신속한 사신 | Reaper 공격 쿨다운 ×0.7 (공속 +30%) | 곱연산 |
| `HexRangeBoost` | 저주의 시야 | Hex 사거리 ×1.4 | 곱연산 |
| `SpawnReapers` | 사신 떼거리 | Reaper Spawner 동시 출력 +1 (영구) | 가산 |
| `ReplaceReapersToHex` | 처형 명령 | Reaper·Hex 데미지 **+30%** (영구, `ReaperHexPowerBoostEffect`) | 곱연산 |

> **스펙 vs 코드**: ReplaceReapersToHex 원안 = "Reaper 스포너→Hex 교체 (멱등)" → **현행 = Power ×1.3 강화.** 종 교체 카드 아님.

**Debuff 축 P4 (보라 `#A855F7` — "갉아내고 무력화한다")**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `PlagueSlowBoost` | 역병의 손길 | Plague 둔화 강도 ×0.75 (BaseSlowFactor 0.8→0.6) | 곱연산 |
| `SpawnPlagues` | 역병 증식 | Plague Spawner 동시 출력 +1 (영구) | 가산 |
| `HeroPoisonAura` | 독장판 | 영웅 발밑 독장판 5 DPS, 5s 지속 (이동 시 따라옴) | 지속시간 누적 (잔여+5s) |
| `HeroAttackDown` | 약화의 저주 | 영웅 공격력 영구 ×0.75 | 곱연산 (2픽=×0.5625) |

**Swarm 축 P4 (검정 `#1F2937` — "머릿수로 압도한다")**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `PhantomMoveSpeedBoost` | 환령의 발걸음 | Phantom 이동속도 ×1.5 | 곱연산 |
| `SpawnPhantoms` | 환령 떼 | Phantom Spawner 동시 출력 +1 (영구) | 가산 |
| `SpawnWisps` | 위스프 떼 | Wisp Spawner 동시 출력 +1 (영구) | 가산 |
| `SpawnerHaste` | 던전의 박동 | 모든 Spawner 주기 ×0.8 (영구 가속, 신규) | 곱연산 (2픽=×0.64, 3픽=×0.512 상한) |

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
| SkillUnlockBannerView | unscaledDeltaTime 사용 → 정지 중에도 슬라이드 재생 |

### 4.1 액티브 카드 12장 (2026-06-01 에셋 기준)

**Tank 축 A3**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `IronWill` | 강철 의지 | 모든 몬스터 받는 데미지 -30% (15s) | 지속시간 누적 (`AddBuff` dedup — Remain 연장, 효과량 1배) |
| `WallOfWisps` | 단단한 살갗 | Wisp·Wraith 받는 데미지 -25% **영구** (`ToughHideEffect`) | 멱등 (`AddBuff` dedup — 영구 1개 유지) |
| `Berserk` (효과=`GuardianRageEffect`) | 수호자의 분노 | Wisp·Wraith 받는 데미지 -50% (15s) | 지속시간 누적 (Remain 연장) |

> **스펙 vs 코드**:  
> - WallOfWisps 원안 = "위스프 4마리 즉시 소환" → **현행 = 영구 ToughHide (소환 카드 아님)**.  
> - GuardianRage(Berserk) 원안 = "HP×2.0 + 받피×0.5" → **현행 = 받피 ×0.5 만** (2026-06-01 HP×2.0 제거).

**Dps 축 A3**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `Frenzy` | 광폭화 | 모든 몬스터 공속 +50% (10s) | 지속시간 누적 |
| `BloodThirst` | 피의 갈증 | 처치 시 주변 몬스터 회복 (30s) | 지속시간 누적 |
| `MarkOfDeath` | 죽음의 표식 | 다음 5s간 영웅 받는 데미지 +50% (신규) | 지속시간 누적 (잔여+5s) |

**Debuff 축 A3**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `Fear` | 공포 | 영웅 3s 도주 | 지속시간 누적 |
| `Bleed` | 출혈 | 영웅 **이동 시** HP -2%/s (10s) | 지속시간 누적 |
| `Weaken` | 무력화 | 영웅 공격력 ×0.5 (10s) | 지속시간 누적 |

**Swarm 축 A3**

| ECardId | 한글명 | 효과 요약 | 중첩 정책 |
|---|---|---|---|
| `TimeStop` | 시간 정지 | 영웅 5s 정지 | 지속시간 누적 |
| `Multiply` | 빠른 번식 | Phantom Spawner 주기 ×0.6 **영구** (`FastBreedingEffect`) | 곱연산 (2픽=×0.36, 3픽=×0.216 상한) |
| `Slow` | 던전의 점성 | 영웅 이동속도 ×0.5 + 모든 몬스터 이동속도 ×1.3 (10s) | 지속시간 누적 |

> **스펙 vs 코드**: Multiply 원안 = "SwarmRush (Phantom 6마리 즉시 소환)" 으로 교체 예정이었으나 **미구현**. 현행 `Multiply.asset`("빠른 번식") + `FastBreedingEffect` 잔존.

### 4.2 2-Layer 시너지

**Layer 1 — 축 누적 픽 수 기반 시너지 (같은 카드 K픽도 카운트 포함)**

| 축 | Tier 1 (3장) | Tier 2 (5장) | Tier 3 (7장) |
|---|---|---|---|
| **Tank** | Wisp·Wraith HP ×1.3 (글로벌 영구) | Wisp·Wraith Power ×1.2 (글로벌 영구) | 필드 캡 +6 (18→24, 영구) |
| **Dps** | Reaper·Hex Power ×1.3 | Reaper·Hex Cooldown ×0.8 (공속 +25%) | Reaper·Hex Range ×1.3 |
| **Debuff** | Plague SlowFactor ×0.8 (둔화 강화) | HeroAttackDown 자동 등록 (영웅 공격력 ×0.85 영구) | **영구 출혈** — 영웅 이동 시 HP -1%/s, 라운드 끝까지 |
| **Swarm** | Phantom·Wisp MoveSpeed ×1.3 | 모든 Spawner 주기 ×0.85 (영구) | 모든 Spawner 동시 출력 +1 (영구) |

> Tier 효과는 누적(**Tier1 + Tier2 동시 활성**). 임계 도달 즉시 1회 발화, 라운드당 1회만.  
> 수치는 `BuildSynergyService` 코드 영역 — 에셋으로 검증 불가, 별도 qa-simulator 검증 대상.

**Layer 2 — 같은 카드 K픽 누적 (3픽 캡으로 실효 상한은 3픽 값)**

| 정책 | 적용 카드 |
|---|---|
| 곱연산 누적 | 종 글로벌 스탯 배율 카드 (HP·Power·Cooldown·Range·MoveSpeed) |
| 가산 누적 | Spawner 동시 출력 +N 카드 |
| 지속시간 누적 | 영웅 대상 액티브 디버프/오라 (Fear·Bleed·Weaken·Slow·MarkOfDeath·HeroPoisonAura) |
| 버프 dedup (시한 Remain 연장) | 몬스터 글로벌 버프 (IronWill·Frenzy·GuardianRage / MonsterBuffService 위임) |
| 멱등 영구 | WallOfWisps (ToughHide — 중복 픽해도 영구 buff 1개만) |
| 곱연산 영구 | HeroAttackDown·FastBreeding(Multiply)·SpawnerHaste |

---

## 5. 상태 디버프 시각 표시 (8종)

> **스펙 vs 코드**: 기획 초안 (2026-05-20) 은 영웅 주변 월드스페이스 프리미티브 6종이었으나, **현행 구현은 영웅 HP 바 아래 8종 아이콘 방식** (`hero-status-icons.md` 기준, 6→8종 확대 + 표시 위치 변경).

영웅에 Aura 가 부착되면 `HeroAuraRunner` 가 HP 바 아래 아이콘 슬롯에 해당 카드 아이콘을 표시한다. 만료 시 슬롯 반환, HLG 자동 재정렬.

### 5.1 아이콘 배치 사양

| 항목 | 값 |
|---|---|
| 표시 위치 | HP 바 바로 아래 (`anchoredPosition.y = -2`) |
| HP 바 너비 | 120px → 콘텐츠 110px (좌우 여백 제외) |
| 아이콘 크기 | 12 × 12 px |
| 아이콘 간격 | 2 px |
| 최대 슬롯 수 | 8 |
| 슬롯 배정 | 최초 빈 슬롯 (lowest-free-slot 정책) |
| 재정렬 | 아이콘 만료 시 HLG 자동 reflflow |

### 5.2 상태 아이콘 8종

| 아이콘 상태명 | ECardId | 지속 시간 | 효과 개요 |
|---|---|---|---|
| `SlowAura` | 18 (Slow) | 10s | 영웅 이동속도 ×0.5 |
| `FearAura` | 15 (Fear) | 3s | 영웅 도주 행동 |
| `WeakenAura` | 17 (Weaken) | 10s | 영웅 공격력 ×0.5 |
| `TimeStopAura` | 23 (TimeStop) | 5s | 영웅 행동 완전 정지 |
| `BleedAura` | 16 (Bleed) | 10s | 영웅 이동 시 HP -2%/s |
| `MarkOfDeathAura` | 26 (MarkOfDeath) | 5s | 영웅 받는 데미지 ×1.5 |
| `HeroAttackDownAura` | 14 (HeroAttackDown) | **영구** | 영웅 공격력 ×0.75 (픽마다 곱연산) |
| `EternalBleedAura` | 16 (Bleed) | **영구** | Debuff Tier3 발동 — 영웅 이동 시 HP -1%/s |

---

## 6. 빌드 패널

HUD 하단 및 좌측에 항상 표시.

### 6.1 BuildPanel (하단 아이콘 미니 패널)

- 패시브 픽: 왼쪽 가로 스크롤 영역 (`BuildIconPoolingScrollView`)
- 액티브 픽: 오른쪽 가로 스크롤 영역 (`BuildIconPoolingScrollView`)
- 각 셀(`BuildIconCell`): 아이콘 + 4축 색 프레임 + ×N 배지 (N≥2 시 표시)
  - 셀은 `raycastTarget=false` → 패널 루트 클릭이 셀에 가로막히지 않음
- **패널 루트 클릭** (`CHButton`) → `CHMUI.ShowUI(EUI.BuildModalPopup)` 호출

### 6.2 BuildModalPopup (화면 중앙 모달)

- 좌(패시브): Tank→Dps→Debuff→Swarm 순 그룹화, 그룹 내 픽 시간 순 (`EBuildAxis` 기준 정렬)
- 우(액티브): 픽 시간 순 (추가 정렬 없음)
- 빈 섹션: `_passiveEmptyText` / `_activeEmptyText` 표시
- 팝업 오픈 중 카드 픽 발생 시 `OnBuildChanged` → 자동 갱신
- 닫기: `_dimButton` (배경) 또는 `_closeButton` (X 버튼), `reuse:true`
- **일시정지 없음**: 모달이 열려있어도 전투 진행 (timeScale 유지)

### 6.3 BuildSynergyPanel (좌측 4축 시너지 패널)

HUD 좌상단. 4행 × 축별 픽 수 / 다음 임계 / 활성 Tier 표시.

| 행 | 라벨 | 색 |
|---|---|---|
| 0 | TANK | `#22C55E` |
| 1 | DPS | `#EF4444` |
| 2 | DEBUFF | `#A855F7` |
| 3 | SWARM | `#1F2937` |

표시 형식: `<축이름>  N/<다음 임계>`. 7장 도달 후엔 `N+`.  
티어 마커: 축 아이콘(`SynergyIcons/{TANK,DPS,DEBUFF,SWARM}.png`) 활성 티어 수만큼 반복 (미도달 0개 / Tier1 1개 / Tier2 2개 / Tier3 3개).  
임계 새로 도달 시: **0.3s 펄스 효과** (셀 배경 알파 50% → 100% → 50%, sin 곡선).

**루트 버튼 클릭** → `CHMUI.ShowUI(EUI.SynergyModalPopup)` (일시정지 없음).

### 6.4 SynergyModalPopup (시너지 상세)

활성 티어의 효과 설명만 표시 (미달성 숨김). `_dimButton` 또는 닫기 버튼으로 닫기 (`reuse:true`).

### 6.5 SpawnerStatusPanel (하단 스포너 6셀)

HUD 하단 가로 6셀 (인덱스 0→5, 왼쪽→오른쪽 = ring 0°→300° 순서 고정).

**각 셀 (SpawnerStatusCell) 내용:**

| 요소 | 설명 |
|---|---|
| 색칩 | 현재 출력 종 색상 |
| 종명 텍스트 (CHText) | "Wisp" / "Reaper" / "Phantom" / "Plague" / "Wraith" / "Hex" |
| ×N 배지 (CHText) | 동시 출력 수. N≥2 일 때만 노란 `#FBBF24` 텍스트 |
| 진행 바 (Image fillAmount) | 0~69%: Cool `#60A5FA` / 70~100%: Warm `#F97316` |

**셀 클릭**: **동작 없음** (v0.6.4 에서 툴팁 제거, `onClick = null`).

**진행 바 폴링**: `SpawnerStatusCell.Update()` 에서 매 프레임 `ISpawnerProgress.Progress` 직접 읽음.

---

## 7. 결과 화면 — ResultPopup

`EndBattle` 호출 시 `CHMUI.ShowUIAsync(EUI.ResultPopup, arg)` 로 표시.

| 요소 | 동작 |
|---|---|
| `_resultText` (CHText) | 승리 시 "승리", 패배 시 "패배" |
| `_restartButton` (CHButton) | 클릭 시 `SceneManager.LoadScene("Battle")` — 전체 재초기화 |

재시작 후: BalanceConfig 수치로 스탯 재적용, 픽 이력 초기화, 타이머 0:00 재시작.

---

## 8. (에디터 전용) LairBalanceWindow 디버그 윈도우

메뉴 `Lair/Balance Window` 로 열기. 플레이 모드에서만 치트 패널 활성. 비플레이 시 히스토리 패널만 표시.

### 8.1 치트 패널 (플레이 모드 한정)

| 버튼 / 컨트롤 | 동작 |
|---|---|
| [강제 패시브 트리거] | 패시브 카드 선택 큐에 즉시 enqueue |
| [강제 액티브 트리거] | 액티브 카드 선택 큐에 즉시 enqueue |
| [ECardId 드롭다운] + [카드 즉시 적용] | 팝업 없이 해당 카드 효과 즉시 Apply |
| [영웅 HP 정수 필드] + [적용] | 목표 HP 로 보정 |
| [영웅 즉사] | 현재 HP 만큼 데미지 → 승리 종료 |
| [전투 종료 — 승리] | `DebugEndBattle(Win)` |
| [전투 종료 — 패배] | `DebugEndBattle(Lose)` |

### 8.2 결과 히스토리 패널

전체 누적 판 목록 스크롤 뷰. 로그 파일: `Logs/lair_runs.jsonl` (`.gitignore` 대상).

---

## 9. UI 인터랙션 매트릭스

| # | UI 요소 | 컴포넌트 | 트리거 | 동작 | timeScale 변화 |
|---|---|---|---|---|---|
| 1 | 카드 선택 팝업 — 카드 1 | `CardView._pickButton` (CHButton) | 클릭 | 카드 효과 Apply + 팝업 Close(reuse:false) + 게임 재개 | 0 → 1 |
| 2 | 카드 선택 팝업 — 카드 2 | `CardView._pickButton` (CHButton) | 클릭 | 동일 | 0 → 1 |
| 3 | 카드 선택 팝업 — 카드 3 | `CardView._pickButton` (CHButton) | 클릭 | 동일 | 0 → 1 |
| 4 | 결과 팝업 — 재시작 | `ResultPopup._restartButton` (CHButton) | 클릭 | `SceneManager.LoadScene("Battle")` | — |
| 5 | 빌드 패널 루트 | `BuildPanel._rootButton` (CHButton) | 클릭 | `CHMUI.ShowUI(EUI.BuildModalPopup)` | 변화 없음 |
| 6 | 빌드 모달 — 배경 dim | `BuildModalPopup._dimButton` (CHButton) | 클릭 | `Close(reuse:true)` | 변화 없음 |
| 7 | 빌드 모달 — X 버튼 | `BuildModalPopup._closeButton` (CHButton) | 클릭 | `Close(reuse:true)` | 변화 없음 |
| 8 | 시너지 패널 루트 | `BuildSynergyPanel._rootButton` (CHButton) | 클릭 | `CHMUI.ShowUI(EUI.SynergyModalPopup)` | 변화 없음 |
| 9 | 시너지 모달 — dim | `SynergyModalPopup._dimButton` (CHButton) | 클릭 | `Close(reuse:true)` | 변화 없음 |
| 10 | 시너지 모달 — 닫기 | `SynergyModalPopup._closeButton` (CHButton) | 클릭 | `Close(reuse:true)` | 변화 없음 |
| 11 | [에디터] 강제 패시브 트리거 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugForcePassiveTrigger()` | 0 (큐 처리) |
| 12 | [에디터] 강제 액티브 트리거 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugForceActiveTrigger()` | 0 (큐 처리) |
| 13 | [에디터] 카드 즉시 적용 | `LairBalanceWindow` EnumPopup + Button | 드롭다운+클릭 | `bc.DebugApplyCard(ECardId)` | 변화 없음 |
| 14 | [에디터] 영웅 HP 설정 | `LairBalanceWindow` IntField + Button | 값 입력+클릭 | `bc.DebugSetHeroHp(int)` | 변화 없음 |
| 15 | [에디터] 영웅 즉사 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugKillHero()` | — |
| 16 | [에디터] 전투 종료 승리 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugEndBattle(Win)` | — |
| 17 | [에디터] 전투 종료 패배 | `LairBalanceWindow` GUILayout.Button | 클릭 | `bc.DebugEndBattle(Lose)` | — |
| 18 | [에디터] 히스토리 새로고침 | `LairBalanceWindow` GUILayout.Button | 클릭 | `ReloadHistory()` | 변화 없음 |
| 19 | [에디터] 히스토리 초기화 | `LairBalanceWindow` GUILayout.Button | 클릭 | `ClearHistory()` (파일 삭제) | 변화 없음 |

**비인터랙션 요소 (표시 전용)**: BattleHud 타이머, 영웅 HP 바, 상태 아이콘 8종, BuildSynergyPanel 4행, SpawnerStatusPanel 6셀 (v0.6.4 툴팁 제거 후 클릭 없음).

---

## 10. 자동 정지 / 재개 흐름

### 10.1 PauseService — 중첩 depth 카운터

| 상태 | Time.timeScale | 발생 원인 |
|---|---|---|
| 정상 진행 | 1.0f | 초기값, Resume() 시 depth=0 |
| 카드 선택 일시정지 | 0.0f | TryProcessNext → Pause() (depth=1) |
| 중첩 일시정지 | 0.0f | 카드 선택 중 추가 트리거 (depth>1 이어도 timeScale 0 유지) |
| 스킬 해금 컷신 | 0.0f | SkillUnlockCutscene → Pause() |
| 전투 종료 후 | AI 비활성 | EndBattle → BattleClock.Stop() + 모든 AutoCombatAI.enabled=false |
| 재시작 | — | SceneManager.LoadScene("Battle") → 씬 전체 재초기화 |

```
Pause()  → depth++; if (depth == 1) timeScale = 0
Resume() → depth--; if (depth == 0) timeScale = 1
```

### 10.2 TriggerQueue — 직렬 처리

패시브·액티브 트리거가 동시에 발생하면 `TriggerQueue` 에 순차 enqueue → 하나 완료 후 다음 처리.

### 10.3 정지 발생 시나리오 전체

| 시나리오 | timeScale | 정지 주체 | 총 정지 시간 |
|---|---|---|---|
| 패시브 카드 선택 팝업 | 0 | PauseService | 플레이어 결정 때까지 |
| 액티브 카드 선택 팝업 | 0 | PauseService | 플레이어 결정 때까지 |
| 영웅 스킬 해금 컷신 (×3) | 0 | PauseService | 1.9s/회 (카메라셰이크 0.4s 포함) |
| BuildModalPopup 열람 | 1 유지 | — (정지 없음) | — |
| SynergyModalPopup 열람 | 1 유지 | — (정지 없음) | — |

---

## 11. 쉬운 설명 (비개발자 요약)

Project Lair는 "내가 던전 주인이 되어 침입하는 영웅을 막는" 게임이다. 영웅은 혼자서 알아서 싸우고, 나는 5분 동안 카드를 골라 몬스터들을 점점 강하게 만들면 된다. 영웅의 HP가 10% 줄어들 때마다 카드 3장 중 하나를 고를 수 있고, 30초마다도 한 번씩 카드를 고를 수 있어서 총 최대 18번의 선택 기회가 생긴다.

**이번 주 주요 변경점:**
1. **상태 표시 방식이 바뀌었다** — 영웅 주변에 3D 오브젝트가 붙던 방식에서, HP 바 바로 아래에 작은 카드 아이콘 8개가 뜨는 방식으로 교체됐다.
2. **카드 효과 일부 수정** — "단단한 살갗"(WallOfWisps)이 위스프 소환 카드에서 위스프/레이스가 피를 덜 맞는 카드로 바뀌었고, "공포의 군세"와 "처형 명령"은 몬스터를 교체하는 게 아니라 공격력을 30% 올려주는 카드로 정정됐다.
3. **스포너 #4(180°)가 위스프에서 플레이그로 전환** — 보라색 납작 몬스터가 영웅을 느리게 하는 역할을 맡는다.
4. **영웅 스킬 해금 타이밍이 85%/65%/45%** — 기존 기획 초안(90%/60%/30%)과 다른 실제 적용값. 스킬이 해금될 때마다 화면이 잠깐 멈추고 배너가 슬라이드된다.

즉, 이번 매뉴얼의 포인트는: 카드 효과·상태 표시·스포너 구성이 코드 기준으로 갱신됐으며, 영웅이 HP 85%/65%/45%에서 자동 스킬을 해금하므로 초반에 카드를 잘 골라 몬스터 군단을 키워야 5분 안에 이긴다.
