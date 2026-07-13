# Content Audit — 2026-07-14 — SpawnReapers 3픽 × Dps Tier1 복합 — 리퍼 스포너 동시 4대 DPS 밀집 + MaxReaperSimultaneousOutput 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10)
- 참조 spec/plan 수: 30 specs, 30 plans = 60개
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED — DebugAutoPicker 훅 미구현으로 시뮬 미실행)
- 과거 감사 이력 (git log): 21건 (가장 최근: 2026-07-13, 8b3c7e8)

---

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight) | 없음 |
| 몬스터 | 6종 | 6종 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 없음 |
| 패시브 카드 | 16장 (4축 × 4장) | 16장 (.asset 16개 확인) | 없음 |
| 액티브 카드 | 12장 (4축 × 3장) | 12장 (.asset 12개 확인) | 없음 |
| 캐릭터 프리팹 | 7 (영웅+몬스터6) | 7 (Knight+6종) | 없음 |
| 카드 효과 클래스 | 28 | 28 (Effects/*.cs 확인) | 없음 |

### 계획 있으나 미구현
- **SwarmRush 효과 (Multiply 자리 교체 예정)**: 원안 "Phantom 6마리 즉시 소환(`SwarmRushEffect`)" 미구현 — `FastBreedingEffect` (팬텀 스포너 주기 ×0.6 영구) 잔존. `card-renewal.md` §3.4 + 컨셉 §11.3 주석에서 명시.
- **WallOfWisps 소환 효과**: 원안 "영웅 주변 4방위 Wisp 4마리 즉시 소환" 미구현 — 현행 `ToughHideEffect` (Wisp·Wraith 받는 데미지 ×0.75 영구)로 교체.
- **스포너 종 교체 효과**: `ReplaceWispsToWraith`·`ReplaceReapersToHex` 원안 교체 효과 미구현 — 현행 Power ×1.3 강화(`WispWraithPowerBoostEffect`/`ReaperHexPowerBoostEffect`)로 대체.

### QA 권고 미해결
- gameplay-programmer 에게 `BattleController.DebugAutoPicker` 훅 추가 요청 (2026-05-22 QA §3) — **미구현 상태 지속**
- `LairSimWindow` + `SimDriver` 시뮬레이션 인프라 미구축 — QA §4 권고 사항

### 과거 감사 후보 (git log 조회 결과)

| 날짜(Seoul) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-07-13 | 8b3c7e8 | Tank 액티브 WallOfWisps(ToughHide효과) 반복 픽 멱등 — ToughHideDamageTakenScale 손잡이 + 중첩 정책 미결정 |
| 2026-07-12 | 4058eae | Dps 액티브 MarkOfDeath 3픽 × Dps Tier3(Range×1.3) 복합 — MaxMarkOfDeathDmgTakenMul 손잡이 미설계 |
| 2026-07-11 | 18dea17 | Tank Tier3 필드 캡 +6 발동 시 스폰 밀집도 — TankTier3CapBonus 손잡이 미설계 |
| 2026-07-10 | 92bff1d | Debuff 패시브 HeroPoisonAura 5s 독장판 — HP% 트리거 간격 불일치 + BalanceConfig 손잡이 미설계 |
| 2026-07-09 | 63ab1a5 | Swarm 액티브 TimeStop 영웅 스킬 우회 — HeroSkillRunner IAttacker.Enabled 미체크 + TimeStopDuration 손잡이 미설계 |
| 2026-07-08 | 1be6efc | Debuff 패시브 HeroAttackDown 3픽+Tier2(×0.85) 복합 영구 공격력 ×0.358 — MinHeroAttackScale(영구) 손잡이 미설계 |
| 2026-07-07 | bddf4f3 | Tank 액티브 ToughHide·IronWill·GuardianRage 3중 데미지 감소 복합 ×0.2625 — MinMonsterDamageTakenScale 손잡이 미설계 |
| 2026-07-06 | 78c61f3 | Debuff 액티브 Weaken _factor·_duration 하드코딩 — WeakenFactor·MinHeroAttackScaleFloor BalanceConfig 손잡이 미설계 |
| 2026-07-05 | 9b3303b | Debuff 액티브 Weaken 영웅 스킬 도입 후 실효성 급감 — WeakenFactor BalanceConfig 손잡이 미설계 |
| 2026-07-04 | 647bc82 | Dps 패시브 HexRangeBoost 3픽+Tier3 복합 Hex 사거리 ×3.567 — 영웅 AI 타격 우선순위 영구 회피 + MaxHexRangeMul 손잡이 미설계 |
| 2026-07-03 | ea6803e | Debuff 액티브 Bleed '이동/정지 무관' 구현-설계 불일치 — 트리거 조건 결정 요청 |
| 2026-07-02 | 148ae90 | Dps ReplaceReapersToHex 3픽(×2.197)+Tier1(×1.3) 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계 |
| 2026-07-01 | db9b2d7 | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계 |
| 2026-06-30 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |
| 2026-06-29 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-27 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-26 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-25 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — 액티브 트리거 9→5 감소 후 Tier3 도달 난이도 50% 픽집중 · SynergyTierThreshold 손잡이 미설계 |
| 2026-06-24 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-23 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-21 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### SpawnReapers 3픽 × Dps Tier1(Power ×1.3) 복합 — 리퍼 스포너 동시 출력 4대 DPS 밀집 + MaxReaperSimultaneousOutput 손잡이 미설계

- **카테고리**: 패시브 카드 (Dps 축) + BalanceConfig 손잡이 미설계
- **요지**: `SpawnReapers` (Dps P — "Reaper 스포너 동시 출력 +1")를 3픽 누적하면 리퍼 스포너가 한 주기에 동시 4마리를 방출한다. 여기에 Dps Tier1 시너지(Reaper·Hex Power ×1.3)가 발화하면 강화된 리퍼 4마리가 동시에 DPS를 투하하는 밀집 구간이 형성되는데, 이 복합 상단을 제한하는 `MaxReaperSimultaneousOutput` BalanceConfig 손잡이가 현재 존재하지 않는다.
- **검증/구현/시너지/데이터**: 4/2/4/3 → 종합 **15**
- **근거**:
  - 컨셉 §11.3 Dps 축 `SpawnReapers`: "Reaper 스포너 동시 출력 +1"
  - `card-renewal.md` §변경이력 #6: 전역 3픽 캡으로 포섭 → 3픽 후 SpawnReapers 제외 (×0.8³ 유사, 출력 상한은 기본 1 + 추가 3 = 4)
  - 2026-06-30 감사(07d6dd7): SpawnPhantoms 3픽+Tier3 → `MaxSpawnerSimultaneousOutput` 손잡이 미설계로 동일 패턴 확인됨 — SpawnReapers 는 글로벌 캡이 아닌 **리퍼 특정 출력 상한** 이라는 차별 각도
  - 컨셉 §11.3 Reaper: DPS 40, HP 100 — 4마리 × Power ×1.3 = 약 208 DPS 동시 투하 가능
- **MVP 범위**: 컨셉 §11.2 ✅ — 패시브 카드 16장 범위 내, BalanceConfig 손잡이 추가(slice-c balance-tooling 범위)

#### 유저 플로우

**1. 노출 시점·트리거**

영웅 HP가 10%씩 깎일 때마다 패시브 카드 3택 1 팝업이 열린다. `SpawnReapers`(Dps 패시브, 테두리 빨강)가 후보 3장 중 하나로 등장했을 때 플레이어가 픽한다. 1픽 시 리퍼 스포너 동시 출력 1→2, 2픽 1→3, 3픽 1→4. 세 번 다 고르거나 다른 Dps 카드 2장+SpawnReapers 1장 조합으로 Dps 축 합계가 3에 도달하면 Tier1 시너지가 즉시 발화한다.

**2. 화면 변화**

카드 팝업 상단 Dps 빌드 카운트 바가 픽할 때마다 1씩 증가하여, 3에 도달하면 Tier1 발화 연출(빨강 강조 연출)이 출력된다. 필드에서는 Reaper(빨강 Capsule)의 수가 픽 직후 다음 스폰 주기부터 눈에 띄게 늘어난다. Dps Tier1 발화 후 Reaper Power ×1.3 적용으로 리퍼가 때릴 때마다 영웅의 HP 바가 더 빠르게 깎이는 것을 확인할 수 있다.

**3. 입력 행동**

플레이어는 패시브 팝업에서 `SpawnReapers` 카드(또는 임의 Dps 카드 조합)를 클릭해 픽한다. 3픽 캡(`card-3pick-cap.md`) 정책에 따라 `SpawnReapers`는 3번 픽 후 카드 풀에서 제거된다. 이후 추가 행동 없이도 출력 4대 효과가 런 종료까지 유지된다.

**4. 시스템 반응**

`SpawnReapersEffect`가 Reaper 스포너의 `SimultaneousOutput` 값을 +1씩 증가시킨다(덧셈 누적). 3픽 시 기본 1 + 추가 3 = **동시 4대**. Dps Tier1 발화 시 `BuildSynergyService`가 Reaper·Hex 전체에 Power ×1.3 글로벌 영구 버프를 적용한다. 결과: Reaper 기본 DPS 40 → 강화 후 52. 스포너 출력 4대 × 52 DPS = **필드 Reaper DPS 합계 ≈ 208/s**. 현행 `BalanceConfig`에 `MaxReaperSimultaneousOutput` 손잡이가 없어 밸런서가 이 상단을 인스펙터에서 직접 조정할 수 없다.

**5. 반복·재발생 패턴**

`SpawnReapers` 3픽 후 효과는 영구 적용되어 이후 스폰된 모든 Reaper에 소급한다(컨셉 §4.1 지속 스폰 모델). 다음 스폰 주기부터 4마리씩 방출이 반복된다. 추가 Dps 카드(ReaperAtkSpeed, Frenzy, MarkOfDeath 등)를 더 픽하면 Tier2·3으로 진입해 Reaper 공속·사거리가 추가 강화된다. 글로벌 캡(기본 18)이 포화 상태이면 다른 종과 캡을 두고 경쟁하여 Reaper 비중이 변동한다.

**6. 종료·해소 조건**

런 종료(영웅 HP 0 또는 5:00 타임오버)까지 출력 4대 유지. `SpawnReapers` 3픽 후 카드 풀 제외로 추가 증가 없음. Reaper가 영웅에게 처치당하면 해당 마리는 즉시 제거되고 다음 주기에 다시 4마리 방출이 예정된다 — 처치 압박 이후 빠른 복구가 특징. 런이 끝날 때까지 해소 메커니즘이 없다.

**7. 다른 시스템과 상호작용**

- **글로벌 필드 캡(18/24)**: Reaper 4마리 + 다른 종 14마리 = 18 포화 시 Reaper 스포너 자연 백오프. Tank Tier3(+6) 후 캡 24이면 Reaper 비중이 더 높아질 수 있다.
- **Frenzy (A, Dps)**: 모든 몬스터 공속 +50%, 10s. SpawnReapers 3픽 × Frenzy 1픽 조합 시 4마리 Reaper가 약 1.5배 속도로 공격하는 10초 버스트 윈도우 발생 — Frenzy × SpawnReapers 복합 DPS 버스트는 별도 손잡이 미설계.
- **ReaperAtkSpeed (P, Dps)**: Reaper 공격 쿨다운 ×0.7. 2026-06-24 감사(b83b566)에서 3픽+Tier2 복합 쿨다운 0.137s 이슈 확인됨 — SpawnReapers 4마리와 쿨다운 극감이 동시에 가해지면 DPS가 더 폭주한다.
- **Dps Tier2(Cooldown ×0.8)·Tier3(Range ×1.3)**: Tier2 도달 시 4마리 Reaper 공속 추가 강화, Tier3 시 사거리 확대로 리퍼가 영웅을 더 오래 사거리 안에 붙잡는다.

**8. 엣지 케이스**

- **ReplaceReapersToHex 선행 픽 후 SpawnReapers 픽**: 현행 `ReplaceReapersToHex`는 Power ×1.3(ReaperHexPowerBoostEffect)이므로 종 교체가 아니다 — 실제 Reaper 스포너는 그대로 존재한다. 원안(스포너 종 교체)이 구현됐다면 이 조합은 Reaper 스포너 부재 엣지지만, 현행 구현에서는 상호작용 이상 없음.
- **글로벌 캡 포화 + SpawnReapers 4대 예약**: 캡(18) 포화 시 4마리 예약이 걸려 실제 필드에는 2~3마리만 즉시 등장. 스폰 백오프 동안 다른 몬스터가 죽으면 즉시 보충된다 — SpawnReapers 의 실효 이익이 캡 포화 상태에서 줄어들 수 있으며, 이 동작을 밸런서가 BalanceConfig 에서 제어할 수 없다.
- **3픽 캡 + 다른 Dps 패시브 중첩**: SpawnReapers 3픽(출력 +3) + SpawnReapers 배제 후 ReaperAtkSpeed 3픽(쿨다운 극감) 순서로 풀에서 제거되면, 4마리 × 초고속 공격이 동시에 발생하는 루트가 생긴다. 이 조합의 DPS 천장은 현재 미측정이다.

**9. 유저 정보·피드백**

카드 팝업에서 "Dps 빌드 카운트 +1" 표시 및 Tier 도달 시 강조 연출을 통해 방향성을 확인할 수 있다. 필드에서 빨강 Capsule(Reaper)의 수가 직관적으로 늘어나 픽 효과를 눈으로 확인한다. **현행 미설계 상태**: `MaxReaperSimultaneousOutput` 손잡이가 없어 밸런서가 리퍼 스포너 출력 상단을 런타임 없이 조정할 수 없다. 3픽 후 실제 필드 Reaper DPS가 밸런스 목표(평균 사망 2~4분, 컨셉 §8)에서 벗어나는지 여부를 BalanceConfig 조작만으로 검증·수정할 경로가 없다.

---

### 보류

| 후보 | 카테고리 | 종합점수 | 보류 이유 |
|---|---|---|---|
| SpawnWraith 3픽 × Tank Tier1 복합 HP 밀집 | Tank 패시브 | 13 | Tank 축은 최근 7일 이내 3회(2026-07-07·11·13) 다뤄짐. 차별 근거 불충분 |
| Frenzy × SpawnReapers 3픽 버스트 DPS 윈도우 | Dps 패시브+액티브 복합 | 14 | Frenzy 이미 2026-06-19에서 다뤄짐. SpawnReapers 단독 후보에 §7(다른 시스템 상호작용)로 포섭 가능 |
| SpawnReapers × SpawnPhantoms 혼합 글로벌 캡 경합 | 크로스 축 | 12 | SpawnPhantoms + Tier3 는 2026-06-30에서 이미 다뤄짐. 크로스 축 경합은 후속 시너지 과제 |

---

## 3. 과거 감사 대비 차별성

git log 조회 21건 검토 완료.

가장 유사한 과거 커밋:
- **07d6dd7 (2026-06-30)** — "Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계"

차별점:
1. **축**: SpawnPhantoms 는 Swarm 축, SpawnReapers 는 Dps 축 — 서로 다른 빌드 경로
2. **시너지 복합**: SpawnPhantoms 는 Swarm Tier3(모든 스포너 출력 +1)와 상호작용. SpawnReapers 는 Dps Tier1(Power ×1.3) 발화와 연동해 필드 DPS 자체가 증폭되는 구조 — 출력 수 × 공격력 복합이 핵심
3. **손잡이 성격**: 07d6dd7은 글로벌 `MaxSpawnerSimultaneousOutput`. 본 후보는 **리퍼 종 특정** `MaxReaperSimultaneousOutput` — 종별 세밀 조정 필요성 강조

카테고리(Dps 패시브 × Power 시너지 복합)와 요지(스포너 출력 × Power 강화의 DPS 누적)가 충분히 차별화됨.

---

## 4. 제외 (범위 밖)

- 신규 영웅·몬스터 추가: CLAUDE.md §8 금지 (잠금 슬롯 더미만)
- 카드 매수 변경 (16/12 고정): 컨셉 §11.2 lock
- 서버 연동 신규 화면: v0.3 서버 연동 클라이언트 코드 범위, 별도 기획 필요
- SwarmRush 효과 구현: 컨셉 §11.3 미구현 명시 — 별도 card-ideas 기획서 프로세스 필요

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청
- 구체 작업: `BalanceConfig.cs`에 `MaxReaperSimultaneousOutput` (int, 기본값 4) 손잡이 추가 + `SpawnReapersEffect`에 클램프 로직 삽입
- 부가 검토: SpawnReapers 4대 × ReaperAtkSpeed 3픽 복합 DPS 천장 측정 (QA 훅 구현 후 시뮬레이션 권장)

---

## 6. 쉬운 설명 (비개발자 요약)

리퍼는 이 게임에서 가장 빠르게 데미지를 입히는 빨간 몬스터다. "SpawnReapers" 카드를 3번 고르면 리퍼가 한꺼번에 4마리씩 계속 나타나고, 동시에 리퍼 전체 공격력이 30% 더 강해지는 보너스까지 발동된다. 이렇게 되면 화면에 빨간 몬스터가 동시에 4마리 쏟아지면서 영웅을 집중 공격하는 상황이 만들어진다. 문제는 이 4마리라는 숫자와 공격력을 게임 개발자가 설정 파일에서 손쉽게 조절할 수 있는 "손잡이"가 현재 없다는 것이다. 즉, 영웅이 너무 빨리 죽거나 너무 느리게 죽는 상황이 생겨도 코드를 직접 고치지 않으면 조정이 안 된다. 그래서 이번에 제안하는 것은: 리퍼 최대 동시 출현 수를 설정 파일에서 바꿀 수 있는 "MaxReaperSimultaneousOutput 손잡이"를 추가하자.
