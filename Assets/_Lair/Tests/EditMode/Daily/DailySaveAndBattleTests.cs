using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 데일리 본격 스위트 2 — EndBattle 데일리 분기 격리 · 확정 유물 · 세이브(마이그레이션/복사/병합/정리) · DailyViewModel 날짜 경계 (daily-challenge §11).
    public class DailySaveAndBattleTests : RelicStaticStateTestBase
    {
        private const long KstMidnight20753 = 1793026800000L;
        private const int Day = 20753;

        private DailyDatabase _prevDaily;
        private DailyClock _prevClock;
        private DailyChallenge _prevContext;

        [SetUp]
        public void 데일리_세션_정적상태를_저장한다()
        {
            _prevDaily = DailyDatabase.Current;
            _prevClock = MetaSession.DailyClock;
            _prevContext = MetaSession.DailyContext;
        }

        [TearDown]
        public void 데일리_세션_정적상태를_원복한다()
        {
            DailyDatabase.Current = _prevDaily;
            MetaSession.DailyClock = _prevClock;
            MetaSession.DailyContext = _prevContext;
        }

        private static DailyDatabase Daily()
        {
            DailyConfigRow config = JsonUtility.FromJson<DailyConfigRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "DailyConfig.json"));
            return DailyDatabase.Build(config, WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), 2);
        }

        //# ---------- EndBattle 데일리 분기 (private RecordDailyResult 를 리플렉션으로 직접 호출 — TowerGapTests 하네스) ----------

        private static BattleController MakeController(BattleSpec spec, float elapsed, Health heroHealth = null)
        {
            BattleController controller = (BattleController)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(BattleController));
            BattleClock clock = new BattleClock(300f);
            clock.Start();
            clock.Tick(elapsed);
            SetField(controller, "_spec", spec);
            SetField(controller, "_clock", clock);
            SetField(controller, "_effectiveDifficulty", spec.EffectiveDifficulty);
            SetField(controller, "_heroHealth", heroHealth);
            return controller;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = typeof(BattleController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, name);
            field.SetValue(target, value);
        }

        //# 반환 = private struct DailyEndResult(boxed). 필드는 리플렉션으로 읽는다.
        private static object InvokeDailyRecord(BattleController controller, MetaProfile profile, BattleResult result)
        {
            MethodInfo method = typeof(BattleController).GetMethod("RecordDailyResult", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return method.Invoke(controller, new object[] { profile, result });
        }

        private static T Read<T>(object boxed, string name)
        {
            FieldInfo field = boxed.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, name);
            return (T)field.GetValue(boxed);
        }

        //# 챕터·탑·난이도·유물 천장 진행이 모두 있는 프로필 + 어제 데일리 참가(연속 3일).
        private static MetaProfile ProgressedProfile()
        {
            MetaProfile p = new MetaProfile { SelectedStage = 3, ClearedStage = 10, SelectedDifficulty = 1, BestClearTime = 200f, TotalWins = 4, TotalRuns = 9, Souls = 1000 };
            p.RecordStageRun(3, true, 150f, true);
            p.RecordDifficultyClear(3, 1);
            p.AddRelic("HeartOfStone");
            p.RelicMissStreak = 2;
            p.RecordTowerClear(1, 90f, 1);
            p.RecordTowerClear(2, 95f, 1);
            p.DailyStreak = 3;
            p.DailyResults.Add(new DailyResultEntry { Day = Day - 1, BestScore = 300400 });
            p.UseDailyAttempt(Day);
            return p;
        }

        private static BattleSpec DailySpec()
        {
            DailyChallenge c = Daily().Generate(Day);
            BattleSpec spec = BattleSpecResolver.ResolveDaily(c, WorldMapTestSupport.ShippedDb());
            Assert.IsNotNull(spec);
            return spec;
        }

        private static void AssertChapterAndTowerUntouched(MetaProfile p, string beforeJson)
        {
            MetaProfile before = JsonUtility.FromJson<MetaProfile>(beforeJson);
            Assert.AreEqual(before.ClearedStage, p.ClearedStage, "ClearedStage");
            Assert.AreEqual(before.StageRecords.Count, p.StageRecords.Count, "StageRecords");
            Assert.AreEqual(before.StageRecords[0].Runs, p.StageRecords[0].Runs, "StageRecords 전적");
            Assert.AreEqual(before.BestClearTime, p.BestClearTime, "BestClearTime");
            Assert.AreEqual(before.DifficultyClears.Count, p.DifficultyClears.Count, "DifficultyClears");
            Assert.AreEqual(before.SelectedStage, p.SelectedStage, "SelectedStage");
            Assert.AreEqual(before.SelectedDifficulty, p.SelectedDifficulty, "SelectedDifficulty");
            Assert.AreEqual(before.TowerFloorClears.Count, p.TowerFloorClears.Count, "탑 기록");
            Assert.AreEqual(before.GetTowerHighestFloor(), p.GetTowerHighestFloor(), "탑 최고층");
            Assert.AreEqual(before.RelicMissStreak, p.RelicMissStreak, "RelicMissStreak");
        }

        [Test]
        public void 데일리_첫_승리는_TotalWins_데일리_기록_보너스_소울만_바꾼다()
        {
            DailyDatabase.Current = Daily();
            MetaProfile p = ProgressedProfile();
            BattleSpec spec = DailySpec();
            string before = JsonUtility.ToJson(p);

            object end = InvokeDailyRecord(MakeController(spec, 42.5f), p, BattleResult.Win);

            MetaProfile expected = JsonUtility.FromJson<MetaProfile>(before);
            expected.TotalWins++;
            expected.RecordDailyResult(Day, 42500);
            expected.Souls += 195;
            Assert.AreEqual(JsonUtility.ToJson(expected), JsonUtility.ToJson(p), "그 외 필드 전부 불변");
            AssertChapterAndTowerUntouched(p, before);
            DailyRecordResult record = Read<DailyRecordResult>(end, "Record");
            Assert.IsTrue(record.IsFirstWin);
            Assert.AreEqual(4, record.Streak);
            Assert.AreEqual(42500, record.BestScore);
            Assert.AreEqual(195, Read<int>(end, "BonusSouls"), "150 x 1.3");
            Assert.AreEqual(1.3m, Read<decimal>(end, "Multiplier"));
            Assert.IsTrue(Read<bool>(end, "Recorded"));
            Assert.AreEqual(1, p.GetDailyAttemptsUsed(Day), "종료 정산은 도전을 다시 차감하지 않는다(시작 시 1회)");
        }

        [Test]
        public void 데일리_패배는_데일리_기록만_바꾸고_TotalWins_소울은_그대로다()
        {
            DailyDatabase.Current = Daily();
            MetaProfile p = ProgressedProfile();
            BattleSpec spec = DailySpec();
            string before = JsonUtility.ToJson(p);
            GameObject go = new GameObject("test-hero");
            try
            {
                Health health = go.AddComponent<Health>();
                health.SetMax(10000);
                health.TakeDamage(8800);

                object end = InvokeDailyRecord(MakeController(spec, 120f, health), p, BattleResult.Lose);

                MetaProfile expected = JsonUtility.FromJson<MetaProfile>(before);
                expected.RecordDailyResult(Day, 300120);
                Assert.AreEqual(JsonUtility.ToJson(expected), JsonUtility.ToJson(p));
                AssertChapterAndTowerUntouched(p, before);
                Assert.AreEqual(4, p.TotalWins);
                Assert.AreEqual(1000, p.Souls);
                Assert.AreEqual(300120, Read<DailyRecordResult>(end, "Record").BestScore, "남은 HP 12% → 300120");
                Assert.IsFalse(Read<DailyRecordResult>(end, "Record").IsFirstWin);
                Assert.AreEqual(0, Read<int>(end, "BonusSouls"));
                Assert.AreEqual(4, p.DailyStreak, "패배도 참가 — 연속 갱신");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void 영웅_체력_참조가_없는_패배는_무피해_패배_301000으로_기록된다()
        {
            DailyDatabase.Current = Daily();
            MetaProfile p = ProgressedProfile();

            object end = InvokeDailyRecord(MakeController(DailySpec(), 10f), p, BattleResult.Lose);

            Assert.AreEqual(DailyRules.ScoreMax, Read<DailyRecordResult>(end, "Record").BestScore);
            Assert.IsTrue(Read<bool>(end, "Recorded"));
        }

        [Test]
        public void 같은_날_두번째_승리는_보너스가_없고_더_빠르면_최고_점수만_갱신한다()
        {
            DailyDatabase.Current = Daily();
            MetaProfile p = ProgressedProfile();
            BattleSpec spec = DailySpec();
            InvokeDailyRecord(MakeController(spec, 100f), p, BattleResult.Win);
            int soulsAfterFirst = p.Souls;

            object second = InvokeDailyRecord(MakeController(spec, 80f), p, BattleResult.Win);

            Assert.AreEqual(soulsAfterFirst, p.Souls);
            Assert.AreEqual(0, Read<int>(second, "BonusSouls"));
            Assert.AreEqual(1m, Read<decimal>(second, "Multiplier"));
            Assert.IsFalse(Read<DailyRecordResult>(second, "Record").IsFirstWin);
            Assert.AreEqual(80000, Read<DailyRecordResult>(second, "Record").BestScore);
            Assert.AreEqual(6, p.TotalWins);
            Assert.AreEqual(4, p.DailyStreak);
        }

        [Test]
        public void 패배_후_첫_승리도_첫_승리_보너스를_받는다()
        {
            DailyDatabase.Current = Daily();
            MetaProfile p = ProgressedProfile();
            BattleSpec spec = DailySpec();
            InvokeDailyRecord(MakeController(spec, 10f), p, BattleResult.Lose);

            object win = InvokeDailyRecord(MakeController(spec, 150f), p, BattleResult.Win);

            Assert.IsTrue(Read<DailyRecordResult>(win, "Record").IsFirstWin);
            Assert.AreEqual(195, Read<int>(win, "BonusSouls"), "연속은 첫 종료(패배) 때 4 로 이미 갱신");
            Assert.AreEqual(1195, p.Souls);
        }

        [Test]
        public void 대조군_챕터_승리는_ClearedStage와_스테이지_전적을_실제로_바꾸고_데일리_기록은_건드리지_않는다()
        {
            MetaProfile p = new MetaProfile { SelectedStage = 1, ClearedStage = 0 };
            BattleSpec spec = BattleSpecResolver.ResolveChapter(p, WorldMapTestSupport.ShippedDb());
            MethodInfo method = typeof(BattleController).GetMethod("RecordChapterResult", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            object[] args = { p, BattleResult.Win, false, 0 };
            RankingClient prevRanking = MetaSession.Ranking;
            try
            {
                //# 챕터 승리는 랭킹 제출을 탄다 — 네트워크 없이 조기 반환하도록 비운다.
                MetaSession.Ranking = null;
                method.Invoke(MakeController(spec, 60f), args);
            }
            finally
            {
                MetaSession.Ranking = prevRanking;
            }

            Assert.AreEqual(1, p.ClearedStage, "챕터 판은 진행을 올린다 — 하네스가 실제 코드를 탄다");
            Assert.AreEqual(1, p.StageRecords.Count);
            Assert.AreEqual(60f, p.BestClearTime);
            Assert.AreEqual(0, p.DailyResults.Count);
            Assert.AreEqual(0, p.DailyDays.Count);
            Assert.AreEqual(0, p.DailyStreak);
        }

        [Test]
        public void 데일리_사양_해석은_프로필을_전혀_바꾸지_않는다()
        {
            MetaProfile p = ProgressedProfile();
            string before = JsonUtility.ToJson(p);

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Daily, p, WorldMapTestSupport.ShippedDb(), null, Daily().Generate(Day));

            Assert.AreEqual(before, JsonUtility.ToJson(p));
            Assert.AreEqual(1, spec.RelicDifficulty, "확정 굴림 등급 가중 = DailyConfig 난이도");
            Assert.IsNull(BattleSpecResolver.ResolveDaily(Daily().Generate(Day), null), "챕터 데이터 없으면 진입 중단");
        }

        //# ---------- 첫 승리 확정 유물 ----------

        [Test]
        public void 확정_굴림은_RelicMissStreak을_읽지도_쓰지도_않고_반드시_획득한다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            DailyChallenge c = Daily().Generate(Day);
            foreach (int streak in new[] { 0, 2, 50 })
            {
                foreach (float roll in new[] { 0f, 0.5f, 0.99999994f })
                {
                    MetaProfile p = new MetaProfile { RelicMissStreak = streak };

                    RelicDropResult r = RelicDropRoller.RollGuaranteed(db, p, c.HeroId, 1, () => roll);

                    Assert.AreEqual(ERelicDropOutcome.Acquired, r.Outcome, $"streak {streak} roll {roll}");
                    Assert.AreEqual(streak, p.RelicMissStreak);
                    Assert.AreEqual(1, p.GetRelicStack(r.RelicId));
                    Assert.IsTrue(r.IsNew);
                }
            }
        }

        [Test]
        public void 영웅_드랍표가_전부_최대중첩이면_확정_굴림은_미획득이고_프로필_불변이다()
        {
            RelicDatabase db = RelicDataSet.Shipped().Build();
            string hero = Daily().Generate(Day).HeroId;
            MetaProfile p = new MetaProfile { RelicMissStreak = 3 };
            foreach (Relic_DropRow drop in db.DropsOf(hero))
            {
                p.Relics.Add(new RelicEntry { RelicId = drop.relicId, Stack = db.Get(drop.relicId).MaxStack });
            }
            string before = JsonUtility.ToJson(p);

            RelicDropResult r = RelicDropRoller.RollGuaranteed(db, p, hero, 1, () => 0.5f);

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, r.Outcome);
            Assert.AreEqual(before, JsonUtility.ToJson(p));
            Assert.AreEqual(ERelicDropOutcome.NoCandidates, RelicDropRoller.RollGuaranteed(null, p, hero, 1, () => 0.5f).Outcome);
            Assert.AreEqual(ERelicDropOutcome.NoCandidates, RelicDropRoller.RollGuaranteed(db, p, hero, 1, null).Outcome);
        }

        //# ---------- 마이그레이션 v10 → v11 ----------

        [Test]
        public void v10_세이브는_데일리_키_없이_로드되어_v11_빈_기록이_된다()
        {
            MetaProfile p = JsonUtility.FromJson<MetaProfile>("{\"Version\":10,\"Souls\":77,\"ClearedStage\":3,\"SelectedStage\":3,\"SelectedHero\":\"Knight\"}");

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(14, p.Version);
            Assert.AreEqual(MetaProfileMigration.CurrentVersion, p.Version);
            Assert.IsNotNull(p.DailyDays);
            Assert.IsNotNull(p.DailyResults);
            Assert.AreEqual(0, p.DailyDays.Count);
            Assert.AreEqual(0, p.DailyResults.Count);
            Assert.AreEqual(0, p.DailyStreak);
            Assert.AreEqual(77, p.Souls);
            Assert.AreEqual(3, p.ClearedStage);
        }

        [Test]
        public void 데일리_정리는_무효행_제거_중복_병합_음수연속_0이고_멱등이다()
        {
            MetaProfile p = new MetaProfile { DailyStreak = -4 };
            p.DailyDays = new List<DailyDayEntry>
            {
                null,
                new DailyDayEntry { Day = -1, AttemptsUsed = 2 },
                new DailyDayEntry { Day = 5, AttemptsUsed = 0 },
                new DailyDayEntry { Day = 5, AttemptsUsed = 1 },
                new DailyDayEntry { Day = 5, AttemptsUsed = 3 },
                new DailyDayEntry { Day = 6, AttemptsUsed = 2 },
            };
            p.DailyResults = new List<DailyResultEntry>
            {
                new DailyResultEntry { Day = 5, BestScore = 200000 },
                new DailyResultEntry { Day = 5, BestScore = 150000 },
                new DailyResultEntry { Day = 6, BestScore = 0 },
                new DailyResultEntry { Day = 7, BestScore = 301001 },
                new DailyResultEntry { Day = -2, BestScore = 5 },
                null,
            };

            MetaProfileMigration.NormalizeDaily(p);
            string once = JsonUtility.ToJson(p);
            MetaProfileMigration.NormalizeDaily(p);

            Assert.AreEqual(once, JsonUtility.ToJson(p), "멱등");
            Assert.AreEqual(2, p.DailyDays.Count);
            Assert.AreEqual(3, p.GetDailyAttemptsUsed(5), "같은 Day 는 최댓값");
            Assert.AreEqual(2, p.GetDailyAttemptsUsed(6));
            Assert.AreEqual(1, p.DailyResults.Count);
            Assert.IsTrue(p.TryGetDailyBest(5, out int best));
            Assert.AreEqual(150000, best, "같은 Day 는 최솟값");
            Assert.AreEqual(0, p.DailyStreak);
        }

        [Test]
        public void 데일리_정리는_null_리스트를_빈_리스트로_만든다()
        {
            MetaProfile p = new MetaProfile { DailyDays = null, DailyResults = null, DailyStreak = 2 };

            MetaProfileMigration.NormalizeDaily(p);

            Assert.IsNotNull(p.DailyDays);
            Assert.IsNotNull(p.DailyResults);
            Assert.AreEqual(2, p.DailyStreak);
        }

        [Test]
        public void 데일리_기록은_JSON_왕복에도_보존된다()
        {
            MetaProfile p = new MetaProfile { DailyStreak = 6 };
            p.UseDailyAttempt(Day);
            p.RecordDailyResult(Day, 123456);

            MetaProfile loaded = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));
            MetaProfileMigration.Apply(loaded, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(1, loaded.GetDailyAttemptsUsed(Day));
            Assert.IsTrue(loaded.TryGetDailyBest(Day, out int best));
            Assert.AreEqual(123456, best);
            Assert.AreEqual(1, loaded.DailyStreak, "어제 행이 없어 1로 갱신된 값");
        }

        //# ---------- CopyFrom / 병합 / 정리 ----------

        [Test]
        public void CopyFrom은_데일리_3필드를_깊은_복사한다()
        {
            MetaProfile source = new MetaProfile { DailyStreak = 5 };
            source.UseDailyAttempt(Day);
            source.RecordDailyResult(Day, 200000);
            source.DailyStreak = 5;
            MetaProfile target = new MetaProfile();

            target.CopyFrom(source);
            source.DailyDays[0].AttemptsUsed = 99;
            source.DailyResults[0].BestScore = 1;
            source.DailyDays.Add(new DailyDayEntry { Day = Day + 1, AttemptsUsed = 1 });
            source.DailyStreak = 9;

            Assert.AreNotSame(source.DailyDays, target.DailyDays);
            Assert.AreNotSame(source.DailyResults, target.DailyResults);
            Assert.AreEqual(1, target.DailyDays.Count);
            Assert.AreEqual(1, target.GetDailyAttemptsUsed(Day));
            Assert.IsTrue(target.TryGetDailyBest(Day, out int best));
            Assert.AreEqual(200000, best);
            Assert.AreEqual(5, target.DailyStreak);
        }

        [Test]
        public void CopyFrom은_원본_데일리_리스트가_null이면_빈_리스트로_대체한다()
        {
            MetaProfile source = new MetaProfile { DailyDays = null, DailyResults = null };
            MetaProfile target = new MetaProfile();
            target.UseDailyAttempt(Day);

            target.CopyFrom(source);

            Assert.IsNotNull(target.DailyDays);
            Assert.IsNotNull(target.DailyResults);
            Assert.AreEqual(0, target.DailyDays.Count, "전체 대체 규약 — 병합은 MergeDailyFrom 몫");
        }

        [Test]
        public void 병합은_날짜별_도전_최댓값_점수_최솟값_한쪽행_유지_연속_최댓값이다()
        {
            MetaProfile cloud = new MetaProfile { DailyStreak = 4 };
            cloud.DailyDays.Add(new DailyDayEntry { Day = 10, AttemptsUsed = 1 });
            cloud.DailyDays.Add(new DailyDayEntry { Day = 11, AttemptsUsed = 2 });
            cloud.DailyResults.Add(new DailyResultEntry { Day = 10, BestScore = 200000 });
            List<DailyDayEntry> localDays = new List<DailyDayEntry>
            {
                new DailyDayEntry { Day = 10, AttemptsUsed = 3 },
                new DailyDayEntry { Day = 12, AttemptsUsed = 1 },
                null,
            };
            List<DailyResultEntry> localResults = new List<DailyResultEntry>
            {
                new DailyResultEntry { Day = 10, BestScore = 250000 },
                new DailyResultEntry { Day = 12, BestScore = 300500 },
                null,
            };

            cloud.MergeDailyFrom(localDays, localResults, 2);

            Assert.AreEqual(3, cloud.GetDailyAttemptsUsed(10));
            Assert.AreEqual(2, cloud.GetDailyAttemptsUsed(11), "클라우드에만 있는 행 유지");
            Assert.AreEqual(1, cloud.GetDailyAttemptsUsed(12), "로컬에만 있는 행 유지");
            Assert.IsTrue(cloud.TryGetDailyBest(10, out int best10));
            Assert.AreEqual(200000, best10);
            Assert.IsTrue(cloud.TryGetDailyBest(12, out int best12));
            Assert.AreEqual(300500, best12);
            Assert.AreEqual(4, cloud.DailyStreak);
            localDays[0].AttemptsUsed = 99;
            Assert.AreEqual(3, cloud.GetDailyAttemptsUsed(10), "로컬 스냅샷과 행을 공유하지 않는다");
        }

        [Test]
        public void 병합은_null_입력과_null_리스트_대상에서도_안전하다()
        {
            MetaProfile p = new MetaProfile { DailyDays = null, DailyResults = null, DailyStreak = 1 };

            p.MergeDailyFrom(null, null, 3);

            Assert.IsNotNull(p.DailyDays);
            Assert.IsNotNull(p.DailyResults);
            Assert.AreEqual(3, p.DailyStreak);
        }

        [Test]
        public void 복원_후_병합은_로컬에서_이미_끝낸_승리를_첫_승리로_되살리지_않는다()
        {
            MetaProfile local = new MetaProfile { ClearedStage = 5 };
            local.UseDailyAttempt(Day);
            local.RecordDailyResult(Day, 100000);
            MetaProfile oldCloud = new MetaProfile { ClearedStage = 5 };
            List<DailyDayEntry> days = MetaProfile.CopyDailyDays(local.DailyDays);
            List<DailyResultEntry> results = MetaProfile.CopyDailyResults(local.DailyResults);
            int streak = local.DailyStreak;

            local.CopyFrom(oldCloud);
            local.MergeDailyFrom(days, results, streak);

            Assert.IsTrue(local.HasDailyWin(Day));
            Assert.IsFalse(local.RecordDailyResult(Day, 90000).IsFirstWin, "보너스 이중 수령 불가");
            Assert.AreEqual(2, local.DailyAttemptsLeft(Day, 3));
        }

        [Test]
        public void 정리는_today_빼기_6일은_남기고_7일_전은_지운다()
        {
            MetaProfile p = new MetaProfile();
            for (int day = 90; day <= 100; ++day)
            {
                p.DailyDays.Add(new DailyDayEntry { Day = day, AttemptsUsed = 1 });
                p.DailyResults.Add(new DailyResultEntry { Day = day, BestScore = 300500 });
            }

            p.PruneDaily(100);

            Assert.AreEqual(7, p.DailyDays.Count);
            Assert.AreEqual(7, p.DailyResults.Count);
            Assert.AreEqual(1, p.GetDailyAttemptsUsed(94));
            Assert.AreEqual(0, p.GetDailyAttemptsUsed(93));
            Assert.IsTrue(p.HasDailyResult(99), "어제 행 유지");
        }

        [Test]
        public void 차감과_기록은_자동으로_7일_밖_행을_정리한다()
        {
            MetaProfile p = new MetaProfile();
            p.DailyDays.Add(new DailyDayEntry { Day = 50, AttemptsUsed = 3 });
            p.DailyResults.Add(new DailyResultEntry { Day = 50, BestScore = 1000 });

            p.UseDailyAttempt(100);
            Assert.AreEqual(1, p.DailyDays.Count);
            Assert.AreEqual(0, p.DailyResults.Count, "차감도 결과 행 정리");

            p.DailyDays.Add(new DailyDayEntry { Day = 60, AttemptsUsed = 1 });
            p.RecordDailyResult(100, 300500);
            Assert.AreEqual(1, p.DailyDays.Count);
        }

        //# ---------- DailyViewModel 날짜 경계 · 오프라인 (c779ef0 회귀 고정) ----------

        private static DailyViewModel MakeVm(MetaProfile profile, FakeLairApiClient api, System.Func<double> realtime)
        {
            return new DailyViewModel(Daily(), WorldMapTestSupport.ShippedDb(), RelicDataSet.Shipped().Build(), profile, api,
                null, realtime, "me", "나");
        }

        [Test]
        public void 오프라인이면_시작_불가이고_저장된_연속도_0으로_보인다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 5, DailyStreak = 5 };
            FakeLairApiClient api = new FakeLairApiClient { ServerTimeToReturn = ServerTimeResult.Failed };
            DailyViewModel vm = MakeVm(p, api, () => 0.0);

            vm.LoadAsync().GetAwaiter().GetResult();

            Assert.IsFalse(vm.IsOnline);
            Assert.IsNull(vm.Challenge);
            Assert.IsFalse(vm.StartEnabled);
            Assert.AreEqual("연속 0일", vm.StreakText);
            Assert.AreEqual(EDailyStartCheck.Offline, vm.TryStart());
            Assert.IsFalse(vm.RefreshIfDayChanged());
            Assert.AreEqual(EDailyRankState.Failed, vm.RankState);
            Assert.AreEqual(1, api.ServerTimeCalls);
        }

        [Test]
        public void 팝업을_연_채_0시가_지나면_서버_재호출_없이_새_조합으로_갱신된다()
        {
            double t = 0.0;
            MetaProfile p = new MetaProfile { ClearedStage = 5 };
            FakeLairApiClient api = new FakeLairApiClient { ServerTimeToReturn = new ServerTimeResult(true, KstMidnight20753 - 2000L) };
            DailyViewModel vm = MakeVm(p, api, () => t);
            int changed = 0;
            vm.OnChanged += () => changed++;

            vm.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(Day - 1, vm.Challenge.DayIndex);
            Assert.IsFalse(vm.RefreshIfDayChanged());
            int changedBefore = changed;
            t = 3.0;

            Assert.IsTrue(vm.RefreshIfDayChanged());
            Assert.AreEqual(Day, vm.Challenge.DayIndex);
            Assert.AreEqual(Daily().Generate(Day).ComboKey, vm.Challenge.ComboKey);
            Assert.Greater(changed, changedBefore);
            Assert.IsFalse(vm.RefreshIfDayChanged(), "같은 날 재호출은 갱신 없음");
            Assert.AreEqual(1, api.ServerTimeCalls, "단조 시계만 사용");
        }

        [Test]
        public void 시작_순간_날짜가_바뀌었으면_DayChanged로_거부하고_새_조합을_받는다()
        {
            double t = 0.0;
            MetaProfile p = new MetaProfile { ClearedStage = 5 };
            FakeLairApiClient api = new FakeLairApiClient { ServerTimeToReturn = new ServerTimeResult(true, KstMidnight20753 + 1000L) };
            DailyViewModel vm = MakeVm(p, api, () => t);
            vm.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(EDailyStartCheck.Ok, vm.TryStart());

            t = 86400.0;

            Assert.AreEqual(EDailyStartCheck.DayChanged, vm.TryStart());
            Assert.AreEqual(Day + 1, vm.Challenge.DayIndex);
            Assert.AreEqual(EDailyStartCheck.Ok, vm.TryStart(), "갱신된 날에는 바로 시작 가능");
            Assert.AreEqual(0, p.DailyDays.Count, "판정은 차감하지 않는다");
        }

        [Test]
        public void 소진되면_시작_비활성이고_첫_승리_미리보기는_실제_지급값과_같다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 5, DailyStreak = 3 };
            p.DailyResults.Add(new DailyResultEntry { Day = Day - 1, BestScore = 300500 });
            FakeLairApiClient api = new FakeLairApiClient { ServerTimeToReturn = new ServerTimeResult(true, KstMidnight20753 + 1000L) };
            DailyViewModel vm = MakeVm(p, api, () => 0.0);
            vm.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual("첫 승리 보상: 소울 +195, 유물 1개 확정", vm.FirstWinText);
            Assert.AreEqual("연속 3일", vm.StreakText);
            for (int i = 0; i < 3; ++i)
            {
                p.UseDailyAttempt(Day);
            }

            Assert.IsFalse(vm.StartEnabled);
            Assert.AreEqual(0, vm.AttemptsLeft);
            Assert.AreEqual(EDailyStartCheck.Exhausted, vm.TryStart());
            CollectionAssert.AreEqual(new[] { false, false, false }, vm.AttemptPips);
            CollectionAssert.AreEqual(new[] { false, false, false, false, false, true, false }, vm.Attendance, "어제만 참가");
        }
    }
}
