using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 무한의 탑 슬라이스 A 보강 - TowerFoundationTests 가 다루지 않은 골든 배율·SplitMix64, 탑/챕터 기록 격리, 클리어 기록 엣지, 사다리·라운딩.
    public class TowerGapTests : RelicStaticStateTestBase
    {
        private static string Dir => WorldMapTestSupport.JsonDir;

        private static TowerConfigRow Config() => JsonUtility.FromJson<TowerConfigRow>(File.ReadAllText(Dir + "TowerConfig.json"));

        private static TowerDatabase Build()
        {
            return TowerDatabase.Build(
                Config(),
                JsonArrayUtility.FromJsonArray<HeroArchetype_TowerRow>(File.ReadAllText(Dir + "HeroArchetype_Tower.json")),
                JsonArrayUtility.FromJsonArray<Stage_TowerRow>(File.ReadAllText(Dir + "Stage_Tower.json")),
                WorldMapTestSupport.FullDb());
        }

        //# ---------- 골든: 난수 / 층 배율 / 층 메타 ----------

        [Test]
        public void SplitMix64는_시드_0에서_표준_수열을_낸다()
        {
            TowerRandom rng = new TowerRandom(0UL);

            Assert.AreEqual(0xE220A8397B1DCDAFUL, rng.Next());
            Assert.AreEqual(0x6E789E6AA1B965F4UL, rng.Next());
            Assert.AreEqual(0x06C45D188009454FUL, rng.Next());
        }

        [Test]
        public void 골든_1에서_10층의_영웅_챕터_위치_보스_구간_유물단계가_고정된다()
        {
            TowerDatabase db = Build();
            Dictionary<string, int> chapterOf = new Dictionary<string, int>
            {
                { "Knight", 1 }, { "Archer", 2 }, { "Assassin", 3 }, { "Berserker", 4 }, { "Paladin", 5 },
            };

            for (int floor = 1; floor <= 10; ++floor)
            {
                TowerFloor f = db.GetFloor(floor, 2);
                Assert.AreEqual(floor, f.Floor);
                Assert.AreEqual(floor <= 5 ? 1 : 2, f.Band, $"{floor}층 구간");
                Assert.AreEqual((floor - 1) % 5 + 1, f.Position, $"{floor}층 위치");
                Assert.AreEqual(floor % 5 == 0, f.IsBoss, $"{floor}층 보스");
                Assert.AreEqual(chapterOf[f.HeroId], f.HeroChapterId, $"{floor}층 영웅 챕터");
                Assert.AreEqual(0, f.RelicTier, $"{floor}층은 Easy 등급");
                Assert.GreaterOrEqual(f.RuleFactor, 1f, $"{floor}층 ruleFactor");
            }
        }

        [Test]
        public void 골든_1에서_10층의_층_배율은_복리_공식과_일치한다()
        {
            TowerDatabase db = Build();

            for (int floor = 1; floor <= 10; ++floor)
            {
                bool boss = floor % 5 == 0;
                double hp = 2.0 * Math.Pow(1.035, floor - 1) * (boss ? 1.1 : 1.0);
                double power = 1.0 * Math.Pow(1.015, floor - 1) * (boss ? 1.05 : 1.0);
                TowerFloor f = db.GetFloor(floor, 2);
                Assert.AreEqual(hp, (double)f.FloorHp, 1e-6, $"{floor}층 HP 배율");
                Assert.AreEqual(power, (double)f.FloorPower, 1e-6, $"{floor}층 공격력 배율");
            }
            Assert.AreEqual(2.0m, db.GetFloor(1, 2).FloorHp, "1층은 정확히 hpBase");
            Assert.AreEqual(2.07m, db.GetFloor(2, 2).FloorHp, "decimal 누적이라 오차 없이 정확");
        }

        [Test]
        public void 층_배율이_단조_증가하고_구간_5층_보스가_인접_일반층보다_크다()
        {
            TowerDatabase db = Build();

            for (int floor = 2; floor <= 60; ++floor)
            {
                if (db.GetFloor(floor, 2).IsBoss || db.GetFloor(floor - 1, 2).IsBoss)
                    continue;
                Assert.Greater(db.GetFloor(floor, 2).FloorHp, db.GetFloor(floor - 1, 2).FloorHp, $"{floor}층");
            }
            Assert.Greater(db.GetFloor(5, 2).FloorHp, db.GetFloor(4, 2).FloorHp);
            Assert.Greater(db.GetFloor(10, 2).FloorPower, db.GetFloor(9, 2).FloorPower);
        }

        [Test]
        public void 같은_층을_다른_순서로_조회해도_결과가_같다_메모이즈_순서_무관()
        {
            TowerDatabase forward = Build();
            TowerDatabase backward = Build();
            string[] expected = Enumerable.Range(1, 30).Select(n => Describe(forward.GetFloor(n, 2))).ToArray();

            for (int n = 30; n >= 1; --n)
            {
                Assert.AreEqual(expected[n - 1], Describe(backward.GetFloor(n, 2)), $"{n}층");
            }
        }

        private static string Describe(TowerFloor f)
        {
            return $"{f.Floor}|{f.HeroId}|{f.TemplateChapterId}-{f.TemplateStageIndex}|{f.FloorHp}|{f.FloorPower}|{f.RelicTier}";
        }

        //# ---------- MetaProfile.RecordTowerClear 엣지 ----------

        [Test]
        public void 음수_층_음수_시간_NaN_시간은_무시한다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordTowerClear(-3, 50f, 1);
            p.RecordTowerClear(2, -1f, 1);
            p.RecordTowerClear(2, float.NaN, 1);

            Assert.AreEqual(0, p.TowerFloorClears.Count, "NaN 이 기록되면 안 된다");
        }

        [Test]
        public void 같은_시간이면_유지하고_최고층은_층_구멍이_있어도_최대값이다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordTowerClear(5, 100f, 1);
            p.RecordTowerClear(5, 100f, 1);
            p.RecordTowerClear(2, 80f, 1);

            Assert.AreEqual(2, p.TowerFloorClears.Count);
            Assert.AreEqual(5, p.GetTowerHighestFloor(), "층 구멍(3,4 없음)은 보정하지 않는다");
            Assert.AreEqual(100f, p.GetTowerFloorClearTime(5));
            Assert.AreEqual(0f, p.GetTowerFloorClearTime(3));
        }

        [Test]
        public void 시즌_정리_후_새_시즌_기록은_이전_시즌_행을_섞지_않는다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(7, 100f, 1);

            Assert.IsTrue(p.NormalizeTowerSeason(2));
            p.RecordTowerClear(1, 60f, 2);

            Assert.AreEqual(2, p.TowerSeason);
            Assert.AreEqual(1, p.GetTowerHighestFloor());
            Assert.AreEqual(1, p.TowerFloorClears.Count);
        }

        [Test]
        public void 클리어_리스트가_null이어도_기록과_조회가_안전하다()
        {
            MetaProfile p = new MetaProfile { TowerFloorClears = null };

            Assert.AreEqual(0, p.GetTowerHighestFloor());
            Assert.AreEqual(0f, p.GetTowerFloorClearTime(1));
            p.RecordTowerClear(1, 10f, 1);
            Assert.AreEqual(1, p.TowerFloorClears.Count);
        }

        //# ---------- 전투 사양: 유물 키 / 챕터 격리 ----------

        [Test]
        public void 탑_사양의_유물_키는_영웅_챕터_구간_위치_층_등급이다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            TowerDatabase tower = Build();
            MetaProfile p = new MetaProfile();
            for (int floor = 1; floor <= 4; ++floor)
            {
                p.RecordTowerClear(floor, 100f, 1);
            }

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, tower);

            Assert.AreEqual(5, spec.Floor.Floor);
            Assert.IsTrue(spec.Floor.IsBoss);
            Assert.AreEqual(spec.Floor.HeroChapterId, spec.RelicChapterId);
            Assert.AreEqual(5, spec.RelicStageIndex, "유물 stageIndex 는 구간 내 위치");
            Assert.AreEqual(spec.Floor.RelicTier, spec.RelicDifficulty);
            Assert.AreEqual(0, spec.EffectiveDifficulty);
            Assert.AreEqual(5, spec.StageIndex, "LinearStage 는 영웅 챕터의 5단계");
            Assert.IsNotNull(spec.TowerHero);
            Assert.IsNull(spec.Stage, "탑은 챕터 Stage 행을 쓰지 않는다");
        }

        [Test]
        public void 탑_사양은_진행이_없는_프로필의_다른_필드를_바꾸지_않는다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            MetaProfile p = new MetaProfile { SelectedStage = 3, ClearedStage = 10, SelectedDifficulty = 1, BestClearTime = 200f };
            string before = JsonUtility.ToJson(p);

            BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, Build());

            Assert.AreEqual(before, JsonUtility.ToJson(p));
        }

        //# ---------- EndBattle 탑 분기 (private RecordTowerResult/RecordChapterResult 를 리플렉션으로 직접 호출) ----------

        private static BattleController MakeController(BattleSpec spec, float elapsed)
        {
            BattleController controller = (BattleController)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(BattleController));
            BattleClock clock = new BattleClock(300f);
            clock.Start();
            clock.Tick(elapsed);
            SetField(controller, "_spec", spec);
            SetField(controller, "_clock", clock);
            SetField(controller, "_effectiveDifficulty", spec.EffectiveDifficulty);
            return controller;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = typeof(BattleController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, name);
            field.SetValue(target, value);
        }

        private static void InvokeTowerRecord(BattleController controller, MetaProfile profile, BattleResult result)
        {
            MethodInfo method = typeof(BattleController).GetMethod("RecordTowerResult", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(controller, new object[] { profile, result });
        }

        private static MetaProfile ChapterProgressedProfile()
        {
            MetaProfile p = new MetaProfile { SelectedStage = 3, ClearedStage = 10, SelectedDifficulty = 1, BestClearTime = 200f, TotalWins = 4, TotalRuns = 9 };
            p.RecordStageRun(3, true, 150f, true);
            p.RecordDifficultyClear(3, 1);
            p.AddRelic("HeartOfStone");
            p.RelicMissStreak = 2;
            return p;
        }

        [Test]
        public void 탑_패배는_프로필을_전혀_바꾸지_않는다()
        {
            TowerDatabase prev = TowerDatabase.Current;
            try
            {
                TowerDatabase tower = Build();
                TowerDatabase.Current = tower;
                MetaProfile p = ChapterProgressedProfile();
                BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Tower, p, WorldMapTestSupport.ShippedDb(), tower);
                string before = JsonUtility.ToJson(p);

                InvokeTowerRecord(MakeController(spec, 10f), p, BattleResult.Lose);

                Assert.AreEqual(before, JsonUtility.ToJson(p));
            }
            finally
            {
                TowerDatabase.Current = prev;
            }
        }

        [Test]
        public void 탑_승리는_TotalWins와_탑_층_기록만_바꾸고_챕터_기록은_그대로다()
        {
            TowerDatabase prev = TowerDatabase.Current;
            try
            {
                TowerDatabase tower = Build();
                TowerDatabase.Current = tower;
                MetaProfile p = ChapterProgressedProfile();
                BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Tower, p, WorldMapTestSupport.ShippedDb(), tower);
                string before = JsonUtility.ToJson(p);

                InvokeTowerRecord(MakeController(spec, 42.5f), p, BattleResult.Win);

                MetaProfile expected = JsonUtility.FromJson<MetaProfile>(before);
                expected.TotalWins++;
                expected.RecordTowerClear(1, 42.5f, tower.Config.seasonId);
                Assert.AreEqual(JsonUtility.ToJson(expected), JsonUtility.ToJson(p));
                Assert.AreEqual(200f, p.BestClearTime, "챕터 최단 기록 불변");
                Assert.AreEqual(10, p.ClearedStage);
                Assert.AreEqual(1, p.StageRecords.Count);
                Assert.AreEqual(1, p.DifficultyClears.Count);
                Assert.AreEqual(3, p.SelectedStage);
                Assert.AreEqual(1, p.SelectedDifficulty);
                Assert.AreEqual(42.5f, p.GetTowerFloorClearTime(1));
            }
            finally
            {
                TowerDatabase.Current = prev;
            }
        }

        [Test]
        public void 탑_연속_승리는_층을_올리고_같은_층_재승리는_더_빠를_때만_갱신한다()
        {
            TowerDatabase prev = TowerDatabase.Current;
            try
            {
                TowerDatabase tower = Build();
                TowerDatabase.Current = tower;
                MetaProfile p = new MetaProfile();
                ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();

                BattleSpec first = BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, tower);
                InvokeTowerRecord(MakeController(first, 80f), p, BattleResult.Win);
                BattleSpec second = BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, tower);
                InvokeTowerRecord(MakeController(second, 90f), p, BattleResult.Win);
                InvokeTowerRecord(MakeController(first, 100f), p, BattleResult.Win);
                InvokeTowerRecord(MakeController(first, 70f), p, BattleResult.Win);

                Assert.AreEqual(1, first.Floor.Floor);
                Assert.AreEqual(2, second.Floor.Floor);
                Assert.AreEqual(2, p.GetTowerHighestFloor());
                Assert.AreEqual(70f, p.GetTowerFloorClearTime(1));
                Assert.AreEqual(90f, p.GetTowerFloorClearTime(2));
                Assert.AreEqual(4, p.TotalWins, "승리마다 TotalWins 증가");
            }
            finally
            {
                TowerDatabase.Current = prev;
            }
        }

        [Test]
        public void 대조군_챕터_패배는_스테이지_전적을_기록해_리플렉션_하네스가_실제_코드를_탄다()
        {
            MetaProfile p = ChapterProgressedProfile();
            BattleSpec spec = BattleSpecResolver.ResolveChapter(p, WorldMapTestSupport.ShippedDb());
            string before = JsonUtility.ToJson(p);
            MethodInfo method = typeof(BattleController).GetMethod("RecordChapterResult", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            object[] args = { p, BattleResult.Lose, false, 0 };

            method.Invoke(MakeController(spec, 20f), args);

            Assert.AreNotEqual(before, JsonUtility.ToJson(p), "챕터 판은 StageRecords 를 갱신한다");
            Assert.AreEqual(0, p.TowerFloorClears.Count, "챕터 판은 탑 기록을 건드리지 않는다");
        }

        //# ---------- TowerViewModel: 라운딩 / 사다리 상태 ----------

        private static RelicDatabase RelicDbWithChance(float stage1Chance)
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.StageChances.First(r => r.chapterId == 1 && r.stageIndex == 1).dropChance = stage1Chance;
            return set.Build();
        }

        private static string RewardPercentText(float chance, bool ownsRelic = true)
        {
            MetaProfile p = new MetaProfile();
            if (ownsRelic)
            {
                p.AddRelic("HeartOfStone");
            }
            TowerViewModel vm = new TowerViewModel(Build(), WorldMapTestSupport.FullDb(), p, WorldMapTestSupport.Text, RelicDbWithChance(chance), 4000, 50);
            TowerFloorDetail detail = vm.BuildDetail();
            Assert.IsTrue(detail.ShowReward);
            return detail.RewardText;
        }

        [Test]
        public void 유물_확률_표시는_은행가_반올림이라_12점5는_12_37점5는_38_62점5는_62_87점5는_88이다()
        {
            StringAssert.Contains("12%", RewardPercentText(0.125f));
            StringAssert.Contains("38%", RewardPercentText(0.375f));
            StringAssert.Contains("62%", RewardPercentText(0.625f));
            StringAssert.Contains("88%", RewardPercentText(0.875f));
        }

        [Test]
        public void 유물_확률_표시는_정수에_가까운_값을_그대로_표시하고_미보유_프로필은_100이다()
        {
            StringAssert.Contains("30%", RewardPercentText(0.3f));
            StringAssert.Contains("100%", RewardPercentText(0.3f, ownsRelic: false), "유물이 하나도 없으면 첫 드랍 보장");
        }

        [Test]
        public void 사다리_층_구멍이_있으면_클리어_층만_내림차순으로_나오고_선택_가능_범위도_그에_따른다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(1, 100f, 1);
            p.RecordTowerClear(3, 100f, 1);
            TowerViewModel vm = new TowerViewModel(Build(), WorldMapTestSupport.FullDb(), p, WorldMapTestSupport.Text, null, 4000, 50);

            List<TowerFloorCellData> cells = vm.BuildLadder();

            CollectionAssert.AreEqual(new[] { 8, 7, 6, 5, 4, 3, 1 }, cells.Select(c => c.Floor).ToArray());
            Assert.AreEqual(4, vm.ChallengeFloor, "도전 층 = 최고 층 + 1, 구멍 보정 없음");
            int changed = 0;
            vm.OnChanged += () => changed++;
            vm.SelectFloor(2);
            Assert.AreEqual(0, changed, "클리어도 도전도 아닌 구멍 층은 선택 불가");
            Assert.AreEqual(4, vm.SelectedFloor);
            vm.SelectFloor(9);
            Assert.AreEqual(0, changed, "미리보기 범위(4개) 밖은 선택 불가");
            vm.SelectFloor(8);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void 선택_표식은_ApplySelectionMarks로_기존_셀만_다시_칠한다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(1, 100f, 1);
            TowerViewModel vm = new TowerViewModel(Build(), WorldMapTestSupport.FullDb(), p, WorldMapTestSupport.Text, null, 4000, 50);
            List<TowerFloorCellData> cells = vm.BuildLadder();
            Assert.IsTrue(cells.Single(c => c.Floor == 2).IsSelected, "초기 선택 = 도전 층");

            vm.SelectFloor(1);
            vm.ApplySelectionMarks(cells);

            Assert.IsTrue(cells.Single(c => c.Floor == 1).IsSelected);
            Assert.AreEqual(1, cells.Count(c => c.IsSelected), "선택은 항상 하나");
        }

        [Test]
        public void 앵커_스탯이_0이면_능력치_줄을_숨기고_탑_잠금은_5_5_클리어_경계다()
        {
            TowerDatabase tower = Build();
            TowerViewModel vm = new TowerViewModel(tower, WorldMapTestSupport.FullDb(), new MetaProfile(), WorldMapTestSupport.Text, null, 0, 0);

            Assert.IsFalse(vm.BuildDetail().ShowStats);
            Assert.IsFalse(tower.IsUnlocked(24));
            Assert.IsTrue(tower.IsUnlocked(25));
            Assert.IsTrue(tower.IsUnlocked(26));
        }

        //# ---------- 문구 ----------

        [Test]
        public void 탑_문구_상수_전부가_존재하고_탑_전용_24개가_빠짐없다()
        {
            int[] ids = typeof(TowerStrings).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(int))
                .Select(f => (int)f.GetRawConstantValue())
                .ToArray();

            Assert.AreEqual(24, ids.Count(id => id >= 1413 && id <= 1436), "탑 전용 id 1413~1436 이 빠짐없이 상수로 존재");
            foreach (int id in ids)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"문구 {id}");
            }
            Assert.AreEqual(ids.Length, ids.Distinct().Count(), "문구 id 중복 없음");
        }
    }
}
