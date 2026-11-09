using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 몬스터 도감 개편(monster-codex) — 해금 산식·NEW 스냅샷·이펙트 상한 배분 자체 테스트(정상 + 엣지). 본격 스위트는 test-engineer.
    public class CodexViewModelTests
    {
        private FormationDatabase _db;

        [SetUp]
        public void SetUp()
        {
            CodexSession.ResetForTest();
            _db = FormationTestKit.ShippedDb();
        }

        [TearDown]
        public void TearDown()
        {
            CodexSession.ResetForTest();
        }

        private MetaProfile Profile(EMonster race, int grade, int level, bool seen = true)
        {
            MetaProfile profile = new MetaProfile();
            if (seen)
            {
                profile.SeenMonsters.Add(race.ToString());
            }
            _db.TryGetRace(race, out ERace eRace);
            profile.SetRaceGrade(eRace, grade);
            profile.SetShopLevel("Enhance_" + race, level);
            return profile;
        }

        private CodexViewModel Vm(MetaProfile profile) => new CodexViewModel(profile, _db, WorldMapTestSupport.Text);

        [TestCase(4, 5, 16)]
        [TestCase(2, 3, 6)]
        [TestCase(1, 1, 2)]
        [TestCase(3, 0, 3)]
        [TestCase(1, 0, 1)]
        public void 열린_칸_수는_등급과_티어_조합의_곱이다(int grade, int level, int expected)
        {
            CodexViewModel vm = Vm(Profile(EMonster.Wisp, grade, level));
            vm.SelectRace(EMonster.Wisp);

            Assert.AreEqual(expected, vm.OpenCountOf(EMonster.Wisp));
        }

        [Test]
        public void 미조우_종족은_전부_닫히고_Lv범위는_클램프된다()
        {
            CodexViewModel closed = Vm(Profile(EMonster.Wisp, 5, 5, seen: false));
            Assert.AreEqual(0, closed.OpenCountOf(EMonster.Wisp));

            CodexViewModel clamped = Vm(Profile(EMonster.Wisp, 5, 99));
            Assert.AreEqual(20, clamped.OpenCountOf(EMonster.Wisp));
            Assert.AreEqual(5 * 4, clamped.CellsPerRace);
            Assert.AreEqual(120, clamped.TotalCount);
        }

        [Test]
        public void 미해금_칸도_실제_이름을_갖고_물음표가_없다()
        {
            CodexViewModel vm = Vm(new MetaProfile());
            foreach (CodexCellModel cell in vm.Cells)
            {
                Assert.IsFalse(cell.Open);
                Assert.IsFalse(string.IsNullOrEmpty(cell.Name));
                Assert.IsFalse(cell.Name.Contains("?"));
            }
            Assert.IsNull(vm.BuildDetail().StatsText);
        }

        [Test]
        public void 신규_칸은_스냅샷_대비로_잡히고_종족_표시_후_스냅샷이_갱신된다()
        {
            MetaProfile profile = Profile(EMonster.Wisp, 1, 0);
            CodexSession.EnsureCaptured(profile, _db);

            profile.SetShopLevel("Enhance_Wisp", 1);
            CodexViewModel vm = Vm(profile);

            Assert.AreEqual(EMonster.Wisp, vm.SelectedRace);
            Assert.IsTrue(vm.Cells[CodexRules.CellIndex(1, 1)].PlayUnlock);
            Assert.IsTrue(vm.Cells[CodexRules.CellIndex(1, 1)].IsNew);
            Assert.IsFalse(vm.HasUnseenNew(EMonster.Wisp));

            CodexViewModel reopened = Vm(profile);
            Assert.IsFalse(reopened.Cells[CodexRules.CellIndex(1, 1)].PlayUnlock);
        }

        private sealed class FakeSlots : ICodexFxSlots
        {
            public int Free = 3;
            public int Held;

            public bool TryAcquire(int grade)
            {
                if (Free <= 0)
                    return false;
                Free--;
                Held++;
                return true;
            }

            public void Release(int grade)
            {
                Free++;
                Held--;
            }
        }

        [Test]
        public void 이펙트_배분은_등급_내림차순_티어_오름차순_상한까지이고_전부_반환된다()
        {
            FakeSlots slots = new FakeSlots { Free = 10 };
            CodexFxAllocator allocator = new CodexFxAllocator(slots, 2);
            List<CodexFxAllocator.Candidate> list = new List<CodexFxAllocator.Candidate>
            {
                new CodexFxAllocator.Candidate(0, 1, 0),
                new CodexFxAllocator.Candidate(1, 3, 2),
                new CodexFxAllocator.Candidate(2, 5, 3),
                new CodexFxAllocator.Candidate(3, 5, 1),
            };

            HashSet<int> live = allocator.Rebalance(list);

            CollectionAssert.AreEquivalent(new[] { 3, 2 }, live);
            allocator.ReleaseAll();
            Assert.AreEqual(0, slots.Held);
        }

        [Test]
        public void 슬롯_확보가_실패한_칸은_건너뛰고_정지_프레임으로_남는다()
        {
            FakeSlots slots = new FakeSlots { Free = 1 };
            CodexFxAllocator allocator = new CodexFxAllocator(slots, 8);

            HashSet<int> live = allocator.Rebalance(new List<CodexFxAllocator.Candidate>
            {
                new CodexFxAllocator.Candidate(0, 4, 0),
                new CodexFxAllocator.Candidate(1, 3, 0),
            });

            Assert.AreEqual(1, live.Count);
            Assert.IsTrue(live.Contains(0));
        }
    }
}
