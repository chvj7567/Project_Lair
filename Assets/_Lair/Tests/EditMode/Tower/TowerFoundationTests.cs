using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 무한의 탑 슬라이스 A 기반 검증 (infinite-tower §3~§8) - 데이터 로드/검증, 결정적 층 생성(골든), 능력치, 세이브, 전투 사양, 사다리 VM, 결과창 변형.
    //# 골든 1~10층은 SplitMix64 + Fisher-Yates 를 파이썬으로 독립 재구현해 얻은 값이다(출하 시드 20261027).
    public class TowerFoundationTests : ChapterStaticStateTestBase
    {
        private static string Dir => WorldMapTestSupport.JsonDir;

        private static TowerConfigRow Config() => JsonUtility.FromJson<TowerConfigRow>(File.ReadAllText(Dir + "TowerConfig.json"));
        private static List<HeroArchetype_TowerRow> Heroes() => JsonArrayUtility.FromJsonArray<HeroArchetype_TowerRow>(File.ReadAllText(Dir + "HeroArchetype_Tower.json")).ToList();
        private static List<Stage_TowerRow> Templates() => JsonArrayUtility.FromJsonArray<Stage_TowerRow>(File.ReadAllText(Dir + "Stage_Tower.json")).ToList();

        private static TowerDatabase Build(TowerConfigRow config = null, List<HeroArchetype_TowerRow> heroes = null, List<Stage_TowerRow> templates = null)
        {
            return TowerDatabase.Build(config ?? Config(), (heroes ?? Heroes()).ToArray(), (templates ?? Templates()).ToArray(), WorldMapTestSupport.FullDb());
        }

        //# ---------- 데이터 ----------

        [Test]
        public void 출하_JSON_3종은_검증을_통과하고_행_수가_1_5_25다()
        {
            TowerDatabase db = Build();

            Assert.AreEqual(1, db.Config.seasonId);
            Assert.AreEqual(5, Heroes().Count);
            Assert.AreEqual(25, Templates().Count);
            Assert.AreEqual(5, db.HeroCount);
        }

        [Test]
        public void 영웅이_4명뿐이면_로드_오류다()
        {
            List<HeroArchetype_TowerRow> heroes = Heroes();
            heroes.RemoveAt(4);

            Assert.Throws<ChapterDataException>(() => Build(heroes: heroes));
        }

        [Test]
        public void ruleFactor가_1_미만이거나_없는_스테이지를_참조하면_로드_오류다()
        {
            List<Stage_TowerRow> low = Templates();
            low[1].ruleFactor = 0.9f;
            Assert.Throws<ChapterDataException>(() => Build(templates: low));

            List<Stage_TowerRow> missing = Templates();
            missing[0].chapterId = 9;
            Assert.Throws<ChapterDataException>(() => Build(templates: missing));
        }

        [Test]
        public void minFloor_1인_일반_템플릿이_3개뿐이면_로드_오류다()
        {
            List<Stage_TowerRow> templates = Templates();
            foreach (Stage_TowerRow row in templates.Where(t => t.stageIndex < 5 && (t.chapterId > 1 || t.stageIndex == 4)))
            {
                row.minFloor = 6;
            }
            Assert.Throws<ChapterDataException>(() => Build(templates: templates));
        }

        [Test]
        public void 시즌이_0이면_로드_오류다()
        {
            TowerConfigRow config = Config();
            config.seasonId = 0;
            Assert.Throws<ChapterDataException>(() => Build(config: config));
        }

        //# ---------- 결정적 층 생성 ----------

        [Test]
        public void 골든_출하_시드의_1에서_10층_영웅과_템플릿이_고정된다()
        {
            TowerDatabase db = Build();
            (string hero, int chapter, int index)[] golden =
            {
                ("Knight", 5, 2), ("Assassin", 2, 2), ("Berserker", 2, 1), ("Paladin", 4, 2), ("Archer", 1, 5),
                ("Paladin", 1, 3), ("Assassin", 2, 3), ("Berserker", 4, 4), ("Archer", 3, 1), ("Knight", 4, 5),
            };

            for (int floor = 1; floor <= golden.Length; ++floor)
            {
                TowerFloor f = db.GetFloor(floor, 2);
                Assert.AreEqual(golden[floor - 1].hero, f.HeroId, $"{floor}층 영웅");
                Assert.AreEqual(golden[floor - 1].chapter, f.TemplateChapterId, $"{floor}층 템플릿 챕터");
                Assert.AreEqual(golden[floor - 1].index, f.TemplateStageIndex, $"{floor}층 템플릿 인덱스");
            }
        }

        [Test]
        public void 같은_데이터는_두_번_계산해도_같고_JSON_행_순서를_뒤집어도_같다()
        {
            TowerDatabase a = Build();
            List<HeroArchetype_TowerRow> heroes = Heroes();
            heroes.Reverse();
            List<Stage_TowerRow> templates = Templates();
            templates.Reverse();
            TowerDatabase b = Build(heroes: heroes, templates: templates);

            for (int floor = 1; floor <= 200; ++floor)
            {
                TowerFloor x = a.GetFloor(floor, 2);
                TowerFloor y = b.GetFloor(floor, 2);
                Assert.AreEqual(x.HeroId, y.HeroId, $"{floor}층");
                Assert.AreEqual(x.TemplateChapterId, y.TemplateChapterId, $"{floor}층");
                Assert.AreEqual(x.TemplateStageIndex, y.TemplateStageIndex, $"{floor}층");
                Assert.AreEqual(x.FloorHp, a.GetFloor(floor, 2).FloorHp);
            }
        }

        [Test]
        public void 생성_불변식이_1에서_200층_전체에서_성립한다()
        {
            TowerDatabase db = Build();
            Dictionary<string, int> minFloor = Templates().ToDictionary(t => t.chapterId + "-" + t.stageIndex, t => t.minFloor);
            TowerFloor previous = null;
            for (int band = 1; band <= 40; ++band)
            {
                List<TowerFloor> floors = Enumerable.Range(5 * (band - 1) + 1, 5).Select(n => db.GetFloor(n, 2)).ToList();
                Assert.AreEqual(5, floors.Select(f => f.HeroId).Distinct().Count(), $"{band}구간 영웅 5명 서로 다름");
                Assert.AreEqual(4, floors.Take(4).Select(f => f.TemplateChapterId + "-" + f.TemplateStageIndex).Distinct().Count(), $"{band}구간 일반 템플릿 4개 서로 다름");
                Assert.IsTrue(floors[4].IsBoss && floors[4].TemplateStageIndex == 5, $"{band}구간 보스층은 stageIndex 5");
                foreach (TowerFloor f in floors)
                {
                    Assert.LessOrEqual(minFloor[f.TemplateChapterId + "-" + f.TemplateStageIndex], f.IsBoss ? f.Floor : floors[0].Floor, $"{f.Floor}층 minFloor");
                    if (previous != null)
                    {
                        Assert.AreNotEqual(previous.HeroId, f.HeroId, $"{f.Floor}층 인접 영웅");
                    }
                    previous = f;
                }
            }
        }

        [Test]
        public void 층_번호가_1_미만이어도_1층으로_취급한다()
        {
            TowerDatabase db = Build();

            Assert.AreEqual(1, db.GetFloor(0, 2).Floor);
            Assert.AreEqual(db.GetFloor(1, 2).HeroId, db.GetFloor(-7, 2).HeroId);
        }

        //# ---------- 능력치 ----------

        [Test]
        public void 기사_1층은_HP_8000_공격력_75다()
        {
            TowerDatabase db = Build();
            ChapterDatabase chapters = WorldMapTestSupport.FullDb();
            TowerFloor floor = db.GetFloor(1, 2);

            HeroStats actual = HeroStatCalculator.ComputeTower(4000, 50, chapters.GetHero("Knight"), db.GetHeroRow("Knight"), floor);
            floor.RuleFactor = 1f;
            HeroStats plain = HeroStatCalculator.ComputeTower(4000, 50, chapters.GetHero("Knight"), db.GetHeroRow("Knight"), floor);

            Assert.AreEqual(8000, plain.Hp, "ruleFactor 1.0 이면 4000 x 2.0");
            Assert.AreEqual(75, plain.Power);
            Assert.AreEqual(7414, actual.Hp, "골든 1층 = 5-2 (ruleFactor 1.079) 이라 8000 / 1.079 = 7414.27");
            Assert.AreEqual(75, actual.Power);
        }

        [Test]
        public void 광전사_1층_공격력은_55점5의_경계값이라_반올림해_56이다()
        {
            TowerDatabase db = Build();
            ChapterDatabase chapters = WorldMapTestSupport.FullDb();
            TowerFloor floor = db.GetFloor(1, 2);
            floor.RuleFactor = 1f;

            HeroStats stats = HeroStatCalculator.ComputeTower(4000, 50, chapters.GetHero("Berserker"), db.GetHeroRow("Berserker"), floor);

            Assert.AreEqual(56, stats.Power);
            Assert.AreEqual(8000, stats.Hp);
        }

        [Test]
        public void 성기사_10층은_ruleFactor_1_12로_나눠_HP_10719_공격력_99다()
        {
            TowerDatabase db = Build();
            ChapterDatabase chapters = WorldMapTestSupport.FullDb();
            TowerConfigRow c = db.Config;
            TowerFloor floor = new TowerFloor
            {
                Floor = 10,
                IsBoss = true,
                RuleFactor = 1.12f,
                FloorHp = TowerFloorGenerator.FloorMultiplier(c.hpBase, c.hpGrowthPerFloor, c.bossHpMul, c.multiplierCap, 10, true),
                FloorPower = TowerFloorGenerator.FloorMultiplier(1f, c.powerGrowthPerFloor, c.bossPowerMul, c.multiplierCap, 10, true),
            };

            HeroStats stats = HeroStatCalculator.ComputeTower(4000, 50, chapters.GetHero("Paladin"), db.GetHeroRow("Paladin"), floor);

            Assert.AreEqual(10719, stats.Hp);
            Assert.AreEqual(99, stats.Power);
        }

        [Test]
        public void 층_배율은_multiplierCap으로_절단된다()
        {
            decimal normal = TowerFloorGenerator.FloorMultiplier(2f, 0.035f, 1.1f, 100f, 500, false);
            decimal boss = TowerFloorGenerator.FloorMultiplier(2f, 0.035f, 1.1f, 100f, 500, true);

            Assert.AreEqual(100m, normal);
            Assert.AreEqual(100m, boss);
        }

        [Test]
        public void 유물_등급_단계는_10층_Easy_11층_Normal_21층_Hard이고_최대_인덱스로_클램프된다()
        {
            TowerDatabase db = Build();

            Assert.AreEqual(0, db.RelicTierOf(10, 2));
            Assert.AreEqual(1, db.RelicTierOf(11, 2));
            Assert.AreEqual(1, db.RelicTierOf(20, 2));
            Assert.AreEqual(2, db.RelicTierOf(21, 2));
            Assert.AreEqual(1, db.RelicTierOf(21, 1), "난이도 표가 2단계뿐이면 클램프");
            Assert.AreEqual(0, db.RelicTierOf(99, 0), "표 미로드(0)면 항상 0");
        }

        //# ---------- 세이브 ----------

        [Test]
        public void 새_프로필은_v10_탑_기본값을_가진다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(0, p.TowerSeason);
            Assert.AreEqual(0, p.TowerFloorClears.Count);
            Assert.AreEqual(0, p.GetTowerHighestFloor());
        }

        [Test]
        public void 층_클리어_기록은_더_짧을_때만_갱신하고_시즌을_남긴다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordTowerClear(1, 150f, 3);
            p.RecordTowerClear(2, 140f, 3);
            p.RecordTowerClear(1, 200f, 3);
            p.RecordTowerClear(1, 120f, 3);

            Assert.AreEqual(3, p.TowerSeason);
            Assert.AreEqual(2, p.GetTowerHighestFloor());
            Assert.AreEqual(120f, p.GetTowerFloorClearTime(1));
            Assert.AreEqual(2, p.TowerFloorClears.Count);
        }

        [Test]
        public void 불변식을_어기는_층_기록은_무시한다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordTowerClear(0, 100f, 1);
            p.RecordTowerClear(1, 0f, 1);

            Assert.AreEqual(0, p.TowerFloorClears.Count);
            Assert.AreEqual(0, p.TowerSeason);
        }

        [Test]
        public void 시즌_정리는_다른_시즌이면_비우고_같은_시즌_또는_빈_기록이면_유지한다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(1, 100f, 1);

            Assert.IsFalse(p.NormalizeTowerSeason(1));
            Assert.AreEqual(1, p.TowerFloorClears.Count);

            Assert.IsTrue(p.NormalizeTowerSeason(2));
            Assert.AreEqual(0, p.TowerFloorClears.Count);
            Assert.AreEqual(2, p.TowerSeason);

            MetaProfile empty = new MetaProfile { TowerSeason = 5 };
            Assert.IsFalse(empty.NormalizeTowerSeason(9));
            Assert.AreEqual(5, empty.TowerSeason, "빈 리스트의 시즌 값은 의미가 없어 건드리지 않는다");
        }

        [Test]
        public void 마이그레이션은_손상된_탑_행을_정리하고_멱등이다()
        {
            MetaProfile p = new MetaProfile { Version = 9, TowerSeason = -3 };
            p.TowerFloorClears.Add(null);
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 0, ClearTime = 10f });
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 3, ClearTime = 0f });
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 2, ClearTime = 90f });
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 2, ClearTime = 70f });

            MetaProfileMigration.Apply(p, (Func<int, string>)null);
            string once = JsonUtility.ToJson(p);
            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(0, p.TowerSeason);
            Assert.AreEqual(1, p.TowerFloorClears.Count);
            Assert.AreEqual(70f, p.GetTowerFloorClearTime(2));
            Assert.AreEqual(once, JsonUtility.ToJson(p));
        }

        [Test]
        public void CopyFrom은_탑_진행을_새_인스턴스로_복사하고_null이면_빈_리스트다()
        {
            MetaProfile src = new MetaProfile();
            src.RecordTowerClear(4, 111f, 2);
            MetaProfile dst = new MetaProfile();

            dst.CopyFrom(src);
            src.TowerFloorClears[0].ClearTime = 999f;

            Assert.AreEqual(2, dst.TowerSeason);
            Assert.AreEqual(4, dst.GetTowerHighestFloor());
            Assert.AreEqual(111f, dst.GetTowerFloorClearTime(4), "원본 행을 공유하지 않는다");

            src.TowerFloorClears = null;
            dst.CopyFrom(src);
            Assert.AreEqual(0, dst.TowerFloorClears.Count);
        }

        //# ---------- 전투 사양 ----------

        [Test]
        public void 탑_사양은_SelectedStage와_난이도를_읽지도_바꾸지도_않는다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            TowerDatabase tower = Build();
            MetaProfile p = new MetaProfile { SelectedStage = 7, ClearedStage = 25, SelectedDifficulty = 2 };

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, tower);

            Assert.IsTrue(spec.IsTower);
            Assert.AreEqual(1, spec.Floor.Floor);
            Assert.AreEqual("Knight", spec.Hero.heroId);
            Assert.AreEqual(0, spec.EffectiveDifficulty);
            Assert.AreEqual(5, spec.RuleChapterId, "골든 1층 템플릿 5-2");
            Assert.AreEqual(2, spec.RuleStageIndex);
            Assert.AreEqual(1, spec.BackdropChapterId, "배경은 영웅(기사) 챕터");
            Assert.AreEqual(StageProgress.ToLinear(1, 1), spec.LinearStage);
            Assert.AreEqual(7, p.SelectedStage);
            Assert.AreEqual(2, p.SelectedDifficulty);
        }

        [Test]
        public void 도전_층은_최고_층_더하기_1이고_탑_데이터가_없으면_사양이_null이다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            TowerDatabase tower = Build();
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(1, 100f, 1);
            p.RecordTowerClear(2, 100f, 1);

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, tower);

            Assert.AreEqual(3, spec.Floor.Floor);
            Assert.IsNull(BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, null));
        }

        [Test]
        public void 시즌이_바뀌면_사양_해석이_이전_시즌_진행을_지우고_1층부터다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            TowerDatabase tower = Build();
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(5, 100f, 7);

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Tower, p, chapters, tower);

            Assert.AreEqual(1, spec.Floor.Floor);
            Assert.AreEqual(0, p.TowerFloorClears.Count);
        }

        [Test]
        public void 챕터_사양은_기존_SelectedStage_해석과_같다()
        {
            ChapterDatabase chapters = WorldMapTestSupport.ShippedDb();
            MetaProfile p = new MetaProfile { SelectedStage = 8, ClearedStage = 8, SelectedDifficulty = 0 };

            BattleSpec spec = BattleSpecResolver.Resolve(EBattleMode.Chapter, p, chapters, null);

            Assert.IsFalse(spec.IsTower);
            Assert.AreEqual(8, spec.LinearStage);
            Assert.AreEqual(2, spec.RuleChapterId);
            Assert.AreEqual(3, spec.RuleStageIndex);
            Assert.AreEqual("Archer", spec.Hero.heroId);
            Assert.AreSame(chapters.GetStageByLinear(8), spec.Stage);
            Assert.IsTrue(spec.CanComputeHeroStats);
        }

        //# ---------- 제목 / 결과창 ----------

        [Test]
        public void 탑_제목은_보스층에만_보스층_접미가_붙는다()
        {
            Assert.AreEqual("기사 · 탑 18층", BattleViewModel.ComposeTowerTitle("기사", 18, false, WorldMapTestSupport.Text));
            Assert.AreEqual("성기사 · 탑 10층 · 보스층", BattleViewModel.ComposeTowerTitle("성기사", 10, true, WorldMapTestSupport.Text));
            Assert.AreEqual("기사 · 탑 3층", BattleViewModel.ComposeTowerTitle("기사", 3, false, null), "문구 테이블이 없으면 기본 문구");
        }

        [Test]
        public void 탑_승리_결과창은_도달_층_줄_돌파_배너_다음_층_버튼이다()
        {
            ResultPopupArg arg = new ResultPopupArg { Result = BattleResult.Win, HasMeta = true, IsTower = true, TowerFloor = 18, ClearTime = 100f, IsNewBest = true };

            ResultRowsData rows = ResultPopup.BuildRows(arg);

            Assert.IsTrue(rows.ShowDifficultyRow);
            Assert.AreEqual("도달 층", rows.DifficultyLabelText);
            Assert.AreEqual("18층", rows.DifficultyText);
            Assert.IsTrue(rows.ShowUnlockBanner);
            Assert.AreEqual("18층 돌파!", rows.UnlockBannerText);
            Assert.AreEqual("다음 층", rows.RetryLabelText);
            Assert.IsFalse(rows.ShowNewBadge, "탑에는 최단 신기록이 없다");
            StringAssert.EndsWith("탑 18층", rows.SubText);
        }

        [Test]
        public void 탑_패배_결과창은_배너가_없고_다시_도전이며_챕터_판은_난이도_라벨이_복원된다()
        {
            ResultRowsData lose = ResultPopup.BuildRows(new ResultPopupArg { Result = BattleResult.Lose, IsTower = true, TowerFloor = 4 });
            Assert.IsFalse(lose.ShowUnlockBanner);
            Assert.AreEqual("다시 도전", lose.RetryLabelText);
            Assert.IsTrue(lose.ShowHeroHpRow);
            Assert.IsTrue(lose.ShowDifficultyRow);

            ResultRowsData chapter = ResultPopup.BuildRows(new ResultPopupArg { Result = BattleResult.Win, Difficulty = 1 });
            Assert.AreEqual("난이도", chapter.DifficultyLabelText, "재사용 팝업이 탑 라벨을 남기지 않게 챕터도 매번 확정");
            Assert.AreEqual("다시 도전", chapter.RetryLabelText);
            Assert.IsFalse(chapter.ShowUnlockBanner);
        }

        //# ---------- 사다리 VM ----------

        private static TowerViewModel Vm(MetaProfile p, TowerDatabase tower = null)
        {
            return new TowerViewModel(tower ?? Build(), WorldMapTestSupport.FullDb(), p, WorldMapTestSupport.Text, null, 4000, 50);
        }

        [Test]
        public void 사다리는_미리보기_4개_도전_층_클리어_내림차순이고_도전_버튼은_최고_층_더하기_1이다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(1, 100f, 1);
            p.RecordTowerClear(2, 90.5f, 1);
            p.RecordTowerClear(3, 80f, 1);
            TowerViewModel vm = Vm(p);

            List<TowerFloorCellData> cells = vm.BuildLadder();

            CollectionAssert.AreEqual(new[] { 8, 7, 6, 5, 4, 3, 2, 1 }, cells.Select(c => c.Floor).ToArray());
            Assert.AreEqual(ETowerFloorState.Challenge, cells[4].State);
            Assert.AreEqual(ETowerFloorState.Preview, cells[0].State);
            Assert.AreEqual(ETowerFloorState.Cleared, cells[5].State);
            Assert.AreEqual("도전 중", cells[4].TagText);
            Assert.AreEqual("클리어 1:30.5", cells[6].TagText);
            Assert.AreEqual("도전 · 4층", vm.ChallengeButtonText);
            Assert.AreEqual("시즌 1 · 최고 3층", vm.SubtitleText);
            Assert.IsTrue(cells[0].IsBoss == false && cells[3].IsBoss, "5층(위에서 4번째)은 보스층");
        }

        [Test]
        public void 기록이_없으면_부제가_기록_없음이고_1층이_도전_층이다()
        {
            TowerViewModel vm = Vm(new MetaProfile());

            Assert.AreEqual("시즌 1 · 기록 없음", vm.SubtitleText);
            Assert.AreEqual(1, vm.ChallengeFloor);
            Assert.AreEqual("기록 없음", vm.BuildDetail().RecordText);
        }

        [Test]
        public void 셀을_선택하면_상세만_바뀌고_도전_버튼의_층은_그대로다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordTowerClear(1, 100f, 1);
            TowerViewModel vm = Vm(p);
            int changed = 0;
            vm.OnChanged += () => changed++;

            vm.SelectFloor(1);
            vm.SelectFloor(1);
            vm.SelectFloor(99);

            Assert.AreEqual(1, changed, "같은 층·사다리 밖은 통지하지 않는다");
            Assert.AreEqual(1, vm.SelectedFloor);
            Assert.AreEqual("1층", vm.BuildDetail().FloorText);
            Assert.AreEqual("클리어 1:40.0", vm.BuildDetail().RecordText);
            Assert.AreEqual("도전 · 2층", vm.ChallengeButtonText);
        }

        [Test]
        public void 상세는_실제_계산_능력치와_규칙_출처를_보여준다()
        {
            TowerViewModel vm = Vm(new MetaProfile());

            TowerFloorDetail detail = vm.BuildDetail();

            Assert.AreEqual("영웅 HP 7,414 · 공격력 75", detail.StatsText);
            Assert.AreEqual(1, detail.RuleTexts.Length, "골든 1층 = 5-2 (규칙 1개)");
            Assert.IsTrue(detail.SourceText.StartsWith("규칙 출처: 5-2"));
            Assert.IsFalse(detail.ShowReward, "유물 DB 가 없으면 보상 줄을 숨긴다");
        }

        //# ---------- 문구 / 프리팹 ----------

        [Test]
        public void Strings_Ko_1413에서_1436은_24개가_빠짐없이_연속_존재한다()
        {
            for (int id = 1413; id <= 1436; ++id)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"문구 {id}");
            }
            Assert.IsNotEmpty(WorldMapTestSupport.Text(TowerStrings.Retry));
            StringAssert.Contains("{0}", WorldMapTestSupport.Text(TowerStrings.LockedToast));
            StringAssert.Contains("{1}", WorldMapTestSupport.Text(TowerStrings.StatsLine));
            StringAssert.Contains("{2}", WorldMapTestSupport.Text(TowerStrings.RuleSource));
        }

        [Test]
        public void 탑_프리팹의_직렬화_참조가_전부_연결되어_있다()
        {
            AssertNoNullReferences("Assets/_Lair/Art/UI/TowerPopup.prefab", typeof(TowerPopup));
            AssertNoNullReferences("Assets/_Lair/Art/UI/TowerFloorCell.prefab", typeof(TowerFloorCell));
            AssertSerializedRefs("Assets/_Lair/Art/UI/VillageHud.prefab", typeof(VillageHud), "_towerButton", "_towerLockOverlay");
            AssertSerializedRefs("Assets/_Lair/Art/UI/ResultPopup.prefab", typeof(ResultPopup), "_difficultyLabelText", "_retryLabelText");
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

        //# UIBase 의 선택 필드(_backgroundButton/_backButton)는 제외하고 이 컴포넌트 자신의 [SerializeField] 만 본다.
        private static void AssertNoNullReferences(string path, Type componentType)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            foreach (System.Reflection.FieldInfo field in componentType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
                    continue;
                UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(prefab.GetComponent(componentType));
                UnityEditor.SerializedProperty prop = so.FindProperty(field.Name);
                if (prop.propertyType == UnityEditor.SerializedPropertyType.ObjectReference)
                {
                    Assert.IsNotNull(prop.objectReferenceValue, $"{componentType.Name}.{field.Name} 미연결");
                }
                else if (prop.isArray)
                {
                    for (int i = 0; i < prop.arraySize; ++i)
                    {
                        if (prop.GetArrayElementAtIndex(i).propertyType == UnityEditor.SerializedPropertyType.ObjectReference)
                        {
                            Assert.IsNotNull(prop.GetArrayElementAtIndex(i).objectReferenceValue, $"{componentType.Name}.{field.Name}[{i}] 미연결");
                        }
                    }
                }
            }
        }
    }
}
