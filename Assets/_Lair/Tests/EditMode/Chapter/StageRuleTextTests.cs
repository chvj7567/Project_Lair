using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# StageRuleText — 규칙명/설명 문구 생성과 봉인 해제 HP% 변환 (chapter-system §3.4·§6.5). 실제 Strings_Ko 로 파라미터 치환까지 확인.
    public class StageRuleTextTests : ChapterStaticStateTestBase
    {
        private static Func<int, string> Text => WorldMapTestSupport.Text;

        private static readonly EStageRule[] AllRules =
        {
            EStageRule.HolySpring, EStageRule.DenseFog, EStageRule.SealedNest,
            EStageRule.BossBulwark, EStageRule.ArrowRain, EStageRule.HunterTraps,
        };

        //# --- 규칙명 ---

        [TestCase(EStageRule.HolySpring, 1201)]
        [TestCase(EStageRule.DenseFog, 1202)]
        [TestCase(EStageRule.SealedNest, 1203)]
        [TestCase(EStageRule.BossBulwark, 1204)]
        [TestCase(EStageRule.ArrowRain, 1205)]
        [TestCase(EStageRule.HunterTraps, 1206)]
        public void 규칙명은_EStageRule_순서_더하기_1201_id_문구다(EStageRule rule, int expectedId)
        {
            Assert.AreEqual(Text(expectedId), StageRuleText.Name(rule, Text));
            Assert.IsNotEmpty(StageRuleText.Name(rule, Text));
        }

        [Test]
        public void 규칙명_6종은_서로_다르다()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (EStageRule rule in AllRules)
            {
                names.Add(StageRuleText.Name(rule, Text));
            }

            Assert.AreEqual(6, names.Count);
        }

        //# --- shipped 규칙 10건 전부 ---

        [Test]
        public void 실제_규칙_10건은_설명이_비어_있지_않고_치환되지_않은_자리표시자가_없다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            int checkedCount = 0;

            for (int chapter = 1; chapter <= 2; ++chapter)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    foreach (StageRuleData data in db.GetRules(chapter, stage))
                    {
                        string description = StageRuleText.Description(data, Text);
                        string entry = StageRuleText.Entry(data, Text);

                        Assert.IsNotEmpty(description, $"{chapter}-{stage} {data.RuleId}");
                        StringAssert.DoesNotContain("{", description, $"{chapter}-{stage} {data.RuleId}");
                        StringAssert.DoesNotContain("}", description);
                        StringAssert.StartsWith(StageRuleText.Name(data.RuleId, Text) + "\n", entry);
                        StringAssert.EndsWith(description, entry);
                        ++checkedCount;
                    }
                }
            }
            Assert.AreEqual(10, checkedCount);
        }

        [Test]
        public void 트리거_규칙_설명만_영웅_HP_이하_문구로_시작한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            for (int chapter = 1; chapter <= 2; ++chapter)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    foreach (StageRuleData data in db.GetRules(chapter, stage))
                    {
                        string description = StageRuleText.Description(data, Text);
                        if (data.HasTrigger)
                        {
                            StringAssert.StartsWith("영웅 HP ", description, $"{chapter}-{stage}");
                        }
                        else
                        {
                            StringAssert.DoesNotStartWith("영웅 HP ", description, $"{chapter}-{stage}");
                        }
                    }
                }
            }
        }

        //# --- 규칙별 정확한 문구 (실제 수치 치환) ---

        [Test]
        public void 성수샘_설명은_주기_개방시간_회복퍼센트를_치환한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual("40초마다 8초간 샘이 열려, 샘 안의 영웅이 초당 최대 HP의 0.4% 회복", StageRuleText.Description(db.GetRules(1, 2)[0], Text));
            Assert.AreEqual("40초마다 8초간 샘이 열려, 샘 안의 영웅이 초당 최대 HP의 0.5% 회복", StageRuleText.Description(db.GetRules(2, 4)[0], Text));
        }

        [Test]
        public void 안개_설명은_배율과_최소_사거리를_치환한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual("몬스터 공격 사거리 x0.7 (최소 0.8)", StageRuleText.Description(db.GetRules(1, 3)[0], Text));
        }

        [Test]
        public void 봉인_설명은_종족명과_해제_HP퍼센트를_치환한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual("망령 둥지가 봉인되어 있다 - 영웅 HP 60% 이하에서 해제", StageRuleText.Description(db.GetRules(1, 4)[0], Text));
            Assert.AreEqual("환령 둥지가 봉인되어 있다 - 영웅 HP 70% 이하에서 해제", StageRuleText.Description(db.GetRules(2, 2)[0], Text));
        }

        [Test]
        public void 수호결계_설명은_트리거_퍼센트와_지속시간_배율을_치환한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual("영웅 HP 55% 이하에서 15초간 영웅이 받는 피해 x0.6", StageRuleText.Description(db.GetRules(1, 5)[0], Text));
        }

        [Test]
        public void 화살비와_덫_설명은_트리거와_수치를_치환한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual("영웅 HP 75% 이하에서 10초마다 몬스터 밀집 지점에 화살비 (4회 x 피해 50)", StageRuleText.Description(db.GetRules(2, 5)[0], Text));
            Assert.AreEqual("영웅 HP 35% 이하에서 영웅 주변에 덫 6개 - 몬스터 이동속도 x0.5, 초당 피해 10", StageRuleText.Description(db.GetRules(2, 5)[1], Text));
        }

        //# --- 숫자 표기 / 경계 ---

        [Test]
        public void 숫자는_소수_2자리까지_표기하고_불필요한_0은_제거한다()
        {
            StageRuleData data = StageRuleDataFactory.Make(EStageRule.DenseFog, null, ("monsterRangeMul", 0.456f), ("minRange", 2f));

            Assert.AreEqual("몬스터 공격 사거리 x0.46 (최소 2)", StageRuleText.Description(data, Text));
        }

        [Test]
        public void 트리거_퍼센트의_부동소수_오차는_정수로_표기된다()
        {
            StageRuleData data = StageRuleDataFactory.Make(EStageRule.BossBulwark, 0.55f, ("damageTakenMul", 0.6f), ("durationSec", 15f));

            StringAssert.StartsWith("영웅 HP 55% ", StageRuleText.Description(data, Text));
        }

        [Test]
        public void 설명_치환은_문화권과_무관하게_점_소수점을_쓴다()
        {
            System.Globalization.CultureInfo previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                StageRuleData data = StageRuleDataFactory.Make(EStageRule.DenseFog, null, ("monsterRangeMul", 0.7f), ("minRange", 0.8f));

                Assert.AreEqual("몬스터 공격 사거리 x0.7 (최소 0.8)", StageRuleText.Description(data, Text));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void 알_수_없는_규칙_ID의_설명은_빈_문자열이고_예외가_없다()
        {
            StageRuleData data = StageRuleDataFactory.Make((EStageRule)99, null);

            Assert.DoesNotThrow(() => StageRuleText.Description(data, Text));
            Assert.AreEqual(string.Empty, StageRuleText.Description(data, Text));
        }

        [Test]
        public void 파라미터가_빠진_규칙의_설명은_KeyNotFound다_데이터_검증이_선행돼야_한다()
        {
            StageRuleData data = StageRuleDataFactory.Make(EStageRule.DenseFog, null, ("monsterRangeMul", 0.7f));

            Assert.Throws<KeyNotFoundException>(() => StageRuleText.Description(data, Text));
        }

        //# --- 봉인 해제 HP% ---

        [Test]
        public void 봉인_해제_HP퍼센트는_대상_종에만_정수로_돌려준다()
        {
            StageRuleData phantom = StageRuleDataFactory.Make(EStageRule.SealedNest, null, ("lockedSpecies", 5f), ("unlockHpFraction", 0.7f));

            Assert.AreEqual(70, StageRuleText.SealUnlockHpPercent(phantom, EMonster.Phantom));
            foreach (EMonster other in new[] { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague })
            {
                Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(phantom, other), other.ToString());
            }
        }

        [Test]
        public void 봉인_규칙이_아니면_종과_무관하게_0이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            foreach (EMonster species in Enum.GetValues(typeof(EMonster)))
            {
                Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(db.GetRules(1, 2)[0], species), "샘");
                Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(db.GetRules(1, 5)[0], species), "결계");
            }
        }

        [Test]
        public void 실제_데이터의_봉인_표기_대상은_1_4_망령_60과_2_2_환령_70_두_쌍뿐이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            Dictionary<string, int> found = new Dictionary<string, int>();

            for (int chapter = 1; chapter <= 2; ++chapter)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    foreach (StageRuleData data in db.GetRules(chapter, stage))
                    {
                        foreach (EMonster species in Enum.GetValues(typeof(EMonster)))
                        {
                            int percent = StageRuleText.SealUnlockHpPercent(data, species);
                            if (percent > 0)
                            {
                                found[$"{chapter}-{stage}/{species}"] = percent;
                            }
                        }
                    }
                }
            }

            Assert.AreEqual(2, found.Count);
            Assert.AreEqual(60, found["1-4/Wraith"]);
            Assert.AreEqual(70, found["2-2/Phantom"]);
        }

        [TestCase(60, "봉인 - 영웅 HP 60% 이하 해제")]
        [TestCase(70, "봉인 - 영웅 HP 70% 이하 해제")]
        public void 스포너_봉인_표기_문구는_해제_HP퍼센트를_치환한다(int percent, string expected)
        {
            //# SpawnerStatusCell.SealTextStringId(1319) 와 같은 id.
            Assert.AreEqual(expected, string.Format(Text(1319), percent));
        }
    }
}
