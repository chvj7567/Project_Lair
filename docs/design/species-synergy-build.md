# 종족 시너지 빌드 — 4축 시너지를 몬스터 6종족 시너지로 개편

> 작성 2026-11-22 · 단계 v0.3 · game-designer
> 입력: 사용자 확정 결정 8개(§1), `card-renewal.md`, `card-3pick-cap.md`, `card-synergy-indicator.md`, `monster-growth-currencies.md` §3·§15, `dungeon-formation-free.md`, QA `docs/qa-reports/2026-07-24.md`
> 사용자용 요약: `docs/design/species-synergy-build.html` · UI 목업: `.mockups/species-synergy-build.html`
> 수치·규칙의 단일 진실(SoT)은 이 문서다. 이 문서가 승인되면 `card-renewal.md`(4축·Layer 2 중첩), `card-3pick-cap.md`(3픽 캡), `card-synergy-indicator.md`(4칩·4행)의 해당 규칙을 대체한다(§18).

---

## § 헤더

- **목표**: 카드 빌드 시너지를 Tank/Dps/Debuff/Swarm 4축에서 **몬스터 6종족마다 1개**로 바꾸고, 패시브 48장(종족당 8장) · 액티브 10장(시너지 밖 "임시 스킬") · 1판 1회 픽 · 픽 이력 기반 추첨 보정 · 영웅 스킬 해금 HP 75/55/35% 를 묶어 "영웅이 강해지면 직후 내가 맞받아친다" 리듬을 만든다.
- **검증 가설**: 시너지가 "내가 키우는 몬스터" 와 같은 말이 되면 (1) 플레이어가 첫 픽에서 종족을 고르고 끝까지 밀어붙이는 의도적 빌드를 만들고, (2) 편성(어느 종족을 몇 칸)과 카드 빌드가 서로를 강화해 판마다 다른 선택이 생기며, (3) 영웅 3단계 스킬 직후 열리는 Tier3 가 "역전의 결정타" 로 체감된다.
- **현재 단계 범위 적합성**: **현재 §8 범위 밖 — 승격 필요.** CLAUDE.md §8 "몬스터 육성 재화 분리" ⑤ 가 카드 총수 28장 · 축당 7장 · Debuff 축 카드 구성을 고정하고 있다. §17 승격안을 사용자가 승인해야 구현에 들어간다. 신규 몬스터 · ★ 목표 · 신규 화면 · 메인 메뉴 금지는 그대로 유지한다.
- **핵심 메커니즘**:
  1. 시너지 = 6종족. 패시브 카드는 모두 한 종족에 속하고, 그 종족 패시브를 3/5/7장 고르면 Tier1/2/3 이 즉시 발동한다. 액티브 카드는 종족이 없고 시너지 장수에 들지 않는다.
  2. 모든 카드는 한 판에 한 번만 고를 수 있다. 패시브 3택 중 1장은 "지금까지 가장 많이 고른 종족" 의 남은 패시브에서 나온다(편성 무관, 픽 이력 기준).
  3. 영웅 스킬 75% → 내 Tier1(70%) → 스킬 55% → Tier2(50%) → 스킬 35% → Tier3(30%). Tier3 는 "즉시 사건 1개 + 영구 강화 1개" 로 된 결정타다.

---

## 1. 사용자 확정 결정 (변경 금지)

| # | 결정 | 이 문서에서 반영 위치 |
|---|---|---|
| D1 | 시너지 축 = 6종족(도깨비불 Wisp · 망령 Wraith · 사신 Reaper · 저주술사 Hex · 역병귀 Plague · 환령 Phantom)마다 1개. `EBuildAxis` 4축 폐기 | §3, §12, §16 |
| D2 | 카드 출현은 편성과 무관 — 편성에 없는 종족 카드도 똑같이 나온다 | §4 (보정 입력에 편성 없음) |
| D3 | 같은 카드는 한 판에 한 번만. 고른 카드는 다시 나오지 않는다(3픽 캡·중복 픽 누적 폐지) | §3.1 |
| D4 | 시너지 장수는 패시브만 센다. 액티브는 "임시 스킬" 로 시너지 제외 | §3.1, §7 |
| D5 | 임계 3/5/7장 = Tier1/2/3(기존 값). 가장 빠른 도달 시점은 HP 70/50/30%. 다른 종족은 2장까지만 섞을 수 있어 Tier3 는 한 종족 집중 보상이며, 2종족 동시 Tier3 불가 | §3.2, §3.3 |
| D6 | 패시브 = 종족당 8장 x 6 = 48장(7장 + 추첨 여유 1장). 카드 일러스트 없음, 아이콘은 종족별 6장 | §5, §11.3 |
| D7 | 영웅 스킬 해금 HP 85/65/45% → **75/55/35%** (`HeroArchetype_Skill.hpFraction`, 5직업 공통) | §9 |
| D8 | 추첨 보정 — 가장 많이 고른 종족의 패시브 1장 보장(픽 이력 기반), 동률·첫 픽·소진 처리 포함 | §4 |

---

## 2. 설계 결정 — 대안 비교

### 2.1 추첨 보정 방식 (D8)

| 안 | 내용 | 2번째 픽에 주력 종족이 1장 이상 보일 확률 | Tier3 가 HP30% 에 열릴 수 있는가 | trade-off |
|---|---|---|---|---|
| 보정 없음 (현행) | 48장 균등 3장 | 39.1% (`1 - C(40,3)/C(47,3) = 1 - 9880/16215`) | 6연속 39% 대 → 0.391^6 = 0.0036 | 집중이 사실상 불가 |
| B. 주력 종족 가중치 x3 | 주력 남은 카드 가중 3, 나머지 1 | 72.5% (`1 - 40/61 x 39/60 x 38/59`) | 6연속 0.725^6 = 0.145 이하(뒤로 갈수록 주력 잔여가 줄어 더 낮음) | "운이 좋으면 Tier3". 집중 의도가 결과로 이어지지 않는다 |
| **A. 주력 종족 1장 보장 (채택)** | 3장 중 1장 = 주력 종족 남은 패시브 균등, 나머지 2장 = 남은 전체 균등 | **100%** (주력 잔여 1장 이상일 때) | **항상 가능** (8장 중 7장을 연속 선택 가능) | Tier3 도달 = 플레이어 의도. 대신 "보장 카드가 마음에 안 들면" 고민이 생긴다 — 이것이 의도한 선택 압력 |
| C. Tier1 이후 2장 보장 | 주력 3장 이상이면 보장 2장 | 100% | 항상 가능 | 다른 종족 카드가 1장만 보여 탐색·전환이 거의 막힌다 |

- **채택 A.** 사용자 결정 D5 "한 종족에 몰아야 Tier3" 가 확률이 아니라 **의도로** 성립해야 한다. 보장 슬롯 수는 `BalanceConfig.passiveFocusSlots = 1` 로 데이터화해 게이트 SS3 결과에 따라 조정한다.
- A 의 결과로 종족 우선 플레이의 Tier3 도달률은 구조상 100% 에 가깝다. 이것이 원하는 상태인지는 §19 Q1 로 사용자에게 확인한다.

### 2.2 액티브 카드 풀 (D4)

| 안 | 내용 | 판정 |
|---|---|---|
| **A. 기존 시한 액티브 10장 (채택)** | 공포·출혈·무력화·던전의 점성·광폭화·피의 갈증·강철 의지·시간 정지·수호자의 분노·죽음의 표식. 영구 효과 2장(단단한 살갗·빠른 번식)은 종족 패시브로 이동 | "임시 스킬" 정의(모두 시한)와 일치. 한 판 액티브 최대 5회(30/90/150/210/270초) → 5번째 추첨 적격 `10 - 4 = 6 ≥ 3` |
| B. 기존 12장 유지 | 영구 2장도 액티브 | "임시 스킬" 정의와 충돌, 영구 종족 효과가 시너지 밖에 남는다 |
| C. 7장으로 축소 | 최소 적격(5번째 추첨 `7 - 4 = 3`) | 선택지 다양성 감소. 줄일 근거 없음 |

- 수호자의 분노(`Berserk`)는 도깨비불·망령 한정이었다. 액티브는 종족이 없어야 하므로 **모든 몬스터 대상**으로 바꾸고, 강철 의지(전체 -30% 15초)와 겹치지 않게 **-50% 6초** 로 짧고 강하게 만든다(§7).

### 2.3 패시브 카드 표시 (D6 — 일러스트 없음)

| 안 | 내용 | 판정 |
|---|---|---|
| **A. 패시브 48장 모두 종족 아이콘 카드 (채택)** | 아트 영역에 기존 몬스터 UI 아이콘(`MonsterIcons/{Name}.png`, 512 캔버스)을 크게 + 종족 색 후광. 재사용 18장의 기존 일러스트도 패시브 카드 화면에서는 쓰지 않음 | 48장이 한 문법으로 보인다. "이 카드 = 이 몬스터" 가 그림으로 바로 읽힌다. 신규 아트 0 |
| B. 재사용 18장은 일러스트, 신규 30장만 아이콘 | 기존 자산 활용 | 같은 종족 카드가 두 가지 모양으로 섞여 보인다 |
| C. 종족 문장 아이콘 6장 신규 제작 | 전용 엠블럼 | §8 "몬스터 UI 아이콘 교체" 항목이 신규 아이콘 파일을 금지. 별도 승격·아트 작업 필요 |

- 기존 4축 시너지 아이콘(`SynergyIcons/{TANK,DPS,DEBUFF,SWARM}.png`)은 사용처가 사라진다(삭제는 사용자 확인 후, §19 Q6).

### 2.4 카드 → 종족 소속 데이터

| 안 | 내용 | 판정 |
|---|---|---|
| A. `Card.axis` 에 종족 값 + 액티브는 `None` | 필드 하나 | 액티브에 sentinel(`None`) — Rule 02 §11-1 위반 |
| **B. 선택적 관계 `Card_Species` 재정의 (채택)** | 패시브 카드만 1행(`cardId` PK → `monster`). 액티브는 행 없음. `Card.axis` 필드 삭제 | sentinel 없음. 기존 `Card_Species.json` 은 런타임에서 읽는 곳이 없고(로더가 `FormationCardSpecies.Current` 에 대입만, 2026-11-22 grep), 이름이 "카드의 종족" 그대로라 재정의가 가장 짧다 |
| C. 새 관계 `Card_Synergy` 신설 + 구 `Card_Species` 삭제 | 이름이 "시너지" 를 드러냄 | EData·로더·검증을 새로 만든다. B 대비 이득 없음 |

### 2.5 전투 HUD 시너지 패널 6행 수납

| 안 | 내용 | 판정 |
|---|---|---|
| **A. 행 압축 170x34 (채택)** | 2줄 구성 유지(아이콘·이름·장수 / 7칸 트랙), 트랙 높이 7 → 5 | 6행 고정 위치 — 종족 자리가 판마다 같아 학습이 쉽다. 패널 높이 247 유지: `14(헤더) + 6(간격) + 6 x 34 + 5 x 4 = 244 ≤ 247` |
| B. 장수 1 이상 종족만 표시 | 행 수 가변 | 9픽 기준 최대 6종 모두 1장 이상이 될 수 있어 상한 해결이 안 되고, 행 위치가 판마다 바뀐다 |
| C. 패널 높이 확장 | 44 높이 유지 시 `14 + 6 + 6 x 44 + 5 x 6 = 314` | 우상단에서 전투 시야를 67 더 가린다 |

---

## 3. 규칙

### 3.1 픽 · 장수

- **1판 1회 픽**: 패시브·액티브 모두, 한 번 고른 카드는 그 판의 이후 추첨 후보에서 제외된다(기존 `CardPickCounter` 상한 3 → 1). 중복 픽 누적(Layer 2)은 없다 — 카드 1장의 수치가 곧 최종값이다.
- **시너지 장수** `c(s)` = 그 판에서 고른 **패시브** 카드 중 종족 `s` 에 속한 장수. 액티브 픽은 어느 종족에도 더하지 않는다.
- **발동**: `c(s)` 가 3·5·7 이 되는 그 픽 직후 해당 Tier 효과를 1회 적용한다(카드 효과 적용 → 장수 +1 → Tier 판정 순, 기존 `ApplyCardEffect` 순서 유지). Tier 효과는 영구이며 누적(Tier2 도달 시 Tier1 유지 + Tier2 추가).
- **최대 Tier**: 한 판 패시브 최대 9장 → 종족당 최대 Tier3.

### 3.2 패시브 9장 조합표 (D5 검산)

| 장수 배분 (내림차순) | 열리는 시너지 | 비고 |
|---|---|---|
| 9 / 8+1 / 7+2 / 7+1+1 | 1종족 Tier3 | 8장 이상은 Tier3 위 추가 효과 없음 |
| 6+3 / 5+4 / 5+3+1 | Tier2 1개 + Tier1 1개 | 5+5 = 10 > 9 → **2종족 동시 Tier2 불가** |
| 6+2+1 / 5+2+2 / 5+2+1+1 | Tier2 1개 | |
| 4+4+1 / 4+3+2 / 3+3+3 | Tier1 2~3개 | 3+3+3 이 Tier1 최대 개수(3개) |

- 7+7 = 14 > 9 → **2종족 동시 Tier3 불가**(D5).

### 3.3 페이싱 — 가장 빠른 시점과 섞었을 때

패시브 k번째 픽은 영웅 HP `(100 - 10k)%` 에서 열린다(`BalanceConfig.passiveThresholds` 0.9~0.1).

| 영웅 HP | 사건 (한 종족만 연속 선택) |
|---|---|
| 90% / 80% | 패시브 1·2번째 |
| **75%** | **영웅 스킬 1단계** |
| **70%** | 패시브 3번째 → **Tier1** |
| 60% | 패시브 4번째 |
| **55%** | **영웅 스킬 2단계** |
| **50%** | 패시브 5번째 → **Tier2** |
| 40% | 패시브 6번째 |
| **35%** | **영웅 스킬 3단계** |
| **30%** | 패시브 7번째 → **Tier3 (결정타)** |
| 20% / 10% | 패시브 8·9번째 (다른 종족 2장을 여기서 섞으면 Tier3 시점이 지켜진다) |

- **다른 종족을 섞으면 그 장수만큼 Tier 가 10%p 씩 늦어진다.** 예: 2번째 픽을 다른 종족으로 고르면 Tier1 60% · Tier2 40% · Tier3 20%. 두 장을 앞에서 섞으면 Tier3 는 HP 10%(마지막 픽)에 열린다 — 결정타 리듬은 깨지지만 Tier3 효과(§6) 중 소환·영구 강화는 남은 10% 를 깎는 데 그대로 쓰인다. 리듬을 지키려면 다른 종족 카드는 8·9번째에 고른다 — 이것이 "다른 종족은 2장까지" 의 실제 의미다.
- 액티브는 30/90/150/210/270초에 5회(`BalanceConfig.activeThresholds`). 2026-07-24 QA 기준 영웅 처치 중앙값 124초라 한 판 액티브는 보통 2~3장이다.

---

## 4. 추첨 보정 — 패시브 (확정 알고리즘)

입력: 적격 패시브 집합 `E`(이 판에서 아직 안 고른 패시브), 종족별 장수 `c(s)`, 종족별 마지막 선택 순번 `last(s)`(그 종족 패시브를 마지막으로 고른 픽 번호, 없으면 없음), 덱의 시드 난수 1개(데일리는 날짜 시드, 그 외 무작위 — 기존 `CardDeck` 난수 그대로). **편성은 입력이 아니다(D2).**

1. **첫 패시브 추첨**(`c(s)` 전부 0): 슬롯 1→2→3 순으로, "이번 추첨에 아직 나오지 않은 종족" 의 `E` 카드 중 균등 1장. → 첫 3택은 항상 서로 다른 3종족 = 첫 픽이 곧 종족 선택.
2. **2번째 이후 추첨**:
   1. 주력 종족 `L` = `E` 에 남은 카드가 1장 이상인 종족 중 `c(s)` 최대. 동률이면 `last(s)` 가 가장 큰(가장 최근에 고른) 종족.
   2. `L` 이 있으면: 슬롯 1 = `E ∩ L` 균등 1장(`passiveFocusSlots` 가 1 보다 크면 그 수만큼 반복). 없으면(고른 종족 전부 소진) 보정 없음.
   3. 남은 슬롯 = `E` 에서 이미 뽑힌 카드를 뺀 전체 균등. 주력 종족 카드가 다시 나올 수 있다(주력 2장 중 고르는 선택).
3. **표시 순서** = 추첨 순서. 보장 카드는 항상 왼쪽 첫 칸이다("왼쪽 = 내 주력 종족" 으로 학습되게).
4. **후보 부족**: `E` 가 3장 미만이면 가능한 만큼(기존 graceful fallback). 48장 · 9픽이라 정상 판에서는 발생하지 않는다(9번째 추첨 적격 40장).
5. **소진**: 주력 종족 8장을 다 고르면 그 종족은 `L` 후보에서 빠지고 다음 장수 종족이 주력이 된다. 9픽 구조상 9번째 추첨에서만 생긴다.
6. **재현성**: 난수 호출 순서는 슬롯 1 → 2 → 3 고정. 같은 시드 + 같은 선택 → 같은 카드(데일리 D-5 유지).
7. **액티브 추첨**: 보정 없음. 적격 액티브(이 판에서 안 고른 것) 중 균등 3장.

동률 예시: 1번째 도깨비불, 2번째 사신 → `c` 1:1 동률 → 더 최근인 사신이 주력. 한 장으로 주력을 갈아탈 수 있어 초반 전환 비용이 낮다.

---

## 5. 패시브 카드 48장

### 5.1 구성 원칙

- 종족당 8칸을 같은 틀로 채운다: ① HP ② 공격력 ③ 둥지 출력 +1 ④ 둥지 소환 주기 ⑤ 역할 스탯 ⑥ 보조 스탯 ⑦·⑧ 종족 고유 메커니즘. 같은 칸끼리 비교가 쉬워 학습이 1회로 끝난다.
- **대상은 자기 종족만.** 기존 2종족 카드(공포의 군세·처형 명령·단단한 살갗)는 한 종족으로 좁힌다.
- **종족 특성(`monster-growth-currencies` §3.4)과 같은 메커니즘 금지**: 가시 반사(도깨비불) · 사망 시 공격력 버프(망령) · HP 30% 이하 추가 피해(사신) · 받는 피해 누적(저주술사) · 사망 시 독 전염(역병귀) · 첫 피격 회피(환령)는 카드에 넣지 않았다. 특성 보유를 전제로 한 카드도 없다(신규 유저에게 빈 카드 금지).
- 카드 1장 수치 = 최종값(중복 픽 없음). 초기값이며 §15 게이트로 확정한다.
- `SpeciesStat` 의 파라미터 키는 `EMonsterStatKind` 값명이다(예 `Hp` 1.5). 대상 종족은 `Card_Species` 행에서 읽는다.

### 5.2 카드 표

열: 상태 = 재사용(기존 ID) / 신규. 키 = nameKey/descKey(`Strings_Ko`). 설명 = descKey 의 새 문구(재사용 카드도 이 문구로 교체).

**도깨비불 Wisp — "불꽃 떼: 값싼 몸으로 영웅을 감싸 태운다"**

| # | ECardId | 상태 | 이름 | 설명 | effectKind | 파라미터 | 키 |
|---|---|---|---|---|---|---|---|
| 1 | `WispHpBoost` | 재사용 | 끈질긴 도깨비불 | 모든 도깨비불 HP +50% | `SpeciesStat` | `Hp` 1.5 | 1632/1660 |
| 2 | `WispPowerBoost` | 신규 | 불꽃 손톱 | 모든 도깨비불 공격력 +30% | `SpeciesStat` | `Power` 1.3 | 1900/1930 |
| 3 | `SpawnWisps` | 재사용 | 도깨비불 떼 | 도깨비불 둥지마다 한 번에 1마리 더 소환 | `SpeciesSpawnOutput` | 없음 | 1638/1666 |
| 4 | `WispSpawnHaste` | 신규 | 도깨비 화로 | 도깨비불 둥지 소환 주기 -25% | `SpeciesSpawnPeriod` | `periodMul` 0.75 | 1901/1931 |
| 5 | `WallOfWisps` | 재사용 (액티브→패시브, 대상 도깨비불만) | 단단한 살갗 | 도깨비불이 받는 피해 -25% | `SpeciesStat` | `DamageTaken` 0.75 | 1656/1684 |
| 6 | `WispMoveSpeed` | 신규 | 들불 | 모든 도깨비불 이동속도 +35% | `SpeciesStat` | `MoveSpeed` 1.35 | 1902/1932 |
| 7 | `WispEmber` | 신규 | 불씨 폭발 | 도깨비불이 쓰러질 때 가까운 영웅에게 공격력 3배 피해 | `SpeciesDeathBurst` | `radius` 2.0, `powerRatio` 3 | 1903/1933 |
| 8 | `WispDazzle` | 신규 | 도깨비 홀림 | 도깨비불에게 맞은 영웅의 공격 간격 +15% (2초) | `SpeciesHitHeroAtkSlow` | `cdMul` 1.15, `duration` 2 | 1904/1934 |

**망령 Wraith — "철벽: 버티며 영웅의 발을 묶는다"**

| # | ECardId | 상태 | 이름 | 설명 | effectKind | 파라미터 | 키 |
|---|---|---|---|---|---|---|---|
| 1 | `WraithDamageBoost` | 재사용 | 망령의 압박 | 모든 망령 HP +50% | `SpeciesStat` | `Hp` 1.5 | 1633/1661 |
| 2 | `ReplaceWispsToWraith` | 재사용 (대상 망령만) | 공포의 군세 | 모든 망령 공격력 +30% | `SpeciesStat` | `Power` 1.3 | 1643/1671 |
| 3 | `SpawnWraith` | 재사용 | 더 많은 망령 | 망령 둥지마다 한 번에 1마리 더 소환 | `SpeciesSpawnOutput` | 없음 | 1639/1667 |
| 4 | `WraithSpawnHaste` | 신규 | 원혼의 둥지 | 망령 둥지 소환 주기 -25% | `SpeciesSpawnPeriod` | `periodMul` 0.75 | 1905/1935 |
| 5 | `WraithKnockback` | 신규 | 꿈쩍 않는 망령 | 망령이 밀려나는 거리 -50% | `SpeciesStat` | `KnockbackTaken` 0.5 | 1906/1936 |
| 6 | `WraithArmor` | 신규 | 망령 갑주 | 망령이 받는 피해 -20% | `SpeciesStat` | `DamageTaken` 0.8 | 1907/1937 |
| 7 | `WraithChain` | 신규 | 원혼의 사슬 | 망령에게 맞은 영웅 1초 속박 (8초에 한 번) | `SpeciesHitRoot` | `rootDuration` 1.0, `cooldown` 8 | 1908/1938 |
| 8 | `WraithWard` | 신규 | 서늘한 기운 | 망령 곁 몬스터가 받는 피해 -15% | `SpeciesGuardAura` | `radius` 3.0, `takenMul` 0.85 | 1909/1939 |

**사신 Reaper — "난도질: 빠르게 붙어 몰아친다"**

| # | ECardId | 상태 | 이름 | 설명 | effectKind | 파라미터 | 키 |
|---|---|---|---|---|---|---|---|
| 1 | `ReaperHpBoost` | 신규 | 사신의 맷집 | 모든 사신 HP +50% | `SpeciesStat` | `Hp` 1.5 | 1910/1940 |
| 2 | `ReaperPowerBoost` | 신규 | 날 선 낫 | 모든 사신 공격력 +30% | `SpeciesStat` | `Power` 1.3 | 1911/1941 |
| 3 | `SpawnReapers` | 재사용 | 사신 떼거리 | 사신 둥지마다 한 번에 1마리 더 소환 | `SpeciesSpawnOutput` | 없음 | 1640/1668 |
| 4 | `ReaperSpawnHaste` | 신규 | 밤의 둥지 | 사신 둥지 소환 주기 -25% | `SpeciesSpawnPeriod` | `periodMul` 0.75 | 1912/1942 |
| 5 | `ReaperAtkSpeed` | 재사용 | 신속한 사신 | 모든 사신 공격 간격 -30% | `SpeciesStat` | `Cooldown` 0.7 | 1634/1662 |
| 6 | `ReaperMoveSpeed` | 신규 | 그림자 질주 | 모든 사신 이동속도 +30% | `SpeciesStat` | `MoveSpeed` 1.3 | 1913/1943 |
| 7 | `ReaperCombo` | 신규 | 연속 베기 | 사신이 영웅을 4번 때릴 때마다 4번째 피해 2.5배 | `SpeciesComboStrike` | `hitCount` 4, `dmgMul` 2.5 | 1914/1944 |
| 8 | `ReaperPack` | 신규 | 피 냄새 | 영웅 곁 사신 1마리당 사신 피해 +6% (최대 +30%) | `SpeciesPackPower` | `radius` 2.0, `perMul` 0.06, `maxCount` 5 | 1915/1945 |

**저주술사 Hex — "포격: 멀리서 정확히 꿰뚫는다"**

| # | ECardId | 상태 | 이름 | 설명 | effectKind | 파라미터 | 키 |
|---|---|---|---|---|---|---|---|
| 1 | `HexHpBoost` | 신규 | 저주술사의 결계 | 모든 저주술사 HP +50% | `SpeciesStat` | `Hp` 1.5 | 1916/1946 |
| 2 | `ReplaceReapersToHex` | 재사용 (대상 저주술사만, 이름 변경) | 저주 증폭 | 모든 저주술사 공격력 +30% | `SpeciesStat` | `Power` 1.3 | 1644/1672 |
| 3 | `SpawnHexes` | 신규 | 저주 학파 | 저주술사 둥지마다 한 번에 1마리 더 소환 | `SpeciesSpawnOutput` | 없음 | 1917/1947 |
| 4 | `HexSpawnHaste` | 신규 | 저주 공방 | 저주술사 둥지 소환 주기 -25% | `SpeciesSpawnPeriod` | `periodMul` 0.75 | 1918/1948 |
| 5 | `HexRangeBoost` | 재사용 | 저주의 시야 | 모든 저주술사 사거리 +40% | `SpeciesStat` | `Range` 1.4 | 1635/1663 |
| 6 | `HexCastSpeed` | 신규 | 빠른 영창 | 모든 저주술사 공격 간격 -20% | `SpeciesStat` | `Cooldown` 0.8 | 1919/1949 |
| 7 | `HexDoubleCast` | 신규 | 저주탄 분열 | 저주술사 공격이 25% 확률로 한 발 더 | `SpeciesExtraShot` | `chance` 0.25 | 1920/1950 |
| 8 | `HexFarShot` | 신규 | 원거리 저주 | 4칸 넘게 떨어져 맞히면 저주술사 피해 +35% | `SpeciesRangeBonus` | `minDistance` 4.0, `dmgMul` 1.35 | 1921/1951 |

> "처형 명령" → "저주 증폭" 개명: 사신 특성 "처형"(HP 30% 이하 피해)과 이름이 겹치고, 효과가 저주술사 공격력이 되었기 때문.

**역병귀 Plague — "역병: 독과 저주로 갉아낸다"**

| # | ECardId | 상태 | 이름 | 설명 | effectKind | 파라미터 | 키 |
|---|---|---|---|---|---|---|---|
| 1 | `PlagueHpBoost` | 신규 | 역병귀의 몸 | 모든 역병귀 HP +50% | `SpeciesStat` | `Hp` 1.5 | 1922/1952 |
| 2 | `PlaguePowerBoost` | 신규 | 썩은 발톱 | 모든 역병귀 공격력 +30% (독 피해도 함께 오름) | `SpeciesStat` | `Power` 1.3 | 1923/1953 |
| 3 | `SpawnPlagues` | 재사용 | 역병 증식 | 역병귀 둥지마다 한 번에 1마리 더 소환 | `SpeciesSpawnOutput` | 없음 | 1641/1669 |
| 4 | `PlagueSpawnHaste` | 신규 | 역병 둥지 | 역병귀 둥지 소환 주기 -25% | `SpeciesSpawnPeriod` | `periodMul` 0.75 | 1924/1954 |
| 5 | `PlagueSlowBoost` | 재사용 | 역병의 손길 | 역병귀 독 피해 +40% | `SpeciesStat` | `PoisonDamage` 1.4 | 1636/1664 |
| 6 | `LingeringCurse` | 재사용 | 끈질긴 저주 | 영웅에게 걸린 디버프 지속 +25% | `LingeringCurse` | `durationBonus` 0.25 | 1659/1687 |
| 7 | `HeroAttackDown` | 재사용 | 약화의 저주 | 영웅 공격력 -25% (영구) | `HeroAttackDown` | `factor` 0.75 | 1645/1673 |
| 8 | `PlagueMoveSpeed` | 신규 | 역병의 발걸음 | 모든 역병귀 이동속도 +30% | `SpeciesStat` | `MoveSpeed` 1.3 | 1925/1955 |

- 끈질긴 저주·약화의 저주는 역병귀가 편성에 없어도 효과가 있다(영웅 대상). 역병귀 빌드가 "역병귀 1칸" 편성에서도 성립하는 이유다.
- 역병의 발걸음은 2026-11-01 독 전환에서 우려된 "카이팅 영웅(궁수·암살자) 상대 독이 안 붙는" 문제(`monster-growth-currencies` §15.6 PQ2)의 카드 쪽 해답이다.

**환령 Phantom — "분열: 수로 밀어붙이고 끝없이 다시 나온다"**

| # | ECardId | 상태 | 이름 | 설명 | effectKind | 파라미터 | 키 |
|---|---|---|---|---|---|---|---|
| 1 | `PhantomHpBoost` | 신규 | 환령의 그림자 | 모든 환령 HP +50% | `SpeciesStat` | `Hp` 1.5 | 1926/1956 |
| 2 | `PhantomPowerBoost` | 신규 | 환령의 이빨 | 모든 환령 공격력 +40% | `SpeciesStat` | `Power` 1.4 | 1927/1957 |
| 3 | `SpawnPhantoms` | 재사용 | 환령 떼 | 환령 둥지마다 한 번에 1마리 더 소환 | `SpeciesSpawnOutput` | 없음 | 1642/1670 |
| 4 | `Multiply` | 재사용 (액티브→패시브) | 빠른 번식 | 환령 둥지 소환 주기 -30% | `SpeciesSpawnPeriod` | `periodMul` 0.7 | 1651/1679 |
| 5 | `PhantomMoveSpeedBoost` | 재사용 | 환령의 발걸음 | 모든 환령 이동속도 +50% | `SpeciesStat` | `MoveSpeed` 1.5 | 1637/1665 |
| 6 | `PhantomCooldown` | 신규 | 환령의 손톱 | 모든 환령 공격 간격 -25% | `SpeciesStat` | `Cooldown` 0.75 | 1928/1958 |
| 7 | `SpawnerHaste` | 재사용 | 던전의 박동 | 모든 둥지 소환 주기 -20% | `SpawnerHaste` | `periodMul` 0.8 | 1658/1686 |
| 8 | `PhantomSplit` | 신규 | 갈라지는 그림자 | 환령이 쓰러질 때 30% 확률로 1마리 다시 나타남 | `SpeciesDeathSplit` | `chance` 0.3 | 1929/1959 |

- 환령의 공격력·공격 간격 카드 2장은 QA 2026-07-24 진단("환령 전군 딜 점유율 4.3% — 강화해도 처치 시간 4.8% 단축")에 대한 보정이다. 환령 공격력 2 는 6종 최저라 공격력 카드만 +40%(타 종족 +30%).
- 던전의 박동(전 둥지)은 환령 카드지만 모든 종족을 돕는다 — 다른 종족을 섞는 빌드의 연결 카드.
- 빠른 번식 x0.7: 환령 기본 주기 6초 → 4.2초. 다른 종족 주기 카드(x0.75)보다 한 단계 강한 것은 환령 개체 화력이 가장 낮기 때문.

### 5.3 신규 메커니즘 규칙 (카드 ⑦·⑧)

| effectKind | 규칙 | 겹침·한계 |
|---|---|---|
| `SpeciesDeathBurst` | 대상 종족 몬스터가 쓰러질 때 영웅이 `radius` 안이면 그 몬스터 **최종 공격력 x `powerRatio`** 피해 1회. 영웅의 받는 피해 배율(`BossBulwark` 등)을 그대로 곱한다 | 전투 중 모든 사망(Tier3 소환 개체 포함)에서 발동. 피해 숫자는 기존 피해 숫자 경로 |
| `SpeciesHitHeroAtkSlow` | 대상 종족 기본 공격 적중 시 영웅 기본 공격 간격 x `cdMul`, `duration` 초. 중첩 없음, 재적중 시 지속만 `duration` 으로 갱신 | 디버프 저항 대상(§10.3). 상태 아이콘 = 이 카드 `_icon` |
| `SpeciesHitRoot` | 대상 종족 기본 공격 적중 시 영웅 이동 불가 `rootDuration` 초(공격·스킬 시전은 가능). 같은 영웅에게 `cooldown` 초에 최대 1회(속박이 끝난 시점이 아니라 걸린 시점부터 계산) | 디버프 저항 대상. 공포·시간 정지 중에 걸린 속박은 그 효과가 끝난 뒤 남은 시간만 적용(공포 도주를 막지 않는다) |
| `SpeciesGuardAura` | 대상 종족 몬스터 각각의 `radius` 안 **다른** 몬스터(대상 종족 포함, 자기 자신 제외)의 받는 피해 x `takenMul`. 여러 망령 범위가 겹쳐도 1회만 적용 | 받는 피해 곱 표면(각성 `DamageTaken` 과 같은 곱 합성 — 덮어쓰기 금지, `monster-growth-currencies` §3.2) |
| `SpeciesComboStrike` | 대상 종족 몬스터 개체마다 영웅 적중 횟수를 센다. `hitCount` 번째 적중 피해 x `dmgMul` 후 카운트 0. 개체가 사라지면 카운트도 사라짐 | 사신 특성 처형(HP 30% 이하)과 곱으로 합성 |
| `SpeciesPackPower` | 대상 종족 몬스터가 영웅에게 피해를 줄 때, 그 순간 영웅 `radius` 안 대상 종족 수 `n`(자신 포함) 기준 피해 x `(1 + perMul x min(n, maxCount))` | n 은 적중 순간 1회 계산 |
| `SpeciesExtraShot` | 대상 종족 공격 발사 시 `chance` 확률로 같은 피해·같은 대상의 투사체 1발 추가(추가 발은 다시 판정하지 않음) | 저주 누적 특성 중첩은 추가 발도 1회로 센다(적중 1회) |
| `SpeciesRangeBonus` | 대상 종족 공격이 영웅에게 맞을 때 발사 위치~영웅 거리 ≥ `minDistance` 면 피해 x `dmgMul` | 짙은 안개 규칙으로 사거리가 줄면 덜 발동 — 규칙 우선 |
| `SpeciesDeathSplit` | 대상 종족 몬스터가 쓰러질 때 `chance` 확률로 그 위치에 같은 등급·같은 강화 티어 1마리 소환. 분열로 나온 개체는 다시 분열하지 않는다 | 둥지와 무관한 소환(봉인 둥지와 무관). 필드 개체 상한은 현재 없음(`tank-tier3-renewal.md` 캡 제거) |

---

## 6. 종족 시너지 18단계

### 6.1 표

설계 틀: Tier1 = 그 종족의 핵심 수치 · Tier2 = 수·템포 · **Tier3 = 즉시 사건 1개 + 영구 강화 1개**(결정타). 소환은 영웅 주위 반경 3.0 원 위에 균등 간격, 종족 보유 최고 등급 + 현재 강화 티어로 나온다(편성에 없는 종족이어도 소환된다 — 그 종족 집중이 빈 결과가 되지 않게).

| 종족 | Tier1 (3장) | Tier2 (5장) | Tier3 (7장) — 결정타 |
|---|---|---|---|
| 도깨비불 | **불씨** 도깨비불 공격력 +25% (`SpeciesStat Power 1.25`) | **불길** 도깨비불 둥지마다 1마리 더 (`SpeciesSpawnOutput`) | **불바다** 도깨비불 7마리 즉시 소환 (`SpeciesSummon count 7`) + 도깨비불 공격 간격 -25% (`SpeciesStat Cooldown 0.75`) |
| 망령 | **서린 한** 망령 HP +30% (`SpeciesStat Hp 1.3`) | **원혼의 벽** 망령 공격력 +25% (`SpeciesStat Power 1.25`) | **원혼 강림** 망령 3마리 즉시 소환 (`SpeciesSummon count 3`) + 망령 받는 피해 -40% (`SpeciesStat DamageTaken 0.6`) |
| 사신 | **핏빛 낫** 사신 공격력 +25% (`SpeciesStat Power 1.25`) | **피의 춤** 사신 공격 간격 -20% (`SpeciesStat Cooldown 0.8`) | **사신의 밤** 사신 3마리 즉시 소환 (`SpeciesSummon count 3`) + 사신 이동속도 +40% (`SpeciesStat MoveSpeed 1.4`) |
| 저주술사 | **저주 각인** 저주술사 공격력 +25% (`SpeciesStat Power 1.25`) | **먼 저주** 저주술사 사거리 +30% (`SpeciesStat Range 1.3`) | **저주 폭풍** 즉시 영웅 최대 HP 5% 피해, 이후 6초마다 반복 (`HexCurseStorm maxHpRatio 0.05, period 6`) |
| 역병귀 | **고인 독** 독 최대 중첩 +2 (3 → 5) (`PlagueToxinMaxStack bonus 2`) | **쇠약** 영웅 공격력 -15% 영구 (`HeroAttackDown factor 0.85`) | **역병의 끝** 영구 출혈 — 이동 중 1초마다 최대 HP 1% (`EternalBleed ratio 0.01`) + 독 피해 +50% (`SpeciesStat PoisonDamage 1.5`) |
| 환령 | **바람 그림자** 환령 이동속도 +30% (`SpeciesStat MoveSpeed 1.3`) | **그림자 무리** 환령 둥지마다 1마리 더 (`SpeciesSpawnOutput`) | **환령 폭주** 환령 14마리 즉시 소환 (`SpeciesSummon count 14`) + 모든 둥지 소환 주기 -15% (`SpawnerHaste periodMul 0.85`) |

- 역병귀 3단계는 기존 Debuff 시너지(독 최대 중첩 +2 · 약화 x0.85 · 영구 출혈)를 그대로 옮기고 Tier3 에 독 피해 +50% 를 더했다.
- 저주 폭풍: 매 반복 시점에 필드에 저주술사가 1마리 이상 있을 때만 피해(없으면 그 회차 건너뜀, 주기 계속). 피해는 영웅 받는 피해 배율을 곱한다. 첫 피해는 발동 즉시(저주술사 0마리여도 즉시분은 준다 — 발동 순간의 체감 보장).

### 6.2 Tier3 추가 화력 검산 (카드·육성 보정 전 기본값)

영웅 HP 4000(Easy), 잔여 30% = 1200. 기준선 전군 평균 화력 = `4000 ÷ 124초 = 32.3/초`(QA 2026-07-24 median).

| 종족 | Tier3 즉시 사건의 초당 추가 피해 | 산식 |
|---|---|---|
| 도깨비불 | 35.0 | 7 마리 x 공격력 5 ÷ 간격 1.0 |
| 망령 | 30.0 | 3 x 10 ÷ 1.0 (+ 탱킹) |
| 사신 | 36.0 | 3 x 6 ÷ 0.5 |
| 저주술사 | 33.3 (+ 즉시 200) | 4000 x 0.05 ÷ 6 |
| 역병귀 | 40.0 (이동 중) | 4000 x 0.01 ÷ 1 |
| 환령 | 35.0 | 14 x 2 ÷ 0.8 |

- 6종 30.0~40.0/초 — 최대/최소 1.33. Tier3 는 기준 화력의 0.93~1.24배를 더해 남은 1200 을 기준 37.2초(`1200 ÷ 32.3`)에서 16.6~19.3초(`1200 ÷ (32.3 + 40)` ~ `1200 ÷ (32.3 + 30)`)로 줄이는 크기다. 최종값은 게이트 SS4.
- 저주 폭풍·영구 출혈은 영웅 최대 HP 비례라 난이도·탑 층에서 상대 크기가 유지되고, 소환형은 영웅 HP 가 커질수록 상대 크기가 작아진다 → 게이트 SS8 로 탑에서 확인.

---

## 7. 액티브 카드 10장 — "임시 스킬"

| # | ECardId | 이름 | 효과 | 파라미터 | 변경 |
|---|---|---|---|---|---|
| 1 | `Fear` | 공포 | 영웅 3초 도주 | `duration` 3 | 없음 |
| 2 | `Bleed` | 출혈 | 영웅 이동 시 HP 감소 10초 | `ratio` 0.02, `duration` 10 | 없음 |
| 3 | `Weaken` | 무력화 | 영웅 피해 -50% 10초 | `factor` 0.5, `duration` 10 | 없음 |
| 4 | `Slow` | 던전의 점성 | 영웅 이동 -50%, 모든 몬스터 이동 +30% 10초 | `heroFactor` 0.5, `monsterMul` 1.3, `duration` 10 | 없음 |
| 5 | `Frenzy` | 광폭화 | 모든 몬스터 공속 +50% 10초 | `duration` 10 | 없음 |
| 6 | `BloodThirst` | 피의 갈증 | 처치 시 주변 몬스터 회복 30초 | `duration` 30 | 없음 |
| 7 | `IronWill` | 강철 의지 | 모든 몬스터 받는 피해 -30% 15초 | `duration` 15 | 없음 |
| 8 | `TimeStop` | 시간 정지 | 영웅 5초 정지 | `duration` 5 | 없음 |
| 9 | `Berserk` | 수호자의 분노 | **모든 몬스터** 받는 피해 -50% **6초** | `duration` 6 | 대상 도깨비불·망령 → 전체, 15초 → 6초 |
| 10 | `MarkOfDeath` | 죽음의 표식 | 영웅 받는 피해 +50% 5초 | `dmgTakenMul` 1.5, `duration` 5 | 없음 |

- 모든 액티브는 종족이 없다(`Card_Species` 행 없음) → 시너지 장수 0, 카드 라벨 "임시 스킬".
- 한 판 액티브 최대 5회 · 1회 픽 → 5번째 추첨 적격 `10 - 4 = 6 ≥ 3`.
- 설명 문구: `Berserk` descKey 1683 만 "모든 몬스터 받는 피해 -50% (6초)" 로 교체. 나머지 액티브 문구 불변.

---

## 8. 기존 28장 처분표

| 기존 카드 | 기존 축/트리거 | 처분 |
|---|---|---|
| WispHpBoost · SpawnWisps | Tank P · Swarm P | 도깨비불 패시브 |
| WraithDamageBoost · SpawnWraith | Tank P | 망령 패시브 |
| ReplaceWispsToWraith | Tank P (도깨비불·망령 공격력) | 망령 패시브, 망령만 |
| WallOfWisps | Tank A (영구) | 도깨비불 패시브, 도깨비불만 |
| ReaperAtkSpeed · SpawnReapers | Dps P | 사신 패시브 |
| HexRangeBoost | Dps P | 저주술사 패시브 |
| ReplaceReapersToHex | Dps P (사신·저주술사 공격력) | 저주술사 패시브, 저주술사만, 이름 "저주 증폭" |
| PlagueSlowBoost · SpawnPlagues · LingeringCurse · HeroAttackDown | Debuff P | 역병귀 패시브 |
| PhantomMoveSpeedBoost · SpawnPhantoms · SpawnerHaste | Swarm P | 환령 패시브 |
| Multiply | Swarm A (영구) | 환령 패시브 |
| Fear · Bleed · Weaken · Slow · Frenzy · BloodThirst · IronWill · TimeStop · MarkOfDeath | 각 축 A | 액티브(임시 스킬) 유지 |
| Berserk | Tank A | 액티브 유지, 전체 대상 · 6초 |
| HeroPoisonAura | (2026-11-01 폐기) | 자리 보존 그대로 |

검산: 패시브로 가는 기존 카드 = 16(기존 패시브 전부) + 2(WallOfWisps · Multiply) = 18. 액티브 유지 10. 18 + 10 = 28 ✓. 신규 30 → 패시브 18 + 30 = 48 ✓.

---

## 9. 영웅 스킬 해금 HP 75/55/35% (D7)

- `HeroArchetype_Skill.json` 15행의 `hpFraction` 값만 바꾼다: phaseIndex 1 → 0.75, 2 → 0.55, 3 → 0.35. 스키마 변경 없음. 5직업 공통.
- **영향**: 영웅이 1·2·3단계 스킬을 각각 HP 10%p 늦게 얻는다 → 영웅 생존 구간에서 스킬 가동 시간이 줄어 처치가 빨라지는 방향. 크기는 게이트 SS5.
- **같은 타격에 두 임계를 동시에 넘을 때**(예: HP 76% → 69%): 스킬 해금이 먼저 처리되고(해금 컷인·스킬 활성), 그 뒤 패시브 트리거 큐가 카드 팝업을 연다(팝업이 전투를 일시정지). 3번째 픽을 고른 순간 Tier1 이 발동한다. 즉 화면 순서는 언제나 "영웅 강화 → 내 카드 → 내 시너지".
- **보스 스테이지 페이즈 규칙과 겹침**: `Stage_Rule_Trigger.json` 의 x-5 스테이지 페이즈 규칙이 HP 0.75 / 0.55 / 0.35 에서 켜진다(챕터 1·4 는 0.55·0.35, 2·3·5 는 0.75·0.35). 스킬 해금과 **같은 순간**이 된다. 결정: **값 유지** — 보스 스테이지의 "영웅 각성 = 전장 변화" 가 한 순간으로 묶이는 것이 연출상 명확하다. 연출 순서 = 스킬 해금 컷인 → 규칙 FX. §19 Q4 로 확인.
- 유물 "분노의 영혼"(`WrathSpirit`, HP 50% 몬스터 광폭화)은 Tier2(50%)와 같은 순간 — 유물이 먼저(HP 판정), 카드 팝업이 뒤. 변경 없음.

---

## 10. 연동 영향

### 10.1 시스템별

| 시스템 | 영향 | 결정 |
|---|---|---|
| 상점 육성 | `ShopItem*.json` 은 축을 참조하지 않는다(2026-11-22 확인). "짙은 역병" `PlagueVenomUp` = 역병귀 독 피해 배율, 그대로 | 변경 없음. 각성 스탯(§3.2)·특성(§3.4)은 카드와 곱으로 합성 |
| 던전 자유 편성 | 카드 추첨은 여전히 편성과 무관(D2). 보정 입력은 픽 이력뿐 | 편성 팝업 문구 1846 "편성과 관계없이 카드가 무작위로 나와요" 유지. 대상 없는 효과 안내(1852~1855)는 새 effectKind 에도 적용(§10.2) |
| 편성 종족 행 축 칩 | `FormationSpeciesRow` 가 `SpeciesAxis` 로 "Tank" 등 축 칩을 그린다. 종족 = 시너지가 되어 정보가 중복 | 칩 숨김(위젯은 남기고 비활성). 프리팹 구조 변경 없음 |
| 전투 HUD 스포너 칸 색띠 | `SpeciesAxis` → 축 색 | 종족 발광색 `SpeciesVisual.SpeciesGlowColor` 로 교체 |
| 데일리 | 패시브·액티브 날짜 시드 덱 그대로. 보정은 같은 난수를 쓰므로 "같은 선택 = 같은 카드" 유지 | 변경 없음. 기록 비교성은 시즌(날짜) 단위라 영향 없음 |
| 무한의 탑 | 카드 규칙만 바뀜. 층 생성·보상 불변 | 게이트 SS8 |
| 유물 | 축·카드 ID 를 참조하는 유물 없음(2026-11-22 grep) | 변경 없음 |
| 도전과제 | `SynergyTierReached` 는 "한 런 최고 Tier" — 종족 최고 Tier 로 그대로 성립 | 문구 1615 · 1616 만 교체(§16.6) |
| 결과창 · 기록 | 픽 목록·최고 Tier 표시 경로 불변 | 변경 없음 |
| 도감 카드 탭 | 출하 카드 목록을 순회 → 58장(패시브 48 + 액티브 10) | 패시브 칸은 종족 아이콘 표시, 종족 순 정렬(§11.6) |
| 성기사(Ch5) 디버프 저항 | "Debuff 축 디버프" 개념이 사라진다 | §10.3 |
| 세이브 · 클라우드 | `PickedCards` 는 카드 ID 문자열 목록. 신규 ID 는 append, 기존 ID 문자열 불변 | `MetaProfile` 버전·필드 변경 없음 |
| 랭킹 · 최단 기록 | Easy 한정 규칙 불변. 카드 개편으로 기록 난이도가 바뀐다 | 기록 초기화 없음(§19 Q5) |
| 시뮬 · 디버그 | `SimPickStrategy` 축 우선 전략(`DebuffAxisPriority` 등), `ForcedCardDraw` 가 3픽 캡 참조 | 종족 우선 전략 6종 신설, 강제 등장은 1회 픽 기준(§16.8) |

### 10.2 카드 선택창 대상 안내 (`CardTargetNotice`)

| effectKind | 안내 종류 | 대상 |
|---|---|---|
| `SpeciesSpawnOutput` · `SpeciesSpawnPeriod` | 둥지 안내(1852 · 1855) | 카드 종족 둥지 |
| `SpawnerHaste` | 둥지 안내 | 모든 둥지(종족 없음) |
| `SpeciesStat` · `SpeciesDeathBurst` · `SpeciesHitHeroAtkSlow` · `SpeciesHitRoot` · `SpeciesGuardAura` · `SpeciesComboStrike` · `SpeciesPackPower` · `SpeciesExtraShot` · `SpeciesRangeBonus` · `SpeciesDeathSplit` | 몬스터 안내(1853 · 1854) | 카드 종족 몬스터(편성·필드) |
| `LingeringCurse` · `HeroAttackDown` · 액티브 영웅 대상 | 안내 없음 | 영웅 |
| `Frenzy` · `IronWill` · `Berserk` · `Slow` · `BloodThirst` | 몬스터 안내(종족 없음) | 기존 그대로 |

대상 종족은 effectKind 분기가 아니라 `Card_Species` 행에서 읽는다(카드별 switch 증가 금지).

### 10.3 성기사 디버프 저항 · 끈질긴 저주 대상 재정의

- `IDebuffAxisAura` 의 의미를 "Debuff 축 출처" → **"영웅 디버프(저항·지속 연장 대상)"** 로 바꾼다. 대상 집합은 현행 그대로(독 `PlagueToxinAura` · 공포 · 출혈 · 무력화, `Slow` 카드 둔화는 제외) + 신규 2종(도깨비 홀림 `SpeciesHitHeroAtkSlow` · 원혼의 사슬 `SpeciesHitRoot`).
- 성기사 "정화의 빛" 해제 목록에도 신규 2종을 더한다.
- 챕터 5 의도 "Debuff 축이 약해지는 챕터" → "디버프로 싸우는 빌드(역병귀·망령 사슬·도깨비 홀림)가 약해지는 챕터". 끈질긴 저주가 해답 1장인 구조 유지.

---

## 11. UI (기준 해상도 1280x720) — 목업 `.mockups/species-synergy-build.html`

### 11.1 공통 — 시너지 행 위젯 압축판 (HUD 행 = 선택창 칩)

| 항목 | 사양 |
|---|---|
| 크기 | 170 x 34. 검산 `3(상) + 22 + 2 + 5(트랙) + 2(하) = 34` |
| 1줄 (22) | 종족 아이콘 22x22(`MonsterIcons`) · 종족 이름 · 장수 텍스트(12px, 종족 색, 우측) |
| 2줄 (5) | 7칸 트랙, 칸 19 x 5, 간격 2 → `7 x 19 + 6 x 2 = 145`. 3·5·7번째 칸 Tier 테두리(기존 규칙) |
| 장수 텍스트 | 기존 산식 그대로(`c/N`, `T{t}` 부착, `7+ T3`) — `card-synergy-indicator.md` §2 |
| 불투명도·색띠 | 기존 규칙(c=0 0.55, c≥3 왼쪽 3px 색띠) |
| 색 | 종족 발광색: 도깨비불 `#28E66E` · 망령 `#C0CCE6` · 사신 `#E64141` · 저주술사 `#E6AF08` · 역병귀 `#9C4FE6` · 환령 `#82ABE6` |
| 순서 | 도깨비불 → 망령 → 사신 → 저주술사 → 역병귀 → 환령 (`EMonster` 순, 판마다 고정) |

### 11.2 전투 HUD 우상단 시너지 패널

- 6행(§11.1). 높이 `14 + 6 + 6 x 34 + 5 x 4 = 244 ≤ _expandedHeight 247` — 247 유지.
- 접힌 탭 점 4 → 6. 점 8x8, 간격 4 → `6 x 8 + 5 x 4 = 68 ≤ 170`. 프리팹에 점 Image 2개 추가.
- 클릭 → 시너지 모달(기존 경로).

### 11.3 카드 선택창

- **하단 "내 시너지" 칩 6개**(§11.1 위젯). `6 x 170 + 5 x 12 = 1080 ≤ 1280 - 2 x 40 = 1200`, 가운데 정렬. 칩 높이 34(기존 44 대비 10 낮음) — 기존 칩 하단 정렬 위치를 유지하면 카드와의 간격이 10 늘어난다. **패시브 팝업에만 표시**, 액티브 팝업에서는 칩 줄을 숨긴다(액티브는 시너지에 안 들어감 — 보여 주면 오해).
- **패시브 카드**(종족 카드):
  - 아트 영역(현행 높이 200) = 종족 아이콘 128x128 가운데 + 종족 색 원형 후광(중심 알파 0.35 → 가장자리 0). 기존 `_cardImage` 는 이 화면에서 쓰지 않는다.
  - 좌상단 종류 라벨 "패시브" → **종족 이름**(종족 색 글자).
  - 기존 모서리 축 아이콘 위젯 → 숨김(아트가 곧 종족 아이콘이라 중복).
  - 우상단 배지(기존 `N/3` 자리) → **발동 예고** "T{n} 발동": 이 카드를 고르면 그 종족 장수가 3·5·7 이 될 때만 표시(`c(s) + 1 ∈ {3, 5, 7}`), 아니면 숨김. 문구 1999.
- **액티브 카드**: 일러스트 유지, 라벨 "임시 스킬"(문구 1998), 모서리 아이콘·배지 숨김.
- 보장 카드 표시는 따로 하지 않는다 — 항상 왼쪽 첫 칸(§4-3).

### 11.4 시너지 모달 (`SynergyModalPopup`)

- 축 4 → 종족 6. 활성 Tier 가 있는 종족만 헤더 1행 + Tier 행(기존 구조). Tier 행 = "Tier 이름 — 설명"(문구 §16.6). 헤더 아이콘 = 종족 아이콘. 리스트는 기존 풀링 스크롤이라 행 수 증가 수용.

### 11.5 빌드 모달 (`BuildModalPopup`)

- 정렬: 패시브를 종족 순(§11.1) → 같은 종족 안에서 픽 순, 그 뒤 액티브 픽 순. 칸 테두리 색 = 종족 색(액티브는 기존 금색).

### 11.6 도감 카드 탭

- 58칸. 정렬 = 종족 순 패시브 8장씩 → 액티브 10장. 패시브 칸 그림 = 종족 아이콘. 해금 규칙(한 번이라도 고르면 해금) 불변.

### 11.7 UI 범위 메모

이 기능은 프리팹 위젯 추가·숨김·크기 조정을 포함한다(칩 6, 행 압축, 접힌 점 +2, 카드 라벨·배지 용도 변경). "공통 UI 아트 교체" 항목의 "프리팹 구조 변경 금지" 와 충돌하므로 §17 승격안에 명시 허용을 넣는다. 신규 화면·신규 팝업은 없다.

---

## 12. 데이터 구조표 (사용자 승인 게이트)

> Rule 02 §11-1. 파일명 = `EData` 값명. sentinel 없음. 이름 규칙 `<부모>_<관계명>`.

### 12.1 엔티티 / 관계 트리

```
Card  (엔티티: 카드 1장)                         PK cardId              [변경: axis 필드 삭제]
 ├─ Card_Param    (관계: 카드 효과 수치 1개)     PK (cardId, paramKey)  [기존 스키마, 행 교체]
 └─ Card_Species  (선택적 관계: 패시브 카드의 종족) PK cardId            [재정의: 1카드 1행, 액티브는 행 없음]
        monster ──→ EMonster 기본 6종 (Race_Monster 행 존재 검증)

SynergyTier  (엔티티: 한 종족 시너지의 한 단계)  PK (species, tier)     [신규]
 └─ SynergyTier_Effect  (관계: 그 단계의 효과 1개) PK (species, tier, effectIndex)   [신규]
     │   effectKind ──→ ECardEffect (카드와 같은 효과 종류를 공유)
     └─ SynergyTier_Effect_Param (관계: 효과 수치 1개) PK (species, tier, effectIndex, paramKey) [신규]

BalanceConfig (단일 객체, 기존)                                          [변경: passiveFocusSlots 추가]
HeroArchetype_Skill (기존)                                               [값만 변경: hpFraction]

CardPool_Passive / CardPool_Active (SO, 기존)  카드 SO 참조 목록           [구성 변경: 48 / 10]
SynergyVisualConfig (SO, 기존)  Entries[6] (Species → Icon Sprite)        [변경: 키 EBuildAxis → EMonster, 4 → 6]
```

- 패시브/액티브 구분의 단일 진실은 카드 풀 SO 소속이다(기존). `Card_Species` 행 존재 = 패시브, 이 두 사실이 일치하는지 로드 검증한다(§12.3).
- 시너지 임계 3/5/7 은 데이터로 빼지 않는다 — 사용자 결정 D5 로 고정이고 `BuildSynergyService.Tier1/2/3Threshold` 가 단일 진실. `SynergyTier.tier`(1~3)가 그 임계에 대응한다.
- Tier 효과를 카드 효과 종류(`ECardEffect`)로 공유하는 이유: Tier 효과 18개 중 12개가 카드와 같은 동작(`SpeciesStat` · `SpeciesSpawnOutput` · `HeroAttackDown` · `SpawnerHaste`)이다. 별도 enum 을 만들면 같은 동작이 두 번 구현된다. 대상 종족은 카드는 `Card_Species`, Tier 는 `SynergyTier.species` 에서 공급한다.

### 12.2 필드 표

| 데이터 | 필드 | 타입 | 설명 |
|---|---|---|---|
| `Card` | `cardId` | string | `ECardId` 값명(세이브 문자열, 불변) |
| | `nameKey` · `descKey` | int | `Strings_Ko` |
| | `effectKind` | string | `ECardEffect` 값명 |
| | ~~`axis`~~ | - | **삭제** |
| `Card_Param` | `cardId` · `paramKey` · `value` | string · string · float | 기존. `SpeciesStat` 은 `paramKey` = `EMonsterStatKind` 값명 |
| `Card_Species` | `cardId` | string | PK. 패시브 카드만 |
| | `monster` | string | `EMonster` 기본 6종 값명(`WispG2` 등 등급 값 거부) |
| `SynergyTier` | `species` | string | `EMonster` 기본 6종 |
| | `tier` | int | 1 · 2 · 3 |
| | `nameKey` · `descKey` | int | Tier 이름 · 설명 (`Strings_Ko`) |
| `SynergyTier_Effect` | `species` · `tier` | string · int | FK → `SynergyTier` |
| | `effectIndex` | int | 1부터, 단계 안 효과 순서(적용 순서) |
| | `effectKind` | string | `ECardEffect` 값명 |
| `SynergyTier_Effect_Param` | `species` · `tier` · `effectIndex` | | FK → `SynergyTier_Effect` |
| | `paramKey` · `value` | string · float | 효과별 필수 키 표(§16.1)와 정확히 일치 |
| `BalanceConfig` | `passiveFocusSlots` | int | 패시브 추첨 보장 슬롯 수. 초기 1, 범위 0~2 |

### 12.3 로드 검증 (위반 시 로드 오류 — 기존 `ChapterDataException` 수집 패턴)

1. `Card` 행에 `axis` 키가 남아 있으면 오류(구 데이터 혼입 방지).
2. `Card_Species`: `cardId` 중복 0, `monster` 는 기본 6종. **`CardPool_Passive` 의 모든 카드는 정확히 1행, `CardPool_Active` 카드는 0행**(EditMode 테스트로 검증 — 풀 SO 대조는 기존처럼 테스트 담당).
3. 종족별 패시브 정확히 8장(EditMode 테스트).
4. `SynergyTier`: (species, tier) 6 x 3 = 18행 정확히, 누락·중복 0.
5. `SynergyTier_Effect`: 각 (species, tier) 에 1행 이상, `effectIndex` 1부터 연속.
6. `Card_Param` · `SynergyTier_Effect_Param`: 효과별 필수 키 집합과 정확히 일치. `SpeciesStat` 은 키 정확히 1개이며 `EMonsterStatKind` 값명, 값 > 0(스탯 2개가 필요하면 효과 행을 2개로 나눈다).
7. `BalanceConfig.passiveFocusSlots` 0~2 밖이면 오류.
8. **실패 정책 변경**: 지금 `Card_Species` 는 편성 묶음(`LoadFormationAsync`)에서 읽고 실패해도 "카드 후보 제외만 꺼진다"(E-2 관용)로 넘어간다. 이제 이 파일이 범용 카드 효과의 대상과 시너지 장수의 유일한 출처이므로, **카드 데이터 묶음(`LoadCardsAsync` · `CardDataSet`)으로 옮겨 `Card`·`Card_Param` 과 같은 필수 묶음으로 취급한다** — 로드·검증 실패 시 카드 데이터 미로드(`CardDatabase.Current = null`)와 같게 전투 진입 불가. `SynergyTier*` 3종도 같은 필수 묶음이다. `FormationCardSpecies` 클래스는 사용처가 사라지므로 삭제.

### 12.4 JSON 행 수

| 파일 | 행 수 | 검산 |
|---|---|---|
| `Card.json` | 58 | 기존 28 + 신규 30. `HeroPoisonAura` 는 행 없음(기존) — `ECardId` 정수 순 정렬 유지 |
| `Card_Species.json` | 48 | 패시브 1행씩 |
| `Card_Param.json` | 65 | 패시브 50 + 액티브 15 — 산식은 §16.3 |
| `SynergyTier.json` | 18 | 6 x 3 |
| `SynergyTier_Effect.json` | 23 | Tier1·2 12 + Tier3 11 — 산식은 §16.4 |
| `SynergyTier_Effect_Param.json` | 22 | 산식은 §16.4 |

행 수의 단일 진실은 §16.3 · §16.4 표이며, EditMode 무결성 테스트가 위 수를 고정한다.

---

## 13. 저장 · 마이그레이션

- `MetaProfile` 버전·필드 변경 없음. `PickedCards` 에 신규 ID 문자열이 추가될 뿐이다. 기존 ID 는 전부 존속(삭제 0, 개명 0) → 도감 해금 기록 보존.
- `ECardId` 는 append-only(정수 직렬화 SO 안전). 신규 30개는 `LingeringCurse`(28) 뒤 29~58.
- 클라우드 복원: 변경 없음.

---

## 14. 엣지 케이스

| 케이스 | 동작 |
|---|---|
| 주력 종족이 편성에 없음 | 보정은 그대로 그 종족 카드를 준다(D2). 카드 선택창 대상 안내가 "이 효과를 받을 몬스터가 없어요" 표시. Tier3 소환은 편성과 무관하게 나온다 |
| 패시브 픽 0장 상태에서 액티브만 고름 | 장수 전부 0. 다음 패시브 추첨은 "첫 패시브 추첨" 규칙(서로 다른 3종족) |
| 동률 3종족 이상 | `last(s)` 최대 1종족 |
| 주력 8장 소진 | 다음 장수 종족이 주력. 고른 종족 전부 소진이면 보정 없음 |
| `passiveFocusSlots` = 0 | 보정 없음(균등 3장, 첫 추첨 서로 다른 3종족 규칙은 유지) |
| 적격 패시브 3장 미만 | 가능한 만큼. 0장이면 트리거 소비(기존) |
| 한 타격에 여러 패시브 임계 통과 | 큐 순서대로 팝업. 각 팝업 사이 Tier 발동은 즉시 |
| Tier 발동과 동시에 전투 종료(영웅 사망) | 종료 우선, 이후 Tier 적용 없음(기존 결과 판정 가드) |
| Tier3 소환 위치가 벽·장애물 | 기존 `SpawnMonster(nearHero)` 위치 보정 규칙 |
| 갈라지는 그림자로 소환된 환령 | 분열 없음, 불씨 폭발·사신 콤보 등 다른 종족 카드와 무관 |
| 원혼의 사슬 + 성기사 | 지속 1.0 x 0.6 = 0.6초, 정화의 빛 해제 대상 |
| 원혼의 사슬 + 끈질긴 저주 | 1.0 x 1.25 = 1.25초. 성기사 상대 1.0 x 1.25 x 0.6 = 0.75초 |
| 데일리 같은 시드 · 같은 선택 | 같은 카드(보정 포함 난수 순서 고정) |
| 디버그 강제 등장(`ForcedCardDraw`) | 이미 고른 카드는 강제 등장 불가(1회 픽 기준). 보정 슬롯보다 강제 카드가 우선 |
| 구 세이브의 3픽 기록(`CardPickCounts` 류) | 도감 표시용 문자열이라 영향 없음 |
| 영웅 HP 회복(성수 샘 등)으로 HP 가 임계 위로 돌아감 | 이미 연 패시브 트리거·스킬은 다시 열리지 않는다(기존 1회 규칙) |

---

## 15. qa-simulator 검증 게이트 (구현 + 테스트 후)

공통: 시뮬 `BalanceSimulationTest`, 15x, 시나리오당 N=30, 육성 Lv0 · 유물 없음 · 기본 편성(종족당 1칸) 기준. 판정은 승리 판 처치 시간 중앙값.

| # | 지표 | 통과 기준 | 실패 시 레버 |
|---|---|---|---|
| SS1 | 무작위 전략 처치 시간 중앙값 (전 챕터 Easy, 5직업) | 컨셉 §8 창 120~240초 | 카드 공통 배율(HP 1.5 · 공격력 1.3) 한 단계 |
| SS2 | 종족 우선 전략 6종 중앙값의 최대 ÷ 최소 | ≤ 1.35, 그리고 6종 모두 무작위 전략 중앙값 이하 | 약한 종족 ⑦·⑧ 수치, Tier3 소환 수 |
| SS3 | Tier3 도달률 / HP 30% 시점 도달률 | 종족 우선: 도달률 ≥ 95%, 그중 HP 30% 도달 ≥ 90%. 무작위: 도달률 ≤ 10% | `passiveFocusSlots` |
| SS4 | 종족 우선 Tier3 발동 → 처치까지 시간 중앙값 | 12~30초 (결정타 체감, 즉사 아님) | Tier3 소환 수 · 저주 폭풍 비율 · 영구 출혈 비율 |
| SS5 | 영웅 스킬 HP 85/65/45 대비 75/55/35 (같은 시드·전략) | 무작위 전략 중앙값 차이 ≤ 10% 단축 | 스킬 HP 값(사용자 결정 D7 — 조정 시 사용자 승인) |
| SS6 | 영웅별 상성 (5직업 x 6종족) | 어느 칸도 종족 우선 중앙값 > 무작위 중앙값 x 1.15 아님 | 해당 종족 ⑦·⑧ 수치 |
| SS7 | 종족 1칸 편성 vs 같은 종족 3칸 편성 (종족 우선) | 3칸이 1칸보다 빠르고(편성 의미), 1칸도 무작위 전략 이하(1칸 집중이 손해가 아님) | 둥지 출력·주기 카드 수치 |
| SS8 | 무한의 탑 종족 우선 6종 도달 층 (상점 중반 프로필) | 최대 - 최소 ≤ 3층 | 영웅 최대 HP 비례 효과(저주 폭풍 5%, 영구 출혈 1%) |
| SS9 | 데일리 시간 예산(DQ2 재검증) | 기존 DQ2 기준 유지 | 보정 슬롯 · 액티브 구성 |

- 시뮬 로그(`Logs/lair_runs.jsonl`)에 종족별 최종 장수와 Tier3 발동 시 영웅 HP·경과 시간이 필요하다(§16.8).

---

## 16. 구현 요청사항 (gameplay-programmer)

### 16.1 Enum (`Assets/_Lair/Scripts/Data/CommonEnum.cs` · `ECardId.cs`)

- **`EBuildAxis` 삭제.** 모든 사용처(2026-11-22 `Assets/_Lair` 전체 grep `EBuildAxis|.axis|"axis"`: 런타임 `Scripts` 17파일 + 테스트 42파일(EditMode 41 · PlayMode `SimPickStrategy`·`ContinuousSpawnIntegrationTest`·`SynergyModalPopupPoolingRepro` 포함), `Editor` 0파일 — 카드 에디터 창 `LairCardEditorWindow` 는 그림·풀 소속만 다뤄 축을 쓰지 않는다)는 `EMonster`(기본 6종)로 교체. 기본 6종이 아닌 값이 들어오면 `MonsterGrades.BaseOf` 로 정규화.
- **`ECardId`** — `LingeringCurse` 뒤, `<card-editor:insert>` 표식 위에 30개 append(이 순서 그대로, 정수 29~58): `WispPowerBoost, WispSpawnHaste, WispMoveSpeed, WispEmber, WispDazzle, WraithSpawnHaste, WraithKnockback, WraithArmor, WraithChain, WraithWard, ReaperHpBoost, ReaperPowerBoost, ReaperSpawnHaste, ReaperMoveSpeed, ReaperCombo, ReaperPack, HexHpBoost, SpawnHexes, HexSpawnHaste, HexCastSpeed, HexDoubleCast, HexFarShot, PlagueHpBoost, PlaguePowerBoost, PlagueSpawnHaste, PlagueMoveSpeed, PhantomHpBoost, PhantomPowerBoost, PhantomCooldown, PhantomSplit`.
- **`ECardEffect`** 추가 16개: `SpeciesStat, SpeciesSpawnOutput, SpeciesSpawnPeriod, SpeciesDeathBurst, SpeciesHitHeroAtkSlow, SpeciesHitRoot, SpeciesGuardAura, SpeciesComboStrike, SpeciesPackPower, SpeciesExtraShot, SpeciesRangeBonus, SpeciesDeathSplit, SpeciesSummon, PlagueToxinMaxStack, EternalBleed, HexCurseStorm`. `Card.json`·`SynergyTier_Effect.json` 이 더 이상 참조하지 않는 값(`WispHpBoost, WraithDamageBoost, ReaperAtkSpeed, HexRangeBoost, PlagueToxinBoost, PhantomMoveSpeedBoost, SpawnWisps, SpawnWraith, SpawnReapers, SpawnPlagues, SpawnPhantoms, WispWraithPowerBoost, ReaperHexPowerBoost, FastBreeding, ToughHide`)과 그 효과 클래스는 삭제한다(`ECardEffect` 는 JSON 문자열로만 직렬화). `EMonsterBuff.ToughHide` 는 사용처가 사라지므로 enum 자리만 보존(정수 직렬화).
- **`EData`** 맨 끝 append: `SynergyTier, SynergyTier_Effect, SynergyTier_Effect_Param`.
- 효과별 필수 paramKey 표(`CardDatabase.RequiredParams`) 추가·변경:

| effectKind | 필수 키 |
|---|---|
| `SpeciesStat` | `EMonsterStatKind` 값명 정확히 1개 (특례 검증) |
| `SpeciesSpawnOutput` | 없음 |
| `SpeciesSpawnPeriod` | `periodMul` |
| `SpeciesDeathBurst` | `radius`, `powerRatio` |
| `SpeciesHitHeroAtkSlow` | `cdMul`, `duration` |
| `SpeciesHitRoot` | `rootDuration`, `cooldown` |
| `SpeciesGuardAura` | `radius`, `takenMul` |
| `SpeciesComboStrike` | `hitCount`, `dmgMul` |
| `SpeciesPackPower` | `radius`, `perMul`, `maxCount` |
| `SpeciesExtraShot` | `chance` |
| `SpeciesRangeBonus` | `minDistance`, `dmgMul` |
| `SpeciesDeathSplit` | `chance` |
| `SpeciesSummon` | `count` |
| `PlagueToxinMaxStack` | `bonus` |
| `EternalBleed` | `ratio` |
| `HexCurseStorm` | `maxHpRatio`, `period` |
| `HeroAttackDown` | `factor` (변경: 기존 없음 → 필수) |
| `LingeringCurse` | `durationBonus` (변경: `durationBonusPerPick` 개명) |
| `GuardianRage` | `duration` (값만 6) |

### 16.2 Interface (`Assets/_Lair/Scripts/Card/CommonInterface.cs`)

- `IBattleContext.RegisterCardPick` · `GetBuildCount` 의 인자를 `EBuildAxis` → `EMonster`(종족). 액티브 픽은 `RegisterCardPick` 을 호출하지 않는다.
- 카드 효과가 대상 종족을 받아야 한다 — `ICardEffect` 생성 시 `Card_Species` 의 종족(Tier 는 `SynergyTier.species`)을 주입. 주입 방식은 구현 판단.
- 필요한 동작 표면(시그니처는 구현 판단): 종족 몬스터 사망 훅(불씨 폭발·갈라지는 그림자), 종족 몬스터 적중 훅(홀림·사슬·연속 베기·피 냄새·원거리 저주), 발사 훅(저주탄 분열), 종족 주변 받는 피해 오라(서늘한 기운), 영웅 주위 종족 소환(`SpawnMonster` 재사용 가능), 영웅 최대 HP 비례 주기 피해(저주 폭풍).
- `IDebuffAxisAura` 의미 변경(§10.3) — 이름 유지 시 주석만 갱신, 개명은 구현 판단(동작 불변).

### 16.3 `Card_Param.json` 행 (패시브 — 이 표가 단일 진실)

| cardId | paramKey | value |
|---|---|---|
| WispHpBoost | Hp | 1.5 |
| WispPowerBoost | Power | 1.3 |
| WispSpawnHaste | periodMul | 0.75 |
| WallOfWisps | DamageTaken | 0.75 |
| WispMoveSpeed | MoveSpeed | 1.35 |
| WispEmber | radius / powerRatio | 2.0 / 3 |
| WispDazzle | cdMul / duration | 1.15 / 2 |
| WraithDamageBoost | Hp | 1.5 |
| ReplaceWispsToWraith | Power | 1.3 |
| WraithSpawnHaste | periodMul | 0.75 |
| WraithKnockback | KnockbackTaken | 0.5 |
| WraithArmor | DamageTaken | 0.8 |
| WraithChain | rootDuration / cooldown | 1.0 / 8 |
| WraithWard | radius / takenMul | 3.0 / 0.85 |
| ReaperHpBoost | Hp | 1.5 |
| ReaperPowerBoost | Power | 1.3 |
| ReaperSpawnHaste | periodMul | 0.75 |
| ReaperAtkSpeed | Cooldown | 0.7 |
| ReaperMoveSpeed | MoveSpeed | 1.3 |
| ReaperCombo | hitCount / dmgMul | 4 / 2.5 |
| ReaperPack | radius / perMul / maxCount | 2.0 / 0.06 / 5 |
| HexHpBoost | Hp | 1.5 |
| ReplaceReapersToHex | Power | 1.3 |
| HexSpawnHaste | periodMul | 0.75 |
| HexRangeBoost | Range | 1.4 |
| HexCastSpeed | Cooldown | 0.8 |
| HexDoubleCast | chance | 0.25 |
| HexFarShot | minDistance / dmgMul | 4.0 / 1.35 |
| PlagueHpBoost | Hp | 1.5 |
| PlaguePowerBoost | Power | 1.3 |
| PlagueSpawnHaste | periodMul | 0.75 |
| PlagueSlowBoost | PoisonDamage | 1.4 |
| LingeringCurse | durationBonus | 0.25 |
| HeroAttackDown | factor | 0.75 |
| PlagueMoveSpeed | MoveSpeed | 1.3 |
| PhantomHpBoost | Hp | 1.5 |
| PhantomPowerBoost | Power | 1.4 |
| Multiply | periodMul | 0.7 |
| PhantomMoveSpeedBoost | MoveSpeed | 1.5 |
| PhantomCooldown | Cooldown | 0.75 |
| SpawnerHaste | periodMul | 0.8 |
| PhantomSplit | chance | 0.3 |

행 수 검산(키 하나 = 1행): 2키 카드 6장(WispEmber · WispDazzle · WraithChain · WraithWard · ReaperCombo · HexFarShot) = 12행, 3키 카드 1장(ReaperPack) = 3행, 1키 카드 = 표 42장 - 7 = 35행 → 패시브 50행. `SpeciesSpawnOutput` 6장(SpawnWisps · SpawnWraith · SpawnReapers · SpawnHexes · SpawnPlagues · SpawnPhantoms)은 행 없음: 42 + 6 = 48 ✓. 액티브는 기존 16행에서 `Multiply` 1행이 패시브로 옮겨 15행(Fear 1 · Bleed 2 · Weaken 2 · Slow 3 · Frenzy 1 · BloodThirst 1 · IronWill 1 · TimeStop 1 · Berserk 1(값 15 → 6) · MarkOfDeath 2). **`Card_Param.json` 합계 50 + 15 = 65행.** 재사용 카드의 기존 키(`hpMul` · `cdMul` · `rangeMul` · `toxinMul` · `speedMul` · `powerMul` · `durationBonusPerPick`)는 위 표의 키로 교체된다.

### 16.4 `SynergyTier*` 행 (단일 진실)

| species | tier | 이름(nameKey) | 설명(descKey) | effectIndex | effectKind | 파라미터 |
|---|---|---|---|---|---|---|
| Wisp | 1 | 불씨 (1960) | 도깨비불 공격력 +25% (1980) | 1 | SpeciesStat | Power 1.25 |
| Wisp | 2 | 불길 (1961) | 도깨비불 둥지마다 1마리 더 (1981) | 1 | SpeciesSpawnOutput | 없음 |
| Wisp | 3 | 불바다 (1962) | 도깨비불 7마리 즉시 소환 · 공격 간격 -25% (1982) | 1 / 2 | SpeciesSummon / SpeciesStat | count 7 / Cooldown 0.75 |
| Wraith | 1 | 서린 한 (1963) | 망령 HP +30% (1983) | 1 | SpeciesStat | Hp 1.3 |
| Wraith | 2 | 원혼의 벽 (1964) | 망령 공격력 +25% (1984) | 1 | SpeciesStat | Power 1.25 |
| Wraith | 3 | 원혼 강림 (1965) | 망령 3마리 즉시 소환 · 받는 피해 -40% (1985) | 1 / 2 | SpeciesSummon / SpeciesStat | count 3 / DamageTaken 0.6 |
| Reaper | 1 | 핏빛 낫 (1966) | 사신 공격력 +25% (1986) | 1 | SpeciesStat | Power 1.25 |
| Reaper | 2 | 피의 춤 (1967) | 사신 공격 간격 -20% (1987) | 1 | SpeciesStat | Cooldown 0.8 |
| Reaper | 3 | 사신의 밤 (1968) | 사신 3마리 즉시 소환 · 이동속도 +40% (1988) | 1 / 2 | SpeciesSummon / SpeciesStat | count 3 / MoveSpeed 1.4 |
| Hex | 1 | 저주 각인 (1969) | 저주술사 공격력 +25% (1989) | 1 | SpeciesStat | Power 1.25 |
| Hex | 2 | 먼 저주 (1970) | 저주술사 사거리 +30% (1990) | 1 | SpeciesStat | Range 1.3 |
| Hex | 3 | 저주 폭풍 (1971) | 즉시 영웅 최대 HP 5% 피해, 6초마다 반복 (1991) | 1 | HexCurseStorm | maxHpRatio 0.05, period 6 |
| Plague | 1 | 고인 독 (1972) | 독 최대 중첩 +2 (1992) | 1 | PlagueToxinMaxStack | bonus 2 |
| Plague | 2 | 쇠약 (1973) | 영웅 공격력 -15% (영구) (1993) | 1 | HeroAttackDown | factor 0.85 |
| Plague | 3 | 역병의 끝 (1974) | 영구 출혈 1%/초 · 독 피해 +50% (1994) | 1 / 2 | EternalBleed / SpeciesStat | ratio 0.01 / PoisonDamage 1.5 |
| Phantom | 1 | 바람 그림자 (1975) | 환령 이동속도 +30% (1995) | 1 | SpeciesStat | MoveSpeed 1.3 |
| Phantom | 2 | 그림자 무리 (1976) | 환령 둥지마다 1마리 더 (1996) | 1 | SpeciesSpawnOutput | 없음 |
| Phantom | 3 | 환령 폭주 (1977) | 환령 14마리 즉시 소환 · 모든 둥지 주기 -15% (1997) | 1 / 2 | SpeciesSummon / SpawnerHaste | count 14 / periodMul 0.85 |

행 수: `SynergyTier` 18 · `SynergyTier_Effect` = Tier1·2 12 + Tier3 (2+2+2+1+2+2 = 11) = **23** · `SynergyTier_Effect_Param` = 효과 23개 - 파라미터 없는 효과 2개(Wisp T2 · Phantom T2) = 21개 효과, 그중 2키 1개(HexCurseStorm) → 20 + 2 = **22행**.

- 기존 `Card/Synergy/*SynergyTier*.cs` 12클래스는 데이터 구동으로 대체되어 삭제한다. `Strings_Ko` 200~211 은 사용처가 사라지지만 행은 남겨 둔다(삭제 이득 없음).

### 16.5 에셋 키 · 파일

| 에셋 | 경로 | 키 |
|---|---|---|
| 신규 카드 SO 30 | `Assets/_Lair/Art/Cards/Items/{ECardId}.asset` (기존 카드 SO 폴더) | 파일명 = `ECardId` 값명. `_id` = 그 값, `_icon` = 종족 `MonsterIcons/{Name}.png`, `_cardImage` 비움 |
| 카드 풀 SO | `CardPool_Passive` 48장 · `CardPool_Active` 10장 | 기존 `EData` 키. `WallOfWisps` · `Multiply` 를 액티브 → 패시브로 이동 |
| 시너지 아이콘 SO | `Assets/_Lair/Art/Synergy/SynergyVisualConfig.asset` | Entries 6 = (`EMonster` 기본 6종 → `MonsterIcons/{Name}.png`) |
| JSON 신규 3 | `Assets/_Lair/Data/Json/SynergyTier.json` · `SynergyTier_Effect.json` · `SynergyTier_Effect_Param.json` | `EData` 값명 = 파일명, Addressable 라벨 `Resource` |
| 몬스터 아이콘 | `Assets/_Lair/Art/Sprites/MonsterIcons/*.png` (기존 6장) | 파일 변경 없음 — 카드·시너지 표시에서 참조만 추가 |

- 상태 아이콘(영웅 HP바 아래): 도깨비 홀림 · 원혼의 사슬 오라의 `IconCardId` = 각 카드 ID(아이콘 = 종족 아이콘). 영구 출혈은 기존 출혈 아이콘 경로 유지.

### 16.6 `Strings_Ko` (신규 id 1900~1999, 현행 최대 1878)

| id | 내용 |
|---|---|
| 1900~1929 | 신규 카드 30장 이름 (§5.2, `ECardId` 신규 순서) |
| 1930~1959 | 신규 카드 30장 설명 (같은 순서) |
| 1960~1977 | Tier 이름 18 (§16.4) |
| 1980~1997 | Tier 설명 18 (§16.4) |
| 1998 | `임시 스킬` |
| 1999 | `T{0} 발동` |

변경: 1644 `처형 명령` → `저주 증폭`. 재사용 카드 descKey(1660 · 1661 · 1662 · 1663 · 1664 · 1665 · 1666 · 1667 · 1668 · 1669 · 1670 · 1671 · 1672 · 1673 · 1679 · 1684 · 1686 · 1687)는 §5.2 "설명" 열 문구로 교체. 1683 `모든 몬스터 받는 피해 -50% (6초)`. 1615 `한 런에서 한 종족 시너지 Tier2(패시브 5장)에 도달한다`, 1616 `한 런에서 한 종족 시너지 Tier3(패시브 7장)에 도달한다`. 1978 · 1979 는 비워 둔다(구간 경계).

### 16.7 SO 스키마

- `SynergyVisualConfig.Entries[i]` = `{ Species: EMonster, Icon: Sprite }`, 길이 6, 기본 6종 각 1개(중복·누락 금지, 등급 값 금지).
- `CardData` SO 스키마 불변(`_id` · `_icon` · `_cardImage`).

### 16.8 코드 접점 (빠지면 안 되는 지점)

- `Card_Species` · `SynergyTier*` 로드를 카드 필수 묶음으로 이동(§12.3-8), `FormationCardSpecies` 삭제. 신규 카드 SO 30개는 기존 카드 에디터 창(`LairCardEditorWindow`)으로 만들어도 된다(축 필드 없음).
- `CardPickCounter` 상한 3 → 1. `CardView` `N/3` 배지 → 발동 예고 배지(§11.3).
- `CardDeck` 패시브 추첨에 §4 보정 — 입력(픽 이력·종족 소속)만 받고 편성은 받지 않는다. 난수 순서 고정.
- `BuildSynergyService` 키 `EMonster`, Tier 바인딩은 `SynergyTier*` 데이터에서 생성.
- `BattleController`: 액티브 픽은 시너지 등록 제외. `GetMaxSynergyTier` 종족 순회.
- `HeroArchetype_Skill.json` hpFraction 15행.
- `BuildSynergyPanel` · `BuildSynergyCell` · `CardSelectionPopup` · `CardView` · `SynergyModalPopup` · `BuildModalPopup` · `FormationSpeciesRow`(축 칩 숨김) · 스포너 HUD 색띠 · `CodexPopup` 카드 탭.
- `SpeciesVisual.SpeciesAxis` 삭제(사용처 교체 후).
- `MonsterBuffService.GuardianRage` 대상 전체 종으로.
- 성기사 디버프 저항 · 정화의 빛 목록에 신규 2오라 추가(§10.3).
- 시뮬: `SimPickStrategy` 에 `SpeciesPriority(종족)` 6종(보이면 주력 종족 카드 중 무작위, 없으면 무작위) — 축 우선 전략 삭제. `RunRecord` 에 종족별 패시브 장수와 Tier3 발동 시 영웅 HP 비율·경과 초 기록.
- `ForcedCardDraw`: 1회 픽 기준 제외.

### 16.9 구현 슬라이스 (독립 구현·테스트 단위)

| 슬라이스 | 범위 | 선행 |
|---|---|---|
| D | 영웅 스킬 HP 75/55/35 (JSON 값만) | 없음 — 먼저 출하 가능 |
| A | Enum · JSON 스키마(§12) · 카드 데이터 58행 · SO 30 · 풀 재구성 · `EBuildAxis` 제거 | 없음 |
| B | 1회 픽 · 패시브만 장수 · 추첨 보정 · 데일리 재현 | A |
| C | 신규 효과 9 + 시너지 효과 4 + Tier 18 데이터 구동 | A |
| E | UI (선택창 칩·카드 표시·배지, HUD 6행·점 6, 모달 2, 편성 칩, 스포너 색띠, 도감) | A, B |
| F | 시뮬 전략 · 로그 → qa-simulator SS1~SS9 | B, C |

### 16.10 테스트 항목 (test-engineer)

- 데이터 무결성: 58장 · 패시브 48 · 종족당 8 · 액티브 10 · `Card_Species` 패시브 1행/액티브 0행 · `SynergyTier` 18 · `Card_Param` 65행 · `SynergyTier_Effect` 23 · `_Param` 22 · `SpeciesStat` 키 검증.
- 추첨: 첫 추첨 3종족 상이 · 보장 슬롯 1 · 동률 최근 우선 · 소진 이월 · `passiveFocusSlots` 0/2 · 같은 시드 같은 결과 · 편성 변경이 결과를 바꾸지 않음.
- 1회 픽: 고른 카드 재등장 0(패시브·액티브).
- 장수: 액티브 픽이 장수를 바꾸지 않음 · 3/5/7 발동 1회 · 2종족 동시 Tier3 불가(9픽 전수).
- 효과: §5.3 규칙별 단위 테스트(쿨다운·중첩·재분열 없음·겹침 1회·반경 경계).
- 영웅 스킬 HP 값 · 동시 임계 순서.
- 회귀: 기존 4축 전제 테스트(`BuildSynergyTiersTests`, `SynergyTierDescriptionTests`, `SynergyRealStringTableTests`, `CardBorderColorsTests`, 28장·축당 7장 무결성, `ForcedCardDrawTests`, `FormationCardAndSealTests`, `SpawnerStatusCell*Tests`, PlayMode `SimPickStrategy`)을 종족 전제로 교체.

---

## 17. CLAUDE.md §8 승격안 (사용자 승인 대상)

### 17.1 신규 항목 (몬스터 육성 재화 분리 항목 뒤)

> - **종족 시너지 빌드 허용 (2026-11-22 v0.3 승격) - 단 기존 카드 시스템의 시너지 축·카드 구성·추첨 규칙 개편에 한정** - 카드 빌드 시너지를 4축(Tank/Dps/Debuff/Swarm)에서 기존 몬스터 6종족마다 1개로 바꾼다. 패시브 카드는 종족당 8장 x 6 = 48장(기존 패시브 16장과 영구 액티브 2장을 종족별로 재배치 + 신규 30장, 카드 일러스트 없이 기존 몬스터 UI 아이콘 6장으로 표시)이고, 액티브 카드는 시너지에 들지 않는 "임시 스킬" 10장(기존 시한 액티브)이다. 같은 카드는 한 판에 한 번만 고른다(3픽 누적 폐지). 시너지 장수는 패시브만 세고 임계 3/5/7장은 유지한다. 카드 출현은 편성과 무관하며, 패시브 3택 중 1장은 지금까지 가장 많이 고른 종족에서 나온다(픽 이력 기준 보정). 영웅 스킬 해금 HP 를 85/65/45% 에서 75/55/35% 로 바꾼다(5직업 공통). 허용 범위는 ① 카드·시너지 JSON(`Card` 축 필드 삭제, `Card_Species` 재정의, `Card_Param`, 신규 `SynergyTier`·`SynergyTier_Effect`·`SynergyTier_Effect_Param`, `BalanceConfig.passiveFocusSlots`, `HeroArchetype_Skill` 값) ② 신규 카드 SO 30개와 카드 풀 SO 재구성, `SynergyVisualConfig` 6종족화 ③ 카드 선택창(종족 칩 6개·종족 아이콘 카드·발동 예고 배지)·전투 HUD 시너지 패널(6행·접힌 탭 점 6개)·시너지 모달·빌드 모달·편성 종족 행 축 칩·스포너 칸 색띠·도감 카드 탭의 표시 변경(이 화면들의 위젯 추가·숨김·크기 조정 포함 - "공통 UI 아트 교체" 항목의 프리팹 구조 불변 원칙의 예외, 신규 화면·팝업 없음) ④ 기존 몬스터 UI 아이콘 6장을 카드·시너지 표시에 재사용("몬스터 UI 아이콘 교체" 항목의 사용처 확장, 파일 변경 없음) ⑤ 성기사 디버프 저항 대상에 신규 영웅 디버프 2종 추가 ⑥ 도전과제 문구. `MetaProfile` 버전·필드는 바꾸지 않는다. **신규 몬스터, 신규 카드 일러스트·신규 아이콘 파일, 카드 덱 편집·카드 해금·카드 강화, 시너지 임계 변경, 신규 스테이지 규칙, 신규 화면, ★ 목표는 계속 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지. 랭킹·최단 기록 규칙은 기존(Easy 한정) 그대로다. 수치는 qa-simulator 게이트 SS1~SS9 로 확정한다. 세부는 `docs/design/species-synergy-build.md`.

### 17.2 기존 항목 수정 (before → after)

| 항목 | before | after |
|---|---|---|
| 몬스터 육성 재화 분리 ⑤ | "…Debuff 축 패시브 카드 "독장판"(`HeroPoisonAura`)을 "끈질긴 저주"(`LingeringCurse` - Debuff 축 시한 디버프 지속 연장)로 교체(카드 총수 28장·축당 7장 유지), 연동되는 카드 "역병의 손길"·Debuff 시너지 Tier1·상점 "짙은 역병" 효과 교체." | "…패시브 카드 "독장판"(`HeroPoisonAura`)을 "끈질긴 저주"(`LingeringCurse` - 영웅 시한 디버프 지속 연장)로 교체, 연동되는 카드 "역병의 손길"·상점 "짙은 역병" 효과 교체. 카드 총수·시너지 구성은 "종족 시너지 빌드" 항목을 따른다." |
| 던전 자유 편성 | "편성·빈칸·봉인 여부는 기존 카드 덱의 무작위 추첨에 영향을 주지 않으며" | "편성·빈칸·봉인 여부는 카드 덱 추첨에 영향을 주지 않으며(패시브 추첨 보정은 픽 이력 기준 — "종족 시너지 빌드" 항목)" |
| 몬스터 UI 아이콘 교체 | "전투 HUD 스포너 칸·상점·편성에서 쓰는 몬스터 아이콘 6장" | "전투 HUD 스포너 칸·상점·편성·카드·시너지 표시에서 쓰는 몬스터 아이콘 6장" |

- "몬스터 등급 2~5" 항목의 "카드·시너지 문구는 기존 이름 유지" 는 그대로 성립(시너지 이름 = 종족 기존 이름) — 수정 없음.

### 17.3 컨셉서 동기화 (`docs/design/project_lair_concept.md`)

- §5.2 시너지 가시성: 4축 → 6종족, 칩·HUD 6행.
- §11.2 카드 범위 표: 패시브 16 · 액티브 12 → 패시브 48(종족당 8) · 액티브 10.

---

## 18. 대체되는 문서 (구현 시 각 문서 머리에 "대체됨 → species-synergy-build.md" 한 줄)

- `card-renewal.md` — 4축 정의 · 28장 라인업 · Layer 1 Tier 표 · Layer 2 중첩.
- `card-3pick-cap.md` — 3픽 캡 전체(상한 1로 대체).
- `card-synergy-indicator.md` — 4칩 · 4행 · 축 아이콘(행 위젯 산식은 §11.1 이 계승).
- `synergy-modal-popup.md` · `synergy-modal-visual-polish.md` — 축 4 → 종족 6 부분.
- `monster-growth-currencies.md` §5 "카드 시너지 4축" 행 · §15.2 Debuff Tier1(내용은 역병귀 Tier1 로 계승).
- `chapter-5.md` — 신앙의 가호 대상·정화의 빛 해제 목록(§10.3 신규 디버프 2종 추가, "Debuff 축" 표현 교체).

---

## 19. 열린 질문 (추천안)

| # | 질문 | 추천 |
|---|---|---|
| Q9 (최우선) | 결정 D5 의 "Tier1=HP70% · Tier2=50% · Tier3=30% 시점에 고정" 을 이 문서는 **"한 종족 7장 연속일 때의 가장 빠른 시점"** 으로 해석했다. 앞쪽에서 다른 종족을 섞으면 그 장수만큼 10%p 씩 늦어진다(9장·HP 10% 간격 구조상 피할 수 없음, §3.3). 이 해석이 맞는가? | **맞다로 확정.** 다른 종족은 8·9번째(HP 20·10%)에 섞으면 결정타 리듬이 그대로 지켜진다. 시점을 문자 그대로 고정하려면 "Tier 를 장수가 아니라 HP 시점에 판정" 하는 별도 규칙이 필요하고, 그러면 장수 임계 3/5/7 의 의미가 흐려진다 |
| Q1 | 보장 1장 방식이면 "한 종족만 고르는" 플레이의 Tier3 도달률이 구조상 100% 에 가깝다. 의도와 맞는가? | **맞다로 확정.** 긴장은 "보장 카드 1장 vs 다른 종족 2장" 선택과 "섞으면 결정타가 늦어진다" 에서 나온다. SS3 무작위 ≤ 10% 로 "아무렇게나 고르면 안 열린다" 만 보장 |
| Q2 | 보장 카드를 항상 왼쪽 첫 칸에 둘까, 무작위 위치에 둘까 | **왼쪽 고정.** "왼쪽 = 내 주력" 이 한 번에 학습된다 |
| Q3 | 재사용 18장의 기존 일러스트를 패시브 카드 화면에서 쓰지 않는다(종족 아이콘 통일) | **통일.** 48장이 한 문법. 일러스트 파일은 삭제하지 않음(도감 외 사용처 확인 후 별도 정리) |
| Q4 | 보스 스테이지 페이즈 규칙(HP 75/55/35)과 새 영웅 스킬 HP 가 같은 순간이 된다 | **유지.** "영웅 각성 = 전장 변화" 한 순간. 연출은 스킬 컷인 → 규칙 FX 순 |
| Q5 | 카드 개편 후 최단 기록·랭킹을 초기화할까 | **초기화하지 않음.** 기록은 개편 전후 섞이지만 시즌 개념이 없는 기록이라 리셋 비용이 이득보다 크다 |
| Q6 | 4축 아이콘 4장(`SynergyIcons`) 처리 | **사용처 0 확인 후 삭제**(이 기능 마지막 커밋에서 사용자 확인) |
| Q7 | 액티브 카드 선택창에서 "내 시너지" 칩 줄을 숨긴다 | **숨김.** 액티브는 시너지에 안 들어가므로 보이면 오해 |
| Q8 | 편성 종족 행의 축 칩(Tank 등)을 숨긴다 | **숨김.** 종족 = 시너지라 정보 중복 |

---

## 20. Self-Review

- Placeholder: 미정 마커 · 애매한 권유 · 두 갈래 위임 0건. 수치는 전부 초기값 + §15 게이트로 확정. 행 수(58 · 48 · 65 · 18 · 23 · 22)는 §12.4 · §16.3 · §16.4 · §16.10 에서 동일.
- 일관성: 임계 3/5/7, 보장 슬롯 1, 스킬 HP 0.75/0.55/0.35, 액티브 10장·5회, 문자열 id 1900~1999, effectKind 16개 이름이 §4·§5·§6·§12·§16·§17 에서 동일.
- 명명: `SpeciesStat` · `SpeciesSpawnOutput` · `SpeciesSpawnPeriod` · `SpeciesSummon` · `PlagueToxinMaxStack` · `EternalBleed` · `HexCurseStorm` · `passiveFocusSlots` · `SynergyTier_Effect_Param` 표기 통일.
- 스코프: 단일 문서, 구현은 §16.9 6슬라이스.
- 구현 요청: Enum · Interface · 에셋 키 · SO 스키마 · JSON 행 · 문자열 · 코드 접점 · 테스트 포함.
- UI 목업: `.mockups/species-synergy-build.html` (카드 선택창 · 전투 HUD, 도트 UI 톤은 `card-synergy-indicator.html` 재사용).
