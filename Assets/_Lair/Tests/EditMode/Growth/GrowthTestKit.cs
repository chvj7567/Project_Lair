using System.IO;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 몬스터 육성 재화 본격 스위트 공용 — 출하 JSON 으로 GrowthDatabase · TowerDatabase · DailyConfig 를 만든다 (정적 Current 는 건드리지 않는다).
    public static class GrowthTestKit
    {
        private static string Dir => WorldMapTestSupport.JsonDir;

        private static T[] Rows<T>(string name) => JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(Dir + name + ".json"));

        public static GrowthDatabase Shipped()
        {
            return GrowthDatabase.Build(new GrowthDataSet
            {
                Currencies = Rows<CurrencyRow>("Currency"),
                Exchanges = Rows<Currency_ExchangeRow>("Currency_Exchange"),
                AwakenStats = Rows<AwakenStatRow>("AwakenStat"),
                NestMasteries = Rows<NestMasteryRow>("NestMastery"),
                NestParams = Rows<NestMastery_ParamRow>("NestMastery_Param"),
                Traits = Rows<MonsterTraitRow>("MonsterTrait"),
                TraitParams = Rows<MonsterTrait_ParamRow>("MonsterTrait_Param"),
                DifficultyAwakens = Rows<Difficulty_AwakenRow>("Difficulty_Awaken"),
                TowerCrystals = Rows<Difficulty_TowerCrystalRow>("Difficulty_TowerCrystal"),
                Shards = Rows<RelicRarity_ShardRow>("RelicRarity_Shard"),
            }, 2);
        }

        public static TowerDatabase ShippedTower()
        {
            return TowerDatabase.Build(
                JsonUtility.FromJson<TowerConfigRow>(File.ReadAllText(Dir + "TowerConfig.json")),
                Rows<HeroArchetype_TowerRow>("HeroArchetype_Tower"),
                Rows<Stage_TowerRow>("Stage_Tower"),
                WorldMapTestSupport.FullDb());
        }

        public static DailyConfigRow ShippedDailyConfig()
            => JsonUtility.FromJson<DailyConfigRow>(File.ReadAllText(Dir + "DailyConfig.json"));
    }
}
