using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 메타 진행 런타임 모델 — MetaConfig.json 외 8종의 조립 결과 (so-to-json-migration §6.1). 값은 JSON 이 단일 진실이다.
    //# 로더가 Current 에 올리고, 진입점(전투·마을·쇼케이스)이 한 번 받아 서비스에 인자로 넘긴다. 필드 기본값은 테스트 조립용이다.
    public class MetaConfig
    {
        //# 소울 보상 (기획서 §2.1)
        public int WinBaseSouls = 100;             //# 승리 기본 보상
        public float WinTimeBonusPerSec = 0.5f;    //# 승리 시 잔여 1초당 보너스 (floor)
        public int LoseMaxSouls = 60;              //# 패배 시 영웅 HP 깎은 비율 × 이 값 (floor)

        //# 영주 XP (기획서 §4.1)
        public int WinXp = 100;
        public int LoseXp = 40;
        public int XpPerLevelBase = 100;           //# Lv1→2 필요 XP
        public float XpGrowth = 1.25f;             //# 레벨당 필요 XP 증가율

        //# 상점 / 영주 보상 / 도전과제 (기획서 §3.2 / §4.3 / §5.2)
        public List<ShopItemDef> ShopItems = new List<ShopItemDef>();
        public EnhanceTierTable EnhanceTiers;
        public List<LordRewardDef> LordRewards = new List<LordRewardDef>();
        public List<AchievementDef> Achievements = new List<AchievementDef>();

        public ShopItemDef FindShopItem(string id)
        {
            foreach (ShopItemDef item in ShopItems)
            {
                if (item != null && item.Id == id)
                    return item;
            }
            return null;
        }

        //# 로드·검증 성공 시에만 채워진다 (so-to-json-migration §6.2 7번). null 이면 메타 미로드 — 전투 진입 불가(§6.4).
        public static MetaConfig Current { get; set; }

        //# JSON row → 런타임 모델. 위반(§10 SJ3~SJ5)은 전부 모아 ChapterDataException 으로 던진다.
        //# nameKeyExists·text 는 Strings_Ko 조회, currencyExists 는 Currency.json 의 재화 존재 조회 (Data 가 로더·Growth 에 기대지 않게 주입).
        public static MetaConfig Build(MetaDataSet data, Func<int, bool> nameKeyExists, Func<int, string> text, Func<ECurrency, bool> currencyExists)
        {
            if (data == null || data.Config == null)
                throw new ChapterDataException("MetaConfig.json 이 비어 있음");

            List<string> errors = new List<string>();
            ValidateConstants(data.Config, errors);
            List<ShopItemDef> shopItems = BuildShopItems(data, nameKeyExists, text, currencyExists, errors);
            List<LordRewardDef> lordRewards = BuildLordRewards(data, nameKeyExists, text, errors);
            List<AchievementDef> achievements = BuildAchievements(data.Achievements, nameKeyExists, text, errors);
            if (errors.Count > 0)
                throw new ChapterDataException("메타 데이터 검증 실패:\n - " + string.Join("\n - ", errors));

            MetaConfig result = new MetaConfig();
            result.WinBaseSouls = data.Config.winBaseSouls;
            result.WinTimeBonusPerSec = data.Config.winTimeBonusPerSec;
            result.LoseMaxSouls = data.Config.loseMaxSouls;
            result.WinXp = data.Config.winXp;
            result.LoseXp = data.Config.loseXp;
            result.XpPerLevelBase = data.Config.xpPerLevelBase;
            result.XpGrowth = data.Config.xpGrowth;
            result.ShopItems = shopItems;
            result.LordRewards = lordRewards;
            result.Achievements = achievements;
            return result;
        }

        //# 7개 상수 전부 0 초과(xpGrowth 는 1 이상) — JsonUtility 가 오타·누락 필드를 0 으로 채우는 것을 막는다.
        private static void ValidateConstants(MetaConfigRow c, List<string> errors)
        {
            if (c.winBaseSouls < 1 || c.loseMaxSouls < 1 || c.winXp < 1 || c.loseXp < 1 || c.xpPerLevelBase < 1)
            {
                errors.Add("MetaConfig.json winBaseSouls·loseMaxSouls·winXp·loseXp·xpPerLevelBase 는 1 이상이어야 함");
            }
            if (c.winTimeBonusPerSec <= 0f || float.IsNaN(c.winTimeBonusPerSec))
            {
                errors.Add("MetaConfig.json winTimeBonusPerSec 는 0 보다 커야 함");
            }
            if (c.xpGrowth < 1f || float.IsNaN(c.xpGrowth))
            {
                errors.Add("MetaConfig.json xpGrowth 는 1 이상이어야 함");
            }
        }

        //# Strings_Ko 에 id 0 이 실재하므로 존재 검사만으로는 누락(0 채움)을 못 잡는다 — 양수 조건을 함께 건다.
        private static bool KeyOk(int key, Func<int, bool> nameKeyExists) => key > 0 && nameKeyExists(key);

        private static string TextOf(int key, Func<int, string> text) => text(key) ?? string.Empty;

        private static List<ShopItemDef> BuildShopItems(MetaDataSet data, Func<int, bool> nameKeyExists, Func<int, string> text,
            Func<ECurrency, bool> currencyExists, List<string> errors)
        {
            ShopItemRow[] itemRows = data.ShopItems ?? Array.Empty<ShopItemRow>();
            if (itemRows.Length == 0)
            {
                errors.Add("ShopItem.json 이 비어 있음");
            }

            Dictionary<string, ShopItemRow> items = new Dictionary<string, ShopItemRow>();
            foreach (ShopItemRow row in itemRows)
            {
                if (row == null || string.IsNullOrEmpty(row.itemId))
                {
                    errors.Add("ShopItem.json itemId 가 비어 있음");
                    continue;
                }
                if (items.ContainsKey(row.itemId))
                {
                    errors.Add($"ShopItem.json itemId {row.itemId} 중복");
                    continue;
                }
                items.Add(row.itemId, row);
            }

            Dictionary<string, ShopItem_StatRow> stats = IndexByItem(data.Stats, r => r?.itemId, "ShopItem_Stat", items, errors);
            Dictionary<string, ShopItem_SpeciesRow> species = IndexByItem(data.Species, r => r?.itemId, "ShopItem_Species", items, errors);
            Dictionary<string, ShopItem_MulRow> muls = IndexByItem(data.Muls, r => r?.itemId, "ShopItem_Mul", items, errors);
            Dictionary<string, ShopItem_DescRow> descs = IndexByItem(data.Descs, r => r?.itemId, "ShopItem_Desc", items, errors);

            List<ShopItemDef> result = new List<ShopItemDef>();
            foreach (ShopItemRow row in itemRows)
            {
                if (row == null || string.IsNullOrEmpty(row.itemId) || items[row.itemId] != row)
                    continue;
                string id = row.itemId;
                if (KeyOk(row.nameKey, nameKeyExists) == false)
                {
                    errors.Add($"ShopItem[{id}].nameKey {row.nameKey} 가 Strings_Ko 에 없음");
                }
                if (row.maxLevel < 1 || row.basePrice < 1 || row.priceGrowth < 1f || float.IsNaN(row.priceGrowth))
                {
                    errors.Add($"ShopItem[{id}] maxLevel·basePrice 는 1 이상, priceGrowth 는 1 이상이어야 함");
                }
                if (FormationDatabase.TryParseEnum(row.effectKind, out EShopEffectKind kind) == false)
                {
                    errors.Add($"ShopItem[{id}].effectKind '{row.effectKind}' 가 EShopEffectKind 에 없음");
                    continue;
                }
                if (FormationDatabase.TryParseEnum(row.currency, out ECurrency currency) == false || currencyExists(currency) == false)
                {
                    errors.Add($"ShopItem[{id}].currency '{row.currency}' 가 ECurrency·Currency.json 에 없음");
                    continue;
                }

                bool needStat = kind == EShopEffectKind.MonsterStat;
                bool needSpecies = kind == EShopEffectKind.MonsterSpecies || kind == EShopEffectKind.MonsterAwaken
                    || kind == EShopEffectKind.NestMastery || kind == EShopEffectKind.MonsterTrait;
                bool needMul = kind == EShopEffectKind.MonsterStat || kind == EShopEffectKind.SpawnerPeriod || kind == EShopEffectKind.MonsterSpecies;
                CheckRelation("ShopItem_Stat", id, kind, needStat, stats.ContainsKey(id), errors);
                CheckRelation("ShopItem_Species", id, kind, needSpecies, species.ContainsKey(id), errors);
                CheckRelation("ShopItem_Mul", id, kind, needMul, muls.ContainsKey(id), errors);

                ShopItemDef def = new ShopItemDef
                {
                    Id = id,
                    DisplayName = TextOf(row.nameKey, text),
                    Description = string.Empty,
                    EffectKind = kind,
                    StatKind = EMonsterStatKind.Hp,
                    Species = EMonster.Wisp,
                    PerLevelMul = 1f,
                    MaxLevel = row.maxLevel,
                    BasePrice = row.basePrice,
                    PriceGrowth = row.priceGrowth,
                    Currency = currency,
                };
                if (stats.TryGetValue(id, out ShopItem_StatRow statRow))
                {
                    if (FormationDatabase.TryParseEnum(statRow.statKind, out EMonsterStatKind statKind))
                    {
                        def.StatKind = statKind;
                    }
                    else
                    {
                        errors.Add($"ShopItem_Stat[{id}].statKind '{statRow.statKind}' 가 EMonsterStatKind 에 없음");
                    }
                }
                if (species.TryGetValue(id, out ShopItem_SpeciesRow speciesRow))
                {
                    if (FormationDatabase.TryParseMonster(speciesRow.monster, out EMonster monster) && MonsterGrades.IsBase(monster))
                    {
                        def.Species = monster;
                    }
                    else
                    {
                        errors.Add($"ShopItem_Species[{id}].monster '{speciesRow.monster}' 는 EMonster 기준 6종 값명이어야 함");
                    }
                }
                if (muls.TryGetValue(id, out ShopItem_MulRow mulRow))
                {
                    if (mulRow.perLevelMul <= 0f || float.IsNaN(mulRow.perLevelMul))
                    {
                        errors.Add($"ShopItem_Mul[{id}].perLevelMul 은 0 보다 커야 함");
                    }
                    def.PerLevelMul = mulRow.perLevelMul;
                }
                if (descs.TryGetValue(id, out ShopItem_DescRow descRow))
                {
                    if (KeyOk(descRow.descKey, nameKeyExists) == false)
                    {
                        errors.Add($"ShopItem_Desc[{id}].descKey {descRow.descKey} 가 Strings_Ko 에 없음");
                    }
                    def.Description = TextOf(descRow.descKey, text);
                }
                result.Add(def);
            }
            return result;
        }

        //# 관계 행을 itemId 로 색인 — null·중복 PK·ShopItem 에 없는 FK 는 오류로 모으고 색인에서 뺀다.
        private static Dictionary<string, T> IndexByItem<T>(T[] relation, Func<T, string> itemIdOf, string label,
            Dictionary<string, ShopItemRow> items, List<string> errors) where T : class
        {
            Dictionary<string, T> index = new Dictionary<string, T>();
            foreach (T row in relation ?? Array.Empty<T>())
            {
                string id = row != null ? itemIdOf(row) : null;
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"{label}.json itemId 가 비어 있음");
                    continue;
                }
                if (items.ContainsKey(id) == false)
                {
                    errors.Add($"{label}.json itemId {id} 가 ShopItem.json 에 없음");
                    continue;
                }
                if (index.ContainsKey(id))
                {
                    errors.Add($"{label}.json itemId {id} 중복");
                    continue;
                }
                index.Add(id, row);
            }
            return index;
        }

        //# effectKind 별 관계 행 규칙(§3.6) — 필수인데 없거나 금지인데 있으면 오류.
        private static void CheckRelation(string label, string itemId, EShopEffectKind kind, bool required, bool present, List<string> errors)
        {
            if (required && present == false)
            {
                errors.Add($"{label}.json 에 {itemId}({kind}) 행이 없음");
            }
            else if (required == false && present)
            {
                errors.Add($"{label}.json 에 {itemId}({kind}) 행이 있으면 안 됨");
            }
        }

        //# 칸 level 은 1 이상 엄격 오름차순(JSON 행 순서 = 표시 순서). 보상 행이 없는 칸은 잠금 칸(이름·소울 비움).
        private static List<LordRewardDef> BuildLordRewards(MetaDataSet data, Func<int, bool> nameKeyExists, Func<int, string> text, List<string> errors)
        {
            LordLevelRow[] levels = data.LordLevels ?? Array.Empty<LordLevelRow>();
            if (levels.Length == 0)
            {
                errors.Add("LordLevel.json 이 비어 있음");
            }

            HashSet<int> levelSet = new HashSet<int>();
            int previous = 0;
            foreach (LordLevelRow row in levels)
            {
                if (row == null || row.level < 1)
                {
                    errors.Add("LordLevel.json level 은 1 이상이어야 함");
                    continue;
                }
                if (row.level <= previous)
                {
                    errors.Add($"LordLevel.json level {row.level} 는 중복 없이 오름차순이어야 함");
                    continue;
                }
                previous = row.level;
                levelSet.Add(row.level);
            }

            Dictionary<int, LordLevel_RewardRow> rewards = new Dictionary<int, LordLevel_RewardRow>();
            foreach (LordLevel_RewardRow row in data.LordRewards ?? Array.Empty<LordLevel_RewardRow>())
            {
                if (row == null || levelSet.Contains(row.level) == false)
                {
                    errors.Add($"LordLevel_Reward.json level {row?.level} 가 LordLevel.json 에 없음");
                    continue;
                }
                if (rewards.ContainsKey(row.level))
                {
                    errors.Add($"LordLevel_Reward.json level {row.level} 중복");
                    continue;
                }
                if (KeyOk(row.nameKey, nameKeyExists) == false)
                {
                    errors.Add($"LordLevel_Reward[{row.level}].nameKey {row.nameKey} 가 Strings_Ko 에 없음");
                }
                if (row.rewardSouls < 1)
                {
                    errors.Add($"LordLevel_Reward[{row.level}].rewardSouls 는 1 이상이어야 함");
                }
                rewards.Add(row.level, row);
            }

            List<LordRewardDef> result = new List<LordRewardDef>();
            foreach (LordLevelRow row in levels)
            {
                if (row == null || levelSet.Contains(row.level) == false)
                    continue;
                if (rewards.TryGetValue(row.level, out LordLevel_RewardRow reward))
                {
                    result.Add(new LordRewardDef
                    {
                        Level = row.level,
                        IsLockedDummy = false,
                        RewardSouls = reward.rewardSouls,
                        DisplayName = TextOf(reward.nameKey, text),
                    });
                }
                else
                {
                    result.Add(new LordRewardDef { Level = row.level, IsLockedDummy = true, RewardSouls = 0, DisplayName = string.Empty });
                }
            }
            return result;
        }

        private static List<AchievementDef> BuildAchievements(AchievementRow[] rows, Func<int, bool> nameKeyExists, Func<int, string> text,
            List<string> errors)
        {
            rows = rows ?? Array.Empty<AchievementRow>();
            if (rows.Length == 0)
            {
                errors.Add("Achievement.json 이 비어 있음");
            }

            HashSet<string> seen = new HashSet<string>();
            List<AchievementDef> result = new List<AchievementDef>();
            foreach (AchievementRow row in rows)
            {
                if (row == null || string.IsNullOrEmpty(row.achievementId))
                {
                    errors.Add("Achievement.json achievementId 가 비어 있음");
                    continue;
                }
                if (seen.Add(row.achievementId) == false)
                {
                    errors.Add($"Achievement.json achievementId {row.achievementId} 중복");
                    continue;
                }
                if (KeyOk(row.nameKey, nameKeyExists) == false || KeyOk(row.descKey, nameKeyExists) == false)
                {
                    errors.Add($"Achievement[{row.achievementId}] nameKey {row.nameKey}·descKey {row.descKey} 가 Strings_Ko 에 없음");
                }
                if (FormationDatabase.TryParseEnum(row.condition, out EAchievementCondition condition) == false)
                {
                    errors.Add($"Achievement[{row.achievementId}].condition '{row.condition}' 가 EAchievementCondition 에 없음");
                    continue;
                }
                if (row.threshold <= 0f || float.IsNaN(row.threshold) || row.rewardSouls < 1)
                {
                    errors.Add($"Achievement[{row.achievementId}] threshold 는 0 보다 크고 rewardSouls 는 1 이상이어야 함");
                }
                result.Add(new AchievementDef
                {
                    Id = row.achievementId,
                    DisplayName = TextOf(row.nameKey, text),
                    Description = TextOf(row.descKey, text),
                    Condition = condition,
                    Threshold = row.threshold,
                    RewardSouls = row.rewardSouls,
                });
            }
            return result;
        }
    }

    [Serializable]
    public class ShopItemDef
    {
        public string Id;                  //# 예: "MonsterHpUp" — MetaProfile.ShopLevels 키
        public string DisplayName;
        public string Description;         //# 효과 설명문 (셀 표기) — 기획서 §3.2 / §11.3 [추가 3]
        public EShopEffectKind EffectKind;
        public EMonsterStatKind StatKind;  //# EffectKind == MonsterStat 일 때만 사용
        public EMonster Species;           //# EffectKind == MonsterSpecies 일 때만 사용 — 강화 대상 종족
        public float PerLevelMul = 1f;     //# 레벨당 곱연산 배율 (SpawnerPeriod/Cooldown 은 1 미만이 강화). 육성 3축은 1.0 고정(수치는 JSON)
        public int MaxLevel = 5;
        public int BasePrice = 50;
        public float PriceGrowth = 1.6f;   //# 레벨당 가격 배율 (floor)
        //# 결제 재화 (monster-growth-currencies §6.2, 필수 1:1). 기본 Soul(enum 0) — 기존 항목은 에셋 재저장 없이 호환.
        public ECurrency Currency = ECurrency.Soul;
    }

    [Serializable]
    public class LordRewardDef
    {
        public int Level;
        public bool IsLockedDummy;         //# true 면 "??? — 추후 해금" 잠금 슬롯
        public int RewardSouls;            //# IsLockedDummy == false 일 때 지급
        public string DisplayName;
    }

    [Serializable]
    public class AchievementDef
    {
        public string Id;                  //# 예: "FirstWin" — MetaProfile.AchievedIds 키
        public string DisplayName;
        public string Description;
        public EAchievementCondition Condition;
        public float Threshold;            //# WinUnderSeconds=초 / TotalWins·TotalRuns=횟수 / SynergyTierReached=티어
        public int RewardSouls = 30;
    }
}
