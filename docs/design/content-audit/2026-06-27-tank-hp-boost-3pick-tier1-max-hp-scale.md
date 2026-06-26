# Content Audit — 2026-06-27 — Tank 패시브 WispHpBoost·WraithDamageBoost 3픽 + Tank Tier1 복합 HP 상한 미설계 — MaxMonsterHpScale BalanceConfig 손잡이

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

| 항목 | 값 |
|---|---|
| 컨셉서 버전 | v0.7 (2026-06-10) |
| 참조 spec/plan 수 | 30 specs / 31 plans |
| 참조 QA 리포트 수 | 1개 (최신: 2026-05-22 — BLOCKED 상태) |
| 참조 기획서 수 | card-renewal.md · continuous-spawn-round.md · village-meta-hub.md 외 다수 |
| 과거 감사 이력 (git log) | 16건 (가장 최근: 2026-06-25 `63ecbd3`) |

---

## 1. 현황

### 1.1 카테고리별 구현 수

| 카테고리 | 컨셉 §11 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 (기사) | 1 (`Knight.prefab`) | 0 |
| 몬스터 | 6종 | 6종 (`Hex/Phantom/Plague/Reaper/Wraith/Wisp.prefab`) | 0 |
| 패시브 카드 | 16장 | 16 SO (28 중 P16) | 0 |
| 액티브 카드 | 12장 | 12 SO (28 중 A12) | 0 |
| BalanceConfig 손잡이 | (명세 없음) | `CharacterStat(Hp/Power/Range/Cooldown/MoveSpeed)` + `SpawnPeriod` | HP 상한 캡 없음 |

### 1.2 계획 있으나 미구현

| 항목 | 출처 | 상태 |
|---|---|---|
| SwarmRush (`ECardId.Multiply` 교체 예정) | card-renewal.md §3.4 · §3.5 | 미구현. `FastBreedingEffect`("빠른 번식") 잔존 |
| Debuff Tier3 EternalBleedAura (`ratio=0.01`, 무제한) | card-renewal.md §4.2 · §4.5 | 클래스 파일 미확인 (BleedEffect.cs 별도 — ratio 0.02 시한 버전만 존재) |
| DebugAutoPicker 훅 (`BattleController`) | QA 리포트 §3 | 미구현 — QA 시뮬레이션 차단 원인 |

### 1.3 QA 권고 미해결

| 항목 | 출처 | 상태 |
|---|---|---|
| `BattleController.DebugAutoPicker` 델리게이트 추가 (약 10줄) | 2026-05-22 QA 리포트 §3 | 미처리 — 해당 훅 없으면 QA 시뮬레이션 불가 |

### 1.4 과거 감사 후보 (git log 조회 결과)

| 날짜 | SHA | subject 설명 |
|---|---|---|
| 2026-06-25 | `63ecbd3` | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-24 | `128bdb8` | 시너지 임계값 3/5/7 하드코딩 — 액티브 트리거 9→5 감소 후 Tier3 도달 난이도 50% 픽집중 · SynergyTierThreshold 손잡이 미설계 |
| 2026-06-23 | `b83b566` | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-22 | `9118936` | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | `a1e0ba4` | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | `0fb40b1` | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | `dcaa8b7` | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | `3a9bed3` | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | `d8fdcfe` | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | `68db140` | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-14 | `6e02b2a` | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — 종별 HP 불균형 + 손잡이 이관 |
| 2026-06-13 | `c07cc2c` | BalanceConfig Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 제안 |
| 2026-06-12 | `8de2ecb` | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale 손잡이 |
| 2026-06-11 | `e4c765b` | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 |
| 2026-06-10 | `abe2ecd` | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | `440794c` | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Tank 패시브 WispHpBoost·WraithDamageBoost 3픽 + Tank Tier1 복합 HP 상한 미설계 — MaxMonsterHpScale BalanceConfig 손잡이

- **카테고리**: Tank 패시브 재조정 / BalanceConfig 손잡이 추가
- **요지**: `WispHpBoostEffect`(`_hpMul=1.5`)와 `WraithDamageBoostEffect`(`_hpMul=1.5`)는 `StatMultiplier.HpMul *= multiplier`로 무한 곱연산 누적되며 상한이 없다. 전역 3픽 캡 기준 최대 복합 배율은 ×1.5³ × 1.3(Tank Tier1) = **×4.39**로, Wraith 기준 HP가 500 → **2,194**로 치솟아 영웅(DPS 50/s) 단독 처치에 **43.9초**가 걸린다. `BalanceConfig`에 `MaxMonsterHpScale` 손잡이가 없어 이 값을 런타임 조절 없이 변경할 수 없다.
- **검증/구현/시너지/데이터**: 4/2/4/4 → 종합 **16**
- **근거**: `StatMultiplier.cs:25` (`HpMul *= multiplier`, cap 없음) + `card-renewal.md §3.1` (WispHpBoost/WraithDamageBoost `_hpMul=1.5`, 곱연산 누적) + `card-renewal.md §4.2` (Tank Tier1: Wisp·Wraith HP ×1.3) + `BalanceConfig.cs:9` (`CharacterStat.Hp` 필드만 존재, HpMul 상한 없음)
- **MVP 범위**: 컨셉 §11.3 — Tank 축 패시브 WispHpBoost·WraithDamageBoost 수치 재조정 + §11.2 BalanceConfig 손잡이 추가 범주

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**

영웅 HP 10%마다 발생하는 패시브 카드 선택 팝업(최대 9회)에서 `WispHpBoost`("끈질긴 위스프") 또는 `WraithDamageBoost`("망령의 압박") 카드가 3택 1 후보로 등장한다. Tank 축 카드를 3장 픽하는 순간 Tank Tier1 시너지(Wisp·Wraith HP ×1.3)가 즉시 발화된다. 복합 HP 상한 문제는 WispHpBoost·WraithDamageBoost를 각각 3픽(전역 3픽 캡 도달)하고 Tank Tier1이 발화된 시점에 완성된다.

**2. 화면 변화**

Tank 축 카드를 픽할 때마다 좌측 시너지 패널의 "TANK N/3" 카운터가 올라간다. 3픽 도달 시 화면 중앙 상단에 "Tank 시너지 Tier 1 발동!" 토스트가 1.5초 표시되고, 좌측 패널 TANK 행에 TANK 아이콘 1개가 점등된다. 필드의 Wisp·Wraith는 `ApplyMonsterStats` 소급 호출로 즉시 HP 상한이 상향되며, HP 바가 늘어난 만큼 채워진다. MaxMonsterHpScale 부재 상태에서는 HP 바가 가득 찬 채로 표시되어 "이 몬스터는 매우 튼튼하다"는 시각 신호를 준다.

**3. 입력 행동**

플레이어는 HP 10% 트리거마다 카드 팝업에서 Tank 축 카드(초록 테두리)를 선택한다. 이상적인 집중 픽 경로: HP 90% 시 WispHpBoost 픽 → HP 80% 시 WispHpBoost 2번째 픽 → HP 70% 시 WispHpBoost 3번째 픽(Tank Tier1 발화). 이후 HP 60%~40%에서 WraithDamageBoost 3픽을 반복하면 Wraith HP × 4.39 복합 상태에 도달한다.

**4. 시스템 반응**

`WispHpBoostEffect.Apply` 호출 시 `ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)` → `StatMultiplier.HpMul *= 1.5`. 3픽 후 `HpMul = 1.5³ = 3.375`. Tank Tier1 발화 시 `BuildSynergyService`가 `RegisterMonsterTypeBuff(Wisp, Hp, 1.3)` 추가 호출 → `HpMul = 3.375 × 1.3 = 4.3875`. 현재 `StatMultiplier.Multiply` (`StatMultiplier.cs:25`)에는 `Mathf.Clamp`나 상한 없이 `HpMul *= multiplier` 한 줄뿐이다. `BattleController.ApplyMonsterStats`는 `BalanceConfig.GetMonster(key).Hp × HpMul`로 최대 HP를 계산 — Wraith 기준 `500 × 4.39 = 2,194 HP`.

**5. 반복·재발생 패턴**

전역 3픽 캡으로 WispHpBoost·WraithDamageBoost 각 카드는 3픽 이상 불가 → 복합 HpMul 최대값은 단일 종 기준 `1.5³ × 1.3 = 4.39`로 고정. 그러나 Tank Tier2(Wisp·Wraith Power ×1.2)까지 도달하면 HP 외 Power도 동시 강화된다. Tank Tier1 이후 추가 픽에서 `SpawnWraith`를 중복 선택해 Wraith Spawner 출력 +1씩 쌓으면(최대 +3), HP 2,194짜리 Wraith가 더 자주 스폰되어 영웅이 처치 전 다수 Wraith와 대치하는 상황이 지속적으로 반복된다.

**6. 종료·해소 조건**

`StatMultiplier.HpMul`은 라운드 내 영구 유지 — 한번 올라간 HP 상한은 낮아지지 않는다. 현재 `BalanceConfig`에 `MaxMonsterHpScale` 필드가 없으므로, 설계 의도와 무관하게 `1.5³ × 1.3 = 4.39`가 런타임 허용 상한이다. 라운드 종료(영웅 사망 또는 5분 타임오버) 시 `_typeModifiers` dict가 초기화되어 다음 런에는 초기화됨. 인스펙터·에셋 수정 없이는 이 수치를 조정할 방법이 없다.

**7. 다른 시스템과 상호작용**

- **GuardianRage + IronWill + ToughHide (Tank 액티브 3종)**: DamageTakenScale 중첩은 2026-06-10 감사에서 다뤘으나, HP 상한 상승과 DamageTakenScale 감소가 복합되면 Wraith 생존 시간이 기하급수적으로 늘어난다. HP 2,194 + 받피 ×0.5×0.7×0.75 = ×0.2625 조합 시 영웅 유효 DPS 기준 처치 시간: 2,194 / (50 × 0.2625) ≈ **167초** (2분 47초). 5분 게임에서 단 1마리가 2분 47초 동안 살아있게 된다.
- **BloodThirst (Dps 액티브)**: Wraith HP가 높을수록 처치 순간 회복량(주변 몬스터 HP +30)의 절대값은 동일하지만, 처치 빈도가 낮아져 BloodThirst의 실효 발동 횟수가 줄어든다.
- **영웅 스킬 (hero-skills.md Phase1·Phase2·Phase3)**: 광역 스킬은 HP에 비례해 타격 횟수를 더 요구하므로, HP 2,194짜리 Wraith가 많을수록 영웅의 스킬 효율이 떨어지고 영웅 생존 시간이 단축될 수 있다.
- **메타 상점 MonsterHpUp**: ShopItem `Id="MonsterHpUp"` (`PerLevelMul=1.02f`, `MaxLevel=5`) 가 만렙 시 HP ×1.104 추가. 카드 복합 HP에 메타 보너스까지 쌓이면 Wraith HP = 2,194 × 1.104 = **2,422 HP** (처치 48.4초).

**8. 엣지 케이스**

| 상황 | Wraith HP | 영웅 처치 시간(50 DPS) |
|---|---|---|
| 기준 (카드 없음) | 500 | 10.0s |
| WraithDamageBoost 1픽 | 750 | 15.0s |
| WraithDamageBoost 3픽 | 1,687 | 33.7s |
| WraithDamageBoost 3픽 + Tank Tier1 | 2,194 | **43.9s** |
| WraithDamageBoost 3픽 + Tank Tier1 + ToughHide (받피×0.75 영구) | 2,194 | 영웅 유효: 43.9/0.75 ≈ **58.5s** |
| WraithDamageBoost 3픽 + Tank Tier1 + MetaShop Lv5 | 2,422 | **48.4s** |

`StatMultiplier`의 `HpMul`에 `Mathf.Clamp` 없음 — `BattleController.RegisterMonsterTypeBuff` (`BattleController.cs:683`)에서 `m.Multiply(stat, multiplier)` 호출 시 상한 검사 없음. BalanceConfig에 `MaxMonsterHpScale` 필드 부재 확인 (`BalanceConfig.cs` 전체 검토).

**9. 유저 정보·피드백**

HP 바(`MonsterHpBar`)는 현재 HP/최대 HP 비율로 길이가 결정된다. HP 2,194짜리 Wraith는 처치 중에도 HP 바가 대부분 채워진 채 보여 플레이어가 "영웅이 고전하고 있다"는 긍정 피드백을 받는다. 그러나 영웅이 같은 Wraith에 43초 동안 붙어있는 상황이 연출되면 **전투 페이싱이 느리게 느껴질 수 있다** — 5분 게임의 핵심 템포(§8 평균 2~4분 사망)가 훼손될 가능성이 있다. `card-renewal.md §1 QA 정합성` 노트("평균 사망 86s라도 영웅이 사망한다")는 이 상황이 과도한 압박을 주지 않도록 설계하는 방향이나, Wraith HP 극단 빌드는 그 노트의 기본 가정을 벗어난다. MaxMonsterHpScale 손잡이가 있으면 영웅이 한 Wraith에 묶이는 시간을 설계 범위 안으로 조절할 수 있다.

### 보류

- **SwarmRush 구현 (Multiply 교체)**: card-renewal.md §3.4 원안 의도이나 신규 카드 구현 범주 — BalanceConfig 손잡이 패턴과 성격 달라 보류.
- **DebuffTier2Factor (×0.85) BalanceConfig 이관**: Debuff Tier2 HeroAttackDown 자동 등록의 factor=0.85가 코드 상수. 2026-06-12 MinHeroAttackScale 감사와 부분 중복. 보류.
- **Spawner 출력 수 상한 (MaxSpawnerOutputCount)**: SpawnWraith 3픽 + Swarm Tier3 = Wraith 출력 최대 4. 글로벌 캡(18마리)으로 자연 제한되어 즉각 위험도는 낮음. 보류.

---

## 3. 과거 감사 대비 차별성

git log 조회 16건 검토 완료.

가장 유사했던 과거 커밋: `0fb40b1` (2026-06-19) — "Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계"

**차별점**:
- 2026-06-19는 **Power** 스탯 상한(MaxTankPowerScale) 부재를 다뤘음.
- 본 감사는 **HP** 스탯 상한(MaxMonsterHpScale) 부재를 다룸 — `StatMultiplier`의 다른 필드(`PowerMul` vs `HpMul`).
- Power 상한 부재는 "영웅에게 가하는 데미지 과잉" 문제. HP 상한 부재는 "영웅이 단일 몬스터에 43초 이상 묶이는 페이싱 훼손" 문제 — 게임 템포와 밸런싱 기준(§8 평균 2~4분 사망)에 직접 영향.
- 직전 7일 이내 Tank 축 감사: 없음 (2026-06-19가 7일 초과, 8일 전). 카테고리 차별 확보.

---

## 4. 제외 (범위 밖)

- **영웅 다중화·신규 영웅 추가** — v0.3 §8 금지.
- **신규 몬스터 종 추가** — v0.3 §8 금지.
- **신규 카드 추가** — v0.3 §8 "신규 영웅·몬스터·카드 리소스 제작 금지".
- **HP 상한 초과 시 HP 바 시각화 전면 리디자인** — 범위 밖 UI 변경.
- **서버 측 HP 검증 로직** — 별도 레포 `Project_Lair_Server` 소관.

---

## 5. 다음 단계 제안

1. **채택 시**: game-designer에게 `MaxMonsterHpScale` 제안 수치(예: `4.0`) 기획 요청. 컨셉 §8 밸런싱 기준(평균 2~4분 사망)과의 정합성 검증 포함.
2. **구현 경로**: `BalanceConfig.cs`에 `[SerializeField] private float _maxMonsterHpScale = 4.0f` 추가 → `BattleController.RegisterMonsterTypeBuff` (`:683`) 또는 `StatMultiplier.Multiply` (`:21`)에서 `HpMul = Mathf.Clamp(HpMul, 0f, _balance.MaxMonsterHpScale)` 적용. 약 5~15줄 변경.
3. **QA**: qa-simulator 훅(`DebugAutoPicker`) 미구현 상태이므로 에디터 `LairBalanceWindow`로 Wraith HP 스탯을 직접 확인하거나 PlayMode 테스트로 검증 권장.

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어(던전 주인)는 "위스프 강화" 카드나 "레이스 강화" 카드를 같은 것을 3번까지 고를 수 있다. 3번 다 고르면 레이스의 체력이 원래보다 약 4.4배까지 커진다 — 예를 들어 기본 체력이 500이라면 2,200 가까이 된다. 영웅이 공격력 50으로 때리면 이 레이스 한 마리를 죽이는 데만 44초가 걸린다. 5분짜리 게임에서 한 몬스터 한 마리 처치에 44초를 쓰는 건 너무 지루하게 느껴질 수 있다. 지금은 이 체력 상한을 조절하는 설정 값(손잡이)이 없어서 수치를 바꾸려면 코드를 직접 고쳐야 한다. 그래서 이번에 제안하는 것은: **"몬스터 체력 배율 최대값"을 설정 파일에 추가해서 손쉽게 조절할 수 있게 하자**는 것이다.
