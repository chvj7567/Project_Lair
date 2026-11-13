using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ChvjUnityInfra;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 출하 데이터 회귀 (so-to-json-migration §10) — 메타 JSON 9종이 로드 검증(SJ3~SJ5)을 통과하고 에셋 키 규약(SJ6)을 지킨다.
    public class MetaShippedDataTests
    {
        private const string GroupDir = "Assets/AddressableAssetsData/AssetGroups";
        private static readonly EData[] Keys =
        {
            EData.MetaConfig, EData.ShopItem, EData.ShopItem_Stat, EData.ShopItem_Species, EData.ShopItem_Mul,
            EData.ShopItem_Desc, EData.LordLevel, EData.LordLevel_Reward, EData.Achievement,
        };

        private static T[] Rows<T>(string name) => JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(WorldMapTestSupport.JsonDir + name + ".json"));

        [Test]
        public void 출하_메타_JSON은_검증을_통과하고_행_수가_기획서와_같다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            Assert.AreEqual(31, data.ShopItems.Length, "ShopItem");
            Assert.AreEqual(6, data.Stats.Length, "ShopItem_Stat");
            Assert.AreEqual(24, data.Species.Length, "ShopItem_Species");
            Assert.AreEqual(13, data.Muls.Length, "ShopItem_Mul");
            Assert.AreEqual(19, data.Descs.Length, "ShopItem_Desc");
            Assert.AreEqual(9, data.LordLevels.Length, "LordLevel");
            Assert.AreEqual(5, data.LordRewards.Length, "LordLevel_Reward");
            Assert.AreEqual(13, data.Achievements.Length, "Achievement");

            MetaConfig meta = WorldMapTestSupport.BuildMeta(data);
            try
            {
                Assert.AreEqual(31, meta.ShopItems.Count);
                Assert.AreEqual(9, meta.LordRewards.Count);
                Assert.AreEqual(13, meta.Achievements.Count);
            }
            finally
            {
                Object.DestroyImmediate(meta);
            }
        }

        [Test]
        public void Strings_Ko_1541에서_1616은_76개가_연속_존재하고_비어있지_않다()
        {
            for (int id = 1541; id <= 1616; id++)
            {
                Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(id), $"id {id} 없음");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id} 비어 있음");
            }
        }

        //# 육성 3축 상점 항목(각성·둥지·특성)의 결제 재화·최대 레벨·종족 데이터가 JSON 조립 결과로도 무결하다 — 로더 이관(커밋 ②) 전 동등성 확인.
        [Test]
        public void 출하_메타_JSON_상점_항목은_육성_상점_무결성_검사를_통과한다()
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
            MetaConfig meta = WorldMapTestSupport.ShippedMeta();
            try
            {
                CollectionAssert.IsEmpty(growth.ValidateShopItems(meta));
            }
            finally
            {
                Object.DestroyImmediate(meta);
            }
        }

        [Test]
        public void effectKind_별_관계_행이_빠지거나_넘치면_위반을_모아_던진다()
        {
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            List<ShopItem_MulRow> muls = new List<ShopItem_MulRow>(data.Muls);
            muls.RemoveAll(row => row.itemId == "MonsterHpUp");
            data.Muls = muls.ToArray();
            List<ShopItem_StatRow> stats = new List<ShopItem_StatRow>(data.Stats);
            stats.Add(new ShopItem_StatRow { itemId = "SpawnerHasteUp", statKind = "Hp" });
            data.Stats = stats.ToArray();

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));

            StringAssert.Contains("ShopItem_Mul.json 에 MonsterHpUp(MonsterStat) 행이 없음", e.Message);
            StringAssert.Contains("ShopItem_Stat.json 에 SpawnerHasteUp(SpawnerPeriod) 행이 있으면 안 됨", e.Message);
        }

        //# Strings_Ko 에 id 0 이 실재하고 JsonUtility 는 누락 필드를 0 으로 채운다 — 존재 검사만으로는 누락을 못 잡으므로 양수 조건이 필요하다.
        [Test]
        public void nameKey_와_상수가_0으로_채워지면_Strings_Ko_에_id_0_이_있어도_위반이다()
        {
            Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(0), "전제: Strings_Ko 에 id 0 존재");
            MetaDataSet data = WorldMapTestSupport.ShippedMetaData();
            data.ShopItems[0].nameKey = 0;
            data.Config.winBaseSouls = 0;
            data.Achievements[0].descKey = 0;

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildMeta(data));

            StringAssert.Contains("ShopItem[MonsterHpUp].nameKey 0", e.Message);
            StringAssert.Contains("winBaseSouls", e.Message);
            StringAssert.Contains("Achievement[FirstRun]", e.Message);
        }

        [Test]
        public void 메타_JSON_9종은_EData_값명_파일로_Addressables에_Resource_라벨로_한_번만_등록돼_있다()
        {
            string groups = string.Empty;
            foreach (string path in Directory.GetFiles(GroupDir, "*.asset"))
            {
                groups += File.ReadAllText(path).Replace("\r\n", "\n") + "\n";
            }

            foreach (EData key in Keys)
            {
                string jsonPath = WorldMapTestSupport.JsonDir + key + ".json";
                Assert.IsTrue(File.Exists(jsonPath), jsonPath);
                string entry = "  - m_GUID: " + AssetDatabase.AssetPathToGUID(jsonPath) + "\n    m_Address: " + key
                    + "\n    m_ReadOnly: 0\n    m_SerializedLabels:\n    - Resource\n";
                StringAssert.Contains(entry, groups, key.ToString());
                Assert.AreEqual(1, Regex.Matches(groups, "m_Address: " + key + "\n").Count, key + " 주소 중복");
            }
        }
    }
}
