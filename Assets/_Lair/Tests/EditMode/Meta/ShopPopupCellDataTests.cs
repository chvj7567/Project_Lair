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

            //# 통합 리스트 = [스탯 섹션 헤더, MonsterHpUp 항목] — 항목은 헤더 다음 list[1] (기획서 §1·7).
            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, _cfg);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(ShopPopup.ShopRowKind.SectionHeader, list[0].RowKind);
            ShopItemCellData item = list[1];
            Assert.AreEqual("강골 군세", item.DisplayName);
            Assert.AreEqual("Lv 2/5", item.LevelText);
            Assert.AreEqual(204, item.Price);      //# floor(80×1.6^2)
            Assert.IsFalse(item.IsMax);
            Assert.IsFalse(item.CanBuy);           //# 200 < 204 — 소울 부족
        }

        [Test]
        public void 만렙이면_IsMax가_참이고_구매불가다()
        {
            MetaProfile profile = new MetaProfile { Souls = 99999 };
            profile.SetShopLevel("MonsterHpUp", 5);

            //# 항목은 스탯 헤더 다음 list[1].
            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, _cfg);
            ShopItemCellData item = list[1];
            Assert.IsTrue(item.IsMax);
            Assert.IsFalse(item.CanBuy);
            Assert.AreEqual("Lv 5/5", item.LevelText);
        }
    }
}
