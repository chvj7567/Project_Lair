using System.IO;
using ChvjUnityInfra;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 출하 데이터 회귀 — 육성 JSON 10종이 검증을 통과하고, 출하 MetaConfig.asset 의 육성 3축 항목 무결성 오류가 0 (monster-growth-currencies §6.3).
    public class GrowthShippedDataTests
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";
        private const string MetaConfigPath = "Assets/_Lair/Data/MetaConfig.asset";

        private static T[] Rows<T>(string name) => JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));

        [Test]
        public void 출하_육성_JSON과_MetaConfig_상점_항목은_검증_오류가_없다()
        {
            GrowthDatabase growth = GrowthDatabase.Build(new GrowthDataSet
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
            MetaConfig cfg = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);
            Assert.IsNotNull(cfg);

            CollectionAssert.IsEmpty(growth.ValidateShopItems(cfg));
            Assert.IsTrue(growth.HasAwaken && growth.HasNest && growth.HasTraits && growth.HasShards);
        }
    }
}
