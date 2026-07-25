using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup 단일 스크롤 통합 리스트 — 탭 제거 후 [스탯 헤더 + 스탯 항목 + 몬스터 헤더 + 종족 항목] 구성 검증 (기획서 §1·7).
    //# 구 ShopPopupTabFilterTests(2탭 필터)를 통합 리스트 검증으로 재작성.
    public class ShopSectionListTests
    {
        private readonly List<MetaConfig> _configs = new List<MetaConfig>();

        private MetaConfig Cfg()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, MaxLevel = 5 },
                new ShopItemDef { Id = "SpawnFaster", EffectKind = EShopEffectKind.SpawnerPeriod, MaxLevel = 5 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, MaxLevel = 5 },
            };
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

        [Test]
        public void 통합리스트는_스탯헤더_스탯항목_몬스터헤더_종족항목_순서다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg());

            //# [스탯헤더, MonsterHpUp, SpawnFaster, 몬스터헤더, Enhance_Wisp]
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual(ShopPopup.ShopRowKind.SectionHeader, list[0].RowKind);
            Assert.AreEqual("MonsterHpUp", list[1].Id);
            Assert.AreEqual("SpawnFaster", list[2].Id);
            Assert.AreEqual(ShopPopup.ShopRowKind.SectionHeader, list[3].RowKind);
            Assert.AreEqual("Enhance_Wisp", list[4].Id);
        }

        [Test]
        public void 헤더행은_섹션문구를_담는다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg());
            Assert.AreEqual("스탯 강화", list[0].HeaderText);
            Assert.AreEqual("몬스터 강화", list[3].HeaderText);
        }

        [Test]
        public void 항목행은_Item_RowKind다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Cfg());
            Assert.AreEqual(ShopPopup.ShopRowKind.Item, list[1].RowKind);
            Assert.AreEqual(ShopPopup.ShopRowKind.Item, list[4].RowKind);
        }

        //# 종족 셀 필드(Species/Level/MaxLevel)는 유지 — Icon 은 Rebuild 주입이라 여기선 null.
        [Test]
        public void 종족셀은_Species와_레벨필드가_채워진다()
        {
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 2);

            List<ShopItemCellData> list = ShopPopup.BuildCellData(profile, Cfg());
            ShopItemCellData wisp = list.Find(c => c.Id == "Enhance_Wisp");

            Assert.IsNotNull(wisp);
            Assert.AreEqual(EMonster.Wisp, wisp.Species);
            Assert.AreEqual(2, wisp.Level);
            Assert.AreEqual(5, wisp.MaxLevel);
            Assert.IsNull(wisp.Icon);
        }

        [Test]
        public void null_가드는_빈리스트다()
        {
            Assert.IsEmpty(ShopPopup.BuildCellData(null, Cfg()));
            Assert.IsEmpty(ShopPopup.BuildCellData(new MetaProfile(), null));
        }
    }
}
