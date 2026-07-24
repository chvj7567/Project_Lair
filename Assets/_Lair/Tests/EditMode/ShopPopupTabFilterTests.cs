using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup 2탭 필터 — 스탯 탭은 글로벌 항목만, 몬스터 탭은 종족 항목만 (monster-species-enhancement §5).
    public class ShopPopupTabFilterTests
    {
        private static MetaConfig Cfg()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, MaxLevel = 5 },
                new ShopItemDef { Id = "SpawnFaster", EffectKind = EShopEffectKind.SpawnerPeriod, MaxLevel = 5 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, MaxLevel = 5 },
            };
            return cfg;
        }

        [Test]
        public void 스탯탭은_글로벌항목만_보인다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg(), ShopPopup.ShopTab.Stat);
            CollectionAssert.AreEquivalent(new[] { "MonsterHpUp", "SpawnFaster" }, list.ConvertAll(c => c.Id));
        }

        [Test]
        public void 몬스터탭은_종족항목만_보인다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg(), ShopPopup.ShopTab.Species);
            CollectionAssert.AreEquivalent(new[] { "Enhance_Wisp" }, list.ConvertAll(c => c.Id));
        }

        //# 엣지 — 종족 항목 셀에 Species/Level/MaxLevel 이 채워진다 (Icon 은 Rebuild 주입이라 여기선 null).
        [Test]
        public void 종족셀은_Species와_레벨필드가_채워진다()
        {
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 2);

            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, Cfg(), ShopPopup.ShopTab.Species);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(EMonster.Wisp, list[0].Species);
            Assert.AreEqual(2, list[0].Level);
            Assert.AreEqual(5, list[0].MaxLevel);
            Assert.IsNull(list[0].Icon);
        }
    }
}
