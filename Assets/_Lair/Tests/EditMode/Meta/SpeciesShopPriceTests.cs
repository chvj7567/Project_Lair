using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 체급 티어 강화 회귀 (dungeon-formation-free §3 '상점에서 강화 티어 구매') — 기본→T1→T2→T3 = 390 / 998 / 983 소울, 저장 대표값 0/2/4/5.
    public class SpeciesShopPriceTests
    {
        private const string ItemId = "Enhance_Wisp";

        private MetaConfig _cfg;
        private MetaProfile _profile;
        private ShopService _shop;

        private static ShopItemDef SpeciesDef(EMonster species)
            => new ShopItemDef
            {
                Id = "Enhance_" + species,
                EffectKind = EShopEffectKind.MonsterSpecies,
                Species = species,
                PerLevelMul = 1.18f,
                MaxLevel = 5,
                BasePrice = 150,
                PriceGrowth = 1.6f,
            };

        private static EnhanceTierTable Tiers()
            => EnhanceTierTable.Build(new[]
            {
                new EnhanceTierRow { tier = 1, levelEquivalent = 2, soulCost = 390 },
                new EnhanceTierRow { tier = 2, levelEquivalent = 4, soulCost = 998 },
                new EnhanceTierRow { tier = 3, levelEquivalent = 5, soulCost = 983 },
            });

        [SetUp]
        public void 준비()
        {
            _cfg = new MetaConfig { EnhanceTiers = Tiers() };
            _cfg.ShopItems.Add(SpeciesDef(EMonster.Wisp));
            _profile = new MetaProfile { Souls = 100000 };
            _shop = new ShopService(_profile, _cfg);
        }

        //# 현재 저장값 → 다음 티어 구매가. 구버전 Lv1/Lv3 도 같은 티어(T1/T2)의 다음 단계 가격으로 읽는다.
        [TestCase(0, 390)]
        [TestCase(1, 998)]
        [TestCase(2, 998)]
        [TestCase(3, 983)]
        [TestCase(4, 983)]
        public void 티어강화_현재_저장값별_다음_티어_가격이_기획서와_일치한다(int level, int expectedPrice)
        {
            _profile.SetShopLevel(ItemId, level);
            Assert.AreEqual(expectedPrice, _shop.PriceOf(ItemId));
        }

        [Test]
        public void T3_최대_티어면_가격이_없고_구매가_차단된다()
        {
            _profile.SetShopLevel(ItemId, 5);
            Assert.AreEqual(-1, _shop.PriceOf(ItemId));
            Assert.IsFalse(_shop.CanBuy(ItemId));
            Assert.IsFalse(_shop.Buy(ItemId));
            Assert.AreEqual(100000, _profile.Souls);
        }

        [Test]
        public void 기본에서_순차_구매하면_대표값_2_4_5와_누계_2371소울이_든다()
        {
            int[] expectedLevels = { 2, 4, 5 };
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(_shop.Buy(ItemId), $"{i + 1}번째 구매");
                Assert.AreEqual(expectedLevels[i], _profile.GetShopLevel(ItemId));
            }
            Assert.AreEqual(100000 - 2371, _profile.Souls);
            Assert.IsFalse(_shop.Buy(ItemId));
        }

        [Test]
        public void 소울이_389면_T1_구매불가이고_부족분이_1이다()
        {
            _profile.Souls = 389;
            Assert.IsFalse(_shop.CanBuy(ItemId));
            Assert.IsFalse(_shop.Buy(ItemId));
            Assert.AreEqual(389, _profile.Souls);
            Assert.AreEqual(0, _profile.GetShopLevel(ItemId));
            Assert.IsTrue(_shop.TryGetTierOffer(ItemId, out ShopService.TierOffer offer));
            Assert.AreEqual(1, offer.MissingSouls);
            Assert.IsFalse(offer.CanBuy);
        }

        [Test]
        public void 소울이_정확히_390이면_T1_구매되고_대표값_2가_된다()
        {
            _profile.Souls = 390;
            Assert.IsTrue(_shop.Buy(ItemId));
            Assert.AreEqual(0, _profile.Souls);
            Assert.AreEqual(2, _profile.GetShopLevel(ItemId));
        }

        [Test]
        public void 티어_단계_데이터가_없으면_구매가_비활성이고_소울을_쓰지_않는다()
        {
            _cfg.EnhanceTiers = null;
            Assert.IsFalse(_shop.CanBuy(ItemId));
            Assert.IsFalse(_shop.Buy(ItemId));
            Assert.AreEqual(100000, _profile.Souls);
        }

        //# 여섯 종족 통일 — 어느 종족 항목이든 같은 390 첫 가격과 1.18^2 배율 예고(효율 정답 종족 없음).
        [Test]
        public void 여섯종족_모두_동일_티어_단계를_공유한다()
        {
            foreach (EMonster species in MonsterGrades.BaseMonsters)
            {
                MetaConfig cfg = new MetaConfig { EnhanceTiers = Tiers() };
                cfg.ShopItems.Add(SpeciesDef(species));
                ShopService shop = new ShopService(new MetaProfile { Souls = 100000 }, cfg);
                string id = "Enhance_" + species;
                Assert.IsTrue(shop.TryGetTierOffer(id, out ShopService.TierOffer offer), $"{species}");
                Assert.AreEqual(390, offer.SoulCost, $"{species} 기본→T1");
                Assert.AreEqual(2, offer.NextLevel, $"{species} 대표값");
                Assert.AreEqual(1.18f * 1.18f, offer.NextMultiplier, 0.0001f, $"{species} 배율");
            }
        }
    }
}
