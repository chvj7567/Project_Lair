using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 가격 곡선 = floor(BasePrice × 1.6^현재레벨) — 기획서 §3.2 / §3.5.
    public class ShopServiceTests
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
        public void 가격은_레벨에_따라_점증한다()
        {
            Assert.AreEqual(80, _shop.PriceOf("MonsterHpUp"));    //# Lv0→1
            _profile.SetShopLevel("MonsterHpUp", 1);
            Assert.AreEqual(128, _shop.PriceOf("MonsterHpUp"));   //# floor(80×1.6)
        }

        [Test]
        public void 구매하면_소울이_차감되고_레벨이_오른다()
        {
            Assert.IsTrue(_shop.Buy("MonsterHpUp"));
            Assert.AreEqual(20, _profile.Souls);
            Assert.AreEqual(1, _profile.GetShopLevel("MonsterHpUp"));
        }

        [Test]
        public void 소울_부족이면_구매_실패한다()
        {
            _profile.Souls = 10;
            Assert.IsFalse(_shop.Buy("MonsterHpUp"));
            Assert.AreEqual(10, _profile.Souls);
        }

        [Test]
        public void 만렙이면_구매_불가다()
        {
            _profile.Souls = 99999;
            _profile.SetShopLevel("MonsterHpUp", 5);
            Assert.IsFalse(_shop.CanBuy("MonsterHpUp"));
        }
    }
}
