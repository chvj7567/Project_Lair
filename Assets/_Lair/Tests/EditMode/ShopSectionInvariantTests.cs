using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup.BuildCellData 통합 리스트의 구조 불변식 (기획서 §1·5·7).
    //# 기존 ShopSectionList(Edge)Tests 는 고정 config 의 개수·헤더문구를 본다.
    //# 여기서는 임의 both-sections config 에서 헤더 위치·섹션 멤버십·헤더/항목 경계의 일반 불변식을 검증 (중복 회피).
    public class ShopSectionInvariantTests
    {
        private readonly List<MetaConfig> _configs = new List<MetaConfig>();

        private static ShopItemDef Stat(string id)
            => new ShopItemDef { Id = id, DisplayName = id, EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, MaxLevel = 5 };

        private static ShopItemDef Spawner(string id)
            => new ShopItemDef { Id = id, DisplayName = id, EffectKind = EShopEffectKind.SpawnerPeriod, MaxLevel = 5 };

        private static ShopItemDef Species(string id, EMonster s)
            => new ShopItemDef { Id = id, DisplayName = id, EffectKind = EShopEffectKind.MonsterSpecies, Species = s, MaxLevel = 5 };

        //# 두 섹션이 모두 채워진 넉넉한 config (스탯 3 + 종족 3).
        private MetaConfig BothSections()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>
            {
                Stat("MonsterHpUp"),
                Stat("MonsterDmgUp"),
                Spawner("SpawnFaster"),
                Species("Enhance_Wisp", EMonster.Wisp),
                Species("Enhance_Reaper", EMonster.Reaper),
                Species("Enhance_Phantom", EMonster.Phantom),
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

        //# 헤더 문구 → 그 섹션 항목이 가져야 할 Species 유무 규약. 스탯="스탯 강화"(Species null), 몬스터="몬스터 강화"(Species 있음).
        private static bool ExpectsSpecies(string headerText) => headerText == "몬스터 강화";

        //# 통합 리스트를 훑어 각 헤더 다음부터 다음 헤더 전까지 항목들이 그 섹션 규약(Species 유무)을 지키는지 확인.
        [Test]
        public void 헤더_다음부터_다음헤더_전까지_같은섹션_항목만_온다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), BothSections());

            bool sawHeader = false;
            bool expectSpecies = false;
            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.SectionHeader)
                {
                    sawHeader = true;
                    expectSpecies = ExpectsSpecies(cell.HeaderText);
                    continue;
                }

                //# 첫 항목보다 헤더가 반드시 먼저 나온다 (고아 항목 없음).
                Assert.IsTrue(sawHeader, $"{cell.Id} 항목이 헤더 없이 등장");
                Assert.AreEqual(expectSpecies, cell.Species.HasValue,
                    $"{cell.Id} 가 현재 섹션 규약(Species={expectSpecies})과 불일치");
            }
        }

        //# 스탯 섹션 항목은 전부 글로벌(Species null), 몬스터 섹션 항목은 전부 종족(Species 있음).
        [Test]
        public void 스탯섹션은_전부_글로벌이고_몬스터섹션은_전부_종족이다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), BothSections());

            List<ShopItemCellData> statItems = SectionItems(list, "스탯 강화");
            List<ShopItemCellData> speciesItems = SectionItems(list, "몬스터 강화");

            Assert.AreEqual(3, statItems.Count);
            Assert.AreEqual(3, speciesItems.Count);
            foreach (ShopItemCellData cell in statItems)
                Assert.IsFalse(cell.Species.HasValue, $"{cell.Id} 스탯 섹션인데 Species 존재");
            foreach (ShopItemCellData cell in speciesItems)
                Assert.IsTrue(cell.Species.HasValue, $"{cell.Id} 몬스터 섹션인데 Species 없음");
        }

        //# 각 헤더는 그 섹션의 첫 행이고 바로 뒤에 최소 1개 항목이 온다 (외로운 배너 금지 §5).
        [Test]
        public void 헤더는_직후에_최소_한_항목이_따라온다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), BothSections());

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].RowKind != ShopPopup.ShopRowKind.SectionHeader)
                    continue;
                Assert.Less(i + 1, list.Count, "헤더가 리스트 마지막에 홀로 존재");
                Assert.AreEqual(ShopPopup.ShopRowKind.Item, list[i + 1].RowKind,
                    "헤더 바로 뒤가 항목 행이 아님");
            }
        }

        //# 헤더 행은 HeaderText 만 세팅하고 항목 식별 필드(Id)는 미설정 — OnBuy 오발동 방지.
        [Test]
        public void 헤더행은_HeaderText만_있고_Id는_null이다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), BothSections());

            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind != ShopPopup.ShopRowKind.SectionHeader)
                    continue;
                Assert.IsNotEmpty(cell.HeaderText, "헤더 문구 미설정");
                Assert.IsNull(cell.Id, "헤더 행에 항목 Id 가 설정됨");
                Assert.IsFalse(cell.CanBuy, "헤더 행이 구매 가능으로 표시됨");
                Assert.IsFalse(cell.Species.HasValue, "헤더 행에 Species 가 설정됨");
            }
        }

        //# 항목 행은 HeaderText 를 갖지 않는다 (헤더로 오판 방지).
        [Test]
        public void 항목행은_HeaderText가_null이다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), BothSections());

            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind != ShopPopup.ShopRowKind.Item)
                    continue;
                Assert.IsNull(cell.HeaderText, $"{cell.Id} 항목이 HeaderText 를 가짐");
            }
        }

        //# 정확히 헤더 2개 (스탯·몬스터) 이고 등장 순서는 스탯 먼저.
        [Test]
        public void 헤더는_스탯_몬스터_순으로_정확히_2개다()
        {
            List<ShopItemCellData> list = ShopPopup.BuildCellData(new MetaProfile(), BothSections());

            List<string> headers = new List<string>();
            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.SectionHeader)
                    headers.Add(cell.HeaderText);
            }
            CollectionAssert.AreEqual(new[] { "스탯 강화", "몬스터 강화" }, headers);
        }

        //# 지정 헤더 다음부터 다음 헤더(또는 끝) 전까지의 Item 행 수집.
        private static List<ShopItemCellData> SectionItems(List<ShopItemCellData> list, string headerText)
        {
            List<ShopItemCellData> items = new List<ShopItemCellData>();
            bool active = false;
            foreach (ShopItemCellData cell in list)
            {
                if (cell.RowKind == ShopPopup.ShopRowKind.SectionHeader)
                {
                    active = cell.HeaderText == headerText;
                    continue;
                }
                if (active)
                    items.Add(cell);
            }
            return items;
        }
    }
}
