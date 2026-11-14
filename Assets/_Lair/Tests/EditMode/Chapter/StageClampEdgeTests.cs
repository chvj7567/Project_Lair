using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# C1 — Stage.json heroScale·heroTier 와 ChapterDatabase.GetStageClamped 의 경계·단조성 (so-to-json-migration §3.11·§7.6).
    //# 클램프 상한은 db.ChapterCount x 5 다(StageProgress 의 정적 챕터 수가 아님) — 정적 상태를 만지는 케이스가 있어 격리 베이스를 쓴다.
    public class StageClampEdgeTests : ChapterStaticStateTestBase
    {
        //# 스테이지마다 크기·티어를 다르게 박아 "어느 스테이지가 골라졌는지" 를 값으로 식별한다(heroScale = 1 + 0.01 x 선형, heroTier = 인덱스 - 1).
        private static ChapterDataSet Labeled(int chapters)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            for (int chapter = 3; chapter <= chapters; chapter++)
            {
                set.AddChapter(chapter, "Knight", "Battle_Backdrop");
            }
            if (chapters < 2)
            {
                set.Chapters.RemoveAll(c => c.chapterId == 2);
                set.Stages.RemoveAll(s => s.chapterId == 2);
                set.Rules.RemoveAll(r => r.chapterId == 2);
                set.Params.RemoveAll(p => p.chapterId == 2);
                set.Triggers.RemoveAll(t => t.chapterId == 2);
            }
            foreach (StageRow stage in set.Stages)
            {
                stage.heroScale = LabelScale(stage.chapterId, stage.stageIndex);
                stage.heroTier = stage.stageIndex - 1;
            }
            return set;
        }

        //# 합성 row 값과 단정 기대값이 같은 float 로 나오도록 한 곳에서 계산한다(double 승격으로 마지막 자릿수가 어긋나는 것을 막는다).
        private static float LabelScale(int chapterId, int stageIndex)
        {
            float value = 1f + 0.01f * StageProgress.ToLinear(chapterId, stageIndex);
            return value;
        }

        private static void AssertStage(StageRow stage, int chapterId, int stageIndex, string label)
        {
            Assert.IsNotNull(stage, label);
            Assert.AreEqual(chapterId, stage.chapterId, label + " chapterId");
            Assert.AreEqual(stageIndex, stage.stageIndex, label + " stageIndex");
            Assert.AreEqual(LabelScale(chapterId, stageIndex), stage.heroScale, 0f, label + " heroScale");
            Assert.AreEqual(stageIndex - 1, stage.heroTier, label + " heroTier");
        }

        //# --- 클램프 경계 ---

        [TestCase(int.MinValue, 1, 1)]
        [TestCase(-10, 1, 1)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(3, 1, 3)]
        [TestCase(5, 1, 5)]
        [TestCase(6, 2, 1)]
        [TestCase(10, 2, 5)]
        [TestCase(11, 2, 5)]
        [TestCase(int.MaxValue, 2, 5)]
        public void 챕터_2개_DB의_클램프는_1_1과_2_5_사이다(int linear, int chapterId, int stageIndex)
        {
            ChapterDatabase db = Labeled(2).Build();

            AssertStage(db.GetStageClamped(linear), chapterId, stageIndex, "linear " + linear);
        }

        [TestCase(10, 2, 5)]
        [TestCase(11, 3, 1)]
        [TestCase(15, 3, 5)]
        [TestCase(16, 3, 5)]
        [TestCase(int.MaxValue, 3, 5)]
        public void 챕터가_3개로_늘면_상한이_15로_따라간다(int linear, int chapterId, int stageIndex)
        {
            ChapterDatabase db = Labeled(3).Build();

            Assert.AreEqual(3, db.ChapterCount);
            AssertStage(db.GetStageClamped(linear), chapterId, stageIndex, "linear " + linear);
        }

        [TestCase(1, 1, 1)]
        [TestCase(5, 1, 5)]
        [TestCase(6, 1, 5)]
        [TestCase(25, 1, 5)]
        [TestCase(int.MaxValue, 1, 5)]
        public void 챕터가_1개면_상한이_5다(int linear, int chapterId, int stageIndex)
        {
            ChapterDatabase db = Labeled(1).Build();

            Assert.AreEqual(1, db.ChapterCount);
            AssertStage(db.GetStageClamped(linear), chapterId, stageIndex, "linear " + linear);
        }

        //# 클램프 상한은 db 자신의 챕터 수 — 정적 StageProgress 챕터 수를 바꿔도 달라지지 않는다.
        [Test]
        public void 출하_5챕터_DB의_상한은_정적_챕터_수와_무관하게_5_5다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            StageProgress.ConfigureChapterCount(1);

            StageRow atMax = db.GetStageClamped(25);
            StageRow overMax = db.GetStageClamped(26);
            StageRow farOver = db.GetStageClamped(int.MaxValue);

            Assert.AreEqual(5, db.ChapterCount);
            Assert.AreEqual(5, atMax.chapterId);
            Assert.AreEqual(5, atMax.stageIndex);
            Assert.AreSame(atMax, overMax);
            Assert.AreSame(atMax, farOver);
            Assert.AreEqual(1, db.GetStageClamped(int.MinValue).chapterId);
            Assert.AreEqual(1, db.GetStageClamped(int.MinValue).stageIndex);
        }

        //# --- 단조성 ---

        [Test]
        public void 입력이_커질수록_선택_스테이지가_역행하지_않고_범위_안에서는_입력_그대로다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            int max = db.ChapterCount * StageProgress.StagesPerChapter;
            int previous = 0;

            for (int linear = -10; linear <= max + 15; linear++)
            {
                StageRow stage = db.GetStageClamped(linear);
                Assert.IsNotNull(stage, "linear " + linear);
                int selected = StageProgress.ToLinear(stage.chapterId, stage.stageIndex);

                Assert.GreaterOrEqual(selected, previous, "역행: linear " + linear);
                Assert.AreEqual(System.Math.Max(1, System.Math.Min(linear, max)), selected, "linear " + linear);
                previous = selected;
            }
        }

        [Test]
        public void 범위_안_입력은_GetStage와_같은_행_인스턴스를_돌려준다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();

            for (int linear = 1; linear <= 25; linear++)
            {
                StageRow clamped = db.GetStageClamped(linear);

                Assert.AreSame(db.GetStage(StageProgress.ChapterOf(linear), StageProgress.IndexOf(linear)), clamped, "linear " + linear);
            }
        }

        //# --- C1 값 검증 (heroScale > 0, heroTier 0~4) ---

        private static void AssertBuildFails(ChapterDataSet set, params string[] fragments)
        {
            BuildEdgeTestKit.AssertFails(() => set.Build(), fragments);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void 영웅_티어_0에서_4는_통과하고_그대로_조회된다(int tier)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages[2].heroTier = tier;

            ChapterDatabase db = set.Build();

            Assert.AreEqual(tier, db.GetStage(1, 3).heroTier);
        }

        [TestCase(5)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void 영웅_티어가_0에서_4_밖이면_위반이다(int tier)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages[2].heroTier = tier;

            AssertBuildFails(set, "Stage[1-3].heroTier " + tier + " 는 0~4 범위 밖");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(-0.0001f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        public void 영웅_크기_배율이_0_이하이거나_NaN이면_위반이다(float scale)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages[0].heroScale = scale;

            AssertBuildFails(set, "Stage[1-1].heroScale");
        }

        [TestCase(0.0001f)]
        [TestCase(1f)]
        [TestCase(1.4f)]
        [TestCase(100f)]
        public void 영웅_크기_배율이_0보다_크면_통과하고_그대로_조회된다(float scale)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages[0].heroScale = scale;

            Assert.AreEqual(scale, set.Build().GetStage(1, 1).heroScale, 0f);
        }

        [Test]
        public void 티어와_크기_위반이_여러_스테이지에_있으면_전부_모아_보고한다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages[0].heroScale = 0f;
            set.Stages[1].heroTier = 9;
            set.Stages[7].heroTier = -3;

            ChapterDataException e = BuildEdgeTestKit.Fails(() => set.Build());

            StringAssert.Contains("Stage[1-1].heroScale", e.Message);
            StringAssert.Contains("Stage[1-2].heroTier 9", e.Message);
            StringAssert.Contains("Stage[2-3].heroTier -3", e.Message);
            Assert.AreEqual(3, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }
    }

    //# §6.3 이중 진실 제거 — ChapterDatabase 는 보스 트리거가 피할 패시브 임계·스킬 게이트를 하드코딩하지 않고 인자로 받는다.
    public class ChapterDatabaseThresholdSourceTests : ChapterStaticStateTestBase
    {
        private static Stage_Rule_TriggerRow BossTrigger(ChapterDataSet set)
        {
            return set.Triggers.Find(t => t.chapterId == 1 && t.stageIndex == 5);
        }

        [Test]
        public void 기본_임계에서_트리거_0_55는_통과한다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();

            Assert.AreEqual(0.55f, BossTrigger(set).hpFraction, 0f);
            Assert.DoesNotThrow(() => set.Build());
        }

        [Test]
        public void 넘겨준_패시브_임계에_트리거_비율이_있으면_위반이다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.PassiveThresholds = new[] { 0.9f, 0.55f, 0.1f };

            BuildEdgeTestKit.AssertFails(() => set.Build(), "Stage_Rule_Trigger[", "스킬 게이트/패시브 임계와 일치");
        }

        [Test]
        public void 넘겨준_스킬_게이트에_트리거_비율이_있으면_위반이다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.SkillGateFractions = new[] { 0.55f };

            BuildEdgeTestKit.AssertFails(() => set.Build(), "스킬 게이트/패시브 임계와 일치");
        }

        [Test]
        public void 임계와의_일치는_1만분의_1_미만_차이까지만_본다()
        {
            ChapterDataSet near = ChapterDataSet.Valid();
            near.SkillGateFractions = new[] { 0.55005f };
            ChapterDataSet far = ChapterDataSet.Valid();
            far.SkillGateFractions = new[] { 0.5502f };

            BuildEdgeTestKit.AssertFails(() => near.Build(), "스킬 게이트/패시브 임계와 일치");
            Assert.DoesNotThrow(() => far.Build());
        }

        [Test]
        public void 임계_인자가_null이면_그_대조만_생략된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            BossTrigger(set).hpFraction = 0.5f;
            ChapterDataSet withDefaults = ChapterDataSet.Valid();
            BossTrigger(withDefaults).hpFraction = 0.5f;
            set.PassiveThresholds = null;
            set.SkillGateFractions = null;

            BuildEdgeTestKit.AssertFails(() => withDefaults.Build(), "스킬 게이트/패시브 임계와 일치");
            Assert.DoesNotThrow(() => set.Build());
        }

        //# 출하 BalanceConfig.passiveThresholds 와 HeroArchetype_Skill 게이트가 이전 하드코딩 값과 같아야 같은 트리거 검증 결과가 보존된다.
        [Test]
        public void 출하_데이터에서_넘어가는_임계는_이전_하드코딩_값과_같다()
        {
            float[] passive = WorldMapTestSupport.ShippedBalance().PassiveThresholds;
            float[] gates = HeroSkillDatabase.GateFractionsOf(WorldMapTestSupport.ShippedHeroSkillData());

            CollectionAssert.AreEqual(new[] { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f }, passive);
            CollectionAssert.AreEquivalent(new[] { 0.85f, 0.65f, 0.45f }, gates);
        }

        //# 출하 패시브 임계(0.5)에 걸리는 트리거는 기본 인자에서 위반이지만, 출하 BalanceConfig 를 넘겨도 똑같이 위반이다.
        [Test]
        public void 출하_패시브_임계를_넘겨도_0_5_트리거는_위반이다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            BossTrigger(set).hpFraction = 0.5f;
            set.PassiveThresholds = WorldMapTestSupport.ShippedBalance().PassiveThresholds;

            BuildEdgeTestKit.AssertFails(() => set.Build(), "스킬 게이트/패시브 임계와 일치");
        }

        [Test]
        public void Build는_정적_챕터_수와_Current를_바꾸지_않는다()
        {
            int before = StageProgress.ChapterCount;

            ChapterDataSet.Valid().Build();

            Assert.AreEqual(before, StageProgress.ChapterCount);
            Assert.IsNull(ChapterDatabase.Current);
        }
    }
}
