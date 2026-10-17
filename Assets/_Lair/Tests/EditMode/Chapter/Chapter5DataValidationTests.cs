using Lair.Battle;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 챕터 5(성기사) 데이터 슬라이스 (a) 자체 스모크 — chapter-5 §2·§5·§7.4. 본격 스위트는 test-engineer.
    public class Chapter5DataValidationTests : ChapterStaticStateTestBase
    {
        private static HeroArchetypeRow Paladin()
        {
            return new HeroArchetypeRow { heroId = "Paladin", nameKey = 1025, hpScale = 1.1f, powerScale = 0.85f, range = 1.6f, cooldown = 1.1f, moveSpeed = 2.9f, aiType = "Guardian" };
        }

        [Test]
        public void 실제_JSON_챕터5는_성기사_스테이지_규칙_응징_저항_설정을_갖고_구조_최대치_25다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            StageProgress.ConfigureChapterCount(db.ChapterCount);

            Assert.AreEqual(5, db.ChapterCount);
            Assert.AreEqual(25, StageProgress.MaxStage);
            Assert.AreEqual("Paladin", db.GetChapter(5).heroId);
            Assert.AreEqual("Battle_Backdrop_FrostCathedral", db.GetChapter(5).backdropKey);
            Assert.AreEqual(1.56f, db.GetStage(5, 1).hpMultiplier, 0.0001f);
            Assert.AreEqual(2.25f, db.GetStage(5, 5).hpMultiplier, 0.0001f);
            Assert.AreEqual(1.95f, db.GetStage(5, 5).powerMultiplier, 0.0001f);
            Assert.AreEqual(0, db.GetRules(5, 1).Count);
            Assert.AreEqual(EStageRule.SealedNest, db.GetRules(5, 2)[0].RuleId);
            Assert.AreEqual(EStageRule.DenseFog, db.GetRules(5, 3)[0].RuleId);
            Assert.AreEqual(2, db.GetRules(5, 4).Count);
            Assert.AreEqual(EStageRule.HunterTraps, db.GetRules(5, 5)[0].RuleId);
            Assert.AreEqual(0.75f, db.GetRules(5, 5)[0].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(EStageRule.BossBulwark, db.GetRules(5, 5)[1].RuleId);
            Assert.AreEqual(0.35f, db.GetRules(5, 5)[1].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(2.0f, db.GetGuard("Paladin").retributionLockSec, 0.0001f);
            Assert.AreEqual(0.6f, db.GetDebuffResist("Paladin").durationMul, 0.0001f);
            Assert.AreEqual(0.6f, db.GetDebuffResist("Paladin").penaltyMul, 0.0001f);
            Assert.IsNull(db.GetGuard("Knight"));
            Assert.IsNull(db.GetDebuffResist("Berserker"));
            Assert.AreEqual(4, ChapterGate.PlayableChapterCount, "게이트 상향은 분할 (d)");
        }

        [Test]
        public void Guardian인데_Guard_row가_없거나_Guard가_Guardian이_아닌_영웅에_있으면_로드_오류다()
        {
            ChapterDataSet missing = ChapterDataSet.Valid();
            missing.Heroes.Add(Paladin());
            ChapterDataException e1 = Assert.Throws<ChapterDataException>(() => missing.Build());
            StringAssert.Contains("Guardian 인데 HeroArchetype_Guard row 가 없음", e1.Message);

            ChapterDataSet wrongAi = ChapterDataSet.Valid();
            wrongAi.Guards.Add(new HeroArchetype_GuardRow { heroId = "Knight", retributionLockSec = 2f });
            ChapterDataException e2 = Assert.Throws<ChapterDataException>(() => wrongAi.Build());
            StringAssert.Contains("aiType=Guardian 영웅에만", e2.Message);
        }

        [Test]
        public void Guard_DebuffResist_값_범위와_FK_위반은_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes.Add(Paladin());
            set.Guards.Add(new HeroArchetype_GuardRow { heroId = "Paladin", retributionLockSec = 0f });
            set.DebuffResists.Add(new HeroArchetype_DebuffResistRow { heroId = "Paladin", durationMul = 1.2f, penaltyMul = 0.6f });
            set.DebuffResists.Add(new HeroArchetype_DebuffResistRow { heroId = "Ghost", durationMul = 0.6f, penaltyMul = 0.6f });

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());

            StringAssert.Contains("retributionLockSec 는 0 보다 커야 함", e.Message);
            StringAssert.Contains("HeroArchetype_DebuffResist[Paladin] durationMul", e.Message);
            StringAssert.Contains("HeroArchetype_DebuffResist[Ghost] 의 heroId 가 HeroArchetype 에 없음", e.Message);
        }

        [Test]
        public void 정상_Guard_DebuffResist_묶음은_구축된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes.Add(Paladin());
            set.Guards.Add(new HeroArchetype_GuardRow { heroId = "Paladin", retributionLockSec = 2f });
            set.DebuffResists.Add(new HeroArchetype_DebuffResistRow { heroId = "Paladin", durationMul = 0.6f, penaltyMul = 0f });

            ChapterDatabase db = set.Build();

            Assert.AreEqual(2f, db.GetGuard("Paladin").retributionLockSec, 0.0001f);
            Assert.AreEqual(0f, db.GetDebuffResist("Paladin").penaltyMul, 0.0001f);
        }

        [Test]
        public void 보스_트리거_0_70은_패시브_임계와_같아_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.AddRule(1, 3, 1, "BossBulwark", 0.7f);

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());

            StringAssert.Contains("스킬 게이트/패시브 임계와 일치", e.Message);
        }
    }
}
