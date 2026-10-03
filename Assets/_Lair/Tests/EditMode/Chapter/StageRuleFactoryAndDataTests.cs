using System;
using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# StageRuleFactory / StageRuleParams / MonsterRangeModifier + shipped JSON 규칙 데이터 일관성 (chapter-system §3·§4.1·§4.2·§4.4).
    public class StageRuleFactoryAndDataTests : ChapterStaticStateTestBase
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        private static ChapterDatabase BuildShipped()
        {
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter"), Load<StageRow>("Stage"), Load<Stage_RuleRow>("Stage_Rule"),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param"), Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"));
        }

        //# --- StageRuleParams ---

        [Test]
        public void 파라미터_Get은_값을_돌려주고_없는_키는_키_이름을_담은_예외다()
        {
            StageRuleParams p = StageRuleDataFactory.Params(("a", 1.5f));

            Assert.AreEqual(1.5f, p.Get("a"), 0.0001f);
            KeyNotFoundException e = Assert.Throws<KeyNotFoundException>(() => p.Get("missing"));
            StringAssert.Contains("missing", e.Message);
        }

        [Test]
        public void 파라미터_맵이_null이면_Get은_예외다()
        {
            StageRuleParams p = new StageRuleParams(null);

            Assert.Throws<KeyNotFoundException>(() => p.Get("a"));
        }

        [TestCase(4f, 4)]
        [TestCase(3.4f, 3)]
        [TestCase(3.6f, 4)]
        [TestCase(0f, 0)]
        [TestCase(-1.6f, -2)]
        public void 파라미터_GetInt는_가장_가까운_정수로_반올림한다(float value, int expected)
        {
            StageRuleParams p = StageRuleDataFactory.Params(("n", value));

            Assert.AreEqual(expected, p.GetInt("n"));
        }

        //# --- StageRuleFactory ---

        private static readonly EStageRule[] AllRules =
        {
            EStageRule.HolySpring, EStageRule.DenseFog, EStageRule.SealedNest,
            EStageRule.BossBulwark, EStageRule.ArrowRain, EStageRule.HunterTraps,
        };

        private static StageRuleData DataWithAllKeys(EStageRule ruleId, string skip = null)
        {
            List<(string key, float value)> ps = new List<(string key, float value)>();
            foreach (string key in ChapterDataSet.RequiredKeys(ruleId.ToString()))
            {
                if (key == skip)
                    continue;
                ps.Add((key, 1f));
            }
            return StageRuleDataFactory.Make(ruleId, null, ps.ToArray());
        }

        [TestCaseSource(nameof(AllRules))]
        public void 팩토리는_6종_규칙_ID에_맞는_구현을_만든다(EStageRule ruleId)
        {
            IStageRule rule = StageRuleFactory.Create(DataWithAllKeys(ruleId));

            Assert.AreEqual(ruleId, rule.Id);
            Assert.IsFalse(rule.IsActive, "생성만으로는 활성되지 않는다");
        }

        [TestCaseSource(nameof(AllRules))]
        public void 팩토리는_필수_파라미터가_하나라도_빠지면_그_키를_담은_예외를_던진다(EStageRule ruleId)
        {
            foreach (string missing in ChapterDataSet.RequiredKeys(ruleId.ToString()))
            {
                StageRuleData data = DataWithAllKeys(ruleId, missing);

                KeyNotFoundException e = Assert.Throws<KeyNotFoundException>(() => StageRuleFactory.Create(data), $"{ruleId}/{missing}");
                StringAssert.Contains(missing, e.Message);
            }
        }

        [Test]
        public void 팩토리는_알_수_없는_규칙_ID에_ArgumentOutOfRange를_던진다()
        {
            StageRuleData data = StageRuleDataFactory.Make((EStageRule)99, null);

            Assert.Throws<ArgumentOutOfRangeException>(() => StageRuleFactory.Create(data));
        }

        [Test]
        public void 팩토리는_파라미터_맵이_null이면_KeyNotFound다()
        {
            StageRuleData data = new StageRuleData { RuleId = EStageRule.DenseFog, Params = null };

            Assert.Throws<KeyNotFoundException>(() => StageRuleFactory.Create(data));
        }

        [TestCase(-1f)]
        [TestCase(6f)]
        [TestCase(99f)]
        public void 봉인_규칙은_EMonster에_없는_종족값이면_생성_시_ArgumentException이다(float species)
        {
            StageRuleData data = StageRuleDataFactory.Make(EStageRule.SealedNest, null, ("lockedSpecies", species), ("unlockHpFraction", 0.6f));

            Assert.Throws<ArgumentException>(() => StageRuleFactory.Create(data));
        }

        [TestCase(0f, EMonster.Wisp)]
        [TestCase(1f, EMonster.Wraith)]
        [TestCase(5f, EMonster.Phantom)]
        public void 봉인_규칙은_종족값을_EMonster로_해석해_해당_종을_봉인한다(float species, EMonster expected)
        {
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            IStageRule rule = StageRuleFactory.Create(StageRuleDataFactory.Make(EStageRule.SealedNest, null,
                ("lockedSpecies", species), ("unlockHpFraction", 0.6f)));

            rule.Activate(ctx);

            CollectionAssert.AreEqual(new[] { expected }, ctx.Locks);
        }

        //# --- MonsterRangeModifier ---

        [Test]
        public void 사거리_규칙은_비활성이면_충돌거리와_무관하게_입력을_그대로_돌려준다()
        {
            MonsterRangeModifier m = new MonsterRangeModifier();

            Assert.IsFalse(m.IsActive);
            Assert.AreEqual(0.3f, m.Apply(0.3f, 5f), 0.0001f);
            Assert.AreEqual(7f, m.Apply(7f, 0f), 0.0001f);
        }

        [TestCase(5f, 0f, 3.5f)]
        [TestCase(5f, 2f, 3.5f)]
        [TestCase(3f, 2f, 2.1f)]
        [TestCase(2.5f, 2f, 2.1f)]
        [TestCase(2.1f, 2f, 2.1f)]
        [TestCase(1.5f, 2f, 1.5f)]
        [TestCase(1f, 0f, 0.8f)]
        [TestCase(0.8f, 0f, 0.8f)]
        [TestCase(0.5f, 0f, 0.5f)]
        [TestCase(0f, 0f, 0f)]
        public void 사거리_규칙은_배율_0_7_하한_max_minRange_충돌거리_0_1을_적용하되_짧은_사거리는_늘리지_않는다(float range, float collision, float expected)
        {
            MonsterRangeModifier m = new MonsterRangeModifier();
            m.Set(0.7f, 0.8f);

            Assert.AreEqual(expected, m.Apply(range, collision), 0.0001f);
        }

        [Test]
        public void 사거리_규칙은_충돌거리가_작으면_minRange가_하한이다()
        {
            MonsterRangeModifier m = new MonsterRangeModifier();
            m.Set(0.7f, 0.8f);

            Assert.AreEqual(0.8f, m.Apply(1f, 0.3f), 0.0001f, "충돌 0.3+0.1=0.4 < minRange 0.8");
        }

        [Test]
        public void 사거리_규칙_결과는_원래_사거리를_넘지_않는다()
        {
            MonsterRangeModifier m = new MonsterRangeModifier();
            m.Set(0.7f, 0.8f);

            for (float range = 0f; range <= 8f; range += 0.1f)
            {
                for (float collision = 0f; collision <= 3f; collision += 0.5f)
                {
                    Assert.LessOrEqual(m.Apply(range, collision), range + 0.0001f, $"range {range} collision {collision}");
                }
            }
        }

        [Test]
        public void 사거리_규칙은_다시_Set하면_값이_갱신되고_Clear는_멱등이다()
        {
            MonsterRangeModifier m = new MonsterRangeModifier();
            m.Set(0.5f, 0.2f);
            m.Set(0.7f, 0.8f);

            Assert.AreEqual(3.5f, m.Apply(5f), 0.0001f);

            m.Clear();
            m.Clear();
            Assert.IsFalse(m.IsActive);
            Assert.AreEqual(5f, m.Apply(5f), 0.0001f);
        }

        [Test]
        public void 사거리_규칙_여유값은_0_1이다()
        {
            Assert.AreEqual(0.1f, MonsterRangeModifier.ClearanceMargin, 0.0001f);
        }

        //# --- shipped JSON 일관성 ---

        [Test]
        public void 실제_JSON_챕터1은_소급_규칙_4행이_기획서_표와_일치한다()
        {
            ChapterDatabase db = BuildShipped();

            Assert.AreEqual(0, db.GetRules(1, 1).Count, "1-1 은 규칙 없음");
            Assert.AreEqual(EStageRule.HolySpring, db.GetRules(1, 2)[0].RuleId);
            Assert.AreEqual(EStageRule.DenseFog, db.GetRules(1, 3)[0].RuleId);
            Assert.AreEqual(EStageRule.SealedNest, db.GetRules(1, 4)[0].RuleId);
            Assert.AreEqual(EStageRule.BossBulwark, db.GetRules(1, 5)[0].RuleId);

            int total = 0;
            for (int stage = 1; stage <= 5; ++stage)
            {
                total += db.GetRules(1, stage).Count;
                Assert.LessOrEqual(db.GetRules(1, stage).Count, 1, $"1-{stage}");
            }
            Assert.AreEqual(4, total);
        }

        [Test]
        public void 실제_JSON_챕터1_수치는_기획서_3_4와_같다()
        {
            ChapterDatabase db = BuildShipped();
            IReadOnlyDictionary<string, float> spring = db.GetRules(1, 2)[0].Params;
            IReadOnlyDictionary<string, float> fog = db.GetRules(1, 3)[0].Params;
            IReadOnlyDictionary<string, float> nest = db.GetRules(1, 4)[0].Params;
            IReadOnlyDictionary<string, float> bulwark = db.GetRules(1, 5)[0].Params;

            Assert.AreEqual(0.4f, spring["healPercentPerSec"], 0.0001f);
            Assert.AreEqual(3f, spring["radius"], 0.0001f);
            Assert.AreEqual(20f, spring["firstActiveSec"], 0.0001f);
            Assert.AreEqual(40f, spring["periodSec"], 0.0001f);
            Assert.AreEqual(8f, spring["activeSec"], 0.0001f);
            Assert.AreEqual(0.7f, fog["monsterRangeMul"], 0.0001f);
            Assert.AreEqual(0.8f, fog["minRange"], 0.0001f);
            Assert.AreEqual((float)EMonster.Wraith, nest["lockedSpecies"], 0.0001f);
            Assert.AreEqual(0.6f, nest["unlockHpFraction"], 0.0001f);
            Assert.AreEqual(0.6f, bulwark["damageTakenMul"], 0.0001f);
            Assert.AreEqual(15f, bulwark["durationSec"], 0.0001f);
        }

        [Test]
        public void 실제_JSON_챕터2는_규칙_6행이고_2_4와_2_5는_ruleIndex_순서로_조회된다()
        {
            ChapterDatabase db = BuildShipped();

            Assert.AreEqual(0, db.GetRules(2, 1).Count);
            Assert.AreEqual(EStageRule.SealedNest, db.GetRules(2, 2)[0].RuleId);
            Assert.AreEqual((float)EMonster.Phantom, db.GetRules(2, 2)[0].Params["lockedSpecies"], 0.0001f);
            Assert.AreEqual(0.7f, db.GetRules(2, 2)[0].Params["unlockHpFraction"], 0.0001f);
            Assert.AreEqual(EStageRule.DenseFog, db.GetRules(2, 3)[0].RuleId);
            Assert.AreEqual(EStageRule.HolySpring, db.GetRules(2, 4)[0].RuleId);
            Assert.AreEqual(0.5f, db.GetRules(2, 4)[0].Params["healPercentPerSec"], 0.0001f);
            Assert.AreEqual(EStageRule.DenseFog, db.GetRules(2, 4)[1].RuleId);
            Assert.AreEqual(EStageRule.ArrowRain, db.GetRules(2, 5)[0].RuleId);
            Assert.AreEqual(EStageRule.HunterTraps, db.GetRules(2, 5)[1].RuleId);

            int total = 0;
            for (int stage = 1; stage <= 5; ++stage)
            {
                total += db.GetRules(2, stage).Count;
            }
            Assert.AreEqual(6, total);
        }

        [Test]
        public void 실제_JSON_보스_페이즈_트리거는_Ch1_0_55_Ch2_0_75_0_35이고_상시_규칙은_트리거가_없다()
        {
            ChapterDatabase db = BuildShipped();

            Assert.IsTrue(db.GetRules(1, 5)[0].HasTrigger);
            Assert.AreEqual(0.55f, db.GetRules(1, 5)[0].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(0.75f, db.GetRules(2, 5)[0].TriggerHpFraction, 0.0001f);
            Assert.AreEqual(0.35f, db.GetRules(2, 5)[1].TriggerHpFraction, 0.0001f);
            for (int chapter = 1; chapter <= 2; ++chapter)
            {
                for (int stage = 1; stage <= 4; ++stage)
                {
                    foreach (StageRuleData rule in db.GetRules(chapter, stage))
                    {
                        Assert.IsFalse(rule.HasTrigger, $"{chapter}-{stage} 은 상시 규칙");
                    }
                }
            }
        }

        [Test]
        public void 실제_JSON_규칙_10건은_팩토리로_만들어_활성_틱_비활성해도_FX가_남지_않는다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { HasDensePoint = true };

            for (int chapter = 1; chapter <= 2; ++chapter)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    foreach (StageRuleData data in db.GetRules(chapter, stage))
                    {
                        IStageRule rule = StageRuleFactory.Create(data);
                        Assert.DoesNotThrow(() =>
                        {
                            rule.Activate(ctx);
                            rule.Tick(21f, 0.5f);
                            rule.Deactivate();
                        }, $"{chapter}-{stage} {data.RuleId}");
                    }
                }
            }

            Assert.AreEqual(0, ctx.LiveFxCount);
        }

        //# --- 실제 데이터 + 러너 (스테이지 단위) ---

        private static StageRuleRunner RunnerFor(ChapterDatabase db, int chapter, int stage, IStageRuleContext ctx, IHealth hero)
        {
            return new StageRuleRunner(ctx, hero, db.GetRules(chapter, stage));
        }

        [Test]
        public void 규칙_없는_스테이지_1_1은_러너를_돌려도_컨텍스트가_전혀_바뀌지_않는다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            StageRuleFakeHero hero = new StageRuleFakeHero { Current = 10 };
            StageRuleRunner runner = RunnerFor(db, 1, 1, ctx, hero);

            runner.Start();
            for (int i = 1; i <= 30; ++i)
            {
                runner.Tick(i * 10f);
            }
            runner.Shutdown();

            Assert.AreEqual(0, runner.AlwaysRuleCount + runner.TriggeredRuleCount);
            Assert.AreEqual(0, ctx.Heals.Count + ctx.DamageMultiplies.Count + ctx.RangeRuleSets.Count + ctx.Locks.Count + ctx.Fxs.Count + ctx.Notices.Count);
        }

        [Test]
        public void 실제_1_2_성수샘은_Start부터_상시이고_20초_개방_전엔_회복이_없다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { HeroMaxHp = 4000 };
            StageRuleRunner runner = RunnerFor(db, 1, 2, ctx, new StageRuleFakeHero());
            runner.Start();

            runner.Tick(19.5f);
            Assert.AreEqual(0, ctx.TotalHealed);

            runner.Tick(20.5f);
            Assert.AreEqual(16, ctx.TotalHealed, "개방 1초 = 2틱 x 8");
            runner.Shutdown();
            Assert.AreEqual(0, ctx.LiveFxCount);
        }

        [Test]
        public void 실제_1_3_안개는_Start에서_사거리_규칙을_걸고_Shutdown에서_푼다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            StageRuleRunner runner = RunnerFor(db, 1, 3, ctx, new StageRuleFakeHero());

            runner.Start();
            Assert.AreEqual(1, ctx.RangeRuleSets.Count);
            Assert.AreEqual(0.7f, ctx.RangeRuleSets[0].Key, 0.0001f);
            Assert.AreEqual(0.8f, ctx.RangeRuleSets[0].Value, 0.0001f);

            runner.Shutdown();
            Assert.AreEqual(1, ctx.RangeRuleClears);
        }

        [Test]
        public void 실제_1_4_봉인은_망령을_봉인하고_HP_60퍼센트에서_풀며_종료시_추가_해제가_없다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            StageRuleFakeHero hero = new StageRuleFakeHero();
            StageRuleRunner runner = RunnerFor(db, 1, 4, ctx, hero);
            runner.Start();
            CollectionAssert.AreEqual(new[] { EMonster.Wraith }, ctx.Locks);

            ctx.HeroHpRatio = 0.61f;
            runner.Tick(1f);
            Assert.AreEqual(0, ctx.Unlocks.Count);

            ctx.HeroHpRatio = 0.6f;
            runner.Tick(2f);
            Assert.AreEqual(1, ctx.Unlocks.Count);

            runner.Shutdown();
            Assert.AreEqual(1, ctx.Unlocks.Count);
        }

        [Test]
        public void 실제_2_2_봉인은_환령을_HP_70퍼센트에서_푼다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            StageRuleRunner runner = RunnerFor(db, 2, 2, ctx, new StageRuleFakeHero());
            runner.Start();
            CollectionAssert.AreEqual(new[] { EMonster.Phantom }, ctx.Locks);

            ctx.HeroHpRatio = 0.7f;
            runner.Tick(1f);

            CollectionAssert.AreEqual(new[] { EMonster.Phantom }, ctx.Unlocks);
        }

        [Test]
        public void 실제_2_4는_성수샘과_안개가_함께_상시_활성된다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            StageRuleRunner runner = RunnerFor(db, 2, 4, ctx, new StageRuleFakeHero());

            runner.Start();

            Assert.AreEqual(2, runner.AlwaysRuleCount);
            Assert.AreEqual(0, runner.TriggeredRuleCount);
            Assert.AreEqual(1, ctx.RangeRuleSets.Count, "안개 활성");
            runner.Tick(19.5f);
            runner.Tick(20.5f);
            Assert.AreEqual(2 * 10, ctx.TotalHealed, "Ch2 샘은 0.5%: 틱당 4000 x 0.5% x 0.5 = 10");
        }

        [Test]
        public void 실제_2_5는_75퍼센트에서_화살비_35퍼센트에서_덫이_각각_한번씩_발동한다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { HasDensePoint = true };
            StageRuleFakeHero hero = new StageRuleFakeHero();
            StageRuleRunner runner = RunnerFor(db, 2, 5, ctx, hero);
            runner.Start();
            Assert.AreEqual(0, runner.AlwaysRuleCount);
            Assert.AreEqual(2, runner.TriggeredRuleCount);

            hero.Current = 80;
            runner.Tick(1f);
            Assert.AreEqual(0, ctx.Notices.Count, "75% 위에선 둘 다 대기");

            hero.Current = 75;
            runner.Tick(2f);
            Assert.AreEqual(1, ctx.Notices.Count);
            Assert.AreEqual(ArrowRainRule.NoticeStringId, ctx.Notices[0].StringId);

            hero.Current = 100;
            runner.Tick(3f);
            hero.Current = 74;
            runner.Tick(4f);
            Assert.AreEqual(1, ctx.Notices.Count, "회복 후 재진입 재발동 없음(E-1)");

            hero.Current = 35;
            runner.Tick(5f);
            Assert.AreEqual(2, ctx.Notices.Count);
            Assert.AreEqual(HunterTrapsRule.NoticeStringId, ctx.Notices[1].StringId);

            runner.Shutdown();
            Assert.AreEqual(0, ctx.LiveFxCount);
            Assert.GreaterOrEqual(ctx.SpeedClears, 1, "덫 감속 복구");
        }

        [Test]
        public void 실제_2_5에서_HP가_한_프레임에_20퍼센트로_떨어지면_화살비_다음_덫_순서로_발동한다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { HasDensePoint = true };
            StageRuleFakeHero hero = new StageRuleFakeHero();
            StageRuleRunner runner = RunnerFor(db, 2, 5, ctx, hero);
            runner.Start();

            hero.Current = 20;
            runner.Tick(1f);

            Assert.AreEqual(2, ctx.Notices.Count);
            Assert.AreEqual(ArrowRainRule.NoticeStringId, ctx.Notices[0].StringId);
            Assert.AreEqual(HunterTrapsRule.NoticeStringId, ctx.Notices[1].StringId);
        }

        [Test]
        public void 실제_1_5_수호결계는_55퍼센트_발동_15초_후_원복한다()
        {
            ChapterDatabase db = BuildShipped();
            RecordingStageRuleContext ctx = new RecordingStageRuleContext();
            StageRuleFakeHero hero = new StageRuleFakeHero();
            StageRuleRunner runner = RunnerFor(db, 1, 5, ctx, hero);
            runner.Start();

            hero.Current = 55;
            runner.Tick(1f);
            Assert.AreEqual(0.6f, ctx.DamageScale, 0.0001f);

            runner.Tick(16f);
            Assert.AreEqual(1f, ctx.DamageScale, 0.0001f);
        }
    }
}
