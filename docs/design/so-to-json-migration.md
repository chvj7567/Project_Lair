# SO → JSON 데이터 이전 — 기획서 (구현용)

> 사용자용 요약: `docs/design/so-to-json-migration.html`. 수치·규칙의 단일 진실은 이 md 다.
> 작성 기준일 2026-11-12. 모든 초기 값은 `.asset` YAML 을 직접 읽어 옮겼다(동기 사본 JSON 이 아니라 에셋이 출처).

---

## §0 헤더

- **목표**: ScriptableObject 에 들어 있는 콘텐츠·밸런스 수치와 표시 문구를 JSON 16개로 옮기고, SO 에는 `UnityEngine.Object` 참조(스프라이트·SO 목록)만 남긴다.
- **검증 가설**: "수치·문구의 저장 위치만 바꿔도 게임 동작이 한 글자·한 수치도 달라지지 않는다" — 이전 전 SO 값과 JSON 로드 결과를 전 필드 비교하는 패리티 테스트(§10 SJ1)로 증명한다. 재미·밸런스 가설은 없다(수치 변경 0).
- **현재 단계 범위 적합성**: 범위 안. 신규 기능·콘텐츠·화면 0건, Rule 02 §11(JSON 우선) 준수를 위한 데이터 저장 위치 이전이다. `MetaProfile` 버전·필드·세이브 키 불변(세이브 마이그레이션 없음), CLAUDE.md §8 의 금지 항목(신규 몬스터·신규 화면·신규 재화 등)에 해당하는 변경 없음.
- **핵심 메커니즘**:
  1. 수치·문구는 `Assets/_Lair/Data/Json/<표 이름>.json` 으로 가고, 문구는 `Strings_Ko` 의 새 id 를 `nameKey`/`descKey` 로 가리킨다.
  2. 런타임은 기존 `ChapterDatabaseLoader` 가 새 JSON 을 읽어 `*.Current` 에 올리고, 씬이 SO 를 직접 물고 있던 `[SerializeField]` 참조는 사라진다.
  3. JSON 이 단일 진실이 되므로 에디터 동기 도구(JSON Sync 창·syncer·동기 사본 JSON 4개)는 전부 제거한다.

---

## §1 배경과 감사 정정

### 1.1 규칙

Rule 02 §11 — "콘텐츠·밸런스 데이터는 JSON, ScriptableObject 는 `UnityEngine.Object` 참조가 필요할 때만". Rule 02 §11-1 — 엔티티 고유 데이터만 row 에, 선택적 관계는 `<부모>_<관계명>` 별도 JSON, sentinel 금지, 그룹 내 여러 항목은 복합 키.

### 1.2 메인 감사 결과와 실제 에셋의 차이 (이 기획서는 실제 값을 따른다)

| 항목 | 메인 감사 | 실제 (`.asset`·`.cs` 확인) |
|---|---|---|
| 도전과제 수 | 9개 | **13개** (`MetaConfig.asset` `Achievements` — FirstRun ~ SynergyTier3) |
| 영주 보상 행 | 수 미기재 | 9행 (Lv2~10), 그중 잠금 더미 4행 (Lv4·6·8·10) |
| 카드 효과 클래스 | 26개 | **28개** (`Scripts/Card/Effects/*.cs`, 카드 28장과 1:1) |
| 카드 효과 파라미터 | 수 미기재 | 26개 값 (카드 21장), 파라미터 없는 카드 7장 |
| 스킬 SO | 서브클래스 15개 | 클래스 15 + 에셋 15 + 로드아웃 에셋 5. **Object 참조 0** (수치 + 이름뿐) |
| C2 "Race.json 과 이중 진실" | 이중 진실 | **이중 진실 아님** — `Race.json` 은 종족 이름(이그니스), `SpeciesVisual` 은 몬스터 이름(도깨비불). 서로 다른 문구다. C# 하드코딩이라는 점만 위반 |
| `BattleHudArg.Balance` | 소비처 | 대입만 있고 읽는 곳 0 — 삭제 대상 |
| 코드 기본값 ≠ 에셋 값 | - | 스킬 3종 6필드(AoeNova cooldown 7→5, DashStrike dashLength 7→3·coneHalfAngle 35→180·knockbackStrength 2→1·centroidRadius 8→5, OrbitingBlade damage 15→30). JSON 은 **에셋 값** |
| `SlowEffect._monsterMul` | - | 직렬화돼 있으나 코드가 읽지 않는 값(1.3). 그대로 이전한다(§4.9) |
| `HeroStageVariant.UseEmission` | 수치 동거 | 25행 전부 false → `EmissionColor`·`EmissionIntensity` 는 적용된 적 없는 죽은 값 |

### 1.3 범위

| 묶음 | 대상 | 이 기획서 |
|---|---|---|
| A1 | `BalanceConfig` | 포함 |
| A2 | `MetaConfig` | 포함 |
| B1 | 영웅 스킬 SO 15 + 로드아웃 SO 5 | 포함 |
| B2 | 카드 SO 28 | 포함 |
| C1 | `HeroStageVariantConfig` 수치 | 포함 권장 (§14 Q4, 사용자 선택 필요) |
| C2 | `SpeciesVisual` 하드코딩 | 제외 권장 (§14 Q5, 사용자 선택 필요) |

구현 순서 A1 → A2 → B1 → B2 → C1, 단계마다 독립 커밋(§11).

---

## §2 경계 표 — 무엇이 JSON 으로 가고 무엇이 SO 에 남는가 (필드 단위)

### 2.1 A1 `BalanceConfig`

| SO 필드 | 가는 곳 | SO 잔존 |
|---|---|---|
| `_hero.Hp` | `BalanceConfig.json` `heroHp` | 없음 |
| `_hero.Power` | `BalanceConfig.json` `heroPower` | 없음 |
| `_runDuration` | `BalanceConfig.json` `runDuration` | 없음 |
| `_passiveThresholds[9]` | `BalanceConfig.json` `passiveThresholds` | 없음 |
| `_activeThresholds[5]` | `BalanceConfig.json` `activeThresholds` | 없음 |
| `_monsters[].Key` | `MonsterStat.json` `monster` | 없음 |
| `_monsters[].Stat.{Hp,Power,Range,Cooldown,MoveSpeed}` | `MonsterStat.json` `hp`·`power`·`range`·`cooldown`·`moveSpeed` | 없음 |
| `_monsters[].SpawnPeriod` | `MonsterStat.json` `spawnPeriod` | 없음 |

결과: `BalanceConfig.asset` 삭제. SO 에 남는 필드 0.

### 2.2 A2 `MetaConfig`

| SO 필드 | 가는 곳 | SO 잔존 |
|---|---|---|
| `WinBaseSouls`·`WinTimeBonusPerSec`·`LoseMaxSouls`·`WinXp`·`LoseXp`·`XpPerLevelBase`·`XpGrowth` | `MetaConfig.json` (같은 이름 camelCase) | 없음 |
| `ShopItems[].Id` | `ShopItem.json` `itemId` | 없음 |
| `ShopItems[].DisplayName` | `ShopItem.json` `nameKey` → `Strings_Ko` | 없음 |
| `ShopItems[].Description` (19행만 값 있음) | `ShopItem_Desc.json` `descKey` → `Strings_Ko` | 없음 |
| `ShopItems[].EffectKind` | `ShopItem.json` `effectKind` | 없음 |
| `ShopItems[].StatKind` (MonsterStat 6행만 의미) | `ShopItem_Stat.json` `statKind` | 없음 |
| `ShopItems[].Species` (종족 항목 24행만 의미) | `ShopItem_Species.json` `monster` | 없음 |
| `ShopItems[].PerLevelMul` (13행만 의미, 나머지 18행은 1.0 고정·미사용) | `ShopItem_Mul.json` `perLevelMul` | 없음 |
| `ShopItems[].MaxLevel`·`BasePrice`·`PriceGrowth`·`Currency` | `ShopItem.json` `maxLevel`·`basePrice`·`priceGrowth`·`currency` | 없음 |
| `LordRewards[].Level` | `LordLevel.json` `level` | 없음 |
| `LordRewards[].IsLockedDummy` | 저장하지 않음 — `LordLevel_Reward` 행이 없으면 잠금 칸 | 없음 |
| `LordRewards[].RewardSouls` (실보상 5행) | `LordLevel_Reward.json` `rewardSouls` | 없음 |
| `LordRewards[].DisplayName` (실보상 5행) | `LordLevel_Reward.json` `nameKey` → `Strings_Ko` | 없음 |
| `LordRewards[].DisplayName` (잠금 4행, 물음표 3개 문자열) | **이전하지 않음** — 화면이 읽지 않는 값(`LordLevelPopup` 이 잠금 칸에는 코드 리터럴을 표시) | 없음 |
| `Achievements[].Id` | `Achievement.json` `achievementId` | 없음 |
| `Achievements[].DisplayName`·`Description` | `Achievement.json` `nameKey`·`descKey` → `Strings_Ko` | 없음 |
| `Achievements[].Condition`·`Threshold`·`RewardSouls` | `Achievement.json` `condition`·`threshold`·`rewardSouls` | 없음 |

결과: `MetaConfig.asset` 삭제. SO 에 남는 필드 0.

### 2.3 B1 영웅 스킬

| SO 필드 | 가는 곳 | SO 잔존 |
|---|---|---|
| `HeroSkillData._displayName` | `HeroSkill.json` `nameKey` → `Strings_Ko` | 없음 |
| 서브클래스 수치 `[SerializeField]` 97개 (§4.7) | `HeroSkill_Param.json` `paramKey`·`value` | 없음 |
| 스킬 종류(= SO 서브클래스 타입) | `HeroSkill.json` `skillId` (= `EHeroSkill` 값명) | 없음 |
| `HeroSkillLoadout._phases[].HpFraction` | `HeroArchetype_Skill.json` `hpFraction` | 없음 |
| `HeroSkillLoadout._phases[].Skill` (SO → SO 참조) | `HeroArchetype_Skill.json` `skillId` | 없음 |
| 로드아웃 ↔ 영웅 매핑 (`BattleController.LoadoutKeys` 코드 표) | `HeroArchetype_Skill.json` `heroId` | 없음 |

결과: 스킬 에셋 15 + 로드아웃 에셋 5 삭제. 스킬 SO 는 `UnityEngine.Object` 참조가 하나도 없고(이펙트는 코드의 `EVisual` Enum 키), 로드아웃이 가리키던 대상(스킬 SO)이 사라지므로 로드아웃도 SO 로 남길 이유가 없다. 대안 비교는 §13 D2.

### 2.4 B2 카드

| SO 필드 | 가는 곳 | SO 잔존 |
|---|---|---|
| `CardData._id` | `Card.json` `cardId` (SO 와 행을 잇는 키) | **잔존** — SO ↔ JSON 행 매칭 키 |
| `CardData._axis` | `Card.json` `axis` | 없음 |
| `CardData._displayName` | `Card.json` `nameKey` → `Strings_Ko` | 없음 |
| `CardData._description` | `Card.json` `descKey` → `Strings_Ko` | 없음 |
| `CardData._effect` 의 타입 (`[SerializeReference]`) | `Card.json` `effectKind` (= `ECardEffect` 값명) | 없음 |
| `CardData._effect` 의 수치 26개 (§4.9) | `Card_Param.json` `paramKey`·`value` | 없음 |
| `CardData._icon` (Sprite) | 그대로 | **잔존** |
| `CardData._cardImage` (Sprite) | 그대로 | **잔존** |
| `CardPool._cards` (CardData SO 목록, 순서 포함) | 그대로 | **잔존** — Object 참조뿐인 SO. 대안 비교는 §13 D4 |

결과: `CardData` 에셋 28개는 `_id`·`_icon`·`_cardImage` 3필드만 남긴다. `CardPool_Passive`·`CardPool_Active` 에셋은 변경 없음.

### 2.5 C1 `HeroStageVariantConfig` (포함 권장)

| SO 필드 | 가는 곳 | SO 잔존 |
|---|---|---|
| `ChapterId`·`StageIndex` | 그대로 (SO 엔트리 키) | **잔존** |
| `Portrait` (Sprite) | 그대로 | **잔존** |
| `ScaleMultiplier` | `Stage.json` 신규 필드 `heroScale` | 없음 |
| `Tier` | `Stage.json` 신규 필드 `heroTier` | 없음 |
| `UseEmission`·`EmissionColor`·`EmissionIntensity` | **삭제** (25행 전부 `UseEmission` false 라 적용된 적 없음) — 사용자 선택 필요(§14 Q4) | 없음 |

### 2.6 C2 `SpeciesVisual` (제외 권장)

이번에 옮기지 않는다. 이유와 후속 방향은 §13 D9.

### 2.7 변경 없는 SO (참고)

`SynergyVisualConfig`(축 아이콘 스프라이트), `MonsterTierSheetSet` 18개(시트 텍스처), `CardPool` 2개 — 전부 Object 참조 전용이라 규칙에 맞다.

---

## §3 데이터 구조표 (구조 확인 게이트 대상)

### 3.1 한눈에 보는 트리

범례: **[E]** 엔티티 · **[R]** 관계 데이터 · **[C]** 단일 설정 객체 · `PK` 기본키 · `FK→` 가리키는 대상.

```
[C] BalanceConfig            전투 전역 상수 (영웅 앵커·런 길이·카드 트리거 시점)
[E] MonsterStat              PK monster(EMonster 기준 6종)          기준 몬스터 한 종의 기본 능력치·스폰 주기

[C] MetaConfig               소울·영주 XP 공식 상수 7개
[E] ShopItem                 PK itemId                              상점 항목 하나
 ├─[R] ShopItem_Stat         PK itemId   FK→ShopItem, EMonsterStatKind   전종 스탯 강화 항목만 (6행)
 ├─[R] ShopItem_Species      PK itemId   FK→ShopItem, EMonster(기준 6종) 종족 항목만 (24행)
 ├─[R] ShopItem_Mul          PK itemId   FK→ShopItem                    레벨당 배율이 있는 항목만 (13행)
 └─[R] ShopItem_Desc         PK itemId   FK→ShopItem, Strings_Ko        설명 문구가 있는 항목만 (19행)
       ShopItem.currency     FK→Currency.currencyId (필수 1:1, 직접 필드)
       ShopItem.nameKey      FK→Strings_Ko (필수 1:1, 직접 필드)
[E] LordLevel                PK level                               영주 보상 트랙에 표시되는 레벨 칸 하나
 └─[R] LordLevel_Reward      PK level    FK→LordLevel, Strings_Ko       보상이 있는 칸만 (5행). 행 없음 = 잠금 칸
[E] Achievement              PK achievementId                       고정 도전과제 하나

[E] HeroSkill                PK skillId(EHeroSkill)                 영웅 스킬 하나
 └─[R] HeroSkill_Param       PK skillId + paramKey   FK→HeroSkill       스킬 수치 하나 (키-값)
[R] HeroArchetype_Skill      PK heroId + phaseIndex  FK→HeroArchetype, HeroSkill
                                                                    이 영웅이 이 HP 구간에서 여는 스킬

[E] Card                     PK cardId(ECardId)                     카드 하나 (그림은 CardData SO 가 cardId 로 연결)
 ├─[R] Card_Param            PK cardId + paramKey    FK→Card            카드 효과 수치 하나 (키-값)
 └─[R] Card_Species (기존)   PK cardId + monster     FK→Card            (변경 없음 — 부모 엔티티 Card 가 이제 실재)

(C1) Stage (기존) + heroScale, heroTier  — 모든 스테이지가 하나씩 갖는 필수 1:1 이라 직접 필드
```

신설 JSON 16개. 기존 JSON 변경은 C1 의 `Stage.json` 필드 2개 추가뿐.

### 3.2 `BalanceConfig.json` — [C] 단일 객체

핵심 엔티티 한 문장: "전투 한 판에 공통으로 적용되는 전역 상수 묶음".

| 필드 | 타입 | 키 | 의미 |
|---|---|---|---|
| `heroHp` | int | - | 영웅 HP 앵커. 실제 HP = 앵커 x `HeroArchetype.hpScale` x `Stage.hpMultiplier` x 난이도 배율 |
| `heroPower` | int | - | 영웅 공격력 앵커 |
| `runDuration` | float | - | 한 판 길이(초) |
| `passiveThresholds` | float[] | - | 패시브 카드 3택1 을 여는 영웅 HP 비율 목록 (내림차순) |
| `activeThresholds` | float[] | - | 액티브 카드 3택1 을 여는 경과 초 목록 (오름차순) |

배열 2개를 별도 표로 쪼개지 않는 근거: 원소가 속성 없는 스칼라 목록이고 그룹이 하나뿐이라 `groupId + index` 가 표현할 관계가 없다. Rule 02 §11 의 예시 row(`recoilKicks` 배열)와 같은 형태다. 대안 비교는 §13 D6.

### 3.3 `MonsterStat.json` — [E]

핵심 엔티티: "기준 몬스터 한 종의 기본 능력치와 스폰 주기". 등급 몬스터(`WispG2` 등)는 행을 갖지 않고 기준 종 행을 쓴다(현행 `MonsterGrades.BaseOf` 규칙 그대로).

| 필드 | 타입 | 키 | 의미 |
|---|---|---|---|
| `monster` | string | **PK**, FK→`EMonster` 기준 6종 | 값명 |
| `hp` | int | - | 기본 HP |
| `power` | int | - | 기본 공격력 |
| `range` | float | - | 사거리 |
| `cooldown` | float | - | 공격 간격(초) |
| `moveSpeed` | float | - | 이동속도 |
| `spawnPeriod` | float | - | 이 종을 출력하는 둥지의 기본 스폰 주기(초). 모든 종이 하나씩 갖는 필수 1:1 이라 직접 필드 |

이름을 `MonsterStat` 으로 두는 근거: 같은 키(기준 몬스터)를 쓰는 기존 표 `MonsterTrait`·`AwakenStat` 과 같은 명명이다.

### 3.4 `MetaConfig.json` — [C] 단일 객체

핵심 엔티티: "소울 보상과 영주 XP 공식에 쓰이는 상수 묶음".

| 필드 | 타입 | 의미 |
|---|---|---|
| `winBaseSouls` | int | 승리 기본 소울 |
| `winTimeBonusPerSec` | float | 승리 시 잔여 1초당 보너스 |
| `loseMaxSouls` | int | 패배 시 최대 소울 |
| `winXp` | int | 승리 XP |
| `loseXp` | int | 패배 XP |
| `xpPerLevelBase` | int | Lv1→2 필요 XP |
| `xpGrowth` | float | 레벨당 필요 XP 증가율 |

### 3.5 `ShopItem.json` — [E]

핵심 엔티티: "상점에서 레벨을 올려 사는 영구 강화 항목 하나". **행 순서 = 상점 표시 순서**(현행 SO 리스트 순서 그대로).

| 필드 | 타입 | 키 | 의미 |
|---|---|---|---|
| `itemId` | string | **PK** | `MetaProfile.ShopLevels[].ItemId` 가 쓰는 세이브 키 — 글자 그대로 불변 |
| `nameKey` | int | FK→`Strings_Ko` | 항목 이름 (필수 1:1) |
| `effectKind` | string | FK→`EShopEffectKind` 값명 | 효과 종류 (필수 1:1) |
| `maxLevel` | int | - | 최대 레벨 |
| `basePrice` | int | - | Lv0→1 가격 |
| `priceGrowth` | float | - | 레벨당 가격 배율 |
| `currency` | string | FK→`Currency.currencyId` | 결제 재화 (필수 1:1) |

### 3.6 `ShopItem_Stat` · `ShopItem_Species` · `ShopItem_Mul` · `ShopItem_Desc` — [R] 선택적 관계 4종

| 표 | 필드 | 타입 | 키 | 행이 존재하는 조건 |
|---|---|---|---|---|
| `ShopItem_Stat` | `itemId` | string | **PK**, FK→`ShopItem` | `effectKind` = `MonsterStat` 인 항목만 |
| | `statKind` | string | FK→`EMonsterStatKind` 값명 | |
| `ShopItem_Species` | `itemId` | string | **PK**, FK→`ShopItem` | `effectKind` ∈ {`MonsterSpecies`, `MonsterAwaken`, `NestMastery`, `MonsterTrait`} 인 항목만 |
| | `monster` | string | FK→`EMonster` 기준 6종 | |
| `ShopItem_Mul` | `itemId` | string | **PK**, FK→`ShopItem` | `effectKind` ∈ {`MonsterStat`, `SpawnerPeriod`, `MonsterSpecies`} 인 항목만 |
| | `perLevelMul` | float | - | 레벨당 곱 배율 |
| `ShopItem_Desc` | `itemId` | string | **PK**, FK→`ShopItem` | 설명 문구가 있는 항목만 (현재 19행) |
| | `descKey` | int | FK→`Strings_Ko` | |

선택적 관계로 가른 근거 (Rule 02 §11-1-3):
- `statKind`·`monster` 는 SO 에서 "해당 종류일 때만 사용, 나머지는 0" 인 sentinel 필드였다.
- `perLevelMul` 은 육성 3축 18행에서 1.0 고정·미사용이었다("배율 없음"을 1.0 으로 표현한 sentinel).
- `Description` 은 둥지·특성 12행에서 빈 문자열이었다(그 칸 문구는 `NestMastery`·`MonsterTrait` 데이터에서 조립).

`effectKind` 별 관계 행 규칙 (검증 SJ4 가 누락·초과 모두 오류로 잡는다):

| `effectKind` | `_Stat` | `_Species` | `_Mul` | `_Desc` |
|---|---|---|---|---|
| `MonsterStat` | 필수 | 금지 | 필수 | 선택 |
| `SpawnerPeriod` | 금지 | 금지 | 필수 | 선택 |
| `MonsterSpecies` | 금지 | 필수 | 필수 | 선택 |
| `MonsterAwaken` | 금지 | 필수 | 금지 | 선택 |
| `NestMastery` | 금지 | 필수 | 금지 | 선택 |
| `MonsterTrait` | 금지 | 필수 | 금지 | 선택 |

### 3.7 `LordLevel.json` · `LordLevel_Reward.json`

`LordLevel` [E] — 핵심 엔티티: "영주 보상 트랙에 표시되는 레벨 칸 하나". 트랙 만렙은 이 표의 최대 `level`(현행 `TrackMaxLevel` 규칙 그대로).

| 필드 | 타입 | 키 |
|---|---|---|
| `level` | int | **PK** |

`LordLevel_Reward` [R] — 선택적 관계: 보상이 있는 칸만 행 존재. 행이 없는 칸 = 잠금 칸(현행 `IsLockedDummy`).

| 필드 | 타입 | 키 | 의미 |
|---|---|---|---|
| `level` | int | **PK**, FK→`LordLevel` | |
| `nameKey` | int | FK→`Strings_Ko` | 보상 이름 |
| `rewardSouls` | int | - | 지급 소울 |

메인 초안(`LordReward{level, nameKey}` + `LordReward_Souls{level, amount}`)과 다른 이유: 잠금 칸의 이름은 화면이 읽지 않는 값이라 엔티티에 `nameKey` 를 두면 4행이 가짜 문구를 가리키게 된다. 이름과 소울은 "보상이 있는 칸"에만 함께 존재하므로 둘을 같은 관계 데이터에 둔다. 대안 비교는 §13 D5.

### 3.8 `Achievement.json` — [E]

핵심 엔티티: "고정 도전과제 하나". **행 순서 = 퀘스트 팝업 표시 순서**.

| 필드 | 타입 | 키 | 의미 |
|---|---|---|---|
| `achievementId` | string | **PK** | `MetaProfile.AchievedIds` 가 쓰는 세이브 키 — 글자 그대로 불변 |
| `nameKey` | int | FK→`Strings_Ko` | |
| `descKey` | int | FK→`Strings_Ko` | |
| `condition` | string | FK→`EAchievementCondition` 값명 | |
| `threshold` | float | - | 조건 임계 |
| `rewardSouls` | int | - | 달성 보상 |

전 필드가 모든 행에 존재하는 필수 1:1 이라 관계 데이터 없음.

### 3.9 `HeroSkill.json` · `HeroSkill_Param.json` · `HeroArchetype_Skill.json`

`HeroSkill` [E] — 핵심 엔티티: "영웅 스킬 하나". `skillId` 가 곧 동작 종류(코드의 런타임 클래스 선택 키)다.

| 필드 | 타입 | 키 |
|---|---|---|
| `skillId` | string | **PK**, FK→`EHeroSkill` 값명 (신규 Enum) |
| `nameKey` | int | FK→`Strings_Ko` (해금 컷인에 표시되는 이름) |

`HeroSkill_Param` [R] — 스킬 수치 하나. 스킬마다 필드 구성이 달라 키-값 (`Relic_Effect_Param`·`Stage_Rule_Param`·`MonsterTrait_Param` 선례).

| 필드 | 타입 | 키 |
|---|---|---|
| `skillId` | string | **복합 PK** (1/2), FK→`HeroSkill` |
| `paramKey` | string | **복합 PK** (2/2) |
| `value` | float | 정수 의미 값(damage·tickCount 등)도 float 로 저장하고 읽을 때 반올림 |

`HeroArchetype_Skill` [R] — `HeroArchetype` 의 관계: "이 영웅이 HP 가 이 비율 이하로 내려가면 이 스킬을 연다". 한 영웅에 여러 행이라 복합 키(Rule 02 §11-1-4).

| 필드 | 타입 | 키 |
|---|---|---|
| `heroId` | string | **복합 PK** (1/2), FK→`HeroArchetype.heroId` |
| `phaseIndex` | int | **복합 PK** (2/2), 1부터 연속 |
| `hpFraction` | float | 해금 HP 비율 (0~1) |
| `skillId` | string | FK→`HeroSkill` |

스킬별 필수 `paramKey` 집합 (누락·초과 모두 오류):

| `skillId` | 필수 `paramKey` | 개수 |
|---|---|---|
| `DashStrike` | damage, cooldown, dashLength, coneHalfAngle, knockbackStrength, centroidRadius | 6 |
| `AoeNova` | damage, cooldown, radius, knockbackStrength | 4 |
| `OrbitingBlade` | damage, hitInterval, orbitRadius, bladeSphereRadius, bladeCount, rotationSpeedDeg | 6 |
| `PiercingShot` | damage, cooldown, lineLength, lineHalfWidth, knockbackStrength, centroidRadius | 6 |
| `ArrowVolley` | damagePerTick, tickCount, tickIntervalSec, radius, cooldown, targetSearchRadius | 6 |
| `Evade` | damage, cooldown, leapDistance, leapDurationSec, blastRadius, knockbackStrength, triggerMonsterCount, triggerRadius | 8 |
| `ShadowStrike` | damage, cooldown, blinkRange, minBlinkDistance, arriveOffset, invulnerableSec, killRefundFraction | 7 |
| `BladeWhirl` | damagePerTick, tickCount, tickIntervalSec, radius, cooldown, damageTakenMul, triggerMonsterCount, triggerRadius | 8 |
| `SmokeBomb` | radius, durationSec, monsterSpeedMul, monsterRangeMul, monsterMinRange, cooldown, triggerMonsterCount, triggerRadius | 8 |
| `CrushingLeap` | damage, cooldown, leapRange, minLeapDistance, leapDurationSec, invulnerableSec, slamRadius, knockbackStrength, clusterRadius | 9 |
| `ChainPull` | damage, cooldown, pullRadius, maxTargets, pullToDistance, pullDurationSec, triggerMonsterCount, triggerRadius | 8 |
| `BloodFrenzy` | durationSec, attackCooldownMul, damageTakenMul, cooldown, triggerMonsterCount, triggerRadius | 6 |
| `JudgmentHammer` | damage, cooldown, radius, stunSec, castRange | 5 |
| `PurifyingLight` | triggerDebuffCount, healPercent, cooldown | 3 |
| `Sanctuary` | durationSec, damageTakenMul, radius, monsterSpeedMul, cooldown, triggerMonsterCount, triggerRadius | 7 |

검산: 6+4+6+6+6+8+7+8+8+9+8+6+5+3+7 = **97**.

### 3.10 `Card.json` · `Card_Param.json`

`Card` [E] — 핵심 엔티티: "카드 하나". **행 순서 = `ECardId` 정수값 순**.

| 필드 | 타입 | 키 | 의미 |
|---|---|---|---|
| `cardId` | string | **PK**, FK→`ECardId` 값명 | `CardData` SO 의 `_id` 와 매칭 |
| `axis` | string | FK→`EBuildAxis` 값명 | 빌드 축 (필수 1:1) |
| `nameKey` | int | FK→`Strings_Ko` | 카드 이름 |
| `descKey` | int | FK→`Strings_Ko` | 카드 설명 |
| `effectKind` | string | FK→`ECardEffect` 값명 (신규 Enum) | 효과 동작 종류 (필수 1:1 — 모든 카드가 효과 하나) |

`cardId` 와 `effectKind` 를 따로 두는 근거: 카드 id 는 세이브·Enum 정수 호환 때문에 옛 이름을 유지하고(`Berserk` → 효과는 수호자의 분노) 효과 이름과 다르다. 현재 6장이 이름이 어긋난다(§4.8).

`Card_Param` [R] — 카드 효과 수치 하나. 카드당 효과가 하나라 `cardId` 로 묶는다.

| 필드 | 타입 | 키 |
|---|---|---|
| `cardId` | string | **복합 PK** (1/2), FK→`Card` |
| `paramKey` | string | **복합 PK** (2/2) |
| `value` | float | |

`effectKind` 별 필수 `paramKey` 집합 (누락·초과 모두 오류):

| `effectKind` | 필수 `paramKey` |
|---|---|
| `GuardianRage`, `BloodThirst`, `Fear`, `Frenzy`, `IronWill`, `TimeStop` | duration |
| `Bleed` | ratio, duration |
| `Weaken` | factor, duration |
| `MarkOfDeath` | dmgTakenMul, duration |
| `Slow` | heroFactor, monsterMul, duration |
| `LingeringCurse` | durationBonusPerPick |
| `HexRangeBoost` | rangeMul |
| `PhantomMoveSpeedBoost` | speedMul |
| `PlagueToxinBoost` | toxinMul |
| `ReaperAtkSpeed` | cdMul |
| `ReaperHexPowerBoost`, `WispWraithPowerBoost` | powerMul |
| `FastBreeding`, `SpawnerHaste` | periodMul |
| `WispHpBoost`, `WraithDamageBoost` | hpMul |
| `HeroAttackDown`, `ToughHide`, `SpawnWisps`, `SpawnWraith`, `SpawnReapers`, `SpawnPlagues`, `SpawnPhantoms` | (없음) |

### 3.11 (C1) `Stage.json` 필드 추가

| 필드 | 타입 | 의미 |
|---|---|---|
| `heroScale` | float | 이 스테이지 영웅의 크기 배율 (현행 `ScaleMultiplier`) |
| `heroTier` | int | 이 스테이지 영웅의 강화 외형 티어 0~4 (현행 `Tier`) |

25개 스테이지 전부가 값을 갖는 필수 1:1 → 관계 데이터 없이 `Stage` 직접 필드.

---

## §4 초기 데이터 (현재 `.asset` 값 그대로)

### 4.1 `BalanceConfig.json`

| 필드 | 값 |
|---|---|
| `heroHp` | 4000 |
| `heroPower` | 50 |
| `runDuration` | 300 |
| `passiveThresholds` | 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1 (9개) |
| `activeThresholds` | 30, 90, 150, 210, 270 (5개) |

### 4.2 `MonsterStat.json` (6행)

| monster | hp | power | range | cooldown | moveSpeed | spawnPeriod |
|---|---|---|---|---|---|---|
| Wisp | 200 | 5 | 1 | 1 | 1.0 | 9 |
| Wraith | 500 | 10 | 1.3 | 1 | 0.8 | 20 |
| Reaper | 100 | 6 | 1 | 0.5 | 1.5 | 12 |
| Hex | 60 | 9 | 5 | 1 | 1.4 | 15 |
| Plague | 50 | 2 | 1 | 1 | 1.3 | 10 |
| Phantom | 30 | 2 | 1 | 0.8 | 2.4 | 6 |

### 4.3 `MetaConfig.json`

| 필드 | 값 |
|---|---|
| `winBaseSouls` | 100 |
| `winTimeBonusPerSec` | 0.5 |
| `loseMaxSouls` | 60 |
| `winXp` | 100 |
| `loseXp` | 40 |
| `xpPerLevelBase` | 100 |
| `xpGrowth` | 1.25 |

### 4.4 `ShopItem.json` (31행) + 관계 4종

| # | itemId | nameKey | 이름 | effectKind | maxLevel | basePrice | priceGrowth | currency |
|---|---|---|---|---|---|---|---|---|
| 1 | MonsterHpUp | 1541 | 강골 군세 | MonsterStat | 5 | 80 | 1.6 | Soul |
| 2 | MonsterPowerUp | 1542 | 흉포한 발톱 | MonsterStat | 5 | 100 | 1.6 | Soul |
| 3 | MonsterAtkSpeedUp | 1543 | 신속한 손놀림 | MonsterStat | 5 | 100 | 1.6 | Soul |
| 4 | MonsterMoveSpeedUp | 1544 | 유령 걸음 | MonsterStat | 5 | 80 | 1.6 | Soul |
| 5 | MonsterRangeUp | 1545 | 긴 손아귀 | MonsterStat | 5 | 120 | 1.6 | Soul |
| 6 | PlagueVenomUp | 1546 | 짙은 역병 | MonsterStat | 5 | 120 | 1.6 | Soul |
| 7 | SpawnerHasteUp | 1547 | 깨어나는 둥지 | SpawnerPeriod | 5 | 150 | 1.6 | Soul |
| 8 | Enhance_Wisp | 1548 | 도깨비불 체급 | MonsterSpecies | 5 | 150 | 1.6 | Soul |
| 9 | Enhance_Wraith | 1549 | 망령 체급 | MonsterSpecies | 5 | 150 | 1.6 | Soul |
| 10 | Enhance_Reaper | 1550 | 사신 체급 | MonsterSpecies | 5 | 150 | 1.6 | Soul |
| 11 | Enhance_Hex | 1551 | 저주술사 체급 | MonsterSpecies | 5 | 150 | 1.6 | Soul |
| 12 | Enhance_Plague | 1552 | 역병귀 체급 | MonsterSpecies | 5 | 150 | 1.6 | Soul |
| 13 | Enhance_Phantom | 1553 | 환령 체급 | MonsterSpecies | 5 | 150 | 1.6 | Soul |
| 14 | Awaken_Wisp | 1554 | 도깨비불 각성 | MonsterAwaken | 5 | 10 | 1.5 | AwakenStone |
| 15 | Awaken_Wraith | 1555 | 망령 각성 | MonsterAwaken | 5 | 10 | 1.5 | AwakenStone |
| 16 | Awaken_Reaper | 1556 | 사신 각성 | MonsterAwaken | 5 | 10 | 1.5 | AwakenStone |
| 17 | Awaken_Hex | 1557 | 저주술사 각성 | MonsterAwaken | 5 | 10 | 1.5 | AwakenStone |
| 18 | Awaken_Plague | 1558 | 역병귀 각성 | MonsterAwaken | 5 | 10 | 1.5 | AwakenStone |
| 19 | Awaken_Phantom | 1559 | 환령 각성 | MonsterAwaken | 5 | 10 | 1.5 | AwakenStone |
| 20 | Nest_Wisp | 1560 | 도깨비불 둥지 | NestMastery | 3 | 20 | 1.5 | MoonSeal |
| 21 | Nest_Wraith | 1561 | 망령 둥지 | NestMastery | 3 | 20 | 1.5 | MoonSeal |
| 22 | Nest_Reaper | 1562 | 사신 둥지 | NestMastery | 3 | 20 | 1.5 | MoonSeal |
| 23 | Nest_Hex | 1563 | 저주술사 둥지 | NestMastery | 3 | 20 | 1.5 | MoonSeal |
| 24 | Nest_Plague | 1564 | 역병귀 둥지 | NestMastery | 3 | 20 | 1.5 | MoonSeal |
| 25 | Nest_Phantom | 1565 | 환령 둥지 | NestMastery | 3 | 20 | 1.5 | MoonSeal |
| 26 | Trait_Wisp | 1566 | 도깨비불 특성 | MonsterTrait | 3 | 3 | 2 | AbyssCrystal |
| 27 | Trait_Wraith | 1567 | 망령 특성 | MonsterTrait | 3 | 3 | 2 | AbyssCrystal |
| 28 | Trait_Reaper | 1568 | 사신 특성 | MonsterTrait | 3 | 3 | 2 | AbyssCrystal |
| 29 | Trait_Hex | 1569 | 저주술사 특성 | MonsterTrait | 3 | 3 | 2 | AbyssCrystal |
| 30 | Trait_Plague | 1570 | 역병귀 특성 | MonsterTrait | 3 | 3 | 2 | AbyssCrystal |
| 31 | Trait_Phantom | 1571 | 환령 특성 | MonsterTrait | 3 | 3 | 2 | AbyssCrystal |

검산: 7(전종) + 6(체급) + 6(각성) + 6(둥지) + 6(특성) = 31. 소울 결제 13, 특수 재화 결제 18.

`ShopItem_Stat.json` (6행)

| itemId | statKind |
|---|---|
| MonsterHpUp | Hp |
| MonsterPowerUp | Power |
| MonsterAtkSpeedUp | Cooldown |
| MonsterMoveSpeedUp | MoveSpeed |
| MonsterRangeUp | Range |
| PlagueVenomUp | PoisonDamage |

`ShopItem_Species.json` (24행) — `Enhance_`·`Awaken_`·`Nest_`·`Trait_` 접두 4종 x 종 6:

| itemId 접두 | Wisp | Wraith | Reaper | Hex | Plague | Phantom |
|---|---|---|---|---|---|---|
| `Enhance_<종>` | Wisp | Wraith | Reaper | Hex | Plague | Phantom |
| `Awaken_<종>` | Wisp | Wraith | Reaper | Hex | Plague | Phantom |
| `Nest_<종>` | Wisp | Wraith | Reaper | Hex | Plague | Phantom |
| `Trait_<종>` | Wisp | Wraith | Reaper | Hex | Plague | Phantom |

(각 칸 = 그 행의 `monster` 값. 예: `Nest_Hex` → `Hex`.)

`ShopItem_Mul.json` (13행)

| itemId | perLevelMul |
|---|---|
| MonsterHpUp | 1.02 |
| MonsterPowerUp | 1.015 |
| MonsterAtkSpeedUp | 0.99 |
| MonsterMoveSpeedUp | 1.015 |
| MonsterRangeUp | 1.015 |
| PlagueVenomUp | 1.04 |
| SpawnerHasteUp | 0.985 |
| Enhance_Wisp · Enhance_Wraith · Enhance_Reaper · Enhance_Hex · Enhance_Plague · Enhance_Phantom | 1.18 (6행 각각) |

`ShopItem_Desc.json` (19행, 문구 14종)

| itemId | descKey | 문구 |
|---|---|---|
| MonsterHpUp | 1572 | 모든 몬스터 HP +2%/Lv |
| MonsterPowerUp | 1573 | 모든 몬스터 공격력 +1.5%/Lv |
| MonsterAtkSpeedUp | 1574 | 모든 몬스터 공격속도 +1%/Lv |
| MonsterMoveSpeedUp | 1575 | 모든 몬스터 이동속도 +1.5%/Lv |
| MonsterRangeUp | 1576 | 모든 몬스터 사거리 +1.5%/Lv |
| PlagueVenomUp | 1577 | 역병귀 독 피해 +4%/Lv |
| SpawnerHasteUp | 1578 | 모든 스포너 주기 -1.5%/Lv |
| Enhance_Wisp ~ Enhance_Phantom (6행) | 1579 | HP·공격력 +18%/Lv (6행이 같은 id 공유) |
| Awaken_Wisp | 1580 | 받는 피해 -4%/Lv |
| Awaken_Wraith | 1581 | 넉백 -15% · 받는 피해 -3%/Lv |
| Awaken_Reaper | 1582 | 공격 간격 -5%/Lv |
| Awaken_Hex | 1583 | 사거리 +6%/Lv |
| Awaken_Plague | 1584 | 독 피해 +5% · 지속 +10%/Lv |
| Awaken_Phantom | 1585 | 이동속도 +6%/Lv |

검산: 7 + 6 + 6 = 19행, 문구 7 + 1 + 6 = 14종.

### 4.5 `LordLevel.json` (9행) · `LordLevel_Reward.json` (5행)

`LordLevel`: level = 2, 3, 4, 5, 6, 7, 8, 9, 10.

| level | nameKey | 이름 | rewardSouls |
|---|---|---|---|
| 2 | 1586 | 영주의 첫 봉급 | 50 |
| 3 | 1587 | 던전 운영비 | 80 |
| 5 | 1588 | 영혼 곳간 | 120 |
| 7 | 1589 | 깊어진 금고 | 150 |
| 9 | 1590 | 영주의 위엄 | 200 |

잠금 칸(보상 행 없음): 4, 6, 8, 10. 트랙 만렙 = 10(현행과 동일).

### 4.6 `Achievement.json` (13행)

| # | achievementId | nameKey | 이름 | descKey | 설명 | condition | threshold | rewardSouls |
|---|---|---|---|---|---|---|---|---|
| 1 | FirstRun | 1591 | 던전 개장 | 1604 | 첫 출격을 마친다 | TotalRuns | 1 | 20 |
| 2 | FirstWin | 1592 | 첫 사냥감 | 1605 | 영웅을 처음으로 처치한다 | FirstWin | 1 | 30 |
| 3 | Win120 | 1593 | 신속한 처형 | 1606 | 2분 안에 영웅을 처치한다 | WinUnderSeconds | 120 | 40 |
| 4 | Win90 | 1594 | 전광석화 | 1607 | 90초 안에 영웅을 처치한다 | WinUnderSeconds | 90 | 60 |
| 5 | Win60 | 1595 | 찰나의 죽음 | 1608 | 60초 안에 영웅을 처치한다 | WinUnderSeconds | 60 | 100 |
| 6 | Wins5 | 1596 | 다섯 영혼 | 1609 | 누적 5승을 달성한다 | TotalWins | 5 | 40 |
| 7 | Wins10 | 1597 | 열 개의 무덤 | 1610 | 누적 10승을 달성한다 | TotalWins | 10 | 60 |
| 8 | Wins25 | 1598 | 영웅 학살자 | 1611 | 누적 25승을 달성한다 | TotalWins | 25 | 100 |
| 9 | Wins50 | 1599 | 던전의 전설 | 1612 | 누적 50승을 달성한다 | TotalWins | 50 | 150 |
| 10 | Runs10 | 1600 | 성실한 영주 | 1613 | 누적 10회 출격한다 | TotalRuns | 10 | 50 |
| 11 | Runs30 | 1601 | 끈질긴 영주 | 1614 | 누적 30회 출격한다 | TotalRuns | 30 | 100 |
| 12 | SynergyTier2 | 1602 | 빌드의 맛 | 1615 | 한 런에서 한 축 Tier2(5장)에 도달한다 | SynergyTierReached | 2 | 50 |
| 13 | SynergyTier3 | 1603 | 진성 빌드 | 1616 | 한 런에서 한 축 Tier3(7장)에 도달한다 | SynergyTierReached | 3 | 80 |

### 4.7 `HeroSkill.json` (15행) · `HeroSkill_Param.json` (97행) · `HeroArchetype_Skill.json` (15행)

`HeroSkill`:

| # | skillId | nameKey | 이름 |
|---|---|---|---|
| 1 | DashStrike | 1617 | 분쇄의 파동 |
| 2 | AoeNova | 1618 | 파멸의 노바 |
| 3 | OrbitingBlade | 1619 | 수호의 칼날 |
| 4 | PiercingShot | 1620 | 관통 사격 |
| 5 | ArrowVolley | 1621 | 화살 소나기 |
| 6 | Evade | 1622 | 회피 도약 |
| 7 | ShadowStrike | 1623 | 그림자 습격 |
| 8 | BladeWhirl | 1624 | 칼날 선풍 |
| 9 | SmokeBomb | 1625 | 연막 탄 |
| 10 | CrushingLeap | 1626 | 분쇄 도약 |
| 11 | ChainPull | 1627 | 사슬 갈고리 |
| 12 | BloodFrenzy | 1628 | 피의 광란 |
| 13 | JudgmentHammer | 1629 | 심판의 망치 |
| 14 | PurifyingLight | 1630 | 정화의 빛 |
| 15 | Sanctuary | 1631 | 성역 |

`HeroSkill_Param` (skillId 별 `paramKey = value`):

| skillId | paramKey = value |
|---|---|
| DashStrike | damage=80, cooldown=3, dashLength=3, coneHalfAngle=180, knockbackStrength=1, centroidRadius=5 |
| AoeNova | damage=100, cooldown=5, radius=3.5, knockbackStrength=3 |
| OrbitingBlade | damage=30, hitInterval=0.3, orbitRadius=1.4, bladeSphereRadius=0.9, bladeCount=3, rotationSpeedDeg=180 |
| PiercingShot | damage=70, cooldown=4, lineLength=9, lineHalfWidth=0.6, knockbackStrength=0.5, centroidRadius=8 |
| ArrowVolley | damagePerTick=40, tickCount=3, tickIntervalSec=0.5, radius=2.8, cooldown=8, targetSearchRadius=7 |
| Evade | damage=30, cooldown=10, leapDistance=4.5, leapDurationSec=0.25, blastRadius=2, knockbackStrength=2.5, triggerMonsterCount=3, triggerRadius=2 |
| ShadowStrike | damage=120, cooldown=6, blinkRange=9, minBlinkDistance=2.5, arriveOffset=1.2, invulnerableSec=0.3, killRefundFraction=0.5 |
| BladeWhirl | damagePerTick=35, tickCount=4, tickIntervalSec=0.3, radius=2.4, cooldown=9, damageTakenMul=0.7, triggerMonsterCount=2, triggerRadius=2.4 |
| SmokeBomb | radius=3, durationSec=3, monsterSpeedMul=0.6, monsterRangeMul=0.5, monsterMinRange=0.8, cooldown=12, triggerMonsterCount=4, triggerRadius=2.4 |
| CrushingLeap | damage=90, cooldown=7, leapRange=8, minLeapDistance=3, leapDurationSec=0.3, invulnerableSec=0.3, slamRadius=2.6, knockbackStrength=1.5, clusterRadius=2.2 |
| ChainPull | damage=40, cooldown=10, pullRadius=5, maxTargets=6, pullToDistance=1.5, pullDurationSec=0.4, triggerMonsterCount=4, triggerRadius=5 |
| BloodFrenzy | durationSec=6, attackCooldownMul=0.6, damageTakenMul=1.2, cooldown=18, triggerMonsterCount=3, triggerRadius=2.2 |
| JudgmentHammer | damage=110, cooldown=6, radius=2.4, stunSec=0.6, castRange=5 |
| PurifyingLight | triggerDebuffCount=2, healPercent=0.08, cooldown=12 |
| Sanctuary | durationSec=6, damageTakenMul=0.6, radius=2.6, monsterSpeedMul=0.7, cooldown=18, triggerMonsterCount=3, triggerRadius=2.6 |

`HeroArchetype_Skill`:

| heroId | phaseIndex 1 (hpFraction 0.85) | phaseIndex 2 (hpFraction 0.65) | phaseIndex 3 (hpFraction 0.45) |
|---|---|---|---|
| Knight | DashStrike | AoeNova | OrbitingBlade |
| Archer | PiercingShot | ArrowVolley | Evade |
| Assassin | ShadowStrike | BladeWhirl | SmokeBomb |
| Berserker | CrushingLeap | ChainPull | BloodFrenzy |
| Paladin | JudgmentHammer | PurifyingLight | Sanctuary |

검산: 영웅 5 x 구간 3 = 15행.

### 4.8 `Card.json` (28행)

| # | cardId | axis | effectKind | nameKey | 이름 | descKey | 설명 |
|---|---|---|---|---|---|---|---|
| 1 | WispHpBoost | Tank | WispHpBoost | 1632 | 끈질긴 도깨비불 | 1660 | 모든 도깨비불 HP +50% |
| 2 | WraithDamageBoost | Tank | WraithDamageBoost | 1633 | 망령의 압박 | 1661 | 모든 망령 HP +50% |
| 3 | ReaperAtkSpeed | Dps | ReaperAtkSpeed | 1634 | 신속한 사신 | 1662 | 모든 사신 공격속도 +30% |
| 4 | HexRangeBoost | Dps | HexRangeBoost | 1635 | 저주의 시야 | 1663 | 모든 저주술사 사거리 +40% |
| 5 | PlagueSlowBoost | Debuff | **PlagueToxinBoost** | 1636 | 역병의 손길 | 1664 | 역병귀 독 피해 강화 |
| 6 | PhantomMoveSpeedBoost | Swarm | PhantomMoveSpeedBoost | 1637 | 환령의 발걸음 | 1665 | 모든 환령 이동속도 +50% |
| 7 | SpawnWisps | Swarm | SpawnWisps | 1638 | 도깨비불 떼 | 1666 | 도깨비불 스포너 출력 +1 |
| 8 | SpawnWraith | Tank | SpawnWraith | 1639 | 더 많은 망령 | 1667 | 망령 스포너 출력 +1 |
| 9 | SpawnReapers | Dps | SpawnReapers | 1640 | 사신 떼거리 | 1668 | 사신 스포너 출력 +1 |
| 10 | SpawnPlagues | Debuff | SpawnPlagues | 1641 | 역병 증식 | 1669 | 역병귀 스포너 출력 +1 |
| 11 | SpawnPhantoms | Swarm | SpawnPhantoms | 1642 | 환령 떼 | 1670 | 환령 스포너 출력 +1 |
| 12 | ReplaceWispsToWraith | Tank | **WispWraithPowerBoost** | 1643 | 공포의 군세 | 1671 | 도깨비불·망령 데미지 +30% (영구) |
| 13 | ReplaceReapersToHex | Dps | **ReaperHexPowerBoost** | 1644 | 처형 명령 | 1672 | 사신·저주술사 데미지 +30% (영구) |
| 14 | HeroAttackDown | Debuff | HeroAttackDown | 1645 | 약화의 저주 | 1673 | 영웅 공격력 영구 -25% |
| 15 | Fear | Debuff | Fear | 1646 | 공포 | 1674 | 영웅 3초간 도망 |
| 16 | Bleed | Debuff | Bleed | 1647 | 출혈 | 1675 | HP 지속 감소 (10초) |
| 17 | Weaken | Debuff | Weaken | 1648 | 무력화 | 1676 | 영웅 데미지 -50% (10초) |
| 18 | Slow | Swarm | Slow | 1649 | 던전의 점성 | 1677 | 영웅 -50% 이동속도, 모든 몬스터 +30% 이동속도 (10초) |
| 19 | Frenzy | Dps | Frenzy | 1650 | 광폭화 | 1678 | 모든 몬스터 공속 +50% (10초) |
| 20 | Multiply | Swarm | **FastBreeding** | 1651 | 빠른 번식 | 1679 | 환령 스포너 주기 -40% (영구) |
| 21 | BloodThirst | Dps | BloodThirst | 1652 | 피의 갈증 | 1680 | 처치 시 주변 몬스터 회복 (30초) |
| 22 | IronWill | Tank | IronWill | 1653 | 강철 의지 | 1681 | 모든 몬스터 받는 데미지 -30% (15초) |
| 23 | TimeStop | Swarm | TimeStop | 1654 | 시간 정지 | 1682 | 영웅 5초 멈춤 |
| 24 | Berserk | Tank | **GuardianRage** | 1655 | 수호자의 분노 | 1683 | 도깨비불·망령 받는 데미지 -50% (15초) |
| 25 | WallOfWisps | Tank | **ToughHide** | 1656 | 단단한 살갗 | 1684 | 도깨비불·망령 받는 데미지 -25% (영구) |
| 26 | MarkOfDeath | Dps | MarkOfDeath | 1657 | 죽음의 표식 | 1685 | 다음 5초간 영웅이 받는 데미지 +50% |
| 27 | SpawnerHaste | Swarm | SpawnerHaste | 1658 | 던전의 박동 | 1686 | 모든 스포너 주기 -20% (영구) |
| 28 | LingeringCurse | Debuff | LingeringCurse | 1659 | 끈질긴 저주 | 1687 | 영웅 디버프 지속 +25% |

굵은 6칸 = `cardId` 와 `effectKind` 이름이 다른 카드. 축 검산: Tank 7 · Dps 7 · Debuff 7 · Swarm 7 = 28. `ECardId.HeroPoisonAura`(13)는 폐기된 자리라 행이 없다.

카드 풀 소속(변경 없음, `CardPool` SO 가 진실 — 순서 포함):
- 패시브 16: WispHpBoost, WraithDamageBoost, SpawnWraith, ReplaceWispsToWraith, ReaperAtkSpeed, HexRangeBoost, SpawnReapers, ReplaceReapersToHex, PlagueSlowBoost, SpawnPlagues, LingeringCurse, HeroAttackDown, PhantomMoveSpeedBoost, SpawnPhantoms, SpawnWisps, SpawnerHaste
- 액티브 12: IronWill, WallOfWisps, Berserk, Frenzy, BloodThirst, MarkOfDeath, Fear, Bleed, Weaken, TimeStop, Multiply, Slow

### 4.9 `Card_Param.json` (26행)

| cardId | paramKey = value |
|---|---|
| WispHpBoost | hpMul=1.5 |
| WraithDamageBoost | hpMul=1.5 |
| ReaperAtkSpeed | cdMul=0.7 |
| HexRangeBoost | rangeMul=1.4 |
| PlagueSlowBoost | toxinMul=1.4 |
| PhantomMoveSpeedBoost | speedMul=1.5 |
| ReplaceWispsToWraith | powerMul=1.3 |
| ReplaceReapersToHex | powerMul=1.3 |
| Fear | duration=3 |
| Bleed | ratio=0.02, duration=10 |
| Weaken | factor=0.5, duration=10 |
| Slow | heroFactor=0.5, monsterMul=1.3, duration=10 |
| Frenzy | duration=10 |
| Multiply | periodMul=0.6 |
| BloodThirst | duration=30 |
| IronWill | duration=15 |
| TimeStop | duration=5 |
| Berserk | duration=15 |
| MarkOfDeath | dmgTakenMul=1.5, duration=5 |
| SpawnerHaste | periodMul=0.8 |
| LingeringCurse | durationBonusPerPick=0.25 |

검산: 1행짜리 17장 + 2행짜리 3장(Bleed·Weaken·MarkOfDeath) + 3행짜리 1장(Slow) = 17 + 6 + 3 = 26행, 카드 21장. 파라미터 없는 카드 7장(HeroAttackDown, SpawnWisps, SpawnWraith, SpawnReapers, SpawnPlagues, SpawnPhantoms, WallOfWisps) → 21 + 7 = 28.

`Slow.monsterMul=1.3` 은 현재 코드가 읽지 않는 값이다(몬스터 가속은 `EMonsterBuff.SwarmSpeed` 쪽 상수가 담당). 패리티를 위해 그대로 옮기고 정리는 이 기획 범위 밖이다.

### 4.10 (C1) `Stage.json` 추가 값 (25행)

| chapterId | stageIndex 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| 1 | heroScale 1 / heroTier 0 | 1 / 1 | 1 / 2 | 1 / 3 | 1.4 / 4 |
| 2 | 1 / 0 | 1 / 1 | 1 / 2 | 1 / 3 | 1.25 / 4 |
| 3 | 1 / 0 | 1 / 1 | 1 / 2 | 1 / 3 | 1.2 / 4 |
| 4 | 1 / 0 | 1 / 1 | 1 / 2 | 1 / 3 | 1.35 / 4 |
| 5 | 1 / 0 | 1 / 1 | 1 / 2 | 1 / 3 | 1.3 / 4 |

(각 칸 = `heroScale` / `heroTier`.)

---

## §5 `Strings_Ko` 신규 id

### 5.1 대역

현재 최대 id 는 1540 이다. 기존 관례(기능마다 직전 최대 id 다음부터 연속 블록)를 따라 **1541~1687 (147개)** 를 연속 배정한다.

| 블록 | 범위 | 개수 | 내용 | 추가 단계 |
|---|---|---|---|---|
| 상점 항목 이름 | 1541~1571 | 31 | §4.4 | A2 |
| 상점 항목 설명 | 1572~1585 | 14 | §4.4 | A2 |
| 영주 보상 이름 | 1586~1590 | 5 | §4.5 | A2 |
| 도전과제 이름 | 1591~1603 | 13 | §4.6 | A2 |
| 도전과제 설명 | 1604~1616 | 13 | §4.6 | A2 |
| 영웅 스킬 이름 | 1617~1631 | 15 | §4.7 | B1 |
| 카드 이름 | 1632~1659 | 28 | §4.8 | B2 |
| 카드 설명 | 1660~1687 | 28 | §4.8 | B2 |

검산: 31 + 14 + 5 + 13 + 13 + 15 + 28 + 28 = 147 = 1687 − 1541 + 1. `Strings_Ko` 행 수 329 → 476. A2 에서 76개, B1 에서 15개, B2 에서 56개.

### 5.2 번호 충돌 규칙

구현 시점에 `Strings_Ko` 최대 id 가 1540 이 아니면(다른 기능이 먼저 1541 이후를 썼으면) **아직 추가하지 않은 블록 전체를 그 시점 최대 id + 1 부터 같은 순서로 평행 이동**하고, 이 md §4·§5 의 id 와 JSON 의 `nameKey`/`descKey` 를 함께 고친다(`infinite-tower.md` 의 재번호 선례). 블록 안 순서는 바꾸지 않는다.

### 5.3 문구 규칙

- 문구는 §4 표의 글자 그대로다(공백·가운뎃점 `·`·퍼센트·괄호 포함). 패리티 테스트 SJ1 이 SO 문자열과 `Strings_Ko[nameKey]` 를 문자 단위로 비교한다.
- 같은 문구라도 기존 id 를 재사용하지 않는다(예: 카드 이름 "공포"는 새 id 1646). 예외는 §4.4 의 체급 설명 6행이 1579 하나를 공유하는 것뿐이다.

---

## §6 이름·로더·실패 정책

### 6.1 이름 정리

| 대상 | JSON 파일 = `EData` 값 | row 클래스 | 런타임 조회 지점 |
|---|---|---|---|
| A1 | `BalanceConfig`, `MonsterStat` | `BalanceConfigRow`, `MonsterStatRow` | `BalanceConfig.Current` |
| A2 | `MetaConfig`, `ShopItem`, `ShopItem_Stat`, `ShopItem_Species`, `ShopItem_Mul`, `ShopItem_Desc`, `LordLevel`, `LordLevel_Reward`, `Achievement` | 각 `<파일명>Row` | `MetaConfig.Current` |
| B1 | `HeroSkill`, `HeroSkill_Param`, `HeroArchetype_Skill` | 각 `<파일명>Row` | `HeroSkillDatabase.Current` |
| B2 | `Card`, `Card_Param` | `CardRow`, `Card_ParamRow` | `CardDatabase.Current` |

- 기존 클래스 `BalanceConfig`·`MetaConfig`(+ `ShopItemDef`·`LordRewardDef`·`AchievementDef`·`BalanceConfig.CharacterStat`·`BalanceConfig.HeroAnchor`)는 **이름과 공개 멤버를 유지한 채 ScriptableObject 상속만 떼어 일반 클래스**로 바꾼다. JSON row 로부터 조립되는 런타임 모델이 된다. 근거: 이 타입을 인자로 받는 호출부가 `BalanceConfig` 22파일·`MetaConfig` 61파일(테스트 포함)이라, 타입 이름을 바꾸면 변경 범위가 로직과 무관하게 커진다. 대안 비교는 §13 D1.
- 기존 중첩 클래스 `BalanceConfig.MonsterStatRow` 는 삭제하고, 새 최상위 `MonsterStatRow`(JSON row)가 그 이름을 쓴다.
- `MetaConfig.json`(공식 상수 7개)과 런타임 `MetaConfig`(상수 + 상점 + 영주 + 도전과제 묶음)는 이름이 같다. JSON row 는 `MetaConfigRow` 로 구분된다(`TowerConfig.json` ↔ `TowerConfigRow` 선례).

### 6.2 로드 위치와 순서

로더는 기존 `ChapterDatabaseLoader.LoadAsync` 에 얹는다(신규 로더 클래스 없음). 순서:

| 순서 | 묶음 | 결과 | 순서의 이유 |
|---|---|---|---|
| 1 | `BalanceConfig`·`MonsterStat` | `BalanceConfig.Current` | 3번 검증이 `passiveThresholds` 를 쓴다 |
| 2 | `HeroSkill`·`HeroSkill_Param`·`HeroArchetype_Skill` 파싱 | (행만 보관) | 3번 검증이 스킬 해금 HP 비율 집합을 쓴다 |
| 3 | 챕터 12종 (기존) | `ChapterDatabase.Current` | - |
| 4 | 2번 행 검증 (`heroId` FK) | `HeroSkillDatabase.Current` | `HeroArchetype` 이 3번에서 확정 |
| 5 | 기존 선택 묶음 — Difficulty → Relic → Formation → GradeFx → Codex → Showcase → Tower → Daily → Growth | 기존과 동일 | 순서 변경 없음 |
| 6 | `Card`·`Card_Param` | `CardDatabase.Current` | - |
| 7 | Meta 9종 | `MetaConfig.Current` | `currency` FK 와 육성 상점 무결성(`ValidateShopItems`)이 Growth 를 쓴다 |

- 7번 직후 로더가 육성 상점 무결성 검사를 1회 실행한다. 현재 마을·전투 진입점이 각자 부르던 호출은 없앤다(위반 시 동작은 현행과 같다: 육성 재화 기능만 비활성).
- 로딩 씬의 몬스터 쇼케이스 준비는 `LoadAsync` 가 끝난 뒤 시작하므로 `MetaConfig.Current` 를 읽을 수 있다(현행 4.5 → 4.6 순서 유지).

### 6.3 이중 진실 제거 (이전과 함께 반드시 처리)

`ChapterDatabase` 에 이전 대상 값의 하드코딩 사본이 2개 있다. JSON 이 단일 진실이 되면 사본이 조용히 어긋나므로 같은 단계에서 없앤다.

| 하드코딩 사본 | 대체 | 단계 |
|---|---|---|
| `PassiveFractions` {0.9 … 0.1} | `BalanceConfig.json` `passiveThresholds` | A1 |
| `SkillGateFractions` {0.85, 0.65, 0.45} | `HeroArchetype_Skill.json` 의 `hpFraction` 중복 제거 집합 | B1 |

값이 같으므로 검증 결과는 달라지지 않는다.

### 6.4 로드 실패 정책

신설 4묶음(Balance·HeroSkill·Card·Meta)은 **필수 묶음**이다. 하나라도 로드·검증에 실패하면 로더는 `LogError` 로 위반 전부를 출력하고 **필수 묶음의 `Current` 4개(`BalanceConfig.Current`·`HeroSkillDatabase.Current`·`CardDatabase.Current`·`MetaConfig.Current`)를 전부 null 로 되돌린 뒤** `LoadAsync` 가 null 을 돌려준다.

전투 진입 차단은 **`BattleController` 진입 시점의 명시 가드 한 곳**이 맡는다: 필수 묶음의 `Current` 가 하나라도 null 이면 `LogError` 후 마을로 복귀한다(`AbortToVillage`). 로더가 4개를 함께 null 로 되돌리므로 가드는 한 곳에서 판정한다. 단계별로는 그 단계까지 도입된 묶음만 판정한다(A1 은 Balance).

| 상황 | 동작 |
|---|---|
| 전투 진입 (필수 묶음 미로드) | 명시 가드가 진입 중단, 마을 복귀 |
| 마을 | 기존 null 가드대로 동작 (메뉴 열기 불가 + 오류 로그) |
| 다음 전투 진입 | `EnsureLoadedAsync` 가 다시 로드를 시도 (챕터와 필수 묶음의 `Current` 가 모두 있을 때만 즉시 반환) |

기존 "전투 사양 생성 실패" 경로에 기대지 않는 이유: 챕터 모드의 전투 사양은 챕터 데이터가 없어도 항상 만들어진다(`BattleSpecResolver.ResolveChapter`). 사양 생성 실패로 마을에 돌아가는 것은 탑·데일리뿐이라, 그 경로만으로는 챕터 모드 진입이 막히지 않는다. 따라서 판정 기준은 `ChapterDatabase.Current` 나 전투 사양이 아니라 필수 묶음의 `Current` 다.

**범위 밖 (변경 없음)**: 챕터 JSON 만 단독으로 실패했을 때의 기존 동작(챕터 모드는 영웅 기본 스탯으로 진행, 탑·데일리는 사양 생성 실패로 마을 복귀)은 이번에 바꾸지 않는다.

근거: SO 는 씬에 직렬화돼 있어 "로드 실패"가 없었다. JSON 전환으로 생긴 새 실패 경로에서 기본 스탯·무보상으로 전투를 진행시키면 틀린 수치의 판이 랭킹에 올라간다. 이 실패는 배포 누락(Addressables 미등록·JSON 문법 오류)에서만 나며 SJ3~SJ6 이 커밋 전에 잡는다. 사용자 확인 항목 §14 Q7.

기존 가드 2개는 이 명시 가드로 대체된다: `_balance == null` 시 "프리팹 기본 스탯으로 진행", 스킬 로드아웃 로드 실패 시 "영웅 스킬 비활성".

---

## §7 소비처 전환

### 7.1 A1 `BalanceConfig`

| 소비처 | 현재 | 전환 후 |
|---|---|---|
| `BattleController` | `[SerializeField] BalanceConfig _balance` (Battle.unity 7742행) | `BalanceConfig.Current` 조회. 직렬화 필드 삭제. 진입 시 필수 묶음 미로드면 마을 복귀하는 명시 가드 추가(§6.4) |
| `VillageController` | `[SerializeField] BalanceConfig _balance` (Village.unity 11067행) — 영웅 앵커만 읽음 | `BalanceConfig.Current` 조회. 직렬화 필드 삭제 |
| `BattleHudArg.Balance` | 대입만 있고 읽는 곳 0 | 필드 삭제 |
| `BattleController.Balance` 프로퍼티 | 공개 getter | `BalanceConfig.Current` 로 대체 후 삭제 |
| `ChapterDatabase.PassiveFractions` | 하드코딩 | §6.3 |
| 주석만 참조 (`Spawner`·`TowerViewModel`·`CommonEnum`·`CommonInterface`·`MetaConfig`) | - | 문구만 "BalanceConfig.json / MonsterStat.json" 으로 갱신 |
| EditMode 테스트 8파일 · PlayMode 테스트 2파일 | 실에셋 로드(`LoadAssetAtPath<BalanceConfig>`) · `CreateInstance` + `SerializedObject` 로 값 주입 | 실데이터 검증 테스트 → shipped JSON 을 읽어 조립(§7.5). 값 주입 테스트 → row 리터럴로 조립. EditMode 8파일 중 `BalanceConfigSyncerTests` 는 전환하지 않고 삭제(§8.1) |

### 7.2 A2 `MetaConfig`

| 소비처 | 현재 | 전환 후 |
|---|---|---|
| `BattleController` | `[SerializeField] MetaConfig _metaConfig` (Battle.unity 7743행) | `MetaConfig.Current`. 직렬화 필드 삭제 |
| `VillageController` | `[SerializeField] MetaConfig _metaConfig` (Village.unity 11066행) | `MetaConfig.Current`. 직렬화 필드 삭제 |
| `MonsterShowcaseStage` | `[SerializeField] MetaConfig _metaConfig` (Village.unity 5588행, Loading.unity 9978행) | `MetaConfig.Current`. 직렬화 필드 삭제 |
| `MetaConfig` 를 인자로 받는 순수 로직 16파일 (`ShopService`, `LordLevelService`, `AchievementService`, `SoulRewardCalculator`, `MetaBattleBonus`, `DungeonPowerSummary`, `SaveSummary`, `MonsterLooks`, `GrowthDatabase.ValidateShopItems`, `ShopPopup`, `QuestPopup`, `LordLevelPopup`, `CodexPopup`, `ResultPopup`, `VillageViewModel`, `CommonEnum` 주석) | 인자 타입 `MetaConfig` | **시그니처·본문 변경 없음** (타입 이름과 멤버가 그대로) |
| `ChapterDatabaseLoader` | 육성 상점 무결성 검사를 진입점이 넘긴 SO 로 실행 | Meta 묶음 로드(§6.2 7번)와 무결성 검사 1회 실행을 맡는다 |
| 표시 문구 (`def.DisplayName`·`def.Description`) | SO 인라인 문자열 | 조립 시 `Strings_Ko[nameKey]`·`Strings_Ko[descKey]` 로 채운 같은 문자열. 설명 행이 없는 항목은 빈 문자열(현행과 동일) |
| `ShopItemDef.StatKind`·`Species`·`PerLevelMul` | 미사용 항목은 0 / 0 / 1.0 | 관계 행이 없으면 기본값 `Hp` / `Wisp` / 1.0 (현행 직렬화 값과 동일) |
| `LordRewardDef` 목록 | SO 리스트 9개 | `LordLevel` 9행을 level 순으로 조립. `LordLevel_Reward` 행이 있으면 `IsLockedDummy=false`, 없으면 `true`·`RewardSouls=0` |
| 육성 상점 무결성 호출 2곳 (마을·전투) | 진입점이 호출 | 로더가 1회 호출(§6.2) |
| EditMode 테스트 38파일 · PlayMode 테스트 1파일 | `ScriptableObject.CreateInstance<MetaConfig>()` 후 공개 필드 대입, 실에셋 로드 1종(`MetaConfigAssetRegressionTests`) | `new MetaConfig()` 로 교체(필드 대입 코드는 그대로), `DestroyImmediate` 호출 삭제. 실에셋 회귀 테스트 → shipped JSON 조립으로 교체(§7.5) |

### 7.3 B1 영웅 스킬

| 소비처 | 현재 | 전환 후 |
|---|---|---|
| `BattleController.SpawnHero` | 영웅별 로드아웃 SO 를 `EData.HeroSkillLoadout*` 키로 로드 | `HeroSkillDatabase.Current` 에서 `heroId` 로 구간 목록 조회 |
| `BattleController.LoadoutKeys` (영웅 → `EData` 표) | 코드 표 | 삭제 (`HeroArchetype_Skill.heroId` 가 대체) |
| `HeroSkillRunner.Bind` / `OnSkillUnlocked` | 로드아웃 SO·스킬 SO 를 받음 | 같은 정보(구간별 HP 비율·스킬·표시 이름)를 JSON 조립 결과로 받음. 해금 순서·동시 조건 우선순위(`CastPriority`, 코드 상수)·컷인 표시 이름은 현행과 동일 |
| 스킬 15종의 수치 프로퍼티 | `[SerializeField]` | `HeroSkill_Param` 값. 프로퍼티 이름과 런타임 동작은 그대로 |
| `ChapterDatabase.SkillGateFractions` | 하드코딩 | §6.3 |
| 스킬·로드아웃 SO 를 직접 생성·로드하는 테스트 (PlayMode `HeroSkillRunnerPlayTests`·`HeroSkillRunnerPhasePlayTests`, EditMode `AoeNovaSkillTests`·`DashStrikeSkillTests`·`OrbitingBlade*Tests`·`ArcherSkill*Tests`·`AssassinSkillRuntimeTests`·`AssassinTests`·`AssassinWorldIntegrationTests`·`ArcherDataConsistencyTests`·`HeroSkillRunnerUnlockEventTests`·`Chapter3ArtIntegrityTests` — 대상 전수는 `SkillData`·`HeroSkillLoadout` 검색 결과 전부) | `CreateInstance<…SkillData>()` · 실에셋 로드 · `hero_skills.json` 읽기 | 파라미터 사전으로 스킬 생성. 실데이터 검증은 shipped JSON 조립. `hero_skills.json` 을 읽던 테스트는 새 JSON 3종을 읽는다 |

### 7.4 B2 카드

| 소비처 | 현재 | 전환 후 |
|---|---|---|
| `CardData.Id`·`Icon`·`CardImage` | SO 직렬화 | 변경 없음 |
| `CardData.Axis`·`DisplayName`·`Description`·`Effect` | SO 직렬화 | **공개 조회 API 는 유지**, 값 출처만 `CardDatabase.Current` 의 `cardId` 행. 호출부(54파일)는 수정하지 않는다 |
| 효과 인스턴스 | SO 안 `[SerializeReference]` 객체 | `effectKind` 로 효과를 만들고 `Card_Param` 값을 주입. 효과 클래스의 `Apply` 동작은 그대로 |
| `BattleController`·`VillageController` 의 풀 로드 | `CardPool` SO 로드 | 변경 없음 (풀 SO 유지) |
| `Card_Species` 검증 | `ECardId` 값명 대조 | `Card.json` 행 존재 대조 추가(FK 가 실재 표를 가리키게 됨) |
| 테스트 헬퍼 `FakeCardData` | 리플렉션으로 `_id`·`_axis`·`_displayName`·`_effect` 주입 | 헬퍼 한 곳만 새 주입 방식으로 고친다(헬퍼를 쓰는 테스트 본문은 그대로) |
| `cards.json`·`card_pools.json` 을 읽던 테스트 (`CardJsonTests`·`CardRenewalConsistencyTests`·`GrowthCardIntegrityTests`) | 동기 사본 읽기 | `Card.json`·`Card_Param.json`·`CardPool` SO 를 읽는다 |

### 7.5 테스트의 실데이터 접근 방식

기존 선례 `WorldMapTestSupport`(테스트가 `Assets/_Lair/Data/Json/<이름>.json` 을 파일로 읽어 row 배열로 조립)를 그대로 따른다. 새 헬퍼 4개(Balance·Meta·HeroSkill·Card 의 shipped 조립)를 같은 방식으로 둔다. 테스트는 Addressables 를 거치지 않는다.

### 7.6 (C1) `HeroStageVariantConfig`

| 소비처 | 현재 | 전환 후 |
|---|---|---|
| `HeroStageVariantApplier` | `variant.Tier`·`variant.ScaleMultiplier`·발광 3필드 | `Stage.heroTier`·`Stage.heroScale`. 발광 분기는 "꺼짐" 경로만 남긴다(현행 25행 전부 꺼짐과 동일) |
| `VillageController`·`HeroSelectPopup`·`RecordsPopup` 의 초상 조회 | `GetStage(...).Portrait` | 변경 없음 |
| 범위 밖 스테이지 번호(탑·데일리) | 가장 가까운 엔트리로 클램프 | 같은 클램프 규칙으로 고른 스테이지의 `heroScale`·`heroTier` 를 쓴다 |

---

## §8 제거 대상

### 8.1 단계별 목록

| 단계 | 제거 | 경로 |
|---|---|---|
| A1 | SO 에셋 | `Assets/_Lair/Data/BalanceConfig.asset` (+ `.meta`) |
| A1 | syncer·DTO·동기 사본 | `Editor/JsonSync/BalanceConfigSyncer.cs`, `Scripts/Data/Dto/BalanceConfigDto.cs`, `Data/Json/balance_config.json`, JSON Sync 창의 Balance Config 구역 |
| A1 | 테스트 | `Tests/EditMode/JsonSync/BalanceConfigSyncerTests.cs` |
| A1 | 씬 직렬화 잔여 줄 | Battle.unity `_balance`, Village.unity `_balance` |
| A2 | SO 에셋 | `Assets/_Lair/Data/MetaConfig.asset` (+ `.meta`) |
| A2 | 에디터 창 | `Editor/MetaTool/LairMetaEditorWindow.cs`, `Editor/MetaTool/MetaEditorCalc.cs`, `Tests/EditMode/Meta/MetaEditorCalcTests.cs` |
| A2 | 씬 직렬화 잔여 줄 | Battle.unity·Village.unity(2곳)·Loading.unity 의 `_metaConfig` |
| B1 | SO 에셋 20 | `Art/Skills/HeroSkill_*.asset` 15, `Art/Skills/HeroSkillLoadout*.asset` 5 (+ `.meta`, Addressables 엔트리) |
| B1 | SO 클래스 | `HeroSkillLoadout` 클래스, `HeroSkillData` 와 서브클래스 15의 ScriptableObject 상속·`CreateAssetMenu` |
| B1 | syncer·동기 사본 | `Editor/JsonSync/HeroSkillSyncer.cs`, `HeroSkillDataConverter.cs`, `Dto/HeroSkillsDto.cs`, `Data/Json/hero_skills.json`, JSON Sync 창의 Hero Skills 구역 |
| B1 | `EData` 값 5개 | `HeroSkillLoadout`, `HeroSkillLoadout_Archer`, `HeroSkillLoadout_Assassin`, `HeroSkillLoadout_Berserker`, `HeroSkillLoadout_Paladin` — **값은 지우지 않고 "폐기" 주석으로 자리만 보존**(`FormationMonster` 선례, 정수 순서 유지) |
| B2 | SO 필드 | `CardData` 의 `_axis`·`_displayName`·`_description`·`_effect` (에셋 28개 재직렬화) |
| B2 | JSON Sync 전체 | `Editor/JsonSync/` 폴더 전체(`CardDataSyncer`, `CardPoolSyncer`, `EffectConverter`, `UnitySerializeFieldContractResolver`, `JsonSyncSettings`, `LairJsonSyncWindow`, `Dto/CardDataDto`, `Dto/CardPoolDto`, asmdef `Lair.Editor.JsonSync`), `Data/Json/cards.json`, `Data/Json/card_pools.json` |
| B2 | 테스트 | `Tests/EditMode/JsonSync/CardDataSyncerTests.cs`, `EffectConverterTests.cs` (폴더 삭제) |
| B2 | 카드 에디터 축소 | `Editor/CardTool/LairCardEditorWindow.cs` 의 편집 필드를 `_icon`·`_cardImage` 로 줄인다. 유지 기능: 목록, 생성하기, Enum 추가(`CardEnumCodegen`), 풀 소속, 삭제 |
| C1 | SO 필드 | `HeroStageVariant` 의 `UseEmission`·`EmissionColor`·`EmissionIntensity`·`ScaleMultiplier`·`Tier` (에셋 1개 재직렬화) |

`Newtonsoft` 패키지 의존성은 유지한다(`CardJsonTests` 등 남는 테스트가 `Newtonsoft.Json.Linq` 를 쓴다).

### 8.2 문서·메타 파일 갱신

| 파일 | 변경 | 단계 |
|---|---|---|
| `.claude/project.md` `balance_config_asset` | 키 이름 유지(에이전트 정의가 이 키를 호명), 값을 `Assets/_Lair/Data/Json/BalanceConfig.json` 으로. 같은 절에 `monster_stat_json: Assets/_Lair/Data/Json/MonsterStat.json`, `json_data_folder: Assets/_Lair/Data/Json/` 추가 | A1 |
| `.claude/project.md` 밸런스 조정 흐름 5단계 | "SO/수치 수정" → "JSON 수치 수정" | A1 |
| `.claude/project.md` `card_data_folder` | 설명에 "SO 는 id + 그림, 수치·문구는 Card.json" 추가 | B2 |
| `CLAUDE.md` §4 폴더 구조의 `Data/ … BalanceConfig.asset` 표기 | `Data/Json/` 표기로 수정 — 사용자 승인 후 메인이 반영 | A1 |
| `docs/design/json-data-sync.md` | 문서 첫 줄에 "폐기 — so-to-json-migration 으로 대체" 표기 | B2 |
| `docs/design/card-editor-tool.md` §3.4 | 편집 필드 축소 반영 | B2 |
| `.mockups/data-architecture.html` | JSON 73개·SO 목록 갱신 | B2 이후 |

### 8.3 JSON 파일 수 검산

현재 `Data/Json/` 61개(동기 사본 4개 포함) − 동기 사본 4 + 신설 16 = **73개**.

---

## §9 엣지 케이스

| # | 상황 | 처리 |
|---|---|---|
| E1 | 에디터에서 Battle 씬 직행 | 현행처럼 `EnsureLoadedAsync` 가 전체 묶음을 로드한다. 신설 묶음도 같은 호출에 포함 |
| E2 | 에디터에서 Village 씬 직행 (로딩 씬 미경유) | `MetaConfig.Current`·`ChapterDatabase.Current` 가 null — 기존 null 가드대로 메뉴 열기 불가. 현행도 로딩 씬이 리소스·UI 초기화를 맡으므로 동작 범위는 같다 |
| E3 | `Strings_Ko` 가 아직 등록되지 않은 상태에서 조립 | 문구 존재 검증은 통과시키고(현행 `NameKeyExists` 규칙), 표시 문자열은 빈 문자열. 로딩 씬 경로는 `Strings_Ko` 등록이 먼저라 해당 없음 |
| E4 | JSON 문법 오류·필수 필드 누락 | §6.4 필수 묶음 실패 — 위반 전부를 한 번에 로그, 필수 묶음 `Current` 전부 null, 전투 진입은 명시 가드가 차단 |
| E5 | `Card.json` 에 행이 있는데 `CardData` SO 가 풀에 없음 / 풀의 SO 에 맞는 행이 없음 | EditMode 테스트 SJ5 가 양방향 대조로 잡는다. 런타임에서 행 없는 SO 를 만나면 `LogError` + 그 카드를 후보에서 제외 |
| E6 | 세이브에 있는 `ShopLevels[].ItemId` 가 `ShopItem.json` 에 없음 | 현행과 동일 — 조회 결과 없음으로 무시(레벨 보존) |
| E7 | 세이브의 `AchievedIds` 에 있는 id 가 `Achievement.json` 에 없음 | 현행과 동일 — 무시 |
| E8 | 폐기 카드 `HeroPoisonAura` 가 세이브 기록에 남아 있음 | 현행과 동일 — `Card.json` 에 행이 없어도 Enum 자리와 세이브 문자열은 유지 |
| E9 | 밸런스 수치를 고치고 싶을 때 (이전 후 워크플로) | JSON 을 텍스트 편집기로 고친다. 인스펙터·Meta Editor·JSON Sync 창은 없다. 오타는 로드 검증과 SJ3~SJ5 테스트가 잡는다 |
| E10 | 새 카드 추가 (이전 후 워크플로) | ① 카드 에디터 [Enum 추가] ② `Strings_Ko` 에 이름·설명 ③ `Card.json`·`Card_Param.json` 행 ④ 카드 에디터 [생성하기]로 SO(그림 2장 배정) ⑤ 풀 소속 지정. 새 효과 종류면 `ECardEffect` 값과 효과 클래스도 추가 |
| E11 | 씬 파일에 남는 직렬화 잔여 줄 | 직렬화 필드를 지운 뒤 씬 3개를 다시 저장해 잔여 줄을 없앤다(SJ7 이 GUID 검색으로 확인) |
| E12 | 데일리 카드 시드 | 풀 SO 와 그 순서가 불변이라 같은 날 같은 카드가 제시된다. `DailyGoldenAndRulesTests` 통과로 확인 |
| E13 | float 값 표기 | JSON 에는 §4 표의 십진 표기를 그대로 쓴다(YAML 과 같은 십진 문자열 → 같은 float). 패리티 비교는 오차 0 |
| E14 | 단계 사이에 다른 기능이 `Strings_Ko` 1541 이후를 먼저 씀 | §5.2 평행 이동 |

---

## §10 검증 게이트

| 게이트 | 내용 | 통과 기준 | 시점 |
|---|---|---|---|
| **SJ1 패리티** | 일회용 EditMode 테스트 — SO 에셋(이전 전 값)과 새 JSON 조립 결과를 **전 필드** 비교. 문구는 SO 문자열 vs `Strings_Ko[key]` 문자 단위 비교. 세이브 키 집합(`itemId` 31개, `achievementId` 13개) 일치 포함 | 불일치 0건 | 각 단계의 첫 커밋(SO 가 아직 있을 때). SO 삭제 커밋에서 이 테스트도 삭제 |
| **SJ2 회귀** | 기존 EditMode·PlayMode 전체 (`Lair/Tests/Run EditMode Tests`, `Lair/Tests/Run PlayMode Tests`) | 실패 0건 | 각 단계의 마지막 커밋 |
| **SJ3 스키마** | 신설 JSON 이 row 클래스로 파싱되고 필수 필드가 모두 있다. 값 범위: `hp`·`heroHp`·`heroPower` ≥ 1, `range`·`cooldown`·`moveSpeed`·`spawnPeriod`·`runDuration` > 0, `passiveThresholds` 는 0 초과 1 미만 내림차순, `activeThresholds` 는 0 초과 `runDuration` 미만 오름차순, `maxLevel` ≥ 0, `basePrice` ≥ 1, `priceGrowth` ≥ 1, `perLevelMul` > 0, `rewardSouls` ≥ 0, `threshold` > 0, `hpFraction` 0 초과 1 미만이며 `phaseIndex` 순 내림차순 | 위반 0건 | 조립 시 검증 + shipped 데이터 EditMode 테스트 |
| **SJ4 PK·FK** | PK·복합 PK 중복 0. FK 대상 존재: Enum 값명(`EMonster` 기준 6종, `EShopEffectKind`, `EMonsterStatKind`, `ECurrency`, `EAchievementCondition`, `EHeroSkill`, `EBuildAxis`, `ECardId`, `ECardEffect`), 표(`ShopItem`, `LordLevel`, `HeroSkill`, `HeroArchetype`, `Card`, `Currency`), `Strings_Ko` id. §3.6 의 `effectKind` 별 관계 행 규칙 | 위반 0건 | 위와 같음 |
| **SJ5 커버리지** | `MonsterStat` = 기준 6종 전부. `HeroSkill` = `EHeroSkill` 전 값. `HeroSkill_Param`·`Card_Param` = §3.9·§3.10 필수 키 집합과 정확히 일치(누락·초과 0). `HeroArchetype_Skill` = `HeroArchetype` 5행 전부가 1행 이상, `phaseIndex` 1부터 연속. `Card` 28행 ↔ 풀 SO 의 카드 28장 양방향 일치. `Strings_Ko` 1541~1687 147개 존재 | 위반 0건 | 위와 같음 |
| **SJ6 에셋 키** | 신설 16개 파일명 = `EData` 값명, Addressables(라벨 `Resource`) 등록, 기존 주소와 중복 0 | 위반 0건 | EditMode 테스트 |
| **SJ7 잔여물** | 삭제한 SO 에셋 GUID(`f37d9d9829bfbdf4198bbf1e4215d0ca` BalanceConfig, `c667348905314e24f96b1b3e1f099b96` MetaConfig, 스킬·로드아웃 20개)가 씬·프리팹에 0건. 삭제한 동기 사본 4개·syncer 를 참조하는 코드 0건 | 검색 결과 0건 | 각 단계의 마지막 커밋 |
| **SJ8 결정성** | `DailyGoldenAndRulesTests` (카드 풀 순서·시드) | 통과 | B2 |

qa-simulator 는 돌리지 않는다. 수치 변경이 0 이고, 확률적 시뮬 결과 비교보다 SJ1 의 전 필드 일치가 더 강한 증명이다.

---

## §11 구현 단계

각 단계는 "① 데이터·로더·패리티 → ② 전환·제거" 두 커밋으로 나뉘고, 단계 사이에 게임은 항상 실행 가능하다.

| 단계 | 커밋 ① (SO 유지) | 커밋 ② (SO 제거) | 완료 조건 |
|---|---|---|---|
| **A1** | `BalanceConfig.json`·`MonsterStat.json` + row + 로드(§6.2 1번) + SJ1 패리티 테스트 + SJ3~SJ6 테스트 | 소비처 전환(§7.1), `PassiveFractions` 대체, §8.1 A1 제거, `project.md` 갱신, 패리티 테스트 삭제 | SJ1(①) · SJ2~SJ7(②) |
| **A2** | Meta JSON 9개 + `Strings_Ko` 1541~1616 + row + 로드(7번) + 패리티 + 검증 테스트 | 소비처 전환(§7.2), §8.1 A2 제거, 패리티 테스트 삭제 | SJ1(①) · SJ2~SJ7(②) |
| **B1** | 스킬 JSON 3개 + `Strings_Ko` 1617~1631 + `EHeroSkill` + row + 로드(2·4번) + 패리티 + 검증 테스트 | 소비처 전환(§7.3), `SkillGateFractions` 대체, §8.1 B1 제거, 패리티 테스트 삭제 | SJ1(①) · SJ2~SJ7(②) |
| **B2** | `Card.json`·`Card_Param.json` + `Strings_Ko` 1632~1687 + `ECardEffect` + row + 로드(6번) + 패리티 + 검증 테스트 | 소비처 전환(§7.4), §8.1 B2 제거, 문서 갱신(§8.2), 패리티 테스트 삭제 | SJ1(①) · SJ2~SJ8(②) |
| **C1** (Q4 승인 시) | `Stage.json` 필드 2개 + `StageRow` 필드 + 패리티 테스트 | 소비처 전환(§7.6), §8.1 C1 제거, 패리티 테스트 삭제 | SJ1(①) · SJ2~SJ7(②) |

단계 간 의존: A2 의 `currency` FK 검증은 기존 `Currency.json` 만 쓴다(A1 불필요). B1·B2 는 A 와 독립. C1 은 다른 단계와 독립. 순서는 사용자 확정(A1 → A2 → B1 → B2)을 따른다.

---

## §12 구현 요청사항 (gameplay-programmer 용)

### 12.1 Enum

| Enum | 변경 | 위치 |
|---|---|---|
| `EData` | 맨 끝에 16개 append, 이 순서: `BalanceConfig`, `MonsterStat`, `MetaConfig`, `ShopItem`, `ShopItem_Stat`, `ShopItem_Species`, `ShopItem_Mul`, `ShopItem_Desc`, `LordLevel`, `LordLevel_Reward`, `Achievement`, `HeroSkill`, `HeroSkill_Param`, `HeroArchetype_Skill`, `Card`, `Card_Param` (단계별로 해당 값만 추가). `HeroSkillLoadout*` 5개는 자리 보존 + 폐기 주석 | `CommonEnum.cs` |
| `EHeroSkill` (신규) | `DashStrike`, `AoeNova`, `OrbitingBlade`, `PiercingShot`, `ArrowVolley`, `Evade`, `ShadowStrike`, `BladeWhirl`, `SmokeBomb`, `CrushingLeap`, `ChainPull`, `BloodFrenzy`, `JudgmentHammer`, `PurifyingLight`, `Sanctuary` (15) | `CommonEnum.cs` |
| `ECardEffect` (신규) | `WispHpBoost`, `WraithDamageBoost`, `ReaperAtkSpeed`, `HexRangeBoost`, `PlagueToxinBoost`, `PhantomMoveSpeedBoost`, `SpawnWisps`, `SpawnWraith`, `SpawnReapers`, `SpawnPlagues`, `SpawnPhantoms`, `WispWraithPowerBoost`, `ReaperHexPowerBoost`, `HeroAttackDown`, `Fear`, `Bleed`, `Weaken`, `Slow`, `Frenzy`, `FastBreeding`, `BloodThirst`, `IronWill`, `TimeStop`, `GuardianRage`, `ToughHide`, `MarkOfDeath`, `SpawnerHaste`, `LingeringCurse` (28 — 기존 효과 클래스명에서 접미사 `Effect` 를 뗀 값) | `CommonEnum.cs` |

기존 Enum(`EMonster`·`ECardId`·`EShopEffectKind`·`EMonsterStatKind`·`EAchievementCondition`·`ECurrency`·`EBuildAxis`·`EHero`)은 값·순서 변경 없음.

### 12.2 Interface

신규 Interface 없음. `ICardEffect`·`IHeroSkillRuntime`·`IBattleContext` 시그니처 변경 없음.

### 12.3 에셋 키

- JSON 16개: `Assets/_Lair/Data/Json/<EData 값명>.json`, Addressables 주소 = 파일명, 라벨 `Resource` (기존 JSON 과 같은 그룹).
- `CardData` 에셋 파일명 = `ECardId` 값명 (현행 유지).
- `CardPool_Passive`·`CardPool_Active` 변경 없음.

### 12.4 스키마

row 필드명·타입은 §3.2~§3.11 표 그대로(필드명 = JSON 키, camelCase). 단일 객체 2개(`BalanceConfig`·`MetaConfig`)는 최상위가 객체, 나머지 14개는 최상위가 배열.

### 12.5 불변 조건 (구현이 지켜야 하는 것)

1. `MetaProfile` 클래스·버전·필드·세이브 문자열(`itemId`, `achievementId`, `ECardId` 값명) 변경 없음.
2. `BalanceConfig`·`MetaConfig`·`ShopItemDef`·`LordRewardDef`·`AchievementDef`·`CardData` 의 공개 멤버 이름 유지.
3. `GetMonster`·`GetSpawnPeriod` 의 등급 몬스터 → 기준 종 해석과 미발견 시 폴백(스폰 주기 9) 유지.
4. 상점·도전과제·영주 트랙의 표시 순서 = JSON 행 순서 = 현행 SO 리스트 순서.
5. 카드 풀 구성·순서 불변.

row → 런타임 모델 조립 방식, 효과·스킬 생성 방식 등 코드 구조는 gameplay-programmer 판단 영역이다.

---

## §13 핵심 결정과 대안 비교

| # | 결정 | 대안 | trade-off | 채택 |
|---|---|---|---|---|
| D1 | 런타임 타입 이름 | (가) `BalanceConfig`·`MetaConfig` 이름 유지, SO 상속만 제거 + `Current` | 두 타입을 참조하는 파일(`BalanceConfig` 22 + `MetaConfig` 61, 일부 중복)의 타입 이름 불변. JSON 이름과 런타임 클래스 이름이 같아 row 는 `…Row` 접미사로 구분해야 함 | **(가)** |
| | | (나) `BalanceDatabase`·`MetaDatabase` 신설, 기존 클래스 삭제 | 기존 `*Database` 명명과 완전 일치. 타입 이름 치환이 위 참조 파일 전부에 번짐 | |
| D2 | 스킬 SO 처리 | (가) 스킬·로드아웃 SO 20개 삭제, 전부 JSON | SO 에 남을 Object 참조가 0 이라 규칙에 정확히 맞음. 스킬 클래스 15개와 스킬 SO 를 직접 만드는 테스트(§7.3 목록) 수정 | **(가)** |
| | | (나) SO 는 종류 표시용 껍데기로 남기고 수치만 JSON 주입 | 코드 변경이 작음. 데이터 없는 SO 20개와 로드아웃(콘텐츠 배치)이 SO 에 남아 규칙 위반이 절반 남음 | |
| D3 | 카드 효과 종류 | (가) `effectKind` 를 `Card.json` 에 | 카드의 설계 정보(축·문구·효과·수치)가 한 곳. `ECardEffect` + 효과 생성 표 필요 | **(가)** |
| | | (나) `_effect` 타입은 SO 에 남기고 수치만 JSON | 변경이 작음. "어떤 효과인가"가 SO, "수치"가 JSON 으로 갈려 카드 한 장을 두 곳에서 읽어야 함 | |
| D4 | 카드 풀 소속 | (가) `CardPool` SO 유지 | Object 참조만 가진 SO 라 규칙에 맞음. 카드 SO 를 Addressables 로 불러올 목록이 어차피 필요. 데일리 시드가 의존하는 순서가 그대로 보존됨 | **(가)** |
| | | (나) `Card.json` 에 풀·순서 필드 추가 | 풀 구성이 텍스트 diff 로 보임. SO 목록과 JSON 이 같은 사실을 두 번 적게 됨(이중 진실) | |
| D5 | 영주 트랙 구조 | (가) `LordLevel`(9행) + `LordLevel_Reward`(5행) | SO 9행과 1:1, 암묵 규칙 0. PK 만 있는 표가 하나 생김 | **(가)** |
| | | (나) `MetaConfig.lordTrackMaxLevel` + 보상 5행만 | 표 1개 감소. "칸은 Lv2 부터"라는 암묵 규칙이 코드에 생김 | |
| | | (다) 메인 초안: `LordReward{level,nameKey}` + `LordReward_Souls` | 잠금 4행이 화면에 안 쓰이는 문구를 가리킴 | |
| D6 | 트리거 임계 | (가) `BalanceConfig.json` 안 배열 2개 | 파일 1개, 스칼라 목록에 맞는 형태 | **(가)** |
| | | (나) `PassiveTrigger`·`ActiveTrigger` 표 2개 | 임계마다 속성을 붙일 수 있음. 지금은 붙일 속성이 없음 | |
| D7 | `ShopItem.perLevelMul`·`descKey` | (가) 선택적 관계로 분리(`_Mul`·`_Desc`) | sentinel(1.0·빈 문자열) 제거, Rule 02 §11-1-3 준수. 표 2개 증가 | **(가)** |
| | | (나) 메인 초안: `ShopItem` 직접 필드 | 표가 적음. 18행의 1.0 과 12행의 빈 설명이 sentinel 로 남음 | |
| D8 | 수치 동일 증명 | (가) 단계별 일회용 패리티 테스트(SO vs JSON 전 필드), SO 삭제 시 함께 삭제 | 전 필드 증명, 이후 밸런스 조정에 부담 0 | **(가)** |
| | | (나) 영구 골든 테스트(기대값을 테스트에 하드코딩) | 영구 감시. 이후 수치 조정마다 테스트를 고쳐야 함 | |
| D9 | C2 `SpeciesVisual` | (가) 이번 제외 | 정적 함수 3종을 26곳(14파일)과 테스트가 DB 없이 부른다. JSON 화하면 로드 전 폴백이 필요해 하드코딩이 다시 남음. 값은 종 정체성 상수(이름·색·축)라 조정 빈도가 낮음 | **(가)** 권장 |
| | | (나) 이번 포함: `MonsterStat` 에 `nameKey`·`axis`·`glowColor` 추가 | 하드코딩 제거. 호출 시그니처 변경이 14파일에 번지고 테스트 격리 작업이 추가됨 | |

---

## §14 열린 질문 (사용자 선택 필요)

| # | 질문 | 추천 | 근거 |
|---|---|---|---|
| Q1 | 영웅 스킬 SO 20개(스킬 15 + 로드아웃 5)를 완전히 없애는가 | 없앤다 | §13 D2 — SO 에 남을 에셋 참조가 0 |
| Q2 | 카드의 효과 종류도 JSON 으로 옮기고 SO 에는 id + 그림 2장만 남기는가 | 그렇게 한다 | §13 D3 |
| Q3 | 카드 풀(패시브/액티브 소속과 순서)은 SO 에 그대로 두는가 | 그대로 둔다 | §13 D4 |
| Q4 | C1(영웅 스테이지 외형 수치)을 이번에 포함하는가. 포함 시 쓰이지 않는 발광 3필드(색·세기·켜짐)를 삭제하는가 | 포함(마지막 단계 C1), 발광 3필드 삭제 | 크기 배율·티어는 `Stage.json` 필드 2개로 끝나는 작은 이전. 발광은 25행 전부 꺼져 있어 적용된 적 없는 값(git 이력에는 남음) |
| Q5 | C2(종족 이름·색·축 하드코딩)를 이번에 포함하는가 | 제외, 후속 별도 기획 | §13 D9 |
| Q6 | 에디터 편집 창을 없애는가 — Meta Editor 창·JSON Sync 창 삭제, 카드 에디터는 그림·생성·풀 소속만 | 없앤다. 수치·문구는 JSON 텍스트 편집 | 다른 JSON 57개와 같은 워크플로. JSON 편집 창을 새로 만드는 것은 지금 필요한 근거가 없음 |
| Q7 | 새 JSON 로드 실패 시 전투 진입을 막고 마을로 돌려보내는가 | 막는다 | §6.4 — 틀린 수치·무보상 판이 랭킹에 오르는 것을 방지 |
| Q8 | 문구 id 를 1541~1687 연속 블록으로 배정하는가 | 그렇게 한다 | §5.1 — 기존 관례 |
| Q9 | 코드에 박힌 밸런스 상수(예: 광폭화 공속 배율, 약화의 저주 감소율, 단단한 살갗 감소율 — SO 가 아니라 C# 리터럴)도 다루는가 | 이번 범위 밖, 후속 감사 | 이번 목표는 SO 에 있던 값의 위치 이전. 코드 상수는 대상 목록부터 새로 감사해야 함 |
| Q10 | `CLAUDE.md` §4 의 `BalanceConfig.asset` 표기 수정을 승인하는가 | 승인 | §8.2 — 실제 경로와 문서 일치 |

---

## §15 Self-Review

- Placeholder: No Placeholders 표의 5개 카테고리 금지 표현 0건(문서 전체 검색으로 확인).
- 내부 일관성: 신설 16개 표 이름이 §2·§3·§6·§10·§11·§12 에서 동일. 행 수(6·31·6·24·13·19·9·5·13·15·97·15·28·26)와 문구 147개가 §3·§4·§5·§10 에서 동일.
- 스코프: 5단계 각각 2커밋으로 독립 구현·테스트 가능.
- 구현 요청사항: Enum(§12.1)·Interface(§12.2)·에셋 키(§12.3)·스키마(§12.4) 명세.
- UI 목업: 해당 없음(화면 변경 0).
