using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup 2탭 필터 경계 (monster-species-enhancement §5) — 빈 config·글로벌만·종족만·null 가드·순서·불량 def.
    //# 기존 ShopPopupTabFilterTests(정상 필터 2 + 종족셀 필드 1)와 비중복.
    public class ShopPopupTabFilterEdgeTests
    {
        private readonly List<MetaConfig> _configs = new List<MetaConfig>();

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

        private static ShopItemDef Stat(string id)
            => new ShopItemDef { Id = id, EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, MaxLevel = 5 };

        private static ShopItemDef Spawner(string id)
            => new ShopItemDef { Id = id, EffectKind = EShopEffectKind.SpawnerPeriod, MaxLevel = 5 };

        private static ShopItemDef Species(string id, EMonster s)
            => new ShopItemDef { Id = id, EffectKind = EShopEffectKind.MonsterSpecies, Species = s, MaxLevel = 5 };

        [Test]
        public void 빈_config면_두_탭_모두_빈_목록이다()
        {
            MetaConfig cfg = Make();
            Assert.AreEqual(0, ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Stat).Count);
            Assert.AreEqual(0, ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Species).Count);
        }

        [Test]
        public void 글로벌항목만_있으면_몬스터탭은_비고_스탯탭은_전부다()
        {
            MetaConfig cfg = Make(Stat("MonsterHpUp"), Spawner("SpawnFaster"));

            List<ShopItemCellData> species = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Species);
            List<ShopItemCellData> stat = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Stat);

            Assert.AreEqual(0, species.Count);
            CollectionAssert.AreEquivalent(new[] { "MonsterHpUp", "SpawnFaster" }, stat.ConvertAll(c => c.Id));
        }

        [Test]
        public void 종족항목만_있으면_스탯탭은_비고_몬스터탭은_전부다()
        {
            MetaConfig cfg = Make(Species("Enhance_Wisp", EMonster.Wisp), Species("Enhance_Reaper", EMonster.Reaper));

            List<ShopItemCellData> stat = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Stat);
            List<ShopItemCellData> species = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Species);

            Assert.AreEqual(0, stat.Count);
            CollectionAssert.AreEquivalent(new[] { "Enhance_Wisp", "Enhance_Reaper" }, species.ConvertAll(c => c.Id));
        }

        //# 몬스터 탭은 등록(=enum) 순서를 보존 — §7 표시 순서 Wisp→Phantom.
        [Test]
        public void 몬스터탭은_등록순서_enum순서를_보존한다()
        {
            MetaConfig cfg = Make(
                Stat("MonsterHpUp"),
                Species("Enhance_Wisp", EMonster.Wisp),
                Species("Enhance_Wraith", EMonster.Wraith),
                Species("Enhance_Reaper", EMonster.Reaper),
                Species("Enhance_Hex", EMonster.Hex),
                Species("Enhance_Plague", EMonster.Plague),
                Species("Enhance_Phantom", EMonster.Phantom));

            List<ShopItemCellData> species = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Species);

            Assert.AreEqual(
                new List<string> { "Enhance_Wisp", "Enhance_Wraith", "Enhance_Reaper", "Enhance_Hex", "Enhance_Plague", "Enhance_Phantom" },
                species.ConvertAll(c => c.Id));
        }

        //# 종족 셀의 Species 가 각 항목의 Species 로 정확히 매핑된다(아이콘 resolver 입력 무결성).
        [Test]
        public void 종족셀_Species가_항목별로_정확히_매핑된다()
        {
            MetaConfig cfg = Make(
                Species("Enhance_Plague", EMonster.Plague),
                Species("Enhance_Phantom", EMonster.Phantom));

            List<ShopItemCellData> species = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Species);

            ShopItemCellData plague = species.Find(c => c.Id == "Enhance_Plague");
            ShopItemCellData phantom = species.Find(c => c.Id == "Enhance_Phantom");
            Assert.AreEqual(EMonster.Plague, plague.Species);
            Assert.AreEqual(EMonster.Phantom, phantom.Species);
        }

        //# 글로벌 셀은 Species=null (아이콘/발광 프레임 숨김 트리거).
        [Test]
        public void 글로벌셀은_Species가_null이다()
        {
            MetaConfig cfg = Make(Stat("MonsterHpUp"), Spawner("SpawnFaster"));

            List<ShopItemCellData> stat = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Stat);

            foreach (ShopItemCellData cell in stat)
            {
                Assert.IsNull(cell.Species, $"{cell.Id} 글로벌 Species null");
            }
        }

        [Test]
        public void null_profile나_config면_빈_목록이다()
        {
            MetaConfig cfg = Make(Species("Enhance_Wisp", EMonster.Wisp));
            Assert.AreEqual(0, ShopPopup.BuildCellData(null, cfg, ShopPopup.ShopTab.Species).Count);
            Assert.AreEqual(0, ShopPopup.BuildCellData(new MetaProfile(), null, ShopPopup.ShopTab.Species).Count);
        }

        //# 불량 def(null·빈 Id)는 건너뛰고 정상 항목만 남긴다.
        [Test]
        public void null이나_빈Id_항목은_건너뛴다()
        {
            MetaConfig cfg = Make(
                null,
                new ShopItemDef { Id = "", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, MaxLevel = 5 },
                Species("Enhance_Reaper", EMonster.Reaper));

            List<ShopItemCellData> species = ShopPopup.BuildCellData(new MetaProfile(), cfg, ShopPopup.ShopTab.Species);

            CollectionAssert.AreEquivalent(new[] { "Enhance_Reaper" }, species.ConvertAll(c => c.Id));
        }
    }
}
