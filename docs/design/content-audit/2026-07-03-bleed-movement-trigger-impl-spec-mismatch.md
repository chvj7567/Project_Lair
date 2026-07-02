# Content Audit — 2026-07-03 — BleedEffect "이동/정지 무관" 구현 vs 기획서 "이동 시" 조건 불일치

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (최종 갱신 2026-06-10)
- 참조 spec/plan 수: 30개 specs, 30개 plans
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22)
- 과거 감사 이력 (git log): 20건 (가장 최근: 2026-07-01)

---

## 1. 현황

| 카테고리 | 컨셉 목표 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1명 | 1 (Knight.prefab) | 없음 ✓ |
| 몬스터 | 6종 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) | 없음 ✓ |
| 패시브 카드 | 16장 | 16 (SO 28개 중 P=16) | 없음 ✓ |
| 액티브 카드 | 12장 | 12 (SO 28개 중 A=12) | 없음 ✓ |
| 카드 효과 클래스 | 28개 | 28 (.cs 확인) | 없음 ✓ |

### 계획 있으나 미구현
- **SwarmRush 카드** — `card-renewal.md §3.4` 원안에서 `Multiply` 폐기 후 `SwarmRush`(Phantom 6마리 즉시 소환)로 대체 예정이었으나 미구현. `Multiply.asset`("빠른 번식", `FastBreedingEffect`, 팬텀 스포너 주기 ×0.6) 잔존.
- **DebugAutoPicker 훅** — QA 자동 시뮬 인프라 구동에 필요한 `BattleController.DebugAutoPicker` 델리게이트 미구현 (`docs/qa-reports/2026-05-22.md §3` 요청).
- **액티브 트리거 5→9 복원** — BalanceConfig `_activeThresholds` 현재 5개(30·90·150·210·270s). 컨셉서 §4.2 원안 9회 대비 절반. Tier3 도달 난이도에 영향.

### QA 권고 미해결
- `LairSimWindow`·`SimDriver` 시뮬레이션 인프라 미착수 — `BattleController.DebugAutoPicker` 없이는 자동 플레이 불가. 데이터 부재로 카드별 픽률·승률 검증 불가 상태.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-07-01 | 148ae90 | Dps ReplaceReapersToHex 3픽(×2.197)+Tier1(×1.3) 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계 |
| 2026-06-30 | db9b2d7 | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계 |
| 2026-06-29 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — 액티브 트리거 9→5 감소 후 Tier3 도달 난이도 50% 픽집중 · SynergyTierThreshold 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — BalanceConfig MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-14 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량 하드코딩 — BalanceConfig 손잡이 이관 제안 |
| 2026-06-13 | c07cc2c | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### BleedEffect "이동/정지 무관" 구현 vs 기획서 "이동 시" 조건 불일치 — 출혈 메커니즘 결정 요청

- **카테고리**: Debuff 축 구현-설계 불일치 / 메커니즘 확정 요청
- **요지**: `BleedEffect.cs:7`의 주석에 "(이동/정지 무관)"이 명시되어 있으나, `card-renewal.md §3.3`과 컨셉서 §6·§11.3은 "영웅 이동 시 HP 감소"로 이동 조건을 전제한다. 현행 구현이 기획 의도보다 더 강력하게 동작 중이며, 이를 의도로 승인할지(기획서 텍스트 수정) 또는 이동 트리거로 수정할지(BleedAura 코드 수정) 결정이 필요하다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 5 / 3 / 4 / 5 → **종합 17**
- **근거**:
  - `Assets/_Lair/Scripts/Card/Effects/BleedEffect.cs:7` — `//# 출혈 — 부착 후 _duration 초간 1초당 HP -_ratio% (이동/정지 무관).`
  - `docs/design/card-renewal.md §3.3` #6 — "효과 요약: 영웅 이동 시 HP 감소 (10초)"
  - `docs/design/project_lair_concept.md §6` — "출혈: 영웅 이동 시 HP -1%"
  - `docs/design/project_lair_concept.md §11.3 Debuff 축` #6 — `Bleed | A | 영웅 이동 시 1s당 HP -2%, 10s`
  - `docs/design/card-renewal.md §4.2` Debuff Tier3 — "영웅 이동 시 1s당 HP -1%, 라운드 끝까지"
- **MVP 범위**: 컨셉 §11.2 액티브 카드 12장 확정, §11.3 Debuff 축 카드 7장 내 항목

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**: 30초 간격 액티브 이벤트(BalanceConfig `_activeThresholds` 현재 5회: 30s·90s·150s·210s·270s) 때마다 Debuff 축 액티브 3장(Fear·Bleed·Weaken) 중 하나로 무작위 제시된다. Bleed는 Debuff 축 카드가 많이 픽됐을 때 후보 풀에서 선택될 가능성이 높아진다. 카드 선택 팝업이 일시정지(Time.timeScale=0) 상태에서 열리고 선택을 기다린다.

2. **화면 변화**: 카드 픽 팝업에 보라색 테두리(Debuff 축 `#A855F7`) Bleed 카드가 표시된다. 카드 설명 텍스트에는 "영웅 이동 시 HP 감소 (10초)"가 표시된다. 그러나 실제 구현은 이동 조건 없이 발동하므로, 이 설명 텍스트는 현재 동작을 정확히 반영하지 않는다. 빌드 카운트 바의 Debuff 카운트가 +1 증가한다.

3. **입력 행동**: 플레이어가 Bleed 카드를 클릭 선택한다. 이미 Bleed가 활성 중이면 `BleedAura` 지속시간이 +10s 연장된다(지속시간 누적 정책). 같은 카드 3픽 상한 도달 후에는 후보에서 제외된다.

4. **시스템 반응**: `BleedEffect.Apply` → `ctx.ApplyHeroAura(new BleedAura(0.02f), 10f)` 호출. 현행 `BleedAura`는 영웅이 정지해 있어도 매 틱마다 `hero.HP *= (1 - 0.02f * deltaTime)` 방식으로 HP를 지속 감소시킨다. 이는 "이동/정지 무관" 주석이 명시하는 동작이며, 기획서의 "이동 시" 조건과 다르다.

5. **반복·재발생 패턴**: 전역 3픽 캡으로 최대 3번 픽 가능. 3픽 = 누적 30s 지속. Debuff 축 카드가 5장 쌓이면 Tier2(HeroAttackDown 자동 등록, 영구 ×0.85)가 발화하고, 7장이면 Tier3(EternalBleed — 영구 출혈 등록)이 발화한다. EternalBleed도 기획서상 "이동 시 HP -1%"가 조건이지만, 동일 `BleedAura` 계열 구현이라면 같은 불일치를 가질 가능성이 높다. BleedAura의 이동 조건 로직 유무는 `Assets/_Lair/Scripts/Character/BleedAura.cs` 직접 확인이 필요하다.

6. **종료·해소 조건**: `BleedAura` 지속시간 만료 시 자동 해제된다. 전투 종료(영웅 HP=0으로 승리 또는 5분 타임오버 패배) 시 모든 Aura가 즉시 정리된다. EternalBleed는 `duration=-1f`(무제한)이므로 전투 종료 이전에는 해소되지 않는다.

7. **다른 시스템과 상호작용**: 기획서 의도 기준에서 Fear(영웅 3s 도주)와 Bleed는 강력한 조합이다 — Fear가 영웅의 이동을 강제하여 Bleed 발동을 보장한다. 그러나 현행 구현에서 Bleed는 이동과 무관하게 발동하므로 Fear와의 시너지가 기획서 기준과 다르게 계산된다. TimeStop(영웅 5s 정지)의 경우: 기획 의도라면 정지 중 Bleed 무효 → TimeStop과 Bleed는 배타적 조합이 됨. 하지만 구현에서는 TimeStop 중에도 Bleed가 계속 발동하여 오히려 강한 조합이 된다.

8. **엣지 케이스**: (a) 영웅이 몬스터를 처치해 근처 몬스터가 없어 잠시 정지하는 구간 — 기획서 기준 Bleed 무효/구현 기준 계속 발동. (b) EternalBleed(Tier3)와 Bleed(시한) 동시 활성 — 두 BleedAura가 독립 적용되어 HP가 동시에 이중 감소. 이때 두 Aura 모두 이동 조건 불일치가 있다면 "정지 중에도 3% HP/s 감소"가 영구 지속되는 극단 시나리오가 된다. (c) `_activeThresholds` 현행 5회에서 Debuff 축 7픽(Tier3) 도달은 패시브 9픽 중 최소 4픽을 Debuff에 써야 하므로, EternalBleed 발화 빈도 자체가 현재는 낮다.

9. **유저 정보·피드백**: 카드 설명 텍스트 "이동 시 HP 감소"와 실제 동작("항상 HP 감소")이 불일치하여 플레이어가 오해할 수 있다. 영웅을 정지시키는 전략(특정 위치에 묶어두기 등)을 시도해도 Bleed가 계속 작동해 "왜 안 멈추지?" 혼란을 야기할 수 있다. 반대로 이를 발견한 플레이어는 Bleed를 더 강력한 카드로 인식해 Debuff 빌드를 선호하게 될 수 있다. 기획 의도 확정 후 카드 설명 텍스트와 코드 중 하나를 맞춰야 한다.

### 보류
- SwarmRush 카드 구현 제안: CLAUDE.md §8 "신규 영웅·몬스터·카드 리소스 제작 금지" 위반. 범위 승격 전 착수 불가.
- BloodThirst HealAmount 추가 손잡이: 2026-06-14에 이미 제안됨.
- Fear+Bleed 교차 시너지 설계 명시: 본 감사의 핵심인 Bleed 트리거 불일치가 먼저 해소되어야 Fear와의 시너지 설계가 의미를 가진다.

---

## 3. 과거 감사 대비 차별성

git log 조회 20건 검토 완료. 가장 유사했던 과거 커밋: `db9b2d7 | 2026-06-30` "Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계".

차별점:
- **과거(2026-06-30)**: Bleed 카드의 **수치적 누적 상한** 분석 — "3픽+Tier3 조합 시 출혈 합계 비율이 얼마인지, BalanceConfig에 상한 손잡이가 없다"는 밸런스 수치 이슈.
- **이번(2026-07-03)**: Bleed 카드의 **메커니즘 정의 불일치** — "이동 시 발동"이라는 트리거 조건 자체가 코드와 기획서에서 서로 다르게 정의되어 있다는 구현-설계 일관성 이슈.

두 발견은 같은 카드(Bleed)를 다루지만 분석 차원이 근본적으로 다르다. 수치 문제는 손잡이로 해결할 수 있지만, 메커니즘 불일치는 "어느 쪽이 옳은가" 결정이 먼저 필요하다.

직전 7일 내 Debuff 카테고리 2건(2026-06-28 SpawnPlagues, 2026-06-30 Bleed+EternalBleed) 있음 — 신중한 차별 근거: 선행 감사 모두 "BalanceConfig 수치 손잡이 미설계" 유형이며 이번은 "코드-기획서 트리거 정의 불일치 → 게임플레이 메커니즘 결정" 유형으로 이슈 카테고리가 다름.

---

## 4. 제외 (범위 밖)

- EternalBleedAura 코드 직접 수정: 이번 감사는 이슈 발굴만. 실제 수정은 game-designer 결정 후 gameplay-programmer 위임.
- Debuff Tier3 EternalBleed 발동 빈도 증가(액티브 트리거 9회 복원): 별도 밸런스 사이클 이슈.
- SwarmRush 신규 카드 추가: CLAUDE.md §8 신규 카드 리소스 제작 금지 제약.

---

## 5. 다음 단계 제안

- **채택 시 game-designer 에게 정식 기획 요청**: 다음 두 선택지 중 하나를 결정.
  - (a) **기획서 유지(이동 트리거 복원)**: `BleedAura.cs`에 매 틱 영웅 이동 속도(velocity magnitude) 임계값 체크 추가 → 정지 시 감소 중단. EternalBleedAura도 동일 처리.
  - (b) **구현 승인(이동/정지 무관 공식화)**: `card-renewal.md §3.3` 설명 "이동 시 HP 감소" → "HP 감소 (10초)" 로 수정. 카드 displayName description SO 텍스트도 동기화. Debuff Tier3 기획서도 갱신.
- 결정 전까지는 BleedEffect·EternalBleedAura 코드 미수정 권고 (의도 불명 상태에서 수정하면 두 번 고쳐야 할 가능성).

---

## 6. 쉬운 설명 (비개발자 요약)

출혈(Bleed) 카드의 기획서에는 "영웅이 걸어다닐 때만 피가 깎인다"라고 적혀 있다. 마치 뛰다 보면 상처가 벌어지는 것처럼. 그런데 실제 게임 코드를 열어보면, 영웅이 가만히 서 있어도 피가 계속 줄어들도록 만들어져 있다. 이 차이 때문에 출혈 카드가 기획서보다 훨씬 강력하게 동작 중이며, 특히 "영구 출혈(Tier3 보너스)"까지 붙으면 영웅이 꼼짝만 안 해도 계속 피를 흘리는 상황이 된다. 기획서와 코드 중 어느 쪽이 맞는 게임을 만들려는 것인지 개발팀이 먼저 결정해야 한다. 그래서 이번에 제안하는 것은: 출혈 카드가 "움직일 때만" 발동해야 하는지, 아니면 "항상" 발동해야 하는지를 공식으로 정하고, 기획서와 코드를 맞춰달라는 것이다.
