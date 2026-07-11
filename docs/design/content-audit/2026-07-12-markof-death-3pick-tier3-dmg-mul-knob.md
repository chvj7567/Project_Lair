# Content Audit — 2026-07-12 — Dps 액티브 MarkOfDeath 3픽 × Dps Tier3(Range×1.3) 복합 — MaxMarkOfDeathDmgTakenMul 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (2026-06-10 기준)
- 참조 spec/plan 수: 60개 (spec 30 + plan 30)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22)
- 과거 감사 이력 (git log): 22건 (가장 최근: 2026-07-11)

---

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 없음 |
| 몬스터 | 6 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom .prefab) | 없음 |
| 패시브 카드 | 16 | 16 (.asset × 16 확인) | 없음 |
| 액티브 카드 | 12 | 12 (.asset × 12 확인) | 없음 |
| 카드 효과 클래스 | 28 | 28 .cs 확인 (MarkOfDeathEffect.cs 포함) | 없음 |

### 계획 있으나 미구현

- **SwarmRush** (Multiply 자리 교체) — `card-renewal.md` §3.4 의 "SwarmRush(Phantom 6마리 즉시 소환)" 신설 미구현. `Multiply`("빠른 번식", FastBreedingEffect) 잔존. 별도 구현 사이클 필요.
- **BattleController.DebugAutoPicker 훅** — QA 2026-05-22 §3 권고 미구현 상태. qa-simulator BLOCKED 지속. 헤드리스 시뮬레이션 인프라(LairSimWindow + SimDriver) 미구축.

### QA 권고 미해결

- QA 2026-05-22: `BattleController.DebugAutoPicker` 훅 구현 전까지 qa-simulator 운영 불가. 밸런스 검증이 정성 분석에 의존 중. DebugAutoPicker 구현(~10줄, `#if UNITY_EDITOR`) 이 선행되어야 한다.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-07-11 | 18dea17 | Tank Tier3 필드 캡 +6 발동 시 스폰 밀집도 — TankTier3CapBonus 손잡이 미설계 |
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

### Dps 액티브 MarkOfDeath 3픽 × Dps Tier3(Range×1.3) 복합 — MaxMarkOfDeathDmgTakenMul 손잡이 미설계

- **카테고리**: Dps 축 / 액티브 카드 / 시너지
- **요지**: MarkOfDeath (`_dmgTakenMul=1.5`, `_duration=5`) 를 3픽(런당 최대)하면 30초 간격으로 3개의 독립 5초 압박 창이 생긴다. Dps Tier3 (7장 누적, 즉시 발화)가 Reaper·Hex 사거리를 ×1.3 영구 확장하면 Hex(기본 Range 5u)가 6.5u에서 공격한다. 영웅 flee 트리거 반경(`_fleeThreatRadius=4u`)보다 넓은 사거리 덕에 Hex는 영웅 도주 반응 없이 일방적으로 공격 가능하다. 두 효과가 겹친 5초 창에서 영웅은 Dps Tier1(Power ×1.3) + MarkOfDeath(×1.5) 복합으로 기본 Dps의 ×1.95배 데미지를 받는다. `_dmgTakenMul=1.5` 수치는 MarkOfDeath.asset SO 하드코딩이며 BalanceConfig에 `MaxMarkOfDeathDmgTakenMul` 상한 손잡이가 없어 Play-test 중 즉시 수치 조정이 불가능하다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 4 / 2 / 4 / 3 → 종합 **15**
  - 구현비용 2: BalanceConfig에 필드 1개 추가 + MarkOfDeathEffect 참조 변경(~5줄). SO 편집 범위는 넓지 않음.
  - 데이터근거 3: QA 시뮬레이션 BLOCKED로 실측 없음. 수치는 코드·기획서 분석으로 산출.
- **근거**:
  - `docs/design/card-renewal.md` §3.2 #7 — `ECardId: MarkOfDeath`, `_dmgTakenMul=1.5`, `_duration=5`, Effect 클래스 `MarkOfDeathEffect → MarkOfDeathAura`, 중첩 정책 "지속시간 누적 (잔여+5s)"
  - `docs/design/card-renewal.md` §4.2 — Dps Tier3: "Reaper·Hex Range ×1.3 (글로벌 영구)"; Dps Tier1: "Reaper·Hex Power ×1.3"; Dps Tier2: "Reaper·Hex Cooldown ×0.8"
  - `docs/design/flee-stabilize-center-pull.md` §3 — `_fleeThreatRadius=4`, 영웅 도주 방향 = 위협 중심에서 5u 후퇴. Hex Range 6.5u > `_fleeThreatRadius=4u` → 도주 비트리거 원거리 공격 가능
  - `docs/design/spawn-period-balance.md` — Hex 스폰 주기 15s, Dps 9/s
  - `Assets/_Lair/Art/Cards/Items/MarkOfDeath.asset` 존재 확인 — SO 필드로 `_dmgTakenMul`, `_duration` 보관
  - `Assets/_Lair/Scripts/Card/Effects/MarkOfDeathEffect.cs` 존재 확인
  - 과거 감사 dcaa8b7 (2026-06-18) — Frenzy+MarkOfDeath 복합에서 "MarkOfDeath 복합 압박" 언급. 본 감사는 Frenzy 없이 Tier3 Range 확장만으로 공간 압박 분리 분석 (§3 차별성 참조)
  - 과거 감사 647bc82 (2026-07-03) — HexRangeBoost 패시브 3픽+Tier3로 Hex 사거리 ×3.567 산출. 본 감사는 패시브 누적 아닌 Tier3 단독 ×1.3에서 MarkOfDeath ×1.5가 얼마나 위험한지 분리 분석
- **MVP 범위**: 컨셉 §11.2 Dps 빌드 시너지 + §11.6 BalanceConfig 손잡이 — 범위 내

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**
MarkOfDeath는 Dps 액티브 카드(A3)로 30초 간격 액티브 픽 선택지에 포함될 수 있다. 3픽 캡 정책(2026-06-01)에 따라 런당 최대 3회 선택 가능하며 이후 선택지에서 제외된다. Dps Tier3는 Dps축 카드 7장 누적 시 `BuildSynergyService`가 즉시 발화해 Reaper·Hex 사거리 ×1.3을 영구 적용한다. MarkOfDeath 자체가 Dps 액티브 카드이므로 MarkOfDeath×3 + Dps 패시브 4장 이상으로 Tier3 조건(7장)을 충족할 수 있다.

**2. 화면 변화**
MarkOfDeath 픽 시 카드 픽 팝업이 닫히고 영웅 머리 위에 "죽음의 표식" 상태 아이콘이 5초간 표시된다(StatusEffectView). Dps Tier3 발동 시 "Dps 시너지 Tier 3 발동!" 토스트가 1.5초간 화면 중앙 상단에 출력된다(`card-renewal.md` §8.4). Hex 사거리 6.5u 확장은 Hex 프리팹의 공격 범위 변경이지만 범위 표시 UI가 없어 플레이어는 Hex가 더 멀리서 공격하는 것을 체감으로만 확인한다.

**3. 입력 행동**
플레이어는 30초마다 나타나는 카드 픽 팝업에서 MarkOfDeath를 선택한다. Dps Tier3는 7장 임계 달성 시 자동 발화로 별도 입력이 불필요하다. MarkOfDeath×3 + Dps 패시브 4장을 확보하려면 9회 패시브 픽 기회 중 4회를 Dps 축으로 소비하고, 9회 액티브 픽 기회 중 3회를 MarkOfDeath에 집중해야 해 Dps 단일 집중 빌드 경로를 요구한다.

**4. 시스템 반응**
MarkOfDeath 픽 시 `MarkOfDeathEffect.Apply(ctx)`가 호출되어 영웅에게 `MarkOfDeathAura` 상태 이상을 부여한다. `_dmgTakenMul=1.5` 로 영웅이 받는 모든 데미지가 5초간 ×1.5 배율 적용된다. 동일 런에서 2번째·3번째 픽 발동 시 "잔여+5s" 정책이 적용되지만 30초 활성 간격이 5초 지속 시간을 초과하므로 실제 복수 픽 간에는 Aura가 겹치지 않고 독립 창으로 동작한다. Dps Tier3 발동 후 Hex는 Range 5u → 6.5u에서 공격을 시작하며, 영웅의 flee 반경(4u) 밖에서도 일방적으로 데미지를 가할 수 있다.

**5. 반복·재발생 패턴**
MarkOfDeath는 30초마다 1회 선택 기회가 주어지고 3픽 캡에 따라 런당 최대 3회 발화 — 각각 독립된 5초 압박 창을 만든다(창 3개, 창 간 간격 약 30초, 총 누적 15초 압박이지만 연속이 아닌 분산 발생). Dps Tier3 사거리 확장은 1회 발화 후 런 종료까지 영구 유지된다. 3픽 소진 이후 MarkOfDeath는 선택지에서 제외되어 추가 창 생성 불가.

**6. 종료·해소 조건**
MarkOfDeathAura 개별 발동은 5초 후 자연 소멸하며 영웅 데미지 배율이 정상 복귀한다. 발동 중 영웅 사망 또는 런 종료 시 Aura는 소멸한다. Dps Tier3 사거리 확장은 런 종료까지 영구 유지 — 중간 해제 조건 없음. MarkOfDeath 3번째 픽 이후 활성 기회가 소진되며, 이후 런 잔여 시간은 Tier3 Range 확장 효과만 남는다.

**7. 다른 시스템과 상호작용**
- **Dps Tier1 (Reaper·Hex Power ×1.3)**: Tier3 달성 시점엔 Tier1(3장 임계)도 이미 발화 상태. MarkOfDeath 5초 창에서 영웅 수신 데미지 = 기본 Dps × 1.3(Tier1) × 1.5(MarkOfDeath) = **기본의 ×1.95**. Hex 단독 DPS 기준 9 × 1.3 = 11.7 DPS → 창 내 17.6 DPS (영웅에게).
- **Dps Tier2 (Reaper·Hex Cooldown ×0.8)**: Tier3 달성 시 Tier2도 발화. 쿨다운 단축 = 공격 빈도 증가로 MarkOfDeath 5초 창에서 Hex 공격 횟수가 추가 증가.
- **Flee 행동 (`_fleeThreatRadius=4u`)**: Hex Range 6.5u > flee 반경 4u → Hex가 flee 트리거를 발생시키지 않고 원거리에서 지속 공격 가능. 영웅이 도망쳐도 Hex 사거리 안에 머물러 데미지가 끊기지 않는다.
- **HeroSkillRunner (HP 게이트 스킬)**: MarkOfDeath Aura가 활성인 5초 동안 영웅 스킬(DashStrike 85% / AoeNova 65% / OrbitingBlade 45%)이 발화할 수 있다. 스킬이 몬스터를 밀어내거나 처치하면 창 내 실효 DPS가 감소할 수 있으나, 영웅 HP가 스킬 게이트 아래일 때 MarkOfDeath가 발동된 경우 오히려 스킬-MarkOfDeath 동시 압박이 겹친다.
- **Frenzy (액티브, 공속 버프)**: 2026-06-18 감사(dcaa8b7)에서 Frenzy+MarkOfDeath 복합이 분석됨. 본 감사는 Frenzy 없이 Tier3 Range 단독 공간 압박을 분리 측정해 Frenzy 기여분과 Tier3 기여분을 구별하는 데이터 기반을 제공한다.
- **Hex 스폰 밀도**: 스폰 주기 15s, 5분 런에서 최대 20회 스폰 시도. 글로벌 캡 18(또는 Tank Tier3 발동 시 24)에 막히기 전까지 다수의 Hex가 동시에 Range 6.5u 에서 공격 → MarkOfDeath 창에서 Hex 복수 개체 공격이 중첩된다.

**8. 엣지 케이스**
- "잔여+5s" 중첩 정책 — 30초 간격 픽 시 Aura가 이미 소멸한 상태에서 재발동하므로 실제로는 중첩이 발생하지 않는다. 그러나 영웅 HP 단계와 액티브 트리거 타이밍이 충돌해 Aura 소멸 전(5초 이내)에 다음 픽이 발생할 수 있는 엣지가 있는지 검증 미완료.
- MarkOfDeath 발동 직후 영웅 HP가 0에 도달할 경우 `MarkOfDeathAura` 정리 순서(OnDeath vs Aura ticker 경쟁 상태) 확인 필요.
- Dps Tier3 발동 직후 이미 공격 중인 Hex 개체의 사거리가 즉시 갱신되는지, 아니면 다음 공격 사이클에 적용되는지 타이밍 동기화 미검증.
- `_dmgTakenMul=1.5` 값이 MarkOfDeath.asset SO 필드에 하드코딩되어 있어 BalanceConfig JSON 내보내기(`BalanceConfigSyncer`) 대상이 아니다. Play-test 현장에서 수치를 1.2 수준으로 낮추려면 Unity Editor 를 열어 SO 를 직접 편집해야 하며, 팀 공유·버전 추적이 어렵다.
- `MaxMarkOfDeathDmgTakenMul` 상한 손잡이가 없으면 `_dmgTakenMul` 을 실수로 5.0 이상으로 설정해도 런타임에서 차단할 수단이 없다.

**9. 유저 정보·피드백**
영웅 머리 위 "죽음의 표식" 상태 아이콘 외에 남은 5초를 표시하는 타이머 UI 나 "이 창에서 데미지 +50%"를 알려주는 텍스트가 없다. Dps Tier3 발동 토스트는 있으나 Hex 사거리가 1.3배로 확장됐음을 알려주는 별도 피드백이 없다. 카드 픽 팝업에는 "영웅이 받는 데미지 +50%"가 설명되어 있지만 Tier3 Range 확장과의 복합 효과(수신 데미지 ×1.95, flee 무력화)는 인게임 어디서도 안내되지 않는다. 숙련 플레이어는 Hex가 더 먼 거리에서 공격하는 것을 체감할 수 있으나 수치 근거를 확인할 방법이 없다.

---

### 보류

- **Dps 액티브 Frenzy 3픽 × MarkOfDeath 3픽 복합 공속+데미지배율 — MaxFrenzyAtkSpeedMul+MaxMarkOfDeathDmgTakenMul 손잡이**: 검증가치 4 / 구현비용 2 / 시너지폭 4 / 데이터근거 3 → 종합 14. 2026-07-11 보류에서 "다음 Dps 감사 사이클로 이월" 처리된 후보. 2026-06-18 감사(dcaa8b7)에서 이미 Frenzy+MarkOfDeath 복합 압박을 다뤘으며, 카테고리(Dps 액티브)·요지(MarkOfDeath ×1.5 + 공속 버스트)가 해당 감사와 2축 이상 겹친다. 또한 3픽 캡 하에 Frenzy×3 + MarkOfDeath×3 = 6픽 액티브 소비는 12회 액티브 기회의 절반을 Dps 액티브 단 2종에 집중해야 해 현실적 빌드 경로가 과도하게 좁다. 다음 Dps 감사 사이클로 재이월.
- **Dps 패시브 ReplaceReapersToHex 3픽(Hex수 ×2.197) × Dps Tier3(Range×1.3) — 과밀 Hex 원거리 사거리 미검증**: 검증가치 3 / 구현비용 2 / 시너지폭 3 / 데이터근거 2 → 종합 12. 2026-07-01 감사(148ae90)에서 ReplaceReapersToHex 3픽+Tier1 Power ×2.856 복합이 다뤄졌음. Tier3 Range 확장 각도는 본 감사와 같은 Tier3 배율을 공유하므로 이번 감사의 산출 기반(Range 6.5u)을 재활용 가능하나, 독립 후보로는 점수 12로 채택 기준 미달.

---

## 3. 과거 감사 대비 차별성

git log 조회 22건 검토 완료.

가장 유사했던 과거 커밋:
- **dcaa8b7 (2026-06-18)** "Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박" — 차별점: 해당 감사는 Frenzy의 하드코딩 공속 배율 문제와 Frenzy+MarkOfDeath 복합 압박을 주 주제로 다뤘다. 본 감사는 Frenzy를 제외하고 Tier3 사거리 확장(Range ×1.3) 만으로 발생하는 flee 회피 구조(Range 6.5u > `_fleeThreatRadius` 4u)와 `_dmgTakenMul` 상한 손잡이 부재를 독립적으로 분석한다. 공격 벡터가 "공속 폭발"(2026-06-18) 대 "사거리 기반 도주 무력화"(본 감사)로 구별된다.
- **647bc82 (2026-07-03)** "Dps 패시브 HexRangeBoost 3픽+Tier3 복합 Hex 사거리 ×3.567" — 차별점: 해당 감사는 HexRangeBoost 패시브 3픽 누적으로 사거리가 극단적으로 커지는 수치 문제를 다뤘다. 본 감사는 패시브 누적 없이 Tier3 단독 ×1.3(Hex Range 6.5u) 상태에서 MarkOfDeath 액티브의 ×1.5 데미지 배율이 겹칠 때의 복합 압박을 다룬다. 수치 규모는 2026-07-03보다 훨씬 작지만, flee 반경 4u와의 관계에서 생기는 공간 구조 문제는 별도 설계 검토 사안이다.

---

## 4. 제외 (범위 밖)

- MarkOfDeath Aura 타이머 UI 추가 (남은 5초 표시) → 신규 UI 컴포넌트, 본 감사는 손잡이 추가만 제안
- Hex 사거리 범위 표시 원 HUD → 신규 UI 컴포넌트, 별도 기획 필요
- MarkOfDeath 복합 압박의 AI 난이도 조정(영웅 도주 알고리즘 개선) → 게임 로직 변경, 범위 밖
- qa-simulator 연동 실측 캠페인 → DebugAutoPicker 훅 미구현으로 현재 불가

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청 — MarkOfDeath×3 + Dps Tier1/2/3 전 발화 시나리오에서 5초 창 3회의 영웅 생존 시간 영향 및 `_dmgTakenMul` 적정 범위(1.2~1.8) 가이드라인 산출 포함
- gameplay-programmer 에게 요청: `BalanceConfig` 에 `public float MaxMarkOfDeathDmgTakenMul = 2.0f;` 필드 추가 + `MarkOfDeathEffect`(또는 `MarkOfDeathAura`) 가 `_dmgTakenMul` 을 `Mathf.Clamp(so._dmgTakenMul, 1f, BalanceConfig.MaxMarkOfDeathDmgTakenMul)` 로 읽도록 수정 (~5줄)
- DebugAutoPicker 훅 구현 후 qa-simulator 에게 Dps Tier3 + MarkOfDeath×3 집중 빌드 시나리오 N판 자동 캠페인 의뢰 가능

---

## 6. 쉬운 설명 (비개발자 요약)

"죽음의 표식" 카드는 선택하는 순간 다음 5초 동안 영웅이 모든 공격에 1.5배 더 많은 피해를 받게 만듭니다. 이 카드는 한 게임에서 최대 3번까지 고를 수 있어서, 잘 고르면 30초 간격으로 세 번 영웅을 집중 공격할 수 있습니다. 여기에 "Dps(딜러) 시너지 3단계"라는 보너스가 활성화되면 Hex 몬스터의 공격 사거리가 1.3배 늘어나는데, 이게 문제입니다 — 영웅은 위협을 느끼면 도망치려 하지만 늘어난 사거리 때문에 도망쳐도 Hex의 공격 범위를 벗어나지 못합니다. 결국 죽음의 표식 5초 동안은 영웅이 도망도 못 치면서 평소의 거의 두 배 가까운 피해를 계속 받는 상황이 되는데, "1.5배"라는 핵심 수치가 설정 파일이 아닌 카드 데이터 안에 묻혀 있어서 개발팀이 플레이테스트 중 바로 조정하기가 어렵습니다. 그래서 이번에 제안하는 것은: 이 1.5배 배율을 설정 파일(BalanceConfig)로 꺼내 상한선을 걸어두고, 플레이테스트에서 "이 조합이 너무 강하다" 싶을 때 즉시 수치를 낮출 수 있는 손잡이를 만들자는 것입니다.
