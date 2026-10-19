using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 난이도 선택 1차 — 구현자 자체 최소 검증(정상 + 엣지 1). 본격 스위트는 test-engineer.
    public class DifficultySelectBasicTests
    {
        private static DifficultyRow[] Rows()
        {
            return new[]
            {
                new DifficultyRow { difficulty = 0, nameKey = 1327, hpMultiplier = 1.0f, powerMultiplier = 1.0f },
                new DifficultyRow { difficulty = 1, nameKey = 1328, hpMultiplier = 1.15f, powerMultiplier = 1.1f },
                new DifficultyRow { difficulty = 2, nameKey = 1329, hpMultiplier = 1.3f, powerMultiplier = 1.22f },
            };
        }

        [Test]
        public void 미클리어는_Easy만_클리어하면_Normal_Normal_클리어하면_Hard_상한은_Hard()
        {
            Assert.AreEqual(0, StageProgress.MaxSelectableDifficulty(3, 2, 0, 2));
            Assert.AreEqual(1, StageProgress.MaxSelectableDifficulty(3, 3, 0, 2));
            Assert.AreEqual(2, StageProgress.MaxSelectableDifficulty(3, 3, 1, 2));
            Assert.AreEqual(2, StageProgress.MaxSelectableDifficulty(3, 3, 2, 2));
        }

        [Test]
        public void 해금_범위를_넘은_난이도는_읽기에서_클램프되고_프로필은_그대로다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 2, SelectedDifficulty = 2 };

            Assert.AreEqual(1, p.ClampDifficulty(p.SelectedDifficulty, 2, 2));
            Assert.AreEqual(0, p.ClampDifficulty(2, 3, 2));
            Assert.AreEqual(2, p.SelectedDifficulty);
        }

        [Test]
        public void 낮은_난이도_재클리어는_최고_난이도를_내리지_않고_Easy는_행을_만들지_않는다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordDifficultyClear(1, 2);
            p.RecordDifficultyClear(1, 1);
            p.RecordDifficultyClear(2, 0);

            Assert.AreEqual(2, p.GetMaxClearedDifficulty(1));
            Assert.AreEqual(1, p.DifficultyClears.Count);
        }

        [Test]
        public void Normal_판은_Runs_Wins만_올리고_최단은_건드리지_않는다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, true, 100f, false);

            Assert.AreEqual(1, p.GetStageRecord(1).Wins);
            Assert.AreEqual(-1f, p.GetStageRecord(1).BestClearTime);
        }

        [Test]
        public void 마이그레이션은_범위_밖_행을_제거하고_중복은_최댓값으로_병합한다()
        {
            MetaProfile p = new MetaProfile { Version = 6, ClearedStage = 3, SelectedDifficulty = -1 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 1 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 2 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 5, MaxDifficulty = 1 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 0 });

            MetaProfileMigration.Apply(p, (System.Func<int, string>)(linear => "Knight"));

            Assert.AreEqual(7, p.Version);
            Assert.AreEqual(0, p.SelectedDifficulty);
            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(2));
        }

        [Test]
        public void v6_세이브는_챕터_데이터_미로드_상태에서_진행_범위와_난이도_행을_유지한다()
        {
            MetaProfile p = new MetaProfile { Version = 6, ClearedStage = 14, SelectedStage = 13 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 12, MaxDifficulty = 1 });

            MetaProfileMigration.Apply(p, (System.Func<int, string>)null);

            Assert.AreEqual(7, p.Version);
            Assert.AreEqual(14, p.ClearedStage);
            Assert.AreEqual(13, p.SelectedStage);
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(12));
        }

        [Test]
        public void 난이도_배율은_곱을_1회만_반올림한다()
        {
            Assert.AreEqual(68, StageProgress.ScaleHeroStat(50, 1f, 1.35f, 1f));
            Assert.AreEqual(100, StageProgress.ScaleHeroStat(100, 1f, 1f, 1f));
            Assert.AreEqual(115, StageProgress.ScaleHeroStat(100, 1f, 1f, 1.15f));
        }

        [Test]
        public void 배율표는_3행_Easy_1_단조를_검증하고_위반은_오류다()
        {
            Assert.AreEqual(2, DifficultyTable.Build(Rows(), id => true).MaxIndex);

            DifficultyRow[] twoRows = { Rows()[0], Rows()[1] };
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(twoRows));
            DifficultyRow[] reversed = Rows();
            reversed[2].hpMultiplier = 1.05f;
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(reversed));
        }
    }
}
