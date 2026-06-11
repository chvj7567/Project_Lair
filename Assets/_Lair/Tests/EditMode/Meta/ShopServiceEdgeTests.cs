using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopService 엣지 — 미존재 품목/극단 가격 설정/잔액 경계 (기존 ShopServiceTests 의 정상 케이스와 비중복).
    public class ShopServiceEdgeTests
    {
        private MetaConfig _cfg;
        private MetaProfile _profile;
        private ShopService _shop;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp", BasePrice = 80, PriceGrowth = 1.6f, MaxLevel = 5 });
            _profile = new MetaProfile { Souls = 100 };
            _shop = new ShopService(_profile, _cfg);
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 존재하지_않는_품목은_가격_마이너스1이다()
        {
            Assert.AreEqual(-1, _shop.PriceOf("없는품목"));
        }

        [Test]
        public void 존재하지_않는_품목은_구매불가이며_잔액이_변하지_않는다()
        {
            Assert.IsFalse(_shop.CanBuy("없는품목"));
            Assert.IsFalse(_shop.Buy("없는품목"));
            Assert.AreEqual(100, _profile.Souls);
            Assert.AreEqual(0, _profile.ShopLevels.Count);
        }

        [Test]
        public void MaxLevel_0_품목은_레벨0에서도_구매불가다()
        {
            _cfg.ShopItems.Add(new ShopItemDef { Id = "Sealed", BasePrice = 10, PriceGrowth = 1.6f, MaxLevel = 0 });
            Assert.IsFalse(_shop.CanBuy("Sealed"));
            Assert.IsFalse(_shop.Buy("Sealed"));
        }

        [Test]
        public void BasePrice_0_품목은_소울0으로도_구매된다()
        {
            _cfg.ShopItems.Add(new ShopItemDef { Id = "Free", BasePrice = 0, PriceGrowth = 1.6f, MaxLevel = 5 });
            _profile.Souls = 0;
            Assert.AreEqual(0, _shop.PriceOf("Free"));
            Assert.IsTrue(_shop.Buy("Free"));
            Assert.AreEqual(0, _profile.Souls);
            Assert.AreEqual(1, _profile.GetShopLevel("Free"));
        }

        [Test]
        public void 잔액이_정확히_가격과_같으면_구매되고_0이_남는다()
        {
            //# 경계 — CanBuy 는 >= 판정 (기획서 §3.5 첫 구매 80 = FirstRun 직후 최저 보장선).
            _profile.Souls = 80;
            Assert.IsTrue(_shop.CanBuy("MonsterHpUp"));
            Assert.IsTrue(_shop.Buy("MonsterHpUp"));
            Assert.AreEqual(0, _profile.Souls);
        }

        [Test]
        public void 잔액이_가격보다_1_모자라면_구매불가다()
        {
            _profile.Souls = 79;
            Assert.IsFalse(_shop.CanBuy("MonsterHpUp"));
        }

        [Test]
        public void PriceGrowth_1이면_가격이_전_레벨_동일하다()
        {
            _cfg.ShopItems.Add(new ShopItemDef { Id = "Flat", BasePrice = 100, PriceGrowth = 1f, MaxLevel = 5 });
            Assert.AreEqual(100, _shop.PriceOf("Flat"));
            _profile.SetShopLevel("Flat", 4);
            Assert.AreEqual(100, _shop.PriceOf("Flat"));
        }
    }
}
