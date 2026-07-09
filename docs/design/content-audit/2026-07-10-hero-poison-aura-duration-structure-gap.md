# Content Audit — 2026-07-10 — Debuff 패시브 HeroPoisonAura 5s 독장판 — HP% 트리거 간격 불일치 + BalanceConfig 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10)
- 참조 spec/plan 수: 30개 specs / 30개 plans
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED — DebugAutoPicker 훅 미구현)
- 과거 감사 이력 (git log): 21건 (가장 최근: 2026-07-08)

## 1. 현황

| 카테고리 | 컨셉 §11.3 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 (기사) | 1 (Knight.prefab) | ±0 |
| 몬스터 | 6종 | 6종 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | ±0 |
| 패시브 카드 | 16장 | 16 SO + 16 Effect.cs | ±0 |
| 액티브 카드 | 12장 | 12 SO + 12 Effect.cs (WallOfWisps.asset = ToughHideEffect) | ±0 |
| BalanceConfig 손잡이 | 카드 효과값 전체 | CharacterStat·SpawnPeriod·RunDuration·Thresholds 만 | 카드 효과 전무 |

### 계획 있으나 미구현
- `DebugAutoPicker` 훅 (qa-simulator 2026-05-22 리포트 §3 요청) — 본격 시뮬레이션 캠페인 미착수
- WallOfWisps.asset : ToughHideEffect 교체 완료, 컨셉서 §11.3 설명("4방위 Wisp 4마리 즉시 소환")이 구현("받는 데미지 ×0.75 영구")과 불일치 — 컨셉서 §11.3 미갱신

### QA 권고 미해결
- qa-simulator 2026-05-22: `BattleController.DebugAutoPicker` 구현 요청 → 미구현 유지
- 전략별 승률·빌드 다양성·평균 사망시각 데이터 없음 (실제 시뮬 결과 0건)

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | SHA | subject 설명 |
|---|---|---|
| 2026-07-08 | 63ab1a5 | Swarm 액티브 TimeStop 영웅 스킬 우회 — HeroSkillRunner IAttacker.Enabled 미체크 + TimeStopDuration 손잡이 미설계 |
| 2026-07-07 | 1be6efc | Debuff 패시브 HeroAttackDown 3픽+Tier2(×0.85) 복합 영구 공격력 ×0.358 — MinHeroAttackScale(영구) 손잡이 미설계 |
| 2026-07-06 | bddf4f3 | Tank 액티브 ToughHide·IronWill·GuardianRage 3중 데미지 감소 복합 ×0.2625 — MinMonsterDamageTakenScale 손잡이 미설계 |
| 2026-07-05 | 78c61f3 | Debuff 액티브 Weaken _factor·_duration 하드코딩 — WeakenFactor·MinHeroAttackScaleFloor BalanceConfig 손잡이 미설계 |
| 2026-07-04 | 9b3303b | Debuff 액티브 Weaken 영웅 스킬 도입 후 실효성 급감 — WeakenFactor BalanceConfig 손잡이 미설계 |
| 2026-07-03 | 647bc82 | Dps 패시브 HexRangeBoost 3픽+Tier3 복합 Hex 사거리 ×3.567 — 영웅 AI 회피 + MaxHexRangeMul 손잡이 미설계 |
| 2026-07-02 | ea6803e | Debuff 액티브 Bleed '이동/정지 무관' 구현-설계 불일치 — 트리거 조건 결정 요청 |
| 2026-07-01 | 148ae90 | Dps ReplaceReapersToHex 3픽(×2.197)+Tier1(×1.3) 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계 |
| 2026-06-30 | db9b2d7 | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계 |
| 2026-06-29 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — SynergyTierThreshold 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale 손잡이 미설계 |

## 2. 추가 컨텐츠 후보 (권장 1개)

### Debuff 패시브 HeroPoisonAura — 5s 독장판 지속 시간 vs HP% 트리거 간격 구조적 불일치 + BalanceConfig 손잡이 미설계

- **카테고리**: 패시브 / Debuff 축 (HeroPoisonAura P3)
- **요지**: HeroPoisonAura는 5초짜리 독장판을 영웅 발 밑에 생성한다. 재선택 시 DPS는 5로 고정되고 지속 시간만 +5초 연장된다. 영웅 HP가 천천히 감소하는 시나리오(트리거 간격 30초+)에서는 독장판이 매번 소멸한 뒤 다음 트리거가 발생하여 누적 효과가 없다. 반대로 HP가 빠르게 감소할 때(트리거 간격 ≤5초)만 연장 효과가 작동한다. `_dps·_duration·_radius` 는 SO에 [SerializeField]로 박혀 있어 SO별로 다를 수 있으나 BalanceConfig와 연결되지 않아 글로벌 튜닝이 불가하다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 4 / 2 / 4 / 4 → 종합 **16**
- **근거**: `Assets/_Lair/Scripts/Card/Effects/HeroPoisonAuraEffect.cs` — `_dps=5f, _duration=5f, _radius=1.25f` (SerializeField, BalanceConfig 참조 없음); `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs` — "기존 Remain 에 _duration 만큼 연장" (DPS 누적 아님); 컨셉서 §11.3 Debuff 축 P3
- **MVP 범위**: 컨셉서 §11.3 Debuff 패시브 카드 효과값 재조정 / BalanceConfig 손잡이 추가

#### 유저 플로우

1. **노출 시점·트리거**
   패시브 이벤트는 영웅 HP가 10% 단위로 하락할 때마다 게임을 일시정지하고 3장 카드 선택창을 보여준다. HeroPoisonAura는 이 Debuff 축 패시브 풀에 포함되어 있으며, 선택창에 포함될 확률로 등장한다. 첫 노출은 빠르면 HP 90%(런 초반 30~60초)에 발생할 수 있다.

2. **화면 변화**
   HeroPoisonAura를 선택하면 영웅 발 밑에 연두색 독장판 FX(`EVisual.PoisonAura`)가 즉시 생성된다. 영웅 HP 바 아래 독 상태 아이콘이 점등되어 독장판 활성 여부를 알린다. 카드 선택 팝업이 닫히고 게임이 재개된다.

3. **입력 행동**
   던전 주인이 카드 선택 팝업에서 HeroPoisonAura 카드를 클릭해 선택하는 1회 입력. 카드 선택 후에는 추가 입력 없이 효과가 자동 작동한다.

4. **시스템 반응**
   `HeroPoisonAuraEffect.Apply` 가 `ctx.ApplyHeroAura(new PoisonAura(_dps=5f, _radius=1.25f), durationSeconds=5f)` 를 호출한다. `HeroAuraRunner` 가 독장판을 영웅 위치에 부착하고 영웅 이동을 추적한다. 1초마다 영웅에게 5 데미지를 입히고 연두색 데미지 숫자를 표시한다. 독장판이 이미 활성 중이라면 Remain에 5초를 더해 연장한다(DPS 변화 없음). 비활성 상태에서 재선택하면 새로 생성한다.

5. **반복·재발생 패턴**
   HP 80%, 70%, 60% 등에서 HeroPoisonAura가 다시 선택지에 나타날 수 있다. 독장판이 이미 살아있을 때 재선택하면 지속 시간이 +5초 연장된다. 영웅 HP가 빠르게 감소해 트리거가 5초 이내에 연속 발생하면 독장판이 누적 연장(최대 5×선택횟수 초)될 수 있다. 반대로 트리거 간격이 5초를 초과하면 독장판이 소멸한 뒤 다음 트리거가 발생해 매번 독립적인 5초짜리 장판이 새로 시작된다.

6. **종료·해소 조건**
   독장판 Remain이 0이 되면 FX가 소멸하고 데미지가 중단된다. 영웅 사망(HP=0) 또는 5분 런 종료 시에도 즉시 해소된다. 플레이어가 이후 능동적으로 해소하는 수단은 없다(영구 지속형 아니므로 자연 소멸).

7. **다른 시스템과 상호작용**
   Bleed 액티브 카드(영웅 이동 시 1s당 HP -2%, 10s)와 동시 활성 시 HP 감소 속도가 빨라져 다음 HP% 트리거까지 시간이 단축된다. 이는 HeroPoisonAura 재선택 기회를 늘리는 양의 피드백을 만든다. Fear 액티브(3s 도주) 발동 시 독장판도 영웅을 따라 이동하므로 도주 중에도 독 데미지가 지속된다. Debuff Tier3 EternalBleed(이동 시 HP -1%/s, 영구)와 함께 쌓이면 이동 중 복합 DoT가 된다. TimeStop(5s 정지) 중에는 영웅이 멈춰 독장판도 멈추지만 Remain은 계속 소모된다.

8. **엣지 케이스**
   영웅 HP가 1초 이내에 10% 이상 감소(예: 100+ DPS)하면 여러 HP% 임계가 동시에 큐에 쌓여 팝업이 연속 등장할 수 있다. 이 경우 독장판 Remain이 빠르게 누적될 수 있다. HeroPoisonAura를 9회 모두 선택해도 DPS는 5 고정이며 지속 시간만 최대 45초 연장되는 상황이 이론상 가능하다(전략적으로 낭비). 독장판이 비활성 상태에서 새로 생성될 때 FX 풀 부족이 발생하면 시각 이펙트가 표시되지 않을 수 있다.

9. **유저 정보·피드백**
   독 상태 아이콘(HP 바 아래)이 독장판 활성 여부를 시각화한다. 현재 Remain 값의 구체적 표시 수단은 설계에 명시되지 않아 플레이어가 몇 초 남았는지 직관적으로 알기 어렵다. 1초마다 연두색 데미지 숫자 5가 팝업되어 독 피해 발생을 확인할 수 있다. 독장판 반경(1.25f = 지름 약 2.5 유닛)은 FX 비주얼 크기로 대략 인지 가능하다.

### 보류
- BloodThirst (Dps A): 처치 시 HP+30 회복 하드코딩 — 동일 카테고리(BalanceConfig 손잡이 미설계)이나 HeroPoisonAura 대비 종합점수 14점 (2순위)
- SpawnWraith·SpawnReapers 패시브: 스포너 출력 +1 × 3픽 — 2026-06-29(SpawnPhantoms) 및 2026-06-17(SpawnWisps) 과 카테고리·요지·근거 유사, 중복 회피

## 3. 과거 감사 대비 차별성

git log 조회 21건 검토 완료.

가장 유사한 과거 커밋 후보 2건:
- `db9b2d7` (2026-06-30): "Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계"  
  차별점: Bleed는 이동 시 %기반 HP 감소율의 중첩 상한, HeroPoisonAura는 고정 DPS 독장판의 지속 시간 구조 문제. 카드 자체, 피해 단위(% vs 절대값), 핵심 이슈(중첩 상한 vs 트리거 간격 불일치) 모두 상이.
- `a1e0ba4` (2026-06-20): "PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계"  
  차별점: PlagueSlowBoost는 Plague 몬스터 슬로우 배율 보정, HeroPoisonAura는 독장판 DPS·지속 구조. Debuff 축이라는 공통점 외 카드·메커니즘·이슈 완전 상이.

HeroPoisonAura는 21건 모두에서 독립적으로 다룬 카드가 없으며, "HP% 트리거 간격 vs 5s 지속 시간 구조적 불일치"는 이전 어느 감사에서도 제기되지 않은 신규 관점이다.

## 4. 제외 (범위 밖)

- HeroPoisonAura가 몬스터에게도 피해를 주도록 효과 변경 — 컨셉 §11.3 Debuff 축 설계(영웅 디버프) 밖, 기획 승격 없이 착수 불가
- 영웅 피해 시각(독 스플래터 파티클 추가) — 아트/이펙트 추가는 v0.3 허용이나 본 감사 권장 범위(밸런스 손잡이) 밖
- HeroPoisonAura 지속 시간을 독장판 재생성 방식에서 영구 상시 DoT로 변경 — 카드 의미 변경으로 기획 승격 필요
- 9회 선택 시 최종 DPS 확대(선형 스케일 적용) — 28장 매수 고정 원칙 내에서 효과 누적 정책 변경 = 기획 결정, 감사 범위 초과

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청
- game-designer 가 결정해야 할 항목:
  1. `HeroPoisonAuraDps` (기본 5f) · `HeroPoisonAuraDuration` (기본 5f) · `HeroPoisonAuraRadius` (기본 1.25f) → BalanceConfig 신규 필드로 이전
  2. 재선택 시 "기존 Remain + duration 연장" 정책 유지 여부 확인 및 기획서 명문화
  3. 독장판 Remain 표시 UI (플레이어가 몇 초 남았는지 알 수 있는 방법 설계)
  4. 3픽 이상 누적 시 의미 있는 전략이 되도록 기본값 재조정 제안

## 6. 쉬운 설명 (비개발자 요약)

영웅 발 밑에 독 바닥을 깔아 조금씩 데미지를 주는 카드인데, 이 독 바닥은 딱 5초만 지속됩니다. 영웅의 체력이 천천히 줄어드는 판이면 독 바닥이 소멸한 뒤 한참 지나서야 다음 선택 기회가 오기 때문에 사실상 매번 "5초짜리 독" 하나를 따로따로 켰다 끄는 셈이어서 누적 효과가 거의 없습니다. 반면 적들이 강해서 체력이 빠르게 줄면 독 바닥이 자동으로 연장되긴 하지만, 어차피 영웅이 금방 죽을 판이라 체감이 약합니다. 그래서 이번에 제안하는 것은: 독 바닥의 강도(초당 5 데미지)와 지속 시간(5초)을 게임 균형 설정 파일 하나에 모아서 쉽게 조절할 수 있도록 구조를 바꾸는 것입니다.
