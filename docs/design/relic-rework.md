# 유물 시스템 개편 (장착 4칸 · 분해 · 제작) - 기능 기획서

> 작성: game-designer · 날짜: 2026-11-23 · 단계: v0.3 · 전제: 유물 2차(`relics.md`)·몬스터 육성 재화(`monster-growth-currencies.md` §2.4 유물 잔해)·데일리·탑 구현 완료
> 입력: `relics.md`(현행 규칙) · `monster-growth-currencies.md` §2.4/§8.3(잔해·교환) · 코드 `RelicDropRoller` / `RelicBattleBonus` / `RelicDatabase` / `RelicText` / `RelicViewModel` / `RelicPopup` / `ShardExchangeViewModel` / `CurrencySettlement` / `MetaProfile`·`MetaProfileMigration` / `BattleController`(유물 구간) / `MonsterBuffService` / `ResultPopup` / `DailyChallenge` · `.art-incoming/rl-set/APPLY_PLAN.md`(RelicPopup 현행 배치)
> **사용자용 가이드·UI 목업**: `relic-rework.html` (장착/제작 도감 화면 목업 포함)
> 사용자와 확정한 개편 내용(중첩 제거 · 자동 분해 · 제작 · 장착 4칸 · 16종 고정값)은 변경하지 않는다. 이 문서는 그 위에서 구현 가능한 수치·데이터·이행·UI 를 확정한다.

---

## § 헤더

- **목표**: 유물을 "쌓아서 키우는 52슬롯 수집" 에서 **"16종을 모으고(제작 포함) 4칸에 골라 끼우는 빌드"** 로 바꾼다. 중복은 조각이 되어 원하는 유물을 직접 제작하는 재료가 된다.
- **검증 가설**: (1) 중복이 "또 나온 허탈감" 이 아니라 "제작 재화" 로 읽히는가. (2) 4칸 제한이 공격 빌드(전설 2종)·안정 빌드·경제 빌드의 선택을 만들고, 풀셋 압력(+48~68%)을 4칸으로 묶어도 Hard 가 도전적으로 남는가. (3) 신규 효과 2종(개전 버프·잃은 HP 비례 강화)이 "전투 초반 가속 / 영웅을 몰아붙일수록 강해지는 역전" 이라는 서로 다른 재미를 주는가.
- **현재 단계 범위 적합성**: **범위 밖 항목의 명시 승격 요청 - 사용자 승인 필요.** `CLAUDE.md` §8 "유물 2차 허용" 은 "유물 합성/강화/판매(보유 유물을 재화로 바꾸기)/장착 슬롯" 을 계속 금지한다. 본 개편은 ① 장착 슬롯 ② 중복 유물의 조각 분해(재화 변환) ③ 조각으로 유물 제작(합성에 가까움)을 포함해 충돌한다. 승격 문안은 §9. **승인 전에는 구현에 착수하지 않는다.** ★ 목표 · 신규 몬스터 · 신규 스테이지 규칙 · 강화/세트 효과 · 판매 · 합성(유물끼리 합치기) · 유료 재화는 계속 금지.
- **핵심 메커니즘**: (a) 같은 유물을 또 얻으면 즉시 조각(일반 1 / 희귀 3 / 전설 8)으로 분해된다. (b) 조각으로 **안 가진** 유물을 제작한다(일반 10 / 희귀 18 / 전설 32). (c) 마을 도감에서 4칸에 장착한 유물만 전투에 적용되며 효과는 고정값이다(중첩 폐기).

---

## 1. 현행 → 개편 요약

| 항목 | 현행(`relics.md`) | 개편 |
|---|---|---|
| 중복 | 중첩 +1, 최대 `maxStack`(1~5), 효과 = 기본값 x `RelicStack.effectScale` | **중첩 없음.** 중복 = 즉시 분해 → 조각. 효과는 고정값 |
| 최대 중첩 | 후보 제외, 등급 전부 최대면 잔해(`monster-growth-currencies` §2.4 C) | 폐기. 영웅 드랍 테이블 전체가 항상 후보. 이미 가진 유물이 뽑히면 분해 |
| 잔해 | 등급 최대 중첩 시만 발생, 각성석·인장·결정으로 교환 | **유물 조각**으로 개명·통합(`ECurrency.RelicShard` 값 유지). 중복마다 발생, 제작 전용(교환 폐기 §8) |
| 적용 | 보유 전부 자동 적용(슬롯 없음) | **장착 4칸**, 장착한 유물만 적용. 변경은 마을 유물 도감에서만 |
| 획득 경로 | 드랍만 | 드랍 + 조각 제작 |
| 신규 효과 | - | `OpeningBuff`(개전 버프) · `LostHpEmpower`(잃은 HP 비례 강화). `OpeningSummon` · `LowHpFrenzy` 폐기 |
| 데이터 | `RelicStack.json` · `Relic.maxStack` | 폐기. `RelicRarity_Shard.craftCost`·`RelicConfig.equipSlotCount` 추가 |
| 세이브 | `Relics`(Stack) · `RelicMissStreak` (v8) | `EquippedRelics` · `RelicReworkDone` · `RelicReworkNoticePending` 추가, Version 14 → **15** |

---

## 2. 규칙 확정

### 2.1 보유 · 분해 · 제작

- **보유는 0/1 이다.** 같은 유물을 "여러 개 가질 수 있다" 는 의미는 "여러 번 얻을 수 있다" 이고, 두 번째부터는 저장 전에 분해되므로 저장되는 보유 수는 유물당 최대 1이다. `RelicEntry.Stack` 필드는 구세이브 호환(구클라이언트·구클라우드 문서)을 위해 **이름·타입을 유지**하고 항상 1 로 쓴다(§6).
- **분해량**(`RelicRarity_Shard.shardAmount`, 기존 값 그대로): 일반 **1** / 희귀 **3** / 전설 **8**. 분해는 드랍 굴림이 "이미 가진 유물" 을 뽑은 순간 `profile.AddCurrency(ECurrency.RelicShard, amount)` 로 확정한다(기존 `TotalEarned` 가산 규칙 그대로).
- **제작 비용**(`RelicRarity_Shard.craftCost`, 신규 필드): 일반 **10** / 희귀 **18** / 전설 **32**.

  검산(등급별 필요 중복 개수, 사용자 확정값과 일치): 일반 10 ÷ 1 = **10개분**, 희귀 18 ÷ 3 = **6개분**, 전설 32 ÷ 8 = **4개분**.
- **제작 조건**: 유물 DB 정상 · 그 유물이 DB 에 있음 · **미보유** · 조각 ≥ 비용. 보유 유물은 제작할 수 없다(제작하면 곧바로 분해되는 무의미한 경로 차단 — 버튼이 아예 제작 상태가 되지 않는다).
- **제작 결과**: 조각 비용 차감(`TrySpendCurrency`) → `Relics` 에 행 추가(Stack 1) → 빈 칸이 있으면 **가장 낮은 번호의 빈 칸에 자동 장착**(§2.2) → 즉시 `Save`. `RelicMissStreak` 은 건드리지 않는다.
- **판매·환급 없음**: 보유 유물을 조각으로 되돌리는 경로는 없다. 장착 해제는 되돌릴 수 있는 상태 변경일 뿐 조각을 주지 않는다.

### 2.2 장착 4칸

- 칸 수 `RelicConfig.equipSlotCount` = **4**, 칸 번호 0~3, 등급·종류 제한 없음(같은 유물 2칸 장착 불가).
- **전투 시작에 장착된 유물만 적용**한다(`RelicBattleBonus.From` 과 `FromDaily` 의 개인 유물 반쪽 모두 `EquippedRelics` 만 읽는다). 전투 중 장착 변경 경로는 없다.
- **장착 변경은 마을 유물 도감 팝업에서만** 한다. 예외 1건: 새로 얻은(드랍·제작) 유물은 **빈 칸이 있으면 가장 낮은 번호 빈 칸에 자동 장착**된다(드랍은 결과창에서 일어나므로 도감 밖). 빈 칸이 없으면 장착하지 않고 결과창에 안내한다. 자동 장착이 없으면 보장된 첫 유물이 아무 효과도 없어 발견성이 깨진다(권장 안, 반대 안은 사용자 결정 U-1).
- **도감 조작 상태표**(선택 유물 = 상세 패널에 표시 중인 유물, 슬롯 탭 = 하단 장착 칸 탭):

| 선택 유물 상태 | 상세 패널 주 버튼 | 장착 칸 탭 |
|---|---|---|
| 미보유, 조각 ≥ 비용 | `제작 · 조각 {비용}` (눌리면 확인 팝업 → 제작) | 칸의 유물을 선택 |
| 미보유, 조각 < 비용 | 비활성 `제작 · 조각 {비용}` + 부족 문구 | 칸의 유물을 선택 |
| 보유·미장착, 빈 칸 있음 | `장착` (가장 낮은 빈 칸에 장착) | 탭한 칸이 비어 있으면 그 칸에 장착, 차 있으면 **그 칸의 유물과 교체**(교체된 유물은 미장착으로 돌아감) |
| 보유·미장착, 빈 칸 없음 | 비활성 `장착` + 문구 "장착 칸이 가득 찼습니다. 바꿀 칸을 누르세요" | 탭한 칸의 유물과 **교체** |
| 보유·장착 중 | `장착 해제` | 탭한 칸의 유물을 선택(상세 전환). 선택 유물이 장착 중이면 칸 탭은 선택만 한다 |
| 선택 없음 | 버튼 숨김 | 칸의 유물을 선택(빈 칸이면 아무 일 없음) |

  모드가 없는 상태표다(교체 모드·취소 동작 없음). 슬롯 탭의 "교체" 는 선택 유물이 보유·미장착일 때만 발동한다.
- 장착 상태는 즉시 `Save`(`MetaSession.Store`)한다. 도감을 닫을 때 확정하는 별도 단계는 없다.

### 2.3 드랍 로직 변화

확률 단계(`ComputeChance`: stage x diff + pity, 첫 유물 보장, 패배 x0.25, `maxChance`)는 **변경 없음**. 바뀌는 것은 "굴림이 성공한 뒤" 이다.

```
성공한 굴림:
  table = 영웅 드랍 테이블 전체 (Relic_Drop, DB 에 존재하는 유물)      <- 최대 중첩 후보 제외 폐지
  table 이 비었으면 NoCandidates (굴리지 않음, 연패 카운터 불변)
  rarity  = PickRarity(table)     (변경 없음: baseWeight x 난이도 weightMul, 난수 1회)
  picked  = PickInPool(rarity 후보 전체, weight 비례)   (변경 없음, 난수 1회)
  RelicMissStreak = 0                                     (U-18: 어떤 획득이든 초기화)
  if 미보유(picked):  Relics 에 추가 → Acquired, 빈 칸 있으면 자동 장착
  else:               조각 += shardAmount(rarity) → Dissolved (유물은 변하지 않음)
```

| 상황 | 현행 | 개편 |
|---|---|---|
| 뽑힌 유물이 미보유 | 중첩 1 로 획득 | 획득 + (빈 칸 있으면) 자동 장착 |
| 뽑힌 유물이 보유 중 | 중첩 +1 (상한 전까지) | **분해**: 그 등급 조각 지급, 결과창 "분해" 띠 |
| 영웅 테이블 전부 보유 | 등급 단위 잔해 변환(후보 제외 규칙의 보정) | 별도 처리 불필요 — 매 성공이 곧 분해. E-4(굴림 생략)는 계속 폐지 상태 |
| `ERelicDropOutcome` | NoCandidates / Missed / Acquired / Shard | NoCandidates / Missed / Acquired / **Dissolved**(`Shard` 개명) |
| 난수 소비 | 등급 1 + 유물 1 | 동일(등급 1 + 유물 1). 시드 재현 테스트의 난수 순서는 유지된다 |
| 데일리 확정 굴림(`RollGuaranteed`) | 확률 생략, 등급→유물, 최대 중첩이면 잔해 | 확률 생략, 등급→유물, **보유 중이면 분해**. `RelicMissStreak` 은 읽지도 쓰지도 않음(불변) |
| 첫 유물 보장(보유 0개) | chance 1.0 | 동일. 보유 0개이므로 첫 성공은 항상 Acquired(+자동 장착) |

- 난수 순서가 같고 후보 집합만 커지므로, **같은 시드라도 이후 결과는 현행과 달라진다**(최대 중첩 제외 폐지). 시드 고정 회귀 테스트의 기대값은 갱신 대상이다.
- 패배 굴림도 같은 규칙(확률만 x0.25). 패배에서 얻은 유물도 자동 장착·분해 동일.
- `OwnsAnyKnownRelic`(첫 유물 보장 판정)은 `Relics`(보유 목록)를 읽는다 — 장착 여부와 무관.

### 2.4 조각 경제 검산 (qa-simulator 일괄 튜닝 전제의 출발값)

분해가 일어나는 드랍 1건당 기대 조각(등급 확률 = `relics.md` §2.4 표):

| 난이도 | 일반 / 희귀 / 전설 확률 | 산식 | 기대 조각 |
|---|---|---|---|
| Easy | 70 / 25 / 5 % | 0.70x1 + 0.25x3 + 0.05x8 = 0.70 + 0.75 + 0.40 | **1.85** |
| Normal | 56 / 35 / 10 (합 101) | (56x1 + 35x3 + 10x8) / 101 = 241 / 101 | **2.39** |
| Hard | 42 / 45 / 20 (합 107) | (42x1 + 45x3 + 20x8) / 107 = 337 / 107 | **3.15** |

- 16종 전부 제작할 때의 총 비용 = 8x10 + 6x18 + 2x32 = 80 + 108 + 64 = **252 조각**.
- 전설 1종 제작(32 조각)에 필요한 분해 드랍 수(Easy) = 32 / 1.85 = 17.3건. 일반 스테이지 Easy 의 드랍 1건당 기대 승리 수 2.45(`relics.md` §2.5)를 곱하면 **42.4승**(모든 드랍이 분해라는 상한 속도 가정 — 초반 드랍은 신규 유물이라 조각이 안 나온다. 실측은 게이트 R1').
- 위는 설계 출발값이며 최종 수치는 qa-simulator 일괄 튜닝 대상이다(§11.2).

---

## 3. 유물 16종 (고정값, 4칸 장착)

> 값은 모두 고정이다. `RelicStack` 배율·`maxStack` 은 폐기. 최종 수치는 **qa-simulator 일괄 튜닝 대상**이며 아래는 JSON 초기값이다. ID(`ERelic` 값명 = 아이콘 파일명 = `Relic.relicId`)는 **변경하지 않는다**(세이브 키·Addressables 주소). 이름·설명 문구만 바뀐다(`AmbushHorn` = 표시명 "개전의 뿔피리", `WrathSpirit` = 표시명 "분노의 영혼").

| # | ERelic | 표시명 | 종류 | 등급 | 효과(고정값) | 효과 종류 | 현행(참고: 중첩1 / 중첩3) |
|---|---|---|---|---|---|---|---|
| 1 | `HeartOfStone` | 단단한 심장석 | Stat | 일반 | 몬스터 HP **+6%** | `MonsterStat`(Hp) | 3% / 6.3% |
| 2 | `RustyBlade` | 녹슨 칼날 | Stat | 일반 | 몬스터 공격력 **+5%** | `MonsterStat`(Power) | 2% / 4.2% |
| 3 | `SwiftBoots` | 질풍의 장화 | Stat | 일반 | 몬스터 이동속도 **+5%** | `MonsterStat`(MoveSpeed) | 2% / 4.2% |
| 4 | `EmberBrand` | 잿불 낙인 | Combat | 일반 | 영웅이 매초 최대 HP의 **0.05%** 화상 | `HeroBurn` | 0.02% / 0.042% |
| 5 | `AmbushHorn` | **개전의 뿔피리** | Combat | 일반 | 전투 시작 후 **15초** 동안 모든 몬스터 공격력 **+15%** | **`OpeningBuff`(신규)** | 도깨비불 2마리 소환 / 6마리 |
| 6 | `SoulPurse` | 소울 주머니 | Convenience | 일반 | 전투 소울 보상 **+10%** | `SoulBonus` | 4% / 8.4% |
| 7 | `LordsTome` | 영주의 서 | Convenience | 일반 | 영주 경험치 보상 **+10%** | `XpBonus` | 4% / 8.4% |
| 8 | `ConsolationFeather` | 위로의 깃털 | Convenience | 일반 | 패배 시 소울 보상 **+50%** | `LoseSoulBonus` | 25% / 52.5% |
| 9 | `BrokenHourglass` | 깨진 모래시계 | Stat | 희귀 | 몬스터 공격 대기시간 **-5%** | `MonsterStat`(Cooldown) | -2% / -4.2% |
| 10 | `BreedingCharm` | 번식의 부적 | Stat | 희귀 | 스포너 주기 **-7%** | `SpawnerPeriod` | -3% / -6.3% |
| 11 | `ThunderShard` | 뇌운 조각 | Combat | 희귀 | **10초**마다 번개, 영웅 최대 HP의 **0.6%** | `HeroLightning` | 0.3% / 0.63% |
| 12 | `CursedBanner` | 저주의 깃발 | Combat | 희귀 | 영웅 공격력 **-8%** | `HeroAttackDown` | -4% / -8.4% |
| 13 | `HasteHourglass` | 가속 모래시계 | Convenience | 희귀 | 전투 중 **2배속** 토글 사용 가능(유지) | `BattleSpeed` | 동일 |
| 14 | `SpawnPrelude` | 서두르는 둥지 | Convenience | 희귀 | 모든 스포너 첫 스폰 **4초** 단축(0초 하한) | `SpawnPrelude` | 3초 |
| 15 | `KingsCrownShard` | 왕관 조각 | Stat | 전설 | 몬스터 HP·공격력 각 **+10%** | `MonsterStat` x2 | 5% |
| 16 | `WrathSpirit` | 분노의 영혼 | Combat | 전설 | 영웅이 **잃은 HP 10%마다** 모든 몬스터 공격력·이동속도 **+4%** (HP 0% 에서 최대 **+40%**, 상시) | **`LostHpEmpower`(신규)** x2 | 영웅 HP 50% 이하 시 10초 광폭화 1회 |

- 등급 구성 8 / 6 / 2 = 16종, 종류 구성 Stat 6 · Combat 5 · Convenience 5 (현행과 동일). 도감 정렬은 JSON 순서 유지(종류 묶음, 위 표 순서가 아니라 기존 `Relic.json` 행 순서 — §5.3 에 그대로 유지한다고 명시).
- **2배속(`HasteHourglass`)은 이제 장착 칸을 쓴다.** 전투 HUD 2배속 토글은 "장착 중일 때만" 표시된다(구 "보유 시"). 편의 유물 3종(소울·영주·깃털)과 2배속은 전투력 유물과 칸을 다투는 것이 4칸 설계의 핵심 선택이다.
- 현행 중첩 1 대비 개편값은 대체로 현행 **중첩 3**(x2.1)과 비슷하거나 약간 높게 잡았다(HP 6% = 6.3% 근방, 소울 10% = 8.4% 위). 중첩 5 최대(x2.8)까지 쌓던 유물은 풀셋 압력이 4칸으로 묶이며 낮아진다(§3.2).

### 3.1 빌드 압력 추정 (`relics.md` §1.3 환산 모델 재사용, 단정 아님)

환산 가중치: 공격력·공속 1.0 / HP 0.4 / 스폰 0.5 / 이동 0.3. `LostHpEmpower` 는 영웅 HP 가 시간에 따라 선형으로 줄어든다는 가정으로 시간 평균 단계 4.5(0~9 단계 균등)로 근사해 **평균 +18%**(= 4.5 x 4%)를 쓴다(실측은 게이트 R2').

| 빌드(4칸) | 구성 | 환산 산식 | 환산 압력 |
|---|---|---|---|
| A 공격 극대 | `KingsCrownShard` + `WrathSpirit` + `RustyBlade` + `BrokenHourglass` | 공격 1.10x1.05x1.18 = 1.363 · 공속 1/0.95 = 1.0526 · HP (1+0.10x0.4) = 1.04 · 이동 (1+0.18x0.3) = 1.054 → 1.363x1.0526x1.04x1.054 | **x1.57** |
| B 안정 | `KingsCrownShard` + `HeartOfStone` + `BreedingCharm` + `SwiftBoots` | 공격 1.10 · HP (1.04)x(1+0.06x0.4 = 1.024) = 1.065 · 스폰 (1+(1/0.93-1)x0.5) = 1.0376 · 이동 (1+0.05x0.3) = 1.015 → 1.10x1.065x1.0376x1.015 | **x1.23** |
| C 경제 | `KingsCrownShard` + `SoulPurse` + `LordsTome` + `HasteHourglass` | 공격 1.10 · HP 1.04 → 1.144, 소울 +10% · 경험치 +10% · 2배속 | **x1.14** + 보상 |

- 비교 기준: 현행 풀셋 = 능력치 x1.283 + 전투 효과(`relics.md` §1.3) → 총 x1.48(처치 76초 가정) ~ x1.68(200초 가정). **빌드 A x1.57 은 현행 풀셋 범위 안**이지만 4칸 + 전설 2종(제작 64 조각 혹은 드랍)만으로 도달하므로 수집 난이도가 낮다. 튜닝 1순위 레버는 `LostHpEmpower.amountPerStep`(0.04), 2순위는 `KingsCrownShard` 의 amount(0.10).
- 빌드 A 의 단계 상한 x1.40(HP 0%)은 영웅이 죽는 순간이라 실전 도달 가치가 낮다. 실전 최고 단계는 9단계(+36%)다.

---

## 4. 신규 효과 2종과 적용 파이프 연결점

### 4.1 `OpeningBuff` — 개전 버프 (`AmbushHorn`)

| 항목 | 값 |
|---|---|
| 파라미터 | `durationSec` 15 · `statKind` 1(`EMonsterStatKind.Power`) · `amount` 0.15 |
| 시작 시점 | `BeginRelicCombat`(시계 시작 = 영웅이 중심에 도달한 "전투 시작"). 전투 시작 전 대기·영웅 입장 구간에는 흐르지 않는다 |
| 시간 기준 | 게임 시간(`Time.deltaTime` 누적). 2배속이면 실시간 7.5초에 끝난다. 카드 선택 팝업·정지 중(timeScale 0)에는 흐르지 않는다 |
| 대상 | **전투 중 살아 있는 모든 몬스터**, 창 안에 새로 스폰된 몬스터 포함(전역 오버레이) |
| 합성 | 몬스터 `PowerScale` 에 `x (1 + amount)` 곱연산. 다른 `PowerScale` 곱(카드 버프 `BerserkPower` x3 · 특성 시한 배율 · 광폭 등)과 곱셈으로 합쳐진다 |
| 종료 | 15초 경과 시 소멸. 재발동 없음 |

### 4.2 `LostHpEmpower` — 잃은 HP 비례 강화 (`WrathSpirit`)

| 항목 | 값 |
|---|---|
| 효과 행 | `effectIndex` 1 = Power(`statKind` 1), `effectIndex` 2 = MoveSpeed(`statKind` 4) — 파라미터 동일 |
| 파라미터 | `statKind` · `stepFraction` 0.10 · `amountPerStep` 0.04 |
| 단계 수 | `steps = clamp((maxHp - currentHp) x 10 / maxHp 의 정수 몫, 0, 10)`. 10 은 `round(1 / stepFraction)` 에서 유도(JSON 에 `maxSteps` 를 따로 두지 않는다). 정수 연산이라 float 경계 오차가 없다(HP 90/100 → steps 1) |
| 배율 | `1 + amountPerStep x steps` → HP 0% 에서 steps 10 = **x1.40**, 영웅 HP 100% = x1.00 |
| 시간 | 상시. 영웅 현재 HP 를 **양방향**으로 따른다(회복하면 단계가 내려간다). 영웅이 회복하는 카드/스킬이 있으면 그대로 반영되며 최대 HP 가 바뀌면 같은 정수식으로 재계산 |
| 갱신 | `_heroHealth.OnChanged`(`AttachRelicFrenzy` 가 이미 쓰던 구독 지점)에서 steps 를 재계산해 오버레이 값을 갱신 |
| 대상·합성 | `OpeningBuff` 와 동일(전체 살아 있는 몬스터 + 새 스폰, `PowerScale`/`SpeedScale` 곱연산) |

### 4.3 기존 적용 파이프와의 연결점 (코드 조사 결과)

| 기존 파이프 | 사실(코드 확인) | 개편에서의 사용 |
|---|---|---|
| `_typeModifiers`(`StatMultiplier`) — `ApplyRelicBonuses` | 전투 시작 1회, 종별 Hp·Power·Cooldown·MoveSpeed 곱연산 누적. 되돌리기·시한 제거 수단 없음. 값 변경 시 몬스터 스탯 재적용(`ApplyMonsterStats`)이 필요 | **정적 유물**(`MonsterStat` · `SpawnerPeriod` · `SpawnPrelude`)은 그대로 이 표면. 신규 2종은 **쓰지 않는다**(시한 만료·HP 변화마다 재적용·HP 리셋 부작용이 생김) |
| `MonsterBuffService.Tick` | 매 tick 모든 몬스터의 `MeleeAttacker.PowerScale`·`CooldownScale`, `SimpleMover.SpeedScale`, `Health` 오버레이를 **1 로 리셋한 뒤** 활성 `EMonsterBuff` 를 곱한다. 곱 크기는 `switch` 에 하드코딩 | **신규 2종의 적용 지점.** 리셋 "뒤", 활성 버프 곱 "뒤" 에 유물 오버레이 값(`PowerMul`·`SpeedMul`)을 곱한다 — 여기 밖에서 `PowerScale`/`SpeedScale` 을 쓰면 다음 tick 에 1 로 덮인다. 값이 JSON 파라미터라서 하드코딩 `EMonsterBuff` 항목을 늘리지 않고 서비스가 **값을 받는 전용 입력**(유물 오버레이 setter 2개: 공격력 배율·이동 배율)을 갖는다 |
| `_traits.Tick`(`MonsterTraitService`) | `_monsterBuffs.Tick` **뒤**에 `PowerScale`·`SpeedScale` 에 곱연산(시한 특성) | 같은 방식으로 리셋 뒤 곱. 호출 순서(`Update` 416~421행)를 유지하고 유물 오버레이는 `_monsterBuffs.Tick` 안에서 곱해 특성과 독립 곱이 된다 |
| `IHeroAura`·`HeroAuraRunner.Attach(aura, -1f)` | 영웅 대상 영구 오라. 화상·낙뢰·공격력 감소가 이미 사용 | **변경 없음**: `EmberBrand`·`ThunderShard`·`CursedBanner` 는 값만 새 JSON 값. 신규 2종은 몬스터 측 효과라 오라가 아니다 |
| `BeginRelicCombat` | 시계 시작 1회 호출, `AttachRelicHeroAuras` · `SummonRelicOpeners` · `AttachRelicFrenzy` | `SummonRelicOpeners`·`AttachRelicFrenzy`·`HandleRelicFrenzyHp`·`DetachRelicFrenzy` 제거. 대신 `OpeningBuff` 타이머 시작과 `LostHpEmpower` 구독 부착 |
| 공격 구현 | `IAttacker` 구현체는 `MeleeAttacker` 하나뿐(6종 전부 근접 컴포넌트) | "몬스터 공격력" = `MeleeAttacker.PowerScale` 하나. 별도 원거리·DoT 몬스터 경로 없음 — 추가 대응 불필요 |
| `RelicBattleBonus` | 전투 시작 스냅샷 집계, `ScaledParam` | `Accumulate` 에 두 종류 추가(`OpeningBuffSpec`: durationSec/statKind/amount, `LostHpEmpowerSpec`: statKind/stepFraction/amountPerStep). 중첩 파라미터 스케일 제거 |
| 몬스터 스폰 캡·봉인 | `OpeningSummon` 이 쓰던 `IsSpeciesSpawnerLocked` 경로 | 소환 폐기로 불필요(`IsSpeciesSpawnerLocked` 는 `SummonRelicOpeners` 전용이면 함께 제거, 다른 호출자가 있으면 유지 — 구현 시 Grep 확인) |

- 결과적으로 **전투 중 변하는 몬스터 측 유물 효과는 `MonsterBuffService` 오버레이로, 전투 시작에 고정되는 효과는 `_typeModifiers`/스포너로** 나뉜다. 스탯 곱 순서는 결과에 영향이 없다(전부 곱연산).
- 곱 하한 가드(`RelicBattleBonus.MulFloor` 0.5 · `AttackFactorFloor` 0.1)는 유지한다. 오버레이는 1 이상만 가능해 하한이 필요 없다.
- `EMonsterBuff.Frenzy` 는 카드 효과(`FrenzyEffect`)가 계속 쓰므로 enum 값은 유지한다(유물 사용처만 제거).

---

## 5. 데이터 구조 (사용자 승인 게이트 - 구조표)

> Rule 02 §11-1 적용. 콘텐츠·밸런스 수치 = JSON. 세이브 = `MetaProfile` 엔트리 리스트. sentinel 값(-1/0/null)을 쓰지 않는다. 이번 변경은 **기존 스키마를 줄이거나 필드 1~2개를 더하는 것**이며 신규 JSON 파일은 없다.

### 5.1 엔티티 / 관계 트리 (변경 표시)

```
RelicRarity (엔티티)  "등급 하나"                              3행  (변경 없음)
 +-- RelicRarity_Shard (필수 1:1 관계, 키 rarity)  "등급의 조각 경제"   3행  [필드 추가: craftCost]
RelicConfig (단일 설정 행)                                      1행  [필드 추가: equipSlotCount]
RelicStack (엔티티)                                             5행  [폐기 - 파일·행·로더·EData 값 삭제]

Relic (엔티티)  "유물 하나"                                    16행  [필드 삭제: maxStack]
 |-- rarity ─────────────► RelicRarity.rarity   (필수 1:1, 직접 필드, 변경 없음)
 +-- Relic_Effect (복합키 relicId+effectIndex)                 17→18행 [effectKind 값 교체]
 |     +-- Relic_Effect_Param (복합키 +paramKey)               파라미터 값·키 변경
 +-- Relic_Drop (선택적 관계, relicId+heroId)                  75행  (변경 없음)

Stage_Relic / Difficulty_Relic / Difficulty_Relic_Rarity        (변경 없음)

Currency (엔티티)  RelicShard 행의 nameKey 문구만 "유물 조각"  (변경 없음, 문구)
Currency_Exchange (관계)  RelicShard → 3재화                    3행  [폐기 §8]

MetaProfile (세이브 루트, Version 14 → 15)
 |-- Relics : List<RelicEntry>              { RelicId, Stack(항상 1, 레거시 필드명 유지) }
 |-- EquippedRelics : List<EquippedRelicEntry>   { Slot(0~3), RelicId }   <- 신규. 찬 칸만 행 존재
 |-- RelicReworkDone : bool                 <- 신규. 이행 1회 가드
 |-- RelicReworkNoticePending : bool        <- 신규. 로컬 전용(CopyFrom 비복사)
 |-- RelicMissStreak : int                  (변경 없음)
 |-- Currencies[ RelicShard ]               (기존 CurrencyEntry, 조각 잔액·누적 획득)
 +-- BattleSpeedOn : bool                   (변경 없음, 로컬 선호값)
```

관계 판정(§11-1): `EquippedRelics` 는 "차 있는 칸만 행이 존재하는 선택적 관계" 라 빈 칸을 -1/null 로 채우지 않고 행 자체를 두지 않는다. `(Slot)` 이 칸 내 유일 키, `RelicId` 는 `Relics` 에 있어야 하는 FK. `craftCost` 는 모든 등급에 항상 존재하는 필수 1:1 이라 `RelicRarity_Shard` 의 직접 필드다(관계 데이터를 더 쪼개지 않는다).

### 5.2 필드 표

**RelicRarity_ShardRow** - `RelicRarity_Shard.json` (기존 `EData.RelicRarity_Shard`, 3행)

| 필드 | 타입 | 키/관계 | 값 | 비고 |
|---|---|---|---|---|
| `rarity` | string | PK, FK → `RelicRarity.rarity` | Common / Rare / Legendary | 기존 |
| `shardAmount` | int | | **1 / 3 / 8** | 기존 값 유지. 의미: 분해 시 지급량(구: 잔해 변환량) |
| `craftCost` | int | | **10 / 18 / 32** | **신규**. 제작 비용 |

검증: 3행 모두 존재 · `shardAmount >= 1` · `craftCost >= 1` · `craftCost > shardAmount`(제작이 분해 1건보다 비쌈) · 등급이 높을수록 `shardAmount`/`craftCost` 비감소.

**RelicConfigRow** - `RelicConfig.json` (단일 객체)

| 필드 | 값 | 비고 |
|---|---|---|
| `lossChanceMul` | 0.25 | 기존 |
| `pityPerMiss` | 0.1 | 기존 |
| `pityMaxBonus` | 0.4 | 기존 |
| `maxChance` | 0.95 | 기존 |
| `equipSlotCount` | **4** | **신규**. 검증: 1 이상 `Relic` 행 수 이하 |

**RelicRow** - `Relic.json` : `maxStack` 필드를 **삭제**한다(나머지 `relicId` · `category` · `rarity` · `nameKey` · `descKey` 유지). 검증 §3.5-1 의 maxStack 항목 삭제.

**Relic_EffectRow** - `Relic_Effect.json` (18행): `AmbushHorn` effectKind `OpeningSummon` → `OpeningBuff`, `WrathSpirit` 1행 → 2행(effectIndex 1·2, 둘 다 `LostHpEmpower`). 행 수 17 → **18** (검산: 17 - 1(WrathSpirit 구 행) + 2).

**Relic_Effect_ParamRow** - `Relic_Effect_Param.json` (최종값, 전 행)

| relicId | effectIndex | effectKind | paramKey = value |
|---|---|---|---|
| HeartOfStone | 1 | MonsterStat | statKind = 0, amount = 0.06 |
| RustyBlade | 1 | MonsterStat | statKind = 1, amount = 0.05 |
| SwiftBoots | 1 | MonsterStat | statKind = 4, amount = 0.05 |
| BrokenHourglass | 1 | MonsterStat | statKind = 2, amount = -0.05 |
| BreedingCharm | 1 | SpawnerPeriod | amount = -0.07 |
| KingsCrownShard | 1 | MonsterStat | statKind = 0, amount = 0.10 |
| KingsCrownShard | 2 | MonsterStat | statKind = 1, amount = 0.10 |
| EmberBrand | 1 | HeroBurn | hpRatioPerSec = 0.0005 |
| AmbushHorn | 1 | **OpeningBuff** | durationSec = 15, statKind = 1, amount = 0.15 |
| ThunderShard | 1 | HeroLightning | intervalSec = 10, hpRatio = 0.006 |
| CursedBanner | 1 | HeroAttackDown | amount = 0.08 |
| WrathSpirit | 1 | **LostHpEmpower** | statKind = 1, stepFraction = 0.10, amountPerStep = 0.04 |
| WrathSpirit | 2 | **LostHpEmpower** | statKind = 4, stepFraction = 0.10, amountPerStep = 0.04 |
| HasteHourglass | 1 | BattleSpeed | speedScale = 2.0 |
| SoulPurse | 1 | SoulBonus | amount = 0.10 |
| LordsTome | 1 | XpBonus | amount = 0.10 |
| ConsolationFeather | 1 | LoseSoulBonus | amount = 0.50 |
| SpawnPrelude | 1 | SpawnPrelude | advanceSec = 4.0 |

행 수 검산(파라미터 행): HeartOfStone 2 + RustyBlade 2 + SwiftBoots 2 + BrokenHourglass 2 + BreedingCharm 1 + KingsCrownShard 4 + EmberBrand 1 + AmbushHorn 3 + ThunderShard 2 + CursedBanner 1 + WrathSpirit 6 + HasteHourglass 1 + SoulPurse 1 + LordsTome 1 + ConsolationFeather 1 + SpawnPrelude 1 = **31행**.

**효과 종류별 paramKey**(변경점만, 나머지는 `relics.md` §3.3 에서 `S(n)` 적용 열을 삭제한 것과 같다 — 모든 값이 고정값이고 `RelicDatabase.ScaledParam` 은 `Get` 으로 대체):

| `ERelicEffectKind` | paramKey | 검증 |
|---|---|---|
| `OpeningBuff` (신규) | `durationSec` > 0 · `statKind` ∈ {1 Power, 4 MoveSpeed} · `amount` > 0 | 필수 키 3개 누락·중복 오류 |
| `LostHpEmpower` (신규) | `statKind` ∈ {1, 4} · `stepFraction` ∈ (0, 0.5] 이고 `1/stepFraction` 이 1e-4 이내 정수 · `amountPerStep` > 0 | 필수 키 3개 |
| `OpeningSummon` · `LowHpFrenzy` | **폐기**(enum 값·`RequiredParams` 항목·`RelicText` case·적용 코드 삭제). JSON 에 남아 있으면 `ERelicEffectKind` 에 없음 오류 | 로드 오류 |
| 그 외 10종 | 기존 필수 키 유지, `countPerStack`·`monster`·`hpFraction` 키는 사라진다 | |

**삭제 목록**: `RelicStack.json`(+`RelicStackRow` · `EData.RelicStack` · Addressables 항목) · `Relic.maxStack` · `RelicDatabase.AddStacks`/`EffectScale`/`MaxScaleStack`/`ScaledKeys`/`CountPerStackKey` · `RelicDropRoller` 의 최대 중첩 후보 제외 · `RelicDropResult.StackAfter`/`MaxStack` · `ResultPopupArg.RelicStackAfter`/`RelicMaxStack` · `RelicViewModel` 의 중첩 계산(`OwnedStackSum` · `TotalStackSlots` 등) · `RelicText` 의 중첩 인자.

### 5.3 도감 정렬

`Relic.json` 행 순서(현행 그대로: HeartOfStone, RustyBlade, SwiftBoots, BrokenHourglass, BreedingCharm, KingsCrownShard, EmberBrand, AmbushHorn, ThunderShard, CursedBanner, WrathSpirit, HasteHourglass, SoulPurse, LordsTome, ConsolationFeather, SpawnPrelude). 이행 시 자동 장착 순서의 마지막 동률 키도 이 순서다(§6.2).

### 5.4 로드 검증 변화

`relics.md` §3.5 에서 ① `maxStack`/`RelicStack` 항목(1·4)을 삭제 ② 효과 종류 검증 표를 §5.2 로 교체 ③ `RelicRarity_Shard`(§5.2)·`RelicConfig.equipSlotCount` 검증을 추가한다. 로드 실패 시 동작(§3.5-7: 기능 비활성, 보유 데이터 보존)은 유지한다. 유물 DB 로드 실패 시 **이행(§6.2)은 실행하지 않고 다음 부팅에 재시도**한다.

---

## 6. 세이브 · 이행 · 클라우드

### 6.1 `MetaProfile` Version 14 → 15

현행 `MetaProfileMigration.CurrentVersion = 14`(확인) → **15**.

| 필드 | 타입 | 기본값 | 클라우드 | 설명 |
|---|---|---|---|---|
| `Relics` | `List<RelicEntry>` | 빈 리스트 | 복원 대상 | 보유 유물. 불변식: `RelicId` 비어 있지 않음 · 유물당 1행. `RelicEntry.Stack` 은 **구버전 호환용 레거시 필드**로 이름·타입 유지, 이행 후 항상 1 로 쓴다 |
| `EquippedRelics` | `List<EquippedRelicEntry>` | 빈 리스트 | 복원 대상 | `{ int Slot; string RelicId; }` 찬 칸만 행 존재 |
| `RelicReworkDone` | bool | false | 복원 대상 | 이행 1회 가드(§6.2). 신규 프로필도 false 로 시작해 첫 부팅에서 빈 이행 후 true |
| `RelicReworkNoticePending` | bool | false | **복사 안 함**(로컬 전용, `BattleSpeedOn`·`DisplayName` 선례) | 이행 안내 팝업 1회 표시용 |
| `Currencies` 의 `RelicShard` | 기존 `CurrencyEntry` | - | 복원 대상 | 조각 잔액·누적 획득. 구 잔해 잔액을 **1:1 로 그대로 조각으로 이어받는다** |
| `RelicMissStreak` · `BattleSpeedOn` | 기존 | | | 변경 없음 |

**데이터 비의존 정규화**(`MetaProfileMigration.Apply` 안, 조기 반환보다 앞, 멱등): `NormalizeRelics` 에 추가 — `RelicReworkDone` 이 true 이면 모든 행의 `Stack` 을 1 로 강제. `NormalizeEquippedRelics` 신규 — null 행 · 빈 id · `Slot < 0` 제거, 같은 `Slot` 중복은 먼저 나온 행만 유지, 같은 `RelicId` 중복은 낮은 `Slot` 만 유지, `Relics` 에 없는 `RelicId` 행 제거. `Slot >= equipSlotCount` 는 정규화에서 지우지 않고 **읽는 쪽이 무시**한다(설정이 아직 로드되기 전일 수 있음). 유물 id 가 DB 에 있는지는 정규화에서 확인하지 않는다(`relics.md` §3.6 유지: 알 수 없는 id 는 읽을 때 무시, 세이브는 보존).

### 6.2 데이터 의존 이행 - 1회, 별도 가드

**함정**: `Apply` 는 데이터 로드 여부와 무관하게 `Version` 을 14→15 로 올린다. 변환(중첩 초과분 → 조각)과 자동 장착은 등급 정보가 필요해 유물 DB 가 있어야 하므로, `Version` 만 보고 하면 DB 가 늦게 로드된 부팅에서 영영 건너뛴다. 따라서 **`Version` 과 독립인 `RelicReworkDone` 이 이행의 유일한 가드**다.

`RelicRework.ConvertLegacy(profile, relicDb, growthDb)` (이름은 제안, 구조는 gameplay-programmer 판단) 의 동작 명세:

1. `RelicReworkDone == true` 이거나 `relicDb`/`growthDb`(조각 수량표)가 null 이면 아무것도 하지 않는다(재시도 대기).
2. `Relics` 의 각 행에서 DB 에 **존재하는** 유물만 처리한다(알 수 없는 id 는 건드리지 않고 장착 후보에서도 제외).
3. **중첩 초과분 환산**: `extra = min(Stack, 5) - 1` (5 = 구 `RelicStack` 최대 단계, 변조 세이브 방어). `조각 += extra x shardAmount(rarity)` (일반 1 / 희귀 3 / 전설 8). 그 뒤 `Stack = 1`.
4. **자동 장착**: 위에서 처리한 유물들을 **등급 높은 순(전설 → 희귀 → 일반) → 이행 전 `Stack` 큰 순 → `Relic.json` 행 순서** 로 정렬해 앞의 `equipSlotCount`(4)개를 칸 0, 1, 2, 3 에 순서대로 채운다. 기존 `EquippedRelics` 행이 있으면(이행 도중 재진입 방어) 비우고 다시 채운다 — 어차피 `RelicReworkDone` 이 false 인 프로필은 구버전 문서라 장착 행이 없다.
5. `RelicReworkDone = true`. `Relics`·`Currencies` 가 바뀌었거나 보유 유물이 1개 이상이었으면 `RelicReworkNoticePending = true`(유물·조각 둘 다 없는 신규 프로필은 안내 없음). 1회 `Save`.
6. **조각 지급 총량은 위 3번 한 곳에서만** 일어난다 → `RelicReworkDone` 가 true 가 된 뒤 재호출해도 아무 변화 없음(멱등, 이중 지급 불가).

**호출 지점 3곳**(전부 같은 함수, 같은 가드):
- (a) 부팅에서 `RelicDatabase.Current`·`GrowthDatabase.Current` 가 모두 설정된 직후(`ChapterDatabaseLoader` 가 두 DB 를 채우는 지점), `MetaSession` 프로필 로드 이후.
- (b) 클라우드 복원 직후: `VillageController.ApplyRestoredProfile` 의 `CopyFrom` → `MetaProfileMigration.Apply` 다음 줄 (복원된 v14 문서는 `RelicReworkDone` 필드가 없어 false).
- (c) `BattleController` 전투 시작에서 `BuildRelicBonus` 이전 1회(가드가 true 면 즉시 반환 - 에디터에서 전투 씬 직행·로딩 순서 방어).

**이행 예시(검산)**: 구 풀셋 보유자(일반: 심장석 5 · 칼날 5 · 장화 5 · 잿불 5 · 뿔피리 3 · 소울 주머니 5 · 영주의 서 5 · 깃털 3 / 희귀: 모래시계 3 · 부적 3 · 뇌운 3 · 깃발 3 · 가속 1 · 둥지 1 / 전설 1+1)
- 일반 초과분 = (4+4+4+4+2+4+4+2) = 28 개 x 1 = **28 조각**, 희귀 초과분 = (2+2+2+2) = 8 개 x 3 = **24 조각**, 전설 0 → 합계 **52 조각**.
- 자동 장착 4칸 = 전설 2종(`KingsCrownShard`, `WrathSpirit`) + 희귀 중 이행 전 Stack 3 인 4종의 `Relic.json` 순서 앞 2종(`BrokenHourglass`, `BreedingCharm`).
- 구 잔해 잔액이 있으면 그대로 더해진다.

**이행 안내**: `RelicReworkNoticePending == true` 면 마을 첫 진입에서 기존 `ConfirmPopup` 1버튼 모드로 안내한다(신규 화면 아님): "유물 시스템이 바뀌었습니다 / 유물은 이제 4칸에 장착한 것만 전투에 적용됩니다. 같은 유물을 또 얻으면 조각이 됩니다. 기존 유물 {n}개를 자동 장착했고, 겹친 만큼 조각 {m}개를 드렸습니다." 확인하면 false 로 내리고 저장한다. 2배속(`HasteHourglass`)을 쓰던 유저가 자동 장착에서 밀리면 토글이 사라져 당황할 수 있는 것이 이 안내의 주 목적이다.

### 6.3 클라우드 동기

- **복원은 전체 대체(기존 규약)다. 유물·조각·장착에 합집합 병합을 하지 않는다.** 병합하면 "기기 A 에서 제작(조각 차감) → 오래된 클라우드(조각 있음) 복원 → 합집합" 으로 조각은 되살아나고 제작한 유물은 남는 **무한 제작 취약점**이 생긴다. 데일리(`MergeDailyFrom`)처럼 환불 방지 병합이 필요한 필드가 아니다 — 교환 가능한 경로가 없고 모든 필드가 한 문서의 일관된 한 시점이기 때문이다.
- `CopyFrom` 은 `Relics` · `EquippedRelics`(새 인스턴스로 복사) · `RelicReworkDone` 을 복사한다. `RelicReworkNoticePending` 은 복사하지 않는다. 조각은 `Currencies` 복사에 포함된다.
- 충돌 판정은 기존 `serverVersion` 타임스탬프 기반으로 변경 없음. 충돌 UX 변경 없음.
- **구문서 복원**: v14 문서(`EquippedRelics`·`RelicReworkDone` 없음, Stack 중첩 있음) → `CopyFrom` → `Apply` → `ConvertLegacy` 가 다시 한 번 §6.2 를 수행한다. 복원 문서는 이전 상태이므로 지금 기기의 이행 결과는 덮어써진다(전체 대체와 일관).
- 구클라이언트가 v15 문서를 복원하면 `Relics` 만 읽고 Stack 이 전부 1 이며 장착 개념이 없다(구 클라이언트는 전부 적용). 신구 클라이언트 동시 운용은 지원하지 않는다(`relics.md` §4.2 와 같은 전제).
- 업로드 `schemaVersion` 은 `profile.Version` 을 그대로 쓰므로 15 로 자동 승격.

---

## 7. UI (신규 화면 없음 - 기존 팝업 변경만)

### 7.1 유물 도감 팝업 (`RelicPopup`)

현행 배치(`.art-incoming/rl-set/APPLY_PLAN.md` 적용 후 구조): 팝업 본체 1136x640(`RelicPanel` 1:1, 팝업 로컬 좌표), 필터 칩 열 x≥28, 그리드 `RelicScrollView` 520 폭 x=224, 상세 `DetailPanel` 352x472 x=760, 하단 행 y=556 높이 56(교환 줄 + 안내문, 가용 폭 x≥60 ~ ≤1076). **본체·그리드·상세 크기는 바꾸지 않고**, 하단 행의 용도를 바꾼다.

| 영역 | 변경 |
|---|---|
| 하단 행 (y=556, h=56) | 구 "잔해 교환 줄 + 안내문" → **장착 칸 4개 + 조각 칩 + 안내문**. 장착 칸 4개: x=164, 228, 292, 356 각 56x56(피치 64 = 56+8, 끝 356+56 = 412), 앞에 "장착 {k} / 4" 라벨(x=60, 폭 100). 조각 칩: 구 `ShardChip`(248x56, `CommonUIPanelSunk`)을 x=828~1076 에 재사용해 "유물 조각 {N}" 표시(색 `#c8a27a`). 안내문은 x=420~820 (폭 400) 1줄 짧은 문구 |
| 교환 버튼 3개(`ExchangeButton0~2`) | **삭제**(교환 폐기 §8) |
| 그리드 셀(124x108) | "n/m" 중첩 글자와 MAX 배지 삭제. 보유 셀: 이름 + 장착 중이면 우상단 **"장착" 배지**(기존 `MaxBadge` 위젯 재사용·문구 교체). **미보유 셀: 아이콘 어둡게(현행) + 이름 표시(구 "???" 폐지) + 우하단에 "조각 {비용}"**(조각이 비용 이상이면 금색, 미만이면 회색 — 구 중첩 글자 위젯 재사용). 테두리는 등급 색 유지 |
| 상세 패널(352x472) | 구 "중첩 n/m · 현재 효과 · 다음 중첩/최대 중첩" 삭제. 표시: 아이콘 · 이름 · 등급 칩 · 종류 · **효과 설명(고정값)** · 드랍 영웅 · "중복 시 조각 +{분해량}" · (미보유) "제작 비용 조각 {비용}" · **주 버튼 1개**(§2.2 상태표: 제작 / 장착 / 장착 해제 / 비활성) · 장착 중 표시. 미보유도 이름·효과를 숨기지 않는다(제작하려면 알아야 하므로 사용자 결정 U-2) |
| 상단 | "수집 n / 16"(현행 `CollectedText`) · 우측 `StackSumText` 위젯은 "장착 k / 4" 로 문구만 교체 |
| 필터 칩 | 변경 없음(종류 4 x 등급 4, AND) |
| 제작 확인 | 제작 버튼 → 기존 `ConfirmPopup`(2버튼): 제목 "유물 제작", 본문 "{이름}을(를) 제작할까요? 조각 {비용}개를 사용합니다". 확인 시 제작·토스트 "{이름} 제작 완료" |
| 제작 직후 | 빈 칸이 있으면 자동 장착(§2.1), 선택은 방금 만든 유물 유지, 셀·칸·조각 칩 즉시 갱신 |

- 계층 구조: `RelicViewModel` 이 셀·상세·장착 칸·버튼 상태(문구·활성)를 모두 확정 값으로 계산하고 View 는 표시·입력만 한다(Rule 02 §6). 제작·장착·교체·해제는 VM 의 명령이며 프로필 변경 + `Save` 는 호출부(`RelicPopup`)가 담당한다(현행 `HandleExchange` 패턴).
- 장착 칸 위젯은 `[SerializeField] private` 4개 + 의도 API(`SetSlot(index, icon, rarityColor, isEmpty, isHighlighted)`)로 노출(Rule 02 §6.1). 빈 칸은 어두운 칸 + 얇은 윤곽.
- **프리팹 구조 변경이 있다**: 장착 칸 4개 신규 위젯, 하단 행 재배치, 교환 버튼 제거, 상세 패널 주 버튼 1개. `CLAUDE.md` §8 "공통 UI 아트 교체" 항목은 프리팹 구조 변경을 금지하므로 §9-③ 으로 `RelicPopup` 한정 예외를 둔다. **UI 배선은 사용자 목업 승인 후** (Rule 00 목업 승인 게이트): 이 문서의 HTML 이 승인용 목업이다.
- 스프라이트: 신규 아트 0장을 목표로 한다 — 장착 칸은 `RelicIconFrame`/`RelicCellSel`(틴트) + `Px_Solid` 어두운 배경, 주 버튼은 `CommonUIBtnGold`(활성)/`CommonUIBtnOff`(비활성), 조각 칩은 `CommonUIPanelSunk`, "장착" 배지는 `CommonUIBadgeSoul`. 이것으로 시각 품질이 부족하면 `RelicUI*` 세트에 슬롯 프레임 1장 추가를 `ui-art-refresh.md` 절차(사용자 확인)로 별도 처리한다.
- 팝업을 열어 둔 동안 보유 유물은 전투 불가 상태라 변하지 않는다(E-11). 조각·장착만 팝업 안에서 변한다.

### 7.2 결과창 유물 띠 (`ResultPopup`)

기존 띠(472x64, 레이아웃 불변)의 3가지 상태. `ResultPopupArg`: `HasRelicDrop`(신규 획득) · `HasRelicShard`(분해 — 필드명 유지) · `RelicId`(두 경우 모두 유효) · `RelicIsNew` · `RelicShardAmount` · `RelicShardRarity` · 신규 **`RelicAutoEquipped`**(bool). 삭제: `RelicStackAfter` · `RelicMaxStack`.

| 상태 | 띠 구성 |
|---|---|
| 신규 획득 | 아이콘 + 이름 + 등급 칩 + `NEW` 배지(금색). 하단 줄: 자동 장착되면 "자동 장착됨", 빈 칸이 없으면 "장착 칸이 가득 차 있어 도감에서 장착할 수 있습니다" |
| **분해(중복)** | 아이콘(중복 유물, 어둡게 하지 않음) + 이름 + 등급 칩 + 우측 **"조각 +{n}"**. 하단 줄: "이미 가진 유물이라 조각으로 분해되었다"(구 `ShardConverted` 문구 재사용·개정). 구 "유물 잔해 +{n}"(아이콘 없는 변형)은 이 상태로 대체 |
| 미획득(승리) | 변경 없음: "유물 없음 · 다음 승리 시 획득 확률 {n}%" |

전설 글로우·`RelicAcquire` SFX 규칙은 신규 획득과 분해 모두 유지(전설이 분해돼도 연출은 같다 — "전설을 또 만났다" 피드백).

### 7.3 그 외 표기 영향

| 화면 | 변경 |
|---|---|
| 마을 HUD 유물 버튼 | 변경 없음(도감 진입점). 알림 점 없음(YAGNI) |
| 전투 HUD 2배속 토글 | 표시 조건이 "`HasteHourglass` **장착 중**" 으로 바뀐다(코드: `RelicBattleBonus.HasBattleSpeed` 가 장착 기준) |
| 데일리 팝업(`DailyPopup`) | 오늘의 유물 칸의 "x3" 중첩 표기 삭제(`DailyRelicView.StackText` 제거). 유물 효과는 고정값으로 표시 |
| 탑·챕터 결과/월드맵 | 변경 없음 |
| 상점 재화 지갑 | 조각 사용처는 도감 한 곳뿐이다. 지갑 표시 목록에 `RelicShard` 가 들어 있으면 제거(구현 시 확인) |
| 상점 > 몬스터 강화 | 변경 없음 |
| `PlayDriver.Result.cs` 샘플 인자 | `RelicStackAfter`/`RelicMaxStack` 삭제에 맞춰 갱신 |

### 7.4 Strings_Ko

현행 최대 id = 1878. 신규는 **1900~1913** (연속 14개, 1879~1899 는 다른 작업 여지로 비움).

| id | 문구 | 용도 |
|---|---|---|
| 1900 | `장착` | 셀 배지 · 버튼 |
| 1901 | `장착 해제` | 버튼 |
| 1902 | `제작 · 조각 {0}` | 제작 버튼 |
| 1903 | `장착 {0} / {1}` | 하단 라벨 · 상단 우측 |
| 1904 | `조각이 부족합니다 (필요 {0} · 보유 {1})` | 비활성 안내 |
| 1905 | `{0}을(를) 제작할까요? 조각 {1}개를 사용합니다` | 확인 팝업 본문 |
| 1906 | `유물 제작` | 확인 팝업 제목 |
| 1907 | `{0} 제작 완료` | 토스트 |
| 1908 | `장착 칸이 가득 찼습니다. 바꿀 칸을 누르세요` | 비활성 안내 |
| 1909 | `자동 장착됨` | 결과창 |
| 1910 | `장착 칸이 가득 차 있어 도감에서 장착할 수 있습니다` | 결과창 |
| 1911 | `중복 시 조각 +{0}` | 상세 |
| 1912 | `제작 비용 조각 {0}` | 상세 |
| 1913 | `유물 시스템이 바뀌었습니다` | 이행 안내 제목 |
| 1914 | `유물은 이제 4칸에 장착한 것만 전투에 적용됩니다. 같은 유물을 또 얻으면 조각이 됩니다. 기존 유물 {0}개를 자동 장착했고, 겹친 만큼 조각 {1}개를 드렸습니다.` | 이행 안내 본문 |

(신규 총 15개: 1900~1914, 검산 1914-1900+1 = 15.) 개정: `1367` → `개전의 뿔피리` · `1383` → `전투 시작 후 {0}초 동안 모든 몬스터의 공격력 +{1}%`(인자: durationSec, amount%) · `1386` → `영웅이 잃은 HP {0}%마다 모든 몬스터의 공격력·이동속도 +{1}% (HP 0%에서 최대 +{2}%, 상시)`(인자: stepFraction%, amountPerStep%, 최대합% = amountPerStep x 10) · `1469` → `유물 조각` · `1476` → `유물 조각 +{0}` · `1477` → `이미 가진 유물이라 조각으로 분해되었다` · `1357` → `아직 획득하지 못한 유물입니다. 조각으로 제작할 수 있습니다` · `1392`(도감 안내) → `유물은 전투에서 승리하면 얻을 수 있습니다. 장착한 4개만 전투에 적용되고, 같은 유물은 조각이 됩니다.` · `1347`(`???`)은 유물 이름 용도에서는 더 쓰지 않는다(다른 용도가 없으면 삭제).
삭제(미사용): 중첩 문구 `1348` · `1351` · `1355` · `1356`, 교환 문구(`GrowthStrings` 의 `ExchangeHeader` · `ExchangeButton` · `ExchangeLocked` · `ShardShort` 가 가리키는 id — 구현 시 상수로 확인). `Strings_Ko` 무결성 테스트(연속·중복·포맷 인자)는 삭제된 id 를 허용하도록 갱신한다.
`RelicText.BuildArgs`: `OpeningBuff` → `{durationSec, amount%}`, `LostHpEmpower` → `{stepFraction%, amountPerStep%, amountPerStep x 10 %}`(설명문은 `Effects[0]` 기준), 나머지 효과는 현행(중첩 인자 제거).

---

## 8. 잔해 교환(`Currency_Exchange`) 이행 - 대안 비교

중복이 매번 조각이 되므로 조각 수입이 크게 늘어난다(현행은 등급이 전부 최대 중첩일 때만 발생). 교환 비율(3/5/8)을 유지하면 각성석·달빛 인장·심연 결정 경제가 유물 드랍으로 흘러들어 `monster-growth-currencies.md` 의 콘텐츠별 재화 설계(난이도 → 각성석, 데일리 → 인장, 탑 → 결정)가 약해진다.

| 안 | 내용 | trade-off | 판정 |
|---|---|---|---|
| **A. 폐기 (권장)** | 교환 3행·UI·`CurrencyExchange` 코드 삭제. 조각은 제작 전용. 기존 잔해 잔액은 1:1 조각으로 이어받음 | 육성 재화 3종의 소스가 콘텐츠로 되돌아가 설계 의도 유지. 조각이 단일 용도라 UX 단순(도감 한 곳). 단점: 기존 유저가 잔해를 육성 재화로 바꿀 선택지가 사라짐 — 단 해금 조건이 있던 보조 경로였고, 이미 교환해 얻은 재화는 그대로다 | **채택(권장)** |
| B. 유지 + 재튜닝 | 교환 줄을 도감에 남기고 비율을 qa-simulator 로 재조정(예: 5/8/12) | 제작과 교환이 같은 재화를 다투어 "무엇에 쓸까" 선택이 생기지만, 도감 하단 행이 장착 칸·조각 칩·교환 3버튼으로 과밀해진다. MG1~MG10 게이트 재측정 필요 | 대안 |

**A 선택 시 작업 목록**: `Currency_Exchange.json` 과 `Currency_ExchangeRow`·`EData.Currency_Exchange`·`GrowthDatabase.FindExchange`·`CurrencyExchange`·`ShardExchangeViewModel`·`ShardExchangeView`·`EExchangeResult` 삭제, `CurrencySettlement` 의 교환 함수(`Check`/`Exchange`) 삭제(해금 판정 `IsUnlocked` 의 RelicShard 분기는 지갑 표시용이 아니면 함께 삭제), `RelicPopup` 의 교환 배선 삭제, `GrowthDatabase` 의 교환 검증(`to == RelicShard` 등) 삭제, `monster-growth-currencies.md` 의 §2.4·§8.3 및 게이트 MG 중 교환 항목은 "relic-rework.md 로 대체" 로 표시. 사용자 결정 U-3(HTML 하단 박스).

---

## 9. CLAUDE.md §8 승격 문안 초안 (사용자 승인 후 메인이 반영 - CLAUDE.md 는 이 작업에서 수정하지 않는다)

충돌 지점 3개 항목: ① "유물 2차 허용" ② "몬스터 육성 재화 분리 허용"(잔해 변환·교환) ③ "공통 UI 아트 교체 허용"(프리팹 구조 변경 금지).

**① "유물 2차 허용" 항목을 다음으로 교체한다(2026-11-23 개정).**

> - **유물 2차 허용 (2026-10-19 v0.3 승격, 2026-11-23 개정) - 단 전투 종료 시 드랍되는 수집형 유물 16종·유물 조각 분해/제작·장착 4칸·마을 도감 팝업에 한정** - 영웅(챕터)·스테이지·난이도별 확률로 유물을 드랍하고(승리 시 정상 확률, 패배 시 1/4), 같은 유물을 또 얻으면 즉시 등급별 **유물 조각**(일반 1 / 희귀 3 / 전설 8)으로 분해된다. 조각으로 **보유하지 않은** 유물을 제작할 수 있고(일반 10 / 희귀 18 / 전설 32), 마을 유물 도감에서 **4칸에 장착한 유물만** 전투 시작 시 적용된다(등급 제한 없음, 효과는 중첩 없는 고정값). 유물 종류는 ① 영구 능력치 ② 전투 효과(영웅에게 번개·화상 `IHeroAura`, 전투 시작 후 시간 한정 몬스터 공격력 강화, 영웅이 잃은 HP 비례 몬스터 공격력·이동속도 강화) ③ 편의(소울·XP 보상 가산, 2배속, 첫 스폰 단축)이다. 허용 범위는 ① 유물·드랍 JSON 데이터(`Relic*`, `RelicRarity_Shard`, `Stage_Relic`, `Difficulty_Relic*`; 중첩 데이터 `RelicStack` 폐지) ② `MetaProfile` 의 보유 유물·장착 칸·이행 가드·연패 카운터 저장(Version 15)·클라우드 동기 호환(복원은 전체 대체, 병합 없음) ③ 마을 HUD 유물 버튼과 `RelicPopup` 도감(장착 칸·제작 버튼·조각 표시), 결과창 획득/분해 줄, 이행 안내(기존 확인 팝업 1버튼) ④ 전투 HUD 2배속 토글(가속 모래시계 장착 시)과 `PauseService` 배속 보관 ⑤ `MonsterBuffService` 의 유물 값 오버레이. **유물끼리의 합성, 유물 강화(레벨업), 유물 판매·조각 환급(보유 유물을 재화로 되돌리기), 조각의 타 재화 환전, 세트 효과, 장착 칸 확장, 장착 프리셋, ★ 목표, 신규 몬스터, 신규 스테이지 규칙은 계속 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지(도감은 마을 팝업). 밸런스 수치는 qa-simulator 일괄 튜닝 대상이다. 세부는 `docs/design/relic-rework.md`.

**② "몬스터 육성 재화 분리 허용" 항목 수정(2026-11-23)**: "유물 드랍에서 뽑힌 등급의 유물이 모두 최대 중첩이면 유물 대신 유물 잔해가 나와 마을 유물 도감에서 세 재화로 교환된다" → "중복 유물은 유물 조각으로 분해되어 마을 유물 도감에서 유물 제작에 쓰인다(조각의 각성석·달빛 인장·심연 결정 교환은 폐지)". 허용 범위 ①의 `Currency_Exchange`·`RelicRarity_Shard` 중 `Currency_Exchange` 삭제, ③의 "유물 도감 잔해 교환 줄" → "유물 도감 조각 칩". 계속 금지 문구 "잔해 교환 외의 재화 간 환전" → "재화 간 환전". (사용자 결정 U-3 이 B 면 이 ②는 비율 개정으로 대체.)

**③ "공통 UI 아트 교체 허용" 항목에 예외 1줄 추가**: "단, `RelicPopup` 은 유물 시스템 개편(2026-11-23)에 한해 프리팹 구조 변경(장착 칸 4개·상세 주 버튼·하단 행 재배치·교환 버튼 제거)을 허용한다. 이외 화면의 구조 변경 금지는 유지."

컨셉서 §11 범위 표에는 "유물 개편(장착 4칸·조각 분해/제작) - v0.3 승격 2026-11-23" 한 줄을 추가한다(`relics.md` §13 선례). `relics.md`·`monster-growth-currencies.md` 는 당시 승격 기록이므로 본문은 수정하지 않고 머리말에 "일부 규칙은 relic-rework.md 로 대체" 한 줄을 추가한다.

---

## 10. 엣지 케이스

| # | 케이스 | 처리 |
|---|---|---|
| E-1 | 유물 DB 로드 실패 | 기능 비활성(드랍·효과·도감 빈 팝업). 이행은 실행하지 않고 보유·장착·조각 데이터를 보존, 다음 부팅 재시도 |
| E-2 | `Relics`/`EquippedRelics` 에 DB 에 없는 id | 효과·표시 무시, 세이브 비수정(유물 데이터가 복구되면 되살아남). DB 에 없는 장착 행은 칸을 차지한 채 도감에서 빈 칸처럼 보이므로, 장착 명령(빈 칸 탐색)은 "DB 에 있는 유물이 든 칸" 만 찬 칸으로 센다 |
| E-3 | `EquippedRelics` 의 `Slot >= equipSlotCount` 이거나 중복 | 중복·`Relics` 에 없는 id 는 정규화가 제거, 범위 밖 칸은 읽는 쪽이 무시 |
| E-4 | 4칸 전부 같은 효과 종류 | 허용. 같은 `effectKind` 는 효과 종류별로 합산(`SoulBonus` 등)·곱연산(`MonsterStat`) 규칙을 그대로 따른다(현행과 동일). 같은 유물 2칸 장착은 불가 |
| E-5 | 조각이 비용 이상인데 모든 유물을 이미 보유 | 제작 대상이 없음. 조각은 쌓이기만 하고 사용처 없음(16종 완성 = 엔드 상태, 사용자 인지 사항 — 후속 소비처는 §13 범위 밖) |
| E-6 | 제작 확인 팝업 중 조각이 변함 | 팝업 중에는 조각 변경 경로가 없다(전투 불가). 확인 시점에 비용·미보유를 다시 검사하고 실패하면 토스트(1904)만 |
| E-7 | 제작·장착 직후 앱 종료/크래시 | 변경마다 즉시 `Save`. 제작은 "조각 차감 + 유물 추가 + 자동 장착" 을 한 번의 `Save` 로 확정(중간 상태 저장 금지) |
| E-8 | 전투 중 뒤로가기 종료 | `EndBattle` 비경유 → 드랍·분해·pity 변화 없음(현행 E-7) |
| E-9 | 데일리 | 개인 유물은 `EquippedRelics` 중 `BattleSpeed`·`SoulBonus`·`XpBonus`·`LoseSoulBonus` 만 유지(`IsKeptInDaily`), 오늘의 유물 2개를 이 판에 전부 더함(스택 없이 고정값). 신규 `OpeningBuff`·`LostHpEmpower` 는 개인 유물일 때 데일리에서 비적용, 오늘의 유물일 때 적용 |
| E-10 | 데일리 오늘의 유물 후보 | A = 일반 Stat/Combat(`AmbushHorn`·`EmberBrand`·`HeartOfStone`·`RustyBlade`·`SwiftBoots` 5종), B = 희귀·전설 Stat/Combat(`BreedingCharm`·`BrokenHourglass`·`CursedBanner`·`KingsCrownShard`·`ThunderShard`·`WrathSpirit` 6종, 전부 relicId 서수순). **풀 구성이 현행과 같고 RNG 뽑기 순서(영웅→상시→페이즈→A→B→패시브 시드→액티브 시드)를 유지하므로 날짜별 조합은 현행과 같다** |
| E-11 | `DailyChallenge.ComboKey` | 현행 `...|{RelicAId}x{RelicAStack}|{RelicBId}x{RelicBStack}` 에서 스택 표기 삭제 → `...|{RelicAId}|{RelicBId}`. 이 키는 `SubmitDailyAsync` 로 서버 순위 키에 쓰이므로 **신구 클라이언트의 같은 날 데일리 순위가 갈라진다** — 오늘의 유물 값이 바뀌어 신구 기록을 같은 보드에 섞는 것이 불공정하므로 의도한 분리다. 배포는 한국 시간 0시 직후를 권장 |
| E-12 | `DailyConfig.commonRelicStack`·`rareRelicStack` | 필드 삭제(스택 개념 폐지). `DailyChallenge.RelicAStack`·`RelicBStack` 삭제 |
| E-13 | 탑 | `RelicTier`(드랍 등급 보정)는 변경 없음. 탑 전투는 장착 유물을 전부 적용(현행 "보유 전부 적용" → "장착 적용") |
| E-14 | `AmbushHorn` 보유자 | ID 동일이라 보유가 유지되고 효과만 소환 → 개전 버프로 바뀐다(구 소환 결과 없음). 이행 환산에서 중첩 초과분은 조각이 됨 |
| E-15 | `WrathSpirit` 과 영웅 회복 | 단계가 HP 를 양방향으로 따라간다(§4.2). 회복이 잦은 영웅에서 평균 단계가 낮아지는 것은 의도 |
| E-16 | 정지·카드 선택 중 | `OpeningBuff` 타이머 정지(timeScale 0 = dt 0). `LostHpEmpower` 는 HP 변화 시에만 재계산 |
| E-17 | `OpeningBuff` 와 봉인 규칙·몬스터 캡 | 소환이 없으므로 충돌 없음(구 E-15·E-17 소멸) |
| E-18 | 이행 중 `Currencies` 에 구 `RelicShard` 잔액 | 그대로 이어받음. 이행 안내 문구의 "조각 {m}개" 는 **환산으로 새로 지급한 양만** 센다 |
| E-19 | 신규 설치(유물·조각 0) | 이행은 빈 이행(가드만 true), 안내 없음. 첫 승리 유물 보장 → 자동 장착 |
| E-20 | 자동 장착 대상이 4개 초과 | 정렬 순서(§6.2-4)로 앞 4개만 장착, 나머지는 보유·미장착 |

---

## 11. 검증 게이트

### 11.1 EditMode (test-engineer)

**신규/개정 항목**

| 영역 | 항목 |
|---|---|
| 데이터 | 출하 JSON: `Relic`(16행, `maxStack` 없음) · `Relic_Effect`(18행) · `Relic_Effect_Param`(31행, §5.2 값 일치) · `RelicRarity_Shard`(shardAmount 1/3/8, craftCost 10/18/32, 산식 10x1·6x3·4x8 일치) · `RelicConfig.equipSlotCount` 4 · `RelicStack.json`·`Currency_Exchange.json` 부재. 검증 위반(구 kind·`statKind` 범위·`stepFraction` 정수 역수 등)이 로드 오류 |
| 드랍 굴림 | 보유 유물이 뽑히면 Dissolved + 등급별 조각 · 미보유면 Acquired · 영웅 테이블 전부 보유여도 확률대로 굴림(항상 Dissolved) · 난수 소비 순서 불변(등급 1 + 유물 1) · 획득(Acquired/Dissolved) 시 `RelicMissStreak` 0 · `RollGuaranteed` 의 Dissolved 가 `RelicMissStreak` 불변 · 첫 유물 보장 · 패배 굴림 pity 비사용 |
| 자동 장착 | 빈 칸 있을 때 신규 획득 시 최저 번호 빈 칸에 장착 · 4칸 가득이면 미장착(`RelicAutoEquipped` false) · 분해는 장착 불변 |
| 제작 | 비용 차감·유물 추가·자동 장착 한 번에 · 보유 유물 제작 거부 · 조각 부족 거부 · 알 수 없는 id 거부 · 제작이 `RelicMissStreak` 불변 |
| 장착 상태표 | §2.2 모든 행: 빈 칸 장착, 가득일 때 교체, 같은 유물 중복 장착 불가, 해제, 슬롯 탭 선택/교체 분기 |
| 전투 집계 | `RelicBattleBonus.From`·`FromDaily` 가 장착만 읽음 · 미장착 보유 유물은 효과 0 · 고정값 `MonsterStat`(예: `HeartOfStone` x1.06) · `OpeningBuff` 스펙 · `LostHpEmpower` 스펙 · 보상 가산(소울·XP·패배) 고정값 floor · `BattleSpeed` 는 `HasteHourglass` 장착 시만 |
| 오버레이 | `MonsterBuffService` 유물 오버레이: 15초 경과 후 소멸, 창 안 새 스폰 몬스터 포함, 다른 `PowerScale` 곱(`BerserkPower` x3)과 곱셈 합성, `LostHpEmpower` steps 정수식(100/100→0, 90/100→1, 51/100→4, 50/100→5, 1/100→9, 0/100→10), 최대 HP 변화 시 재계산, 회복 시 단계 하락, 정지 중 타이머 불변 |
| MetaProfile | v14 JSON → v15(필드 기본값) · 정규화 규칙(§6.1) · 멱등 · `CopyFrom`(Relics·EquippedRelics 복사, `RelicReworkNoticePending` 비복사, null → 빈 리스트) · 복원 후 Migration 재적용 |
| 이행 | §6.2 전 단계: 풀셋 보유자 52 조각·4칸(전설 2 + 희귀 `BrokenHourglass`·`BreedingCharm`) · 동률 정렬(등급 → 이전 Stack → `Relic.json` 순서) · DB 미로드 시 가드 불변·재시도 · `RelicReworkDone` 후 재호출 무변화(이중 지급 없음) · 변조 Stack 99 → 5 로 클램프 · 알 수 없는 id 보존 · 신규 프로필 안내 없음 · 구 잔해 잔액 유지 |
| 문자열 | §7.4 신규·개정 id, 포맷 인자 개수, 삭제 id 허용 |
| 데일리 | `DailyChallenge` 스택 필드 제거, ComboKey 새 형식, **날짜별 조합(영웅·규칙·유물 A/B)이 현행과 동일**(골든 갱신: 키 문자열만) |
| 프리팹 | `RelicPopup` 장착 칸 4개·주 버튼·조각 칩의 신규 `[SerializeField]` 미연결 0, `RelicCell` 개정 위젯, `ResultPopup` 유물 띠 상태 3종 |

**기존 테스트 갱신 대상**(컴파일·실패 기준으로 test-engineer 가 확정, 현재 확인된 목록): `Tests/EditMode/Relic/` 전체(`RelicDataTests` · `RelicDropRollerTests` · `RelicBattleBonusTests` · `RelicMetaProfileTests` · `RelicViewAndTextTests` · `RelicAssetWiringTests` · `RelicAuraTests` · `RelicBattleSpeedTests` · `RelicSupportAndSelfTests` · `RelicTestKit`) · `Growth/RelicShardTests` · `Growth/CurrencySettlementTests` · `Growth/GrowthShopAndShardSuiteTests` · `Growth/GrowthShippedDataTests` · `Growth/GrowthTestKit` · `Growth/AwakenBonusTests` · `Growth/NestMasteryTests` · `Meta/MetaShippedDataTests` · `Chapter/MetaProfileMigrationTests`(버전 14 → 15) · `Daily/DailyFoundationTests` · `Daily/DailyGoldenAndRulesTests` · `Daily/DailySaveAndBattleTests` · `UI/ResolutionAdaptationTests`.

### 11.2 qa-simulator 게이트 (밸런스 일괄 튜닝 시, 최종 수치 확정)

`relics.md` §9 의 R1~R7 을 아래로 대체한다. **모든 수치는 가설이며 qa-simulator 검증 후 결정한다.**

| # | 지표 | 통과 기준(초기 가설) | 결정 메트릭 · 실패 시 조치 |
|---|---|---|---|
| R1' | 조각 경제: 전설 1종 제작까지 평균 승리 수, 16종 완성까지 평균 승리 수 | 전설 1종 제작 30~60승(Easy 일반 스테이지 추정 42승, §2.4), 16종 완성은 qa 실측으로 결정 | 범위 밖이면 `shardAmount`/`craftCost`/`Stage_Relic.dropChance` 조정 |
| R2' | 빌드별 처치 시각 비(무유물 대비): A 공격 극대 / B 안정 / C 경제 | A -45% 이내(현행 R2 상한), B -25% 이내, C -15% 이내 | A 초과 시 `LostHpEmpower.amountPerStep` 0.04 → 하향이 1순위, `KingsCrownShard` amount 2순위 |
| R3' | 빌드 A + 상점 만렙 Hard 승률 · 처치 시각 하한 | Hard 승률 90% 이하, 모든 (스테이지, 난이도) p10 처치 시각 >= 75초, 최소 >= 30초 | 초과 시 전투 효과 수치 인하 |
| R4' | `WrathSpirit` 시간 평균 단계 실측(가정 4.5) | 3.5~5.5 | 범위 밖이면 §3.1 압력 표 재산출 |
| R5' | `AmbushHorn`(15초 +15%) 기여도: 개전 15초 구간 영웅 HP 손실 비율 | 동급 일반 유물(`RustyBlade`)과 처치 시각 개선 편차 2배 이내 | 편차 크면 `durationSec`/`amount` 조정 |
| R6' | 등급별 드랍·분해 비율 vs §2.4 표 | 오차 ±2%p | 구현 버그 |
| R7' | 4칸 선택 다양성: 최적 빌드 외 빌드 승률 격차 | 상위 빌드 간 승률 차 10%p 이내 | 편차 큰 유물 수치 조정 |
| R8' | 소울 경제(`SoulPurse`·`LordsTome` 장착 빌드 포함) | 전 항목 만렙 도달 런 수가 49런의 80% 이상 | `SoulBonus` 인하 |
| R9' | 2배속 on/off 승률·처치 시각 분포(`relics.md` R7 이관) | 승률 ±2%p, 평균 ±3초 | §5.3 재검토 |
| R10' | 데일리 DQ1~DQ5 재측정(오늘의 유물이 고정값으로 바뀜: 일반 중첩 3 → 1단계 고정값, 희귀 1단계 값 상승) | 기존 DQ 기준 | 초과 시 `DailyConfig`/유물 값 조정 |
| MG | `monster-growth-currencies.md` MG1~MG10 중 잔해 교환 항목 | A 채택 시 해당 항목 삭제, 나머지 재측정 불필요(육성 재화 소스 불변) | |
| 이행 | 가상 구 세이브 5종(신규·일부·풀셋·잔해 보유·변조) 이행 결과 | 설계 §6.2 산식과 일치 | 구현 버그 |

시뮬레이터가 쓰도록 드랍 굴림 난수원 주입(순수 함수)·장착 상태를 프로필로 재현할 수 있게 유지한다.

---

## 12. 구현 요청사항 (gameplay-programmer)

### 12.1 슬라이스 분할 제안

| 슬라이스 | 범위 | 선행 |
|---|---|---|
| A. 데이터·세이브 | JSON 변경(§5), `RelicDatabase` 중첩 제거·검증, `MetaProfile`(Version 15 · `EquippedRelics` · 가드 필드)·정규화·`CopyFrom`·이행 `ConvertLegacy`(§6) | 승인(§9, 데이터 구조표) |
| B. 규칙·전투 | 드랍 굴림 개정(§2.3) · 장착/제작 서비스(순수 함수) · `RelicBattleBonus` 장착 기준·신규 효과 · `MonsterBuffService` 오버레이 · `BattleController` 연결(§4.3) · 데일리 스택 제거 · 교환 삭제(§8) | A |
| C. UI | `RelicPopup`·`RelicCell`·`RelicDetailView`·`RelicViewModel`(장착 칸·제작) · `ResultPopup` 띠 · 이행 안내 · Strings · `PlayDriver` | B, **목업 승인(Rule 00 게이트)** |

### 12.2 Enum (`CommonEnum.cs`)

- `ERelicEffectKind`: **추가** `OpeningBuff`, `LostHpEmpower`. **삭제** `OpeningSummon`, `LowHpFrenzy`(값 삭제 시 중간 값 번호가 이동하므로 enum 정수 값을 JSON/세이브에 쓰지 않는 현행을 유지한다 — 모두 문자열 값명으로 직렬화).
- `EData`: **삭제** `RelicStack`, `Currency_Exchange`(§8 A 채택 시). 신규 값 없음(`RelicRarity_Shard` 는 기존).
- `ERelic` · `ERelicCategory` · `ERelicRarity` · `ECurrency`: 변경 없음(`RelicShard` 값 유지, 표시명만 "유물 조각").
- `ERelicDropOutcome`: `Shard` → `Dissolved` 개명(공용 enum 이 아니라 `RelicDropRoller.cs` 내부 정의).

### 12.3 Interface

- 신규 공용 Interface 없음. `MonsterBuffService` 의 유물 오버레이 입력은 `BattleController` 가 조립하는 두 값(공격력 배율·이동 배율)과 갱신 호출 한 쌍으로 충분하다.

### 12.4 에셋 키 · 파일

- JSON 수정: `Relic.json`(maxStack 삭제) · `Relic_Effect.json` · `Relic_Effect_Param.json` · `RelicRarity_Shard.json`(craftCost 추가) · `RelicConfig.json`(equipSlotCount 추가) · `DailyConfig.json`(`commonRelicStack`·`rareRelicStack` 삭제) · `Strings_Ko.json`(§7.4).
- JSON 삭제: `RelicStack.json` · `Currency_Exchange.json`(§8 A) — Addressables 항목·`.meta` 정리.
- 프리팹(목업 승인 후): `RelicPopup.prefab`(장착 칸 4 · 조각 칩 재배치 · 상세 주 버튼 · 교환 줄 제거), `RelicCell.prefab`(위젯 의미 교체: 스택 글자 → 조각 비용, MAX 배지 → 장착 배지), `ResultPopup.prefab`(유물 띠 상태), `DailyPopup` 프리팹의 "x3" 위젯 비활성/삭제.
- 신규 스프라이트·FX·오디오 없음(§7.1 스프라이트 재사용 방침).

### 12.5 SO 스키마 / 수치 필드

SO 신규·변경 없음(전부 JSON). 필드는 §5.2 가 단일 진실이다. C# 데이터 행: `RelicRarity_ShardRow.craftCost`, `RelicConfigRow.equipSlotCount`, `RelicRow` 에서 `maxStack` 삭제, 세이브 `EquippedRelicEntry { int Slot; string RelicId; }`, `MetaProfile.EquippedRelics`/`RelicReworkDone`/`RelicReworkNoticePending`.

### 12.6 코드 접점 요약

| 위치 | 변경 |
|---|---|
| `MetaProfile.cs` / `MetaProfileMigration.cs` | §6. `CurrentVersion` 15, `GetRelicStack`/`AddRelic` 정리(보유 판정 `HasRelic`, 장착 헬퍼), `CopyFrom` 갱신 |
| `RelicDatabase.cs` / `RelicRows.cs` / `RelicText.cs` | 중첩 제거, 신규 kind 검증·인자 |
| `RelicDropRoller.cs` | §2.3 (`Candidates`/`TableOf` 통합, `GrantShard` → 분해, `RelicDropResult` 필드 정리, 자동 장착은 호출부가 아니라 이 순수 함수 안의 `profile` 변경으로 처리해 시뮬레이터가 재현 가능) |
| `RelicBattleBonus.cs` | 장착 기준 집계, 스택 제거, 신규 2종 스펙, `OpeningSummons`/`Frenzies` 삭제 |
| `MonsterBuffService.cs` / `BattleController.cs` | §4.3. `AttachRelicFrenzy`·`SummonRelicOpeners` 삭제, 오버레이 부착, `OpeningBuff` 타이머 |
| `RelicViewModel.cs` / `RelicPopup.cs` / `RelicCell.cs` / `RelicDetailView.cs` | §7.1. `ShardExchange*` 삭제 |
| `ResultPopup.cs` / `BattleController.cs`(~2020~2040행 `ResultPopupArg`) / `PlayDriver.Result.cs` | §7.2 |
| `DailyChallenge.cs` / `DailyViewModel.cs` / `DailyPopup.cs` / `DailyConfig` 행 | 스택 필드·표기 삭제, ComboKey |
| `CurrencySettlement.cs` / `GrowthDatabase.cs` / `GrowthRows.cs` | 교환 삭제(§8), `craftCost` 로드·검증 |
| **`PlayDriver.Sweep.cs` deny-list (Rule 05 §5)** | `"RelicPopup:CraftButton"` 추가(제작은 조각을 소모하는 되돌릴 수 없는 확정; 확인 팝업의 `ConfirmPopup:ConfirmButton` 은 이미 제외 중이나 같은 작업에서 갱신) |

---

## 13. 이번 범위 밖 (후속 제안으로만, 개별 승격 전까지 금지)

유물끼리의 합성 · 유물 강화(레벨업) · 판매·조각 환급 · 조각의 타 재화 환전 · 세트 효과 · 장착 칸 확장·프리셋 · 16종 완성 후 조각 소비처 · 전투 중 장착 변경 · 신규 유물(17종째) · 유물 알림 점 · 전투 HUD 유물 요약 · 전투 중 효과 상태 표시(예: 분노 단계 숫자) · ★ 목표 · 신규 몬스터 · 신규 스테이지 규칙.

---

## 14. 사용자 결정 목록 (HTML 하단 박스의 원본)

| ID | 결정 | 권장 | 대안 |
|---|---|---|---|
| U-1 | 새로 얻은(드랍·제작) 유물을 빈 칸에 자동 장착 | 한다 | 안 한다(도감에서만 장착) — 보장된 첫 유물이 효과 없이 시작 |
| U-2 | 미보유 유물도 이름·효과를 도감에서 공개(제작하려면 알아야 함) | 공개한다 | 한 번도 못 본 유물은 "???" 유지 — 제작 선택이 불가능해짐 |
| U-3 | 잔해 교환(조각 → 각성석·달빛 인장·심연 결정) 처리 | 폐기(조각은 제작 전용, 기존 잔해는 1:1 조각) | 유지하고 비율 재조정(§8 B) |

이미 확정(사용자와 합의): 중첩 폐기·고정값 · 자동 분해(1/3/8) · 제작 비용(10/18/32) · 장착 4칸·등급 제한 없음·도감에서만 변경 · 구세이브 이행(등급 높은 순 4개 자동 장착, 중첩 초과분 조각 환산) · 16종 효과값 · 신규 효과 2종.

---

## 15. Self-Review

- Placeholder 잔존: 미정 마커·애매한 권유·두 갈래 위임 0건(두 갈래는 U-1~U-3 으로 "사용자 선택 필요" 명시, 각 항목에 권장 안 지정). 수치 미확정 부분은 §11.2 에서 "qa-simulator 검증 후 결정 + 결정 메트릭"으로 명시.
- 내부 일관성: 효과 행 18 · 파라미터 행 31(합산식 §5.2) · 유물 16 · 등급 8/6/2 · 비용 10/18/32 = 10x1·6x3·4x8 · 조각 총 252 = 80+108+64 · 신규 문구 15개(1900~1914) · Version 15 가 §1/§5/§6/§9/§12 에서 동일.
- 명명 일관성: `OpeningBuff` · `LostHpEmpower` · `equipSlotCount` · `craftCost` · `EquippedRelics` · `EquippedRelicEntry` · `RelicReworkDone` · `RelicReworkNoticePending` · `Dissolved` · `RelicAutoEquipped` 를 본문 전체 동일 표기.
- 구현 요청사항 완전성: Enum(§12.2) · Interface(§12.3) · 에셋 키·파일(§12.4) · 스키마(§12.5) · deny-list(§12.6).
- UI 목업: `relic-rework.html` 에 도감 장착/제작 화면 포함(별도 `.mockups/` 파일 없음 - 이 HTML 이 승인용 목업).
