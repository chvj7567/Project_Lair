using System;
using System.IO;
using System.Linq;
using ChvjUnityInfra;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 챕터 데이터 검증/조회 — 실제 shipped JSON 7종 기준 (chapter-system §1·§7.3). gameplay-programmer 자체 스모크.
    public class ChapterDatabaseTests
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        private static ChapterDatabase BuildFrom(
            ChapterRow[] chapters = null,
            StageRow[] stages = null,
            Stage_RuleRow[] rules = null,
            Stage_Rule_ParamRow[] ruleParams = null,
            Stage_Rule_TriggerRow[] triggers = null)
        {
            return ChapterDatabase.Build(
                chapters ?? Load<ChapterRow>("Chapter"),
                stages ?? Load<StageRow>("Stage"),
                rules ?? Load<Stage_RuleRow>("Stage_Rule"),
                ruleParams ?? Load<Stage_Rule_ParamRow>("Stage_Rule_Param"),
                triggers ?? Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"),
                Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"));
        }

        [Test]
        public void 실제_JSON으로_구축하면_챕터2_스테이지10_Ch1_배수가_기획서_값이다()
        {
            ChapterDatabase db = BuildFrom();

            Assert.AreEqual(2, db.ChapterCount);
            float[] expectedHp = { 1.00f, 1.10f, 1.36f, 1.46f, 1.92f };
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(expectedHp[i], db.GetStage(1, i + 1).hpMultiplier, 0.0001f, $"1-{i + 1} hp");
            }
            Assert.AreEqual(2.55f, db.GetStage(2, 5).hpMultiplier, 0.0001f);
            Assert.AreEqual("Archer", db.GetHeroOfLinear(7).heroId);
            Assert.IsNotNull(db.GetKite("Archer"));
            Assert.IsNull(db.GetKite("Knight"));
        }

        [Test]
        public void 규칙_조회는_파라미터와_트리거를_합쳐_돌려준다()
        {
            ChapterDatabase db = BuildFrom();

            Assert.AreEqual(0, db.GetRules(1, 1).Count, "규칙 없는 스테이지는 빈 목록");
            Assert.AreEqual(2, db.GetRules(2, 5).Count);
            StageRuleData bulwark = db.GetRules(1, 5)[0];
            Assert.AreEqual(EStageRule.BossBulwark, bulwark.RuleId);
            Assert.IsTrue(bulwark.HasTrigger);
            Assert.AreEqual(0.55f, bulwark.TriggerHpFraction, 0.0001f);
            Assert.AreEqual(0.6f, bulwark.Params["damageTakenMul"], 0.0001f);
            Assert.IsFalse(db.GetRules(1, 2)[0].HasTrigger, "상시 규칙은 트리거 row 없음");
        }

        [Test]
        public void 챕터의_스테이지가_4개뿐이면_로드_오류다()
        {
            StageRow[] stages = Load<StageRow>("Stage").Where(s => s.chapterId != 2 || s.stageIndex != 5).ToArray();

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => BuildFrom(stages: stages));
            StringAssert.Contains("챕터 2 의 스테이지 5", e.Message);
        }

        [Test]
        public void 트리거_hpFraction이_스킬게이트와_같으면_로드_오류다()
        {
            Stage_Rule_TriggerRow[] triggers = Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger");
            triggers[0].hpFraction = 0.85f;

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => BuildFrom(triggers: triggers));
            StringAssert.Contains("스킬 게이트/패시브 임계", e.Message);
        }

        [Test]
        public void 규칙_파라미터_필수키가_누락되면_로드_오류다()
        {
            Stage_Rule_ParamRow[] ruleParams = Load<Stage_Rule_ParamRow>("Stage_Rule_Param")
                .Where(p => p.paramKey != "minRange").ToArray();

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => BuildFrom(ruleParams: ruleParams));
            StringAssert.Contains("필수 키 누락: minRange", e.Message);
        }
    }
}
