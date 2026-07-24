using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 종족 강화 항목 가격 곡선 회귀 (monster-species-enhancement §3.1) — BasePrice 150 / PriceGrowth 1.6.
    //# 기존 ShopServiceTests/EdgeTests(BasePrice 80 글로벌)와 비중복 — 종족 전용 150·만렙 2371 누계·차단 경로.
    public class SpeciesShopPriceTests
    {
        private MetaConfig _cfg;
        private MetaProfile _profile;
        private ShopService _shop;

        private static ShopItemDef SpeciesDef()
            => new ShopItemDef
            {
                Id = "Enhance_Wisp",
                EffectKind = EShopEffectKind.MonsterSpecies,
                Species = EMonster.Wisp,
                PerLevelMul = 1.18f,
                MaxLevel = 5,
                BasePrice = 150,
                PriceGrowth = 1.6f,
            };

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems.Add(SpeciesDef());
            _profile = new MetaProfile { Souls = 100000 };
            _shop = new ShopService(_profile, _cfg);
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        //# §3.1 표 — Lv0~4 구매가 150 / 240 / 384 / 614 / 983.
        [TestCase(0, 150)]
        [TestCase(1, 240)]
        [TestCase(2, 384)]
        [TestCase(3, 614)]
        [TestCase(4, 983)]
        public void 종족강화_레벨별_가격이_기획서_곡선과_일치한다(int level, int expectedPrice)
        {
            _profile.SetShopLevel("Enhance_Wisp", level);
            Assert.AreEqual(expectedPrice, _shop.PriceOf("Enhance_Wisp"));
        }

        //# static PriceOf 도 동일 곡선 — ShopPopup.MakeCell 과 공유하는 단일 진실.
        [Test]
        public void static_PriceOf_도_동일_곡선을_낸다()
        {
            ShopItemDef def = SpeciesDef();
            Assert.AreEqual(150, ShopService.PriceOf(def, 0));
            Assert.AreEqual(983, ShopService.PriceOf(def, 4));
        }

        [Test]
        public void 한종_만렙까지_누계는_2371소울이다()
        {
            int total = 0;
            ShopItemDef def = SpeciesDef();
            for (int lv = 0; lv < 5; lv++)
            {
                total += ShopService.PriceOf(def, lv);
            }
            Assert.AreEqual(2371, total);
        }

        [Test]
        public void Lv5_만렙이면_구매가_차단된다()
        {
            _profile.SetShopLevel("Enhance_Wisp", 5);
            Assert.IsFalse(_shop.CanBuy("Enhance_Wisp"));
            Assert.IsFalse(_shop.Buy("Enhance_Wisp"));
        }

        [Test]
        public void 소울이_150_모자라_1이면_첫레벨_구매불가다()
        {
            _profile.Souls = 149;
            Assert.IsFalse(_shop.CanBuy("Enhance_Wisp"));
            Assert.IsFalse(_shop.Buy("Enhance_Wisp"));
            Assert.AreEqual(149, _profile.Souls);
        }

        [Test]
        public void 소울이_정확히_150이면_첫레벨_구매되고_레벨1이_된다()
        {
            _profile.Souls = 150;
            Assert.IsTrue(_shop.Buy("Enhance_Wisp"));
            Assert.AreEqual(0, _profile.Souls);
            Assert.AreEqual(1, _profile.GetShopLevel("Enhance_Wisp"));
        }

        //# 여섯 종족 통일 곡선 — 어느 종족 항목이든 같은 150·1.6 곡선(효율 정답 종족 없음, §2.2/§3.1).
        [Test]
        public void 여섯종족_모두_동일_가격곡선을_공유한다()
        {
            foreach (EMonster species in System.Enum.GetValues(typeof(EMonster)))
            {
                ShopItemDef def = new ShopItemDef
                {
                    Id = "Enhance_" + species,
                    EffectKind = EShopEffectKind.MonsterSpecies,
                    Species = species,
                    BasePrice = 150,
                    PriceGrowth = 1.6f,
                    MaxLevel = 5,
                };
                Assert.AreEqual(150, ShopService.PriceOf(def, 0), $"{species} Lv0");
                Assert.AreEqual(983, ShopService.PriceOf(def, 4), $"{species} Lv4");
            }
        }
    }
}
