using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# GrowthDatabase.Build 입력 - JSON 10종 파싱 결과 묶음. Unity 비의존.
    public class GrowthDataSet
    {
        public CurrencyRow[] Currencies;
        public Currency_ExchangeRow[] Exchanges;
        public AwakenStatRow[] AwakenStats;
        public NestMasteryRow[] NestMasteries;
        public NestMastery_ParamRow[] NestParams;
        public MonsterTraitRow[] Traits;
        public MonsterTrait_ParamRow[] TraitParams;
        public Difficulty_AwakenRow[] DifficultyAwakens;
        public Difficulty_TowerCrystalRow[] TowerCrystals;
        public RelicRarity_ShardRow[] Shards;
    }

    //# 각성 스탯 한 줄 - 종족의 스탯 x 레벨당 배율.
    public struct AwakenStatData
    {
        public EMonsterStatKind Stat;
        public float PerLevelMul;
    }

    //# 몬스터 육성 재화 데이터 검증 + 조회 (monster-growth-currencies §6). 로드 실패면 Current=null - 육성 재화 기능 비활성(§6.3-8).
    public class GrowthDatabase
    {
        public static GrowthDatabase Current { get; set; }

        public const int AwakenMaxLevel = 5;
        public const int TraitMaxTier = 3;

        //# 단계 효과별 필수 paramKey (§6.3-4).
        private static readonly Dictionary<ENestMasteryEffect, string[]> NestRequired = new Dictionary<ENestMasteryEffect, string[]>
        {
            { ENestMasteryEffect.FirstSpawnAdvance, new[] { "advanceSec" } },
            { ENestMasteryEffect.SpawnPeriod, new[] { "periodMul" } },
            { ENestMasteryEffect.SpawnGuard, new[] { "damageTakenMul", "durationSec" } },
        };

        //# 특성별 필수 paramKey (§6.2 표).
        private static readonly Dictionary<EMonsterTrait, string[]> TraitRequired = new Dictionary<EMonsterTrait, string[]>
        {
            { EMonsterTrait.ThornShell, new[] { "reflectRatio" } },
            { EMonsterTrait.Grudge, new[] { "radius", "powerMul", "durationSec" } },
            { EMonsterTrait.Execute, new[] { "hpThreshold", "damageMul" } },
            { EMonsterTrait.CurseStack, new[] { "perStack", "maxStack", "stackSec" } },
            { EMonsterTrait.Contagion, new[] { "radius", "addStacks" } },
            { EMonsterTrait.Afterimage, new[] { "dodgeCount", "hasteMul", "hasteSec" } },
        };

        private readonly Dictionary<ECurrency, CurrencyRow> _currencies = new Dictionary<ECurrency, CurrencyRow>();
        private readonly List<Currency_ExchangeRow> _exchanges = new List<Currency_ExchangeRow>();
        private readonly Dictionary<EMonster, List<AwakenStatData>> _awaken = new Dictionary<EMonster, List<AwakenStatData>>();
        private readonly Dictionary<int, ENestMasteryEffect> _nestEffects = new Dictionary<int, ENestMasteryEffect>();
        private readonly Dictionary<string, float> _nestParams = new Dictionary<string, float>();
        private readonly Dictionary<EMonster, MonsterTraitRow> _traits = new Dictionary<EMonster, MonsterTraitRow>();
        private readonly Dictionary<EMonster, EMonsterTrait> _traitKinds = new Dictionary<EMonster, EMonsterTrait>();
        private readonly Dictionary<string, float> _traitParams = new Dictionary<string, float>();
        private readonly Dictionary<int, Difficulty_AwakenRow> _difficultyAwaken = new Dictionary<int, Difficulty_AwakenRow>();
        private readonly Dictionary<int, int> _towerCrystal = new Dictionary<int, int>();
        private readonly Dictionary<ERelicRarity, int> _shards = new Dictionary<ERelicRarity, int>();

        private GrowthDatabase() { }

        //# 각 축 데이터가 들어왔는가 - 슬라이스별로 해당 JSON 이 없으면 그 축 기능만 꺼진다.
        public bool HasAwaken => _awaken.Count > 0;
        public bool HasNest => _nestEffects.Count > 0;
        public bool HasTraits => _traits.Count > 0;
        public bool HasShards => _shards.Count > 0;
        public int NestMaxLevel => _nestEffects.Count;
        public IReadOnlyList<Currency_ExchangeRow> Exchanges => _exchanges;

        public CurrencyRow GetCurrency(ECurrency currency)
            => _currencies.TryGetValue(currency, out CurrencyRow row) ? row : null;

        public Currency_ExchangeRow FindExchange(ECurrency from, ECurrency to)
        {
            foreach (Currency_ExchangeRow row in _exchanges)
            {
                if (row.fromCurrency == from.ToString() && row.toCurrency == to.ToString())
                    return row;
            }
            return null;
        }

        public IReadOnlyList<AwakenStatData> AwakenStatsOf(EMonster monster)
            => _awaken.TryGetValue(monster, out List<AwakenStatData> list) ? (IReadOnlyList<AwakenStatData>)list : Array.Empty<AwakenStatData>();

        public bool TryGetNestEffect(int level, out ENestMasteryEffect effect) => _nestEffects.TryGetValue(level, out effect);

        public float NestParam(int level, string key) => _nestParams.TryGetValue(NestKey(level, key), out float v) ? v : 0f;

        public MonsterTraitRow TraitOf(EMonster monster) => _traits.TryGetValue(monster, out MonsterTraitRow row) ? row : null;

        public bool TryGetTraitKind(EMonster monster, out EMonsterTrait trait) => _traitKinds.TryGetValue(monster, out trait);

        public float TraitParam(EMonster monster, int tier, string key)
            => _traitParams.TryGetValue(TraitKey(monster, tier, key), out float v) ? v : 0f;

        //# 선택적 관계 - Easy 처럼 행이 없으면 null.
        public Difficulty_AwakenRow AwakenRewardOf(int difficulty)
            => _difficultyAwaken.TryGetValue(difficulty, out Difficulty_AwakenRow row) ? row : null;

        public int CrystalPerFloor(int relicTier) => _towerCrystal.TryGetValue(relicTier, out int v) ? v : 0;

        public int ShardAmount(ERelicRarity rarity) => _shards.TryGetValue(rarity, out int v) ? v : 0;

        private static string NestKey(int level, string key) => level + "|" + key;
        private static string TraitKey(EMonster monster, int tier, string key) => monster + "|" + tier + "|" + key;

        //# 위반은 전부 모아 ChapterDataException (기존 오류 수집 패턴). maxDifficulty = Difficulty.json 최대 인덱스.
        public static GrowthDatabase Build(GrowthDataSet set, int maxDifficulty, Func<int, bool> nameKeyExists = null)
        {
            if (set == null || set.Currencies == null || set.Exchanges == null || set.DifficultyAwakens == null || set.TowerCrystals == null)
                throw new ChapterDataException("육성 재화 JSON(Currency/Currency_Exchange/Difficulty_Awaken/Difficulty_TowerCrystal) 이 비어 있음");

            List<string> errors = new List<string>();
            GrowthDatabase db = new GrowthDatabase();
            db.LoadCurrencies(set.Currencies, nameKeyExists, errors);
            db.LoadExchanges(set.Exchanges, errors);
            db.LoadDifficultyAwaken(set.DifficultyAwakens, maxDifficulty, errors);
            db.LoadTowerCrystal(set.TowerCrystals, maxDifficulty, errors);
            if (set.AwakenStats != null)
            {
                db.LoadAwaken(set.AwakenStats, errors);
            }
            if (set.NestMasteries != null || set.NestParams != null)
            {
                db.LoadNest(set.NestMasteries, set.NestParams, errors);
            }
            if (set.Traits != null || set.TraitParams != null)
            {
                db.LoadTraits(set.Traits, set.TraitParams, nameKeyExists, errors);
            }
            if (set.Shards != null)
            {
                db.LoadShards(set.Shards, errors);
            }
            if (errors.Count > 0)
                throw new ChapterDataException("육성 재화 데이터 오류:\n" + string.Join("\n", errors));
            return db;
        }

        private void LoadCurrencies(CurrencyRow[] rows, Func<int, bool> nameKeyExists, List<string> errors)
        {
            foreach (CurrencyRow row in rows)
            {
                if (row == null || TryParseEnum(row.currencyId, out ECurrency id) == false)
                {
                    errors.Add($"Currency.json currencyId 가 ECurrency 값이 아님: {row?.currencyId}");
                    continue;
                }
                if (_currencies.ContainsKey(id))
                {
                    errors.Add($"Currency.json {id} 중복");
                    continue;
                }
                if (nameKeyExists != null && nameKeyExists(row.nameKey) == false)
                {
                    errors.Add($"Currency.json {id} nameKey {row.nameKey} 가 Strings_Ko 에 없음");
                }
                if (IsHexColor(row.colorHex) == false)
                {
                    errors.Add($"Currency.json {id} colorHex 가 #RRGGBB 아님: {row.colorHex}");
                }
                _currencies[id] = row;
            }
            foreach (ECurrency c in (ECurrency[])Enum.GetValues(typeof(ECurrency)))
            {
                if (_currencies.ContainsKey(c) == false)
                {
                    errors.Add($"Currency.json 에 {c} 행이 없음");
                }
            }
        }

        private void LoadExchanges(Currency_ExchangeRow[] rows, List<string> errors)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (Currency_ExchangeRow row in rows)
            {
                if (row == null)
                    continue;
                bool fromOk = TryParseEnum(row.fromCurrency, out ECurrency from);
                bool toOk = TryParseEnum(row.toCurrency, out ECurrency to);
                if (fromOk == false || toOk == false)
                {
                    errors.Add($"Currency_Exchange.json 재화가 Currency 에 없음: {row.fromCurrency} -> {row.toCurrency}");
                    continue;
                }
                if (from == to || to == ECurrency.Soul || to == ECurrency.RelicShard)
                {
                    errors.Add($"Currency_Exchange.json 허용되지 않는 교환: {from} -> {to}");
                }
                if (row.fromAmount < 1 || row.toAmount < 1)
                {
                    errors.Add($"Currency_Exchange.json {from} -> {to} 수량은 1 이상이어야 함");
                }
                if (seen.Add(from + ">" + to) == false)
                {
                    errors.Add($"Currency_Exchange.json {from} -> {to} 중복");
                    continue;
                }
                _exchanges.Add(row);
            }
        }

        private void LoadDifficultyAwaken(Difficulty_AwakenRow[] rows, int maxDifficulty, List<string> errors)
        {
            foreach (Difficulty_AwakenRow row in rows)
            {
                if (row == null)
                    continue;
                if (row.difficulty < 1 || row.difficulty > maxDifficulty)
                {
                    errors.Add($"Difficulty_Awaken.json difficulty {row.difficulty} 가 1~{maxDifficulty} 밖");
                    continue;
                }
                if (row.firstClear < 0 || row.repeatWin < 0)
                {
                    errors.Add($"Difficulty_Awaken.json difficulty {row.difficulty} 값은 0 이상이어야 함");
                }
                if (_difficultyAwaken.ContainsKey(row.difficulty))
                {
                    errors.Add($"Difficulty_Awaken.json difficulty {row.difficulty} 중복");
                    continue;
                }
                _difficultyAwaken[row.difficulty] = row;
            }
        }

        private void LoadTowerCrystal(Difficulty_TowerCrystalRow[] rows, int maxDifficulty, List<string> errors)
        {
            foreach (Difficulty_TowerCrystalRow row in rows)
            {
                if (row == null)
                    continue;
                if (row.crystalPerFloor < 0)
                {
                    errors.Add($"Difficulty_TowerCrystal.json difficulty {row.difficulty} 값은 0 이상이어야 함");
                }
                if (_towerCrystal.ContainsKey(row.difficulty))
                {
                    errors.Add($"Difficulty_TowerCrystal.json difficulty {row.difficulty} 중복");
                    continue;
                }
                _towerCrystal[row.difficulty] = row.crystalPerFloor;
            }
            for (int d = 0; d <= maxDifficulty; ++d)
            {
                if (_towerCrystal.ContainsKey(d) == false)
                {
                    errors.Add($"Difficulty_TowerCrystal.json 에 difficulty {d} 행이 없음");
                }
            }
        }

        private void LoadAwaken(AwakenStatRow[] rows, List<string> errors)
        {
            Dictionary<EMonster, List<AwakenStatRow>> grouped = new Dictionary<EMonster, List<AwakenStatRow>>();
            foreach (AwakenStatRow row in rows)
            {
                if (row == null || TryParseEnum(row.monster, out EMonster monster) == false)
                {
                    errors.Add($"AwakenStat.json monster 가 EMonster 값이 아님: {row?.monster}");
                    continue;
                }
                if (TryParseEnum(row.statKind, out EMonsterStatKind stat) == false)
                {
                    errors.Add($"AwakenStat.json {monster} statKind 가 EMonsterStatKind 값이 아님: {row.statKind}");
                    continue;
                }
                if (row.perLevelMul > 0f == false)
                {
                    errors.Add($"AwakenStat.json {monster}#{row.statIndex} perLevelMul 은 0 보다 커야 함");
                }
                if (grouped.TryGetValue(monster, out List<AwakenStatRow> list) == false)
                {
                    list = new List<AwakenStatRow>();
                    grouped[monster] = list;
                }
                list.Add(row);
            }
            foreach (EMonster monster in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                if (grouped.TryGetValue(monster, out List<AwakenStatRow> list) == false)
                {
                    errors.Add($"AwakenStat.json 에 {monster} 행이 없음");
                    continue;
                }
                list.Sort((a, b) => a.statIndex.CompareTo(b.statIndex));
                List<AwakenStatData> stats = new List<AwakenStatData>();
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].statIndex != i + 1)
                    {
                        errors.Add($"AwakenStat.json {monster} statIndex 가 1부터 연속이 아님");
                        break;
                    }
                    Enum.TryParse(list[i].statKind, out EMonsterStatKind stat);
                    stats.Add(new AwakenStatData { Stat = stat, PerLevelMul = list[i].perLevelMul });
                }
                _awaken[monster] = stats;
            }
        }

        private void LoadNest(NestMasteryRow[] rows, NestMastery_ParamRow[] paramRows, List<string> errors)
        {
            if (rows == null || paramRows == null)
            {
                errors.Add("NestMastery.json 과 NestMastery_Param.json 은 함께 있어야 함");
                return;
            }
            foreach (NestMasteryRow row in rows)
            {
                if (row == null || TryParseEnum(row.effectKind, out ENestMasteryEffect effect) == false)
                {
                    errors.Add($"NestMastery.json effectKind 가 ENestMasteryEffect 값이 아님: {row?.effectKind}");
                    continue;
                }
                if (_nestEffects.ContainsKey(row.level))
                {
                    errors.Add($"NestMastery.json level {row.level} 중복");
                    continue;
                }
                _nestEffects[row.level] = effect;
            }
            for (int lv = 1; lv <= _nestEffects.Count; ++lv)
            {
                if (_nestEffects.ContainsKey(lv) == false)
                {
                    errors.Add($"NestMastery.json level 이 1부터 연속이 아님(누락 {lv})");
                }
            }
            foreach (NestMastery_ParamRow row in paramRows)
            {
                if (row == null || string.IsNullOrEmpty(row.paramKey))
                    continue;
                string key = NestKey(row.level, row.paramKey);
                if (_nestParams.ContainsKey(key))
                {
                    errors.Add($"NestMastery_Param.json ({row.level}, {row.paramKey}) 중복");
                    continue;
                }
                _nestParams[key] = row.value;
            }
            foreach (KeyValuePair<int, ENestMasteryEffect> pair in _nestEffects)
            {
                foreach (string required in NestRequired[pair.Value])
                {
                    if (_nestParams.ContainsKey(NestKey(pair.Key, required)) == false)
                    {
                        errors.Add($"NestMastery_Param.json level {pair.Key}({pair.Value}) 에 {required} 가 없음");
                    }
                }
            }
        }

        private void LoadTraits(MonsterTraitRow[] rows, MonsterTrait_ParamRow[] paramRows, Func<int, bool> nameKeyExists, List<string> errors)
        {
            if (rows == null || paramRows == null)
            {
                errors.Add("MonsterTrait.json 과 MonsterTrait_Param.json 은 함께 있어야 함");
                return;
            }
            HashSet<EMonsterTrait> seenTraits = new HashSet<EMonsterTrait>();
            foreach (MonsterTraitRow row in rows)
            {
                if (row == null || TryParseEnum(row.monster, out EMonster monster) == false)
                {
                    errors.Add($"MonsterTrait.json monster 가 EMonster 값이 아님: {row?.monster}");
                    continue;
                }
                if (TryParseEnum(row.trait, out EMonsterTrait trait) == false)
                {
                    errors.Add($"MonsterTrait.json {monster} trait 가 EMonsterTrait 값이 아님: {row.trait}");
                    continue;
                }
                if (_traits.ContainsKey(monster) || seenTraits.Add(trait) == false)
                {
                    errors.Add($"MonsterTrait.json {monster}/{trait} 중복");
                    continue;
                }
                if (nameKeyExists != null && (nameKeyExists(row.nameKey) == false || nameKeyExists(row.descKey) == false))
                {
                    errors.Add($"MonsterTrait.json {monster} nameKey/descKey 가 Strings_Ko 에 없음");
                }
                _traits[monster] = row;
                _traitKinds[monster] = trait;
            }
            foreach (EMonster monster in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                if (_traits.ContainsKey(monster) == false)
                {
                    errors.Add($"MonsterTrait.json 에 {monster} 행이 없음");
                }
            }
            foreach (MonsterTrait_ParamRow row in paramRows)
            {
                if (row == null || TryParseEnum(row.monster, out EMonster monster) == false || string.IsNullOrEmpty(row.paramKey))
                {
                    errors.Add($"MonsterTrait_Param.json 잘못된 행: {row?.monster}/{row?.paramKey}");
                    continue;
                }
                string key = TraitKey(monster, row.tier, row.paramKey);
                if (_traitParams.ContainsKey(key))
                {
                    errors.Add($"MonsterTrait_Param.json ({monster}, {row.tier}, {row.paramKey}) 중복");
                    continue;
                }
                _traitParams[key] = row.value;
            }
            foreach (KeyValuePair<EMonster, EMonsterTrait> pair in _traitKinds)
            {
                for (int tier = 1; tier <= TraitMaxTier; ++tier)
                {
                    foreach (string required in TraitRequired[pair.Value])
                    {
                        if (_traitParams.ContainsKey(TraitKey(pair.Key, tier, required)) == false)
                        {
                            errors.Add($"MonsterTrait_Param.json ({pair.Key}, {tier}) 에 {required} 가 없음");
                        }
                    }
                }
            }
        }

        private void LoadShards(RelicRarity_ShardRow[] rows, List<string> errors)
        {
            foreach (RelicRarity_ShardRow row in rows)
            {
                if (row == null || TryParseEnum(row.rarity, out ERelicRarity rarity) == false)
                {
                    errors.Add($"RelicRarity_Shard.json rarity 가 ERelicRarity 값이 아님: {row?.rarity}");
                    continue;
                }
                if (row.shardAmount < 1)
                {
                    errors.Add($"RelicRarity_Shard.json {rarity} shardAmount 는 1 이상이어야 함");
                }
                if (_shards.ContainsKey(rarity))
                {
                    errors.Add($"RelicRarity_Shard.json {rarity} 중복");
                    continue;
                }
                _shards[rarity] = row.shardAmount;
            }
            foreach (ERelicRarity rarity in (ERelicRarity[])Enum.GetValues(typeof(ERelicRarity)))
            {
                if (_shards.ContainsKey(rarity) == false)
                {
                    errors.Add($"RelicRarity_Shard.json 에 {rarity} 행이 없음");
                }
            }
        }

        //# §6.3-7 - 육성 3축 상점 항목 무결성. 오류 문구 목록을 돌려준다(빈 목록 = 통과).
        public List<string> ValidateShopItems(MetaConfig config)
        {
            List<string> errors = new List<string>();
            if (config == null)
                return errors;
            foreach (ShopItemDef item in config.ShopItems)
            {
                if (item == null)
                    continue;
                int expectedMax;
                bool hasData;
                switch (item.EffectKind)
                {
                    case EShopEffectKind.MonsterAwaken:
                        expectedMax = AwakenMaxLevel;
                        hasData = AwakenStatsOf(item.Species).Count > 0;
                        break;
                    case EShopEffectKind.NestMastery:
                        expectedMax = NestMaxLevel;
                        hasData = HasNest;
                        break;
                    case EShopEffectKind.MonsterTrait:
                        expectedMax = TraitMaxTier;
                        hasData = TraitOf(item.Species) != null;
                        break;
                    default:
                        continue;
                }
                if (item.Currency == ECurrency.Soul)
                {
                    errors.Add($"ShopItems {item.Id} 는 결제 재화가 Soul 이 아니어야 함");
                }
                if (hasData == false)
                {
                    errors.Add($"ShopItems {item.Id} 의 {item.Species} 육성 데이터가 없음");
                }
                if (item.MaxLevel != expectedMax)
                {
                    errors.Add($"ShopItems {item.Id} MaxLevel {item.MaxLevel} 가 데이터 최대 단계 {expectedMax} 와 다름");
                }
            }
            return errors;
        }

        private static bool IsHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#')
                return false;
            for (int i = 1; i < 7; ++i)
            {
                if (Uri.IsHexDigit(hex[i]) == false)
                    return false;
            }
            return true;
        }

        private static bool TryParseEnum<T>(string name, out T value) where T : struct
        {
            value = default;
            return string.IsNullOrEmpty(name) == false
                && Enum.TryParse(name, out value)
                && Enum.IsDefined(typeof(T), value)
                && int.TryParse(name, out int _) == false;
        }
    }
}
