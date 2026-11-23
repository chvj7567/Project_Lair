using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# FormationViewModel 편집 규칙 — 이동·교환·비우기·기본 복귀·봉인 예고 (dungeon-formation-free §1·§4, 승인 규칙 기준).
    public class FormationViewModelTests
    {
        private const int StageWithSeal = 4;   //# 1-4 = SealedNest(망령)
        private const int StageWithoutSeal = 1;

        private FormationDatabase _db;
        private ChapterDatabase _chapters;

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
            _chapters = WorldMapTestSupport.FullDb();
        }

        private FormationViewModel Make(MetaProfile profile = null, int stage = StageWithoutSeal)
        {
            return new FormationViewModel(_db, _chapters, profile ?? new MetaProfile { ClearedStage = 5 }, stage, WorldMapTestSupport.Text, null);
        }

        private static string Fmt(int id, params object[] args) => string.Format(WorldMapTestSupport.Text(id), args);

        private static EMonster?[] Slots(FormationViewModel vm)
        {
            EMonster?[] slots = new EMonster?[FormationDatabase.SlotCount];
            for (int i = 0; i < slots.Length; ++i)
            {
                FormationNestData nest = vm.GetNest(i);
                slots[i] = nest.IsEmpty ? (EMonster?)null : nest.Monster;
            }
            return slots;
        }

        private static void ClearAll(FormationViewModel vm)
        {
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                vm.SelectNest(i);
                vm.ClearNest();
            }
        }

        //# --- 초기 상태 ---

        [Test]
        public void 저장_편성이_없으면_기본_편성_6칸_1번_선택으로_시작한다()
        {
            FormationViewModel vm = Make();

            Assert.AreEqual(6, vm.AssignedCount);
            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.AreEqual(0, vm.FilterIndex);
            Assert.IsFalse(vm.IsCustom);
            CollectionAssert.AreEqual(FormationTestKit.Default(), System.Array.ConvertAll(Slots(vm), m => m.Value));
            Assert.AreEqual(0, vm.BuildSaveRows().Count, "기본 상태는 0행 저장");
        }

        [Test]
        public void 수동_저장_편성은_행이_있는_둥지만_채우고_나머지는_빈칸이다()
        {
            MetaProfile profile = new MetaProfile { ClearedStage = 5, HasCustomFormation = true };
            profile.Formation.Add(new FormationSlotEntry { Slot = 2, Monster = EMonster.Wisp.ToString() });
            profile.Formation.Add(new FormationSlotEntry { Slot = 6, Monster = EMonster.Phantom.ToString() });

            FormationViewModel vm = Make(profile);

            Assert.AreEqual(2, vm.AssignedCount);
            Assert.IsTrue(vm.GetNest(0).IsEmpty);
            Assert.AreEqual(EMonster.Wisp, vm.GetNest(1).Monster);
            Assert.IsTrue(vm.GetNest(4).IsEmpty);
            Assert.AreEqual(EMonster.Phantom, vm.GetNest(5).Monster);
            Assert.IsTrue(vm.IsCustom);
        }

        //# --- 배치·이동·교환 ---

        [Test]
        public void 이미_현재_둥지에_있는_몬스터를_다시_고르면_아무_변화도_없다()
        {
            FormationViewModel vm = Make();
            int changed = 0;
            vm.OnChanged += () => ++changed;
            string before = vm.FeedbackText;

            vm.Place(EMonster.Wisp);

            Assert.AreEqual(0, changed);
            Assert.AreEqual(before, vm.FeedbackText);
            Assert.AreEqual(EMonster.Wisp, vm.GetNest(0).Monster);
        }

        [Test]
        public void 선택한_둥지가_차_있고_후보가_다른_둥지에_있으면_두_둥지를_교환한다()
        {
            FormationViewModel vm = Make();

            vm.Place(EMonster.Reaper);

            Assert.AreEqual(EMonster.Reaper, vm.GetNest(0).Monster);
            Assert.AreEqual(EMonster.Wisp, vm.GetNest(1).Monster, "선택 칸에 있던 몬스터가 원래 칸으로");
            Assert.AreEqual(6, vm.AssignedCount);
            Assert.AreEqual(Fmt(FormationViewModel.StrSwappedFeedback, 2, 1), vm.FeedbackText);
        }

        [Test]
        public void 선택한_둥지가_비어_있고_후보가_다른_둥지에_있으면_이동하고_원래_칸은_빈칸이다()
        {
            FormationViewModel vm = Make();
            vm.SelectNest(2);
            vm.ClearNest();

            vm.Place(EMonster.Wisp);

            Assert.IsTrue(vm.GetNest(0).IsEmpty, "원래 칸은 빈칸");
            Assert.AreEqual(EMonster.Wisp, vm.GetNest(2).Monster);
            Assert.AreEqual(5, vm.AssignedCount, "비우기 후 5칸 — 이동은 칸 수 불변");
            Assert.AreEqual(Fmt(FormationViewModel.StrMovedFeedback, 1, 3), vm.FeedbackText);
        }

        [Test]
        public void 미배치_후보를_고르면_선택_칸에_놓고_그_칸의_몬스터는_어디에도_남지_않는다()
        {
            MetaProfile owned = new MetaProfile { ClearedStage = 5 };
            owned.SetRaceGrade(ERace.Ignis, 2);
            FormationViewModel vm = Make(owned);

            //# 기본 편성은 보유 최고 등급을 따라 1번 둥지 = 견습 도깨비불. 풋내기(미배치)를 고르면 그 칸에 놓인다.
            Assert.AreEqual(EMonster.WispG2, vm.GetNest(0).Monster);

            vm.Place(EMonster.Wisp);

            Assert.AreEqual(EMonster.Wisp, vm.GetNest(0).Monster);
            CollectionAssert.DoesNotContain(Slots(vm), (EMonster?)EMonster.WispG2, "밀려난 몬스터는 보유 목록으로 돌아감");
            Assert.AreEqual(Fmt(FormationViewModel.StrPlacedFeedback, 1, SpeciesVisual.MonsterName(EMonster.Wisp, _db, WorldMapTestSupport.Text)), vm.FeedbackText);
        }

        [Test]
        public void 같은_종족의_다른_등급은_서로_다른_몬스터라_여러_둥지에_함께_놓을_수_있다()
        {
            MetaProfile owned = new MetaProfile { ClearedStage = 5 };
            owned.SetRaceGrade(ERace.Ignis, 3);
            FormationViewModel vm = Make(owned);

            //# 기본 편성은 1번 = 숙련 도깨비불(WispG3). 2번에 풋내기를 놓으면 둘이 함께 있다.
            Assert.AreEqual(EMonster.WispG3, vm.GetNest(0).Monster);

            vm.SelectNest(1);
            vm.Place(EMonster.Wisp);

            Assert.AreEqual(EMonster.WispG3, vm.GetNest(0).Monster);
            Assert.AreEqual(EMonster.Wisp, vm.GetNest(1).Monster);
        }

        [Test]
        public void 보유하지_않은_등급은_배치할_수_없다()
        {
            FormationViewModel vm = Make();
            int changed = 0;
            vm.OnChanged += () => ++changed;

            vm.Place(EMonster.WispG2);
            vm.Place(EMonster.PhantomG5);

            Assert.AreEqual(0, changed);
            Assert.AreEqual(EMonster.Wisp, vm.GetNest(0).Monster);
            Assert.IsFalse(vm.IsCustom);
        }

        //# --- 비우기 ---

        [Test]
        public void 비우기는_선택_둥지를_비우고_칸_수와_저장_행이_줄어든다()
        {
            FormationViewModel vm = Make();
            vm.SelectNest(3);

            Assert.IsTrue(vm.CanClear);
            vm.ClearNest();

            Assert.IsTrue(vm.GetNest(3).IsEmpty);
            Assert.AreEqual(5, vm.AssignedCount);
            Assert.IsFalse(vm.CanClear, "빈 둥지는 비우기 비활성");
            Assert.AreEqual(Fmt(FormationViewModel.StrCleared, 4), vm.FeedbackText);
            Assert.IsTrue(vm.IsCustom);
            List<FormationSlotEntry> rows = vm.BuildSaveRows();
            Assert.AreEqual(5, rows.Count);
            Assert.IsFalse(rows.Exists(r => r.Slot == 4), "빈 둥지는 행을 저장하지 않는다");
        }

        [Test]
        public void 이미_빈_둥지를_다시_비워도_변경_이벤트가_없다()
        {
            FormationViewModel vm = Make();
            vm.ClearNest();
            int changed = 0;
            vm.OnChanged += () => ++changed;

            vm.ClearNest();

            Assert.AreEqual(0, changed);
            Assert.AreEqual(5, vm.AssignedCount);
        }

        [Test]
        public void 전부_비우면_0칸이고_경고가_나오며_0행_수동_저장이다()
        {
            FormationViewModel vm = Make();

            ClearAll(vm);

            Assert.AreEqual(0, vm.AssignedCount);
            Assert.AreEqual(WorldMapTestSupport.Text(FormationViewModel.StrEmptyWarning), vm.EmptyWarningText);
            Assert.IsTrue(vm.IsCustom, "0칸은 기본과 구별되는 수동 편성");
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
        }

        [Test]
        public void 칸_수가_1_이상이면_빈_편성_경고가_없다()
        {
            Assert.AreEqual(string.Empty, Make().EmptyWarningText);
        }

        //# --- 기본 복귀 ---

        [Test]
        public void 기본_편성은_배치_선택_필터를_모두_초기화한다()
        {
            FormationViewModel vm = Make();
            vm.SelectNest(4);
            vm.SelectFilter(2);
            vm.ClearNest();
            vm.SelectNest(0);
            vm.Place(EMonster.Reaper);
            Assert.IsTrue(vm.IsCustom);

            vm.ResetToDefault();

            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.AreEqual(0, vm.FilterIndex);
            Assert.IsFalse(vm.IsCustom);
            Assert.AreEqual(6, vm.AssignedCount);
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
            Assert.AreEqual(WorldMapTestSupport.Text(FormationViewModel.StrReset), vm.FeedbackText);
        }

        [Test]
        public void 기본_편성은_종족별_보유_최고_등급을_따라간다()
        {
            MetaProfile owned = new MetaProfile { ClearedStage = 5, HasCustomFormation = true };
            owned.SetRaceGrade(ERace.Ignis, 3);
            FormationViewModel vm = Make(owned);
            ClearAll(vm);

            vm.ResetToDefault();

            Assert.AreEqual(EMonster.WispG3, vm.GetNest(0).Monster);
            Assert.IsFalse(vm.IsCustom);
        }

        [Test]
        public void 수동_편성_저장_행은_기본과_같아지는_순간_0행으로_돌아간다()
        {
            FormationViewModel vm = Make();
            vm.Place(EMonster.Reaper);
            Assert.AreEqual(6, vm.BuildSaveRows().Count);

            vm.Place(EMonster.Wisp);

            Assert.IsFalse(vm.IsCustom);
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
        }

        //# --- 선택 범위 ---

        [Test]
        public void 범위_밖_둥지_선택과_필터는_무시된다()
        {
            FormationViewModel vm = Make();
            vm.SelectNest(-1);
            vm.SelectNest(6);
            vm.SelectFilter(-1);
            vm.SelectFilter(FormationDatabase.SlotCount + 1);

            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.AreEqual(0, vm.FilterIndex);
        }

        //# --- 봉인 예고 ---

        [Test]
        public void 봉인_규칙이_없는_스테이지는_예고가_없다()
        {
            FormationViewModel vm = Make();

            Assert.IsFalse(vm.HasSealForecast);
            Assert.AreEqual(-1, vm.SealedIndex);
            Assert.AreEqual(string.Empty, vm.SealForecastText);
            Assert.IsFalse(vm.GetNest(3).IsSealed);
        }

        [Test]
        public void 봉인_스테이지에서_대상_종_둥지가_예고_위치이고_타일에_표시된다()
        {
            FormationViewModel vm = Make(null, StageWithSeal);

            Assert.IsTrue(vm.HasSealForecast);
            Assert.AreEqual(3, vm.SealedIndex, "기본 편성 망령 = 4번 둥지");
            Assert.IsTrue(vm.GetNest(3).IsSealed);
            Assert.IsFalse(vm.GetNest(0).IsSealed);
            StringAssert.Contains("4", vm.SealForecastText);
        }

        [Test]
        public void 봉인_대상_종을_옮기면_예고가_즉시_따라간다()
        {
            FormationViewModel vm = Make(null, StageWithSeal);
            int changed = 0;
            vm.OnChanged += () => ++changed;

            vm.Place(EMonster.Wraith);

            Assert.AreEqual(1, changed);
            Assert.AreEqual(0, vm.SealedIndex, "망령이 1번으로 이동 → 1번 봉인");
            Assert.IsTrue(vm.GetNest(0).IsSealed);
            Assert.IsFalse(vm.GetNest(3).IsSealed);
        }

        [Test]
        public void 봉인_대상_종_둥지를_비우면_채워진_가장_낮은_번호가_대신_봉인된다()
        {
            FormationViewModel vm = Make(null, StageWithSeal);
            vm.SelectNest(3);
            vm.ClearNest();

            Assert.AreEqual(0, vm.SealedIndex, "망령 기본 위치(4번)도 비었으니 채운 최저 번호");
            vm.SelectNest(0);
            vm.ClearNest();
            Assert.AreEqual(1, vm.SealedIndex);
        }

        [Test]
        public void 모든_둥지를_비우면_봉인_없음_문구가_나온다()
        {
            FormationViewModel vm = Make(null, StageWithSeal);

            ClearAll(vm);

            Assert.IsTrue(vm.HasSealForecast, "규칙은 있지만");
            Assert.AreEqual(-1, vm.SealedIndex);
            Assert.AreEqual(WorldMapTestSupport.Text(FormationViewModel.StrNoSeal), vm.SealForecastText);
        }

        [Test]
        public void 봉인_예고는_저장_편성을_읽어도_같은_규칙으로_계산된다()
        {
            MetaProfile profile = new MetaProfile { ClearedStage = 5, HasCustomFormation = true };
            profile.Formation.Add(new FormationSlotEntry { Slot = 2, Monster = EMonster.Reaper.ToString() });
            profile.Formation.Add(new FormationSlotEntry { Slot = 5, Monster = EMonster.Wraith.ToString() });

            FormationViewModel vm = Make(profile, StageWithSeal);

            Assert.AreEqual(4, vm.SealedIndex, "망령이 5번에만 있음");
        }
    }
}
