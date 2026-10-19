# 유물 (Relics) 2차 - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-19 · 단계: v0.3 · 전제: 난이도 선택 1차 구현 완료(`difficulty-select.md`)
> 입력: `C:\Users\Tae\.claude\plans\reactive-wishing-newt.md` '2차 - 유물' · `difficulty-select.md`(스타일·접점) · `village-meta-hub.md`(소울 경제·상점 상한) · `references/2026-06-01-risk-of-rain-2-item-stacking.md`(중첩 공식) · 코드 `BattleController.cs`(ApplyMetaBonuses ~1026, EndBattle ~1323) / `PauseService.cs` / `BattleClock.cs` / `StatMultiplier.cs` / `MetaProfile.cs` / `PoisonAura.cs` / `HeroAttackDownAura.cs` / `Spawner.cs` / `ResultPopup.cs` / `CommonEnum.cs`
> **UI 목업**: `.mockups/relics.html` (마을 HUD 버튼 위치 · 유물 도감 팝업 · 결과창 획득 줄 · 전투 HUD 2배속 토글)
> **데이터 구조표**: 본 문서 §3 (사용자 승인 게이트 대상 - 신규 JSON 스키마)

---

## § 헤더

- **목표**: 전투가 끝날 때 영웅(챕터)·스테이지·난이도에 따라 정해진 확률로 **유물**이 드랍되고, 모은 유물은 슬롯 제한 없이 **전부 자동 적용**되어 다음 전투부터 영구적으로 던전이 강해지는 수집 루프를 만든다. 유물은 3종류(영구 능력치 / 전투 효과 / 편의)이며 마을의 도감형 팝업에서 수집 현황을 본다.
- **검증 가설**: (1) "이긴 뒤에도 마을에서 할 게 없다" 문제가 "유물 수집 + 높은 난이도 재도전(드랍 확률/등급 상승)" 루프로 해소되는가. (2) 중복 획득을 체감 감소 스택으로 처리했을 때 "또 나온 유물" 이 허탈하지 않고, 풀셋이 되어도 난이도(특히 Hard)가 무의미해지지 않는가. (3) 편의 유물(2배속)이 랜덤 드랍 보상으로서 동기를 주면서 5분 제한·랭킹 공정성을 해치지 않는가.
- **현재 단계 범위 적합성**: **범위 밖이었던 항목의 명시 승격 요청 - 사용자 승인 필요.** CLAUDE.md §8 은 "유물" 을 개별 승격 전까지 금지한다. 난이도 선택 1차 승격 시 "유물은 2차 예고" 로 남겨 둔 항목이며 §9 에 승격 문구 초안이 있다. **승인 전에는 구현에 착수하지 않는다.** ★ 목표 · 던전 편성 · 신규 몬스터 · 무한 탑 · 데일리 · 유물 합성/강화/판매는 계속 금지(후속 제안으로만 §10 에 언급). 서버는 Firebase 클라이언트 코드 범위 유지(신규 서버 코드 0, 유물은 `MetaProfile` 일부로 기존 클라우드 백업에 실린다). 별도 메인 메뉴/세팅 화면 금지 유지(도감은 마을 팝업).
- **핵심 메커니즘**: (a) 승리 시 `스테이지 기본 확률 x 난이도 보정 + 연패 보정(pity)` 로 유물 1개를 굴려, 등급(일반/희귀/전설)을 난이도 가중으로 먼저 뽑고 그 등급 안에서 **영웅 드랍 테이블 가중치**로 유물을 뽑는다. (b) 같은 유물을 또 얻으면 **중첩(최대 1~5)** 되며 효과는 체감 감소 스케일표 `RelicStack` 으로 커진다. 최대 중첩에 도달한 유물은 후보에서 빠진다. (c) 보유 유물은 전투 시작 시 일괄 적용(능력치는 `_typeModifiers` 곱연산, 전투 효과는 `IHeroAura`, 편의는 보상 가산·배속).

**해석 확인**: "전투 종료 시 영웅/스테이지별 확률 드랍 · 슬롯 없이 전부 적용 · 편의도 랜덤 드랍 · 난이도가 높을수록 확률/등급 상승" 모두 반영. 이견 없음. 다만 **"밸런스는 콘텐츠 완성 후 qa-simulator 일괄 튜닝"** 이므로 본 문서의 모든 수치는 JSON 초기값이며 §9 에 검증 게이트만 예약한다.

---

## 1. 유물 목록 (16종)

### 1.1 체계

| 항목 | 내용 |
|---|---|
| 종류(category) | `Stat`(영구 능력치) 6 · `Combat`(전투 효과) 5 · `Convenience`(편의) 5 = 16종. 요구 "종류별 5~8개" 충족 |
| 등급(rarity) | `Common`(일반) 8 · `Rare`(희귀) 6 · `Legendary`(전설) 2 |
| 중첩 상한(maxStack) | Common 3~5 · Rare 1~3 · Legendary 1. 유물별 `Relic.maxStack` 에 저장 |
| 이름 규칙 | 유물 ID = `ERelic` Enum 값명 = 아이콘 스프라이트 파일명 (Rule 03 §2). 한글 표시명·설명은 `Strings_Ko` |
| 테마 | "쓰러진 영웅이 남긴 유품" - 영웅을 이길수록 던전 주인이 노획품으로 강해진다는 서사. 영웅별 드랍 테이블이 다른 근거 |

**RoR2 레퍼런스 적용**(`2026-06-01-risk-of-rain-2-item-stacking.md`): ① Common 은 많이 쌓이되 한계효용이 줄어드는 체감 곡선(§2.3), ② Legendary 는 중첩 1 로 한 번에 체감되는 큰 효과(RoR2 Legendary), ③ "내가 지금 몇 중첩인가" 를 도감 셀과 결과창에 숫자로 노출(RoR2 스택 버블). 쌍곡/곱산 공식은 쓰지 않는다(§2.3 대안 비교).

### 1.2 마스터 표

중첩 1 기준 기본값. 표의 `amount` 류 값은 §2.3 의 `RelicStack` 스케일 `S(n)` 이 곱해진다. 구현 접점은 §6 에 상세.

| # | ERelic (ID) | 표시명 | 종류 | 등급 | maxStack | 효과(중첩 1) | 효과 종류(`ERelicEffectKind`) |
|---|---|---|---|---|---|---|---|
| 1 | `HeartOfStone` | 단단한 심장석 | Stat | Common | 5 | 모든 몬스터 HP +3% | MonsterStat (Hp) |
| 2 | `RustyBlade` | 녹슨 칼날 | Stat | Common | 5 | 모든 몬스터 공격력 +2% | MonsterStat (Power) |
| 3 | `SwiftBoots` | 질풍의 장화 | Stat | Common | 5 | 모든 몬스터 이동속도 +2% | MonsterStat (MoveSpeed) |
| 4 | `BrokenHourglass` | 깨진 모래시계 | Stat | Rare | 3 | 모든 몬스터 공격 대기시간 -2% | MonsterStat (Cooldown) |
| 5 | `BreedingCharm` | 번식의 부적 | Stat | Rare | 3 | 모든 스포너 주기 -3% | SpawnerPeriod |
| 6 | `KingsCrownShard` | 왕관 조각 | Stat | Legendary | 1 | 모든 몬스터 HP·공격력 각 +5% | MonsterStat x2 (Hp, Power) |
| 7 | `EmberBrand` | 잿불 낙인 | Combat | Common | 5 | 영웅이 매초 최대 HP의 0.02% 화상 피해 | HeroBurn |
| 8 | `AmbushHorn` | 매복의 뿔피리 | Combat | Common | 3 | 전투 시작 시 영웅 근처에 도깨비불 2마리 | OpeningSummon |
| 9 | `ThunderShard` | 뇌운 조각 | Combat | Rare | 3 | 10초마다 영웅에게 번개, 최대 HP의 0.3% 피해 | HeroLightning |
| 10 | `CursedBanner` | 저주의 깃발 | Combat | Rare | 3 | 영웅 공격력 -4% | HeroAttackDown |
| 11 | `WrathSpirit` | 분노의 영혼 | Combat | Legendary | 1 | 영웅 HP 50% 이하가 되면 몬스터 전체 10초간 광폭화(1회) | LowHpFrenzy |
| 12 | `HasteHourglass` | 가속 모래시계 | Convenience | Rare | 1 | 전투 중 2배속 토글 사용 가능 | BattleSpeed |
| 13 | `SoulPurse` | 소울 주머니 | Convenience | Common | 5 | 전투 소울 보상 +4% | SoulBonus |
| 14 | `LordsTome` | 영주의 서 | Convenience | Common | 5 | 영주 경험치 보상 +4% | XpBonus |
| 15 | `ConsolationFeather` | 위로의 깃털 | Convenience | Common | 3 | 패배 시 소울 보상 +25% | LoseSoulBonus |
| 16 | `SpawnPrelude` | 서두르는 둥지 | Convenience | Rare | 1 | 모든 스포너의 첫 스폰이 3초 빨라짐(0초 하한) | SpawnPrelude |

- 편의 유물도 랜덤 드랍(요구). 편의 중 `SpawnPrelude` 는 전투 초반 공백을 줄이는 템포 편의이며 순수 편의로 분류했다(전투 효과가 아닌 이유: 몬스터 총량이 아니라 "첫 스폰 대기" 만 앞당김).
- 능력치 유물은 상점 `MonsterHpUp` 등과 **같은 표면(`_typeModifiers` / `StatMultiplier`, 6축 중 Hp·Power·Cooldown·MoveSpeed)** 에서 곱연산 누적된다. Range·SlowFactor 축은 상점이 이미 가지고 있어 유물에서는 쓰지 않는다(축 중복 최소화, 이번 16종 구성에서 비움).
- **중첩별 실제 값**(S(n) 적용, §2.3 표 기준): `HeartOfStone` n=5 → +8.4% · `RustyBlade` n=5 → +5.6% · `SwiftBoots` n=5 → +5.6% · `BrokenHourglass` n=3 → -4.2% · `BreedingCharm` n=3 → -6.3% · `EmberBrand` n=5 → 0.056%/초 · `ThunderShard` n=3 → 0.63% · `CursedBanner` n=3 → -8.4% · `SoulPurse` n=5 → +11.2% · `LordsTome` n=5 → +11.2% · `ConsolationFeather` n=3 → +52.5%. `AmbushHorn` 은 S 가 아니라 `countPerStack x n` (n=3 → 6마리).

### 1.3 풀셋 환산 압력 (초기 모델, 검증 전)

상점 만렙 검산(`village-meta-hub.md` §3.3, 가중치 Power·공속 1.0 / HP 0.4 / 스폰 0.5 / 이동 0.3)과 동일 모델로 유물 풀셋(전 유물 최대 중첩)의 몬스터 측 압력을 추정한다.

| 항 | 풀셋 변화 | 가중치 | 환산 기여 |
|---|---|---|---|
| 공격력 | 1.056 x 1.05 = 1.1088 | 1.0 | +10.9% |
| 공속(쿨다운 0.958) | 1/0.958 = 1.0438 | 1.0 | +4.4% |
| HP | 1.084 x 1.05 (각 항에 가중 0.4 개별 적용) | 0.4 | +5.4% (1.0336 x 1.02 = 1.0543) |
| 스폰(주기 0.937) | 스폰률 1.0672 | 0.5 | +3.4% |
| 이동속도 | 1.056 | 0.3 | +1.7% |

(HP 항은 `HeartOfStone` 1.084 x `KingsCrownShard` 1.05 의 각 가중 환산: (1+0.084x0.4) x (1+0.05x0.4) = 1.0336 x 1.02 = 1.0543.)

- **능력치 풀셋 합산** = 1.1088 x 1.0438 x 1.0543 x 1.0336 x 1.0168 = **1.283 (+28.3%)** 산식 단계: 1.1088x1.0438 = 1.1574 → x1.0543 = 1.2203 → x1.0336 = 1.2613 → x1.0168 = 1.2825.
- **전투 효과 풀셋**(영웅 최대 HP 비율 피해, 처치 시각 T 에 의존): `EmberBrand` 0.056%/초 + `ThunderShard` 0.63%/10초 = 0.063%/초 → 합 0.119%/초. 기준 압력 = 100%/T. T=76초(구 베이스라인, 1.316%/초)면 +9.0%, T=200초(0.5%/초)면 +23.8%. `CursedBanner` n=3 (공격력 x0.916)은 가중 0.3 로 +2.8%, `WrathSpirit`·`AmbushHorn` 은 합 +3% 로 가정.
- **총합** 능력치 x 전투 = T=76초 기준 1.283 x 1.090 x 1.028 x 1.03 ≈ 1.48, T=200초 기준 1.283 x 1.238 x 1.028 x 1.03 ≈ 1.68. 상점 만렙(+30.8%)과 곱하면 영웅 입장 압력이 크게 커진다. **이 값은 qa-simulator 일괄 튜닝 전제의 출발점이며 단정하지 않는다.** 튜닝 1순위 레버는 `RelicStack.effectScale`(2.8 → 2.2 등)과 `Relic_Effect_Param` 수치(코드 변경 0).
- 설계 의도: 풀셋을 모아도 Hard(영웅 HP x1.30 / 공격력 x1.22)는 도전적으로 남아야 한다. 이 의도의 판정 지표는 §9 게이트 R3(Hard 승률 하한 · 처치 시각 하한).

---

## 2. 드랍 규칙

### 2.1 드랍 시점 - 대안 비교

| 안 | 내용 | trade-off | 판정 |
|---|---|---|---|
| A. 승리 시에만 | 패배/시간초과는 드랍 없음 | 단순. 하지만 막혀서 못 깨는 플레이어에게 아무 보상이 없어 "유물이 쌓일수록 쉬워진다" 의 구제 기능이 없다 | 기각 |
| **B. 승리 = 정상 확률, 패배 = 확률 x 0.25 (권장)** | 패배도 굴리되 기본 확률의 1/4. 패배 굴림은 연패 보정(pity) 미사용, 드랍 시에만 연패 카운터 초기화 | 막힌 플레이어의 구제(5분 소모의 대가). 패배는 5분 시간초과뿐이라 일부러 지는 파밍 비용이 크고, 승리 대비 기대값 1/4 라 파밍 유인이 없다 | **채택** |
| C. 패배도 동일 확률 | 승패 무관 | 승리의 프리미엄이 사라지고 의도적 패배(영웅 방치)가 최적 | 기각 |

- 패배 정의: `BattleResult.Lose` (현행은 5분 시간 초과). 뒤로가기 종료(EndBattle 비경유)는 굴리지 않는다.
- 승리 시 굴림은 `profile.ClearedStage` 갱신 여부와 무관하게 매 승리마다 한다(이미 깬 스테이지 재도전도 드랍 - 수집 루프의 핵심).

### 2.2 확률 공식

```
승리:
  pity        = Min(pityMaxBonus, RelicMissStreak x pityPerMiss)
  chance      = Clamp(stageChance x diffChanceMul + pity, 0, maxChance)
  if 보유 유물 0개 → chance = 1.0   (첫 유물 보장, §2.6)
패배:
  chance      = Clamp(stageChance x diffChanceMul x lossChanceMul, 0, maxChance)   (pity 미사용)

stageChance   = Stage_Relic.dropChance (chapterId, stageIndex)
diffChanceMul = Difficulty_Relic.dropChanceMul (EffectiveDifficulty)
```

| 값 | 초기값 | 출처(JSON) |
|---|---|---|
| 일반 스테이지(x-1 ~ x-4) `dropChance` | **0.30** | `Stage_Relic` 20행 |
| 보스 스테이지(x-5) `dropChance` | **0.60** | `Stage_Relic` 5행 |
| 난이도 보정 Easy / Normal / Hard | **1.00 / 1.25 / 1.50** | `Difficulty_Relic` |
| `lossChanceMul` | **0.25** | `RelicConfig` |
| `pityPerMiss` / `pityMaxBonus` | **0.10 / 0.40** | `RelicConfig` |
| `maxChance` | **0.95** | `RelicConfig` |

난이도 x 스테이지 최종 기본 확률(pity 전):

| | Easy | Normal | Hard |
|---|---|---|---|
| 일반 스테이지 승리 | 0.300 | 0.375 | 0.450 |
| 보스 스테이지 승리 | 0.600 | 0.750 | 0.900 |
| 일반 스테이지 패배 (x0.25) | 0.075 | 0.094 | 0.113 |

검산: 0.30x1.25 = 0.375 / 0.30x1.50 = 0.45 / 0.60x1.25 = 0.75 / 0.60x1.50 = 0.90 / 0.30x0.25 = 0.075 / 0.375x0.25 = 0.09375 / 0.45x0.25 = 0.1125. pity 최대 +0.40 가산 시 최악 0.90+0.40 = 1.30 → `maxChance` 0.95 로 절단.

### 2.3 판당 최대 개수 · 중첩(스택) 규칙

- **판당 최대 1개.** 한 판에 여러 번 굴리지 않는다(결과창 연출·밸런스 단순화). 보스/Hard 의 보상 강화는 확률로만 표현한다.
- **중복 획득 = 중첩 +1**(상한 `maxStack`). 중첩 n 의 `amount` 류 효과 = `기본값 x S(n)`, `S` 는 `RelicStack` 표.

`RelicStack` 초기값(체감 감소: 한계 증가분 0.6 → 0.5 → 0.4 → 0.3):

| stack | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| `effectScale` S(n) | 1.00 | 1.60 | 2.10 | 2.50 | 2.80 |

검산: 1.00+0.60 = 1.60 / +0.50 = 2.10 / +0.40 = 2.50 / +0.30 = 2.80.

**중첩 공식 대안 비교** (RoR2 레퍼런스 §1.1 3종):

| 안 | 공식 | trade-off | 판정 |
|---|---|---|---|
| A. 선형 `n x 기본값` | RoR2 Syringe | 직관적이나 maxStack 5 일 때 5배 → Common 이 Legendary 를 압도, 풀셋 압력 폭주(레퍼런스 §1.3 hyperstack 위험 - 5분 고정 런엔 안전장치 없음) | 기각 |
| B. 쌍곡 `1-1/(1+k n)` | RoR2 Tougher Times | 상한 수렴으로 안전하나 "값 하나" 가 아니라 비율형에만 맞고 -% 쿨다운/+% 혼재 표현이 복잡. 설명문 수치 계산이 어려움 | 기각 |
| **C. 이산 스케일표 `RelicStack`** | S(n) 표 (체감 감소) | 밸런서가 JSON 숫자만 바꿔 곡선 전체를 튜닝. 쌍곡의 "첫 픽이 가장 가치 있다" 성질을 표 한 줄로 재현. 설명문 수치 = 기본값 x S(n) 로 단순 | **채택** |

- `countPerStack` 키(소환 마릿수 등 정수 효과)는 S 대신 `x n` 선형(정수 유지). 그 외 고정 키(`intervalSec`, `durationSec`, `hpFraction`, `statKind`, `monster`, `speedScale`)는 중첩해도 변하지 않는다.
- 스케일 적용 키(S(n) 곱): `amount`, `hpRatioPerSec`, `hpRatio`, `advanceSec`. 선형 키: `countPerStack`. 나머지 고정. (표: §3.3)
- **중첩 상한 도달** 유물은 드랍 후보에서 **제외**한다(소울 환급 없음, 합성/강화 없음). 후보가 0개면 그 굴림은 "미획득" 으로 처리하되 연패 카운터는 증가시키지 않는다(풀셋 근처 플레이어가 pity 로 확률만 불어나는 것 방지).

**중복 처리 대안 비교**:

| 안 | 내용 | trade-off | 판정 |
|---|---|---|---|
| A. 중복 무시(미보유만 드랍) | 후보에서 보유 제외 | 간단하나 같은 유물의 양적 성장이 없어 후반 드랍 의미 소멸, 16종 수집 후 종료 | 기각 |
| B. 중복 = 소울 환급 | 중복 시 소울 | 수집 허탈감은 줄지만 소울 경제(상점 49런 페이싱)에 개입 | 기각(경제 개입) |
| **C. 중복 = 중첩(권장, §2.3)** | 상한까지 중첩 | 장기 목표(총 52 슬롯)를 만들고 "또 나왔다" 가 강화가 된다. 합성/강화 시스템 없이 같은 효과 | **채택** |

총 중첩 슬롯 = Common (5+5+5+5+3+5+5+3 = 36) + Rare (3+3+3+3+1+1 = 14) + Legendary (1+1 = 2) = **52**. 52 슬롯 완성에 필요한 승리 횟수는 §9 게이트 R1 로 측정.

### 2.4 등급 -> 유물 2단계 굴림

```
1) 후보 집합 C = { Relic_Drop 에서 heroId == 해당 스테이지 챕터의 영웅 인 행의 relicId } 중 현재 중첩 < maxStack 인 것
2) C 가 비었으면 미획득 (연패 카운터 불변)
3) 등급 굴림: 등급 g 가 C 에 후보가 1개 이상 있는 등급들 중에서
      가중치 w(g) = RelicRarity.baseWeight(g) x Difficulty_Relic_Rarity.weightMul(EffectiveDifficulty, g)
      → 확률 비례 추첨
4) 유물 굴림: g 등급 후보들 중 Relic_Drop.weight 비례 추첨
5) 해당 유물 중첩 +1, RelicMissStreak = 0 (승리 굴림에서 획득한 경우)
```

등급 기본 가중치와 난이도 배율:

| 등급 | baseWeight | Easy | Normal | Hard |
|---|---|---|---|---|
| Common | 70 | 1.0 | 0.8 | 0.6 |
| Rare | 25 | 1.0 | 1.4 | 1.8 |
| Legendary | 5 | 1.0 | 2.0 | 4.0 |

유효 가중치와 등급 확률(세 등급 모두 후보가 있을 때):

| 난이도 | Common | Rare | Legendary | 합 | 확률 C / R / L |
|---|---|---|---|---|---|
| Easy | 70 | 25 | 5 | 100 | 70.0% / 25.0% / 5.0% |
| Normal | 56 | 35 | 10 | 101 | 55.4% / 34.7% / 9.9% |
| Hard | 42 | 45 | 20 | 107 | 39.3% / 42.1% / 18.7% |

검산: 70x0.8 = 56 / 25x1.4 = 35 / 5x2.0 = 10 / 70x0.6 = 42 / 25x1.8 = 45 / 5x4.0 = 20 / 56/101 = 0.554 / 42/107 = 0.393 / 20/107 = 0.187.
- 요구 "난이도가 높을수록 드랍 확률/등급 상승" = `dropChanceMul`(확률) + `weightMul`(등급) 로 충족. `DifficultyTable`/`StageProgress` 의 `EffectiveDifficulty`(전투 시작에 확정된 값)를 그대로 쓰며 판 도중 다시 읽지 않는다.
- 영웅 테이블에 해당 등급 유물이 아예 없는 영웅(예: 기사의 Legendary 중 `WrathSpirit` 없음, 궁수·암살자의 `KingsCrownShard` 없음)은 그 영웅 후보에서 해당 등급이 빠질 뿐 등급 확률은 남은 등급으로 재정규화한다. 모든 영웅의 Legendary 후보는 1~2개 존재한다(§3.4 표).

### 2.5 천장(pity) - 제안과 대안

| 안 | 내용 | 판정 |
|---|---|---|
| 없음 | 순수 확률 | 일반 스테이지 Easy 0.30 에서 5연속 미획득 확률 0.7^5 = 16.8% - 체감 불운이 크다 → 기각 |
| **소프트 pity (권장)** | 승리 굴림 미획득 시 `RelicMissStreak++`, 다음 승리 확률에 `+0.10 x streak` (최대 +0.40), 획득 시 0 | **채택** |
| 하드 pity | N연패 시 100% | 소프트로 충분. 추가 상태 규칙 불필요 → 기각 |

소프트 pity 효과 검산(Easy 일반 스테이지, 확률 수열 0.30 / 0.40 / 0.50 / 0.60 / 0.70 이후 0.70 고정): 첫 획득까지 기대 승리 횟수 = 1 + 0.70 + 0.42 + 0.21 + 0.084 + 0.0252 + 0.0076 + ... ≈ **2.45회**(꼬리 합 포함 2.45). 누적 항 산식: 생존확률 0.7 → 0.7x0.6 = 0.42 → 0.42x0.5 = 0.21 → 0.21x0.4 = 0.084 → 0.084x0.3 = 0.0252 → 0.0252x0.3 = 0.00756.
- `RelicMissStreak` 는 승리 굴림에서만 변한다. 패배 굴림은 읽지도 쓰지도 않고, 패배에서 획득하면 0 으로 초기화한다(연패 카운터를 패배로 날리지 않기 위함이 아니라 "획득 = 초기화" 의 일관성).
- 후보 전부 보유 중인 상태의 승리는 굴리지 않고 미획득 처리(카운터 불변).

### 2.6 첫 유물 보장

보유 유물이 0개인 프로필의 첫 승리는 `chance = 1.0`(등급/유물 굴림은 평소대로). 이유: 기능 발견성(기존 유저는 업데이트 후 첫 승리에서 반드시 유물 시스템을 접함). 보유 1개 이상이 되면 일반 공식. 패배 굴림에는 적용하지 않는다.

### 2.7 드랍 후 처리 순서 (EndBattle 정산 순서)

`difficulty-select.md` §4.4 및 `village-meta-hub.md` §11.4 정산 순서를 유지하고 다음 두 지점을 끼운다.

```
소울/XP 계산: SoulRewardCalculator.Calculate → "유물 보상 가산"(§6.4, 전투 시작 스냅샷의 SoulBonus/XpBonus/LoseSoulBonus 적용, floor)
→ profile.Souls/LordXp 가산 → RecordStageRun/ClearedStage/DifficultyClear (기존)
→ [신규] 유물 드랍 굴림 → profile.Relics 갱신, RelicMissStreak 갱신
→ 도감/픽 기록 → 영주 보상 → 도전과제 → Save → ResultPopupArg 에 유물 필드 채움
```

- **이번 판에 얻은 유물은 이번 판 보상에 적용되지 않는다**(보상 가산은 전투 시작 스냅샷 기준, 드랍은 보상 계산 뒤).
- 도전과제 판정 시점은 드랍 뒤(변경 없음). 유물 관련 도전과제는 이번 범위 밖(§10).

---

## 3. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 콘텐츠·밸런스 수치 = JSON(`Assets/_Lair/Data/Json/`, 파일명 = `EData` 값명). 세이브 = `MetaProfile` 엔트리 리스트(`StageRecordEntry`/`DifficultyClearEntry` 선례). sentinel(-1/0/null) 미사용. 이름 규칙 `<부모>_<관계명>`.

### 3.1 엔티티 / 관계 트리

```
RelicRarity (엔티티)  "등급 하나"                       <- 3행 Common/Rare/Legendary
RelicStack  (엔티티)  "중첩 단계 하나의 효과 배율"       <- 5행 stack 1..5
RelicConfig (단일 설정 행)  전역 드랍 규칙 수치          <- 1행

Relic (엔티티)  "유물 하나"                             <- 16행
 |-- rarity ───────────────────────────► RelicRarity.rarity      (필수 1:1, 직접 필드)
 |
 +-- Relic_Effect (관계, 복합키 relicId+effectIndex)  "유물이 가진 효과들(1~2개)"
 |      +-- Relic_Effect_Param (관계, relicId+effectIndex+paramKey)  "효과 수치 하나"
 |
 +-- Relic_Drop (선택적 관계, 복합키 relicId+heroId)  "이 영웅이 이 유물을 드랍한다 + 가중치"
        heroId ───────────────────────► HeroArchetype.heroId = Chapter.heroId (EHero 값명)

Stage (기존)
 +-- Stage_Relic (관계, 복합키 chapterId+stageIndex)  "스테이지 기본 드랍 확률"       <- 25행

Difficulty (기존)
 +-- Difficulty_Relic (관계, difficulty)               "난이도 드랍 확률 배율"          <- 3행
      +-- Difficulty_Relic_Rarity (관계, difficulty+rarity)  "난이도 x 등급 가중 배율"  <- 9행

MetaProfile (세이브 루트)
 |-- Relics : List<RelicEntry>   { RelicId, Stack(>=1) }   <- 보유한 유물만 행 존재
 |-- RelicMissStreak : int       <- 필수 스칼라(0 = 연패 0 이라는 실제 값)
 +-- BattleSpeedOn : bool        <- 로컬 선호값(2배속 토글), 클라우드 복원 비대상
```

- 관계 판정: `Relic.rarity` 는 모든 유물이 정확히 하나를 가지는 **필수 1:1** 이라 직접 필드(§11-1-3). `Relic_Drop` 은 **선택적 관계**(드랍 안 하는 영웅 x 유물 조합은 행 자체가 없음, 가중치 0 으로 채우지 않음). `Relic_Effect` 는 유물이 1~2개 효과를 갖는 **그룹 내 여러 항목** 이라 복합키 `relicId + effectIndex`(§11-1-4). `Stage_Relic`/`Difficulty_Relic` 는 모든 스테이지/난이도에 항상 존재하는 필수 1:1 이지만 기존 `Stage.json`/`Difficulty.json` 스키마·테스트를 건드리지 않고 유물 기능을 독립 제거 가능하게 하려고 **별도 관계 데이터로 분리**했다(이름 규칙 §11-1-5).

### 3.2 필드 표

**RelicRow** - `Relic.json` (`EData.Relic`). 핵심 엔티티: "유물 하나".

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `relicId` | string | PK | `ERelic` 값명 = 아이콘 파일명 (예: `HeartOfStone`) |
| `category` | string | | `ERelicCategory` 값명 `Stat` / `Combat` / `Convenience` |
| `rarity` | string | 필수 1:1 → `RelicRarity.rarity` | `Common` / `Rare` / `Legendary` |
| `maxStack` | int | | 1~5 (`RelicStack` 최대 stack 이하) |
| `nameKey` | int | 필수 1:1 → Strings_Ko id | 1357~1372 |
| `descKey` | int | 필수 1:1 → Strings_Ko id | 1373~1388 (포맷 인자 `{0}`~ = 현재 중첩 효과값, §7.2) |

**Relic_EffectRow** - `Relic_Effect.json` (`EData.Relic_Effect`). 17행(전 유물 1개 + `KingsCrownShard` 2개).

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `relicId` | string | PK1, FK → Relic | |
| `effectIndex` | int | PK2 | 1부터 연속 |
| `effectKind` | string | | `ERelicEffectKind` 값명 |

**Relic_Effect_ParamRow** - `Relic_Effect_Param.json` (`EData.Relic_Effect_Param`). `Stage_Rule_Param` 선례(키-값).

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `relicId` | string | PK1 | |
| `effectIndex` | int | PK2 | |
| `paramKey` | string | PK3 | §3.3 표의 키 |
| `value` | float | | 중첩 1 기준 값 |

**Relic_DropRow** - `Relic_Drop.json` (`EData.Relic_Drop`). 75행.

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `relicId` | string | PK1, FK → Relic | |
| `heroId` | string | PK2, FK → `EHero` 값명 | 챕터 영웅 (`Chapter.heroId`) |
| `weight` | int | | 등급 안에서 상대 가중치, 1 이상 |

**RelicRarityRow** - `RelicRarity.json` (`EData.RelicRarity`). 3행.

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `rarity` | string | PK | `Common` / `Rare` / `Legendary` |
| `nameKey` | int | | 1341 / 1342 / 1343 |
| `baseWeight` | int | | 70 / 25 / 5 |
| `colorHex` | string | | 표시 색 `#9aa5bd` / `#5aa9ff` / `#b07cff` |

**RelicStackRow** - `RelicStack.json` (`EData.RelicStack`). 5행.

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `stack` | int | PK | 1~5 |
| `effectScale` | float | | 1.00 / 1.60 / 2.10 / 2.50 / 2.80 |

**RelicConfigRow** - `RelicConfig.json` (`EData.RelicConfig`, 단일 객체, `JsonUtility.FromJson` 사용).

| 필드 | 값 | 설명 |
|---|---|---|
| `lossChanceMul` | 0.25 | 패배 굴림 배율 |
| `pityPerMiss` | 0.10 | 연패 1회당 가산 |
| `pityMaxBonus` | 0.40 | 가산 상한 |
| `maxChance` | 0.95 | 최종 확률 상한 |

**Stage_RelicRow** - `Stage_Relic.json` (`EData.Stage_Relic`). 25행: `chapterId`(1~5) x `stageIndex`(1~5).

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `chapterId` | int | PK1, FK → Stage | |
| `stageIndex` | int | PK2, FK → Stage | |
| `dropChance` | float | | 0~1. stageIndex 1~4 = 0.30, stageIndex 5 = 0.60 (초기값) |

**Difficulty_RelicRow** - `Difficulty_Relic.json` (`EData.Difficulty_Relic`). 3행.

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `difficulty` | int | PK, FK → Difficulty | 0 / 1 / 2 |
| `dropChanceMul` | float | | 1.00 / 1.25 / 1.50 |

**Difficulty_Relic_RarityRow** - `Difficulty_Relic_Rarity.json` (`EData.Difficulty_Relic_Rarity`). 9행.

| 필드 | 타입 | 키 | 설명 |
|---|---|---|---|
| `difficulty` | int | PK1 | |
| `rarity` | string | PK2 | |
| `weightMul` | float | | §2.4 표의 값 |

**MetaProfile 신규 필드** (`Scripts/Meta/MetaProfile.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `Relics` | `List<RelicEntry>` | 빈 리스트 | 보유 유물. **클라우드 복원 대상** |
| `RelicMissStreak` | int | 0 | 승리 굴림 연속 미획득 수. 클라우드 복원 대상 |
| `BattleSpeedOn` | bool | false | 2배속 토글 마지막 상태. **로컬 선호값 - `CopyFrom` 이 복사하지 않음**(`DisplayName` 선례) |
| `Version` | int | 7 → **8** | 스키마 추가에 따른 승격 |

**RelicEntry** (`MetaProfile.cs` 안, `[Serializable]`, `DifficultyClearEntry` 옆)

| 필드 | 타입 | 키/관계 | 설명 |
|---|---|---|---|
| `RelicId` | string | PK | `Relic.relicId` |
| `Stack` | int | | 현재 중첩. 불변식 `>= 1` (미보유 유물은 행이 없다) |

> 명명 메모: `RelicEntry` 는 세이브 엔트리라 `StageRecordEntry`/`CardPickCountEntry` 선례를 따른다(§11-1-5 의 JSON 관계 명명 대상 아님).

### 3.3 효과 종류별 paramKey 와 적용 규칙

| `ERelicEffectKind` | paramKey (중첩 1 기준 값) | S(n) 적용 | 적용 방식 |
|---|---|---|---|
| `MonsterStat` | `statKind`(`EMonsterStatKind` 정수: Hp 0 / Power 1 / Cooldown 2 / MoveSpeed 4), `amount` | amount x S | 전종 `_typeModifiers[type].Multiply(statKind, 1 + amount x S(n))` |
| `SpawnerPeriod` | `amount` | amount x S | 모든 스포너 `ScalePeriod(1 + amount x S(n))` |
| `HeroBurn` | `hpRatioPerSec` | x S | 영웅 최대 HP x 비율 x S 를 1초마다 피해(최소 1) |
| `HeroLightning` | `intervalSec`, `hpRatio` | hpRatio x S | `intervalSec` 마다 최대 HP x hpRatio x S 피해 (첫 낙뢰 = 전투 시작 + intervalSec) |
| `HeroAttackDown` | `amount` | amount x S | 영웅 공격력 x (1 - amount x S) 영구 |
| `OpeningSummon` | `monster`(`EMonster` 정수 0=Wisp), `countPerStack` | count x n | 전투 시작 직후 영웅 근처에 소환 |
| `LowHpFrenzy` | `hpFraction`, `durationSec` | 없음(고정) | 영웅 HP 비율 <= hpFraction 최초 1회 `EMonsterBuff.Frenzy` durationSec |
| `BattleSpeed` | `speedScale` | 없음(고정) | 토글 on 시 게임 속도 speedScale |
| `SoulBonus` | `amount` | amount x S | 소울 보상 x (1 + amount x S), floor |
| `XpBonus` | `amount` | amount x S | 영주 XP 보상 x (1 + amount x S), floor |
| `LoseSoulBonus` | `amount` | amount x S | 패배 시 소울 보상 x (1 + amount x S), floor |
| `SpawnPrelude` | `advanceSec` | advanceSec x S | 모든 스포너 첫 스폰 지연 - advanceSec (0 하한) |

### 3.4 초기 데이터 (JSON 입력값)

**Relic_Effect_Param** (중첩 1 기준)

| relicId | effectIndex | effectKind | paramKey = value |
|---|---|---|---|
| HeartOfStone | 1 | MonsterStat | statKind = 0, amount = 0.03 |
| RustyBlade | 1 | MonsterStat | statKind = 1, amount = 0.02 |
| SwiftBoots | 1 | MonsterStat | statKind = 4, amount = 0.02 |
| BrokenHourglass | 1 | MonsterStat | statKind = 2, amount = -0.02 |
| BreedingCharm | 1 | SpawnerPeriod | amount = -0.03 |
| KingsCrownShard | 1 | MonsterStat | statKind = 0, amount = 0.05 |
| KingsCrownShard | 2 | MonsterStat | statKind = 1, amount = 0.05 |
| EmberBrand | 1 | HeroBurn | hpRatioPerSec = 0.0002 |
| AmbushHorn | 1 | OpeningSummon | monster = 0, countPerStack = 2 |
| ThunderShard | 1 | HeroLightning | intervalSec = 10, hpRatio = 0.003 |
| CursedBanner | 1 | HeroAttackDown | amount = 0.04 |
| WrathSpirit | 1 | LowHpFrenzy | hpFraction = 0.5, durationSec = 10 |
| HasteHourglass | 1 | BattleSpeed | speedScale = 2.0 |
| SoulPurse | 1 | SoulBonus | amount = 0.04 |
| LordsTome | 1 | XpBonus | amount = 0.04 |
| ConsolationFeather | 1 | LoseSoulBonus | amount = 0.25 |
| SpawnPrelude | 1 | SpawnPrelude | advanceSec = 3.0 |

**Relic_Drop 가중치 행렬**(`-` = 행 없음; 등급 안에서 상대 가중치. 영웅 = 챕터 영웅)

| ERelic | 등급 | Knight (Ch1) | Archer (Ch2) | Assassin (Ch3) | Berserker (Ch4) | Paladin (Ch5) |
|---|---|---|---|---|---|---|
| HeartOfStone | Common | 6 | 2 | 2 | 3 | 5 |
| RustyBlade | Common | 2 | 3 | 6 | 6 | 2 |
| SwiftBoots | Common | 2 | 6 | 5 | 2 | 2 |
| EmberBrand | Common | 3 | 3 | 2 | 6 | 3 |
| AmbushHorn | Common | 4 | 3 | 4 | 3 | 3 |
| SoulPurse | Common | 3 | 3 | 3 | 3 | 3 |
| LordsTome | Common | 3 | 3 | 3 | 3 | 4 |
| ConsolationFeather | Common | 2 | 2 | 2 | 2 | 2 |
| BrokenHourglass | Rare | 2 | 3 | 5 | 4 | 2 |
| BreedingCharm | Rare | 4 | 3 | 2 | 2 | 4 |
| ThunderShard | Rare | 3 | 5 | 2 | 2 | 3 |
| CursedBanner | Rare | 2 | 2 | 4 | 3 | - |
| HasteHourglass | Rare | 3 | 3 | 3 | 3 | 3 |
| SpawnPrelude | Rare | 3 | 2 | 2 | 2 | 3 |
| KingsCrownShard | Legendary | 5 | - | - | 5 | 5 |
| WrathSpirit | Legendary | - | 5 | 5 | 5 | - |

행 수 검산: Common 8x5 = 40 / Rare 5+5+5+4+5+5 = 29 / Legendary 3+3 = 6 → **75행**. 영웅 테마(가중치가 높은 유물): 기사 = 심장석(탱커) · 궁수 = 질풍의 장화/뇌운 · 암살자 = 녹슨 칼날/모래시계 · 광전사 = 잿불 낙인/녹슨 칼날 · 성기사 = 심장석/번식의 부적. 성기사는 챕터 5 패시브 "신앙의 가호"(디버프 저항, `chapter-5.md`)와의 서사 일관성을 위해 `CursedBanner`(공격력 디버프) 행을 두지 않는다.

**Stage_Relic**: `chapterId` 1~5 x `stageIndex` 1~4 = 0.30 (20행), `stageIndex` 5 = 0.60 (5행) → 25행.

### 3.5 로드 검증 (위반 시 로드 오류, `ChapterDatabase` 오류 수집 패턴 재사용)

1. `Relic.relicId` 가 `ERelic` 값명이고 중복 없음, `category`/`rarity` 가 정의된 값, `maxStack` 1~`RelicStack` 최대, `nameKey`/`descKey` 가 Strings_Ko 에 존재.
2. 모든 유물에 `Relic_Effect` 가 1개 이상(`effectIndex` 1부터 연속), 각 `effectKind` 의 필수 paramKey(§3.3)가 모두 존재하고 누락·중복 없음.
3. `Relic_Drop` 의 `(relicId, heroId)` 중복 없음, `heroId` 가 `Chapter.json` 에 존재, `weight >= 1`. **모든 유물은 최소 1개 영웅의 테이블에 존재**하고, **모든 영웅은 Common 후보를 1개 이상** 가진다(드랍 불가능 유물/영웅 방지).
4. `RelicRarity` 3행(Common/Rare/Legendary), `baseWeight >= 1`. `RelicStack` 의 `stack` 이 1부터 연속, `effectScale(1) = 1.0`, 단조 증가, 한계 증가분 비증가.
5. `Stage_Relic` 이 `Stage.json` 의 모든 (chapterId, stageIndex) 를 정확히 한 번 덮고 `0 <= dropChance <= 1`. `Difficulty_Relic` 이 `Difficulty` 의 모든 단계를 덮고 `dropChanceMul` 이 단조 비감소·1.0 시작. `Difficulty_Relic_Rarity` 가 (난이도 x 등급) 전부를 덮고, Easy 행은 전부 1.0, Common 은 난이도 증가에 비증가, Rare/Legendary 는 비감소.
6. `RelicConfig`: `0 <= lossChanceMul <= 1`, `pityPerMiss >= 0`, `pityMaxBonus >= 0`, `0 < maxChance <= 1`.
7. **로드 실패 시**: 오류 로그 후 "유물 기능 비활성" 상태로 동작 - 드랍 없음, 효과 미적용, 도감 버튼은 표시하되 팝업은 비어 있음, 전투·결과·클라우드는 정상. 보유 유물 데이터(`Relics`)는 삭제하지 않는다(데이터 복구 시 복원).

### 3.6 `ERelic` 이 JSON 에 없는 보유 id 를 만났을 때

세이브에 있는 `RelicId` 가 `Relic.json` 에 없으면(유물 삭제/개명): 효과 적용·도감 표시에서 **무시**하고, 세이브에서 지우지 않는다(되돌릴 때 복구). 보유 중첩이 `maxStack` 보다 크면 적용·표시는 `maxStack` 으로 클램프(읽는 순간 보정, 세이브 비수정). 마이그레이션은 데이터 미로드 상태에서도 안전해야 하므로 이 보정을 읽기 지점이 담당한다(`difficulty-select.md` §3.1 선례).

---

## 4. 마이그레이션 · 클라우드

### 4.1 `MetaProfile` Version 7 → 8

| 단계 | 규칙 (`MetaProfileMigration.Apply`, 멱등) |
|---|---|
| 1 | `CurrentVersion = 8`. `Version < 8` 이면 8 로 승격 |
| 2 | 기존 단계(난이도 포함) 유지 |
| 3 | `Relics` null 이면 빈 리스트 |
| 4 | `Relics` 정리: 행이 null / `RelicId` 가 빈 문자열 / `Stack < 1` 이면 제거. 같은 `RelicId` 중복이면 `Stack` 최댓값 한 행만 남김 |
| 5 | `RelicMissStreak < 0` 이면 0 으로 보정 |

- v7 세이브: 필드 부재 → `Relics` 빈 리스트, `RelicMissStreak` 0, `BattleSpeedOn` false 로 로드.
- 유물 id 존재 여부·maxStack 클램프는 마이그레이션에서 하지 않는다(§3.6 - 데이터 로드 순서 방어).
- 갱신 필요 테스트: `Tests/EditMode/Chapter/MetaProfileMigrationTests.cs` 버전 기대값 7 → 8.

### 4.2 클라우드 동기

- `MetaProfile.CopyFrom` 에 `Relics`(`?? new List<RelicEntry>()`), `RelicMissStreak` 추가 필수(빠뜨리면 복원 시 유물 전부 유실 - 유물은 수집 노력이 큰 자산이라 영향이 크다). `BattleSpeedOn` 은 복사하지 않는다.
- 업로드 `schemaVersion` 은 `profile.Version` 을 그대로 쓰므로 8 로 자동 승격. 복원 직후 `MetaProfileMigration.Apply` 재적용(기존 규칙).
- **충돌 판정은 `serverVersion` 타임스탬프 기반**(`CloudSaveConflict.IsConflict`)이라 유물 수는 판정 기준이 아니다 → 충돌 UX 변경 없음. 충돌 배지·복원 권유 화면에 유물 수를 보여주는 것은 이번 범위 밖(후속 제안).
- v7 문서를 v8 클라이언트가 복원하면 유물 없음(문서 기준 전체 대체라는 기존 동작). v8 문서를 v7 클라이언트가 복원하면 유물 필드는 무시된다(구버전, 알려진 제약 - 신구 클라이언트 동시 배포 전제). v7 클라이언트가 이후 업로드하면 서버 문서에서 유물이 사라진다는 점을 명시한다.
- 기기 이전: 익명 인증은 기기 이전을 풀지 못한다(CLAUDE.md §8). 구글 로그인 연동(Spec 2) 전까지 유물은 같은 기기/계정 한정으로 보존된다.

---

## 5. 편의 유물 특수 이슈 - 2배속 (`HasteHourglass`)

### 5.1 현재 코드 사실 (코드 확인)

- `PauseService` 가 `Time.timeScale` 의 유일 소유자다(`Pause` → 0, `Resume` → **1 고정**, `ResetAll` → 1, `ForcePause` → 0). 다른 `timeScale` 대입은 없다(Grep 확인).
- `BattleClock.Tick` 은 `BattleController.Update` 의 `Time.deltaTime`(스케일됨)로 전진한다. 영웅 오라(`HeroAuraRunner`), 스포너, 몬스터 버프, 영웅 스킬 러너, AI 모두 `Time.deltaTime` 기준이다. 카드 선택 팝업·종료 확인은 `Pause` 로 timeScale 0.
- 카메라 흔들림·UI 연출(`BattleCamera`, 토스트, 시너지 패널 등)은 `unscaledDeltaTime`.

### 5.2 문제: `Resume()` 이 1 로 덮어쓴다

배속 2 상태에서 카드 선택 팝업이 뜨면 `Pause()` → 0, 닫힐 때 `Resume()` 이 1 로 복원해 **배속이 풀린다**. 따라서 배속 유물의 선수정이 필요하다.

**요구(동작 명세, 구조는 gameplay-programmer 판단)**
1. 현재 "목표 게임 속도"(기본 1, 배속 on 이면 `speedScale`)를 하나의 소유자(PauseService 가 이미 timeScale 소유자이므로 그곳)가 보관한다.
2. `Resume()` 은 depth 0 이 되면 timeScale 을 **1 이 아니라 목표 게임 속도**로 복원한다.
3. 배속 변경은 depth 0 일 때만 즉시 timeScale 에 반영, 일시정지 중이면 보관만 하고 Resume 때 반영.
4. `ResetAll()`(씬 이탈) 은 timeScale 1 로 복원하고 목표 게임 속도도 1 로 되돌린다. 전투 씬 진입 시 `BattleSpeedOn` 이 켜져 있고 유물을 보유하면 다시 설정한다.
5. `ForcePause()`(전투 종료)는 변경 없음. 결과창 중에는 배속 해제 상태가 되며, 다시 도전 시 4번으로 복구.

### 5.3 제한 시간은 게임 시간 기준 (2배속이 유리하지 않다)

- `BattleClock` 이 `deltaTime` 기반이므로 **2배속에서는 시계도 2배로 흐른다.** 5분(300 게임초) 제한은 실시간 150초가 되지만 **게임 내 모든 진행(영웅·몬스터·스포너·시계·카드 임계)이 동일 비율로 빨라져 전투 결과 분포는 1배속과 같다.** 즉 2배속은 시간 절약일 뿐 전략적 이득이 없다. 별도 보정 코드를 넣지 않는다(시계 보정은 오히려 불공정해짐).
- `ClearTime`(랭킹·최단 기록)은 `_clock.Elapsed`(게임 시간)이므로 2배속 판도 1배속과 같은 척도의 기록이다 → 랭킹 제출 규칙 변경 없음(`difficulty-select.md` §1.6 Easy 한정 유지). 단, 2배속은 프레임당 시뮬레이션 간격(dt)이 커지므로 같은 시드의 결과가 1배속과 미세하게 다를 수 있다(공격 쿨다운 판정 프레임 양자화). 랭킹 공정성에 영향이 없다고 판단하나 **사용자 확인 항목 D-6** 으로 올린다.
- 점검 항목(gameplay-programmer): 전투 로직에서 `Time.timeScale` 비의존 대기(`Task.Delay`, `UniTask`, `unscaledDeltaTime` 사용) 중 전투 진행에 영향을 주는 곳이 있으면 2배속에서 어긋난다. 위 Grep 기준 전투 로직은 `deltaTime` 이지만 `async` 대기를 쓰는 연출(`EndBattle` 후 UI)은 영향 없음. 구현 시 한 번 점검 후 보고.

### 5.4 토글 UI

- **필요함.** 배속은 "항상 2배" 가 아니라 플레이어가 켜고 끄는 편의(연출을 보고 싶을 때 1배속).
- 위치: 전투 HUD 좌상단 뒤로가기 버튼(16,16 / 56x56) 오른쪽 (80,16) 56x56, 버튼 배열은 `battle-exit-confirm.html` 의 뒤로가기와 같은 톤. `HasteHourglass` 보유 시에만 표시(미보유 시 숨김 - 공간·인지 부하 없음).
- 형태: `Toggle`+`CHToggle`(Rule 03 §3). Off = "2배속" 라벨 회색, On = 금색 테두리 + 라벨 `#f7c64a`. 문구 1355 `2배속`.
- 상태는 `MetaProfile.BattleSpeedOn` 에 저장(Retry·다음 전투에서 유지). 카드 선택 중(pause)에도 눌러 바꿀 수 있고 `Resume` 때 반영(§5.2-3).
- 기본값 off. 유물을 처음 얻으면 off 로 시작(원치 않는 가속 방지).

---

## 6. 유물 효과 구현 접점 (코드 접점 - 구조는 gameplay-programmer 판단)

### 6.1 적용 타이밍 원칙

**전투 시작 시 보유 유물을 한 번 스냅샷해 일괄 적용**한다(`RelicBattleBonus` 가 `MetaBattleBonus.From(profile, cfg)` 와 같은 모양으로 `profile.Relics` + 유물 DB 를 집계). 전투 중 보유 유물은 변하지 않는다(드랍은 전투 종료 후). 클라우드 복원은 마을에서만 가능하므로 전투 중 변경 경로가 없다.

| 단계 | 호출 위치 | 처리 |
|---|---|---|
| 1 | `BattleController` 초기화에서 `ApplyMetaBonuses()`(~181행 호출) **바로 뒤** | `ApplyRelicBonuses()` - 능력치형 `MonsterStat` 을 `_typeModifiers` 에 곱연산 누적(상점과 동일 표면), `SpawnerPeriod`/`SpawnPrelude` 를 스포너 바인딩 이후 적용(`ScalePeriod` 와 첫 지연 단축). `_metaConfig` 가 null 이어도 유물 DB 가 있으면 적용(기존 `ApplyMetaBonuses` 의 `_metaConfig == null` 조기 반환과 독립) |
| 2 | 영웅 스폰 완료 후(`SpawnHero` 이후, `HeroAuraRunner` 준비 시점) | `HeroBurn`/`HeroLightning`/`HeroAttackDown` 을 `ApplyHeroAura(aura, -1f)` 로 부착(영구) |
| 3 | 스포너 바인딩 직후, 전투 개시 시점 | `OpeningSummon`: `SpawnMonster(EMonster, nearHero)` 를 `countPerStack x n` 번 호출 |
| 4 | 영웅 HP 변화 구독(기존 `_heroHealth.OnChanged`) | `LowHpFrenzy`: 비율 <= hpFraction 최초 1회 `AddMonsterBuff(EMonsterBuff.Frenzy, durationSec)` 후 해제 |
| 5 | `EndBattle` 소울 계산 직후 | 보상 가산(§6.4) |
| 6 | 전투 HUD 초기화 | `BattleSpeed` 보유 시 토글 표시, `BattleSpeedOn` 이면 `speedScale` 설정 |

### 6.2 신규 오라 2종 + 재사용 1종

| 유물 | 구현 | 비고 |
|---|---|---|
| `EmberBrand` | 신규 `IHeroAura` (`PoisonAura` 의 1초 누적 틱 패턴, 영역 판정·바닥 비주얼 없음). `hero.TakeDamage(Max(1, Round(최대HP x ratio x S)))` | `IDebuffAxisAura.IsDebuffAxis = true` - 챕터 5 성기사 "신앙의 가호" 대상(카드 DoT 와 동일 규칙). 피해 숫자 색 스탬프 신규 `HitFeedbackPalette.Ember` (`#ff8a3d`) |
| `ThunderShard` | 신규 `IHeroAura`. `intervalSec` 누적 → 낙뢰 FX + `TakeDamage` | 디버프 축이 아닌 순수 피해라 `IDebuffAxisAura` 마커 없음(성기사 저항 비대상). 피해 숫자 색 `HitFeedbackPalette.Lightning` (`#9fd8ff`) |
| `CursedBanner` | 기존 `HeroAttackDownAura(attacker, 1 - amount x S)` 재사용 | `IPenaltyScalable` → 성기사 저항 적용(카드와 동일). 영웅 HP바 아래 상태 아이콘이 HeroAttackDown 카드 아이콘으로 표시되는 점은 허용(원인 구분은 도감/결과로 충분) |

- 영웅 사망(승리) 후 오라 정리는 기존 `HeroAuraRunner` 의 `OnDetached` 흐름. 전투 종료 후 풀 반환 누수 없음(FX는 아래 풀링).
- 라이트닝/화상으로 영웅이 죽으면 정상 승리로 처리(`EndBattle(Win)`).
- `BossBulwark` 등 스테이지 규칙으로 영웅이 무적인 구간은 기존 `TakeDamage` 가 처리(유물 쪽 예외 없음).

### 6.3 능력치 유물과 상점의 곱연산 순서

`_typeModifiers` 는 곱연산 누적이라 순서가 결과에 영향이 없다. 상점(`ApplyMetaBonuses`) → 유물(`ApplyRelicBonuses`) → 전투 중 카드(`RegisterMonsterTypeBuff`) 로 누적. **Cooldown 축은 작을수록 빠름**(`StatMultiplier` 주석 기준)이므로 `BrokenHourglass` 의 음수 amount 가 정상이다. 곱 하한 가드: 최종 mul 은 0.5 미만으로 내려가지 않는다(설계 방어, 현재 수치로는 도달 불가).

### 6.4 보상 가산

`EndBattle` 에서 `SoulRewardCalculator.Calculate` 결과 `reward` 에 대해:
- 승리: `Souls = floor(Souls x (1 + SoulBonus합))`, `Xp = floor(Xp x (1 + XpBonus합))`.
- 패배: `Souls = floor(Souls x (1 + SoulBonus합 + LoseSoulBonus합))`(합산 후 1회 floor). 패배 XP 는 `XpBonus` 만 적용.
- 각 보너스 합 = 보유 유물의 `amount x S(n)` 합(같은 effectKind 가 여러 유물이면 합산; 현재는 종류당 유물 1개).
- 결과창 소울/XP 표기는 가산 후 최종값을 보여준다(별도 표시 줄 없음 - 요구 범위 밖).

### 6.5 FX · 사운드 · 풀링 프리웜

| 항목 | 요구 | 비고 |
|---|---|---|
| `EVisual.RelicLightningFx` (신규 프리팹) | 영웅 머리 위에서 내리꽂는 번개 + 피격 플래시. 수명 0.5초, `CHMPool` 대상, 자동 반환(`ReturnToPoolAfter`) | `ThunderShard` 전용. **사전 워밍 count 2**(동시 1발이지만 지연 겹침 여유). `Assets/_Lair/Art/FX/RelicLightningFx.prefab`, Addressables 주소 = 파일명 |
| `ThunderShard` 외 FX | 없음 | 화상은 피해 숫자 색(`Ember`)과 영웅 HP바만, 분노의 영혼은 기존 Frenzy 비주얼 재사용, 소환은 몬스터 스폰 연출 재사용 |
| 도감/결과창 아이콘 | `ERelic` 16종 스프라이트 64x64 도트 `Assets/_Lair/Art/Sprites/Relic/<ERelic 값명>.png` | 마을 아이콘(`/vicons/Icon_*.png`) 도트 톤 |
| 사운드 `EAudio.RelicAcquire` | 결과창 유물 획득 연출 1회. 등급 무관 동일 SFX 1회(전설 구분은 시각 연출로만) | 신규 mp3 1개 `RelicAcquire.mp3` |
| 사운드 `EAudio.RelicThunder` | 낙뢰 타격 시 1회 | 신규 mp3 1개 `RelicThunder.mp3`, 배속에서도 동일 재생 |
| 기존 hook | 도감 팝업 버튼은 `CHButton.ClickSoundHook` 자동 | 신규 hook 등록 없음 |

- 신규 `EAudio` 2개는 CHMSound 규약상 PlayOneShot 채널이 Init 에서 값마다 자동 생성된다(`CommonEnum.cs` 주석 기준). 에셋(mp3) 없이 값만 추가하면 로드 실패 로그가 나므로 **에셋 제작을 구현 완료 조건에 포함**한다(§11 D-8).
- 낙뢰 SFX 는 최대 10초 간격이라 동시 재생 폭주 없음.

### 6.6 범위 위험 요약

- 배속은 전투 전체 시간 척도를 바꾸므로 PauseService 변경이 전 UI 일시정지 흐름(카드 선택·종료 확인·결과)에 회귀를 만들 수 있다 → 구현 슬라이스 C 로 분리해 단독 테스트(§12).
- 전투 효과형은 영웅 피해량이 처치 시각에 비례해 커진다(§1.3). 초기값이 큰 편이라면 `RelicStack` 스케일부터 낮춘다.

---

## 7. UI

> 목업: `.mockups/relics.html` - 기존 `worldmap-redesign-v2` / `difficulty-select.html` / `village-hud-redesign.html` 프레임·색·버튼 스타일 재사용. 등급 색은 신규 3색(`#9aa5bd` / `#5aa9ff` / `#b07cff`)이며 난이도 색(`#5ef0b4` / `#f7c64a` / `#e5484d`)과 겹치지 않게 정했다(결과창에서 둘이 함께 보임).

### 7.1 마을 HUD 버튼

- `village-hud-redesign.html` 좌 메뉴 2열 x 3행(타일 112x112, 간격 16, x = 16 / 144, y = 104 / 232 / 360) 중 마지막 행 아래 **4행 1열 (16, 488)** 에 "유물" 타일 신규(112x112, 라벨 "유물" 문구 1335). 아이콘은 `Icon_Relic`(신규, 64x64 도트 보석 상자). 검산: 360+112 = 472 → 간격 16 → 488, 488+112 = 600 ≤ 하단 바 시작 632.
- 새 타일 우측(144, 488)은 비워 둔다(전적 패널·캐러셀과 충돌 없음).
- 미보유(0개) 상태에서도 표시. 처음에는 빨간 점(신규 유물 알림)을 쓰지 않는다(YAGNI, 후속).
- `VillageController.OpenMenu` 에 `case EUI.RelicPopup` 추가, `VillageHud` 버튼 배선.

### 7.2 유물 도감 팝업 (`EUI.RelicPopup`)

팝업 루트 1280x720, 패널 (72,40) 1136x640(기존 팝업 공통 프레임).

| 영역 | 좌표 (x, y) | 크기 | 내용 |
|---|---|---|---|
| 제목 | (96, 52) | 300x40 | "유물" (글자 30) |
| 수집 현황 | (282, 64) | 600x20 | "수집 {보유 종류} / {전체 종류}" 문구 1336. 오른쪽에 중첩 합 "{현재 중첩 합} / {전체 슬롯}" |
| 닫기 | (1144, 48) | 48x48 | 기존 X |
| 필터 열 | (88, 120) | 200x472 | 종류 4칩(전체/능력치/전투 효과/편의, 높이 40 간격 8) + 구분선 + 등급 4칩(전체/일반/희귀/전설) |
| 유물 그리드 | (304, 120) | 520x472 | 4열 x 4행 셀 124x108, 간격 8 → 4x124+3x8 = 520, 4x108+3x8 = 456 ≤ 472 |
| 상세 패널 | (840, 120) | 352x472 | 선택 유물 상세 |
| 하단 안내 | (96, 604) | 1096x56 | 문구 (획득 규칙 안내 - 기존 안내 문구 스타일) |

- **필터**: 종류 칩 4개(단일 선택) x 등급 칩 4개(단일 선택)는 AND. 결과 0개면 그리드 중앙에 문구 1356. 필터 변경 시 선택 해제.
- **그리드 셀**(124x108): 상단 아이콘 48x48(가운데), 아래 이름(글자 13, 1줄 말줄임), 우하단 "n/m" 중첩 표기(글자 12), 테두리 3px 등급 색. **미획득**: 아이콘 실루엣(밝기 0.15), 이름 `???`(문구 1344), 테두리 `#232834`. 획득 중첩이 최대인 셀은 "MAX" 금색 라벨 우상단. 셀 데이터는 `RelicCellData`(id, 보유 중첩, 아이콘).
- **스크롤**: 16종은 스크롤 없이 4행이지만 `CHPoolingScrollView<TItem,TData>` 3-class 구조(Rule 03 §3)로 구현해 유물 추가 시 스크롤된다.
- **상세 패널**: 아이콘 96x96 / 이름(글자 21) / 등급 칩(등급 색) + 종류 라벨 / 중첩 "중첩 {n} / {max}" 문구 1345 / "현재 효과" 문구 1351 + 현재 중첩 효과 설명(보유 시) / "다음 중첩" 문구 1352 + 다음 중첩 효과 설명(최대가 아닐 때) 대신 최대일 때는 "최대 중첩" 문구 1353 / "드랍 영웅: {영웅 이름들}" 문구 1350(미획득이어도 표시 - 수집 목표를 알려 파밍 동기 제공) / 미획득이면 설명 대신 문구 1354.
- **효과 설명 포맷**: `descKey` 문구의 `{0}`(필요 시 `{1}`)에 `amount x S(n)` 를 퍼센트 환산해 `{0:0.#}` 로 채운다. 미보유 시 중첩 1 값을 "다음 중첩" 으로 표시.
- View/ViewModel 분리: `RelicViewModel` 이 필터·정렬·셀 데이터·상세 텍스트를 계산, View 는 확정값 표시만(Rule 02 §6). 정렬은 JSON 순서(종류 묶음)로 고정.

### 7.3 결과창 (`ResultPopup`)

`ResultPopupArg` 신규 필드(sentinel 금지):

| 필드 | 타입 | 의미 |
|---|---|---|
| `HasRelicDrop` | bool | 이번 판에 유물 획득 여부 |
| `RelicId` | string | `HasRelicDrop` 가 true 일 때만 유효 |
| `RelicStackAfter` | int | 획득 후 중첩 |
| `RelicMaxStack` | int | 해당 유물 상한 |
| `RelicIsNew` | bool | 이 유물이 처음 얻은 것인가 |
| `ShowNextChance` | bool | 승리 + 미획득일 때 true |
| `NextWinChancePercent` | int | 다음 승리의 예상 확률(정수 %, 일반 규칙: stage x diff + 갱신된 pity, 소수 반올림) |

**패널 레이아웃 변경**(난이도 줄·해금 배너·유물 줄이 모두 나올 최악 경우 기준): 결과 패널 (380,120) 520x440 → **520x480**, 항목 줄 높이 40 → 36(피치 46 → 40), 버튼 y 500 → 528.

| 요소 | 좌표 (x, y) | 크기 | 비고 |
|---|---|---|---|
| 항목 줄 (최대 4: 클리어 시간/난이도/소울/영주 경험) | (404, 222 + 40 i) | 472x36 | 4줄이면 마지막 줄 끝 222+120+36 = 378 |
| 난이도 해금 배너 (기존) | (404, 386) | 472x44 | 끝 430 |
| **획득 유물 띠 (신규)** | (404, 438) | 472x64 | 끝 502. 아래 버튼 (404/646, 528) 48 높이 → 끝 576, 패널 끝 600 (여유 24) |
| 버튼 다시 도전 / 마을로 | (404, 528) / (646, 528) | 230x48 | 기존 폭 |

- **유물 띠 구성**: 좌측 아이콘 48x48(등급 색 테두리 3px), 가운데 2줄(상: "획득 유물" 문구 1346 작은 글자 12 + 등급 칩 / 하: 유물 이름 글자 18 등급 색), 우측: 신규면 "NEW" 배지 문구 1347(금색), 중복이면 "중첩 +1 ({n} / {max})" 문구 1348. 띠 바탕 `#0d1119`, 테두리 2px 등급 색, 전설은 `#b07cff` 글로우.
- **연출**: 보상 줄이 모두 나온 뒤 띠가 아래에서 위로 0.3초 슬라이드 인(`unscaledDeltaTime` 기준 - 결과 시점 timeScale 0) + 아이콘 팝(스케일 0.6 → 1.15 → 1.0, 0.25초). 전설은 띠 테두리 펄스 2회. `EAudio.RelicAcquire` 1회.
- **미획득**: 승리이고 `ShowNextChance` 면 같은 자리에 흐린 줄 "유물 없음 · 다음 승리 시 획득 확률 {n}%" 문구 1349(글자 13 `#8e98ad`). 패배 미획득은 줄 없음(패배 화면을 복잡하게 하지 않음). 후보 전부 보유로 굴리지 않은 경우에는 줄을 표시하지 않는다.
- Easy 첫 유물 보장 판은 띠에 "NEW" 가 뜨고 도감 안내 한 줄은 넣지 않는다(마을에서 버튼 발견).

### 7.4 전투 HUD 2배속 토글

§5.4. 목업의 전투 HUD 탭에서 위치 확인.

### 7.5 Strings_Ko 신규 id (현행 최대 id 1334 다음, 1335~1388)

UI 문구(1335~1356, 22개):

| id | 문구 | 용도 |
|---|---|---|
| 1335 | `유물` | HUD 타일 · 팝업 제목 |
| 1336 | `수집 {0} / {1}` | 수집 현황 |
| 1337 | `전체` | 필터 칩 (종류/등급 공용) |
| 1338 | `능력치` | 종류 `Stat` |
| 1339 | `전투 효과` | 종류 `Combat` |
| 1340 | `편의` | 종류 `Convenience` |
| 1341 | `일반` | 등급 Common (`RelicRarity.nameKey`) |
| 1342 | `희귀` | 등급 Rare |
| 1343 | `전설` | 등급 Legendary |
| 1344 | `???` | 미획득 이름 |
| 1345 | `중첩 {0} / {1}` | 상세/셀 |
| 1346 | `획득 유물` | 결과창 띠 라벨 |
| 1347 | `NEW` | 신규 배지 |
| 1348 | `중첩 +1 ({0} / {1})` | 중복 획득 표기 |
| 1349 | `유물 없음 · 다음 승리 시 획득 확률 {0}%` | 미획득 줄 |
| 1350 | `드랍 영웅: {0}` | 상세 패널 (영웅 이름은 기존 `HeroArchetype`/챕터 영웅 이름 문자열을 `, ` 로 이음) |
| 1351 | `현재 효과` | 상세 |
| 1352 | `다음 중첩` | 상세 |
| 1353 | `최대 중첩` | 상세 |
| 1354 | `아직 획득하지 못한 유물입니다` | 미획득 상세 |
| 1355 | `2배속` | 전투 HUD 토글 라벨 |
| 1356 | `해당 조건의 유물이 없습니다` | 필터 결과 0 |

유물 이름(1357~1372, `Relic.nameKey`): 1357 단단한 심장석 · 1358 녹슨 칼날 · 1359 질풍의 장화 · 1360 깨진 모래시계 · 1361 번식의 부적 · 1362 왕관 조각 · 1363 잿불 낙인 · 1364 매복의 뿔피리 · 1365 뇌운 조각 · 1366 저주의 깃발 · 1367 분노의 영혼 · 1368 가속 모래시계 · 1369 소울 주머니 · 1370 영주의 서 · 1371 위로의 깃털 · 1372 서두르는 둥지 (마스터 표 #1~#16 순서).

유물 설명(1373~1388, `Relic.descKey`, 포맷 인자 개수):

| id | 유물 | 문구 | 인자 |
|---|---|---|---|
| 1373 | HeartOfStone | `모든 몬스터의 HP +{0}%` | 1 |
| 1374 | RustyBlade | `모든 몬스터의 공격력 +{0}%` | 1 |
| 1375 | SwiftBoots | `모든 몬스터의 이동속도 +{0}%` | 1 |
| 1376 | BrokenHourglass | `모든 몬스터의 공격 대기시간 -{0}%` | 1 (값의 절댓값) |
| 1377 | BreedingCharm | `모든 스포너의 주기 -{0}%` | 1 (값의 절댓값) |
| 1378 | KingsCrownShard | `모든 몬스터의 HP와 공격력 +{0}%` | 1 |
| 1379 | EmberBrand | `영웅이 매초 최대 HP의 {0}%만큼 화상 피해를 입는다` | 1 |
| 1380 | AmbushHorn | `전투 시작 시 영웅 근처에 도깨비불 {0}마리가 나타난다` | 1 (정수) |
| 1381 | ThunderShard | `{0}초마다 영웅에게 번개가 떨어져 최대 HP의 {1}%만큼 피해를 입힌다` | 2 |
| 1382 | CursedBanner | `영웅의 공격력 -{0}%` | 1 |
| 1383 | WrathSpirit | `영웅 HP가 {0}% 이하가 되면 몬스터 전체가 {1}초간 광폭화한다 (전투당 1회)` | 2 (hpFraction x 100 정수, durationSec) |
| 1384 | HasteHourglass | `전투 중 {0}배속을 켜고 끌 수 있다. 제한 시간은 게임 시간 기준이다` | 1 (speedScale) |
| 1385 | SoulPurse | `전투 소울 보상 +{0}%` | 1 |
| 1386 | LordsTome | `영주 경험치 보상 +{0}%` | 1 |
| 1387 | ConsolationFeather | `패배 시 소울 보상 +{0}%` | 1 |
| 1388 | SpawnPrelude | `모든 스포너의 첫 스폰이 {0}초 빨라진다` | 1 |

(총 22 + 16 + 16 = 54개 id: 1335~1388 연속, 검산 1388-1335+1 = 54.) 기존 `Strings_Ko` 무결성 테스트(연속·중복·포맷 인자)가 새 id 를 포함해 통과해야 한다. 프리팹 정적 라벨(필터 칩, 결과창 "획득 유물", HUD 타일)은 CHText `_stringID` 로 연결한다(Rule 03 §3). 유물 이름/설명처럼 데이터가 채우는 문구는 코드가 `nameKey`/`descKey` 로 채운다.

---

## 8. 엣지 케이스 · 마이그레이션 · 구버전

| # | 케이스 | 처리 |
|---|---|---|
| E-1 | 유물 DB 로드 실패 / 검증 오류 | §3.5-7 기능 비활성, 보유 데이터 보존, 전투·결과·클라우드 정상 |
| E-2 | `Relics` 에 DB 에 없는 id | 효과 적용/표시 무시, 세이브 비수정(§3.6) |
| E-3 | 보유 중첩 > maxStack | 읽는 순간 클램프(적용·표시), 세이브 비수정 |
| E-4 | 후보 전부 보유(영웅 테이블 풀셋) | 굴림 생략, 미획득 처리, pity 불변, 결과창 줄 없음 |
| E-5 | v7 세이브 | 필드 부재 → 빈 유물, `Version 8` 승격(§4.1). 기존 유저 첫 승리는 유물 보장(§2.6) |
| E-6 | 클라우드 복원 후 | `Relics`/`RelicMissStreak` 복원, `Migration` 재적용, `BattleSpeedOn` 은 로컬 유지. 복원 후 마을 팝업을 열면 즉시 반영(VM 재바인딩) |
| E-7 | 전투 중 뒤로가기 종료 | `EndBattle` 비경유 → 드랍·pity 변화 없음 |
| E-8 | Retry | 새 전투 시작 시 유물 스냅샷을 다시 집계(직전 판 드랍 즉시 반영) |
| E-9 | 같은 판에서 `EndBattle` 중복 호출 | 기존 `_model.Result != None` 가드로 1회만 굴림 |
| E-10 | 난이도 보정 | `EffectiveDifficulty` 사용(전투 시작에 확정, 도중 변경 불가). Normal/Hard 의 최단·랭킹 제외 규칙은 드랍과 무관(유물은 모든 난이도에서 드랍) |
| E-11 | 도감 팝업에서 보유 변경 | 팝업을 연 동안 보유는 변하지 않음(전투 불가 상태). 닫고 다시 열면 재계산 |
| E-12 | 첫 승리 보장이 Legendary 를 뽑음 | 허용(확률적 행운). 등급 굴림은 평소대로 |
| E-13 | 2배속 on 상태에서 씬 이탈 | `ResetAll` 이 timeScale 1 복원(§5.2-4). 다음 전투 진입 시 재설정 |
| E-14 | 배속 중 카드 선택 팝업 | Pause → 0, Resume → 목표 속도(§5.2) |
| E-15 | AmbushHorn 소환이 글로벌 동시 몬스터 캡(18)과 충돌 | 기존 `SpawnMonster` 경로를 따름(캡 초과분은 기존 규칙대로). 구현 시 캡 동작 확인 후 보고 |
| E-16 | 성기사(Ch5) 디버프 저항 | `EmberBrand`/`CursedBanner` 는 저항 대상, `ThunderShard` 는 비대상(§6.2) |
| E-17 | 유물이 `StageRule` 의 몬스터 제한(봉인 둥지 등)과 충돌 | 규칙이 우선(스폰 종 잠금은 `OpeningSummon` 이 `Wisp` 를 소환하기 전에 규칙 판정 - 잠긴 종이면 소환 skip, 구현 시 확인 보고) |

---

## 9. qa-simulator 후속 (밸런스 일괄 튜닝 시 사용 - 이번 범위 밖, 게이트만 예약)

| # | 지표 | 통과 기준(초기 가설) | 실패 시 조치 |
|---|---|---|---|
| R1 | 52 슬롯 완성에 필요한 승리 횟수(스테이지 균등, 난이도 Easy/Normal/Hard 혼합 시뮬) | 80~150승 | 기준 밖이면 `Stage_Relic.dropChance`/pity 조정 |
| R2 | 유물 풀셋 vs 무유물 처치 시각 비(Easy/Normal/Hard 각각) | 풀셋이 무유물 대비 -25%~-45% 이내 | 초과 시 `RelicStack.effectScale` 먼저 하향 |
| R3 | 풀셋 + 상점 만렙에서 Hard 승률 하한 · 처치 시각 하한 | 풀셋 Hard 승률 90% 이하, 모든 (스테이지, 난이도) p10 처치 시각 >= 75초, 최소 >= 30초(난이도 D5 와 동일) | 초과 시 전투 효과 수치 인하 |
| R4 | 등급별 드랍 비율 vs §2.4 표 | 오차 ±2%p | 구현 버그 |
| R5 | 유물 개별 기여도(단일 유물 장착 시 처치 시각 변화) | 같은 등급 안 유물 간 편차 2배 이내 | 편차 큰 유물 수치 조정 |
| R6 | 소울 경제 영향(`SoulPurse` 풀셋 포함) | 전 항목 만렙 도달 런 수가 49런의 80% 이상 | `SoulBonus` 인하 |
| R7 | 2배속 on/off 승률·처치 시각 분포 | 차이 없음(승률 ±2%p, 처치 시각 평균 ±3초) | 있으면 §5.3 재검토 |

- **결정 메트릭**: 위 수치는 가설이며 최종값은 모든 콘텐츠 완성 뒤 일괄 시뮬 리포트로 확정한다("qa-simulator 검증 후 결정").
- 시뮬레이터가 유물 시스템을 쓰려면 유물 보유 상태(프로필)와 드랍 굴림을 시드 고정으로 재현할 수 있어야 한다 - 드랍 굴림은 난수원을 주입 가능한 순수 함수로(§12).

---

## 10. 이번 범위 밖 (후속 제안으로만)

유물 합성/강화/판매 · 유물 장착 슬롯 · 유물 세트 효과 · 유물 전용 도전과제 · 마을 도감 신규 알림 점 · 전투 HUD 보유 유물 요약 · 충돌 화면에 유물 수 표시 · 카드 선택 리롤 같은 편의 유물 · 3배속 이상 · 유물 드랍을 위한 ★ 목표/무한 탑/데일리 · 신규 몬스터/던전 편성과 연계된 유물. 모두 개별 승격 전까지 금지.

---

## 11. 사용자 확인 필요 결정 목록

| ID | 결정 | 대안 | 확인 이유 |
|---|---|---|---|
| D-1 | 승리 = 정상 확률, 패배 = x0.25 (§2.1) | 승리만 / 승패 동일 | 구제 기능과 파밍 유인의 균형 |
| D-2 | 중복 = 체감 감소 스택(`RelicStack`), 최대 52슬롯 (§2.3) | 중복 무시 / 소울 환급 | 풀셋 압력, 장기 목표 |
| D-3 | 최대 중첩 유물은 후보 제외, 소울 환급 없음 (§2.3) | 환급 | 소울 경제 개입 여부 |
| D-4 | 소프트 pity (+0.10/연패, 최대 +0.40) (§2.5) | 없음 / 하드 pity | 체감 불운 대 복잡도 |
| D-5 | 보유 0개일 때 첫 승리 100% 드랍 (§2.6) | 일반 확률 | 발견성 |
| D-6 | 2배속은 게임 시간 기준(제한 시간 동일 비율 가속), 랭킹 제출 규칙 불변 (§5.3) | 2배속 판 랭킹 제외 | 공정성 판단 |
| D-7 | 성기사 디버프 저항이 `EmberBrand`/`CursedBanner` 에 적용, `ThunderShard` 는 비대상 (§6.2) | 전부 비대상 / 전부 대상 | 챕터 5 서사와 수집 가치 |
| D-8 | 아이콘 16종 + `Icon_Relic` + `RelicLightningFx` 프리팹 + SFX 2종 에셋 제작이 구현 완료 조건 | 임시 에셋 후 교체 | 에셋 제작 분업/비용 |
| D-9 | 풀셋 환산 압력 +28% (능력치) + 전투 효과 9~24% (§1.3)를 출발점으로 두고 일괄 튜닝 | 더 보수적 초기값 | 튜닝 전 체감 |
| D-10 | `BattleSpeedOn` 로컬 전용(클라우드 복원 비대상) (§3.2) | 복원 대상 | 기기별 선호 |
| D-11 | 신규 JSON 10개(Relic·Relic_Effect·Relic_Effect_Param·Relic_Drop·RelicRarity·RelicStack·RelicConfig·Stage_Relic·Difficulty_Relic·Difficulty_Relic_Rarity) 구조 (§3) | `Stage_Relic`/`Difficulty_Relic*` 를 기존 Stage/Difficulty 에 필드로 병합(파일 7개로 감소) | 데이터 구조 승인 게이트. 병합은 §11-1-3 필수 1:1 이라 규칙상 허용이나 기존 JSON 스키마·테스트 변경이 생김 |
| D-12 | 도감 정렬은 JSON 순서(종류 묶음) 고정, 신규 알림 점 없음 | 최근 획득순 + 알림 | YAGNI |

---

## 12. 구현 요청사항 (gameplay-programmer)

### 12.1 슬라이스 분할 제안 (독립 구현·테스트 단위)

| 슬라이스 | 범위 | 선행 |
|---|---|---|
| A. 기반 + 수집 | 데이터 10개 + `RelicDatabase` 로더/검증, `MetaProfile` v8 / 마이그레이션 / `CopyFrom`, 드랍 굴림 순수 함수, EndBattle 연동, 결과창 띠, 도감 팝업·HUD 버튼, 능력치형·소울/XP/패배 보상·소환·`SpawnPrelude` 적용 | 승인 |
| B. 전투 효과 | `EmberBrand`/`ThunderShard`/`CursedBanner`/`WrathSpirit` 효과, FX/SFX | A |
| C. 편의 배속 | `PauseService` 배속 보관·복원, 전투 HUD 토글, `HasteHourglass` | A |

### 12.2 Enum (`CommonEnum.cs`, 전부 기존 값 뒤에 append)

- `ERelic`: `HeartOfStone`, `RustyBlade`, `SwiftBoots`, `BrokenHourglass`, `BreedingCharm`, `KingsCrownShard`, `EmberBrand`, `AmbushHorn`, `ThunderShard`, `CursedBanner`, `WrathSpirit`, `HasteHourglass`, `SoulPurse`, `LordsTome`, `ConsolationFeather`, `SpawnPrelude` (값명 = 아이콘 파일명 = `Relic.relicId`)
- `ERelicCategory`: `Stat`, `Combat`, `Convenience`
- `ERelicRarity`: `Common`, `Rare`, `Legendary`
- `ERelicEffectKind`: `MonsterStat`, `SpawnerPeriod`, `HeroBurn`, `HeroLightning`, `HeroAttackDown`, `OpeningSummon`, `LowHpFrenzy`, `BattleSpeed`, `SoulBonus`, `XpBonus`, `LoseSoulBonus`, `SpawnPrelude`
- `EUI`: `RelicPopup` (맨 끝 append, 프리팹 `RelicPopup.prefab`)
- `EData`: `Relic`, `Relic_Effect`, `Relic_Effect_Param`, `Relic_Drop`, `RelicRarity`, `RelicStack`, `RelicConfig`, `Stage_Relic`, `Difficulty_Relic`, `Difficulty_Relic_Rarity` (맨 끝 append, 값명 = JSON 파일명)
- `EVisual`: `RelicLightningFx` (맨 끝 append)
- `EAudio`: `RelicAcquire`, `RelicThunder` (맨 끝 append)

### 12.3 Interface

- 신규 공용 Interface 없음(순수 정적/데이터 클래스). 신규 오라 2종은 기존 `IHeroAura`(+`EmberBrand` 는 `IDebuffAxisAura`) 구현.

### 12.4 에셋 키 · 파일

- JSON 10개: `Assets/_Lair/Data/Json/<EData 값명>.json`, Addressables 주소 = 파일명(`Difficulty.json` 등록 방식과 동일).
- UI 프리팹: `Assets/_Lair/Art/UI/RelicPopup.prefab`, 셀 `RelicCell.prefab`(+`RelicPoolingScrollView`), `ResultPopup.prefab` 유물 띠 추가(패널 높이 변경 포함), `VillageHud.prefab` 유물 타일 추가, `BattleHud.prefab` 배속 토글 추가. 신규 `[SerializeField]` 는 하나도 비우지 않는다(프리팹 연결 검사 테스트).
- 스프라이트 17개: `Assets/_Lair/Art/Sprites/Relic/<ERelic 값명>.png` x16 + 마을 타일 아이콘 `Icon_Relic`(기존 마을 아이콘 폴더/키 규칙 따름).
- FX: `Assets/_Lair/Art/FX/RelicLightningFx.prefab`. 사운드 2개: `RelicAcquire`, `RelicThunder` (`EAudio` 값명 = 파일명).
- UI 배선(UnityMCP/빌더)은 **목업 `.mockups/relics.html` 승인 이후**에만 진행(Rule 00 UI 게이트). 데이터 구조는 §3 승인 이후.

### 12.5 코드 접점

- `Scripts/Meta/MetaProfile.cs`: `Relics`, `RelicMissStreak`, `BattleSpeedOn`, `RelicEntry`, 조회/추가 헬퍼(`GetRelicStack`, `AddRelic` - 상한은 호출부가 판정), `Version = 8`, `CopyFrom` 갱신(`BattleSpeedOn` 제외).
- `Scripts/Meta/MetaProfileMigration.cs`: §4.1, `CurrentVersion = 8`.
- 신규 `Scripts/Meta/Relic*`: 로더/검증(`RelicDatabase`), 전투 스냅샷 집계(`RelicBattleBonus` - `MetaBattleBonus` 형태), 드랍 굴림 순수 함수(난수원 주입 - 단위테스트·시뮬레이터 재현), 보상 가산 계산.
- `Scripts/Battle/BattleController.cs`: `ApplyMetaBonuses`(~1026) 옆 `ApplyRelicBonuses`, 영웅 오라 부착, 소환, 프렌지 트리거, `EndBattle`(~1323) 보상 가산·드랍 굴림 연동, `ResultPopupArg` 필드 채움(~1423).
- `Scripts/Battle/PauseService.cs`: §5.2 배속 보관·복원.
- `Scripts/Card/Auras/`: 신규 `EmberBrandAura`, `ThunderShardAura` (`PoisonAura` 패턴).
- `Scripts/UI/ResultPopup.cs`: 유물 띠 + 패널 레이아웃(`ResultRowsData` 에 확정값 계산).
- `Scripts/UI/Village/RelicPopup.cs`·`RelicViewModel`·`RelicCell`·`RelicPoolingScrollView`, `VillageHud.cs`/`VillageController.cs`(`OpenMenu` case).
- `Scripts/UI/BattleHud*`: 배속 토글(`CHToggle`).
- 확인 항목(구현 후 보고): ① `Spawner._firstSpawnDelay` 단축 API(없으면 추가), ② `SpawnMonster` 의 동시 캡(18) 동작, ③ `HeroAuraRunner` 성기사 저항(`ConfigureDebuffResist`)과 `EmberBrandAura` 연동, ④ 전투 로직 중 `timeScale` 비의존 대기 유무.

### 12.6 테스트 항목 (test-engineer)

**EditMode**

| 영역 | 항목 |
|---|---|
| 데이터 로드 | 출하 JSON 10개 로드, 행 수(16/17/75/3/5/1/25/3/9), 무결성 검증 §3.5 각 위반이 로드 오류, 모든 유물이 최소 1영웅 테이블에 존재, 모든 영웅 Common 후보 존재 |
| 중첩 계산 | `S(n)` 조회, `MonsterStat` mul = 1+amount·S, Cooldown 음수 amount, `countPerStack` 선형, 상한 클램프, 곱 하한 0.5 |
| 보상 가산 | 승리 소울/XP floor, 패배 `SoulBonus+LoseSoulBonus` 합산 후 1회 floor, 가산 전 스냅샷 기준(이번 판 드랍 미반영) |
| 드랍 굴림 | 확률 공식(승/패, pity 상한, `maxChance` 절단, 첫 유물 보장), 후보 제외(최대 중첩), 후보 0이면 미획득+pity 불변, 등급 굴림 가중(난수 주입 경계값), 영웅 테이블 반영, 등급 후보 없음 시 재정규화, 획득 시 pity 0, 패배 굴림이 pity 를 읽지/쓰지 않음, 시드 고정 재현성 |
| MetaProfile | v7 JSON → v8(필드 기본값), 마이그레이션 정리 규칙(null/빈 id/Stack<1/중복 병합/음수 pity), 멱등, `CopyFrom`(Relics·pity 복사, `BattleSpeedOn` 비복사, null → 빈 리스트), 복원 후 Migration 재적용 |
| 읽기 방어 | DB 에 없는 id 무시·세이브 비수정, 중첩 > maxStack 클램프 |
| PauseService | 배속 2 에서 Pause → Resume 후 timeScale 2, 배속 변경이 정지 중이면 Resume 때 반영, `ResetAll` 이 1 복원, `ForcePause`, 중첩 Pause 깊이, 기존 PauseService 테스트 무수정 통과 |
| 오라 | `EmberBrandAura` 틱 누적/최소 1 피해/성기사 저항 마커, `ThunderShardAura` 간격·첫 낙뢰 시각, `CursedBanner` factor 계산 |
| 문자열 | `Strings_Ko` 1335~1388 존재·연속, 포맷 인자 개수 일치, 기존 무결성 테스트 통과 |
| 프리팹 | `RelicPopup`/`RelicCell`/`ResultPopup` 유물 띠/`VillageHud` 타일/`BattleHud` 토글의 신규 `[SerializeField]` 미연결 0 |

**PlayMode (수동 확인)**: 마을에서 유물 버튼 → 빈 도감 → 첫 승리 후 결과창 유물 띠(NEW) → 마을 도감에서 보유 확인 → 같은 유물 재획득(중첩 +1) → Hard 재도전 시 드랍 등급 상승 체감 → 능력치 유물 전투 적용(몬스터 HP 등) → 번개 FX/SFX → 2배속 토글, 카드 선택 후에도 배속 유지, 결과창 이후 정상 → 앱 재시작 후 보유 유지 → 클라우드 복원 후 유물 유지.

---

## 13. CLAUDE.md §8 승격 문구 초안 (사용자 승인 후 메인이 반영)

§8 의 챕터 구조 허용 항목 끝 문장

> **★ 목표, 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.**

을 다음으로 교체한다.

> **★ 목표, 던전 편성·신규 몬스터 6종, 무한 탑·데일리는 개별 승격 전까지 금지.**

그리고 "난이도 선택(Easy / Normal / Hard) 1차 허용" 항목 뒤에 다음 항목을 추가한다. 동시에 그 난이도 항목의 문구 "**유물은 난이도 1차 완료 후 2차로 개별 승격 예정이며 그때까지 금지**(난이도가 유물 드랍 확률/등급에 쓰이는 것은 2차 범위)" 를 "유물 2차는 별도 항목으로 허용(아래)" 로 갱신한다.

> - **유물 2차 허용 (2026-10-19 v0.3 승격) - 단 전투 종료 시 드랍되는 수집형 유물 16종과 마을 도감 팝업에 한정** - 영웅(챕터)·스테이지·난이도별 확률로 유물을 드랍하고(승리 시 정상 확률, 패배 시 1/4), 보유 유물은 슬롯 제한 없이 전투 시작 시 전부 자동 적용한다. 유물 종류는 ① 영구 능력치(몬스터 스탯 배율·스포너 주기, 기존 `StatMultiplier` 축) ② 전투 효과(영웅에게 번개·화상 등 `IHeroAura`, 소환, 광폭화) ③ 편의(소울·XP 보상 가산, 2배속, 첫 스폰 단축)이며, 같은 유물의 중복 획득은 최대 중첩까지 체감 감소 스케일로 중첩된다. 허용 범위는 ① 유물·드랍·중첩 JSON 데이터(`Relic*`, `Stage_Relic`, `Difficulty_Relic*`) ② `MetaProfile` 의 보유 유물·연패 카운터 저장(Version 8)·클라우드 동기 호환 ③ 마을 HUD 유물 버튼과 `RelicPopup` 도감, 결과창 획득 유물 줄 ④ 전투 HUD 2배속 토글과 `PauseService` 배속 보관. **유물 합성/강화/판매/장착 슬롯/세트 효과, ★ 목표, 던전 편성, 신규 몬스터, 무한 탑, 데일리는 계속 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지(도감은 마을 팝업). 밸런스 수치는 콘텐츠 완성 후 qa-simulator 일괄 튜닝 대상이다.

컨셉서(`project_lair_concept.md`) §11 범위 표에는 "유물 2차(수집형 유물 16종, 마을 도감) - v0.3 승격 2026-10-19" 한 줄을 추가한다. `chapter-system.md`, `chapter-5.md`, `chapter-3-hero.md`, `chapter-4.md`, `difficulty-select.md` 의 "유물 금지" 문구는 당시 승격 기록이라 수정하지 않는다.

---

## 14. Self-Review

- Placeholder 잔존: 0건 (모든 미확정 수치는 "qa-simulator 일괄 튜닝" 과 결정 메트릭(§9)으로 명시).
- 내부 일관성: 유물 16종, 슬롯 합 52, 드랍 행 75, 효과 행 17, 스트링 54개(1335~1388), 확률표가 §2/§3/§12 에서 동일.
- 명명 일관성: `RelicStack`, `Relic_Effect_Param`, `RelicMissStreak`, `BattleSpeedOn`, `EVisual.RelicLightningFx`, `EUI.RelicPopup` 를 본문 전체에서 동일 표기.
