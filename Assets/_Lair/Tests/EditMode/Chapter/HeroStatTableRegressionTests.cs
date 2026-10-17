using Lair.Battle;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 영웅 스탯 15스테이지 전수 회귀 - ScaleHeroStat 의 decimal 정규화(0bcd619) 이후에도 기사/궁수/암살자 표 값이 한 칸도 어긋나지 않는다 (chapter-system §2.1·chapter-3-hero §3.2).
    public class HeroStatTableRegressionTests : ChapterStaticStateTestBase
    {
        private const int AnchorHp = 4000;
        private const int AnchorPower = 50;

        private static readonly int[] KnightHp = { 4000, 4400, 5440, 5840, 7680 };
        private static readonly int[] KnightPower = { 50, 55, 60, 68, 75 };
        private static readonly int[] ArcherHp = { 4992, 5696, 6720, 7008, 8160 };
        private static readonly int[] ArcherPower = { 44, 48, 52, 58, 64 };
        private static readonly int[] AssassinHp = { 5768, 6440, 7112, 7560, 8960 };
        private static readonly int[] AssassinPower = { 42, 46, 51, 56, 61 };

        private static void AssertTable(string heroId, int chapterId, int[] expectedHp, int[] expectedPower)
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            HeroArchetypeRow hero = db.GetHero(heroId);

            for (int index = 1; index <= 5; ++index)
            {
                HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, db.GetStage(chapterId, index));

                Assert.AreEqual(expectedHp[index - 1], stats.Hp, $"{heroId} {chapterId}-{index} HP");
                Assert.AreEqual(expectedPower[index - 1], stats.Power, $"{heroId} {chapterId}-{index} Power");
            }
        }

        [Test]
        public void 기사_챕터1_다섯_스테이지_HP와_공격력_표가_그대로다()
        {
            AssertTable("Knight", 1, KnightHp, KnightPower);
        }

        [Test]
        public void 궁수_챕터2_다섯_스테이지_HP와_공격력_표가_그대로다()
        {
            AssertTable("Archer", 2, ArcherHp, ArcherPower);
        }

        [Test]
        public void 암살자_챕터3_다섯_스테이지_HP와_공격력_표가_기획서_값이다()
        {
            AssertTable("Assassin", 3, AssassinHp, AssassinPower);
        }

        [Test]
        public void 선형_번호_1부터_15까지_영웅이_챕터_영웅으로_도출되고_스탯이_표와_같다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            int[][] hp = { KnightHp, ArcherHp, AssassinHp };
            int[][] power = { KnightPower, ArcherPower, AssassinPower };
            string[] heroes = { "Knight", "Archer", "Assassin" };

            for (int linear = 1; linear <= 15; ++linear)
            {
                int chapter = StageProgress.ChapterOf(linear);
                int index = StageProgress.IndexOf(linear);
                HeroArchetypeRow hero = db.GetHeroOfLinear(linear);
                HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, db.GetStageByLinear(linear));

                Assert.AreEqual(heroes[chapter - 1], hero.heroId, $"linear {linear}");
                Assert.AreEqual(hp[chapter - 1][index - 1], stats.Hp, $"linear {linear} HP");
                Assert.AreEqual(power[chapter - 1][index - 1], stats.Power, $"linear {linear} Power");
            }
        }

        [Test]
        public void 공격력_체력은_챕터_안에서_스테이지가_올라갈수록_단조_증가한다()
        {
            for (int chapter = 0; chapter < 3; ++chapter)
            {
                int[][] hp = { KnightHp, ArcherHp, AssassinHp };
                int[][] power = { KnightPower, ArcherPower, AssassinPower };
                for (int i = 1; i < 5; ++i)
                {
                    Assert.Greater(hp[chapter][i], hp[chapter][i - 1], $"챕터 {chapter + 1} HP {i}");
                    Assert.Greater(power[chapter][i], power[chapter][i - 1], $"챕터 {chapter + 1} Power {i}");
                }
            }
        }

        //# --- ScaleHeroStat decimal 정규화 ---

        [TestCase(50, 0.7f, 1.3f, 46)]
        [TestCase(50, 0.7f, 1.45f, 51)]
        [TestCase(50, 0.7f, 1.2f, 42)]
        [TestCase(50, 1.0f, 1.35f, 68)]
        [TestCase(50, 0.7f, 1.75f, 61)]
        [TestCase(4000, 0.8f, 1.56f, 4992)]
        [TestCase(4000, 0.8f, 1.78f, 5696)]
        [TestCase(4000, 0.7f, 2.06f, 5768)]
        public void 소수_곱이_정확히_0_5_경계인_조합도_올림으로_기획서_값이_나온다(int anchor, float scale, float multiplier, int expected)
        {
            Assert.AreEqual(expected, StageProgress.ScaleHeroStat(anchor, scale, multiplier));
        }

        [Test]
        public void 중간_반올림이_없어_한번만_반올림한다()
        {
            Assert.AreEqual(3, StageProgress.ScaleHeroStat(5, 0.5f, 1.2f), "5 x 0.5 x 1.2 = 3.0 (중간 반올림 5 x 0.5 -> 3 이면 4)");
        }

        [TestCase(0, 1f, 1f)]
        [TestCase(10, 0f, 1f)]
        [TestCase(10, 1f, 0f)]
        [TestCase(10, -1f, 1f)]
        [TestCase(1, 0.1f, 1f)]
        public void 결과가_1_미만이면_1로_보정한다(int anchor, float scale, float multiplier)
        {
            Assert.AreEqual(1, StageProgress.ScaleHeroStat(anchor, scale, multiplier));
        }

        [Test]
        public void 스케일_정수_경계에서는_정확히_정수가_나온다()
        {
            Assert.AreEqual(4000, StageProgress.ScaleHeroStat(4000, 1f, 1f));
            Assert.AreEqual(8000, StageProgress.ScaleHeroStat(4000, 1f, 2f));
            Assert.AreEqual(2800, StageProgress.ScaleHeroStat(4000, 0.7f, 1f));
        }

        [Test]
        public void 몬스터용_ScaleStat은_기존_float_경로로_변하지_않았다()
        {
            Assert.AreEqual(68, StageProgress.ScaleStat(50, 1.35f));
            Assert.AreEqual(3, StageProgress.ScaleStat(2, 1.25f));
            Assert.AreEqual(1, StageProgress.ScaleStat(1, 0.1f));
        }

        [Test]
        public void 앵커가_다른_값이어도_같은_공식으로_도출된다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            HeroStats stats = HeroStatCalculator.Compute(1000, 100, db.GetHero("Assassin"), db.GetStage(3, 5));

            Assert.AreEqual(2240, stats.Hp, "1000 x 0.7 x 3.2");
            Assert.AreEqual(123, stats.Power, "100 x 0.7 x 1.75 = 122.5 -> 123");
        }
    }
}
