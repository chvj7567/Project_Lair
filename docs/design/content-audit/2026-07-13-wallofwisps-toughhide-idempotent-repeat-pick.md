# Content Audit — 2026-07-13 — Tank 액티브 WallOfWisps(ToughHide효과) 반복 픽 멱등 — ToughHideDamageTakenScale 손잡이 미설계 + 중첩 정책 미결정

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10)
- 참조 spec/plan 수: 약 62개 (specs 31 + plans 31)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태)
- 과거 감사 이력 (git log): 23건 (가장 최근: 2026-07-12)

---

## 1. 현황

| 카테고리 | 컨셉 §11 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (Knight) | Knight.prefab 1개 | ✅ 일치 |
| 몬스터 | 6종 | Wisp·Wraith·Reaper·Plague·Phantom·Hex 6종 프리팹 | ✅ 일치 |
| 패시브 카드 | 16장 (4축×4) | Items/*.asset 28장 중 P=16장 | ✅ 일치 |
| 액티브 카드 | 12장 (4축×3) | Items/*.asset 28장 중 A=12장 | ✅ 일치 |

### 계획 있으나 미구현
- **SwarmRush (팬텀 6마리 즉시 소환)** — 원안의 Multiply 대체 카드. 현행 `Multiply.asset`(FastBreedingEffect, 팬텀 스포너 주기 ×0.6 영구)가 잔존. `card-renewal.md` §3.4 "SwarmRush 미구현" 명기.
- **WallOfWisps 원안 효과 (영웅 주변 4방위 Wisp 4마리 즉시 소환)** — `card-renewal.md` §변경이력 #1: 현행 구현은 ToughHideEffect(Wisp·Wraith 받는 데미지 영구 ×0.75). 원안 소환 메커니즘과 다름.

### QA 권고 미해결
- **헤드리스 시뮬레이션 훅 미구현** — `BattleController.DebugAutoPicker` 델리게이트 부재로 QA 시뮬레이션 BLOCKED (2026-05-22 리포트 §3). 모든 데이터 기반 밸런스 검증 불가 상태 지속.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-07-12 | 4058eae | Dps 액티브 MarkOfDeath 3픽 × Dps Tier3(Range×1.3) 복합 — MaxMarkOfDeathDmgTakenMul 손잡이 미설계 |
| 2026-07-10 | 18dea17 | Tank Tier3 필드 캡 +6 발동 시 스폰 밀집도 — TankTier3CapBonus 손잡이 미설계 |
| 2026-07-09 | 92bff1d | Debuff 패시브 HeroPoisonAura 5s 독장판 — HP% 트리거 간격 불일치 + BalanceConfig 손잡이 미설계 |
| 2026-07-08 | 63ab1a5 | Swarm 액티브 TimeStop 영웅 스킬 우회 — HeroSkillRunner IAttacker.Enabled 미체크 + TimeStopDuration 손잡이 미설계 |
| 2026-07-07 | 1be6efc | Debuff 패시브 HeroAttackDown 3픽+Tier2(×0.85) 복합 영구 공격력 ×0.358 — MinHeroAttackScale(영구) 손잡이 미설계 |
| 2026-07-06 | bddf4f3 | Tank 액티브 ToughHide·IronWill·GuardianRage 3중 데미지 감소 복합 ×0.2625 — MinMonsterDamageTakenScale 손잡이 미설계 |
| 2026-07-05 | 78c61f3 | Debuff 액티브 Weaken _factor·_duration 하드코딩 — WeakenFactor·MinHeroAttackScaleFloor BalanceConfig 손잡이 미설계 |
| 2026-07-04 | 9b3303b | Debuff 액티브 Weaken 영웅 스킬 도입 후 실효성 급감 — WeakenFactor BalanceConfig 손잡이 미설계 |
| 2026-07-03 | 647bc82 | Dps 패시브 HexRangeBoost 3픽+Tier3 복합 Hex 사거리 ×3.567 — 영웅 AI 타격 우선순위 영구 회피 + MaxHexRangeMul 손잡이 미설계 |
| 2026-07-02 | ea6803e | Debuff 액티브 Bleed '이동/정지 무관' 구현-설계 불일치 — 트리거 조건 결정 요청 |
| 2026-07-01 | 148ae90 | Dps ReplaceReapersToHex 3픽(×2.197)+Tier1(×1.3) 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계 |
| 2026-06-30 | db9b2d7 | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계 |
| 2026-06-29 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — 액티브 트리거 9→5 감소 후 Tier3 도달 난이도 · SynergyTierThreshold 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — BalanceConfig MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| (이전 6건) | - | 2026-05-28 ~ 2026-06-15 감사 이력 — 별도 조회 가능 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Tank 액티브 WallOfWisps(ToughHide효과) 반복 픽 멱등 — ToughHideDamageTakenScale 손잡이 미설계 + 중첩 정책 미결정

- **카테고리**: Tank 액티브 / BalanceConfig 손잡이 + 중첩 정책 결정
- **요지**: `WallOfWisps.asset`은 현행 ToughHideEffect(Wisp·Wraith 영구 받는 데미지 ×0.75)인데, `AddBuff` dedup으로 완전 멱등(2·3픽 = 효과 변화 없음). 배율 `0.75f`가 `MonsterBuffService.cs` 상수로 박혀 있어 BalanceConfig 튜닝 불가 — 중첩 정책 결정과 손잡이 노출이 동시에 필요하다.
- **검증/구현/시너지/데이터**: 4/2/3/4 → 종합 **15**
- **근거**: `docs/design/card-renewal.md` §3.1 #6 (ToughHide 중첩 정책 "멱등에 가까움" + "SO data 비어있음"), §3.1 note (MonsterBuffService 상수 정책), `docs/design/card-3pick-cap.md` (전역 3픽 캡)
- **MVP 범위**: 컨셉 §11.3 WallOfWisps(Tank 액티브 카드), §11.2 패시브·액티브 카드 BalanceConfig 손잡이 관리

---

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**  
   전투 시작 후 30초마다 발동하는 액티브 카드 3택1 팝업에 WallOfWisps("단단한 살갗")이 등장한다. Tank 빌드 중인 플레이어가 Tank 티어 임계값(3·5·7장) 도달을 노릴 때, 이 카드가 풀에서 등장하면 픽 유인이 매우 높다. 초록 테두리(Tank축 색)와 "영구" 효과 문구가 강한 선택 신호를 준다.

2. **화면 변화**  
   카드 선택 팝업 3장 중 WallOfWisps 슬롯: 초록 테두리 + displayName "단단한 살갗" + 설명 "위스프·레이스 받는 데미지 -25% (영구)". 픽 후 시너지 패널 Tank 카운트가 +1 증가하고, 임계 도달 시 Tier 아이콘이 추가 표시된다. 필드 Wisp·Wraith의 체력바 색은 변하지 않아 효과 적용 여부를 시각적으로 확인하기 어렵다.

3. **입력 행동**  
   플레이어가 WallOfWisps 카드를 클릭해 픽. 팝업 닫힘, BattlePause 해제, 전투 재개. 카드 이름이 빌드 기록 패널에 추가된다.

4. **시스템 반응 (1픽)**  
   `ToughHideEffect.Apply` → `MonsterBuffService.AddMonsterBuff(EMonsterBuff.ToughHide, -1f)`. `AddBuff` 내부에서 ToughHide가 없으면 신규 등록: 필드 내 모든 Wisp·Wraith에 즉시 소급 적용(`DamageTakenScale *= 0.75f`). 이후 스폰되는 동종 몬스터는 스폰 시점에 같은 배율 반영. Tank 빌드 카운트 +1.

5. **반복·재발생 패턴**  
   전역 3픽 캡 미달 상태에서 이후 라운드에 WallOfWisps가 재등장할 수 있다. **2픽째**: `AddMonsterBuff` 호출 → `AddBuff` dedup 로직이 이미 등록된 `EMonsterBuff.ToughHide`를 감지. 영구 buff(`Remain=-1f`)라 `Remain` 연장도 발생하지 않음. **효과 변화 없음**, Tank 카운트만 +1. **3픽째**: 동일. 총 3픽을 써도 ToughHide 데미지 감소는 처음 1픽 때의 ×0.75가 전부다.

6. **종료·해소 조건**  
   전역 3픽 캡(2026-06-01 도입) 도달 시 WallOfWisps가 풀에서 제거됨. 또는 런 종료(영웅 처치 또는 5분 타임오버). ToughHide 영구 버프는 런 내내 지속되며 런 외부로 이월되지 않는다.

7. **다른 시스템과 상호작용**  
   - **IronWill(A) + GuardianRage(A) 복합**: ToughHide 영구 ×0.75, IronWill 15s ×0.7, GuardianRage 15s ×0.5 → 동시 활성 시 ×0.2625. ToughHide가 멱등이므로 2·3픽 추가는 이 복합 배율을 낮추지 않음 (Jul 6 감사에서 지적한 MinMonsterDamageTakenScale 문제와 별개로, ToughHide 자체 중첩 한계가 복합 기여도 증가를 차단).  
   - **Tank Tier 임계**: ToughHide 3픽 = Tank 카운트 +3 → Tier1(3장) 즉시 도달 가능. 그러나 그 3픽이 만드는 실질적 전투 기여는 1픽과 동일하여, 카운트 올리기용 픽으로 소모되는 구조. `MonsterBuffService.cs` ToughHide case 상수(`DamageTakenScale *= 0.75f`, line 106)는 BalanceConfig 필드가 아니어서 외부 조정 불가.

8. **엣지 케이스**  
   - 플레이어가 2픽 이후 "ToughHide가 더 강해졌겠지"라 가정하고 이후 전투를 운용할 경우, 실제 방어력 증가가 없어 예상 밖의 몬스터 사망이 발생. UI에 "이미 적용 중 — 추가 효과 없음" 안내가 없어 이 오해를 방지할 수단이 현재 없다.  
   - WallOfWisps 3픽(전역 3픽 캡 도달) 후 풀에서 제거 — ToughHide 배율은 ×0.75 그대로이나 Tank 카운트는 +3 누적, Tier1(3장) 진입 기여. 만약 1픽 후 바로 풀에서 제거하면 Tier 진입 속도가 달라져 Tank 빌드 난이도 상승 가능.  
   - `EMonsterBuff.BerserkPower` case(전체 종 Power×3)가 MonsterBuffService.cs에 미사용 상태로 잔존 — ToughHide 멱등 이슈와 무관하나 동일한 "상수 하드코딩" 패턴이므로 함께 검토 권장.

9. **유저 정보·피드백**  
   카드 설명에 "영구"라고만 표기되어 중복 픽 시 효과 없음을 플레이어가 알 방법이 없다. Tank 카운트 +1이 뜨므로 픽이 "유효했다"는 신호를 받지만 실제 전투 수치 변화는 0. 반면 IronWill·GuardianRage는 중복 픽 시 지속시간이 연장되어 유의미한 효과 증가가 있음 — WallOfWisps만 유독 불리. QA 시뮬레이션 BLOCKED 상태라 빈도 데이터 없음.

---

### 보류
- **Swarm Tier3 전 스포너 동시출력 +1 글로벌 영향** — Jun 29 (SpawnPhantoms+Tier3·MaxSpawnerSimultaneousOutput)와 카테고리·요지·근거 모두 겹침. 보류.
- **Fear+Slow 복합 도주 이속 미정의** — Jun 12 (timestop-fear-duration-cap)와 Debuff 카테고리 동일 + Fear duration 이미 언급. 차별성 낮음. 보류.
- **SpawnWraith 3픽 출력 + Tank Tier 복합** — Jun 29 (MaxSpawnerSimultaneousOutput) 패턴과 동일 메커니즘. 보류.

---

## 3. 과거 감사 대비 차별성

git log 조회 23건 검토 완료.

가장 유사했던 과거 커밋: **bddf4f3 (2026-07-06)** — "Tank 액티브 ToughHide·IronWill·GuardianRage 3중 데미지 감소 복합 ×0.2625 — MinMonsterDamageTakenScale 손잡이 미설계"

차별점:
- Jul 6 감사: 서로 다른 3장(ToughHide·IronWill·GuardianRage)이 **동시에 적용**될 때의 복합 배율 하한 미설계. 요지 = 복합 최저선 손잡이 부재.
- 본 감사: **동일 카드(WallOfWisps=ToughHide)를 반복 픽할 때 효과가 전혀 누적되지 않는 멱등성** + 배율 자체가 BalanceConfig에 노출되지 않는 이중 설계 공백. 요지 = 중첩 정책 미결정 + 단독 손잡이 부재.
- 두 감사는 "ToughHide가 관여한다"는 공통점이 있으나, Jul 6는 3카드 복합, 본 감사는 1카드 멱등 — 문제 축이 상이함.

---

## 4. 제외 (범위 밖)
- **SwarmRush 신규 구현** — 미구현 카드이지만 "신규 영웅·몬스터·카드 리소스 제작 금지" (CLAUDE.md §8) 및 game-designer 승격 전 착수 불가.
- **WallOfWisps 원안(소환 메커니즘) 복원** — 현행 ToughHideEffect로 확정됨. 원안 소환 로직은 별도 기획화 후 착수.

---

## 5. 다음 단계 제안

1. **중첩 정책 결정 (game-designer)**: ToughHide 2·3픽 시 — (a) 멱등 유지(현행, 카드를 1픽 후 즉시 풀 제거로 보완), (b) 가산 중첩(2픽=×0.75²=×0.5625, 3픽=×0.4219), (c) IronWill·GuardianRage와 동일한 지속시간 누적 전환 + 영구→시한 변경 중 선택.
2. **BalanceConfig 손잡이 노출 (gameplay-programmer)**: `MonsterBuffService.cs` ToughHide case 상수 `0.75f` → `BalanceConfig.ToughHideDamageTakenScale`로 추출. SO `data` 필드 활용 또는 BalanceConfig 직접 참조.
3. **3픽 캡 연동 검토**: 정책이 "(a) 멱등 유지"로 결정될 경우, `card-3pick-cap.md`에 "멱등 카드는 1픽 후 풀 제거" 예외 규칙 추가 여부 게임-디자이너와 합의 필요.
4. 채택 시 game-designer에게 정식 기획 요청.

---

## 6. 쉬운 설명 (비개발자 요약)

"단단한 살갗" 카드를 한 번 고르면 우리 편 몬스터(위스프·레이스)가 받는 피해가 영원히 25% 줄어든다. 좋은 카드다. 그런데 똑같은 카드를 두 번, 세 번 다시 골라도 몬스터는 더 강해지지 않는다 — 게임이 "이미 있잖아, 안 줘"하고 조용히 넘어가기 때문이다. 반면 카드를 두 번 이상 골랐다는 기록은 점수판에 쌓여 강화 보너스(탱크 티어)로 이어진다. 실제로는 아무 이득 없는 선택인데 점수판은 "잘 했어"라고 보여주는 셈이다. 게다가 25%라는 수치가 게임 어딘가 숨겨진 숫자에 박혀 있어서 개발자가 "이거 좀 조정해볼까"를 해도 인스펙터에서 바꿀 방법이 없다. 그래서 이번에 제안하는 것은: 카드를 두 번 이상 고를 때 정말 더 강해지는지(또는 아예 못 고르게 하는지)를 정하고, 수치를 인스펙터에서 바꿀 수 있게 꺼내두자.
