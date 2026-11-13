using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 월드맵(슬라이스 c) 테스트 공용 — shipped JSON 기반 ChapterDatabase 와 실제 Strings_Ko 조회.
    public static class WorldMapTestSupport
    {
        public const string JsonDir = "Assets/_Lair/Data/Json/";

        private static Dictionary<int, string> _strings;

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        //# 실서비스와 같이 로드 직후 진행 범위(MaxStage)를 챕터 수에 맞춘다(ChapterDatabaseLoader 동작) — 호출 테스트는 정적 상태 격리(ChapterStaticStateTestBase)로 원복한다.
        public static ChapterDatabase ShippedDb()
        {
            ChapterDatabase db = FullDb();
            StageProgress.ConfigureChapterCount(db.ChapterCount);
            return db;
        }

        //# shipped 데이터 전체 — ConfigureChapterCount 를 건드리지 않는다(정적 상태 불변).
        public static ChapterDatabase FullDb()
        {
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter"), Load<StageRow>("Stage"), Load<Stage_RuleRow>("Stage_Rule"),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param"), Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"),
                Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish"),
                Load<HeroArchetype_ChargeRow>("HeroArchetype_Charge"), Load<HeroArchetype_CleaveRow>("HeroArchetype_Cleave"),
                Load<HeroArchetype_GuardRow>("HeroArchetype_Guard"), Load<HeroArchetype_DebuffResistRow>("HeroArchetype_DebuffResist"),
                ShippedBalance().PassiveThresholds);
        }

        //# shipped BalanceConfig.json + MonsterStat.json 조립 — 로더와 같은 검증을 거친다 (so-to-json-migration §7.5).
        public static BalanceConfig ShippedBalance()
        {
            return BalanceConfig.Build(
                UnityEngine.JsonUtility.FromJson<BalanceConfigRow>(File.ReadAllText(JsonDir + "BalanceConfig.json")),
                Load<MonsterStatRow>("MonsterStat"));
        }

        //# shipped 메타 JSON 9종 묶음 — 엣지 테스트가 행을 바꿔 BuildMeta 로 다시 조립할 수 있게 조립 전 단계도 노출한다.
        public static MetaDataSet ShippedMetaData()
        {
            return new MetaDataSet
            {
                Config = UnityEngine.JsonUtility.FromJson<MetaConfigRow>(File.ReadAllText(JsonDir + "MetaConfig.json")),
                ShopItems = Load<ShopItemRow>("ShopItem"),
                Stats = Load<ShopItem_StatRow>("ShopItem_Stat"),
                Species = Load<ShopItem_SpeciesRow>("ShopItem_Species"),
                Muls = Load<ShopItem_MulRow>("ShopItem_Mul"),
                Descs = Load<ShopItem_DescRow>("ShopItem_Desc"),
                LordLevels = Load<LordLevelRow>("LordLevel"),
                LordRewards = Load<LordLevel_RewardRow>("LordLevel_Reward"),
                Achievements = Load<AchievementRow>("Achievement"),
            };
        }

        //# 엄격 조립 — Strings_Ko 에 없는 id 는 오류(로더의 provider-null 관용 없음), 재화는 Currency.json 행으로 판정한다.
        public static MetaConfig BuildMeta(MetaDataSet data)
        {
            HashSet<string> currencies = new HashSet<string>();
            foreach (CurrencyRow row in Load<CurrencyRow>("Currency"))
            {
                currencies.Add(row.currencyId);
            }
            return MetaConfig.Build(data, id => Strings().ContainsKey(id), Text, currency => currencies.Contains(currency.ToString()));
        }

        //# shipped 메타 JSON 9종 조립 — 커밋 ① 동안 결과가 SO 인스턴스라 호출자가 DestroyImmediate 한다 (so-to-json-migration §7.5).
        public static MetaConfig ShippedMeta()
        {
            return BuildMeta(ShippedMetaData());
        }

        public static IReadOnlyDictionary<int, string> Strings()
        {
            if (_strings == null)
            {
                Dictionary<int, string> table = new Dictionary<int, string>();
                foreach (StringEntry entry in Load<StringEntry>("Strings_Ko"))
                {
                    table[entry.id] = entry.text;
                }
                _strings = table;
            }
            return _strings;
        }

        //# 실제 Strings_Ko — id 누락이면 빈 문자열.
        public static string Text(int id)
        {
            return Strings().TryGetValue(id, out string text) ? text : string.Empty;
        }
    }
}
