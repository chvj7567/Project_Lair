# Content Audit — 2026-07-07 — Tank 축 ToughHide·IronWill·GuardianRage 3중 데미지 감소 복합 — MinMonsterDamageTakenScale 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10)
- 참조 spec/plan 수: 30개 spec, 30개 plan
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED — 시뮬레이션 미실행)
- 과거 감사 이력 (git log): 20건 (가장 최근: 2026-07-06 KST, SHA 78c61f3)

---

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | — |
| 몬스터 | 6 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) + LittleGhost variants | — |
| 패시브 카드 | 16 | 16 (28장 중 P=16) | — |
| 액티브 카드 | 12 | 12 (28장 중 A=12) | — |
| 카드 효과 클래스 | 28 | 28 (Effects/*.cs) | — (Multiply → SwarmRush 미교체 잔존) |

### 계획 있으나 미구현
- `ECardId.Multiply` 자리 `SwarmRushEffect`(Phantom 6마리 즉시 소환) 교체 미완 — `FastBreedingEffect`("빠른 번식", 팬텀 스포너 주기 ×0.6 영구) 잔존 (`card-renewal.md` §3.4)
- `BattleController.DebugAutoPicker` 훅 미구현 → QA 시뮬레이션 전면 차단 (QA 리포트 2026-05-22 §3·§5)

### QA 권고 미해결
- QA 자동 픽 델리게이트 훅 (`#if UNITY_EDITOR`) 미구현 → 헤드리스 전략 시뮬 불가 (2026-05-22 §3)
- 시뮬레이션 캠페인 실행 방식(대화형 에디터 vs `[UnityTest]` 래핑) 미결정 (2026-05-22 §5.4)

### 과거 감사 후보 (git log 조회 결과) — 20건

| 날짜(KST) | SHA | Subject 설명 |
|---|---|---|
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
| 2026-06-20 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — BalanceConfig MaxTankPowerScale 손잡이 미설계 |
| 2026-06-19 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-18 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-17 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-16 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 (영웅 HP 4000 기준 3픽 합산 1.875% — 스킬 도입 후 격차 확대) |
| 2026-06-15 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 제안 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Tank 축 ToughHide(영구)·IronWill·GuardianRage 3중 데미지 감소 복합 — MinMonsterDamageTakenScale 손잡이 미설계

- **카테고리**: BalanceConfig 손잡이 추가 (MinMonsterDamageTakenScale 플로어)
- **요지**: Tank 축의 데미지 감소 효과 3종(ToughHide 영구 ×0.75, IronWill 15s ×0.7, GuardianRage 15s ×0.5)이 모두 `MonsterBuffService` 내부 상수로 하드코딩되어 있으며, 동시 활성 시 Wisp·Wraith가 받는 데미지 배율이 ×0.2625까지 내려가는 극단 시나리오에 플로어 보호 장치가 없다.
- **검증가치/구현비용/시너지폭/데이터근거**: 4/2/4/3 → 종합 15
- **근거**: `docs/design/card-renewal.md` §3.1 #5(IronWill 핵심수치), #6(ToughHide 핵심수치), #7(GuardianRage 핵심수치 및 2026-06-01 HP×2.0 제거 확인)
- **MVP 범위**: 컨셉서 §11.3 Tank 축 카드 7장 내 — IronWill·WallOfWisps·Berserk(GuardianRage) 모두 v0.6 카드 라인업에 포함된 기존 카드. BalanceConfig 손잡이 추가는 `slice-c-balance-tooling` spec 범위 내.

---

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**

Tank 축 빌드에 집중하는 플레이어가 30초마다 오는 액티브 카드 9회 픽 중 IronWill 3픽·WallOfWisps 1픽·GuardianRage 3픽 = 7픽을 Tank 액티브에 몰면 발생한다. 나머지 2픽을 Tank 패시브(WispHpBoost 등)로 채워 Tier2(5장 임계) 달성도 가능한 현실적 빌드 경로다. 3픽 캡 시스템(`card-3pick-cap.md`)에 의해 IronWill·GuardianRage가 각각 3픽 후 풀에서 제거되므로, 약 런 중반(1:30~2:30 구간)에 최대 45s 지속 윈도우가 형성된다.

**2. 화면 변화**

WallOfWisps 최초 픽 직후 필드의 Wisp·Wraith 유닛에 ToughHide 영구 버프가 등록되어 몬스터 색상·버프 아이콘에 변화가 생긴다. IronWill 픽 시 전체 몬스터에 15s(2픽 30s, 3픽 45s) 동안 피격 이펙트가 약해지는 시각 피드백. GuardianRage 픽 시 Wisp·Wraith에 추가 보호 이펙트 오버레이. 두 액티브 버프가 동시에 활성화된 구간에서 영웅의 공격이 Wisp·Wraith에게 시각적으로 거의 튕겨 나가는 수준(데미지 숫자가 최소)으로 보인다. 현재 인게임 버프 스택 수치 UI는 없어 플레이어가 정확한 배율을 알 수 없다.

**3. 입력 행동**

플레이어는 액티브 카드 선택 팝업에서 Tank 축(초록 테두리) 카드를 우선 픽한다. WallOfWisps는 멱등(dedup) 정책으로 1픽 이후 재픽해도 효과가 없지만, 풀 제어가 없으면 2픽·3픽으로 다시 제시될 수 있다. 이 경우 플레이어는 "효과 없는 재픽"으로 Tank 축 카운트 +1만 얻게 된다. IronWill·GuardianRage는 픽마다 지속시간이 15s씩 연장되므로 플레이어가 체감 강화를 느끼며 반복 픽을 유도받는다.

**4. 시스템 반응**

`MonsterBuffService` 내 각 case 가 독립적으로 `DamageTakenScale` 을 곱연산으로 누적 적용한다.

- **ToughHide** (WallOfWisps): `hp.DamageTakenScale *= 0.75f` — Wisp·Wraith, 영구. AddBuff dedup으로 1회만 등록.
- **IronWill** 3픽: `DamageTakenScale *= 0.7f` — 전체 몬스터, Remain 45s.
- **GuardianRage** 3픽: `DamageTakenScale *= 0.5f` — Wisp·Wraith만, Remain 45s.
- **동시 활성 시 Wisp·Wraith 최종 배율**: 0.75 × 0.7 × 0.5 = **0.2625** (데미지 26.25%만 통과).
- 세 상수 모두 BalanceConfig에 노출되지 않은 `MonsterBuffService` 내부 상수로 하드코딩되어 있으며, 복합 결과에 대한 floor clamp 호출 없음.

**5. 반복·재발생 패턴**

IronWill·GuardianRage 3픽 씩 = 각 45s 지속. 두 버프가 동시에 활성인 최대 윈도우는 최초 IronWill 픽 시점 이후 GuardianRage 첫 픽 시점 ~ IronWill 만료 시점까지 겹치는 구간으로, 이론적 최대 45s. 런 5분(300s) 대비 15% 이상의 구간이 극단 방어 상태. ToughHide는 영구이므로 런 전체에 걸쳐 Wisp·Wraith 기저 방어력이 ×0.75 유지된다.

**6. 종료·해소 조건**

IronWill 지속 만료(최대 45s 후) 또는 GuardianRage 지속 만료(최대 45s 후) 시 해당 임시 버프만 해제. ToughHide(×0.75)는 영구이므로 런 내내 남는다. 영웅이 Wisp·Wraith가 아닌 Reaper·Hex 등에 공격을 집중해 생존 경로를 찾을 수 있으나, AI가 가장 가까운 몬스터를 자동 타격하므로 진로 방해 역할인 고 HP Wisp·Wraith가 계속 교전권 안에 들어온다.

**7. 다른 시스템과 상호작용**

- **HP 복합(06-27 감사 이슈와 연계)**: WispHpBoost 3픽(×3.375) + Tank Tier1(×1.3) = Wisp HP 200 × 4.39 = 878 HP. 여기에 데미지 수신 ×0.2625 적용 시 실효 HP = 878 / 0.2625 ≒ **3,344 HP**. 영웅 DPS 50 기준 단일 Wisp 킬타임 ≒ **67초** (1분 7초). MinMonsterDamageTakenScale 손잡이 없이 두 감사 이슈가 공존하면 상승 폭이 배가된다.
- **글로벌 캡**: 필드 캡 18(또는 Tank Tier3 달성 시 24)에서 고 HP Wisp·Wraith가 점거 → 다른 종(Reaper·Hex·Plague·Phantom) 스폰 차단 → Tank 빌드가 Dps 딜 공백을 스스로 만드는 자기모순 상황. 이 역설이 극단 방어력을 자연 상쇄할 수 있으나 튜닝 근거가 문서화되어 있지 않다.
- **Swarm Tier2(스포너 주기 ×0.85)** 와 병렬 픽 시 Wisp 채움 속도 가속 → 캡 점거 더 빨라짐.

**8. 엣지 케이스**

- **WallOfWisps 2픽·3픽 멱등**: ToughHide AddBuff dedup으로 추가 효과 없음. 그러나 Tank 축 카운트 +1은 발생 → 밸런스 관점에서 "효과 없는 픽으로 Tier 달성" 악용 가능. No-op 회피 설계 원칙(`card-renewal.md` §1) 위반 사례.
- **MinDamageTakenScale 부재 + 미래 카드 확장**: 현재 ×0.2625가 사실상 최저지만, v0.3 이후 신규 Tank 카드(예: 영구 추가 방어 버프)가 도입되면 ×0.2625보다 낮아질 수 있다. 열린 설계 상태.
- **IronWill 단독 3픽**: 전체 몬스터 ×0.7, 45s. 2026-06-11 파일(tank-active-defense-triple-stack-floor)에서 단일 카드 floor 관점으로 별도 검토됨. 오늘 이슈와 다른 레이어(단일 vs 교차 복합).

**9. 유저 정보·피드백**

현재 카드 픽 팝업에 데미지 감소 스택 현황이 노출되지 않는다. 플레이어는 "몬스터가 안 죽는다"는 체감만 있고 ×0.2625 수치는 알 수 없다. 밸런서 역시 BalanceConfig 손잡이 없이 상수를 직접 수정해야 조정할 수 있어 반복 튜닝 비용이 높다. `MinMonsterDamageTakenScale` BalanceConfig 손잡이 추가 + 버프 활성 시 인게임 데미지 감소 합계 아이콘(축 아이콘 위 숫자 오버레이) 병행 시, 밸런서는 런타임 SO 편집으로 즉각 조정 가능하고 플레이어도 "내 몬스터가 얼마나 단단한지" 인지할 수 있다.

---

### 보류

- **SpawnWraith 3픽 + Swarm Tier3 Wraith 출력 합산**: Tank 축 패시브·Swarm Tier3 조합. SpawnPhantoms 이슈(2026-06-30)와 메커니즘 유사, Wraith HP 500 고특이성 차별성 있음. 데이터 근거 부족으로 다음 감사 후보.
- **IronWill ×0.7 단독 BalanceConfig handle**: 2026-06-11 감사와 부분 중복 우려 있어 보류.

---

## 3. 과거 감사 대비 차별성

- git log 조회 20건 검토 완료.
- **가장 유사했던 과거 커밋 ①**: `614c299 | 2026-06-27 KST` — "Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale". 차별점: 해당 감사는 **패시브 HP 배율 복합** 이슈. 오늘은 **액티브·영구 혼합 데미지 감소(Defense)** 3중 복합 이슈로 다른 레이어.
- **가장 유사했던 과거 파일**: `2026-06-11-tank-active-defense-triple-stack-floor.md` (git log 외). 파일명 "triple-stack-floor"는 단일 카드 3픽 스택 floor로 추정. 오늘 이슈는 **서로 다른 세 카드**(ToughHide·IronWill·GuardianRage)가 동시 활성될 때의 카드 간 Cross-compound 복합 floor — 다른 시나리오.

---

## 4. 제외 (범위 밖)

- 신규 Tank 카드 설계 — CLAUDE.md §8 신규 카드 리소스 제작 금지
- 영웅 방어력 역강화(영웅이 데미지 리턴) 메커니즘 신설 — 컨셉 §11.2 범위 외
- 서버 기반 글로벌 밸런스 자동 조정 — §8 클라이언트 연동 코드만 허용

---

## 5. 다음 단계 제안

채택 시 game-designer에게 정식 기획 요청:

1. `MinMonsterDamageTakenScale` BalanceConfig 손잡이 신설 (초안 기본값: 0.30 — 데미지 30% 플로어)
2. `MonsterBuffService` 각 case 적용 후 floor clamp 추가 (`hp.DamageTakenScale = Mathf.Max(hp.DamageTakenScale, config.MinMonsterDamageTakenScale)`)
3. 버프 적용 순서 정의 문서화 (ToughHide 영구 → IronWill 임시 → GuardianRage 임시)
4. WallOfWisps(ToughHide) 멱등 2·3픽의 Tank 카운트 기여 정합성 검토 — No-op 회피 원칙과의 충돌 여부 판단

---

## 6. 쉬운 설명 (비개발자 요약)

우리 게임에는 위스프·레이스 같은 몬스터에게 "방어막"을 씌워주는 카드가 세 종류 있다. 이 카드들이 모두 켜지면 몬스터가 원래 받아야 할 피해의 고작 4분의 1만 받게 되어, 영웅이 때려도 때려도 죽지 않는 극단적인 상황이 발생한다. 수학적으로 계산하면 영웅이 위스프 하나를 잡는 데 1분 7초가 걸릴 수도 있다. 문제는 현재 코드에 "아무리 방어막이 쌓여도 최소한 이만큼은 맞아야 한다"는 안전 설정이 없어서, 나중에 카드가 더 추가되면 더 심해질 수 있다는 것이다. 그래서 이번에 제안하는 것은: 세 방어 효과가 겹쳐도 최소 피해 비율을 게임 외부에서 조절할 수 있는 설정값(손잡이)을 추가하자는 것이다.
