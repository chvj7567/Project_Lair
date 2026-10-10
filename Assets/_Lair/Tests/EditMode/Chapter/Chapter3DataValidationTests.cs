using System.Collections.Generic;
using System.Linq;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 챕터 3(암살자) 데이터 검증 보강 (chapter-3-hero §2·§5·§7.3) - HeroArchetype_Skirmish 로더/검증 위반 경로, 챕터 3 row 구조, 규칙 16건·트리거 임계.
    //# 스모크(ChapterDatabaseTests)가 다루는 실제 값(스테이지 배수·직업·습격 12필드·규칙 배정)은 중복하지 않는다.
    public class Chapter3DataValidationTests : ChapterStaticStateTestBase
    {
        //# Knight/Archer + Assassin(Skirmisher) + 챕터 3 까지 갖춘 정상 묶음.
        private static ChapterDataSet ValidWithAssassin()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes.Add(Assassin());
            set.Skirmishes.Add(AssassinSkirmish());
            set.AddChapter(3, "Assassin", "Battle_Backdrop_ShadowCitadel");
            return set;
        }

        private static HeroArchetypeRow Assassin()
        {
            return new HeroArchetypeRow { heroId = "Assassin", nameKey = 1023, hpScale = 0.7f, powerScale = 0.7f, range = 1.6f, cooldown = 0.7f, moveSpeed = 4.2f, aiType = "Skirmisher" };
        }

        private static HeroArchetype_SkirmishRow AssassinSkirmish()
        {
            return new HeroArchetype_SkirmishRow
            {
                heroId = "Assassin", huntRadius = 9f, retargetScoreMul = 1.5f, strikeWindowSec = 2.8f, crowdCount = 3, crowdRadius = 2f,
                slipDistance = 3.5f, slipDurationSec = 0.2f, slipInvulnSec = 0.2f, slipCooldownSec = 5f, minDwellSec = 0.4f, arenaSoftRadius = 7f,
            };
        }

        private static void AssertBuildFails(ChapterDataSet set, params string[] fragments)
        {
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());
            foreach (string fragment in fragments)
            {
                StringAssert.Contains(fragment, e.Message);
            }
        }

        //# --- 정상 ---

        [Test]
        public void 암살자와_챕터3을_갖춘_메모리_묶음은_구축되고_습격_설정이_조회된다()
        {
            ChapterDatabase db = ValidWithAssassin().Build();

            Assert.AreEqual(3, db.ChapterCount);
            Assert.IsNotNull(db.GetSkirmish("Assassin"));
            Assert.AreEqual("Assassin", db.GetHeroOfLinear(13).heroId);
            Assert.IsNull(db.GetKite("Assassin"), "Skirmisher 는 카이트 설정을 갖지 않는다");
        }

        [Test]
        public void 습격_row_배열이_null이어도_Skirmisher가_없으면_구축된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();

            ChapterDatabase db = ChapterDatabase.Build(
                set.Chapters.ToArray(), set.Stages.ToArray(), set.Rules.ToArray(), set.Params.ToArray(),
                set.Triggers.ToArray(), set.Heroes.ToArray(), set.Kites.ToArray(), null);

            Assert.IsNull(db.GetSkirmish("Archer"));
        }

        [Test]
        public void GetSkirmish는_null과_없는_heroId에_null을_돌려준다()
        {
            ChapterDatabase db = ValidWithAssassin().Build();

            Assert.IsNull(db.GetSkirmish(null));
            Assert.IsNull(db.GetSkirmish("Paladin"));
        }

        //# --- 습격 row 위반 ---

        [Test]
        public void 습격_heroId가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Skirmishes.Add(AssassinSkirmish());

            AssertBuildFails(set, "HeroArchetype_Skirmish.heroId 중복/누락: Assassin");
        }

        [Test]
        public void 습격_heroId가_null이면_로드_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            HeroArchetype_SkirmishRow broken = AssassinSkirmish();
            broken.heroId = null;
            set.Skirmishes.Add(broken);

            AssertBuildFails(set, "HeroArchetype_Skirmish.heroId 중복/누락");
        }

        [Test]
        public void 영웅_테이블에_없는_heroId의_습격_row는_FK_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            HeroArchetype_SkirmishRow orphan = AssassinSkirmish();
            orphan.heroId = "Ghost";
            set.Skirmishes.Add(orphan);

            AssertBuildFails(set, "HeroArchetype_Skirmish[Ghost] 의 heroId 가 HeroArchetype 에 없음");
        }

        [Test]
        public void Kiter_영웅에_습격_row가_붙으면_로드_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            HeroArchetype_SkirmishRow wrong = AssassinSkirmish();
            wrong.heroId = "Archer";
            set.Skirmishes.Add(wrong);

            AssertBuildFails(set, "HeroArchetype_Skirmish[Archer] 는 aiType=Skirmisher 영웅에만 존재해야 함 (현재 Kiter)");
        }

        [Test]
        public void Melee_영웅에_습격_row가_붙으면_현재_aiType이_메시지에_나온다()
        {
            ChapterDataSet set = ValidWithAssassin();
            HeroArchetype_SkirmishRow wrong = AssassinSkirmish();
            wrong.heroId = "Knight";
            set.Skirmishes.Add(wrong);

            AssertBuildFails(set, "(현재 Melee)");
        }

        [Test]
        public void Skirmisher_영웅의_습격_row_누락은_영웅_id가_메시지에_나온다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Skirmishes.Clear();

            AssertBuildFails(set, "HeroArchetype[Assassin] 는 Skirmisher 인데 HeroArchetype_Skirmish row 가 없음");
        }

        [Test]
        public void Kiter_영웅에_카이트_row가_없으면_Skirmish_추가_후에도_여전히_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Kites.Clear();

            AssertBuildFails(set, "HeroArchetype[Archer] 는 Kiter 인데 HeroArchetype_Kite row 가 없음");
        }

        [TestCase("skirmisher")]
        [TestCase("2")]
        [TestCase("Skirmish")]
        public void aiType이_EHeroAi_값명이_아니면_암살자도_로드_오류다(string aiType)
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Heroes.Single(h => h.heroId == "Assassin").aiType = aiType;

            AssertBuildFails(set, "HeroArchetype[Assassin].aiType 가 EHeroAi 에 없음");
        }

        [Test]
        public void 위반이_카이트와_습격에_동시에_있으면_두_오류가_한_예외에_모두_나온다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Kites.Clear();
            set.Skirmishes.Clear();

            AssertBuildFails(set, "Kiter 인데 HeroArchetype_Kite row 가 없음", "Skirmisher 인데 HeroArchetype_Skirmish row 가 없음");
        }

        //# --- 챕터 3 row 구조 ---

        [Test]
        public void 챕터3_heroId가_영웅_테이블에_없으면_FK_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Chapters.Single(c => c.chapterId == 3).heroId = "Ninja";

            AssertBuildFails(set, "Chapter[3].heroId 가 HeroArchetype 에 없음: Ninja");
        }

        [Test]
        public void 챕터3_backdropKey가_EBackdrop에_없으면_로드_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Chapters.Single(c => c.chapterId == 3).backdropKey = "Battle_Backdrop_Citadel";

            AssertBuildFails(set, "Chapter[3].backdropKey 가 EBackdrop 에 없음");
        }

        [Test]
        public void 챕터3_스테이지가_4개뿐이면_누락_인덱스가_나온다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Stages.RemoveAll(s => s.chapterId == 3 && s.stageIndex == 3);

            AssertBuildFails(set, "챕터 3 의 스테이지 3 가 없음");
        }

        [Test]
        public void 챕터3_스테이지_복합키가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Stages.Add(new StageRow { chapterId = 3, stageIndex = 5 });

            AssertBuildFails(set, "Stage 복합키 중복: 3-5");
        }

        [Test]
        public void 챕터3이_있는데_챕터2가_빠지면_연속이_아니라_로드_오류다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.Chapters.RemoveAll(c => c.chapterId == 2);
            set.Stages.RemoveAll(s => s.chapterId == 2);
            set.Rules.RemoveAll(r => r.chapterId == 2);
            set.Params.RemoveAll(p => p.chapterId == 2);
            set.Triggers.RemoveAll(t => t.chapterId == 2);

            AssertBuildFails(set, "1부터 연속");
        }

        [Test]
        public void 챕터4를_데이터에_추가해도_행수만큼_챕터_수가_늘고_선형_변환은_20까지_이어진다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.AddChapter(4, "Knight", "Battle_Backdrop");

            ChapterDatabase db = set.Build();

            Assert.AreEqual(4, db.ChapterCount);
            Assert.AreEqual("Knight", db.GetHeroOfLinear(20).heroId);
            Assert.IsNull(db.GetStageByLinear(21));
        }

        //# --- 규칙 16건 / 트리거 ---

        private static int CountRules(ChapterDatabase db, out int triggered)
        {
            int total = 0;
            triggered = 0;
            foreach (ChapterRow chapter in db.Chapters)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    foreach (StageRuleData data in db.GetRules(chapter.chapterId, stage))
                    {
                        ++total;
                        if (data.HasTrigger)
                        {
                            ++triggered;
                        }
                    }
                }
            }
            return total;
        }

        [Test]
        public void 실제_JSON의_규칙은_챕터3까지_16건이고_트리거_규칙은_5건이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            int total = CountRules(db, out int triggered);

            Assert.AreEqual(16, total);
            Assert.AreEqual(5, triggered);
        }

        [Test]
        public void 실제_JSON의_챕터별_규칙_수는_4_6_6이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            int[] perChapter = new int[3];

            for (int chapter = 1; chapter <= 3; ++chapter)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    perChapter[chapter - 1] += db.GetRules(chapter, stage).Count;
                }
            }

            CollectionAssert.AreEqual(new[] { 4, 6, 6 }, perChapter);
        }

        [Test]
        public void 챕터3_보스_트리거는_스킬_게이트와_패시브_임계에서_0_05_이상_떨어져_있다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            float[] gates = { 0.85f, 0.65f, 0.45f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

            foreach (StageRuleData data in db.GetRules(3, 5))
            {
                Assert.IsTrue(data.HasTrigger);
                foreach (float gate in gates)
                {
                    Assert.GreaterOrEqual(System.Math.Abs(data.TriggerHpFraction - gate), 0.05f - 0.0001f, $"{data.RuleId} {data.TriggerHpFraction} vs {gate}");
                }
            }
        }

        [Test]
        public void 챕터3_보스_트리거는_챕터2_보스와_같은_0_75_0_35이고_ruleIndex_순서다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            IReadOnlyList<StageRuleData> rules = db.GetRules(3, 5);

            Assert.AreEqual(2, rules.Count);
            Assert.AreEqual(1, rules[0].RuleIndex);
            Assert.AreEqual(2, rules[1].RuleIndex);
            Assert.AreEqual(db.GetRules(2, 5)[0].TriggerHpFraction, rules[0].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(db.GetRules(2, 5)[1].TriggerHpFraction, rules[1].TriggerHpFraction, 0.0001f);
        }

        [TestCase(0.85f)]
        [TestCase(0.65f)]
        [TestCase(0.45f)]
        [TestCase(0.6f)]
        [TestCase(0.5f)]
        public void 챕터3_보스_트리거를_게이트나_패시브_임계로_바꾸면_로드_오류다(float fraction)
        {
            ChapterDataSet set = ValidWithAssassin();
            set.AddRule(3, 5, 1, "BossBulwark", fraction);

            AssertBuildFails(set, "스킬 게이트/패시브 임계");
        }

        [Test]
        public void 챕터3_봉인_규칙은_상시_규칙이라_봉인_해제_0_60과_0_50이_트리거로_검증되지_않는다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.AddRule(3, 2, 1, "SealedNest");
            set.AddRule(3, 4, 1, "SealedNest");
            set.Params.Single(p => p.chapterId == 3 && p.stageIndex == 2 && p.paramKey == "unlockHpFraction").value = 0.6f;
            set.Params.Single(p => p.chapterId == 3 && p.stageIndex == 4 && p.paramKey == "unlockHpFraction").value = 0.5f;

            ChapterDatabase db = set.Build();

            Assert.IsFalse(db.GetRules(3, 2)[0].HasTrigger);
            Assert.AreEqual(0.6f, db.GetRules(3, 2)[0].Params["unlockHpFraction"], 0.0001f);
        }

        [Test]
        public void 챕터3_규칙_필수키가_하나_빠지면_해당_스테이지가_메시지에_나온다()
        {
            ChapterDataSet set = ValidWithAssassin();
            set.AddRule(3, 5, 2, "HunterTraps", 0.35f);
            set.Params.RemoveAll(p => p.chapterId == 3 && p.paramKey == "damagePerSec");

            AssertBuildFails(set, "3-5-2(HunterTraps)", "필수 키 누락: damagePerSec");
        }

        [Test]
        public void 같은_스테이지의_규칙_두_개가_다른_ruleIndex로_공존한다_3_4_봉인과_안개()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            IReadOnlyList<StageRuleData> rules = db.GetRules(3, 4);

            Assert.AreEqual(EStageRule.SealedNest, rules[0].RuleId);
            Assert.AreEqual(EStageRule.DenseFog, rules[1].RuleId);
            Assert.IsFalse(rules[0].HasTrigger);
            Assert.IsFalse(rules[1].HasTrigger);
        }

        //# --- 열거형 / 키 ---

        [Test]
        public void 암살자_관련_열거형_값이_정의돼_있다()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(EHero), "Assassin"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EHeroAi), "Skirmisher"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EBackdrop), "Battle_Backdrop_ShadowCitadel"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EData), "HeroArchetype_Skirmish"));
        }

        [Test]
        public void 실제_Chapter_json의_챕터3_배경_키는_EBackdrop_값명과_정확히_일치한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(EBackdrop.Battle_Backdrop_ShadowCitadel.ToString(), db.GetChapter(3).backdropKey);
            Assert.AreEqual(1013, db.GetChapter(3).themeTextKey);
            Assert.AreEqual(1003, db.GetChapter(3).nameKey);
        }

        [Test]
        public void 실제_JSON_챕터3_스테이지_이름_키는_1121부터_1125까지_연속이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            for (int index = 1; index <= 5; ++index)
            {
                Assert.AreEqual(1120 + index, db.GetStage(3, index).nameKey, $"3-{index}");
            }
        }

        [Test]
        public void 실제_JSON_챕터3_스테이지_배수는_단조_증가하고_보스가_가장_크다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            for (int index = 2; index <= 5; ++index)
            {
                Assert.Greater(db.GetStage(3, index).hpMultiplier, db.GetStage(3, index - 1).hpMultiplier, $"hp 3-{index}");
                Assert.Greater(db.GetStage(3, index).powerMultiplier, db.GetStage(3, index - 1).powerMultiplier, $"power 3-{index}");
                Assert.Greater(db.GetStage(3, index).targetClearSec, db.GetStage(3, index - 1).targetClearSec, $"clear 3-{index}");
            }
        }

        [Test]
        public void 챕터3_첫_스테이지는_챕터2_보스보다_톱니처럼_낮은_등가_난이도다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            HeroArchetypeRow archer = db.GetHero("Archer");
            HeroArchetypeRow assassin = db.GetHero("Assassin");

            float archerBoss = archer.hpScale * db.GetStage(2, 5).hpMultiplier;
            float assassinFirst = assassin.hpScale * db.GetStage(3, 1).hpMultiplier;

            Assert.Less(assassinFirst, archerBoss, "분자(hpScale x hpMultiplier) 기준 톱니 골");
        }
    }
}
