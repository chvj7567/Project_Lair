using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup 단일 스크롤 통합 리스트 경계 — 빈 config·빈 섹션 헤더 제외(§5)·순서·Species 매핑·null 가드·불량 def.
    //# 구 ShopPopupTabFilterEdgeTests(2탭 필터)를 통합 리스트 검증으로 재작성. 정상 케이스는 ShopSectionListTests.
    public class ShopSectionListEdgeTests
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

        private static List<string> HeaderTexts(List<ShopItemCellData> list)
        {
            List<string> headers = new List<string>();
            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.SectionHeader)
                    headers.Add(cell.HeaderText);
            }
            return headers;
        }

        [Test]
        public void 빈_config면_헤더도_항목도_없는_빈_목록이다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Make());
            Assert.AreEqual(0, list.Count);
        }

        //# §5 빈 섹션 제외 — 스탯 항목만 있으면 몬스터 헤더가 안 나온다.
        [Test]
        public void 스탯항목만_있으면_몬스터_섹션_헤더는_제외된다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(
                new MetaProfile(), Make(Stat("MonsterHpUp"), Spawner("SpawnFaster")));

            CollectionAssert.AreEqual(new[] { "스탯 강화" }, HeaderTexts(list));
            //# [스탯헤더, MonsterHpUp, SpawnFaster]
            Assert.AreEqual(3, list.Count);
        }

        //# §5 빈 섹션 제외 — 종족 항목만 있으면 스탯 헤더가 안 나온다.
        [Test]
        public void 종족항목만_있으면_스탯_섹션_헤더는_제외된다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(
                new MetaProfile(), Make(Species("Enhance_Wisp", EMonster.Wisp), Species("Enhance_Reaper", EMonster.Reaper)));

            CollectionAssert.AreEqual(new[] { "몬스터 강화" }, HeaderTexts(list));
            //# [몬스터헤더, Enhance_Wisp, Enhance_Reaper]
            Assert.AreEqual(3, list.Count);
        }

        //# 종족 항목은 등록(config) 순서를 보존 — §7 표시 순서 Wisp→Phantom.
        [Test]
        public void 종족항목은_등록순서를_보존한다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Make(
                Stat("MonsterHpUp"),
                Species("Enhance_Wisp", EMonster.Wisp),
                Species("Enhance_Wraith", EMonster.Wraith),
                Species("Enhance_Reaper", EMonster.Reaper),
                Species("Enhance_Hex", EMonster.Hex),
                Species("Enhance_Plague", EMonster.Plague),
                Species("Enhance_Phantom", EMonster.Phantom)));

            List<string> speciesIds = new List<string>();
            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.Item && cell.Species.HasValue)
                    speciesIds.Add(cell.Id);
            }
            Assert.AreEqual(
                new List<string> { "Enhance_Wisp", "Enhance_Wraith", "Enhance_Reaper", "Enhance_Hex", "Enhance_Plague", "Enhance_Phantom" },
                speciesIds);
        }

        //# 종족 셀의 Species 가 각 항목의 Species 로 정확히 매핑된다(아이콘 resolver 입력 무결성).
        [Test]
        public void 종족셀_Species가_항목별로_정확히_매핑된다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Make(
                Species("Enhance_Plague", EMonster.Plague),
                Species("Enhance_Phantom", EMonster.Phantom)));

            ShopItemCellData plague = list.Find(c => c.Id == "Enhance_Plague");
            ShopItemCellData phantom = list.Find(c => c.Id == "Enhance_Phantom");
            Assert.AreEqual(EMonster.Plague, plague.Species);
            Assert.AreEqual(EMonster.Phantom, phantom.Species);
        }

        //# 글로벌 항목 셀은 Species=null (아이콘/발광 프레임 숨김 트리거). 헤더 행은 검사 제외.
        [Test]
        public void 글로벌항목셀은_Species가_null이다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(
                new MetaProfile(), Make(Stat("MonsterHpUp"), Spawner("SpawnFaster")));

            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.Item)
                    Assert.IsNull(cell.Species, $"{cell.Id} 글로벌 Species null");
            }
        }

        [Test]
        public void null_profile나_config면_빈_목록이다()
        {
            MetaConfig cfg = Make(Species("Enhance_Wisp", EMonster.Wisp));
            Assert.AreEqual(0, ShopPopup.BuildCellData(null, cfg).Count);
            Assert.AreEqual(0, ShopPopup.BuildCellData(new MetaProfile(), null).Count);
        }

        //# 불량 def(null·빈 Id)는 건너뛰고 정상 항목만 남긴다.
        [Test]
        public void null이나_빈Id_항목은_건너뛴다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), Make(
                null,
                new ShopItemDef { Id = "", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, MaxLevel = 5 },
                Species("Enhance_Reaper", EMonster.Reaper)));

            List<string> itemIds = new List<string>();
            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.Item)
                    itemIds.Add(cell.Id);
            }
            CollectionAssert.AreEqual(new[] { "Enhance_Reaper" }, itemIds);
        }
    }
}
