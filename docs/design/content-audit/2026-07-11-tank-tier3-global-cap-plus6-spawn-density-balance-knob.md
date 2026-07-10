# Content Audit — 2026-07-11 — Tank Tier3 필드 캡 +6 발동 시 스폰 밀집도 — GlobalMonsterCapBonus 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (2026-06-10 기준)
- 참조 spec/plan 수: 60개 (spec 30 + plan 30)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22)
- 과거 감사 이력 (git log): 21건 (가장 최근: 2026-07-09)

---

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 없음 |
| 몬스터 | 6 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom .prefab) | 없음 |
| 패시브 카드 | 16 | 16 (.asset × 16 확인) | 없음 |
| 액티브 카드 | 12 | 12 (.asset × 12 확인) | 없음 |
| 카드 효과 클래스 | 28 | 26 .cs 확인 (WispHpBoostEffect·WraithDamageBoostEffect 별도 위치 가능성) | 2건 미확인 |

### 계획 있으나 미구현

- **SwarmRush** (Multiply 자리 교체) — `card-renewal.md` §3.4 의 "SwarmRush(Phantom 6마리 즉시 소환)" 신설 미구현. `Multiply`("빠른 번식", FastBreedingEffect) 잔존. 별도 구현 사이클 필요.
- **BattleController.DebugAutoPicker 훅** — QA 2026-05-22 §3 권고 미구현 상태. qa-simulator BLOCKED 지속. 헤드리스 시뮬레이션 인프라(LairSimWindow + SimDriver) 미구축.

### QA 권고 미해결

- QA 2026-05-22: `BattleController.DebugAutoPicker` 훅 구현 전까지 qa-simulator 운영 불가. 밸런스 검증이 정성 분석에 의존 중. DebugAutoPicker 구현(~10줄, `#if UNITY_EDITOR`) 이 선행되어야 한다.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
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
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Tank Tier3 필드 캡 +6 발동 시 스폰 밀집도 산출 — TankTier3CapBonus 손잡이 미설계

- **카테고리**: 시너지 / BalanceConfig
- **요지**: Tank Tier3 (7장 누적 시 필드 글로벌 캡 18→24, 영구) 의 +6 값이 BalanceConfig에 없이 하드코딩이다. 캡 확장 후 이미 HP·데미지 감소가 극단적으로 강화된 Wisp·Wraith 6마리가 추가로 필드를 점유할 때 영웅의 실효 생존 시간에 미치는 복합 압박이 미검증 상태다. `TankTier3CapBonus` 손잡이가 없으면 현장 Play-test 에서 즉시 조정이 불가능하다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 5 / 1 / 5 / 4 → 종합 **19**
- **근거**:
  - `docs/design/card-renewal.md` §4.2 Tier 표 — Tank Tier3 "필드 글로벌 캡 +6 (18→24, 영구)" 정의
  - `docs/design/card-renewal.md` §4.5 — 구현 요청 신규 표면: `IBattleContext.IncrementGlobalMonsterCap(int delta)`. delta 값의 BalanceConfig 화는 미요청 상태
  - `docs/design/continuous-spawn-round.md` §5 — "글로벌 하드 캡: 18마리, 캡 초과 시 Spawner 해당 사이클 skip (백오프)"
  - 과거 감사 2026-06-29 (07d6dd7) — SpawnPhantoms 3픽 + Swarm Tier3 복합 출력 5대 산출 시 "캡 18에 막힘" 언급. Tank Tier3 발동 후 캡이 24가 되면 이 cap-limit 가정 자체가 바뀜
  - 과거 감사 2026-06-26 (614c299) — WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 Wisp HP ×4.39(≈877 HP) 산출. HP 877 Wisp가 캡 24 기준으로 6마리 더 필드에 존재할 수 있음을 미산출
  - 과거 감사 2026-07-06 (bddf4f3) — ToughHide·IronWill·GuardianRage 3중 복합 데미지 감소 ×0.2625 산출. 영웅 공격력 50 × 0.2625 = 실효 13.1 데미지/타 → HP 877 Wisp를 처치하는 데 약 67타(67초) 필요. 캡 24 달성 시 24마리 동시 필드에서 영웅의 DPS 처리 능력 완전 포화
- **MVP 범위**: 컨셉 §11.2 Tank 빌드 시너지 + BalanceConfig 손잡이 — 범위 내

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**
Tank 축 카드를 7장 누적 픽한 순간 `BuildSynergyService` 가 Tank Tier3 임계 도달을 감지해 `IBattleContext.IncrementGlobalMonsterCap(+6)` 을 즉시 호출한다. 3픽 캡 정책 상 최소 3종 Tank 카드를 조합해야 하며, 예를 들어 WispHpBoost×3 + WraithDamageBoost×3 + SpawnWraith×1 = 7픽으로 달성 가능하다. 9회 패시브 픽 중 4회 + 3회 액티브 Tank 픽(IronWill·WallOfWisps·Berserk 중 3회)으로도 발동 가능해 집중 플레이 시 현실적으로 도달한다.

**2. 화면 변화**
카드 픽 직후 시너지 패널(BattleHud 좌상단) TANK 행 아이콘이 3개로 완성되고, 화면 중앙 상단에 "Tank 시너지 Tier 3 발동!" 토스트가 1.5초간 표시된다(`card-renewal.md` §8.4). 글로벌 캡 값이 18→24로 즉시 갱신되지만 이를 별도로 표시하는 HUD 요소는 존재하지 않는다. 플레이어는 Spawner 백오프 시점이 늦춰지는 것을 체감으로만 알 수 있다.

**3. 입력 행동**
플레이어(던전 주인)가 별도 액션을 취할 필요 없다. 카드 픽 팝업에서 Tank 카드를 선택하는 순간 자동 발화된다. 이후 플레이어는 필드가 더 빽빽해지는 상황을 수동적으로 지켜본다.

**4. 시스템 반응**
`IncrementGlobalMonsterCap(+6)` 이 BattleController 내부 글로벌 캡 dict 를 갱신한다. 이후 각 Spawner 의 스폰 사이클에서 "현재 필드 몬스터 수 >= 캡" 백오프 조건이 18 대신 24로 비교된다. 기존 18마리 제한에서 멈추던 Spawner 들이 최대 24마리까지 연속 스폰을 이어간다. 발동 시점에 이미 필드에 있던 몬스터들은 소급 영향 없이 유지된다.

**5. 반복·재발생 패턴**
Tank Tier3는 한 런에서 1회 발화 후 영구 유지된다(`card-renewal.md` §4.1 "같은 임계는 라운드당 1회만"). 이후 Tank 축 추가 픽이 들어와도 재발화 없음. 다음 런(새 전투)에서는 초기화되어 7장 조건을 다시 충족해야 한다. 중간 해제 메커니즘 없음.

**6. 종료·해소 조건**
런 종료(영웅 HP 0 or 5분 타임오버) 시까지 영구 유지. 발동 즉시 런 전체 남은 시간에 걸쳐 스폰 밀집도 증가 효과가 누적된다. 영웅이 Tier3 발동 이후 얼마나 빨리 사망하는지가 핵심 밸런스 지표이나, 현재 qa-simulator BLOCKED로 정량 측정 불가.

**7. 다른 시스템과 상호작용**
- **스폰 출력 카드 (SpawnWraith·SpawnPhantoms·SpawnWisps·SpawnReapers)**: 캡 24 달성 시 스폰 출력 +1 카드들이 더 오랫동안 실효 스폰을 만들어 출력 카드의 사후적 가치가 상승한다.
- **Swarm Tier3 (스포너 동시 출력 +1 영구)**: Tank Tier3와 Swarm Tier3가 동시 활성이면 스포너 6개 × 동시 출력 +1 = 초당 최대 12마리 스폰 압박이 캡 24까지 허용된다. 이 복합 시나리오는 미검증.
- **WispHpBoost·WraithDamageBoost 복합 HP (감사 614c299 — ×4.39 배율)**: Wisp HP 200 × 4.39 = 877 HP. 캡 +6 발동 시 877HP Wisp가 추가로 최대 6마리 더 필드에 진입 가능 → 영웅(공격력 50/타, 1s 쿨다운) 기준 Wisp 1마리당 18타(18초) 필요. 24마리 동시 필드 도달 시 영웅의 집중 DPS가 Spawner 생산 속도를 따라가지 못할 수 있다.
- **Tank 액티브 3중 복합 (감사 bddf4f3 — 데미지 감소 ×0.2625)**: 캡 24 + 데미지 감소 ×0.2625 복합 시 영웅 실효 데미지 50 × 0.2625 = 13.1/타. Wisp HP 877 / 13.1 = 67타(67초)로 Wisp 1마리 처치. 24마리가 동시에 필드를 채우면 영웅은 사실상 몬스터를 처치하지 못하는 상태가 된다.
- **Swarm Tier2 (스포너 주기 ×0.85 영구)**: 스폰 속도가 가속된 상태에서 캡 24에 도달하는 시간이 단축되어 Tank Tier3 효과가 더 빨리 포화 상태에 진입한다.

**8. 엣지 케이스**
- Tank Tier3 발동 시점에 다른 버그나 미검증 메커니즘으로 필드 몬스터 수가 이미 18을 초과한 경우, `IncrementGlobalMonsterCap` 의 새 캡이 즉시 적용되는지 혹은 현재 필드 수가 새 캡보다 많은 상태를 허용하는지 경계 동작 미검증.
- `IncrementGlobalMonsterCap` 이 Tier3 재발화 방지 로직과 연동되어 단 1회만 호출되는지 보장 여부 미확인. 다중 호출 시 캡이 24를 초과하는 상황 발생 가능.
- Tank Tier3 + Swarm Tier3 동시 달성 시나리오 — 7+7 = 14장을 두 축에 나누기 위해 18픽 중 14픽이 Tank 또는 Swarm에 집중해야 해 현실적 달성 가능성과 실제 캡 + 출력 복합 효과 미산출.
- `TankTier3CapBonus` 가 BalanceConfig에 없어 Unity Inspector 에서 런타임 조정이 불가능. Play-test에서 +6이 너무 과한지 즉시 확인할 방법이 없음.

**9. 유저 정보·피드백**
Tank Tier3 발동 토스트("Tank 시너지 Tier 3 발동!") 외에 "캡이 24로 늘었음"을 알려주는 별도 HUD 요소가 없다. 숙련 플레이어는 Spawner 백오프 지점이 사라진 것을 체감할 수 있지만, 초급 플레이어는 Tank Tier3의 실제 효과를 인게임에서 확인하기 어렵다. 카드 픽 팝업의 시너지 Tier3 효과 설명("필드 최대 몬스터 +6")이 명시적으로 표기되어야 하며, 현재 표시 여부 미확인.

---

### 보류

- **BloodThirst 3픽(90s) 처치 피드백 루프 — BloodThirstHealAmount 손잡이 미설계**: 검증가치 5 / 구현비용 1 / 시너지폭 4 / 데이터근거 3 → 종합 17. Tank Tier3 대비 낮아 보류. 처치 시 주변 몬스터 HP +30 회복의 Dps 빌드 피드백 루프(빠른 처치 → 잦은 치유 → 생존 몬스터 HP 누적)는 다음 감사 사이클에서 다룰 가치 있음.
- **MarkOfDeath 3픽(15s) + Frenzy 복합 피크 버스트 — MaxHeroReceivedDmgMul 손잡이 미설계**: 검증가치 5 / 구현비용 1 / 시너지폭 4 / 데이터근거 4 → 종합 18. 2026-06-18 감사(dcaa8b7)에서 "MarkOfDeath 복합 압박" 이미 언급됨. 3픽 지속시간 누적(5s→15s)은 별도 각도지만 직전 7일 이내(2026-07-03)에 Dps 복합 배율이 다뤄졌고 카테고리 근접. 다음 Dps 감사 사이클로 이월.

---

## 3. 과거 감사 대비 차별성

git log 조회 21건 검토 완료.

가장 유사했던 과거 커밋:
- **614c299 (2026-06-26)** "Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계" — 차별점: 해당 감사는 Tank Tier1(3장) 수준에서 몬스터 HP 배율 상한을 다뤘고, 글로벌 캡 변화는 다루지 않았다. 본 감사는 Tank Tier3(7장)라는 훨씬 까다로운 조건에서 글로벌 캡 구조 자체가 변경되는 효과에 집중하며, 기존 HP 산출치(×4.39)를 캡 24 시나리오의 재료로 활용한다.
- **07d6dd7 (2026-06-29)** "SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계" — 스폰 출력 적층 시 "캡 18에 막힌다"는 내용을 언급했으나 캡 자체를 높이는 Tank Tier3와는 반대 방향(출력 vs 한계치). 본 감사는 캡 한계를 올리는 Tank Tier3 발동 후 그 한계 변화가 기존 스폰 압박 시나리오에 미치는 영향을 분석한다.

---

## 4. 제외 (범위 밖)

- SwarmRush 신규 구현 → 별도 구현 사이클 필요 (Multiply 잔존 처리 포함)
- 영웅 AutoCombat AI 의 다타깃 스위칭 개선 → Tank Tier3 대응 AI 조정은 새 기능으로 별도 기획 필요
- 글로벌 캡 HUD 표시 (현재 캡 수치 UI) → 신규 UI 컴포넌트 추가, 본 감사는 손잡이 추가만 제안

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청 — Tank Tier3 복합 시나리오(캡 24 + HP ×4.39 + 데미지 감소 ×0.2625) 의 영웅 생존 시간 산출 포함
- gameplay-programmer 에게 요청: `BalanceConfig` 에 `public int TankTier3CapBonus = 6;` 필드 추가 + `BuildSynergyService`(또는 해당 Tier 클래스)가 하드코딩 `+6` 대신 BalanceConfig 값을 참조하도록 수정 (~3줄)
- DebugAutoPicker 훅 구현 후 qa-simulator 에게 Tank 7장 집중 빌드 시나리오 자동 캠페인 N판 의뢰 가능

---

## 6. 쉬운 설명 (비개발자 요약)

던전에는 한 번에 최대 18마리의 몬스터만 동시에 있을 수 있습니다. 그런데 탱커 카드를 7장 모두 골라야만 발동되는 특별 보너스가 있는데, 이 보너스가 그 한도를 24마리로 올려줍니다. 문제는 그 6마리 추가 한도가 정확히 얼마나 강한지 아직 아무도 측정한 적이 없다는 것입니다. 탱커 몬스터들은 이미 HP와 방어력이 최대로 강화된 상태이므로, 거기서 6마리가 더 쏟아지면 영웅이 1분 16초(평균 사망 시간) 이내에 죽어야 하는 게임의 균형이 훨씬 빨리 무너질 수 있습니다. 그래서 이번에 제안하는 것은: "+6"이라는 숫자를 개발팀이 설정 파일에서 바로 바꿀 수 있게 만들어두고, 실제 플레이에서 얼마나 위험한지 직접 눈으로 확인하자는 것입니다.
