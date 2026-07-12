# Project Lair — 게임 매뉴얼

> 자동 생성: 주간 스케줄 루틴 · 최종 갱신 2026-07-12
> 코드 기준 (spec ↔ 코드 불일치 시 코드 우선)
> 단계: v0.3 (서버 연동 클라이언트)

---

## 0. 컨셉 개요

**5분짜리 역방향 보스전 로그라이크.** 플레이어는 던전 주인(영주). 기사 영웅 한 명이 자동으로 던전을 돌파해 오고, 플레이어가 배치한 6종 몬스터 무리가 자동 전투한다. HP 트리거·시간 트리거마다 카드(3택 1)를 골라 덱을 쌓고, 5분 안에 영웅을 처치하면 승리.

### 4축 빌드 정체성

| EBuildAxis | 테마 | 패시브 특징 | 액티브 특징 |
|---|---|---|---|
| Tank | 두꺼운 방어 라인 | HP·방어력 강화, Wraith 소환·교체 | IronWill(무적), WallOfWisps(ToughHide), GuardianRage(Berserk) |
| Dps | 빠른 화력 | 공속·범위·Reaper 소환·교체 | Frenzy(공속), BloodThirst(피흡), MarkOfDeath(5s 표식) |
| Debuff | 디버프 지속압박 | 둔화·Plague 소환·독 장판·공격력 하향 | Fear(3s), Bleed(10s/무기한), Weaken(10s) |
| Swarm | 물량 공세 | 이동속도·Phantom·Wisp 소환·스포너 가속 | TimeStop(5s), Multiply(분열), Slow(10s) |

카드는 패시브 4장 + 액티브 3장 = 축당 7장, 전체 28장. 같은 카드를 최대 3회 중복 픽 가능(글로벌 상한).

---

## 1. 로딩 씬 → Battle 씬 진입 & 초기화

### 1.1 Loading 씬 — LoadingController + LoadingHud

배틀 진입 전 Loading 씬을 경유한다. `LoadingController`(MonoBehaviour)가 씬에 직접 배치되며, `LoadingHud`를 통해 진행률을 표시한다.

| 단계 | 동작 |
|---|---|
| Addressables 카탈로그 초기화 | `CHMResource.Instance.Init()` |
| 에셋 로드 | 키별 진행률 → `LoadingHud.SetProgress(ratio, desc)` |
| 로드 완료 | `LoadingHud.SetDescText("준비 완료 - 클릭하여 시작")` 표시 |
| 클릭 | `LoadingController._clickArea(CHButton)` 클릭 → Battle 씬 로드 |

`LoadingHud` 구성: `_progressFill`(Image fillAmount), `_percentText`(CHText — `{N}%`), `_descText`(CHText). CHMUI로 로드하지 않고 씬 직접 배치.

### 1.2 Battle 씬 고정값

| 항목 | 값 |
|---|---|
| 씬 이름 | `Battle` (EScene.Battle) |
| 카메라 위치 | (0, 12, −8) |
| 카메라 회전 | (50, 0, 0) |
| 배경 바닥 | Plane 30×30 |

### 1.3 BattleController.Start() 순서

`BattleController`(MonoBehaviour)가 `async Start()`에서 아래 순서로 초기화한다.

1. `BattleStateModel` 생성 (결과·HP 트래킹)
2. `BattleViewModel` 생성 (HUD 바인딩용 가공 계층)
3. `PauseService` 생성 (depth-counted `Time.timeScale`)
4. `BattleClock` 생성 (300s 카운트다운, OnTick 이벤트)
5. `TriggerQueue` 생성 (Passive/Active 순차 처리)
6. `PassiveTriggerService` 생성 → HP 이벤트 구독
7. `ActiveTriggerService` 생성 → BattleClock.OnTick 구독
8. Addressables 비동기 로드 — 영웅·몬스터·FX·UI 프리팹, CardPool SO
9. CHMPool 사전 워밍 (영웅 1, 몬스터 3+, 발사체 등)
10. `SkillUnlockCutsceneController` 생성 (PauseService + ICameraShake + ISkillUnlockBanner)
11. CHMUI로 BattleHud 표시, BattleClock 시작

---

## 2. 자동 전투 루프

### 2.1 타이머

- `BattleClock`이 매 프레임 `elapsed`(경과초)를 누적, `OnTick(elapsed)` 발행
- HUD `_timerText`에 `Mathf.CeilToInt(300 − elapsed)` 를 MM:SS로 표시
- 300초 도달 또는 영웅 HP ≤ 0 / 전체 HP 소진 시 전투 종료

### 2.2 스포너 링 (6개)

중심 (0,0,0) 기준 반경 14.0, 60° 간격으로 6개 스포너 배치.

| 스포너 인덱스 | 각도 | 기본 몬스터 |
|---|---|---|
| 0 | 0° | Wisp |
| 1 | 60° | Slime |
| 2 | 120° | Wisp |
| 3 | 180° | Golem |
| 4 | 240° | Wisp |
| 5 | 300° | Orc |

글로벌 필드 캡: 18마리. 개별 스포너도 스폰 주기·출력 상한 존재.

### 2.3 몬스터 스탯

| 몬스터 | HP | 공격력 | 이동속도 | 비고 |
|---|---|---|---|---|
| Wisp | 80 | 10 | 3.5 | 기본 물량 |
| Slime | 120 | 15 | 2.5 | 중간 |
| Golem | 400 | 25 | 1.5 | 탱커 |
| Orc | 200 | 30 | 2.0 | 중근거리 |
| Reaper | 100 | 20 | 4.0 | 고속 DPS |
| Wraith | 300 | 22 | 2.2 | 방어형 |
| Phantom | 60 | 8 | 5.0 | 초고속 물량 |
| Plague | 50 | 12 | 2.8 | 독 장판 보유 (live BalanceConfig 기준; spec 80은 구버전) |

### 2.4 영웅 자동 스킬 (Hero Skills)

영웅 HP 4,000. 아래 HP 비율 임계점에서 스킬 페이즈가 열리고, **스킬 해금 컷신**이 재생된다.

| 페이즈 | HP 비율 | 스킬 | 상세 |
|---|---|---|---|
| P1 | ≤ 85% | DashStrike | 부채꼴 ±35°, 데미지 80, 쿨다운 3.0s |
| P2 | ≤ 65% | AoeNova | 반경 3.5, 데미지 100, 쿨다운 7.0s, 넉백 3.0 |
| P3 | ≤ 45% | OrbitingBlade | 구체 3개 궤도 R=1.4 r=0.9, 타격 15, 간격 0.3s, 180°/s 회전 |

각 페이즈는 해당 HP 이하 진입 시 영구 활성화(중첩 사용). 해금 시 `SkillUnlockCutsceneController.Enqueue(skillName)` 호출.

### 2.5 스킬 해금 컷신 — SkillUnlockCutsceneController

영웅 스킬이 처음 열릴 때 재생되는 연출 시퀀스(POCO, 코루틴 오케스트레이터).

```
Enqueue(skillName)
  → PauseService.Pause()            // Time.timeScale = 0
  → EAudio.AcquireSkill 사운드
  → ICameraShake.Shake(0.4s, 0.3)  // 카메라 흔들림
  → SkillUnlockBannerView.PlayCo   // 배너 슬라이드 연출
      └ 왼쪽 밖 → 중앙 (0.35s SmoothStep)
        홀드 (1.2s)
        중앙 → 오른쪽 밖 (0.35s SmoothStep)
  → PauseService.Resume()           // Time.timeScale = 1
```

복수 스킬이 동시에 해금되면 큐에 쌓아 순차 재생. `SkillUnlockBannerView._label`에 `"영웅의 '{skillName}' 스킬 해제"` 텍스트 설정. `Time.unscaledDeltaTime` 기반이므로 Pause 중에도 연출 진행.

---

## 3. 패시브 트리거 & 카드 픽

### 3.1 HP 임계점 9개

`PassiveTriggerService`가 영웅 HP 비율을 감시. 한 번 발동된 임계점은 재발동 없음(idempotent).

| 임계점 | 영웅 HP 비율 | 순서 |
|---|---|---|
| 1 | 90% | 첫 번째 패시브 픽 |
| 2 | 80% | |
| 3 | 70% | |
| 4 | 60% | |
| 5 | 50% | |
| 6 | 40% | |
| 7 | 30% | |
| 8 | 20% | |
| 9 | 10% | 마지막 패시브 픽 |

### 3.2 처리 흐름

```
PassiveTriggerService.OnTriggered(idx)
  → TriggerQueue.Enqueue(Source.Passive, idx)
  → BattleController.TryProcessNext()
      1. PauseService.Pause()   // Time.timeScale = 0
      2. _passiveDeck.Draw(3)   // 패시브 풀에서 3장 무작위
      3. CHMUI.ShowUIAsync(EUI.CardSelectionPopup, arg)
      4. 플레이어 픽 대기 (TaskCompletionSource)
      5. card.Effect.Apply(ctx)
      6. BattleViewModel.AddPick(card, isPassive:true)
      7. PauseService.Resume()  // Time.timeScale = 1
```

큐 처리 중 새 트리거 도착 시 `_processingQueue` 가드가 중첩 진입을 막고, 현재 픽 완료 후 자동 이어서 처리.

### 3.3 CardSelectionPopup

- 3장 `CardView` 슬롯 나란히 배치
- `EAudio.CardSelect` 사운드 재생 (팝업 오픈 시)
- 각 `CardView`: 축 색 테두리, 카드 아트 이미지(`CardImage`), 이름·설명·중첩 배지(N/3)
- 픽 완료 → 팝업 닫힘 → 게임 재개

---

## 4. 액티브 트리거 & 카드 픽

### 4.1 30초 임계점 9개

`ActiveTriggerService`가 `BattleClock.OnTick`을 구독. 각 임계점 1회만 발동.

| 임계점 | 경과 시간 |
|---|---|
| 1 | 0:30 |
| 2 | 1:00 |
| 3 | 1:30 |
| 4 | 2:00 |
| 5 | 2:30 |
| 6 | 3:00 |
| 7 | 3:30 |
| 8 | 4:00 |
| 9 | 4:30 |

### 4.2 처리 흐름

패시브와 동일한 `TriggerQueue` 경유. `Source.Active`로 구분.

```
ActiveTriggerService.OnTriggered(idx)
  → TriggerQueue.Enqueue(Source.Active, idx)
  → BattleController.TryProcessNext()
      1. PauseService.Pause()
      2. _activeDeck.Draw(3)    // 액티브 풀에서 3장 무작위
      3. CHMUI.ShowUIAsync(EUI.CardSelectionPopup, arg)
      4. 플레이어 픽 대기
      5. card.Effect.Apply(ctx) // 즉발 효과
      6. BattleViewModel.AddPick(card, isPassive:false)
      7. PauseService.Resume()
```

패시브·액티브가 같은 프레임에 동시 트리거 시 둘 다 큐에 진입하고 dequeue 순서대로 순차 처리.

---

## 5. 카드 28장 목록

중복 픽 상한: 글로벌 3회. 각 카드 `[중첩]` 항목은 중복 픽 시 누적 동작을 나타냄.

### 5.1 Tank 축 (EBuildAxis.Tank)

**패시브 카드**

| ECardId | 한글명 | 효과 | 중첩 |
|---|---|---|---|
| WispHpBoost | 위습 HP 강화 | Wisp HP +30% | 누적 ×N |
| WraithDamageBoost | 레이스 공격력 강화 | Wraith 공격력 +25% | 누적 ×N |
| SpawnWraith | 레이스 소환 | 레이스 1마리 즉시 소환 + 스포너 출력 +1 | 소환 반복 |
| ReplaceWispsToWraith | 위습→레이스 교체 | 필드 위습 전체 → 레이스로 교체 | 반복 교체 |

**액티브 카드**

| ECardId | 한글명 | 효과 | 지속 |
|---|---|---|---|
| IronWill | 강철의지 | 모든 Tank 몬스터 무적 | 5s |
| WallOfWisps | 위습 방벽 (ToughHide) | 모든 Wisp 방어력 +50% | 8s |
| GuardianRage | 수호자 분노 (Berserk) | 모든 Tank 몬스터 공격력 ×2 | 6s |

### 5.2 Dps 축 (EBuildAxis.Dps)

**패시브 카드**

| ECardId | 한글명 | 효과 | 중첩 |
|---|---|---|---|
| ReaperAtkSpeed | 리퍼 공속 강화 | Reaper 공격속도 +20% | 누적 ×N |
| HexRangeBoost | 저주 범위 강화 | Hex 계열 사거리 +15% | 누적 ×N |
| SpawnReapers | 리퍼 소환 | Reaper 1마리 소환 + 스포너 출력 +1 | 소환 반복 |
| ReplaceReapersToHex | 리퍼→헥스 교체 | 필드 Reaper 전체 → Hex 변형으로 교체 | 반복 교체 |

**액티브 카드**

| ECardId | 한글명 | 효과 | 지속 |
|---|---|---|---|
| Frenzy | 광란 | 모든 Dps 몬스터 공속 +50% | 8s |
| BloodThirst | 흡혈 | Reaper 공격 시 피해량 30% 흡수 | 6s |
| MarkOfDeath | 죽음의 표식 | 영웅 받는 피해 +50% | 5s |

### 5.3 Debuff 축 (EBuildAxis.Debuff)

**패시브 카드**

| ECardId | 한글명 | 효과 | 중첩 |
|---|---|---|---|
| PlagueSlowBoost | 역병 둔화 강화 | Plague 둔화 효과 +10%p | 누적 ×N |
| SpawnPlagues | 역병 소환 | Plague 1마리 소환 + 스포너 출력 +1 | 소환 반복 |
| HeroPoisonAura | 영웅 독 장판 | 영웅 위치에 독 장판 주기적 생성 | 영구 누적 |
| HeroAttackDown | 영웅 공격력 하향 | 영웅 공격력 −20% | 영구·무기한 |

**액티브 카드**

| ECardId | 한글명 | 효과 | 지속 |
|---|---|---|---|
| Fear | 공포 | 영웅 이동 불능 | 3s |
| Bleed | 출혈 | 영웅 매초 HP −2% | 10s (EternalBleed 픽 시 무기한) |
| Weaken | 약화 | 영웅 공격력 −30% | 10s |

### 5.4 Swarm 축 (EBuildAxis.Swarm)

**패시브 카드**

| ECardId | 한글명 | 효과 | 중첩 |
|---|---|---|---|
| PhantomMoveSpeedBoost | 팬텀 이속 강화 | Phantom 이동속도 +20% | 누적 ×N |
| SpawnPhantoms | 팬텀 소환 | Phantom 1마리 소환 + 스포너 출력 +1 | 소환 반복 |
| SpawnWisps | 위습 소환 | Wisp 2마리 소환 + 스포너 출력 +1 | 소환 반복 |
| SpawnerHaste | 스포너 가속 | 모든 스포너 스폰 주기 −15% | 누적 ×N |

**액티브 카드**

| ECardId | 한글명 | 효과 | 지속 |
|---|---|---|---|
| TimeStop | 시간 정지 | 영웅 완전 동결 | 5s |
| Multiply | 분열 | 현재 필드 몬스터 전체 복사 소환 | 즉발 |
| Slow | 둔화 | 영웅 이동속도 −40% | 10s |

---

## 6. 빌드 시너지 (Layer 1 — Tier 시스템)

### 6.1 Tier 발동 기준

`BuildSynergyPanel`이 `BattleViewModel`의 축별 픽 수를 추적. 임계치 {3, 5, 7}에서 Tier 1/2/3 순차 발동(누적 스택).

| Tier | 발동 조건 (같은 축 픽 수) |
|---|---|
| Tier 1 | 3장 |
| Tier 2 | 5장 |
| Tier 3 | 7장 |

### 6.2 축별 Tier 효과

| 축 | Tier 1 | Tier 2 | Tier 3 |
|---|---|---|---|
| Tank | 전체 HP ×1.3 | 전체 공격력 ×1.2 | 스포너 캡 +6 |
| Dps | 전체 공격력 ×1.3 | 공격속도 +25% | 사거리 ×1.3 |
| Debuff | Plague 둔화율 ×0.8 (더 강함) | 영웅 공격력 ×0.85 (영구) | 출혈 영구화 |
| Swarm | 이동속도 ×1.3 | 스폰 주기 ×0.85 | 스포너 출력 +1 |

### 6.3 SynergyModalPopup

BuildSynergyPanel의 루트(축 아이콘 행) 클릭 시 `SynergyModalPopup` 열림.
- 헤더 행: 축 아이콘(28×28px) + `{AXIS} (N장)` 레이블
- 효과 행: Tier별 설명 (CHPoolingScrollView + scrollbar AutoHideAndExpandViewport)
- 닫기 버튼 / dim 영역 클릭 → `Close(reuse:true)`
- 팝업이 열린 동안 카드 픽 시 `OnBuildChanged` 구독으로 자동 갱신

---

## 7. HUD UI 구성

### 7.1 BattleHud 구조

`BattleHud`(UIBase)가 아래 하위 컴포넌트를 소유한다.

| 컴포넌트 | 역할 |
|---|---|
| `_timerText` (CHText) | 남은 시간 MM:SS 표시 |
| `_heroHpBar` (HpBarView) | 영웅 HP 비율 + 수치 + 상태 아이콘 8슬롯 |
| `_buildPanel` (BuildPanel) | 픽한 카드 아이콘 패널 |
| `_spawnerStatusPanel` (SpawnerStatusPanel) | 스포너 6개 상태 |
| `_synergyPanel` (BuildSynergyPanel) | 4축 시너지 Tier 표시 |

### 7.2 HpBarView & 상태 아이콘 (8슬롯)

HpBar 아래 `StatusIconRow`(HLG MiddleCenter) 컨테이너에 8개 `Image` 슬롯(12px 아이콘)을 정적 배치. 빈 슬롯 중 가장 낮은 인덱스 우선 배정(lowest-free-slot). 아이콘이 없으면 컨테이너 비활성(몬스터 HP바와 프리팹 공유 — 몬스터는 컨테이너 비활성 유지).

**Aura → 아이콘 매핑 (ECardId)**

| Aura 클래스 | 아이콘 (ECardId) | 지속 시간 |
|---|---|---|
| SlowAura | Slow | 10s |
| FearAura | Fear | 3s |
| WeakenAura | Weaken | 10s |
| TimeStopAura | TimeStop | 5s |
| BleedAura | Bleed | 10s |
| MarkOfDeathAura | MarkOfDeath | 5s |
| HeroAttackDownAura | HeroAttackDown | 무기한 |
| EternalBleedAura | Bleed | 무기한 |

### 7.3 SkillUnlockBannerView

`SkillUnlockBannerView`(UIBase, `EUI.SkillUnlockBanner`) — 영웅 스킬 해금 시 재생되는 전면 컷인 배너.

| 필드 | 설명 |
|---|---|
| `_root` (RectTransform) | 슬라이드 대상 가로 밴드 |
| `_label` (CHText) | `"영웅의 '{skillName}' 스킬 해제"` |
| `_slideInDuration` | 0.35s (SmoothStep, 왼쪽 밖 → 중앙) |
| `_holdDuration` | 1.2s |
| `_slideOutDuration` | 0.35s (SmoothStep, 중앙 → 오른쪽 밖) |

`HideImmediate()`로 화면 밖 초기 위치(−OffscreenX). 캔버스 실폭(`_root.rect.width`) 기준으로 화면 밖 X 보정. `Time.unscaledDeltaTime` 사용 — Pause 중 연출 가능.

### 7.4 BuildPanel

화면 하단에 패시브 스크롤 + 액티브 스크롤 두 섹션으로 구성. 픽한 카드를 아이콘으로 누적 표시, 중복 픽 시 ×N 배지. 루트 영역 클릭 → `BuildModalPopup` 열림(전체 픽 목록 상세).

`BuildModalPopup`: 화면 중앙 모달, 패시브(좌 50%)·액티브(우 50%) 분리 스크롤뷰. 빈 상태 라벨 별도. dim/X 버튼 클릭 → `Close(reuse:true)`. 팝업 열린 동안 카드 픽 시 자동 갱신.

### 7.5 SpawnerStatusPanel

6개 셀(134×168px), 배경 #1F2937 α0.85. 각 셀: 스포너 번호, 현재 스폰 몬스터 종류, HP/스폰 주기 프로그레스 바(#60A5FA/#F97316). **셀 클릭 동작 없음 — Tooltip 폐기(v0.6.4).**

---

## 8. 결과 화면 (ResultPopup)

### 8.1 승리·패배 조건

| 결과 | 조건 |
|---|---|
| 승리 (Win) | 영웅 HP ≤ 0 |
| 패배 (Lose) | 타이머 300s 만료 (영웅 생존) |

### 8.2 ResultPopup 구성

`ResultPopup`(UIBase)은 `BattleHudArg`와 별도 전투 결과를 받아 표시.

| 요소 | 설명 |
|---|---|
| 결과 텍스트 | 승리 / 패배 (CHText) |
| 보상 블록 | `HasMeta` 플래그 true 시 노출 — 획득 소울 수량, 달성 업적 목록 |
| 재시작 버튼 | `_retryButton` → Battle 씬 재로드 |
| 마을 버튼 | `_villageButton` → Village 씬 전환 |

씬 전환 시 `TryBeginSceneLoad()` 가드로 중복 로드 방지. 업적 최대 3개 + "외 N건 달성" 오버플로우 텍스트.

---

## 9. 마을 허브 & 메타 진행

### 9.1 소울 경제

| 공식 | 설명 |
|---|---|
| 기본 보상 | `결과 × 기준치` (승리 > 패배) |
| 영주 레벨 보너스 | `기본 × (1 + 레벨 × 0.05)` |
| 클리어 시간 보너스 | 남은 시간 1초당 추가 소울 |

### 9.2 상점 7품목

각 품목 Lv 0→5 성장. 가격은 기준 × 1.6^현재레벨.

| 품목 | 기준가(Lv0→1) | 효과 |
|---|---|---|
| 모집 광장 | 80 | 몬스터 종류 해금 슬롯 +1 |
| 훈련소 | 100 | 전체 몬스터 공격력 ×1.05 |
| 요새화 | 100 | 전체 몬스터 HP ×1.05 |
| 함정 제작소 | 120 | 함정 해금 슬롯 +1 |
| 마법 연구소 | 120 | 카드 풀 희귀 카드 등장율 +5% |
| 영주 집무실 | 150 | 카드 픽 재롤 기회 +1 |
| 비밀 금고 | 150 | 전투 시작 소울 보너스 +10 |

### 9.3 영주 레벨

XP 임계치: `100 × 1.25^(N−1)` (N = 목표 레벨). 레벨업 시 영주 스킬 포인트 +1.

### 9.4 도전과제 (13개)

첫 클리어, 5분 생존, 영웅 HP 10% 이하 클리어, 카드 풀 3중첩, 4축 동시 Tier 1, 축 단일 Tier 3, 축 단일 7픽 완성, 소울 누적 1000/5000, 승리 5/10/25/50회, 마을 상점 만렙.

### 9.5 v0.3 서버 연동 (클라이언트 측)

| 기능 | 설명 |
|---|---|
| 인증 | 기기 ID → 서버 → JWT 로컬 저장 |
| MetaProfile 동기 | 전체 MetaProfile 단위 클라우드 백업/복원 |
| 충돌 처리 | 서버가 409 반환 시 클라이언트 충돌 UI 표시 |
| 리더보드 | 최단 클리어 시간 제출·조회 (클라이언트 전송만; 서버 집계는 별도 레포) |

---

## 10. UI 인터랙션 매트릭스

### 10.1 전체 화면·트리거·처리 흐름

| 화면 / 컴포넌트 | 트리거 | 처리 클래스 | 결과 |
|---|---|---|---|
| LoadingHud | Addressables 진행 | LoadingController | 프로그레스바·% 갱신 |
| LoadingHud | 클릭 (CHButton) | LoadingController | Battle 씬 로드 |
| BattleHud | BattleClock.OnTick | BattleHud | 타이머 갱신 |
| BattleHud | VM.OnHpChanged | HpBarView | HP 바 갱신 |
| BattleHud | VM.OnBuildChanged | BuildPanel | 픽 아이콘 추가/배지 갱신 |
| BattleHud | VM.OnBuildChanged | BuildSynergyPanel | Tier 아이콘 갱신 |
| BattleHud | VM.OnSpawnerChanged | SpawnerStatusPanel | 스포너 셀 갱신 |
| BattleHud | VM.OnStatusIconAdded | HpBarView(8슬롯) | 아이콘 슬롯 배정 |
| BattleHud | VM.OnStatusIconRemoved | HpBarView(8슬롯) | 아이콘 슬롯 해제 |
| SkillUnlockBannerView | SkillUnlockCutsceneController | BannerView.PlayCo | 컷신 슬라이드 재생 (Pause 중) |
| CardSelectionPopup | PassiveTrigger / ActiveTrigger | TriggerQueue → BattleController | PauseService.Pause() + 3택 표시 |
| CardSelectionPopup | 카드 클릭 | BattleController.OnPicked | Effect.Apply() + AddPick() + Resume() |
| BuildSynergyPanel | 루트 클릭 | CHMUI | SynergyModalPopup 열기 |
| SynergyModalPopup | dim/닫기 클릭 | SynergyModalPopup | Close(reuse:true) |
| BuildPanel | 루트 클릭 | CHMUI | BuildModalPopup 열기 |
| BuildModalPopup | dim/닫기 클릭 | BuildModalPopup | Close(reuse:true) |
| SpawnerStatusPanel | — | — | 클릭 없음 (Tooltip 폐기 v0.6.4) |
| ResultPopup | 재시작 클릭 | ResultPopup | Battle 씬 로드 |
| ResultPopup | 마을 클릭 | ResultPopup | Village 씬 로드 |

### 10.2 PauseService 동작

- `Pause()`: depth 카운터 +1, depth=1일 때 `Time.timeScale = 0`
- `Resume()`: depth 카운터 −1, depth=0 될 때 `Time.timeScale = 1`
- 패시브·액티브 동시 트리거 → 큐 순차 처리 → 각 픽마다 Pause/Resume 쌍
- 스킬 해금 컷신도 `PauseService.Pause/Resume` 경유 — 카드 픽과 중첩 시 depth 누적 후 양쪽 Resume 완료 시 재개

### 10.3 에디터 전용 — LairBalanceWindow

`Lair/Balance Window` 메뉴 (EditorWindow). 개발자용 밸런스 조작 6종.

| 치트 | 설명 |
|---|---|
| Force Win | 즉시 승리 처리 |
| Force Lose | 즉시 패배 처리 |
| Add Soul | 소울 N 즉시 추가 |
| Hero HP 1 | 영웅 HP를 1로 설정 |
| Spawn Monster | 지정 몬스터 즉시 소환 |
| Force Card Draw | 강제 카드 픽 팝업 트리거 |

런 기록: `Logs/lair_runs.jsonl`에 누적(RunRecorder). 윈도우에서 최근 런 히스토리 조회 가능.

---

## 11. 쉬운 설명 (비개발자 요약)

Project Lair는 플레이어가 던전 주인이 되어 5분 동안 몬스터 군대를 지휘하는 게임이다. 영웅이 혼자 자동으로 싸워서 들어오고, 플레이어는 전투 중간중간 카드를 골라 몬스터를 더 강하게 만든다. 영웅은 HP가 줄면 자동으로 강력한 스킬을 해금하며, 이때 화면에 컷인 배너가 등장해 위협 신호를 알린다. 카드를 같은 테마로 모으면 시너지 효과가 터지고, 마을로 돌아와 상점에서 업그레이드하면 다음 판이 더 유리해진다. 로딩 화면에서 시작 클릭까지 포함해 Battle 씬 진입 전 과정도 코드로 관리된다.

즉, 이번 매뉴얼의 포인트는: "플레이어가 실제로 건드릴 수 있는 것은 카드 선택뿐이지만, 영웅 스킬 해금 컷신·로딩 씬·HP바 상태 아이콘·빌드 모달 등 모든 UI 흐름이 코드 기준으로 정확히 기록된 문서"이다.
