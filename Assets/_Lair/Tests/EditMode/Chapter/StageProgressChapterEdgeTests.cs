using Lair.Battle;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# StageProgress 선형 번호 변환/해금/상한 경계 (chapter-system §2.1). 챕터 수 정적 상태는 SetUp/TearDown 으로 격리.
    public class StageProgressChapterEdgeTests : ChapterStaticStateTestBase
    {
        [TestCase(1, 1, 1)]
        [TestCase(2, 1, 2)]
        [TestCase(4, 1, 4)]
        [TestCase(5, 1, 5)]
        [TestCase(6, 2, 1)]
        [TestCase(7, 2, 2)]
        [TestCase(9, 2, 4)]
        [TestCase(10, 2, 5)]
        [TestCase(11, 3, 1)]
        [TestCase(15, 3, 5)]
        public void 선형번호는_챕터와_인덱스로_분해된다(int linear, int chapter, int index)
        {
            Assert.AreEqual(chapter, StageProgress.ChapterOf(linear));
            Assert.AreEqual(index, StageProgress.IndexOf(linear));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-5)]
        public void 선형번호가_1미만이면_챕터1_인덱스1로_취급한다(int linear)
        {
            Assert.AreEqual(1, StageProgress.ChapterOf(linear));
            Assert.AreEqual(1, StageProgress.IndexOf(linear));
        }

        [Test]
        public void ToLinear_와_분해는_1부터_15까지_왕복이_일치한다()
        {
            for (int linear = 1; linear <= 15; ++linear)
            {
                int back = StageProgress.ToLinear(StageProgress.ChapterOf(linear), StageProgress.IndexOf(linear));

                Assert.AreEqual(linear, back, $"linear {linear}");
            }
        }

        [Test]
        public void ToLinear_는_챕터1_인덱스1이_1이고_챕터2_인덱스5가_10이다()
        {
            Assert.AreEqual(1, StageProgress.ToLinear(1, 1));
            Assert.AreEqual(5, StageProgress.ToLinear(1, 5));
            Assert.AreEqual(6, StageProgress.ToLinear(2, 1));
            Assert.AreEqual(10, StageProgress.ToLinear(2, 5));
        }

        [Test]
        public void 기본_챕터수는_2이고_MaxStage는_10이다()
        {
            Assert.AreEqual(2, StageProgress.ChapterCount);
            Assert.AreEqual(10, StageProgress.MaxStage);
        }

        [Test]
        public void ConfigureChapterCount_는_MaxStage를_챕터수_곱하기_5로_바꾼다()
        {
            StageProgress.ConfigureChapterCount(3);

            Assert.AreEqual(3, StageProgress.ChapterCount);
            Assert.AreEqual(15, StageProgress.MaxStage);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void ConfigureChapterCount_는_1미만이면_무시한다(int chapterCount)
        {
            StageProgress.ConfigureChapterCount(4);

            StageProgress.ConfigureChapterCount(chapterCount);

            Assert.AreEqual(4, StageProgress.ChapterCount);
            Assert.AreEqual(20, StageProgress.MaxStage);
        }

        [Test]
        public void ConfigureChapterCount_1이면_MaxStage_5이고_챕터2는_해금되지_않는다()
        {
            StageProgress.ConfigureChapterCount(1);

            Assert.AreEqual(5, StageProgress.MaxStage);
            Assert.AreEqual(5, StageProgress.ResolveClearedStage(0, 9));
            Assert.AreEqual(5, StageProgress.MaxPlayableStage(5));
        }

        [TestCase(0, 1, true)]
        [TestCase(0, 2, false)]
        [TestCase(4, 5, true)]
        [TestCase(4, 6, false)]
        [TestCase(5, 6, true)]
        [TestCase(5, 7, false)]
        [TestCase(9, 10, true)]
        [TestCase(10, 10, true)]
        [TestCase(10, 11, true)]
        [TestCase(10, 12, false)]
        public void IsUnlocked_는_챕터경계를_그대로_통과한다(int cleared, int stage, bool expected)
        {
            Assert.AreEqual(expected, StageProgress.IsUnlocked(stage, cleared));
        }

        [TestCase(1, 0, true)]
        [TestCase(2, 0, false)]
        [TestCase(2, 4, false)]
        [TestCase(2, 5, true)]
        [TestCase(2, 10, true)]
        [TestCase(3, 9, false)]
        [TestCase(3, 10, true)]
        public void IsChapterUnlocked_는_그_챕터_1스테이지의_해금과_같다(int chapterId, int cleared, bool expected)
        {
            Assert.AreEqual(expected, StageProgress.IsChapterUnlocked(chapterId, cleared));
        }

        [TestCase(0, 0, 0)]
        [TestCase(0, 5, 5)]
        [TestCase(5, 6, 6)]
        [TestCase(9, 10, 10)]
        [TestCase(10, 11, 10)]
        [TestCase(10, 99, 10)]
        [TestCase(7, 3, 7)]
        [TestCase(-1, 2, 2)]
        [TestCase(10, int.MaxValue, 10)]
        public void ResolveClearedStage_는_MaxStage_10을_상한으로_갱신한다(int cleared, int justCleared, int expected)
        {
            Assert.AreEqual(expected, StageProgress.ResolveClearedStage(cleared, justCleared));
        }

        [Test]
        public void ResolveClearedStage_상한은_챕터수에_따라_움직인다()
        {
            StageProgress.ConfigureChapterCount(3);

            Assert.AreEqual(15, StageProgress.ResolveClearedStage(10, 99));
            Assert.AreEqual(11, StageProgress.ResolveClearedStage(10, 11));
        }

        [TestCase(-3, 1)]
        [TestCase(0, 1)]
        [TestCase(4, 5)]
        [TestCase(5, 6)]
        [TestCase(9, 10)]
        [TestCase(10, 10)]
        [TestCase(11, 10)]
        [TestCase(999, 10)]
        public void MaxPlayableStage_는_1_하한_MaxStage_상한이다(int cleared, int expected)
        {
            Assert.AreEqual(expected, StageProgress.MaxPlayableStage(cleared));
        }

        [TestCase(1, false)]
        [TestCase(4, false)]
        [TestCase(5, true)]
        [TestCase(6, false)]
        [TestCase(0, false)]
        public void IsBossStage_는_챕터내_인덱스_5만_보스다(int stageIndex, bool expected)
        {
            Assert.AreEqual(expected, StageProgress.IsBossStage(stageIndex));
        }

        [Test]
        public void 선형번호_10을_IsBossStage에_그대로_넣으면_보스가_아니다_인덱스로_변환해야_한다()
        {
            Assert.IsFalse(StageProgress.IsBossStage(10));
            Assert.IsTrue(StageProgress.IsBossStage(StageProgress.IndexOf(10)));
            Assert.IsTrue(StageProgress.IsBossStage(StageProgress.IndexOf(5)));
        }

        [Test]
        public void ScaleHeroStat_은_중간_반올림_없이_한번만_반올림한다()
        {
            //# 5 x (0.5 x 1.2) = 3.0 → 3. 중간 반올림(5 x 0.5 = 2.5 → 3, 3 x 1.2 = 3.6 → 4)이면 4 가 된다.
            Assert.AreEqual(3, StageProgress.ScaleHeroStat(5, 0.5f, 1.2f));
            Assert.AreEqual(4, StageProgress.ScaleStat(StageProgress.ScaleStat(5, 0.5f), 1.2f), "이중 반올림이면 달라지는 기준 케이스");
        }

        [Test]
        public void ScaleHeroStat_은_0_5_경계에서_올림하고_최소_1을_보장한다()
        {
            Assert.AreEqual(3, StageProgress.ScaleHeroStat(2, 1f, 1.25f), "2.5 → 3");
            Assert.AreEqual(1, StageProgress.ScaleHeroStat(1, 0.1f, 1f), "0.1 → 최소 1");
            Assert.AreEqual(1, StageProgress.ScaleHeroStat(4000, 0f, 1.5f), "배율 0 → 최소 1");
        }
    }
}
