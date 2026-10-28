using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.Tests.Helpers;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 데일리 본격 스위트 1 — 골든 벡터 · 날짜/시계/iat · 점수 · 연속/보너스 · 유물 필터 · 설정/문구/프리팹 (daily-challenge §11).
    public class DailyGoldenAndRulesTests : RelicStaticStateTestBase
    {
        private const long KstMidnight20753 = 1793026800000L;

        private DailyDatabase _prevDaily;

        [SetUp]
        public void 데일리_정적상태를_저장한다()
        {
            _prevDaily = DailyDatabase.Current;
        }

        [TearDown]
        public void 데일리_정적상태를_원복한다()
        {
            DailyDatabase.Current = _prevDaily;
        }

        private static DailyConfigRow Config()
        {
            return JsonUtility.FromJson<DailyConfigRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "DailyConfig.json"));
        }

        private static DailyDatabase Daily()
        {
            return DailyDatabase.Build(Config(), WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), 2);
        }

        //# ---------- 골든 벡터 (첫 구현 실행값 스냅샷 — 탑 셔플·salt·뽑기 순서·풀 정렬이 바뀌면 실패) ----------

        private static readonly string[] GoldenCombo =
        {
            "Paladin|5-4#1|3-5#2|HeartOfStonex3|BreedingCharmx1",
            "Knight|4-2#1|3-5#1|RustyBladex3|KingsCrownShardx1",
            "Paladin|4-3#1|1-5#1|SwiftBootsx3|BreedingCharmx1",
            "Archer|5-4#2|3-5#1|EmberBrandx3|CursedBannerx1",
            "Assassin|5-4#1|2-5#1|SwiftBootsx3|ThunderShardx1",
            "Berserker|4-4#2|5-5#2|HeartOfStonex3|KingsCrownShardx1",
            "Knight|5-4#2|5-5#2|SwiftBootsx3|ThunderShardx1",
            "Berserker|1-2#1|2-5#1|SwiftBootsx3|BreedingCharmx1",
            "Assassin|3-4#2|2-5#1|RustyBladex3|CursedBannerx1",
            "Archer|2-3#1|5-5#2|HeartOfStonex3|BreedingCharmx1",
        };

        private static readonly int[] GoldenPassive =
        {
            1916352103, 2016277163, 1660828889, 2104321781, 706323967,
            1182176735, 958424420, 365082160, 2110112776, 1984649661,
        };

        private static readonly int[] GoldenActive =
        {
            86450420, 1108110820, 571158110, 355298739, 917475836,
            1315502006, 238540501, 618673540, 1314680059, 793926464,
        };

        [Test]
        public void 골든_20753에서_20762의_comboKey_10개가_고정된다()
        {
            DailyDatabase db = Daily();

            for (int i = 0; i < GoldenCombo.Length; ++i)
            {
                int day = 20753 + i;
                DailyChallenge c = db.Generate(day);
                Assert.AreEqual(GoldenCombo[i], c.ComboKey, $"day {day} comboKey");
                Assert.AreEqual(c.HeroId, db.HeroOfDay(day), $"day {day} HeroOfDay 와 조합 영웅 일치");
                Assert.AreEqual(day, c.DayIndex);
            }
        }

        [Test]
        public void 골든_20753에서_20762의_카드_시드_20개가_고정된다()
        {
            DailyDatabase db = Daily();

            for (int i = 0; i < GoldenPassive.Length; ++i)
            {
                int day = 20753 + i;
                DailyChallenge c = db.Generate(day);
                Assert.AreEqual(GoldenPassive[i], c.PassiveSeed, $"day {day} 패시브 시드");
                Assert.AreEqual(GoldenActive[i], c.ActiveSeed, $"day {day} 액티브 시드");
            }
        }

        [Test]
        public void 골든_상시_페이즈_풀의_키_정렬순이_고정된다()
        {
            DailyDatabase db = Daily();

            Assert.AreEqual(
                "1-2#1,1-3#1,1-4#1,2-2#1,2-3#1,2-4#1,2-4#2,3-2#1,3-3#1,3-4#1,3-4#2,4-2#1,4-3#1,4-4#1,4-4#2,5-2#1,5-3#1,5-4#1,5-4#2",
                string.Join(",", db.AlwaysPool.Select(p => p.Key)));
            Assert.AreEqual(
                "1-5#1,2-5#1,2-5#2,3-5#1,3-5#2,4-5#1,4-5#2,5-5#1,5-5#2",
                string.Join(",", db.PhasePool.Select(p => p.Key)));
            Assert.AreEqual("AmbushHorn,EmberBrand,HeartOfStone,RustyBlade,SwiftBoots", string.Join(",", db.CommonRelics.Select(r => r.RelicId)));
            Assert.AreEqual("BreedingCharm,BrokenHourglass,CursedBanner,KingsCrownShard,ThunderShard,WrathSpirit", string.Join(",", db.RareRelics.Select(r => r.RelicId)));
        }

        [Test]
        public void 음수_dayIndex는_0일로_클램프되어_같은_조합이다()
        {
            DailyDatabase db = Daily();

            Assert.AreEqual(db.Generate(0).ComboKey, db.Generate(-5).ComboKey);
            Assert.AreEqual(0, db.Generate(-5).DayIndex);
            Assert.AreEqual(db.HeroOfDay(0), db.HeroOfDay(-1));
        }

        [Test]
        public void 프리셋_데이터는_원본_Stage_Rule_행_객체_그대로이고_Rules는_상시_페이즈_순_캐시다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            DailyDatabase db = DailyDatabase.Build(Config(), chapters, RelicDataSet.Shipped().Build(), 2);

            foreach (DailyRulePreset preset in db.AlwaysPool.Concat(db.PhasePool))
            {
                StageRuleData origin = chapters.GetRules(preset.ChapterId, preset.StageIndex).First(r => r.RuleIndex == preset.RuleIndex);
                Assert.AreSame(origin, preset.Data, preset.Key);
            }
            DailyChallenge c = db.Generate(20756);
            Assert.AreSame(c.Rules, c.Rules, "c779ef0 — 매 조회 새 배열을 만들지 않는다");
            Assert.AreSame(c.AlwaysRule.Data, c.Rules[0]);
            Assert.AreSame(c.PhaseRule.Data, c.Rules[1]);
        }

        [Test]
        public void 오늘의_유물_중첩은_설정값을_유물_최대중첩으로_클램프한다()
        {
            DailyConfigRow config = Config();
            config.commonRelicStack = 99;
            config.rareRelicStack = 99;
            RelicDatabase relics = RelicDataSet.Shipped().Build();
            DailyDatabase db = DailyDatabase.Build(config, WorldMapTestSupport.ShippedDb(), relics, 2);

            for (int day = 20753; day < 20783; ++day)
            {
                DailyChallenge c = db.Generate(day);
                Assert.AreEqual(relics.Get(c.RelicAId).MaxStack, c.RelicAStack, $"day {day} A");
                Assert.AreEqual(relics.Get(c.RelicBId).MaxStack, c.RelicBStack, $"day {day} B");
            }
        }

        [Test]
        public void 같은_카드_시드의_두_덱은_첫_3장이_같다()
        {
            List<CardData> cards = ((ECardId[])Enum.GetValues(typeof(ECardId))).Take(10).Select(id => FakeCardData.Create(id)).ToList();
            try
            {
                int seed = Daily().Generate(20753).PassiveSeed;
                CardDeck a = new CardDeck(cards, seed);
                CardDeck b = new CardDeck(cards, seed);

                CollectionAssert.AreEqual(a.Draw(3).Select(c => c.Id).ToList(), b.Draw(3).Select(c => c.Id).ToList());
                CollectionAssert.AreEqual(a.Draw(3).Select(c => c.Id).ToList(), b.Draw(3).Select(c => c.Id).ToList(), "두 번째 제시도 같다");
            }
            finally
            {
                foreach (CardData card in cards)
                {
                    UnityEngine.Object.DestroyImmediate(card);
                }
            }
        }

        //# ---------- 날짜 경계 ----------

        [Test]
        public void 오프셋_0이면_UTC_자정이_경계다()
        {
            long utcMidnight = 20753L * DailyRules.MsPerDay;

            Assert.AreEqual(20752, DailyRules.DayIndexOf(utcMidnight - 1, 0));
            Assert.AreEqual(20753, DailyRules.DayIndexOf(utcMidnight, 0));
        }

        [Test]
        public void 음수_오프셋은_UTC_자정_이후에_날짜가_바뀐다()
        {
            long utcMidnight = 20753L * DailyRules.MsPerDay;

            Assert.AreEqual(20752, DailyRules.DayIndexOf(utcMidnight, -300), "UTC-5 는 아직 전날 19시");
            Assert.AreEqual(20752, DailyRules.DayIndexOf(utcMidnight + 5 * 3600000L - 1, -300));
            Assert.AreEqual(20753, DailyRules.DayIndexOf(utcMidnight + 5 * 3600000L, -300));
        }

        [Test]
        public void 음수_시각은_내림으로_음수_날짜가_된다()
        {
            Assert.AreEqual(-1, DailyRules.DayIndexOf(-1L, 0));
            Assert.AreEqual(-1, DailyRules.DayIndexOf(-DailyRules.MsPerDay, 0));
            Assert.AreEqual(-2, DailyRules.DayIndexOf(-DailyRules.MsPerDay - 1, 0));
            Assert.AreEqual(0, DailyRules.DayIndexOf(0L, 0));
        }

        [Test]
        public void 초기화까지_남은_시간은_0시_정각에_하루_직전에_1ms다()
        {
            Assert.AreEqual(DailyRules.MsPerDay, DailyRules.MsUntilNextDay(KstMidnight20753, 540));
            Assert.AreEqual(1L, DailyRules.MsUntilNextDay(KstMidnight20753 - 1, 540));
            Assert.AreEqual(DailyRules.MsPerDay, DailyRules.MsUntilNextDay(-DailyRules.MsPerDay, 0), "음수 시각 자정");
        }

        [Test]
        public void dayIndex_20753은_10월_27일이다()
        {
            DailyRules.MonthDayOf(20753, out int month, out int day);
            Assert.AreEqual(10, month);
            Assert.AreEqual(27, day);

            DailyRules.MonthDayOf(-3, out month, out day);
            Assert.AreEqual(1, month, "음수는 0일(1970-01-01)로 클램프");
            Assert.AreEqual(1, day);
        }

        //# ---------- 단조 시계 ----------

        [Test]
        public void 단조_시계로만_전진하고_0시를_넘기면_오늘이_1_증가한다()
        {
            double t = 100.0;
            DailyClock clock = new DailyClock(KstMidnight20753 - 1000L, () => t);

            Assert.AreEqual(20752, clock.Today(540));
            t = 100.5;
            Assert.AreEqual(KstMidnight20753 - 500L, clock.NowUtcMs);
            Assert.AreEqual(20752, clock.Today(540));
            t = 101.0;
            Assert.AreEqual(20753, clock.Today(540));
        }

        [Test]
        public void 단조_시계가_뒤로_가거나_NaN이면_기준_시각에_머문다()
        {
            double t = 50.0;
            DailyClock clock = new DailyClock(KstMidnight20753, () => t);

            t = 10.0;
            Assert.AreEqual(KstMidnight20753, clock.NowUtcMs, "뒤로 감");
            t = double.NaN;
            Assert.AreEqual(KstMidnight20753, clock.NowUtcMs, "NaN");
            DailyClock noRealtime = new DailyClock(KstMidnight20753, null);
            Assert.AreEqual(KstMidnight20753, noRealtime.NowUtcMs, "realtime null 이면 고정");
        }

        //# ---------- iat 파싱 ----------

        private static string Jwt(string payloadJson)
        {
            string body = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return "eyJhbGciOiJSUzI1NiJ9." + body + ".sig";
        }

        [Test]
        public void iat는_초를_ms로_읽고_콜론_주변_공백을_허용한다()
        {
            Assert.IsTrue(JwtClaims.TryReadIatMs(Jwt("{\"iss\":\"x\",\"iat\":1793026800,\"exp\":1793030400}"), out long ms));
            Assert.AreEqual(KstMidnight20753, ms);
            Assert.IsTrue(JwtClaims.TryReadIatMs(Jwt("{\"iat\" : 123}"), out ms));
            Assert.AreEqual(123000L, ms);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("onlyonepart")]
        [TestCase("a.abcde.c")]
        [TestCase("a.!!!!.c")]
        public void 형식이_잘못된_토큰은_실패하고_0을_돌려준다(string token)
        {
            Assert.IsFalse(JwtClaims.TryReadIatMs(token, out long ms));
            Assert.AreEqual(0L, ms);
        }

        [TestCase("{\"exp\":1793026800}")]
        [TestCase("{\"iat\":0}")]
        [TestCase("{\"iat\":-5}")]
        [TestCase("{\"iat\":\"1793026800\"}")]
        [TestCase("{\"iat\":99999999999999999999}")]
        [TestCase("")]
        public void iat가_없거나_양의_정수가_아니면_실패한다(string payload)
        {
            Assert.IsFalse(JwtClaims.TryReadIatMs(Jwt(payload), out long ms));
            Assert.AreEqual(0L, ms);
        }

        //# ---------- 점수 ----------

        [TestCase(0f, 1)]
        [TestCase(-5f, 1)]
        [TestCase(0.0004f, 1)]
        [TestCase(300f, 300000)]
        [TestCase(300.5f, 300000)]
        [TestCase(float.PositiveInfinity, 300000)]
        [TestCase(float.NegativeInfinity, 1)]
        public void 승리_점수는_1에서_300000으로_클램프된다(float elapsed, int expected)
        {
            Assert.AreEqual(expected, DailyRules.WinScore(elapsed));
        }

        [TestCase(10000, 10000, 301000)]
        [TestCase(9999, 10000, 301000)]
        [TestCase(0, 10000, 300001)]
        [TestCase(-50, 10000, 300001)]
        [TestCase(20000, 10000, 301000)]
        [TestCase(5, 0, 301000)]
        [TestCase(5, -10, 301000)]
        public void 패배_점수는_300001에서_301000_범위다(int hp, int max, int expected)
        {
            int score = DailyRules.LoseScore(hp, max);

            Assert.AreEqual(expected, score);
            Assert.IsFalse(DailyRules.IsWinScore(score));
            Assert.IsTrue(DailyRules.IsValidScore(score));
        }

        [Test]
        public void 승패_경계와_유효_범위_경계()
        {
            Assert.IsTrue(DailyRules.IsWinScore(1));
            Assert.IsTrue(DailyRules.IsWinScore(300000));
            Assert.IsFalse(DailyRules.IsWinScore(300001));
            Assert.IsFalse(DailyRules.IsWinScore(0));
            Assert.IsTrue(DailyRules.IsValidScore(301000));
            Assert.IsFalse(DailyRules.IsValidScore(301001));
            Assert.IsFalse(DailyRules.IsValidScore(0));
            Assert.IsFalse(DailyRules.IsValidScore(-1));
        }

        [TestCase(300120, 12)]
        [TestCase(300005, 1)]
        [TestCase(300004, 0)]
        [TestCase(301000, 100)]
        public void 패배_점수의_HP_퍼센트는_반올림이다(int score, int percent)
        {
            Assert.AreEqual(percent, DailyRules.LosePercent(score));
        }

        [TestCase(0L, 1000, true)]
        [TestCase(-1L, 5, true)]
        [TestCase(1000L, 999, true)]
        [TestCase(1000L, 1000, false)]
        [TestCase(1000L, 1001, false)]
        [TestCase(0L, 0, false)]
        [TestCase(0L, 301001, false)]
        public void 순위_제출은_더_작을_때만_갱신하고_동점과_무효는_미갱신(long existing, int score, bool expected)
        {
            Assert.AreEqual(expected, DailyLeaderboard.ShouldReplace(existing, score));
        }

        [Test]
        public void 순위_문서_id는_날짜_언더스코어_uid다()
        {
            Assert.AreEqual("20753_abc", DailyLeaderboard.DocId(20753, "abc"));
        }

        //# ---------- 최고 점수 갱신 ----------

        [Test]
        public void 무효_점수와_음수_날짜는_기록하지_않는다()
        {
            MetaProfile p = new MetaProfile { DailyStreak = 2 };

            DailyRecordResult zero = p.RecordDailyResult(100, 0);
            DailyRecordResult over = p.RecordDailyResult(100, 301001);
            DailyRecordResult negDay = p.RecordDailyResult(-1, 1000);

            Assert.AreEqual(0, p.DailyResults.Count);
            Assert.AreEqual(2, p.DailyStreak, "연속도 불변");
            Assert.IsFalse(zero.IsFirstFinish || over.IsFirstFinish || negDay.IsFirstFinish);
            Assert.AreEqual(0, zero.BestScore);
        }

        [Test]
        public void 승리_후_패배나_더_느린_승리는_최고_점수를_바꾸지_않고_첫_승리도_아니다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordDailyResult(100, 150000);
            DailyRecordResult lose = p.RecordDailyResult(100, 300500);
            DailyRecordResult slower = p.RecordDailyResult(100, 160000);

            Assert.AreEqual(150000, lose.BestScore);
            Assert.AreEqual(150000, slower.BestScore);
            Assert.IsFalse(lose.IsFirstWin);
            Assert.IsFalse(slower.IsFirstWin);
            Assert.IsFalse(lose.IsFirstFinish);
            Assert.AreEqual(1, p.DailyResults.Count);
        }

        [Test]
        public void 패배만_한_날은_첫_승리_미수령이고_더_좋은_패배는_갱신된다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordDailyResult(100, 300800);
            DailyRecordResult better = p.RecordDailyResult(100, 300200);

            Assert.AreEqual(300200, better.BestScore);
            Assert.IsFalse(p.HasDailyWin(100));
            Assert.IsTrue(p.HasDailyResult(100));
        }

        //# ---------- 연속 · 보너스 ----------

        private static DailyConfigRow ConfigWith(float perDay, int maxDays, int souls)
        {
            DailyConfigRow c = Config();
            c.streakBonusPerDay = perDay;
            c.streakBonusMaxDays = maxDays;
            c.firstWinSouls = souls;
            return c;
        }

        [TestCase(-3, "1")]
        [TestCase(0, "1")]
        [TestCase(1, "1")]
        [TestCase(3, "1.2")]
        [TestCase(4, "1.3")]
        [TestCase(7, "1.6")]
        [TestCase(10, "1.6")]
        public void 연속_배율은_decimal로_정확하다(int streak, string expected)
        {
            Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), DailyRules.StreakMultiplier(streak, Config()));
        }

        [TestCase(1, 150)]
        [TestCase(3, 180)]
        [TestCase(7, 240)]
        [TestCase(0, 150)]
        public void 첫_승리_보너스는_150_180_240이다(int streak, int souls)
        {
            Assert.AreEqual(souls, DailyRules.BonusSouls(streak, Config()));
        }

        [Test]
        public void 배율_설정이_NaN_음수_상한0이면_배율_1이고_음수_보상은_0이다()
        {
            Assert.AreEqual(1m, DailyRules.StreakMultiplier(5, ConfigWith(float.NaN, 7, 150)));
            Assert.AreEqual(1m, DailyRules.StreakMultiplier(5, ConfigWith(-0.5f, 7, 150)));
            Assert.AreEqual(1m, DailyRules.StreakMultiplier(5, ConfigWith(0.1f, 0, 150)));
            Assert.AreEqual(0, DailyRules.BonusSouls(5, ConfigWith(0.1f, 7, -100)));
            Assert.AreEqual(116, DailyRules.BonusSouls(2, ConfigWith(0.1f, 7, 106)), "106 x 1.1 = 116.6 → floor 116");
        }

        [Test]
        public void 배율_표기는_불필요한_0을_뺀다()
        {
            Assert.AreEqual("1", DailyRules.FormatMultiplier(1m));
            Assert.AreEqual("1.3", DailyRules.FormatMultiplier(1.3m));
            Assert.AreEqual("1.6", DailyRules.FormatMultiplier(1.60m));
            Assert.AreEqual("1.25", DailyRules.FormatMultiplier(1.25m));
        }

        [Test]
        public void 도전만_시작하고_못_끝낸_날은_참가가_아니라_다음날_연속이_1로_끊긴다()
        {
            MetaProfile p = new MetaProfile { DailyStreak = 5 };
            p.DailyResults.Add(new DailyResultEntry { Day = 99, BestScore = 200000 });
            p.UseDailyAttempt(100);

            Assert.AreEqual(0, p.DisplayDailyStreak(101), "어제(100) 끝낸 판 없음 → 끊김 표시 0");
            DailyRecordResult r = p.RecordDailyResult(101, 200000);

            Assert.AreEqual(1, r.Streak);
            Assert.AreEqual(1, p.DailyStreak);
        }

        [Test]
        public void 미리보기_연속은_어제_행_유무와_무관하게_실제_기록_연속과_같다()
        {
            MetaProfile withYesterday = new MetaProfile { DailyStreak = 3 };
            withYesterday.DailyResults.Add(new DailyResultEntry { Day = 99, BestScore = 300500 });
            MetaProfile withoutYesterday = new MetaProfile { DailyStreak = 3 };
            withoutYesterday.DailyResults.Add(new DailyResultEntry { Day = 97, BestScore = 300500 });

            int previewA = withYesterday.PreviewDailyStreak(100);
            int previewB = withoutYesterday.PreviewDailyStreak(100);
            DailyRecordResult a = withYesterday.RecordDailyResult(100, 120000);
            DailyRecordResult b = withoutYesterday.RecordDailyResult(100, 120000);

            Assert.AreEqual(4, previewA);
            Assert.AreEqual(1, previewB);
            Assert.AreEqual(previewA, a.Streak);
            Assert.AreEqual(previewB, b.Streak);
            Assert.AreEqual(DailyRules.BonusSouls(previewA, Config()), DailyRules.BonusSouls(a.Streak, Config()));
            Assert.AreEqual(4, withYesterday.PreviewDailyStreak(100), "오늘 끝낸 뒤 미리보기 = 저장값");
        }

        [Test]
        public void 표시_연속은_오늘이나_어제_끝낸_판이_있을_때만_저장값이다()
        {
            MetaProfile p = new MetaProfile { DailyStreak = 5 };
            p.DailyResults.Add(new DailyResultEntry { Day = 98, BestScore = 300500 });

            Assert.AreEqual(5, p.DisplayDailyStreak(98));
            Assert.AreEqual(5, p.DisplayDailyStreak(99));
            Assert.AreEqual(0, p.DisplayDailyStreak(100));
            Assert.AreEqual(5, p.DailyStreak, "표시는 저장값을 바꾸지 않는다");
        }

        [Test]
        public void 연속_7일_넘게_이어져도_배율은_상한_7일에서_멈춘다()
        {
            MetaProfile p = new MetaProfile();
            for (int day = 100; day < 110; ++day)
            {
                DailyRecordResult r = p.RecordDailyResult(day, 300500);
                Assert.AreEqual(day - 99, r.Streak, $"day {day}");
            }

            Assert.AreEqual(10, p.DailyStreak);
            Assert.AreEqual(1.6m, DailyRules.StreakMultiplier(p.DailyStreak, Config()));
            Assert.AreEqual(7, p.DailyResults.Count, "7일 보관");
        }

        //# ---------- 도전 횟수 · 시작 판정 ----------

        [Test]
        public void 세번_차감하면_남은_도전_0이고_설정이_줄어도_음수가_아니며_세이브는_그대로다()
        {
            MetaProfile p = new MetaProfile();
            p.UseDailyAttempt(200);
            p.UseDailyAttempt(200);
            Assert.AreEqual(1, p.DailyAttemptsLeft(200, 3));
            p.UseDailyAttempt(200);

            Assert.AreEqual(0, p.DailyAttemptsLeft(200, 3));
            Assert.AreEqual(0, p.DailyAttemptsLeft(200, 2), "설정 감소 → Max(0, …)");
            Assert.AreEqual(3, p.GetDailyAttemptsUsed(200), "세이브 비수정");
            Assert.AreEqual(3, p.DailyAttemptsLeft(201, 3), "다음 날은 새로 3회");
        }

        [Test]
        public void 음수_날짜_차감은_무시하고_리스트가_null이어도_안전하다()
        {
            MetaProfile p = new MetaProfile { DailyDays = null, DailyResults = null };

            Assert.AreEqual(3, p.DailyAttemptsLeft(5, 3));
            Assert.IsFalse(p.TryGetDailyBest(5, out int _));
            p.UseDailyAttempt(-1);
            Assert.IsNull(p.DailyDays, "가드가 먼저라 리스트를 만들지 않는다");
            p.UseDailyAttempt(5);
            Assert.AreEqual(1, p.GetDailyAttemptsUsed(5));
            Assert.IsNotNull(p.DailyResults);
        }

        private static MetaProfile UnlockedProfile()
        {
            return new MetaProfile { ClearedStage = 5 };
        }

        private static DailyClock ClockAt(long utcMs)
        {
            return new DailyClock(utcMs, () => 0.0);
        }

        [Test]
        public void 시작_판정은_비활성_잠김_오프라인_날짜변경_소진_성공을_구분한다()
        {
            DailyDatabase db = Daily();
            DailyClock clock = ClockAt(KstMidnight20753 + 1000L);
            MetaProfile exhausted = UnlockedProfile();
            for (int i = 0; i < 3; ++i)
            {
                exhausted.UseDailyAttempt(20753);
            }

            Assert.AreEqual(EDailyStartCheck.Disabled, DailyRules.CheckStart(UnlockedProfile(), null, clock, 20753));
            Assert.AreEqual(EDailyStartCheck.Locked, DailyRules.CheckStart(null, db, clock, 20753));
            Assert.AreEqual(EDailyStartCheck.Locked, DailyRules.CheckStart(new MetaProfile { ClearedStage = 4 }, db, clock, 20753));
            Assert.AreEqual(EDailyStartCheck.Offline, DailyRules.CheckStart(UnlockedProfile(), db, null, 20753));
            Assert.AreEqual(EDailyStartCheck.DayChanged, DailyRules.CheckStart(UnlockedProfile(), db, clock, 20752));
            Assert.AreEqual(EDailyStartCheck.Exhausted, DailyRules.CheckStart(exhausted, db, clock, 20753));
            Assert.AreEqual(EDailyStartCheck.Ok, DailyRules.CheckStart(UnlockedProfile(), db, clock, 20753));
        }

        [Test]
        public void 재도전_판정은_단조_시계로_0시를_넘기면_DayChanged다()
        {
            DailyDatabase db = Daily();
            double t = 0.0;
            DailyClock clock = new DailyClock(KstMidnight20753 - 2000L, () => t);
            MetaProfile p = UnlockedProfile();
            p.UseDailyAttempt(20752);

            Assert.AreEqual(EDailyStartCheck.Ok, DailyRules.CheckStart(p, db, clock, 20752));
            t = 3.0;
            Assert.AreEqual(EDailyStartCheck.DayChanged, DailyRules.CheckStart(p, db, clock, 20752), "판 도중 0시 경과 → 재도전 거부");
            Assert.AreEqual(1, p.GetDailyAttemptsUsed(20752), "판정은 차감하지 않는다");
        }

        //# ---------- 유물 필터 ----------

        [Test]
        public void 데일리에서_유지되는_효과는_정확히_배속_소울_경험_패배소울_4종이다()
        {
            HashSet<ERelicEffectKind> kept = new HashSet<ERelicEffectKind>();
            foreach (ERelicEffectKind kind in (ERelicEffectKind[])Enum.GetValues(typeof(ERelicEffectKind)))
            {
                if (RelicBattleBonus.IsKeptInDaily(kind))
                {
                    kept.Add(kind);
                }
            }

            CollectionAssert.AreEquivalent(
                new[] { ERelicEffectKind.BattleSpeed, ERelicEffectKind.SoulBonus, ERelicEffectKind.XpBonus, ERelicEffectKind.LoseSoulBonus },
                kept);
        }

        private static MetaProfile AllRelicsMaxed(RelicDatabase db)
        {
            MetaProfile p = new MetaProfile();
            foreach (RelicData data in db.Relics)
            {
                p.Relics.Add(new RelicEntry { RelicId = data.RelicId, Stack = data.MaxStack });
            }
            return p;
        }

        private static string Describe(RelicBattleBonus b)
        {
            StringBuilder sb = new StringBuilder();
            foreach (EMonsterStatKind kind in (EMonsterStatKind[])Enum.GetValues(typeof(EMonsterStatKind)))
            {
                sb.Append(kind).Append('=').Append(b.GetStatMul(kind).ToString("R")).Append(';');
            }
            sb.Append($"period={b.SpawnerPeriodMul:R};prelude={b.SpawnPreludeAdvanceSec:R};burn={b.HeroBurnRatioPerSec:R};");
            sb.Append($"light={b.Lightnings.Count};summon={b.OpeningSummons.Count};frenzy={b.Frenzies.Count};atk={b.HeroAttackFactor:R};");
            sb.Append($"speed={b.BattleSpeedScale:R};soul={b.SoulBonus:R};xp={b.XpBonus:R};lose={b.LoseSoulBonus:R}");
            return sb.ToString();
        }

        [Test]
        public void 전_유물_만렙_보유자도_데일리에선_전투효과_0이고_보상_배속은_평소와_같다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            MetaProfile p = AllRelicsMaxed(db);

            RelicBattleBonus normal = RelicBattleBonus.From(p, db);
            RelicBattleBonus daily = RelicBattleBonus.FromDaily(p, db, "__none__", 1, null, 1);

            Assert.AreEqual(Describe(RelicBattleBonus.None).Split(new[] { ";speed" }, StringSplitOptions.None)[0],
                Describe(daily).Split(new[] { ";speed" }, StringSplitOptions.None)[0], "전투 영향 효과 전부 꺼짐");
            Assert.IsTrue(daily.HasBattleSpeed);
            Assert.AreEqual(normal.BattleSpeedScale, daily.BattleSpeedScale);
            Assert.AreEqual(normal.SoulBonus, daily.SoulBonus);
            Assert.AreEqual(normal.XpBonus, daily.XpBonus);
            Assert.AreEqual(normal.LoseSoulBonus, daily.LoseSoulBonus);
            Assert.Greater(normal.SoulBonus + normal.XpBonus + normal.LoseSoulBonus, 0f, "출하 데이터에 보상 유물이 실제로 있다(대조)");
        }

        [Test]
        public void 오늘의_유물_2개는_그_유물을_그_중첩만큼_보유한_것과_같은_효과다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            DailyDatabase daily = Daily();
            for (int day = 20753; day <= 20762; ++day)
            {
                DailyChallenge c = daily.Generate(day);
                MetaProfile owner = new MetaProfile();
                owner.Relics.Add(new RelicEntry { RelicId = c.RelicAId, Stack = c.RelicAStack });
                owner.Relics.Add(new RelicEntry { RelicId = c.RelicBId, Stack = c.RelicBStack });

                RelicBattleBonus viaDaily = RelicBattleBonus.FromDaily(new MetaProfile(), db, c.RelicAId, c.RelicAStack, c.RelicBId, c.RelicBStack);

                Assert.AreEqual(Describe(RelicBattleBonus.From(owner, db)), Describe(viaDaily), $"day {day}");
            }
        }

        [Test]
        public void 오늘의_유물은_개인_보유분과_별개로_더해진다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            MetaProfile p = new MetaProfile();
            p.Relics.Add(new RelicEntry { RelicId = "RustyBlade", Stack = 5 });

            RelicBattleBonus daily = RelicBattleBonus.FromDaily(p, db, "SwiftBoots", 3, "BreedingCharm", 1);
            MetaProfile expectedOwner = new MetaProfile();
            expectedOwner.Relics.Add(new RelicEntry { RelicId = "SwiftBoots", Stack = 3 });
            expectedOwner.Relics.Add(new RelicEntry { RelicId = "BreedingCharm", Stack = 1 });

            Assert.AreEqual(Describe(RelicBattleBonus.From(expectedOwner, db)), Describe(daily), "개인 녹슨 칼날(능력치)은 꺼지고 오늘의 유물만");
            Assert.AreEqual(Describe(RelicBattleBonus.None), Describe(RelicBattleBonus.FromDaily(p, null, "SwiftBoots", 3, null, 0)), "db null 이면 빈 집계");
        }

        //# ---------- 설정 JSON 검증 ----------

        [TestCase("utcOffsetMinutes", -721)]
        [TestCase("utcOffsetMinutes", 841)]
        [TestCase("unlockClearedStage", 0)]
        [TestCase("unlockClearedStage", 999)]
        [TestCase("attemptsPerDay", 0)]
        [TestCase("baseStageIndex", 0)]
        [TestCase("baseStageIndex", 6)]
        [TestCase("difficulty", -1)]
        [TestCase("difficulty", 3)]
        [TestCase("commonRelicStack", 0)]
        [TestCase("rareRelicStack", 0)]
        [TestCase("firstWinSouls", -1)]
        [TestCase("streakBonusPerDay", -0.1f)]
        [TestCase("streakBonusPerDay", float.NaN)]
        [TestCase("streakBonusMaxDays", 0)]
        [TestCase("leaderboardTop", 0)]
        [TestCase("leaderboardTop", 101)]
        public void 설정_범위_위반은_로드_오류라_데일리가_비활성된다(string field, object value)
        {
            DailyConfigRow config = Config();
            FieldInfo info = typeof(DailyConfigRow).GetField(field);
            Assert.IsNotNull(info, field);
            info.SetValue(config, Convert.ChangeType(value, info.FieldType, System.Globalization.CultureInfo.InvariantCulture));

            ChapterDataException e = Assert.Throws<ChapterDataException>(() =>
                DailyDatabase.Build(config, WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), 2));
            StringAssert.Contains("DailyConfig", e.Message);
        }

        [TestCase("utcOffsetMinutes", -720)]
        [TestCase("utcOffsetMinutes", 840)]
        [TestCase("difficulty", 2)]
        [TestCase("difficulty", 0)]
        [TestCase("leaderboardTop", 1)]
        [TestCase("leaderboardTop", 100)]
        [TestCase("streakBonusPerDay", 0f)]
        [TestCase("firstWinSouls", 0)]
        public void 설정_범위_경계값은_통과한다(string field, object value)
        {
            DailyConfigRow config = Config();
            FieldInfo info = typeof(DailyConfigRow).GetField(field);
            info.SetValue(config, Convert.ChangeType(value, info.FieldType, System.Globalization.CultureInfo.InvariantCulture));

            Assert.DoesNotThrow(() => DailyDatabase.Build(config, WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), 2));
        }

        [Test]
        public void 출하_설정은_기획값_그대로이고_필수_입력이_null이면_로드_오류다()
        {
            DailyConfigRow c = Config();

            Assert.AreEqual(540, c.utcOffsetMinutes);
            Assert.AreEqual(5, c.unlockClearedStage);
            Assert.AreEqual(3, c.attemptsPerDay);
            Assert.AreEqual(2, c.baseStageIndex);
            Assert.AreEqual(1, c.difficulty);
            Assert.AreEqual(150, c.firstWinSouls);
            Assert.AreEqual(7, c.streakBonusMaxDays);
            Assert.AreEqual(100, c.leaderboardTop);
            Assert.Throws<ChapterDataException>(() => DailyDatabase.Build(null, WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), 2));
            Assert.Throws<ChapterDataException>(() => DailyDatabase.Build(c, null, RelicDataSet.Shipped().Build(), 2));
            Assert.Throws<ChapterDataException>(() => DailyDatabase.Build(c, WorldMapTestSupport.ShippedDb(), null, 2));
            Assert.Throws<ChapterDataException>(() => DailyDatabase.Build(c, WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), 0),
                "Difficulty 표 미로드(최대 0)면 Normal 고정 설정은 위반");
        }

        [Test]
        public void 해금은_1_5_클리어부터다()
        {
            DailyDatabase db = Daily();

            Assert.IsFalse(db.IsUnlocked(4));
            Assert.IsTrue(db.IsUnlocked(5));
            Assert.AreEqual(0, db.HeroChapterOf("Nobody"));
            Assert.AreEqual(0, db.HeroChapterOf(null));
            Assert.AreEqual(5, db.HeroChapterOf("Paladin"));
        }

        //# ---------- 문구 · 프리팹 ----------

        [Test]
        public void Strings_Ko_1437에서_1464는_28개가_연속_존재하고_비어있지_않다()
        {
            for (int id = DailyStrings.Tile; id <= DailyStrings.Claimed; ++id)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id}");
            }
            Assert.AreEqual(28, DailyStrings.Claimed - DailyStrings.Tile + 1);
            Assert.IsNotEmpty(WorldMapTestSupport.Text(DailyStrings.LockedToast));
            Assert.IsNotEmpty(WorldMapTestSupport.Text(DailyStrings.ChapterTitle));
        }

        [Test]
        public void 포맷_인자가_필요한_데일리_문구는_자리표시를_갖는다()
        {
            int[] twoArgs = { DailyStrings.Date, DailyStrings.AttemptsLeft, DailyStrings.StreakMultiplier };
            int[] oneArg =
            {
                DailyStrings.ResetIn, DailyStrings.Streak, DailyStrings.FirstWinReward, DailyStrings.LoseScore, DailyStrings.FirstWinBanner,
                DailyStrings.RankPlace, DailyStrings.RetryLeft, DailyStrings.ModeTitle, DailyStrings.Guide,
            };
            foreach (int id in twoArgs)
            {
                StringAssert.Contains("{0}", WorldMapTestSupport.Text(id), $"id {id}");
                StringAssert.Contains("{1}", WorldMapTestSupport.Text(id), $"id {id}");
            }
            foreach (int id in oneArg)
            {
                StringAssert.Contains("{0}", WorldMapTestSupport.Text(id), $"id {id}");
            }
            Assert.AreEqual("연속 4일 x1.3", string.Format(WorldMapTestSupport.Text(DailyStrings.StreakMultiplier), 4, DailyRules.FormatMultiplier(1.3m)));
        }

        [Test]
        public void 데일리_팝업과_마을_타일의_직렬화_참조가_전부_연결되어_있다()
        {
            AssertNoNullReferences("Assets/_Lair/Art/UI/DailyPopup.prefab", typeof(DailyPopup));
            AssertSerializedRefs("Assets/_Lair/Art/UI/VillageHud.prefab", typeof(VillageHud), "_dailyButton", "_dailyLockOverlay", "_dailyNewFrame");
        }

        private static void AssertSerializedRefs(string path, Type componentType, params string[] fields)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(prefab.GetComponent(componentType));
            foreach (string field in fields)
            {
                UnityEditor.SerializedProperty prop = so.FindProperty(field);
                Assert.IsNotNull(prop, $"{componentType.Name}.{field} 없음");
                Assert.IsNotNull(prop.objectReferenceValue, $"{componentType.Name}.{field} 미연결");
            }
        }

        //# 이 컴포넌트 자신의 [SerializeField] 만 본다 — 배열은 비어 있지 않아야 하고 원소도 전부 연결.
        private static void AssertNoNullReferences(string path, Type componentType)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            Component component = prefab.GetComponent(componentType);
            Assert.IsNotNull(component, componentType.Name);
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(component);
            foreach (FieldInfo field in componentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
                    continue;
                UnityEditor.SerializedProperty prop = so.FindProperty(field.Name);
                Assert.IsNotNull(prop, $"{componentType.Name}.{field.Name} 직렬화 안 됨");
                if (prop.propertyType == UnityEditor.SerializedPropertyType.ObjectReference)
                {
                    Assert.IsNotNull(prop.objectReferenceValue, $"{componentType.Name}.{field.Name} 미연결");
                }
                else if (prop.isArray && prop.propertyType != UnityEditor.SerializedPropertyType.String)
                {
                    for (int i = 0; i < prop.arraySize; ++i)
                    {
                        UnityEditor.SerializedProperty element = prop.GetArrayElementAtIndex(i);
                        if (element.propertyType == UnityEditor.SerializedPropertyType.ObjectReference)
                        {
                            Assert.IsNotNull(element.objectReferenceValue, $"{componentType.Name}.{field.Name}[{i}] 미연결");
                        }
                    }
                    if (prop.arrayElementType.StartsWith("PPtr"))
                    {
                        Assert.Greater(prop.arraySize, 0, $"{componentType.Name}.{field.Name} 배열이 비어 있음");
                    }
                }
            }
        }
    }
}
