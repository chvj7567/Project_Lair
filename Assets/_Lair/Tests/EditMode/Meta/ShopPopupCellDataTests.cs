using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup.BuildCellData — 셀 표시 문자열/가격/구매 가능 가공 검증 (BuildModalCardCellTests 패턴).
    public class ShopPopupCellDataTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems.Add(new ShopItemDef
            {
                Id = "MonsterHpUp",
                DisplayName = "강골 군세",
                Description = "모든 몬스터 HP +2%/Lv",
                BasePrice = 80,
                PriceGrowth = 1.6f,
                MaxLevel = 5,
            });
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 레벨_가격_구매가능이_올바로_가공된다()
        {
            MetaProfile profile = new MetaProfile { Souls = 200 };
            profile.SetShopLevel("MonsterHpUp", 2);

            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, _cfg);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("강골 군세", list[0].DisplayName);
            Assert.AreEqual("Lv 2/5", list[0].LevelText);
            Assert.AreEqual(204, list[0].Price);   //# floor(80×1.6^2)
            Assert.IsFalse(list[0].IsMax);
            Assert.IsFalse(list[0].CanBuy);        //# 200 < 204 — 소울 부족
        }

        [Test]
        public void 만렙이면_IsMax가_참이고_구매불가다()
        {
            MetaProfile profile = new MetaProfile { Souls = 99999 };
            profile.SetShopLevel("MonsterHpUp", 5);

            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, _cfg);
            Assert.IsTrue(list[0].IsMax);
            Assert.IsFalse(list[0].CanBuy);
            Assert.AreEqual("Lv 5/5", list[0].LevelText);
        }
    }
}
