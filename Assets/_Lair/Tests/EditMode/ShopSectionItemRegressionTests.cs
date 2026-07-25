using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 헤더 인터리브 후에도 Item 행의 기존 필드(가격/레벨/IsMax/CanBuy)가 유지됨을 고정 (회귀, 기획서 §5·7).
    //# ShopPopupCellDataTests 는 단일 항목 config 로 가격 공식을 못박는다.
    //# 여기서는 헤더가 섞인 다중 섹션 리스트에서 각 항목이 올바른 위치·값으로 살아남는지(carry-through)를 본다 (중복 회피).
    public class ShopSectionItemRegressionTests
    {
        private readonly List<MetaConfig> _configs = new List<MetaConfig>();

        //# PriceGrowth=2, 정수 BasePrice → floor(base·2^level) 이 정확(부동소수 floor 오차 없음).
        private static ShopItemDef Stat(string id, int basePrice)
            => new ShopItemDef
            {
                Id = id, DisplayName = id, EffectKind = EShopEffectKind.MonsterStat,
                StatKind = EMonsterStatKind.Hp, MaxLevel = 5, BasePrice = basePrice, PriceGrowth = 2f,
            };

        private static ShopItemDef Species(string id, EMonster s, int basePrice)
            => new ShopItemDef
            {
                Id = id, DisplayName = id, EffectKind = EShopEffectKind.MonsterSpecies,
                Species = s, MaxLevel = 5, BasePrice = basePrice, PriceGrowth = 2f,
            };

        private MetaConfig Make(params ShopItemDef[] items)
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>(items);
            _configs.Add(cfg);
            return cfg;
        }

        [TearDown]
        public void 정리()
        {
            foreach (MetaConfig cfg in _configs)
            {
                if (cfg != null)
                    Object.DestroyImmediate(cfg);
            }
            _configs.Clear();
        }

        private static ShopItemCellData FindItem(List<ShopItemCellData> list, string id)
            => list.Find(c => c.RowKind == ShopPopup.ShopRowKind.Item && c.Id == id);

        //# 다중 섹션(헤더 2개 삽입) 리스트에서도 스탯 항목이 정확한 가격/레벨을 담는다.
        [Test]
        public void 헤더가_섞인_리스트에서_스탯항목이_정확한_가격과_레벨을_담는다()
        {
            MetaProfile profile = new MetaProfile { Souls = 99999 };
            profile.SetShopLevel("MonsterHpUp", 2);   //# floor(100·2^2)=400

            List<ShopItemCellData> list = ShopPopup.BuildCellData(
                profile, Make(Stat("MonsterHpUp", 100), Species("Enhance_Wisp", EMonster.Wisp, 50)));

            ShopItemCellData hp = FindItem(list, "MonsterHpUp");
            Assert.IsNotNull(hp);
            Assert.AreEqual(400, hp.Price);
            Assert.AreEqual("Lv 2/5", hp.LevelText);
            Assert.IsFalse(hp.IsMax);
            Assert.IsTrue(hp.CanBuy);   //# 99999 >= 400
        }

        //# 종족 항목도 헤더 뒤에서 정확한 가격/레벨을 담는다.
        [Test]
        public void 헤더가_섞인_리스트에서_종족항목이_정확한_가격과_레벨을_담는다()
        {
            MetaProfile profile = new MetaProfile { Souls = 99999 };
            profile.SetShopLevel("Enhance_Wisp", 3);   //# floor(50·2^3)=400

            List<ShopItemCellData> list = ShopPopup.BuildCellData(
                profile, Make(Stat("MonsterHpUp", 100), Species("Enhance_Wisp", EMonster.Wisp, 50)));

            ShopItemCellData wisp = FindItem(list, "Enhance_Wisp");
            Assert.IsNotNull(wisp);
            Assert.AreEqual(400, wisp.Price);
            Assert.AreEqual("Lv 3/5", wisp.LevelText);
            Assert.AreEqual(EMonster.Wisp, wisp.Species);
        }

        //# 각 Item 행이 자기 def 기준값을 유지 — 헤더 삽입/타 항목 간섭 없음(항목별 격리 회귀).
        [Test]
        public void 모든_항목행이_자기_def의_가격레벨을_교차오염없이_유지한다()
        {
            MetaProfile profile = new MetaProfile { Souls = 99999 };
            profile.SetShopLevel("MonsterHpUp", 1);
            profile.SetShopLevel("Enhance_Wisp", 4);
            //# Enhance_Reaper 는 미구매(레벨 0) 유지.

            ShopItemDef hpDef = Stat("MonsterHpUp", 100);
            ShopItemDef wispDef = Species("Enhance_Wisp", EMonster.Wisp, 50);
            ShopItemDef reaperDef = Species("Enhance_Reaper", EMonster.Reaper, 70);

            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, Make(hpDef, wispDef, reaperDef));

            AssertMatchesDef(FindItem(list, "MonsterHpUp"), hpDef, profile);
            AssertMatchesDef(FindItem(list, "Enhance_Wisp"), wispDef, profile);
            AssertMatchesDef(FindItem(list, "Enhance_Reaper"), reaperDef, profile);
        }

        //# CanBuy 경계 — 소울이 가격과 정확히 같으면 구매 가능, 1 적으면 불가.
        [Test]
        public void 소울이_가격과_정확히_같으면_CanBuy참_1적으면_거짓이다()
        {
            //# level 0 → floor(200·2^0)=200.
            ShopItemDef def = Stat("MonsterHpUp", 200);

            MetaProfile exact = new MetaProfile { Souls = 200 };
            ShopItemCellData atExact = FindItem(ShopPopup.BuildCellData(exact, Make(def)), "MonsterHpUp");
            Assert.IsTrue(atExact.CanBuy, "소울==가격이면 구매 가능이어야 함");

            ShopItemDef def2 = Stat("MonsterHpUp", 200);
            MetaProfile short1 = new MetaProfile { Souls = 199 };
            ShopItemCellData atShort = FindItem(ShopPopup.BuildCellData(short1, Make(def2)), "MonsterHpUp");
            Assert.IsFalse(atShort.CanBuy, "소울<가격이면 구매 불가여야 함");
        }

        //# 헤더가 섞인 리스트에서 만렙 항목은 Price 0 · IsMax · CanBuy 거짓 (소울 충분해도).
        [Test]
        public void 헤더가_섞인_리스트에서_만렙항목은_Price0이고_구매불가다()
        {
            MetaProfile profile = new MetaProfile { Souls = 99999 };
            profile.SetShopLevel("Enhance_Wisp", 5);   //# MaxLevel 5

            List<ShopItemCellData> list = ShopPopup.BuildCellData(
                profile, Make(Stat("MonsterHpUp", 100), Species("Enhance_Wisp", EMonster.Wisp, 50)));

            ShopItemCellData wisp = FindItem(list, "Enhance_Wisp");
            Assert.IsTrue(wisp.IsMax);
            Assert.AreEqual(0, wisp.Price);
            Assert.IsFalse(wisp.CanBuy);
            Assert.AreEqual("Lv 5/5", wisp.LevelText);
        }

        //# 항목 필드가 def+profile 로부터 독립 재계산한 기대값과 일치하는지 검증(가격 공식은 ShopService SoT 공유).
        private static void AssertMatchesDef(ShopItemCellData cell, ShopItemDef def, MetaProfile profile)
        {
            Assert.IsNotNull(cell, $"{def.Id} 항목 누락");
            int level = profile.GetShopLevel(def.Id);
            bool isMax = level >= def.MaxLevel;
            int expectedPrice = isMax ? 0 : ShopService.PriceOf(def, level);

            Assert.AreEqual($"Lv {level}/{def.MaxLevel}", cell.LevelText, $"{def.Id} LevelText");
            Assert.AreEqual(expectedPrice, cell.Price, $"{def.Id} Price");
            Assert.AreEqual(isMax, cell.IsMax, $"{def.Id} IsMax");
            Assert.AreEqual(isMax == false && profile.Souls >= expectedPrice, cell.CanBuy, $"{def.Id} CanBuy");
        }
    }
}
