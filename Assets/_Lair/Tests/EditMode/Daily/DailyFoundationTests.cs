using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 데일리 자체 테스트 (gameplay-programmer, 정상 + 엣지) — 본격 스위트(골든 벡터·엣지 망라)는 test-engineer (daily-challenge §11).
    public class DailyFoundationTests : ChapterStaticStateTestBase
    {
        private static DailyConfigRow ShippedConfig()
        {
            return JsonUtility.FromJson<DailyConfigRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "DailyConfig.json"));
        }

        private static DailyDatabase ShippedDaily()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            return DailyDatabase.Build(ShippedConfig(), chapters, RelicDataSet.Shipped().Build(), 2);
        }

        [Test]
        public void 한국시간_0시_경계에서_dayIndex가_20752에서_20753으로_바뀐다()
        {
            Assert.AreEqual(20752, DailyRules.DayIndexOf(1793026799000L, 540));
            Assert.AreEqual(20753, DailyRules.DayIndexOf(1793026800000L, 540));
        }

        [Test]
        public void 점수는_승리_ms_패배_300000더하기_HP천분율_올림이고_보너스는_decimal_floor다()
        {
            Assert.AreEqual(171300, DailyRules.WinScore(171.3f));
            Assert.AreEqual(300120, DailyRules.LoseScore(1200, 10000));
            Assert.AreEqual(300001, DailyRules.LoseScore(1, 10000), "남은 HP 0.01% 는 올림으로 1 — 승리 점수와 겹치지 않는다");
            Assert.AreEqual(1, DailyRules.WinScore(float.NaN), "NaN 경과는 1 로 막는다");
            Assert.AreEqual(195, DailyRules.BonusSouls(4, ShippedConfig()), "150 x 1.3 = 195 (float 로 194 가 되지 않는다)");
            Assert.AreEqual(240, DailyRules.BonusSouls(10, ShippedConfig()), "7일 상한 x1.6");
        }

        [Test]
        public void 출하_데이터_풀은_상시19_페이즈9이고_같은_날은_항상_같은_조합이다()
        {
            DailyDatabase db = ShippedDaily();

            Assert.AreEqual(19, db.AlwaysPool.Count);
            Assert.AreEqual(9, db.PhasePool.Count);
            Assert.AreEqual(5, db.CommonRelics.Count);
            Assert.AreEqual(6, db.RareRelics.Count);
            Assert.AreEqual(db.Generate(20753).ComboKey, ShippedDaily().Generate(20753).ComboKey);
            Assert.AreEqual(db.Generate(20753).PassiveSeed, ShippedDaily().Generate(20753).PassiveSeed);
        }

        [Test]
        public void 천일_동안_블록마다_영웅이_한번씩이고_연속_이틀_같은_영웅과_봉인_임계_충돌이_없다()
        {
            DailyDatabase db = ShippedDaily();
            string previous = null;
            for (int day = 20750; day < 21750; ++day)
            {
                DailyChallenge c = db.Generate(day);
                Assert.AreNotEqual(previous, c.HeroId, $"day {day} 연속 같은 영웅");
                previous = c.HeroId;
                if (c.AlwaysRule.Data.RuleId == EStageRule.SealedNest)
                {
                    float gap = System.Math.Abs(c.AlwaysRule.Data.Params["unlockHpFraction"] - c.PhaseRule.Data.TriggerHpFraction);
                    Assert.Greater(gap, 0.0001f, $"day {day} 봉인 해제 임계 = 페이즈 트리거");
                }
                Assert.AreNotEqual(0, c.PassiveSeed);
                Assert.AreNotEqual(0, c.ActiveSeed);
            }
            for (int block = 4150; block < 4160; ++block)
            {
                HashSet<string> heroes = new HashSet<string>();
                for (int i = 0; i < 5; ++i)
                {
                    heroes.Add(db.HeroOfDay(block * 5 + i));
                }
                Assert.AreEqual(5, heroes.Count, $"block {block}");
            }
        }

        [Test]
        public void 첫_종료가_연속을_갱신하고_첫_승리만_보너스_대상이며_두번째_승리는_아니다()
        {
            MetaProfile p = new MetaProfile { DailyStreak = 3 };
            p.DailyResults.Add(new DailyResultEntry { Day = 99, BestScore = 300500 });

            DailyRecordResult lose = p.RecordDailyResult(100, 300200);
            DailyRecordResult win = p.RecordDailyResult(100, 150000);
            DailyRecordResult again = p.RecordDailyResult(100, 140000);

            Assert.IsTrue(lose.IsFirstFinish);
            Assert.AreEqual(4, lose.Streak, "어제 행 있음 → +1");
            Assert.IsFalse(lose.IsFirstWin);
            Assert.IsTrue(win.IsFirstWin);
            Assert.IsFalse(again.IsFirstWin);
            Assert.AreEqual(4, again.Streak, "같은 날 두번째 판은 연속 불변");
            Assert.AreEqual(140000, again.BestScore);
        }

        [Test]
        public void 오래된_클라우드_복원은_오늘_쓴_도전_수를_되돌리지_못한다()
        {
            MetaProfile local = new MetaProfile();
            local.UseDailyAttempt(200);
            local.UseDailyAttempt(200);
            MetaProfile oldCloud = new MetaProfile();

            List<DailyDayEntry> days = MetaProfile.CopyDailyDays(local.DailyDays);
            List<DailyResultEntry> results = MetaProfile.CopyDailyResults(local.DailyResults);
            local.CopyFrom(oldCloud);
            local.MergeDailyFrom(days, results, 0);

            Assert.AreEqual(1, local.DailyAttemptsLeft(200, 3));
        }

        [Test]
        public void 데일리_유물_집계는_개인_전투유물을_끄고_오늘의_유물만_더한다()
        {
            RelicDatabase relics = RelicDataSet.Shipped().Build();
            MetaProfile p = new MetaProfile();
            p.AddRelic("ThunderShard");
            p.AddRelic("HasteHourglass");

            RelicBattleBonus normal = RelicBattleBonus.From(p, relics);
            RelicBattleBonus daily = RelicBattleBonus.FromDaily(p, relics, "RustyBlade", 3, "BrokenHourglass", 1);

            Assert.AreEqual(1, normal.Lightnings.Count);
            Assert.AreEqual(0, daily.Lightnings.Count, "개인 뇌운 조각은 데일리에서 꺼진다");
            Assert.IsTrue(daily.HasBattleSpeed, "2배속은 유지");
            Assert.Greater(daily.GetStatMul(EMonsterStatKind.Power), 1f, "오늘의 유물 녹슨 칼날 적용");
        }

        [Test]
        public void 데일리_전투_사양은_SelectedStage를_읽지_않고_규칙_2개와_기준_스테이지를_쓴다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            DailyChallenge c = ShippedDaily().Generate(20753);
            MetaProfile p = new MetaProfile { SelectedStage = 1, SelectedDifficulty = 0 };

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Daily, p, chapters, null, c);

            Assert.IsTrue(spec.IsDaily);
            Assert.AreEqual(StageProgress.ToLinear(c.HeroChapterId, 2), spec.LinearStage);
            Assert.AreEqual(1, spec.EffectiveDifficulty, "DailyConfig 난이도 Normal 고정");
            Assert.AreEqual(2, spec.ResolveRules(chapters).Count);
            Assert.IsFalse(spec.ResolveRules(chapters)[0].HasTrigger);
            Assert.IsTrue(spec.ResolveRules(chapters)[1].HasTrigger);
            Assert.AreEqual(1, p.SelectedStage);
            Assert.IsNull(BattleSpecResolver.Resolve(EBattleMode.Daily, p, chapters, null, null), "컨텍스트 없으면 진입 중단");
        }
    }
}
