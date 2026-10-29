using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 잔해 슬라이스 E 자체 테스트 (정상 + 엣지) — 본격 스위트는 test-engineer (monster-growth-currencies §11.7).
    public class RelicShardTests
    {
        private static GrowthDatabase Growth()
        {
            return GrowthDatabase.Build(new GrowthDataSet
            {
                Currencies = new[]
                {
                    new CurrencyRow { currencyId = "Soul", colorHex = "#5ef0b4" },
                    new CurrencyRow { currencyId = "AwakenStone", colorHex = "#ff9f43" },
                    new CurrencyRow { currencyId = "MoonSeal", colorHex = "#d8e4ff" },
                    new CurrencyRow { currencyId = "AbyssCrystal", colorHex = "#2ec4c4" },
                    new CurrencyRow { currencyId = "RelicShard", colorHex = "#c8a27a" },
                },
                Exchanges = new[] { new Currency_ExchangeRow { fromCurrency = "RelicShard", toCurrency = "AwakenStone", fromAmount = 3, toAmount = 1 } },
                DifficultyAwakens = new Difficulty_AwakenRow[0],
                TowerCrystals = new[] { new Difficulty_TowerCrystalRow { difficulty = 0, crystalPerFloor = 1 } },
                Shards = new[]
                {
                    new RelicRarity_ShardRow { rarity = "Common", shardAmount = 1 },
                    new RelicRarity_ShardRow { rarity = "Rare", shardAmount = 3 },
                    new RelicRarity_ShardRow { rarity = "Legendary", shardAmount = 8 },
                },
            }, 0);
        }

        [Test]
        public void 뽑힌_등급이_모두_최대_중첩이면_유물_대신_잔해를_주고_연패를_0으로_돌린다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile p = new MetaProfile { RelicMissStreak = 2 };
            foreach (Relic_DropRow drop in db.DropsOf("Knight"))
            {
                RelicData data = db.Get(drop.relicId);
                if (data != null && data.Rarity == ERelicRarity.Legendary)
                {
                    while (p.GetRelicStack(drop.relicId) < data.MaxStack)
                    {
                        p.AddRelic(drop.relicId);
                    }
                }
            }
            ScriptedRng rng = new ScriptedRng(0f, 0.9999f, 0f);

            RelicDropResult result = RelicDropRoller.Roll(db, p, "Knight", 1, 1, 0, true, rng.Next, Growth());

            Assert.IsTrue(result.IsShard);
            Assert.AreEqual(ERelicRarity.Legendary, result.ShardRarity);
            Assert.AreEqual(8, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(0, p.RelicMissStreak);
        }

        [Test]
        public void 다_찬_등급이_없으면_잔해_규칙이_있어도_같은_시드에서_같은_유물이_나온다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile a = new MetaProfile();
            a.AddRelic("SoulPurse");
            MetaProfile b = new MetaProfile();
            b.AddRelic("SoulPurse");

            RelicDropResult legacy = RelicDropRoller.Roll(db, a, "Knight", 1, 1, 0, true, new ScriptedRng(0f, 0.5f, 0.5f).Next);
            RelicDropResult withShards = RelicDropRoller.Roll(db, b, "Knight", 1, 1, 0, true, new ScriptedRng(0f, 0.5f, 0.5f).Next, Growth());

            Assert.IsTrue(withShards.Acquired);
            Assert.AreEqual(legacy.RelicId, withShards.RelicId);
        }

        [Test]
        public void 잔해_3개는_각성석_1개로_바뀌고_모자라면_거부된다()
        {
            GrowthDatabase growth = Growth();
            MetaProfile p = new MetaProfile();
            p.AddCurrency(ECurrency.RelicShard, 4);

            EExchangeResult first = CurrencyExchange.Exchange(p, growth, ECurrency.AwakenStone, c => true);
            EExchangeResult second = CurrencyExchange.Exchange(p, growth, ECurrency.AwakenStone, c => true);

            Assert.AreEqual(EExchangeResult.Ok, first);
            Assert.AreEqual(EExchangeResult.Insufficient, second);
            Assert.AreEqual(1, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(1, p.GetCurrency(ECurrency.AwakenStone));
        }
    }
}
