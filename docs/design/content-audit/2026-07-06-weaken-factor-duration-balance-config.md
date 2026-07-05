# Content Audit — 2026-07-06 — Weaken 액티브 카드 `_factor=0.5`·`_duration=10` BalanceConfig 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10 기준)
- 참조 spec/plan 수: spec 30개, plan 29개
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED 상태)
- 과거 감사 이력 (git log): 18건 (가장 최근: 2026-06-28) / content-audit 폴더 파일 30개 (git log 범위 이전 이력 포함)

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 0 |
| 몬스터 | 6 | 6 (Wisp / Wraith / Reaper / Hex / Plague / Phantom) | 0 |
| 패시브 | 16 | 16 (.asset 확인) | 0 |
| 액티브 | 12 | 12 (.asset, Berserk→GuardianRage / Multiply 잔존 포함) | 0 |

### 계획 있으나 미구현
- `SwarmRush` (Phantom 6마리 즉시 소환): 원안 `card-renewal.md` §3.4 에서 `Multiply` 대체 예정이었으나 현행 `Multiply.asset`("빠른 번식", `FastBreedingEffect`) 잔존. 별도 구현 사이클 필요.
- `DebugAutoPicker` 훅: QA 리포트 §3 에서 요청한 `BattleController` 시뮬레이션 훅. 미구현으로 qa-simulator BLOCKED.

### QA 권고 미해결
- **2026-05-22 리포트 §3**: `BattleController` 에 `#if UNITY_EDITOR` `DebugAutoPicker` 델리게이트 추가 요청 (~10줄). 해결 전까지 qa-simulator 전면 BLOCKED — 밸런스 데이터 수집 불가.
- **2026-05-22 리포트 §4.1**: MCP 헤드리스에서 프레임 진행 불가 — 사용자의 대화형 Unity 에디터에서만 시뮬 실행 가능. 실행 방식 결정 대기.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-28 | 6d21dc5 | Debuff SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
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
| 2026-06-14 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — BalanceConfig 손잡이 이관 제안 |
| 2026-06-13 | c07cc2c | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |

## 2. 추가 컨텐츠 후보 (권장 1개)

### Weaken 액티브 카드 `_factor=0.5`·`_duration=10` BalanceConfig 손잡이 미설계

- **카테고리**: 액티브 카드 재조정 (효과값/지속시간)
- **요지**: Weaken 카드(`_factor=0.5`, `_duration=10`)는 영웅 공격력을 일시적으로 -50% 감소시키는 Debuff 축 액티브 카드다. 지속시간 누적 정책으로 3픽 시 최대 30초 연속 Weaken이 가능하고, HeroAttackDown 영구 ×0.75(최대 3픽) · Debuff Tier2 자동 ×0.85와 복합 시 영웅 공격력이 base의 약 18%까지 수렴할 수 있다. `WeakenFactor`, `WeakenDuration`, 임시+영구 복합 하한(`MinHeroAttackScaleFloor`) 모두 BalanceConfig 손잡이가 없어 튜닝 불가 상태다.
- **검증/구현/시너지/데이터**: 4/2/4/3 → 종합 **15**
- **근거**: `docs/design/card-renewal.md` §3.3 #7 (`_factor=0.5 _duration=10` 명시, WeakenEffect.cs); 동일 기획서 §7.2 "지속시간 누적" 정책 (Weaken 해당); `docs/design/project_lair_concept.md` §8 밸런싱 기준 (영웅 2~4분 사망); `docs/design/card-renewal.md` §3.3 (HeroAttackDown 영구 누적과 동일 축)
- **MVP 범위**: 컨셉 §11.2 "액티브 카드 12장" / 컨셉 §11.3 Debuff 축 A3 (Fear · Bleed · Weaken)

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**: 전투 30초 주기 액티브 이벤트에서 Debuff 축 카드 풀(Fear / Bleed / Weaken) 중 Weaken이 3장 후보 안에 포함되어 제시된다. 플레이어가 Weaken 카드를 선택하는 순간 즉시 발동. 이미 Weaken이 진행 중인 상태에서 다시 픽하면 잔여 시간에 10초가 추가된다(지속시간 누적 정책).

2. **화면 변화**: 카드 픽 직후 `CardSelectionPopup` 이 닫히고 전투가 재개된다. 영웅 캐릭터 머리 위 상태 아이콘 영역(`hero-status-icons.md` 시스템)에 Weaken 아이콘과 잔여 시간 카운트다운이 표시된다. BattleHud 좌상단 DEBUFF 행의 빌드 카운트가 Weaken 픽만큼 증가한다.

3. **입력 행동**: 3장 카드 중 Weaken을 탭·클릭으로 선택하는 단 1회의 입력이 전부다. 이후 지속 시간 동안 별도 입력 없이 자동으로 효과가 유지된다. 전투는 일시정지에서 즉시 재개되며 영웅의 `PowerScale` 이 `_factor=0.5` 를 반영한 값으로 갱신된다.

4. **시스템 반응**: `WeakenEffect.Apply(ctx)` 호출 → `WeakenAura` 인스턴스 생성 및 영웅에 등록. 영웅 `AttackerComponent.PowerScale` 에 `_factor=0.5` 곱연산 적용. 이 시점에 HeroAttackDown 카드 픽 이력이 있으면 동일 `PowerScale` 에 기존 누적 ×0.75^N 이 반영된 상태이므로, 합산 결과는 (기존 누적값) × 0.5. `_duration=10` 초 경과(또는 지속시간 누적으로 연장된 시간 경과) 후 Aura 해제 → `PowerScale` 에서 × (1/0.5) 역산 복구.

5. **반복·재발생 패턴**: 30초 주기 액티브 이벤트마다 Debuff 축 3장(Fear / Bleed / Weaken) 풀에서 제시 후보가 구성된다. Weaken이 후보에 포함될 때마다 픽 가능하며, 픽할 때마다 잔여 시간 +10초. 한 판 최대 픽 횟수(전역 3픽 캡) 도달 전까지 반복 가능하며 3픽 = 최대 30초 연속 Weaken. 3픽 캡 이후에는 Weaken이 후보에서 제외되어 추가 갱신 불가.

6. **종료·해소 조건**: WeakenAura 잔여 시간이 0이 되면 자동 해제. 영웅 HP 0(승리) 또는 5분 타임오버(패배) 시에도 해제. Weaken 1픽 단독이면 10초 후 자연 해제. 해제 시 `PowerScale` 이 `×2.0`으로 곱연산 복구되어 Weaken 이전 누적값으로 돌아온다.

7. **다른 시스템과 상호작용**: HeroAttackDown 카드(영구 ×0.75, 최대 3픽 = ×0.421)와 동일한 `PowerScale` 위에서 곱연산 복합된다. Debuff Tier2 자동 HeroAttackDown(×0.85) 역시 동일 표면에 누적. 복합 최소값 = 0.75³(HeroAttackDown 3픽) × 0.85(Tier2) × 0.5(Weaken) ≈ 0.179 — 영웅 공격력이 base의 약 18% 수준. Debuff Tier3 영구 출혈(1%/s, 이동 시)과는 독립 작동 — 영웅이 약화 상태에서도 이동하면 출혈 발동. Fear 카드(3초 도주)와 동시 적용 시 도주 중 영웅이 공격하지 않아 Weaken의 실질 기여가 일시 중단된다.

8. **엣지 케이스**: Weaken 지속 중 Fear 발동 시 도주 상태에서는 공격이 없으므로 공격력 감소 효과가 실질적으로 비활성화된다(출혈 효과는 유지). WeakenAura 해제 직전 프레임에 영웅 공격이 발생하면 감소 공격력으로 히트하고, 해제 직후 프레임은 정상 공격력 — 프레임 경계 불연속 존재. Weaken 3픽(30s) + HeroAttackDown 3픽 + Tier2 동시 활성 시 영웅 DPS ≈ 8.9 — Reaper HP 100 처치에 약 11.2초 소요. 반면 Reaper DPS가 Dps Tier1+2 조합이면 영웅을 빠르게 격파 가능, 타임오버 가능성이 낮아져 대규모 QA 데이터 없이는 밸런스 판정 불가. `MinHeroAttackScaleFloor` 손잡이가 없으면 임시·영구 복합으로 공격력이 바닥을 치는 시나리오를 코드 수정 없이 방어할 수단이 없다.

9. **유저 정보·피드백**: `hero-status-icons.md` 시스템이 구현된 경우 Weaken 아이콘과 잔여 타이머가 상태 표시줄에 노출된다. 미구현 시 약화 상태를 인지할 수단이 없어 플레이어가 "왜 몬스터가 죽지 않는가"를 이해하기 어렵다. BattleHud HP바만으로는 영웅 공격력 감소를 가시화할 수 없으므로, Weaken의 전략적 타이밍 관리 가치(언제 Weaken이 끝나는가)가 전달되지 않는 피드백 공백이 된다. 결과 화면에도 Weaken 기여도 표시는 현행 미구현.

### 보류

- **Dps 패시브 ReplaceReapersToHex Power ×1.3 3픽 + Dps Tier1 복합 MaxDpsPowerScale 손잡이**: 종합 15점 동점. 그러나 Tank Power scale 감사(06-19: ReplaceWispsToWraith + Tank Tier2)와 동일 설계 패턴, 축만 다름 — 차별점 부족으로 보류.
- **SpawnWraith 3픽 고출력 + Tank Tier3 HP ×1.4 복합 Wraith 내구도**: 종합 14점. 시너지폭 3점으로 낮음.
- **Debuff Tier3 영구 출혈 ratio BalanceConfig 손잡이**: content-audit 폴더 내 `2026-06-02-debuff-tier3-eternal-bleed-aura-balance.md` 확인됨 — git log 범위 외 이력이나 폴더에서 이미 제안되었음을 확인, 중복 회피.

## 3. 과거 감사 대비 차별성

- git log 조회 18건 검토 완료. content-audit 폴더 30개 파일도 파일명 기준 추가 확인.
- 가장 유사했던 과거 커밋: `8de2ecb` (2026-06-12) "Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안"
- **차별점**: 과거 감사는 HeroAttackDown의 **영구** 누적 하한에 집중했다. 본 감사는 **임시(Weaken)** 배율의 하드코딩과 임시+영구 복합 시 하한값 미설계 문제를 별개로 다룬다. "영구 MinHeroAttackScale이 설정되어도 그 위에 임시 Weaken ×0.5가 추가로 내려갈 수 있다"는 구간이 설계 공백이며, WeakenFactor·WeakenDuration을 독립 BalanceConfig 항목으로 분리해 제어할 필요성이 있다.
- Fear·TimeStop 지속시간 누적 감사(06-11)는 행동 제약 카드의 duration cap을 다뤘으나, Weaken의 공격력 배율 자체는 다루지 않았다.

## 4. 제외 (범위 밖)

- SwarmRush 신규 카드 구현: 컨셉 §11 범위 안이나 별도 구현 사이클 필요 — 이번 제안 대상 아님.
- DebugAutoPicker 훅 구현: game-designer 영역 아닌 gameplay-programmer 구현 작업 — 컨텐츠 감사 제안 대상 아님.
- 신규 영웅·몬스터 추가: CLAUDE.md §8 금지.
- 서버 리더보드 UI 신규 화면: v0.3 단계 기획/구현 미확정.

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청: `WeakenFactor`(현행 0.5), `WeakenDuration`(현행 10s) BalanceConfig 이관 + 임시·영구 복합 하한 `MinHeroAttackScaleFloor` 설계.
- 병행 고려: DebugAutoPicker 훅(QA BLOCKED 해소) — 데이터 기반 검증을 위해 qa-simulator 활성화가 선행되면 이 후보의 실측 임팩트를 시뮬로 확인할 수 있음.

## 6. 쉬운 설명 (비개발자 요약)

Project Lair는 몬스터 군주가 영웅을 5분 안에 쓰러뜨리는 게임이다. 플레이어는 카드를 골라 몬스터를 강화하거나 영웅을 방해하는데, 지금까지 이 감사에서 "이 효과가 너무 강해질 수 있는데 조절 버튼이 없다"는 문제를 반복적으로 발견해왔다. 이번에 발견한 것은 "무력화(Weaken)" 카드로, 영웅의 공격력을 1분 10초 동안 절반으로 깎는 카드다(세 번 고를 경우). 여기에 영웅 공격력을 영구적으로 줄이는 다른 카드들이 겹치면 영웅이 원래 힘의 5분의 1도 안 되는 힘으로 싸우게 되는데, 이 극단적인 상황을 막을 안전장치가 게임 코드에 없다. 균형을 바꾸려면 개발자가 코드를 직접 고쳐야 해서 반복적인 튜닝이 번거롭다. 그래서 이번에 제안하는 것은: 무력화 카드의 "얼마나 약화시킬지"와 "얼마나 오래 지속될지"를 설정 화면에서 바로 조정할 수 있는 손잡이로 만들고, 다른 약화 효과들과 겹쳤을 때의 최소 공격력 하한선도 함께 설계하자는 것이다.
