# Content Audit — 2026-06-29 — Plague 다중 공격 동시 슬로우 중첩 방식 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (작성일 2026-05-18, 최종 갱신 2026-06-10)
- 참조 spec/plan 수: 30개 (specs/ 29, plans/ 29 — 일부 중복)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22)
- 과거 감사 이력 (git log `# [Routines][Daily Content Audit]`): 17건 (가장 최근: 2026-06-27 614c299)

---

## 1. 현황

### 카테고리별 구현 현황

| 카테고리 | 컨셉 §11.3 기준 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (기사) | 1 (`Knight.prefab`) | 없음 |
| 몬스터 | 6종 | 6 (`Wisp·Wraith·Reaper·Hex·Plague·Phantom.prefab`) | 없음 |
| 패시브 카드 | 16장 | 16 (`Assets/_Lair/Art/Cards/Items/` — 패시브 16) | 없음 |
| 액티브 카드 | 12장 | 12 (단, `Multiply.asset` 잔존 — SwarmRush 미구현) | 설계 간극 1 |

### 계획 있으나 미구현

- **SwarmRush 카드** (`card-renewal.md` §3.4): 원안은 `Multiply` 폐기 → `SwarmRushEffect`(팬텀 6마리 즉시 소환) 신설. 현재 `FastBreedingEffect`("빠른 번식", 팬텀 스포너 주기 ×0.6 영구) 잔존. "광역 압살 우려 일부 잔존" 명기됨.
- **DebugAutoPicker 훅** (`docs/qa-reports/2026-05-22.md` §3): QA 시뮬레이션 차단 해제를 위해 `BattleController` 에 `#if UNITY_EDITOR` Func 훅 추가 요청. 구현 여부 불명 — 미확인.
- **Plague 다중 공격 슬로우 중첩 방식**: `card-renewal.md` §3.3 은 단일 Plague 의 SlowFactor 배율만 정의. 복수 Plague 동시 공격 시 슬로우가 중첩(multiplicative)되는지, 단일 인스턴스로 dedup 되는지 설계 문서 없음.

### QA 권고 미해결

- **DebugAutoPicker 훅 미구현** (`docs/qa-reports/2026-05-22.md` §3): 훅 없이는 카드 픽 자동화 불가 → 밸런스 시뮬레이션 전면 차단. 5단계 이후 조치 요청 미이행.
- **컨셉 §8 밸런싱 기준 검증 미실시**: 영웅 평균 사망 2~4분 목표 검증 시뮬 미실행.

### 과거 감사 후보 (git log 조회 결과 — `# [Routines][Daily Content Audit]`)

| 날짜 (KST 기준 문서명) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-27 | 614c299 | Tank WispHpBoost·WraithDamageBoost 3픽+Tier1 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-26 | 63ecbd3 | Swarm Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor 손잡이 미설계 |
| 2026-06-25 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — SynergyTierThreshold 손잡이 미설계 |
| 2026-06-24 | b83b566 | Dps ReaperAtkSpeed 3픽+Tier2 복합 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-23 | 9118936 | Swarm Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-21 | a1e0ba4 | Debuff PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-20 | 0fb40b1 | Tank ReplaceWispsToWraith 3픽 Power ×2.197 + Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계 |
| 2026-06-19 | dcaa8b7 | Dps Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 손잡이 미설계 |
| 2026-06-18 | 3a9bed3 | Swarm SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-17 | d8fdcfe | Swarm PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-16 | 68db140 | Debuff HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-15 | 6e02b2a | Dps BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — 종별 HP 불균형 손잡이 이관 제안 |
| 2026-06-14 | c07cc2c | Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 제안 |
| 2026-06-13 | 8de2ecb | Debuff HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-12 | e4c765b | Swarm TimeStop·Fear 지속시간 누적 상한 캡 손잡이 추가 제안 |
| 2026-06-11 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-10 | 440794c | Dps HexRangeBoost 3픽+Tier3 중첩 ring 반경 초과 배율 재조정 제안 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Plague 다중 공격 동시 슬로우 중첩 방식 미설계 — SpawnPlagues 3픽 4마리 동시 공격 조건에서 MinHeroMoveSpeedScale 이중 보호 누락

- **카테고리**: 패시브 (SpawnPlagues) + 몬스터 시너지 / BalanceConfig 손잡이
- **요지**: SpawnPlagues 3픽으로 Plague 스포너 동시 출력이 4마리/틱이 되면, 4마리가 동시에 영웅에 접촉해 슬로우를 적용하는 시나리오가 발생한다. 단일 Plague 의 SlowFactor 는 PlagueSlowBoost 3픽 + Debuff Tier1 복합 시 0.27(영웅 이속 27%)임이 이전 감사(a1e0ba4, 2026-06-21)에서 확인됐지만, 복수 Plague 인스턴스가 동시에 동일 슬로우를 적용할 때 중첩(multiplicative) 여부 및 그에 따른 추가 이속 하한이 설계되어 있지 않다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 4 / 2 / 3 / 4 → **종합 13** (계산: 4 + (6-2) + 3 + 4 = 15)
- **근거**:
  - `docs/design/card-renewal.md` §3.3: SpawnPlagues "Plague 스포너 출력 +1, 3픽 → +3 (가산 누적)" → 기본 1 + 3 = 4마리/틱
  - 컨셉 §11.3: Plague "공격 시 영웅 둔화 20% (SlowFactor 기본 0.8)"
  - `docs/design/card-renewal.md` §3.3: PlagueSlowBoost "SlowFactor ×0.75, 3픽 → ×0.75³ = ×0.4219", 기본 0.8 × 0.4219 = 0.3375
  - `docs/design/card-renewal.md` §4.2 Debuff Tier1: "Plague SlowFactor ×0.8 추가" → 0.3375 × 0.8 = 0.27
  - 중첩 시나리오: 4마리 동시 공격, 각각 SlowFactor 0.27 적용 → 곱연산이면 0.27^4 ≈ 0.0053 (이속 0.5%), 단일 인스턴스 dedup 이면 여전히 0.27 (기존 최저값과 동일)
  - 어느 경로인지 `MonsterBuffService` / `HeroAura` 관련 슬로우 적용 코드에서 명시적 설계 없음
- **MVP 범위**: 컨셉 §11.3 (몬스터 6종 Plague 포함), §11.3 Debuff 패시브 카드 (PlagueSlowBoost, SpawnPlagues)

#### 유저 플로우

1. **노출 시점·트리거**: 패시브 이벤트(영웅 HP 10%마다)에서 카드 픽 팝업이 열릴 때 SpawnPlagues("역병 증식", Debuff 보라색 테두리)가 3택 중 하나로 등장한다. 런 초반부터 Debuff 빌드를 집중 픽한 플레이어가 3번째 SpawnPlagues 를 선택하는 시점에 이 시나리오가 활성화된다.

2. **화면 변화**: SpawnPlagues 3픽 직후 빌드 카운트 바(BattleHud 좌상단)의 Debuff 카운트가 +1 누적된다. 시너지 토스트("Debuff 시너지 Tier N 발동!")가 표시될 수 있다. 전투 씬에서는 즉각적인 시각 변화가 없으나, 다음 Plague 스포너 틱(10초 주기)에서 Plague 4마리가 한 번에 스폰되어 화면에 보라색 납작한 큐브들이 무리지어 등장한다.

3. **입력 행동**: 플레이어는 SpawnPlagues 카드를 3번 선택한다(3개 서로 다른 픽 팝업에서, 또는 전역 3픽 캡 도달 전). 이후의 슬로우 중첩은 플레이어 입력 없이 자동전투 AI 에 의해 발생하므로, 플레이어는 결과를 관찰만 한다.

4. **시스템 반응**: Plague 스포너(#4, 180°, 주기 10s)가 기본 동시 출력 1 → 4로 증가한다. 10초마다 4마리 Plague 가 (-14.0, 0.0) 위치에서 스폰되어 영웅 방향으로 수렴 이동한다. Plague HP 50, DPS 5 로 약하지만, 이동 후 영웅에 접촉·공격하면 각 Plague 가 독립적으로 슬로우를 시도한다. 이 시도가 "각각 별도 슬로우 스택을 쌓는가, 아니면 MonsterBuffService 의 dedup 로직이 동일 SlowFactor 를 한 번만 적용하는가"가 현재 미정이다.

5. **반복·재발생 패턴**: 10초마다 Plague 4마리 배출이 반복된다. 글로벌 캡(18)까지 쌓이면 자연 백오프하지만, Plague HP 50 으로 영웅에 의해 빠르게 제거되므로 사실상 매 10초 초기화에 가깝다. PlagueSlowBoost 3픽 + Debuff Tier1 상태에서는 슬로우율이 0.27(이속 27%)로 강화되어 있어, 4마리가 동시에 공격하는 순간마다 중첩 여부가 결과에 미치는 영향이 크다.

6. **종료·해소 조건**: 영웅이 처치되면 런 종료(승리). 5분 타임오버 시 패배. 슬로우 디버프 자체는 Plague 가 영웅 공격을 멈추거나 영웅이 Plague 를 제거하면 자연 해소된다. PlagueSlowBoost 가 있으면 SlowFactor 가 영구 강화 상태이므로, 런 종료까지 지속되는 구조적 상태다.

7. **다른 시스템과 상호작용**: (a) `PlagueSlowBoost` 패시브 3픽: 단일 Plague 슬로우율 강화 → 본 시나리오의 기본 전제. (b) Debuff Tier1: Plague SlowFactor ×0.8 추가 발화 → 0.27 기준 강화. (c) `HeroPoisonAura` 패시브: 영웅 발 밑 독장판이 활성화된 상태에서 영웅이 이속 0.5% 로 제자리에 가까워지면 독장판 DPS 5가 사실상 영구 지속 → 독 + 이속 0% 조합으로 영웅이 사망 전 필드에 "갇히는" 극단 상황 가능. (d) `FleeStabilizeCenterPull` (flee-stabilize-center-pull 기획서): Fear 카드로 도주 중인 영웅이 슬로우에 걸리면 flee AI 의 이동 가중치가 슬로우와 충돌 — 이속 근 0에서의 flee 동작 미검증.

8. **엣지 케이스**: (a) 4마리가 정확히 동일 틱에 영웅을 공격하는 경우: SlowFactor 중첩 계산이 `MonsterBuffService.AddBuff` 의 dedup 로직과 충돌할 수 있다. dedup 가 `EMonsterBuff` 단위로만 작동하면 (슬로우는 Plague 종의 별도 기능), 4마리가 각자 슬로우를 독립 적용 가능. (b) Plague 가 영웅을 공격한 직후 제거되는 경우: 슬로우 잔여 시간 중 다른 Plague 가 추가 공격하면 기존 인스턴스 갱신(duration 연장) vs 새 인스턴스 추가 중 어느 경로인지 불명. (c) 이속 0% 극단: 영웅 AI(`AutoCombatAI`)가 이속 0 인 경우에 대한 예외 처리 없으면 `NavMesh.SetDestination` 호출이 제자리 루프를 반복하며 CPU 부하 발생 가능.

9. **유저 정보·피드백**: 현재 영웅 이속 감소는 `hero-status-icons.md` 의 슬로우 인디케이터(하늘 반투명 Sphere)로 표시된다. 그러나 이속이 극단(0.5% 이하)으로 떨어지는 경우 시각적으로는 "영웅이 거의 안 움직임"으로만 보이며 플레이어가 이것이 의도된 밸런스인지 버그인지 판단하기 어렵다. 또한 Plague 4마리가 동시에 영웅을 공격하는 장면에서 각 슬로우 스택이 UI 에 별도 표시되지 않으므로 정보가 불투명하다.

### 보류

- **Weaken (Debuff Active) 단독 + HeroAttackDown 영구 복합 시 영웅 공격력 최저값**: 점수 14 (4+(6-2)+3+4). Weaken(`_factor=0.5, _duration=10`) 과 HeroAttackDown 3픽(×0.75³=×0.422) 복합 시 영웅 공격력 ≈18% 기준값이 되며, 이미 2026-06-13 HeroAttackDown floor 감사와 요지 유사. 향후 별도 사이클에서 검토 권장.
- **SpawnReapers (Dps Passive) 3픽 Reaper 4마리/틱 + Dps Tier2 복합 DPS 피크**: 점수 12. Reaper 스포너(12s) 출력 4 × DPS 151 (ReaperAtkSpeed 3픽 + Tier2) = 604 DPS 버스트. 구현 비용 낮으나 Dps Tier2 감사(Reaper cooldown, 2026-06-24)와 카테고리 근접. 보류.

---

## 3. 과거 감사 대비 차별성

- git log `# [Routines][Daily Content Audit]` 조회: 17건 검토 완료.
- 가장 유사한 과거 커밋: **a1e0ba4 (2026-06-21)** "Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계"
- **차별점**: a1e0ba4 는 *단일 Plague 의 SlowFactor 배율이 카드 픽(PlagueSlowBoost) 에 의해 강화되는 수치 floor* 를 다뤘다. 본 감사는 *SpawnPlagues 픽으로 Plague 머릿수가 4마리/틱이 될 때, 복수 Plague 인스턴스가 동시에 슬로우를 적용하는 중첩 메커니즘 자체가 미설계* 임을 지적한다. 원인(카드 수치 vs. 인스턴스 중첩), 메커니즘(SlowFactor 배율 vs. AddBuff 동작), 설계 공백(손잡이 누락 vs. 중첩 정책 미정의)이 모두 다르다.
- 이 주제를 다룬 다른 git log 항목 없음. Debuff 카테고리 마지막 감사: 2026-06-21 (8일 전). 7일 경계와 인접하나 주제 차별성이 명확해 채택.

---

## 4. 제외 (범위 밖)

- **SwarmRush 카드 신규 구현**: 범위 내(§11.3 Swarm 액티브)이나 별도 감사 문서(2026-06-04 폴더 기록) 에서 다뤄진 이력이 있어 이번 회차에서는 제외.
- **QA DebugAutoPicker 훅 구현 자체**: 기술 인프라 과제이며 콘텐츠 감사 후보 범주가 아님. 해당 구현 요청은 QA 리포트 §3 에 별도 기록.
- **Plague 신규 종 또는 슬로우 능력 변경**: CLAUDE.md §8 "신규 영웅·몬스터·카드 리소스 제작 금지". 본 제안은 기존 Plague + SpawnPlagues 카드 메커니즘만 다루므로 범위 내.

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청: Plague 동시 공격 슬로우 중첩 정책 설계 (단일 인스턴스 dedup vs. 캡 있는 multiplicative stacking)
- 선행 확인 권장: `Assets/_Lair/Scripts/Character/` 또는 `Battle/` 내 Plague 의 슬로우 on-hit 구현 코드 검토 — `MonsterBuffService` 위임인지, 별도 Plague 컴포넌트 내 처리인지 확인
- 중첩 허용 시: `BalanceConfig` 에 `MaxSlowStackDepth` 또는 `MinHeroMoveSpeedScaleMultiInstance` 손잡이 추가 제안
- 단일 인스턴스 dedup 확정 시: 2026-06-21 감사(a1e0ba4)의 MinHeroMoveSpeedScale 손잡이로 동일 보호 — 추가 조치 불요

---

## 6. 쉬운 설명 (비개발자 요약)

게임에서 플레이그(Plague)라는 보라색 몬스터는 영웅에게 달라붙어 공격할 때 영웅의 발을 느리게 만든다. 카드를 잘 고르면 이 몬스터가 한 번에 4마리씩 무더기로 나올 수 있다. 문제는 그 4마리가 동시에 영웅을 공격할 때 "느리게 만드는 효과"가 4겹으로 쌓이는지, 아니면 한 번만 적용되는지 아직 설계가 되어 있지 않다는 것이다. 만약 4겹으로 쌓인다면 영웅이 거의 움직이지 못할 만큼(속도 0.5%!) 느려질 수 있는데, 이게 의도된 강력한 전략인지 아니면 게임을 너무 쉽게 만드는 버그인지 판단 기준이 없다. 그래서 이번에 제안하는 것은: 여러 마리의 플레이그가 동시에 공격할 때 느리게 만드는 효과가 어떻게 쌓이는지 규칙을 명확히 정하고, 영웅이 지나치게 느려지지 않도록 속도 하한선을 설정하는 것이다.
