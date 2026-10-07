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
            Stage_Rule_TriggerRow[] triggers = null,
            HeroArchetype_SkirmishRow[] skirmishes = null)
        {
            return ChapterDatabase.Build(
                chapters ?? Load<ChapterRow>("Chapter"),
                stages ?? Load<StageRow>("Stage"),
                rules ?? Load<Stage_RuleRow>("Stage_Rule"),
                ruleParams ?? Load<Stage_Rule_ParamRow>("Stage_Rule_Param"),
                triggers ?? Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"),
                Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"),
                skirmishes ?? Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish"));
        }

        [Test]
        public void 실제_JSON으로_구축하면_챕터3_스테이지15_Ch1_배수가_기획서_값이다()
        {
            ChapterDatabase db = BuildFrom();

            Assert.AreEqual(3, db.ChapterCount);
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
        public void 실제_JSON_챕터3은_암살자_스테이지_배수가_기획서_5_3_값이다()
        {
            ChapterDatabase db = BuildFrom();

            float[] expectedHp = { 2.06f, 2.30f, 2.54f, 2.70f, 3.20f };
            float[] expectedPower = { 1.20f, 1.30f, 1.45f, 1.60f, 1.75f };
            int[] expectedClearSec = { 160, 180, 200, 220, 255 };
            for (int i = 0; i < 5; ++i)
            {
                StageRow stage = db.GetStage(3, i + 1);
                Assert.AreEqual(expectedHp[i], stage.hpMultiplier, 0.0001f, $"3-{i + 1} hp");
                Assert.AreEqual(expectedPower[i], stage.powerMultiplier, 0.0001f, $"3-{i + 1} power");
                Assert.AreEqual(expectedClearSec[i], stage.targetClearSec, $"3-{i + 1} targetClearSec");
            }
            Assert.AreEqual(1121, db.GetStage(3, 1).nameKey);
            Assert.AreEqual(1125, db.GetStage(3, 5).nameKey);
            Assert.AreEqual("Assassin", db.GetChapter(3).heroId);
            Assert.AreEqual("Battle_Backdrop_ShadowCitadel", db.GetChapter(3).backdropKey);
            Assert.AreEqual("Assassin", db.GetHeroOfLinear(11).heroId);
            Assert.AreEqual("Assassin", db.GetHeroOfLinear(15).heroId);
        }

        [Test]
        public void 실제_JSON_암살자_직업과_습격_설정은_기획서_2_2_값이다()
        {
            ChapterDatabase db = BuildFrom();

            HeroArchetypeRow hero = db.GetHero("Assassin");
            Assert.AreEqual(0.70f, hero.hpScale, 0.0001f);
            Assert.AreEqual(0.70f, hero.powerScale, 0.0001f);
            Assert.AreEqual(1.6f, hero.range, 0.0001f);
            Assert.AreEqual(0.7f, hero.cooldown, 0.0001f);
            Assert.AreEqual(4.2f, hero.moveSpeed, 0.0001f);
            Assert.AreEqual("Skirmisher", hero.aiType);

            HeroArchetype_SkirmishRow skirmish = db.GetSkirmish("Assassin");
            Assert.IsNotNull(skirmish);
            Assert.AreEqual(9.0f, skirmish.huntRadius, 0.0001f);
            Assert.AreEqual(1.5f, skirmish.retargetScoreMul, 0.0001f);
            Assert.AreEqual(2.8f, skirmish.strikeWindowSec, 0.0001f);
            Assert.AreEqual(3, skirmish.crowdCount);
            Assert.AreEqual(2.0f, skirmish.crowdRadius, 0.0001f);
            Assert.AreEqual(3.5f, skirmish.slipDistance, 0.0001f);
            Assert.AreEqual(0.2f, skirmish.slipDurationSec, 0.0001f);
            Assert.AreEqual(0.2f, skirmish.slipInvulnSec, 0.0001f);
            Assert.AreEqual(5.0f, skirmish.slipCooldownSec, 0.0001f);
            Assert.AreEqual(0.4f, skirmish.minDwellSec, 0.0001f);
            Assert.AreEqual(7.0f, skirmish.arenaSoftRadius, 0.0001f);
            Assert.IsNull(db.GetSkirmish("Knight"));
            Assert.IsNull(db.GetSkirmish("Archer"));
        }

        [Test]
        public void 실제_JSON_챕터3_규칙은_기획서_5_2_배정과_트리거를_따른다()
        {
            ChapterDatabase db = BuildFrom();

            Assert.AreEqual(0, db.GetRules(3, 1).Count, "3-1 은 규칙 없음");
            Assert.AreEqual(EStageRule.SealedNest, db.GetRules(3, 2)[0].RuleId);
            Assert.AreEqual((float)EMonster.Plague, db.GetRules(3, 2)[0].Params["lockedSpecies"], 0.0001f);
            Assert.AreEqual(0.60f, db.GetRules(3, 2)[0].Params["unlockHpFraction"], 0.0001f);
            Assert.AreEqual(EStageRule.HolySpring, db.GetRules(3, 3)[0].RuleId);
            Assert.AreEqual(EStageRule.SealedNest, db.GetRules(3, 4)[0].RuleId);
            Assert.AreEqual((float)EMonster.Hex, db.GetRules(3, 4)[0].Params["lockedSpecies"], 0.0001f);
            Assert.AreEqual(0.50f, db.GetRules(3, 4)[0].Params["unlockHpFraction"], 0.0001f);
            Assert.AreEqual(EStageRule.DenseFog, db.GetRules(3, 4)[1].RuleId);
            Assert.AreEqual(EStageRule.BossBulwark, db.GetRules(3, 5)[0].RuleId);
            Assert.AreEqual(0.75f, db.GetRules(3, 5)[0].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(EStageRule.HunterTraps, db.GetRules(3, 5)[1].RuleId);
            Assert.AreEqual(0.35f, db.GetRules(3, 5)[1].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(6f, db.GetRules(3, 5)[1].Params["trapCount"], 0.0001f);
            Assert.IsFalse(db.GetRules(3, 2)[0].HasTrigger);
        }

        [Test]
        public void Skirmisher_영웅에_습격_row가_없으면_로드_오류다()
        {
            ChapterDataException e = Assert.Throws<ChapterDataException>(
                () => BuildFrom(skirmishes: new HeroArchetype_SkirmishRow[0]));
            StringAssert.Contains("Skirmisher 인데 HeroArchetype_Skirmish row 가 없음", e.Message);
        }

        [Test]
        public void 습격_row가_Skirmisher_아닌_영웅을_가리키면_로드_오류다()
        {
            HeroArchetype_SkirmishRow[] rows = Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish");
            rows[0].heroId = "Knight";

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => BuildFrom(skirmishes: rows));
            StringAssert.Contains("aiType=Skirmisher 영웅에만 존재", e.Message);
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
        public void 챕터_heroId가_숫자_문자열이면_로드_오류다()
        {
            ChapterRow[] chapters = Load<ChapterRow>("Chapter");
            chapters[0].heroId = "0";

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => BuildFrom(chapters: chapters));
            StringAssert.Contains("heroId", e.Message);
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
