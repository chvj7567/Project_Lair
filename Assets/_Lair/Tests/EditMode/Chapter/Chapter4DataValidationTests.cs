using Lair.Battle;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 챕터 4(광전사) 데이터 슬라이스 (a) 자체 스모크 — chapter-4 §2·§5·§7.4. 본격 스위트는 test-engineer.
    public class Chapter4DataValidationTests : ChapterStaticStateTestBase
    {
        private static HeroArchetypeRow Berserker()
        {
            return new HeroArchetypeRow { heroId = "Berserker", nameKey = 1024, hpScale = 1.25f, powerScale = 0.6f, range = 1.8f, cooldown = 1.2f, moveSpeed = 3.4f, aiType = "Charger" };
        }

        private static HeroArchetype_ChargeRow BerserkerCharge()
        {
            return new HeroArchetype_ChargeRow { heroId = "Berserker", huntRadius = 10f, clusterRadius = 2.2f, retargetCountMargin = 2, retargetIntervalSec = 1f, minDwellSec = 0.4f, arenaSoftRadius = 7f };
        }

        [Test]
        public void 실제_JSON_챕터4는_광전사_스테이지_규칙_돌진_베기_설정을_갖고_챕터4_행은_보존된다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            StageProgress.ConfigureChapterCount(db.ChapterCount);

            Assert.AreEqual(ChapterGate.PlayableChapterCount, db.ChapterCount);
            Assert.AreEqual(ChapterGate.PlayableStageCount, StageProgress.MaxStage);
            Assert.AreEqual("Berserker", db.GetChapter(4).heroId);
            Assert.AreEqual("Battle_Backdrop_EmberForge", db.GetChapter(4).backdropKey);
            Assert.AreEqual(1.87f, db.GetStage(4, 5).hpMultiplier, 0.0001f);
            Assert.AreEqual(1.85f, db.GetStage(4, 5).powerMultiplier, 0.0001f);
            Assert.AreEqual(0, db.GetRules(4, 1).Count);
            Assert.AreEqual(EStageRule.SealedNest, db.GetRules(4, 2)[0].RuleId);
            Assert.AreEqual(2, db.GetRules(4, 4).Count);
            Assert.AreEqual(0.55f, db.GetRules(4, 5)[0].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(0.35f, db.GetRules(4, 5)[1].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(2.2f, db.GetCharge("Berserker").clusterRadius, 0.0001f);
            Assert.AreEqual(4, db.GetCleave("Berserker").cleaveMaxTargets);
            Assert.IsNull(db.GetCharge("Knight"));
            Assert.IsNull(db.GetCleave("Knight"));
        }

        [Test]
        public void Charger인데_Charge_row가_없거나_Charge가_Charger가_아닌_영웅에_있으면_로드_오류다()
        {
            ChapterDataSet missing = ChapterDataSet.Valid();
            missing.Heroes.Add(Berserker());
            ChapterDataException e1 = Assert.Throws<ChapterDataException>(() => missing.Build());
            StringAssert.Contains("Charger 인데 HeroArchetype_Charge row 가 없음", e1.Message);

            ChapterDataSet wrongAi = ChapterDataSet.Valid();
            wrongAi.Charges.Add(new HeroArchetype_ChargeRow { heroId = "Knight", huntRadius = 1f, clusterRadius = 1f, retargetCountMargin = 1, retargetIntervalSec = 1f, arenaSoftRadius = 1f });
            ChapterDataException e2 = Assert.Throws<ChapterDataException>(() => wrongAi.Build());
            StringAssert.Contains("aiType=Charger 영웅에만", e2.Message);
        }

        [Test]
        public void 정상_Charge_Cleave_묶음은_구축된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes.Add(Berserker());
            set.Charges.Add(BerserkerCharge());
            set.Cleaves.Add(new HeroArchetype_CleaveRow { heroId = "Berserker", cleaveRadius = 2.2f, cleaveMaxTargets = 4, cleaveSecondaryMul = 0.5f });

            ChapterDatabase db = set.Build();

            Assert.AreEqual(4, db.GetCleave("Berserker").cleaveMaxTargets);
        }
    }
}
