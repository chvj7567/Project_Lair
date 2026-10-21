using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 도감 ViewModel (relics §7.2) — 필터 AND · 기본 선택 · 미획득 표시 · 상세 · 드랍 영웅 힌트. View 없이 POCO 로 검증한다 (Rule 02 §6).
    public class RelicViewModelTests : RelicStaticStateTestBase
    {
        private RelicDatabase _db;

        [SetUp]
        public void 유물_DB를_만들고_챕터_DB를_올린다()
        {
            _db = Db();
            ChapterDatabase.Current = WorldMapTestSupport.FullDb();
        }

        private RelicViewModel Vm(MetaProfile profile, Func<string, string> heroName = null)
        {
            return new RelicViewModel(_db, profile, WorldMapTestSupport.Text, heroName, null);
        }

        private static List<string> Ids(List<RelicData> list)
        {
            List<string> ids = new List<string>();
            foreach (RelicData data in list)
            {
                ids.Add(data.RelicId);
            }
            return ids;
        }

        //# --- 필터 / 선택 ---

        [Test]
        public void 필터_없음은_16종_전부이고_JSON_순서다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            List<RelicData> all = vm.FilteredRelics();

            Assert.AreEqual(16, all.Count);
            Assert.AreEqual("HeartOfStone", all[0].RelicId);
            Assert.AreEqual("SpawnPrelude", all[15].RelicId);
        }

        [Test]
        public void 종류_필터는_능력치_6_전투효과_5_편의_5를_거른다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            vm.SetCategoryFilter(1);
            Assert.AreEqual(6, vm.FilteredRelics().Count);
            vm.SetCategoryFilter(2);
            Assert.AreEqual(5, vm.FilteredRelics().Count);
            vm.SetCategoryFilter(3);
            Assert.AreEqual(5, vm.FilteredRelics().Count);
            vm.SetCategoryFilter(RelicViewModel.FilterAll);
            Assert.AreEqual(16, vm.FilteredRelics().Count);
        }

        [Test]
        public void 등급_필터는_일반_8_희귀_6_전설_2를_거른다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            vm.SetRarityFilter(1);
            Assert.AreEqual(8, vm.FilteredRelics().Count);
            vm.SetRarityFilter(2);
            Assert.AreEqual(6, vm.FilteredRelics().Count);
            vm.SetRarityFilter(3);
            Assert.AreEqual(2, vm.FilteredRelics().Count);
        }

        [Test]
        public void 종류와_등급_필터는_AND_교집합이다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            vm.SetCategoryFilter(1);
            vm.SetRarityFilter(2);
            CollectionAssert.AreEqual(new[] { "BrokenHourglass", "BreedingCharm" }, Ids(vm.FilteredRelics()));

            vm.SetCategoryFilter(2);
            vm.SetRarityFilter(3);
            CollectionAssert.AreEqual(new[] { "WrathSpirit" }, Ids(vm.FilteredRelics()));

            vm.SetCategoryFilter(3);
            vm.SetRarityFilter(1);
            CollectionAssert.AreEqual(new[] { "SoulPurse", "LordsTome", "ConsolationFeather" }, Ids(vm.FilteredRelics()));
        }

        [Test]
        public void 교집합이_비면_빈_결과_상태이고_선택_셀_상세가_모두_비어_있다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            vm.SetCategoryFilter(3);
            vm.SetRarityFilter(3);

            Assert.IsTrue(vm.IsEmptyResult);
            Assert.IsNull(vm.SelectedRelicId);
            Assert.IsNull(vm.BuildDetail());
            Assert.AreEqual(0, vm.BuildCells().Count);
            Assert.AreEqual("해당 조건의 유물이 없습니다", vm.NoResultText);
        }

        [Test]
        public void 선택이_없으면_필터_결과의_첫_유물이_기본_선택이다()
        {
            RelicViewModel vm = Vm(new MetaProfile());
            Assert.AreEqual("HeartOfStone", vm.SelectedRelicId);

            vm.SetCategoryFilter(2);

            Assert.AreEqual("EmberBrand", vm.SelectedRelicId);
        }

        [Test]
        public void 명시_선택은_현재_필터_안에서만_유효하다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            vm.Select("RustyBlade");
            Assert.AreEqual("RustyBlade", vm.SelectedRelicId);
            vm.Select("NoSuchRelic");

            Assert.AreEqual("HeartOfStone", vm.SelectedRelicId, "목록에 없는 선택은 기본 선택으로 폴백");
        }

        [Test]
        public void 필터를_바꾸면_명시_선택이_해제된다()
        {
            RelicViewModel vm = Vm(new MetaProfile());
            vm.Select("RustyBlade");

            vm.SetCategoryFilter(1);
            vm.SetCategoryFilter(RelicViewModel.FilterAll);

            Assert.AreEqual("HeartOfStone", vm.SelectedRelicId, "예전 선택(RustyBlade)이 되살아나면 안 된다");
        }

        [Test]
        public void 등급_필터_변경도_선택을_해제한다()
        {
            RelicViewModel vm = Vm(new MetaProfile());
            vm.Select("RustyBlade");

            vm.SetRarityFilter(1);
            vm.SetRarityFilter(RelicViewModel.FilterAll);

            Assert.AreEqual("HeartOfStone", vm.SelectedRelicId);
        }

        [Test]
        public void OnChanged는_필터와_선택이_실제로_바뀔_때만_발행된다()
        {
            RelicViewModel vm = Vm(new MetaProfile());
            int changed = 0;
            vm.OnChanged += () => changed++;

            vm.SetCategoryFilter(0);
            vm.SetRarityFilter(0);
            vm.Select("HeartOfStone");
            Assert.AreEqual(0, changed, "같은 값은 통지 없음 (기본 선택과 같은 Select 포함)");

            vm.Select("RustyBlade");
            vm.SetCategoryFilter(1);
            vm.SetRarityFilter(1);

            Assert.AreEqual(3, changed);
        }

        //# --- 셀 ---

        [Test]
        public void 미획득_셀은_물음표_이름_빈_중첩_표기_어두운_테두리다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            RelicCellData cell = vm.BuildCells()[1];

            Assert.AreEqual("RustyBlade", cell.RelicId);
            Assert.IsFalse(cell.IsOwned);
            Assert.AreEqual(0, cell.Stack);
            Assert.AreEqual("???", cell.NameText);
            Assert.AreEqual(string.Empty, cell.StackText);
            Assert.IsFalse(cell.IsMax);
            AssertColor(0x23, 0x28, 0x34, cell.BorderColor);
        }

        [Test]
        public void 보유_셀은_이름_n_m_표기와_등급_색_테두리다()
        {
            MetaProfile p = ProfileWith("HeartOfStone", "HeartOfStone", "BrokenHourglass", "KingsCrownShard");
            RelicViewModel vm = Vm(p);

            List<RelicCellData> cells = vm.BuildCells();

            Assert.AreEqual("단단한 심장석", cells[0].NameText);
            Assert.AreEqual("2/5", cells[0].StackText);
            Assert.IsFalse(cells[0].IsMax);
            AssertColor(0x9A, 0xA5, 0xBD, cells[0].BorderColor, "일반");
            AssertColor(0x5A, 0xA9, 0xFF, cells[3].BorderColor, "희귀");
            Assert.AreEqual("1/3", cells[3].StackText);
            AssertColor(0xB0, 0x7C, 0xFF, cells[5].BorderColor, "전설");
            Assert.IsTrue(cells[5].IsMax, "KingsCrownShard 1/1");
        }

        [Test]
        public void 최대_중첩_셀은_IsMax이고_초과_보유는_maxStack으로_클램프되며_프로필은_그대로다()
        {
            MetaProfile p = new MetaProfile();
            SetStack(p, "HeartOfStone", 99);
            RelicViewModel vm = Vm(p);

            RelicCellData cell = vm.BuildCells()[0];

            Assert.AreEqual(5, cell.Stack);
            Assert.AreEqual("5/5", cell.StackText);
            Assert.IsTrue(cell.IsMax);
            Assert.AreEqual(99, p.GetRelicStack("HeartOfStone"), "relics §3.6 — 세이브 비수정");
        }

        [Test]
        public void DB에_없는_보유_id는_셀과_집계에서_무시된다()
        {
            MetaProfile p = new MetaProfile();
            SetStack(p, "GhostRelic", 3);
            RelicViewModel vm = Vm(p);

            Assert.AreEqual(16, vm.BuildCells().Count);
            Assert.AreEqual(0, vm.OwnedKinds);
            Assert.AreEqual(0, vm.OwnedStackSum);
            Assert.AreEqual(3, p.GetRelicStack("GhostRelic"));
        }

        [Test]
        public void 셀은_선택된_하나만_IsSelected이고_OnSelect가_선택을_바꾼다()
        {
            RelicViewModel vm = Vm(new MetaProfile());
            List<RelicCellData> cells = vm.BuildCells();

            Assert.IsTrue(cells[0].IsSelected);
            int selected = 0;
            foreach (RelicCellData cell in cells)
            {
                selected += cell.IsSelected ? 1 : 0;
            }
            Assert.AreEqual(1, selected);

            cells[2].OnSelect(cells[2].RelicId);

            Assert.AreEqual("SwiftBoots", vm.SelectedRelicId);
            Assert.IsTrue(vm.BuildCells()[2].IsSelected);
            Assert.IsFalse(vm.BuildCells()[0].IsSelected);
        }

        [Test]
        public void 필터를_통과한_셀만_만들어지고_순서가_유지된다()
        {
            RelicViewModel vm = Vm(new MetaProfile());
            vm.SetCategoryFilter(3);

            List<RelicCellData> cells = vm.BuildCells();

            Assert.AreEqual(5, cells.Count);
            Assert.AreEqual("HasteHourglass", cells[0].RelicId);
            Assert.AreEqual("SpawnPrelude", cells[4].RelicId);
        }

        //# --- 수집 현황 ---

        [Test]
        public void 수집_현황은_보유_종류_중첩_합_전체_슬롯_52를_표시한다()
        {
            MetaProfile p = ProfileWith("HeartOfStone", "HeartOfStone", "SoulPurse");
            SetStack(p, "RustyBlade", 99);
            RelicViewModel vm = Vm(p);

            Assert.AreEqual(3, vm.OwnedKinds);
            Assert.AreEqual(16, vm.TotalKinds);
            Assert.AreEqual(2 + 1 + 5, vm.OwnedStackSum, "RustyBlade 99 → 5 로 클램프");
            Assert.AreEqual(52, vm.TotalStackSlots);
            Assert.AreEqual("수집 3 / 16", vm.CollectedText);
            Assert.AreEqual("8 / 52", vm.StackSumText);
        }

        [Test]
        public void 유물_DB가_null이면_모든_목록이_비어_있다_E_1()
        {
            RelicViewModel vm = new RelicViewModel(null, new MetaProfile(), WorldMapTestSupport.Text, null, null);

            Assert.AreEqual(0, vm.FilteredRelics().Count);
            Assert.AreEqual(0, vm.BuildCells().Count);
            Assert.IsNull(vm.BuildDetail());
            Assert.IsNull(vm.SelectedRelicId);
            Assert.AreEqual(0, vm.TotalKinds);
            Assert.AreEqual(0, vm.TotalStackSlots);
            Assert.AreEqual("수집 0 / 0", vm.CollectedText);
        }

        [Test]
        public void 프로필이_null이어도_미보유로_취급하고_예외가_없다()
        {
            RelicViewModel vm = new RelicViewModel(_db, null, WorldMapTestSupport.Text, null, null);

            Assert.DoesNotThrow(() => vm.BuildCells());
            Assert.AreEqual(0, vm.OwnedKinds);
            Assert.AreEqual(0, vm.OwnedStackSum);
        }

        //# --- 상세 ---

        [Test]
        public void 미보유_상세는_물음표_이름과_안내문만_보이고_효과_블록이_없다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            RelicDetailData detail = vm.BuildDetail();

            Assert.IsFalse(detail.IsOwned);
            Assert.AreEqual("???", detail.NameText);
            Assert.AreEqual("아직 획득하지 못한 유물입니다", detail.LockedText);
            Assert.IsNull(detail.StackText);
            Assert.IsNull(detail.CurrentEffectText);
            Assert.IsFalse(detail.ShowNext);
            Assert.IsFalse(detail.ShowMaxLabel);
            Assert.AreEqual("일반", detail.RarityText);
            Assert.AreEqual("능력치", detail.CategoryText);
        }

        [Test]
        public void 미보유여도_드랍_영웅_힌트는_챕터_순서로_표시된다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            vm.Select("HeartOfStone");
            Assert.AreEqual("드랍 영웅: Knight, Archer, Assassin, Berserker, Paladin", vm.BuildDetail().DropHeroText);
            vm.Select("KingsCrownShard");
            Assert.AreEqual("드랍 영웅: Knight, Berserker, Paladin", vm.BuildDetail().DropHeroText);
            vm.Select("WrathSpirit");
            Assert.AreEqual("드랍 영웅: Archer, Assassin, Berserker", vm.BuildDetail().DropHeroText);
            vm.Select("CursedBanner");
            Assert.AreEqual("드랍 영웅: Knight, Archer, Assassin, Berserker", vm.BuildDetail().DropHeroText, "성기사 행 없음");
        }

        [Test]
        public void 드랍_영웅_이름은_주입된_이름_해석기를_쓴다()
        {
            RelicViewModel vm = Vm(new MetaProfile(), id => "<" + id + ">");
            vm.Select("WrathSpirit");

            Assert.AreEqual("드랍 영웅: <Archer>, <Assassin>, <Berserker>", vm.BuildDetail().DropHeroText);
        }

        [Test]
        public void 챕터_DB가_없으면_드랍_영웅_힌트는_빈_문자열이다()
        {
            ChapterDatabase.Current = null;
            RelicViewModel vm = Vm(new MetaProfile());

            Assert.AreEqual(string.Empty, vm.BuildDetail().DropHeroText);
        }

        [Test]
        public void 보유_중간_중첩의_상세는_현재_효과와_다음_중첩_효과를_보여준다()
        {
            RelicViewModel vm = Vm(ProfileWith("HeartOfStone", "HeartOfStone"));

            RelicDetailData detail = vm.BuildDetail();

            Assert.IsTrue(detail.IsOwned);
            Assert.AreEqual("단단한 심장석", detail.NameText);
            Assert.AreEqual("중첩 2 / 5", detail.StackText);
            Assert.AreEqual("모든 몬스터의 HP +4.8%", detail.CurrentEffectText);
            Assert.IsTrue(detail.ShowNext);
            Assert.AreEqual("모든 몬스터의 HP +6.3%", detail.NextEffectText);
            Assert.IsFalse(detail.ShowMaxLabel);
        }

        [Test]
        public void 최대_중첩의_상세는_다음_중첩_대신_최대_중첩_라벨이다()
        {
            RelicViewModel vm = Vm(ProfileWith("KingsCrownShard"));
            vm.Select("KingsCrownShard");

            RelicDetailData detail = vm.BuildDetail();

            Assert.AreEqual("중첩 1 / 1", detail.StackText);
            Assert.AreEqual("모든 몬스터의 HP와 공격력 +5%", detail.CurrentEffectText);
            Assert.IsFalse(detail.ShowNext);
            Assert.IsNull(detail.NextEffectText);
            Assert.IsTrue(detail.ShowMaxLabel);
            Assert.AreEqual("최대 중첩", detail.MaxLabelText);
            Assert.AreEqual("전설", detail.RarityText);
            AssertColor(0xB0, 0x7C, 0xFF, detail.RarityColor);
        }

        [Test]
        public void 초과_보유_상세는_maxStack으로_클램프해_표시한다()
        {
            MetaProfile p = new MetaProfile();
            SetStack(p, "HeartOfStone", 99);
            RelicViewModel vm = Vm(p);

            RelicDetailData detail = vm.BuildDetail();

            Assert.AreEqual("중첩 5 / 5", detail.StackText);
            Assert.AreEqual("모든 몬스터의 HP +8.4%", detail.CurrentEffectText);
            Assert.IsTrue(detail.ShowMaxLabel);
            Assert.IsFalse(detail.ShowNext);
        }

        [Test]
        public void 종류_라벨과_등급_색_조회가_문구_id와_색상표를_따른다()
        {
            RelicViewModel vm = Vm(new MetaProfile());

            Assert.AreEqual("능력치", vm.CategoryText(ERelicCategory.Stat));
            Assert.AreEqual("전투 효과", vm.CategoryText(ERelicCategory.Combat));
            Assert.AreEqual("편의", vm.CategoryText(ERelicCategory.Convenience));
            AssertColor(0x9A, 0xA5, 0xBD, vm.RarityColorOf(ERelicRarity.Common));
            AssertColor(0x5A, 0xA9, 0xFF, vm.RarityColorOf(ERelicRarity.Rare));
        }

        [Test]
        public void 문구_조회가_비어_있으면_폴백_문구를_쓴다()
        {
            RelicViewModel vm = new RelicViewModel(_db, new MetaProfile(), id => string.Empty, null, null);

            Assert.AreEqual("수집 0 / 16", vm.CollectedText);
            Assert.AreEqual("해당 조건의 유물이 없습니다", vm.NoResultText);
            Assert.AreEqual("???", vm.BuildCells()[0].NameText);
        }
    }

    //# 유물 설명문 숫자 포맷 (relics §7.2, §1.2 "중첩별 실제 값") — 0.02% / 0.056% 같은 작은 수 보존.
    public class RelicTextTests
    {
        private RelicDatabase _db;

        [SetUp]
        public void 출하_유물_DB를_만든다()
        {
            _db = RelicTestSupport.ShippedDb();
        }

        private string Describe(string relicId, int stack)
        {
            return RelicText.Describe(_db, _db.Get(relicId), stack, WorldMapTestSupport.Text);
        }

        [Test]
        public void Number는_소수_셋째_자리까지_불필요한_0을_없애고_부동소수_잔여_오차를_지운다()
        {
            Assert.AreEqual("0.056", RelicText.Number(0.056f));
            Assert.AreEqual("0.02", RelicText.Number(0.02f));
            Assert.AreEqual("8.4", RelicText.Number(8.4f));
            Assert.AreEqual("52.5", RelicText.Number(52.5f));
            Assert.AreEqual("11.2", RelicText.Number(11.2f));
            Assert.AreEqual("3", RelicText.Number(3f));
            Assert.AreEqual("0.63", RelicText.Number(0.63000005f));
            Assert.AreEqual("0", RelicText.Number(0.0004f), "셋째 자리 아래는 반올림");
            Assert.AreEqual("-1.5", RelicText.Number(-1.5f));
        }

        [Test]
        public void 잿불_낙인은_중첩별로_0_02에서_0_056퍼센트까지_작은_수를_보존한다()
        {
            StringAssert.Contains("0.02%", Describe("EmberBrand", 1));
            StringAssert.Contains("0.032%", Describe("EmberBrand", 2));
            StringAssert.Contains("0.042%", Describe("EmberBrand", 3));
            StringAssert.Contains("0.05%", Describe("EmberBrand", 4));
            StringAssert.Contains("0.056%", Describe("EmberBrand", 5));
        }

        [Test]
        public void 뇌운_조각_설명은_고정_간격과_중첩_비율_두_인자를_채운다()
        {
            Assert.AreEqual("10초마다 영웅에게 번개가 떨어져 최대 HP의 0.3%만큼 피해를 입힌다", Describe("ThunderShard", 1));
            StringAssert.Contains("10초마다", Describe("ThunderShard", 3));
            StringAssert.Contains("0.63%", Describe("ThunderShard", 3));
        }

        [Test]
        public void 퍼센트류_설명은_기획서_중첩별_실제_값과_같다()
        {
            StringAssert.Contains("+8.4%", Describe("HeartOfStone", 5));
            StringAssert.Contains("+5.6%", Describe("RustyBlade", 5));
            StringAssert.Contains("+5.6%", Describe("SwiftBoots", 5));
            StringAssert.Contains("-4.2%", Describe("BrokenHourglass", 3));
            StringAssert.Contains("-6.3%", Describe("BreedingCharm", 3));
            StringAssert.Contains("-8.4%", Describe("CursedBanner", 3));
            StringAssert.Contains("+11.2%", Describe("SoulPurse", 5));
            StringAssert.Contains("+11.2%", Describe("LordsTome", 5));
            StringAssert.Contains("+52.5%", Describe("ConsolationFeather", 3));
            StringAssert.Contains("+5%", Describe("KingsCrownShard", 1));
        }

        [Test]
        public void 음수_amount_유물은_절댓값으로_표시한다()
        {
            string text = Describe("BrokenHourglass", 1);

            StringAssert.Contains("-2%", text);
            StringAssert.DoesNotContain("--", text);
        }

        [Test]
        public void 정수형_설명은_소수점_없이_표시한다()
        {
            StringAssert.Contains("2마리", Describe("AmbushHorn", 1));
            StringAssert.Contains("4마리", Describe("AmbushHorn", 2));
            StringAssert.Contains("6마리", Describe("AmbushHorn", 3));
            StringAssert.Contains("50% 이하", Describe("WrathSpirit", 1));
            StringAssert.Contains("10초간", Describe("WrathSpirit", 1));
            StringAssert.Contains("2배속", Describe("HasteHourglass", 1));
            StringAssert.Contains("3초 빨라진다", Describe("SpawnPrelude", 1));
        }

        [Test]
        public void 중첩_0이하는_중첩_1_값으로_표시한다()
        {
            Assert.AreEqual(Describe("HeartOfStone", 1), Describe("HeartOfStone", 0));
            Assert.AreEqual(Describe("HeartOfStone", 1), Describe("HeartOfStone", -4));
        }

        [Test]
        public void 설명문_조립_실패는_빈_문자열_또는_원문으로_안전하게_돌아온다()
        {
            RelicData heart = _db.Get("HeartOfStone");

            Assert.AreEqual(string.Empty, RelicText.Describe(null, heart, 1, WorldMapTestSupport.Text));
            Assert.AreEqual(string.Empty, RelicText.Describe(_db, null, 1, WorldMapTestSupport.Text));
            Assert.AreEqual(string.Empty, RelicText.Describe(_db, heart, 1, null));
            Assert.AreEqual(string.Empty, RelicText.Describe(_db, heart, 1, id => string.Empty));
            Assert.AreEqual("{0} {1}", RelicText.Describe(_db, heart, 1, id => "{0} {1}"), "인자 부족 → 원문");
        }

        [Test]
        public void 효과가_없는_유물_데이터는_빈_문자열이다()
        {
            RelicData empty = new RelicData { RelicId = "X", DescKey = 1376, Effects = new List<RelicEffectData>() };

            Assert.AreEqual(string.Empty, RelicText.Describe(_db, empty, 1, WorldMapTestSupport.Text));
        }
    }

    //# 결과창 유물 띠 확정값 (relics §7.3) — ResultPopup.BuildRows 의 순수 계산.
    public class ResultPopupRelicRowsTests : RelicStaticStateTestBase
    {
        private RelicDatabase _db;

        [SetUp]
        public void 유물_DB와_실제_문구를_올린다()
        {
            _db = Db();
            RelicDatabase.Current = _db;
            CHTextProviderOn();
        }

        private static void CHTextProviderOn()
        {
            ChvjUnityInfra.CHText.StringProvider = new DifficultyTestSupport.RealStrings();
        }

        private static ResultPopupArg Arg(BattleResult result)
        {
            return new ResultPopupArg { Result = result, HasMeta = true, SoulsGained = 120, XpGained = 40, ClearTime = 222.1f };
        }

        [Test]
        public void 신규_획득_승리는_띠_NEW_배지를_보이고_중첩_문구는_비운다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "RustyBlade";
            arg.RelicIsNew = true;
            arg.RelicStackAfter = 1;
            arg.RelicMaxStack = 5;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsTrue(rows.ShowRelicBand);
            Assert.AreEqual("RustyBlade", rows.RelicId);
            Assert.AreEqual("녹슨 칼날", rows.RelicNameText);
            Assert.AreEqual("일반", rows.RelicRarityText);
            AssertColor(0x9A, 0xA5, 0xBD, rows.RelicRarityColor);
            Assert.IsFalse(rows.RelicIsLegendary);
            Assert.IsTrue(rows.ShowRelicNewBadge);
            Assert.AreEqual(string.Empty, rows.RelicStackText);
            Assert.IsFalse(rows.ShowRelicMissRow);
        }

        [Test]
        public void 중복_획득은_NEW_없이_중첩_플러스_1_문구를_보인다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "HeartOfStone";
            arg.RelicIsNew = false;
            arg.RelicStackAfter = 3;
            arg.RelicMaxStack = 5;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsTrue(rows.ShowRelicBand);
            Assert.IsFalse(rows.ShowRelicNewBadge);
            Assert.AreEqual("중첩 +1 (3 / 5)", rows.RelicStackText);
        }

        [Test]
        public void 전설_유물은_전설_글로우_표시와_보라색_등급이다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "KingsCrownShard";
            arg.RelicIsNew = true;
            arg.RelicStackAfter = 1;
            arg.RelicMaxStack = 1;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsTrue(rows.RelicIsLegendary);
            Assert.AreEqual("전설", rows.RelicRarityText);
            AssertColor(0xB0, 0x7C, 0xFF, rows.RelicRarityColor);
        }

        [Test]
        public void 희귀_유물은_희귀_문구와_파란색이다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "ThunderShard";
            arg.RelicStackAfter = 2;
            arg.RelicMaxStack = 3;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.AreEqual("희귀", rows.RelicRarityText);
            AssertColor(0x5A, 0xA9, 0xFF, rows.RelicRarityColor);
            Assert.IsFalse(rows.RelicIsLegendary);
        }

        [Test]
        public void 승리_미획득은_띠_없이_다음_승리_확률_줄을_보인다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = false;
            arg.ShowNextChance = true;
            arg.NextWinChancePercent = 45;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsFalse(rows.ShowRelicBand);
            Assert.IsTrue(rows.ShowRelicMissRow);
            Assert.AreEqual("유물 없음 · 다음 승리 시 획득 확률 45%", rows.RelicMissText);
        }

        [Test]
        public void 승리_미획득이어도_후보_전부_보유로_ShowNextChance가_꺼져_있으면_줄이_없다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = false;
            arg.ShowNextChance = false;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsFalse(rows.ShowRelicBand);
            Assert.IsFalse(rows.ShowRelicMissRow);
        }

        [Test]
        public void 패배_미획득은_ShowNextChance가_켜져_있어도_줄을_숨긴다()
        {
            ResultPopupArg arg = Arg(BattleResult.Lose);
            arg.HasRelicDrop = false;
            arg.ShowNextChance = true;
            arg.NextWinChancePercent = 45;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsFalse(rows.ShowRelicBand);
            Assert.IsFalse(rows.ShowRelicMissRow, "패배 화면을 복잡하게 하지 않는다");
        }

        [Test]
        public void 패배에서도_획득하면_띠를_보이고_미획득_줄은_없다()
        {
            ResultPopupArg arg = Arg(BattleResult.Lose);
            arg.HasRelicDrop = true;
            arg.RelicId = "SoulPurse";
            arg.RelicIsNew = true;
            arg.RelicStackAfter = 1;
            arg.RelicMaxStack = 5;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsTrue(rows.ShowRelicBand);
            Assert.IsFalse(rows.ShowRelicMissRow);
            Assert.IsTrue(rows.ShowRelicNewBadge);
        }

        [Test]
        public void 획득했으면_ShowNextChance와_무관하게_미획득_줄이_없다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "SoulPurse";
            arg.ShowNextChance = true;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsFalse(rows.ShowRelicMissRow);
        }

        [Test]
        public void 유물_DB가_없으면_획득해도_띠를_숨긴다_E_1()
        {
            RelicDatabase.Current = null;
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "RustyBlade";

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsFalse(rows.ShowRelicBand);
        }

        [Test]
        public void 알_수_없는_유물_id는_띠를_숨긴다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "GhostRelic";

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsFalse(rows.ShowRelicBand);
        }

        [Test]
        public void 문구_공급자가_없으면_이름은_id로_중첩_문구는_기본문으로_폴백한다()
        {
            ChvjUnityInfra.CHText.StringProvider = null;
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "RustyBlade";
            arg.RelicIsNew = false;
            arg.RelicStackAfter = 2;
            arg.RelicMaxStack = 5;

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.AreEqual("RustyBlade", rows.RelicNameText);
            Assert.AreEqual("중첩 +1 (2 / 5)", rows.RelicStackText);
            Assert.AreEqual("Common", rows.RelicRarityText);
        }

        [Test]
        public void 유물_띠가_있어도_기존_항목_줄_확정값은_그대로다()
        {
            ResultPopupArg arg = Arg(BattleResult.Win);
            arg.HasRelicDrop = true;
            arg.RelicId = "RustyBlade";

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsTrue(rows.ShowClearRow);
            Assert.IsNotEmpty(rows.ClearTimeText);
            Assert.IsTrue(rows.ShowSoulsRow);
            Assert.AreEqual("+120", rows.SoulsText);
            Assert.AreEqual("+40", rows.XpText);
        }

        [Test]
        public void RarityColor는_null_또는_잘못된_색상_문자열이면_회색이다()
        {
            AssertColor(0x9A, 0xA5, 0xBD, ResultPopup.RarityColor(null));
            AssertColor(0x9A, 0xA5, 0xBD, ResultPopup.RarityColor(new RelicRarityRow { colorHex = "not-a-color" }));
            AssertColor(0xB0, 0x7C, 0xFF, ResultPopup.RarityColor(new RelicRarityRow { colorHex = "#b07cff" }));
        }
    }
}
