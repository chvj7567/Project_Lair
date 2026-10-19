using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 난이도 선택 — Difficulty.json 검증 · 영웅 능력치 계산(Easy 비트 동일 · 1회 반올림) · 문구 id (difficulty-select §2.2·§4.1·§5.6).
    public class DifficultyDataAndStatTests : DifficultyStaticStateTestBase
    {
        private const int AnchorHp = 4000;
        private const int AnchorPower = 50;

        private static DifficultyRow[] LoadShipped()
        {
            return JsonArrayUtility.FromJsonArray<DifficultyRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Difficulty.json"));
        }

        private static DifficultyRow Row(int difficulty, float hp, float power, int nameKey = 1327)
        {
            return new DifficultyRow { difficulty = difficulty, nameKey = nameKey, hpMultiplier = hp, powerMultiplier = power };
        }

        //# --- 출하 Difficulty.json ---

        [Test]
        public void 출하_Difficulty_json은_정확히_3행이고_기획서_배율표와_같다()
        {
            DifficultyRow[] rows = LoadShipped();

            Assert.AreEqual(3, rows.Length);
            float[] hp = { 1.00f, 1.15f, 1.30f };
            float[] power = { 1.00f, 1.10f, 1.22f };
            int[] nameKey = { 1327, 1328, 1329 };
            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(i, rows[i].difficulty, $"행 {i} difficulty");
                Assert.AreEqual(nameKey[i], rows[i].nameKey, $"행 {i} nameKey");
                Assert.AreEqual(hp[i], rows[i].hpMultiplier, 1e-6f, $"행 {i} hp");
                Assert.AreEqual(power[i], rows[i].powerMultiplier, 1e-6f, $"행 {i} power");
            }
        }

        [Test]
        public void 출하_Difficulty_json은_검증을_통과하고_nameKey가_실제_Strings_Ko에_있다()
        {
            DifficultyTable table = null;

            Assert.DoesNotThrow(() => table = DifficultyTable.Build(LoadShipped(), id => WorldMapTestSupport.Strings().ContainsKey(id)));

            Assert.AreEqual(2, table.MaxIndex);
            Assert.AreEqual("Easy", table.Label(0, WorldMapTestSupport.Text));
            Assert.AreEqual("Normal", table.Label(1, WorldMapTestSupport.Text));
            Assert.AreEqual("Hard", table.Label(2, WorldMapTestSupport.Text));
        }

        //# --- Build 검증 위반 ---

        [Test]
        public void 행_수가_3이_아니면_로드_오류다_null_0_2_4()
        {
            DifficultyRow[] two = { Row(0, 1f, 1f), Row(1, 1.1f, 1.1f) };
            DifficultyRow[] four = { Row(0, 1f, 1f), Row(1, 1.1f, 1.1f), Row(2, 1.2f, 1.2f), Row(3, 1.3f, 1.3f) };

            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(null));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(new DifficultyRow[0]));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(two));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(four));
        }

        [Test]
        public void difficulty_번호가_중복되거나_0_2_밖이거나_null_행이면_로드_오류다()
        {
            DifficultyRow[] duplicated = { Row(0, 1f, 1f), Row(1, 1.1f, 1.1f), Row(1, 1.2f, 1.2f) };
            DifficultyRow[] outOfRange = { Row(0, 1f, 1f), Row(1, 1.1f, 1.1f), Row(3, 1.2f, 1.2f) };
            DifficultyRow[] negative = { Row(-1, 1f, 1f), Row(1, 1.1f, 1.1f), Row(2, 1.2f, 1.2f) };
            DifficultyRow[] hasNull = { Row(0, 1f, 1f), null, Row(2, 1.2f, 1.2f) };

            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(duplicated));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(outOfRange));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(negative));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(hasNull));
        }

        [TestCase(1.01f, 1.0f)]
        [TestCase(1.0f, 0.99f)]
        [TestCase(0.9f, 0.9f)]
        public void Easy_행이_1_0_1_0이_아니면_로드_오류다(float hp, float power)
        {
            DifficultyRow[] rows = { Row(0, hp, power), Row(1, 1.15f, 1.1f), Row(2, 1.3f, 1.22f) };

            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(rows));
        }

        [Test]
        public void 배율이_단계가_오를수록_줄어들면_HP와_Power_각각_로드_오류다()
        {
            DifficultyRow[] hpDrop = { Row(0, 1f, 1f), Row(1, 1.15f, 1.1f), Row(2, 1.14f, 1.22f) };
            DifficultyRow[] powerDrop = { Row(0, 1f, 1f), Row(1, 1.15f, 1.1f), Row(2, 1.3f, 1.09f) };
            DifficultyRow[] normalBelowEasy = { Row(0, 1f, 1f), Row(1, 0.9f, 1.1f), Row(2, 1.3f, 1.22f) };

            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(hpDrop));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(powerDrop));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(normalBelowEasy));
        }

        [Test]
        public void 배율이_0_이하면_로드_오류다()
        {
            DifficultyRow[] zeroHp = { Row(0, 1f, 1f), Row(1, 0f, 1.1f), Row(2, 1.3f, 1.22f) };
            DifficultyRow[] negativePower = { Row(0, 1f, 1f), Row(1, 1.15f, 1.1f), Row(2, 1.3f, -1f) };

            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(zeroHp));
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(negativePower));
        }

        [Test]
        public void nameKey_검증은_존재_판정_함수가_있을_때만_하고_없는_id면_로드_오류다()
        {
            Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(DifficultyTestSupport.Rows(), id => id != 1328));
            Assert.DoesNotThrow(() => DifficultyTable.Build(DifficultyTestSupport.Rows(), null));
        }

        [Test]
        public void 여러_위반은_한_번에_모아_메시지에_담긴다()
        {
            DifficultyRow[] rows = { Row(0, 1.5f, 1f), Row(1, 1.15f, 1.1f), Row(2, 1.1f, 1.22f) };

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => DifficultyTable.Build(rows));

            StringAssert.Contains("Easy", e.Message);
            StringAssert.Contains("단조", e.Message);
        }

        [Test]
        public void 배율이_같은_단계는_단조_비감소라_허용되고_행_순서는_무관하다()
        {
            DifficultyRow[] flat = { Row(0, 1f, 1f), Row(1, 1f, 1f), Row(2, 1f, 1f) };
            DifficultyRow[] shuffled = { Row(2, 1.3f, 1.22f), Row(0, 1f, 1f), Row(1, 1.15f, 1.1f) };

            Assert.DoesNotThrow(() => DifficultyTable.Build(flat));
            DifficultyTable table = DifficultyTable.Build(shuffled);
            Assert.AreEqual(1.15f, table.Get(1).hpMultiplier, 1e-6f);
            Assert.AreEqual(1.3f, table.Get(2).hpMultiplier, 1e-6f);
        }

        //# --- 조회 / 정적 상태 ---

        [Test]
        public void Get은_범위_밖_인덱스를_첫행_마지막행으로_클램프한다()
        {
            DifficultyTable table = DifficultyTestSupport.Table();

            Assert.AreEqual(0, table.Get(-5).difficulty);
            Assert.AreEqual(2, table.Get(99).difficulty);
        }

        [Test]
        public void 표_미로드면_최대_인덱스_0이고_CurrentRow는_null이다()
        {
            DifficultyTable.Current = null;

            Assert.AreEqual(0, DifficultyTable.CurrentMaxIndex);
            Assert.IsNull(DifficultyTable.CurrentRow(0));
            Assert.IsNull(DifficultyTable.CurrentRow(2));
        }

        [Test]
        public void 표_로드시_최대_인덱스는_2이고_CurrentRow는_Easy만_null이다()
        {
            DifficultyTable.Current = DifficultyTestSupport.Table();

            Assert.AreEqual(2, DifficultyTable.CurrentMaxIndex);
            Assert.IsNull(DifficultyTable.CurrentRow(0), "Easy 는 null = 기존 경로");
            Assert.AreEqual(1, DifficultyTable.CurrentRow(1).difficulty);
            Assert.AreEqual(2, DifficultyTable.CurrentRow(2).difficulty);
            Assert.AreEqual(2, DifficultyTable.CurrentRow(9).difficulty, "범위 밖은 Hard 로 클램프");
        }

        [Test]
        public void 라벨은_표_미로드나_문구_미로드면_영문_기본명_로드되면_Strings_Ko다()
        {
            DifficultyTable.Current = null;
            Assert.AreEqual("Easy", DifficultyTable.LabelOf(0));
            Assert.AreEqual("Hard", DifficultyTable.LabelOf(2));
            Assert.AreEqual("Hard", DifficultyTable.LabelOf(9));
            Assert.AreEqual("Easy", DifficultyTable.LabelOf(-1));

            DifficultyTable.Current = DifficultyTestSupport.Table();
            CHText.StringProvider = null;
            Assert.AreEqual("Normal", DifficultyTable.LabelOf(1), "provider 없음 → 빈 문구 → 영문 기본명");

            CHText.StringProvider = new DifficultyTestSupport.RealStrings();
            Assert.AreEqual("Normal", DifficultyTable.LabelOf(1));
            Assert.AreEqual("Custom", DifficultyTable.LabelOf(1, id => "Custom"));
        }

        //# --- 능력치: Easy 경로 비트 동일 ---

        [Test]
        public void Easy_경로_5인자_null은_모든_직업_스테이지에서_4인자와_비트_동일하다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            int checkedCount = 0;

            foreach (ChapterRow chapter in db.Chapters)
            {
                HeroArchetypeRow hero = db.GetHero(chapter.heroId);
                for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
                {
                    StageRow stage = db.GetStage(chapter.chapterId, index);
                    HeroStats legacy = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage);
                    HeroStats viaNull = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage, null);
                    HeroStats viaEasyRow = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage, DifficultyTestSupport.Rows()[0]);

                    string label = $"{chapter.chapterId}-{index}";
                    Assert.AreEqual(legacy.Hp, viaNull.Hp, label);
                    Assert.AreEqual(legacy.Power, viaNull.Power, label);
                    Assert.AreEqual(legacy.Range, viaNull.Range, label);
                    Assert.AreEqual(legacy.Cooldown, viaNull.Cooldown, label);
                    Assert.AreEqual(legacy.MoveSpeed, viaNull.MoveSpeed, label);
                    Assert.AreEqual(legacy.Hp, viaEasyRow.Hp, label + " Easy 행(1.0/1.0)");
                    Assert.AreEqual(legacy.Power, viaEasyRow.Power, label + " Easy 행(1.0/1.0)");
                    ++checkedCount;
                }
            }
            Assert.AreEqual(25, checkedCount);
        }

        [Test]
        public void ScaleHeroStat_4인자_배율1은_3인자와_같다()
        {
            float[] archetypes = { 0.7f, 0.8f, 1f, 1.1f, 1.25f };
            float[] stages = { 1f, 1.1f, 1.36f, 1.92f, 3.2f };

            foreach (float a in archetypes)
            {
                foreach (float s in stages)
                {
                    Assert.AreEqual(StageProgress.ScaleHeroStat(AnchorHp, a, s), StageProgress.ScaleHeroStat(AnchorHp, a, s, 1f), $"a={a} s={s}");
                    Assert.AreEqual(StageProgress.ScaleHeroStat(AnchorPower, a, s), StageProgress.ScaleHeroStat(AnchorPower, a, s, 1f), $"a={a} s={s}");
                }
            }
        }

        //# --- 능력치: Normal / Hard ---

        [TestCase("Knight", 1, 1, 1, 4600, 55)]
        [TestCase("Knight", 1, 1, 2, 5200, 61)]
        [TestCase("Knight", 1, 5, 1, 8832, 83)]
        [TestCase("Knight", 1, 5, 2, 9984, 92)]
        public void 기사_표본은_기획서_곱연산_예시와_일치하고_공격력_반올림_경계_82_5와_91_5는_올림이다(string heroId, int chapter, int stageIndex, int difficulty, int expectedHp, int expectedPower)
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();

            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, db.GetHero(heroId), db.GetStage(chapter, stageIndex), DifficultyTestSupport.Rows()[difficulty]);

            Assert.AreEqual(expectedHp, stats.Hp);
            Assert.AreEqual(expectedPower, stats.Power);
        }

        [Test]
        public void 전_직업_전_스테이지_Normal_Hard는_곱_전체를_1회_반올림한_독립_계산값과_같다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            DifficultyRow[] rows = DifficultyTestSupport.Rows();
            decimal[] hpMul = { 1.00m, 1.15m, 1.30m };
            decimal[] powerMul = { 1.00m, 1.10m, 1.22m };

            foreach (ChapterRow chapter in db.Chapters)
            {
                HeroArchetypeRow hero = db.GetHero(chapter.heroId);
                for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
                {
                    StageRow stage = db.GetStage(chapter.chapterId, index);
                    for (int d = 1; d <= 2; ++d)
                    {
                        decimal hpExact = AnchorHp * (decimal)hero.hpScale * (decimal)stage.hpMultiplier * hpMul[d];
                        decimal powerExact = AnchorPower * (decimal)hero.powerScale * (decimal)stage.powerMultiplier * powerMul[d];
                        HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage, rows[d]);

                        string label = $"{chapter.chapterId}-{index} d{d}";
                        Assert.AreEqual((int)System.Math.Floor(hpExact + 0.5m), stats.Hp, label + " HP");
                        Assert.AreEqual((int)System.Math.Floor(powerExact + 0.5m), stats.Power, label + " Power");
                    }
                }
            }
        }

        [Test]
        public void 난이도가_오를수록_HP_Power는_줄지_않고_사거리_쿨다운_이동속도는_직업_값_그대로다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            DifficultyRow[] rows = DifficultyTestSupport.Rows();

            foreach (ChapterRow chapter in db.Chapters)
            {
                HeroArchetypeRow hero = db.GetHero(chapter.heroId);
                for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
                {
                    StageRow stage = db.GetStage(chapter.chapterId, index);
                    HeroStats easy = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage);
                    HeroStats normal = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage, rows[1]);
                    HeroStats hard = HeroStatCalculator.Compute(AnchorHp, AnchorPower, hero, stage, rows[2]);

                    string label = $"{chapter.chapterId}-{index}";
                    Assert.LessOrEqual(easy.Hp, normal.Hp, label);
                    Assert.LessOrEqual(normal.Hp, hard.Hp, label);
                    Assert.LessOrEqual(easy.Power, normal.Power, label);
                    Assert.LessOrEqual(normal.Power, hard.Power, label);
                    Assert.AreEqual(hero.range, hard.Range, label);
                    Assert.AreEqual(hero.cooldown, hard.Cooldown, label);
                    Assert.AreEqual(hero.moveSpeed, hard.MoveSpeed, label);
                    Assert.AreEqual(hero.range, normal.Range, label);
                }
            }
        }

        //# --- 반올림 경계 ---

        [Test]
        public void 반올림은_곱_전체에_1회만_적용된다_중간_반올림이면_12가_된다()
        {
            //# 10 x 1.05 x 1.05 = 11.025 → 11. (10 x 1.05 = 10.5 → 11 로 먼저 반올림하면 11 x 1.05 = 11.55 → 12)
            Assert.AreEqual(11, StageProgress.ScaleHeroStat(10, 1.05f, 1f, 1.05f));
        }

        [TestCase(10, 0.25f, 3)]
        [TestCase(10, 0.24f, 2)]
        [TestCase(10, 0.26f, 3)]
        [TestCase(1, 0.5f, 1)]
        [TestCase(3, 0.5f, 2)]
        [TestCase(5, 0.3f, 2)]
        public void 난이도_배율_반올림은_0_5에서_올림이다(int anchor, float difficultyMul, int expected)
        {
            Assert.AreEqual(expected, StageProgress.ScaleHeroStat(anchor, 1f, 1f, difficultyMul));
        }

        [Test]
        public void float_표현_오차가_있는_배율도_정확히_절반이면_올림한다()
        {
            //# 50 x 0.7 x 1.3 = 45.5 — float 곱이면 45.4999.. 로 내려갈 수 있는 경계(decimal 정규화).
            Assert.AreEqual(46, StageProgress.ScaleHeroStat(50, 0.7f, 1.3f, 1f));
            Assert.AreEqual(46, StageProgress.ScaleHeroStat(50, 0.7f, 1f, 1.3f));
        }

        [Test]
        public void 난이도_적용_후에도_HP_Power는_최소_1이다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();
            DifficultyRow tiny = Row(1, 0.0001f, 0.0001f);

            HeroStats stats = HeroStatCalculator.Compute(AnchorHp, AnchorPower, db.GetHero("Knight"), db.GetStage(1, 1), tiny);
            HeroStats zeroAnchor = HeroStatCalculator.Compute(0, 0, db.GetHero("Knight"), db.GetStage(1, 1), DifficultyTestSupport.Rows()[2]);

            Assert.AreEqual(1, stats.Hp);
            Assert.AreEqual(1, stats.Power);
            Assert.AreEqual(1, zeroAnchor.Hp);
            Assert.AreEqual(1, zeroAnchor.Power);
        }

        //# --- Strings_Ko 1327~1334 ---

        [Test]
        public void 난이도_문구_1327에서_1334가_존재하고_라벨은_Easy_Normal_Hard다()
        {
            for (int id = 1327; id <= 1334; ++id)
            {
                Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(id), $"id {id} 없음");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id} 비어 있음");
            }
            Assert.AreEqual("Easy", WorldMapTestSupport.Text(1327));
            Assert.AreEqual("Normal", WorldMapTestSupport.Text(1328));
            Assert.AreEqual("Hard", WorldMapTestSupport.Text(1329));
            Assert.AreEqual("난이도", WorldMapTestSupport.Text(1334));
        }

        [Test]
        public void 난이도_서식_문구는_필요한_인자_수로_Format해도_예외가_없고_자리표시자가_남지_않는다()
        {
            (int id, object[] args)[] cases =
            {
                (1330, new object[] { "1.15", "1.1" }), (1331, new object[] { "Easy" }),
                (1332, new object[] { "Normal" }), (1333, new object[] { "Hard" }),
            };

            foreach ((int id, object[] args) in cases)
            {
                string formatted = null;
                Assert.DoesNotThrow(() => formatted = string.Format(WorldMapTestSupport.Text(id), args), $"id {id}");
                StringAssert.DoesNotContain("{", formatted, $"id {id}");
            }
        }

        [Test]
        public void 월드맵_ViewModel_난이도_문구_상수는_1330_1331_1332다()
        {
            Assert.AreEqual(1330, Lair.Village.WorldMapViewModel.StrDifficultyEffect);
            Assert.AreEqual(1331, Lair.Village.WorldMapViewModel.StrDifficultyLockedHint);
            Assert.AreEqual(1332, Lair.Village.WorldMapViewModel.StrBestDifficulty);
        }
    }
}
