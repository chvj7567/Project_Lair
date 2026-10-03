using System.Linq;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 챕터 데이터 정규화 검증 — 위반 경로 전부 + 조회 경계 (chapter-system §1·§3.2·§3.4·§9 E-11). 메모리 데이터 기반.
    public class ChapterDatabaseValidationTests : ChapterStaticStateTestBase
    {
        private static void AssertBuildFails(ChapterDataSet set, params string[] expectedFragments)
        {
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());
            foreach (string fragment in expectedFragments)
            {
                StringAssert.Contains(fragment, e.Message);
            }
        }

        //# --- 정상 ---

        [Test]
        public void 정상_묶음은_예외없이_구축되고_챕터수가_맞다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();

            Assert.AreEqual(2, db.ChapterCount);
            Assert.AreEqual("Knight", db.GetChapter(1).heroId);
            Assert.AreEqual(2, db.GetRules(2, 5).Count);
        }

        [Test]
        public void 챕터3까지_늘려도_구축되고_선형11은_챕터3_영웅이다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.AddChapter(3, "Knight", "Battle_Backdrop");

            ChapterDatabase db = set.Build();

            Assert.AreEqual(3, db.ChapterCount);
            Assert.AreEqual("Knight", db.GetHeroOfLinear(11).heroId);
            Assert.IsNotNull(db.GetStageByLinear(15));
            Assert.IsNull(db.GetStageByLinear(16));
        }

        [Test]
        public void 챕터_행_순서가_뒤섞여도_챕터ID_오름차순으로_정렬된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters.Reverse();

            ChapterDatabase db = set.Build();

            Assert.AreEqual(1, db.Chapters[0].chapterId);
            Assert.AreEqual(2, db.Chapters[1].chapterId);
        }

        //# --- 챕터 ---

        [Test]
        public void 모든_배열이_null이면_챕터_비어있음_오류다()
        {
            ChapterDataException e = Assert.Throws<ChapterDataException>(
                () => ChapterDatabase.Build(null, null, null, null, null, null, null));

            StringAssert.Contains("Chapter.json 이 비어 있음", e.Message);
        }

        [Test]
        public void 챕터ID가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters.Add(new ChapterRow { chapterId = 2, heroId = "Knight", backdropKey = "Battle_Backdrop" });

            AssertBuildFails(set, "Chapter.chapterId 중복: 2");
        }

        [Test]
        public void 챕터ID가_1부터_연속이_아니면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters.RemoveAll(c => c.chapterId == 2);
            set.Stages.RemoveAll(s => s.chapterId == 2);
            set.Rules.RemoveAll(r => r.chapterId == 2);
            set.Params.RemoveAll(p => p.chapterId == 2);
            set.Triggers.RemoveAll(t => t.chapterId == 2);
            set.AddChapter(3, "Knight", "Battle_Backdrop");

            AssertBuildFails(set, "1부터 연속");
        }

        [Test]
        public void 챕터ID가_0부터_시작하면_로드_오류다()
        {
            ChapterDataSet set = new ChapterDataSet();
            set.Heroes.Add(ChapterDataSet.Knight());
            set.AddChapter(0, "Knight", "Battle_Backdrop");

            AssertBuildFails(set, "1부터 연속");
        }

        [Test]
        public void 챕터_heroId가_영웅_테이블에_없으면_FK_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters[1].heroId = "Paladin";

            AssertBuildFails(set, "Chapter[2].heroId 가 HeroArchetype 에 없음: Paladin");
        }

        [Test]
        public void 챕터_heroId가_null이면_FK_오류이고_예외없이_모아진다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters[0].heroId = null;

            AssertBuildFails(set, "Chapter[1].heroId");
        }

        [TestCase("Battle_Backdrop_Unknown")]
        [TestCase("1")]
        [TestCase("battle_backdrop")]
        [TestCase("")]
        [TestCase(null)]
        public void 챕터_backdropKey가_EBackdrop_값명이_아니면_로드_오류다(string backdropKey)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters[0].backdropKey = backdropKey;

            AssertBuildFails(set, "Chapter[1].backdropKey 가 EBackdrop 에 없음");
        }

        //# --- 스테이지 ---

        [Test]
        public void 챕터의_스테이지가_4개뿐이면_누락_인덱스가_메시지에_나온다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages.RemoveAll(s => s.chapterId == 1 && s.stageIndex == 3);

            AssertBuildFails(set, "챕터 1 의 스테이지 3 가 없음");
        }

        [Test]
        public void 스테이지가_6개여도_인덱스_범위_밖_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages.Add(new StageRow { chapterId = 1, stageIndex = 6 });

            AssertBuildFails(set, "Stage[1-6] stageIndex 범위(1~5) 밖");
        }

        [Test]
        public void 스테이지_인덱스가_0이면_범위_밖_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages.Add(new StageRow { chapterId = 2, stageIndex = 0 });

            AssertBuildFails(set, "Stage[2-0] stageIndex 범위(1~5) 밖");
        }

        [Test]
        public void 스테이지_복합키가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages.Add(new StageRow { chapterId = 1, stageIndex = 3, hpMultiplier = 9f });

            AssertBuildFails(set, "Stage 복합키 중복: 1-3");
        }

        [Test]
        public void 존재하지_않는_챕터를_가리키는_스테이지는_FK_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Stages.Add(new StageRow { chapterId = 9, stageIndex = 1 });

            AssertBuildFails(set, "Stage[9-1] 의 chapterId 가 Chapter 에 없음");
        }

        [Test]
        public void 서로_다른_챕터의_같은_인덱스는_복합키_충돌이_아니다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();

            Assert.AreNotSame(db.GetStage(1, 1), db.GetStage(2, 1));
        }

        //# --- 영웅 / 카이트 ---

        [Test]
        public void 영웅_heroId가_EHero에_없으면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes[0].heroId = "Paladin";
            set.Chapters[0].heroId = "Paladin";

            AssertBuildFails(set, "HeroArchetype.heroId 가 EHero 에 없음: Paladin");
        }

        [TestCase("0")]
        [TestCase("1")]
        [TestCase("knight")]
        public void 영웅_heroId가_숫자나_대소문자_불일치면_로드_오류다(string heroId)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes[0].heroId = heroId;

            AssertBuildFails(set, "HeroArchetype.heroId 가 EHero 에 없음");
        }

        [Test]
        public void 영웅_heroId가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes.Add(ChapterDataSet.Knight());

            AssertBuildFails(set, "HeroArchetype.heroId 중복/누락: Knight");
        }

        [Test]
        public void 영웅_aiType이_EHeroAi에_없으면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes[0].aiType = "Berserker";

            AssertBuildFails(set, "HeroArchetype[Knight].aiType 가 EHeroAi 에 없음: Berserker");
        }

        [TestCase("2")]
        [TestCase("melee")]
        [TestCase("")]
        public void 영웅_aiType이_숫자나_대소문자_불일치면_로드_오류다(string aiType)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Heroes[0].aiType = aiType;

            AssertBuildFails(set, "aiType 가 EHeroAi 에 없음");
        }

        [Test]
        public void Kiter_영웅에_카이트_row가_없으면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Kites.Clear();

            AssertBuildFails(set, "HeroArchetype[Archer] 는 Kiter 인데 HeroArchetype_Kite row 가 없음");
        }

        [Test]
        public void Melee_영웅에_카이트_row가_붙으면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            HeroArchetype_KiteRow kite = ChapterDataSet.ArcherKite();
            kite.heroId = "Knight";
            set.Kites.Add(kite);

            AssertBuildFails(set, "HeroArchetype_Kite[Knight] 는 aiType=Kiter 영웅에만 존재해야 함");
        }

        [Test]
        public void 영웅_테이블에_없는_heroId의_카이트는_FK_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            HeroArchetype_KiteRow kite = ChapterDataSet.ArcherKite();
            kite.heroId = "Ghost";
            set.Kites.Add(kite);

            AssertBuildFails(set, "HeroArchetype_Kite[Ghost] 의 heroId 가 HeroArchetype 에 없음");
        }

        [Test]
        public void 카이트_heroId가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Kites.Add(ChapterDataSet.ArcherKite());

            AssertBuildFails(set, "HeroArchetype_Kite.heroId 중복/누락: Archer");
        }

        //# --- 규칙 ---

        [Test]
        public void 규칙이_존재하지_않는_스테이지를_가리키면_FK_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.AddRule(3, 1, 1, "DenseFog");

            AssertBuildFails(set, "Stage_Rule[3-1-1] 의 스테이지가 Stage 에 없음");
        }

        [Test]
        public void 규칙_복합키가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Rules.Add(new Stage_RuleRow { chapterId = 1, stageIndex = 2, ruleIndex = 1, ruleId = "DenseFog" });

            AssertBuildFails(set, "Stage_Rule 복합키 중복: 1-2-1");
        }

        [TestCase("holyspring")]
        [TestCase("Unknown")]
        [TestCase("0")]
        [TestCase("")]
        public void 규칙_ruleId가_EStageRule_값명이_아니면_로드_오류다(string ruleId)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Rules.Add(new Stage_RuleRow { chapterId = 1, stageIndex = 3, ruleIndex = 1, ruleId = ruleId });

            AssertBuildFails(set, "Stage_Rule[1-3-1].ruleId 가 EStageRule 에 없음");
        }

        [Test]
        public void 같은_스테이지에_ruleIndex가_다르면_같은_규칙을_중복_부착할_수_있다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.AddRule(1, 3, 1, "DenseFog");
            set.AddRule(1, 3, 2, "DenseFog");

            ChapterDatabase db = set.Build();

            Assert.AreEqual(2, db.GetRules(1, 3).Count);
        }

        [Test]
        public void 규칙은_입력_순서와_무관하게_ruleIndex_오름차순으로_조회된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.AddRule(1, 4, 3, "DenseFog");
            set.AddRule(1, 4, 1, "SealedNest");
            set.AddRule(1, 4, 2, "HolySpring");

            ChapterDatabase db = set.Build();

            int[] order = db.GetRules(1, 4).Select(r => r.RuleIndex).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
        }

        //# --- 파라미터 ---

        [TestCase("HolySpring")]
        [TestCase("DenseFog")]
        [TestCase("SealedNest")]
        [TestCase("BossBulwark")]
        [TestCase("ArrowRain")]
        [TestCase("HunterTraps")]
        public void 규칙별_필수키_하나라도_빠지면_그_키가_누락_오류로_나온다(string ruleId)
        {
            foreach (string missing in ChapterDataSet.RequiredKeys(ruleId))
            {
                ChapterDataSet set = new ChapterDataSet();
                set.Heroes.Add(ChapterDataSet.Knight());
                set.AddChapter(1, "Knight", "Battle_Backdrop");
                set.AddRule(1, 2, 1, ruleId);
                set.Params.RemoveAll(p => p.paramKey == missing);

                ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build(), $"{ruleId}/{missing}");
                StringAssert.Contains($"필수 키 누락: {missing}", e.Message);
            }
        }

        [TestCase("HolySpring")]
        [TestCase("DenseFog")]
        [TestCase("SealedNest")]
        [TestCase("BossBulwark")]
        [TestCase("ArrowRain")]
        [TestCase("HunterTraps")]
        public void 규칙별_필수키만_정확히_있으면_구축된다(string ruleId)
        {
            ChapterDataSet set = new ChapterDataSet();
            set.Heroes.Add(ChapterDataSet.Knight());
            set.AddChapter(1, "Knight", "Battle_Backdrop");
            set.AddRule(1, 2, 1, ruleId);

            ChapterDatabase db = set.Build();

            Assert.AreEqual(ChapterDataSet.RequiredKeys(ruleId).Length, db.GetRules(1, 2)[0].Params.Count);
        }

        [Test]
        public void 규칙에_허용되지_않은_추가_키가_있으면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Params.Add(new Stage_Rule_ParamRow { chapterId = 1, stageIndex = 2, ruleIndex = 1, paramKey = "bonusMul", value = 2f });

            AssertBuildFails(set, "허용되지 않은 추가 키: bonusMul");
        }

        [Test]
        public void 다른_규칙의_필수키를_섞어_넣으면_추가_키_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Params.Add(new Stage_Rule_ParamRow { chapterId = 1, stageIndex = 5, ruleIndex = 1, paramKey = "radius", value = 1f });

            AssertBuildFails(set, "Stage_Rule_Param[1-5-1(BossBulwark)] 허용되지 않은 추가 키: radius");
        }

        [Test]
        public void 파라미터_복합키가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Params.Add(new Stage_Rule_ParamRow { chapterId = 1, stageIndex = 5, ruleIndex = 1, paramKey = "damageTakenMul", value = 0.1f });

            AssertBuildFails(set, "Stage_Rule_Param 복합키 중복: 1-5-1/damageTakenMul");
        }

        [Test]
        public void 규칙이_없는_파라미터는_FK_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Params.Add(new Stage_Rule_ParamRow { chapterId = 1, stageIndex = 1, ruleIndex = 1, paramKey = "minRange", value = 1f });

            AssertBuildFails(set, "Stage_Rule_Param[1-1-1] 의 규칙이 Stage_Rule 에 없음");
        }

        [Test]
        public void 파라미터_값은_row의_값_그대로_조회된다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Params.Single(p => p.chapterId == 1 && p.stageIndex == 5 && p.paramKey == "durationSec").value = 12.5f;

            ChapterDatabase db = set.Build();

            Assert.AreEqual(12.5f, db.GetRules(1, 5)[0].Params["durationSec"], 0.0001f);
        }

        [Test]
        public void 같은_스테이지의_두_규칙은_파라미터가_서로_섞이지_않는다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();

            Assert.IsTrue(db.GetRules(2, 5)[0].Params.ContainsKey("tickCount"));
            Assert.IsFalse(db.GetRules(2, 5)[0].Params.ContainsKey("trapCount"));
            Assert.IsTrue(db.GetRules(2, 5)[1].Params.ContainsKey("trapCount"));
        }

        //# --- 트리거 ---

        [TestCase(0.85f)]
        [TestCase(0.65f)]
        [TestCase(0.45f)]
        public void 트리거_hpFraction이_스킬게이트와_같으면_로드_오류다(float fraction)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Triggers[0].hpFraction = fraction;

            AssertBuildFails(set, "스킬 게이트/패시브 임계");
        }

        [TestCase(0.9f)]
        [TestCase(0.8f)]
        [TestCase(0.7f)]
        [TestCase(0.6f)]
        [TestCase(0.5f)]
        [TestCase(0.4f)]
        [TestCase(0.3f)]
        [TestCase(0.2f)]
        [TestCase(0.1f)]
        public void 트리거_hpFraction이_패시브_임계_9개와_같으면_로드_오류다(float fraction)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Triggers[0].hpFraction = fraction;

            AssertBuildFails(set, "스킬 게이트/패시브 임계");
        }

        [Test]
        public void 트리거_hpFraction이_임계와_0_00005_이내면_일치로_보고_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Triggers[0].hpFraction = 0.85005f;

            AssertBuildFails(set, "스킬 게이트/패시브 임계");
        }

        [TestCase(0.55f)]
        [TestCase(0.75f)]
        [TestCase(0.35f)]
        [TestCase(0.86f)]
        [TestCase(0.05f)]
        public void 트리거_hpFraction이_임계에서_0_01이상_떨어지면_통과한다(float fraction)
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Triggers[0].hpFraction = fraction;

            ChapterDatabase db = set.Build();

            Assert.AreEqual(fraction, db.GetRules(1, 5)[0].TriggerHpFraction, 0.0001f);
        }

        [Test]
        public void 트리거_복합키가_중복이면_로드_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Triggers.Add(new Stage_Rule_TriggerRow { chapterId = 1, stageIndex = 5, ruleIndex = 1, hpFraction = 0.55f });

            AssertBuildFails(set, "Stage_Rule_Trigger 복합키 중복: 1-5-1");
        }

        [Test]
        public void 규칙이_없는_트리거는_FK_오류다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Triggers.Add(new Stage_Rule_TriggerRow { chapterId = 1, stageIndex = 1, ruleIndex = 1, hpFraction = 0.55f });

            AssertBuildFails(set, "Stage_Rule_Trigger[1-1-1] 의 규칙이 Stage_Rule 에 없음");
        }

        [Test]
        public void 트리거_row가_없는_규칙은_HasTrigger_false이고_0을_sentinel로_쓰지_않는다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();

            StageRuleData rule = db.GetRules(1, 2)[0];

            Assert.IsFalse(rule.HasTrigger);
            Assert.AreEqual(0f, rule.TriggerHpFraction);
        }

        //# --- 오류 모음 / 조회 경계 ---

        [Test]
        public void 위반이_여러_개면_한_예외에_전부_모아_보고한다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters[0].backdropKey = "Nope";
            set.Stages.RemoveAll(s => s.chapterId == 2 && s.stageIndex == 4);
            set.Params.RemoveAll(p => p.paramKey == "durationSec");
            set.Triggers[1].hpFraction = 0.65f;

            AssertBuildFails(set, "backdropKey", "챕터 2 의 스테이지 4", "필수 키 누락: durationSec", "스킬 게이트/패시브 임계");
        }

        [Test]
        public void 오류_메시지는_검증_실패_머리말로_시작한다()
        {
            ChapterDataSet set = ChapterDataSet.Valid();
            set.Chapters[0].backdropKey = "Nope";

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());

            StringAssert.StartsWith("챕터 데이터 검증 실패:", e.Message);
        }

        [Test]
        public void 조회는_범위_밖_입력에_null이나_빈목록을_돌려주고_예외가_없다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();

            Assert.IsNull(db.GetChapter(0));
            Assert.IsNull(db.GetChapter(3));
            Assert.IsNull(db.GetStage(1, 0));
            Assert.IsNull(db.GetStage(1, 6));
            Assert.IsNull(db.GetStage(3, 1));
            Assert.IsNull(db.GetStageByLinear(0));
            Assert.IsNull(db.GetStageByLinear(-3));
            Assert.IsNull(db.GetStageByLinear(11));
            Assert.IsNull(db.GetHero(null));
            Assert.IsNull(db.GetHero("Paladin"));
            Assert.IsNull(db.GetKite(null));
            Assert.IsNull(db.GetHeroOfLinear(11), "존재하지 않는 챕터 3");
            Assert.AreEqual(0, db.GetRules(9, 9).Count);
        }

        [Test]
        public void 선형번호_조회는_챕터경계에서_올바른_스테이지를_돌려준다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();

            Assert.AreSame(db.GetStage(1, 5), db.GetStageByLinear(5));
            Assert.AreSame(db.GetStage(2, 1), db.GetStageByLinear(6));
            Assert.AreSame(db.GetStage(2, 5), db.GetStageByLinear(10));
            Assert.AreEqual("Knight", db.GetHeroOfLinear(5).heroId);
            Assert.AreEqual("Archer", db.GetHeroOfLinear(6).heroId);
        }

        //# --- 정적 상태 ---

        [Test]
        public void Current는_Build만으로는_채워지지_않는다()
        {
            ChapterDataSet.Valid().Build();

            Assert.IsNull(ChapterDatabase.Current);
        }

        [Test]
        public void 검증_실패는_기존_Current를_건드리지_않는다()
        {
            ChapterDatabase good = ChapterDataSet.Valid().Build();
            ChapterDatabase.Current = good;
            ChapterDataSet bad = ChapterDataSet.Valid();
            bad.Chapters[0].backdropKey = "Nope";

            Assert.Throws<ChapterDataException>(() => bad.Build());

            Assert.AreSame(good, ChapterDatabase.Current);
        }
    }
}
