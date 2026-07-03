# Content Audit — 2026-07-04 — Dps 패시브 HexRangeBoost 3픽+Tier3 복합(×3.567) — 영웅 AI 타격 우선순위 영구 회피 + MaxHexRangeMul 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

---

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (docs/design/project_lair_concept.md)
- 참조 spec/plan 수: 30개 spec, 29개 plan
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, 상태: BLOCKED — 시뮬 미실행)
- 과거 감사 이력 (git log): 21건 (가장 최근: 2026-07-02)

---

## 1. 현황

### 구현 현황 대비 컨셉 §11.3 수치

| 카테고리 | 컨셉 목표 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (기사) | 1개 (Knight.prefab) | 없음 |
| 몬스터 | 6종 | 6개 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 없음 |
| 패시브 카드 | 16장 | 16장 (.asset 확인) | 없음 |
| 액티브 카드 | 12장 | 12장 (.asset 확인, Berserk.asset = GuardianRage 효과) | 없음 |

### 계획 있으나 미구현

- **SwarmRush (Swarm 액티브)**: card-renewal.md §3.4 — Multiply 자리를 SwarmRushEffect(Phantom 6마리 즉시 소환)로 교체 예정이었으나 현행 FastBreedingEffect(팬텀 스포너 주기 ×0.6) 잔존. 별도 구현 사이클 필요.
- **DebugAutoPicker 훅**: qa-reports/2026-05-22 §3 — BattleController 에 `#if UNITY_EDITOR` 자동 픽 델리게이트 추가 미구현. qa-simulator 전체 실행 블로킹.

### QA 권고 미해결

- QA 시뮬레이션 1건 (2026-05-22): **BLOCKED** — 카드 자동 픽 API 없어 시뮬 미실행. 헤드리스 N판 메트릭(평균 사망 시각, 클리어율, 카드별 픽률) 미수집 상태.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 손잡이 추가 제안 |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale |
| 2026-06-13 | c07cc2c | BalanceConfig 손잡이 추가 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-14 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 이관 제안 |
| 2026-06-15 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 이속 10.53 m/s — MaxMoveSpeedScale |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 검증 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 — 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 — MaxTankPowerScale |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — SynergyTierThreshold 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale |
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 |
| 2026-06-29 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput |
| 2026-06-30 | db9b2d7 | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec |
| 2026-07-01 | 148ae90 | Dps ReplaceReapersToHex 3픽+Tier1 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계 |
| 2026-07-02 | ea6803e | Debuff 액티브 Bleed '이동/정지 무관' 구현-설계 불일치 — 트리거 조건 결정 요청 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### HexRangeBoost 3픽(×2.744) + Dps Tier3(×1.3) 복합 Hex 사거리 5.35 units — 영웅 AI 타격 우선순위 영구 회피 구조 + MaxHexRangeMul 손잡이 미설계

- **카테고리**: Dps 패시브 / BalanceConfig 손잡이 추가
- **요지**: HexRangeBoost 3픽+Dps Tier3 복합 시 Hex 사거리가 1.5→5.35 units로 팽창한다. 영웅 AI는 "가장 가까운 몬스터" 우선이므로, 근접 몬스터(Wisp·Plague·Reaper 등 1.5 units)가 필드에 있는 한 영웅이 Hex를 타격하지 않는 구조가 고착된다. BalanceConfig에 MaxHexRangeMul 손잡이가 없어 이 상태의 상한을 외부에서 조정할 방법이 없다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 5 / 2 / 3 / 5 → 종합 **17**
- **근거**:
  - `Assets/_Lair/Scripts/Card/Effects/HexRangeBoostEffect.cs` — `_rangeMul = 1.4f`, `ctx.RegisterMonsterTypeBuff(EMonster.Hex, EMonsterStatKind.Range, _rangeMul)`
  - `Assets/_Lair/Scripts/Card/Synergy/DpsSynergyTier3.cs` — `ctx.RegisterMonsterTypeBuff(EMonster.Hex, EMonsterStatKind.Range, RangeMul)` (Tier3 발화 시 Reaper·Hex 공통)
  - `Assets/_Lair/Art/Characters/Hex.prefab` L140 — `_range: 1.5` (MeleeAttacker 기반)
  - `Assets/_Lair/Scripts/Battle/CircularSpawnerArranger.cs` — `_radius = 13f` (스포너 링 반경)
  - 계산: 1.5 × 1.4³ × 1.3 = 1.5 × 2.744 × 1.3 = **5.35 units** (3픽 캡 docs/design/card-3pick-cap.md 적용)
  - 영웅 AI 근거: 컨셉서 §11.3 "AI: 가장 가까운 몬스터에게 자동 이동 후 공격"
- **MVP 범위**: 컨셉 §11.3 Dps 축 패시브 #2 HexRangeBoost + Dps Tier3 (§5.2 / §11.3 4축 시너지표)

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**

영웅 HP가 10%씩 떨어질 때 발동하는 패시브 선택지 팝업에서 Dps 축 카드가 3택 1로 제시된다. HexRangeBoost는 Dps 패시브 4장 중 1장이므로 라운드 중 평균 4~5회 이상 선택지에 등장할 수 있다. 누적 3픽이 완료되면 3픽 캡이 해당 카드를 선택지에서 제외한다. Dps Tier3(사거리 ×1.3)은 Dps 축 픽 카운트 7 도달 시 즉시 발화하며, HexRangeBoost 3픽(카운트 3) 이후 추가 Dps 카드를 4장 더 픽하면 발동한다.

**2. 화면 변화**

HexRangeBoost 픽 직후 필드의 Hex 몬스터 전체에 글로벌 사거리 버프가 즉시 영구 적용된다. 시각적으로는 Hex가 영웅에게 다가가다가 이전보다 더 먼 거리에서 걸음을 멈추고 공격 모션을 시작하는 것으로 관찰된다. Dps Tier3 발화 시 동일 현상이 한 번 더 강화되어 Hex가 약 5.35 units 거리에서 정지하는 최종 포지셔닝이 고착된다. 시너지 패널의 Dps 빌드 카운트 바가 7에 도달하며 Tier3 마커 3개가 표시된다.

**3. 입력 행동**

플레이어는 총 7회 이상 Dps 축 카드를 선택한다. HexRangeBoost를 3픽(동일 카드 3번 선택 또는 패시브 선택 중 누적)하고, 이후 Dps 카드 4장을 추가로 선택하면 Tier3이 발화한다. 실제로는 Dps 집중 빌드를 의도하면 자연스럽게 도달하는 경로다.

**4. 시스템 반응**

HexRangeBoostEffect.Apply()가 호출될 때마다 ctx.RegisterMonsterTypeBuff(EMonster.Hex, Range, 1.4)가 실행되어 누적 곱연산된다. DpsSynergyTier3.Apply()는 동일 경로로 Range ×1.3을 추가한다. MeleeAttacker의 Range 필드(기존 1.5)에 이 배율이 반영되어 Hex의 공격 판정 거리가 5.35 units가 된다. AutoCombatAI는 dist > _range 조건으로 접근을 중단하므로 Hex는 영웅으로부터 약 5.35 units 위치에 정지해 공격을 시작한다.

**5. 반복·재발생 패턴**

Hex의 포지셔닝이 5.35 units로 고착된 후에는 영웅 AI(가장 가까운 몬스터 우선)가 1.5 units 이내의 근접 몬스터(Wisp·Reaper·Plague 등)를 우선 타격하게 된다. 스포너가 지속 스폰하는 한 근접 몬스터는 계속 보충되며, Hex는 매 스폰 사이클마다 5.35 units 정지 포지션에 자리 잡는다. 이 패턴은 런 종료까지 반복된다.

**6. 종료·해소 조건**

영웅이 모든 근접 몬스터를 처치하고 필드에 Hex만 남을 때 비로소 영웅이 Hex를 타격하기 위해 이동을 시작한다. 그러나 지속 스폰(30초 동안 스포너가 일정 주기로 몬스터 보충)으로 인해 실제로 이 상태는 일시적일 가능성이 높다. 글로벌 캡(18, Tank Tier3 시 24)에 도달할 경우 스포너 백오프가 발생하여 일시적으로 필드가 비워지기도 한다.

**7. 다른 시스템과 상호작용**

- **ReplaceReapersToHex(처형 명령)** 3픽(Power ×2.197) + Dps Tier1(Power ×1.3) 동시 발동 시 Hex의 DPS가 동시에 증폭되어, 영웅 타격 없이 후방에서 높은 DPS를 안정적으로 유지하는 Hex 특화 빌드가 완성된다. Dps 축 7픽(Tier3 도달) 조건상 HexRangeBoost 3픽 + ReplaceReapersToHex 2픽 + 나머지 Dps 카드 2픽으로 충족 가능.
- **MarkOfDeath(죽음의 표식)** 액티브 사용 시 영웅 받는 데미지 ×1.5 (5초). 원거리에서 안전하게 유지되는 Hex가 이 버프 기간에 높은 DPS를 집중 투여할 수 있다.
- **Tank Tier3(글로벌 캡 18→24)** 동시 보유 시 더 많은 근접 몬스터가 필드에 유지되어 영웅이 Hex를 더 오래 무시하는 상황이 강화된다.
- **Fear/TimeStop** 등 도주 액티브 사용 시 영웅이 원거리로 이동하게 되어 Hex의 공격 가능 거리에 진입하는 경우가 생길 수 있다. 단 영웅은 Hex를 향해 도주하는 것이 아니라 가장 가까운 몬스터 반대 방향으로 이동한다.

**8. 엣지 케이스**

- **Hex만 남은 상황**: 모든 근접 몬스터가 사망하고 Hex만 필드에 존재하면 영웅이 Hex에게 접근을 시작한다. 영웅이 Hex에게 1.5 units까지 접근하면 영웅이 Hex를 공격한다. 그 사이에 Hex는 후퇴하지 않고(AutoCombatAI는 플레이어 구조상 후퇴 로직 없음) 계속 공격하므로 결국 영웅이 Hex에 맞을 수 있다.
- **범위 중첩 계산 오류 가능성**: RegisterMonsterTypeBuff가 Range stat을 곱연산 누적할 때 내부 타입 검증이 없으면 3픽 캡 이후에도 시너지 Tier 발화로 추가 Range 배율이 적용될 수 있다. 이는 3픽 캡의 의도(SpawnerHaste 상한 ×0.512 보장)와 동일한 보호가 Range 계열에 적용되는지 명시되지 않은 상태.
- **스포너 링 내부 포지셔닝**: 스포너 링 반경 13 units, Hex 최대 사거리 5.35 units. Hex는 스포너에서 출발해 약 7.65 units 이동 후 정지한다. 이 포지션에서 영웅 방향이 아닌 측면으로의 다른 몬스터 충돌이 발생할 경우 Hex의 실제 정지 거리가 달라질 수 있다.

**9. 유저 정보·피드백**

현재 카드 선택지 팝업의 Dps 축 카운트 바가 쌓이면 Hex의 사거리 변화가 시각적으로 표현되지 않는다. 영웅이 Hex를 타격하지 않는 현상이 직관적으로 이해되지 않을 수 있다. "왜 영웅이 저 몬스터를 안 때리지?" 라는 의문이 발생할 수 있으며, 이것이 의도된 시너지인지 버그인지 플레이어가 구별하기 어렵다. 빌드 설명에 "Hex가 영웅의 공격 사각에 위치" 라는 문구 또는 Range 배율 표시가 없다.

### 보류

없음. 이번 회차 후보 분석 결과 상기 1개가 단독 최고 점수(17점)이며 과거 21건 어느 회차와도 카테고리·요지·근거 3항목이 동시에 겹치지 않는다.

---

## 3. 과거 감사 대비 차별성

git log 조회 21건 검토 완료.

가장 유사했던 과거 커밋 2건:
1. **148ae90 (2026-07-01)** — "Dps ReplaceReapersToHex 3픽+Tier1 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계"
   - 공통: Dps 축, 3픽 복합, BalanceConfig 손잡이 누락
   - **차별점**: 대상 카드(HexRangeBoost vs ReplaceReapersToHex), 스탯 종류(Range vs Power), 게임플레이 우려(영웅 AI 타격 회피 구조 vs DPS 수치 과도), 연동 Tier(Tier3 Range vs Tier1 Power)가 완전히 다르다. Power 과도 문제는 "너무 많은 데미지"이고, Range 과도 문제는 "영웅이 Hex를 물리적으로 타격하지 않는 영구 구조 형성"으로 게임플레이 층위 자체가 다르다.
2. **b83b566 (2026-06-23)** — "Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계"
   - 공통: Dps 패시브, 3픽 곱연산 복합, BalanceConfig 손잡이
   - **차별점**: 대상 카드(HexRangeBoost vs ReaperAtkSpeed), 스탯(Range vs Cooldown), 우려 기제(AI 위치·포지셔닝 vs 공격 속도 주기), Tier 연동(Tier3 vs Tier2). 쿨다운 과도는 "시간 축" 문제이고 Range 과도는 "공간 축" 문제다.

직전 7일 이내 Dps 카테고리 출현(07-01): 차별 근거로 ①카드 완전 다름 ②스탯 차원 다름 ③게임플레이 층위(AI 행동 vs 수치) 다름을 충족하므로 채택 유효.

---

## 4. 제외 (범위 밖)

- **SwarmRush 신규 구현**: card-renewal.md §3.4에 의도가 명시되어 있으나 v0.3 현 단계에서 신규 카드 리소스 제작 금지(CLAUDE.md §8). 기존 Multiply 교체 작업은 별도 사이클 필요.
- **영웅 추가**: 컨셉 §11 범위 밖 (영웅 1명 고정).
- **몬스터 신규 종**: 컨셉 §11 범위 밖 (6종 고정, Hex 원거리 리팩터링도 아직 범위 미확정).

---

## 5. 다음 단계 제안

- **채택 시**: game-designer에게 HexRangeBoost 효과 상한 설계 요청 — BalanceConfig에 `MaxHexRangeMul` 필드 추가 + HexRangeBoostEffect 및 DpsSynergyTier3 적용 시 상한 적용 로직 작성.
- **병행 검토 제안**: AutoCombatAI의 영웅 타겟 선택 로직에서 "거리 기반 우선순위"가 Range가 큰 몬스터를 영구 배제하는지 code-reviewer가 확인. 영용 AI spec(컨셉 §11.3 "가장 가까운 몬스터")과의 상호작용 문서화.
- **DebugAutoPicker 훅 (미해결 블로커)**: 실제 시뮬 데이터로 Hex 생존율 및 영웅 타격 분포를 검증하려면 qa-reports/2026-05-22 §3의 훅 구현이 선행되어야 함.

---

## 6. 쉬운 설명 (비개발자 요약)

우리 던전에는 헥스라는 원거리 공격 몬스터가 있다. 보통은 영웅이 가장 가까이 있는 몬스터를 골라 때리는데, 헥스를 세게 키우면 헥스가 영웅에게서 멀리 떨어진 곳에서 공격하게 된다. 이렇게 되면 영웅 바로 옆에 있는 다른 몬스터들만 계속 얻어맞고, 헥스는 안전한 거리에서 혼자 공격만 하는 상황이 생긴다. 이게 나쁜 건 아니지만, 얼마나 멀어질 수 있는지 제한이 없어서 원래 의도보다 훨씬 강력해질 수 있다. 그래서 이번에 제안하는 것은: 헥스 사거리가 너무 늘어나지 않도록 조절 손잡이(BalanceConfig MaxHexRangeMul)를 추가하고, 영웅이 헥스를 완전히 무시하는 상황이 의도된 것인지 설계 문서에 명확히 남기자는 것이다.
