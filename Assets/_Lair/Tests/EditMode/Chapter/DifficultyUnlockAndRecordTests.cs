using Lair.Battle;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 난이도 선택 — 해금 규칙 · 최고 난이도 기록 갱신 · 해금 배너 값 · Normal/Hard 판 최단 기록 제외 (difficulty-select §1.4·§1.5·§1.6·§4.4).
    public class DifficultyUnlockAndRecordTests
    {
        //# --- MaxSelectableDifficulty / ClampDifficulty (순수 함수) ---

        [TestCase(1, 0, 0, 2, 0)]      //# 미클리어
        [TestCase(1, 1, 0, 2, 1)]      //# 클리어(행 없음) → Normal
        [TestCase(1, 1, 1, 2, 2)]      //# 행 Normal → Hard
        [TestCase(1, 1, 2, 2, 2)]      //# 행 Hard → 상한
        [TestCase(1, 5, 3, 2, 2)]      //# 행 값이 상한을 넘어도 상한
        [TestCase(5, 4, 2, 2, 0)]      //# 스테이지 > ClearedStage 면 행이 있어도 Easy 만
        [TestCase(5, 5, 0, 2, 1)]      //# 경계: stage == ClearedStage 는 클리어
        [TestCase(6, 5, 0, 2, 0)]      //# 경계: stage == ClearedStage + 1 은 미클리어
        [TestCase(3, 3, 0, 0, 0)]      //# 난이도 표 미로드(상한 0) → Easy 만
        [TestCase(3, 3, 0, 1, 1)]
        [TestCase(2, 3, -1, 2, 1)]     //# 음수 행 값은 0 으로 본다
        public void MaxSelectableDifficulty는_기획서_1_4_규칙을_따른다(int stage, int clearedStage, int clearedMax, int maxDifficulty, int expected)
        {
            Assert.AreEqual(expected, StageProgress.MaxSelectableDifficulty(stage, clearedStage, clearedMax, maxDifficulty));
        }

        [TestCase(-1, 2, 0)]
        [TestCase(-99, 2, 0)]
        [TestCase(0, 2, 0)]
        [TestCase(1, 2, 1)]
        [TestCase(2, 2, 2)]
        [TestCase(3, 2, 2)]
        [TestCase(99, 2, 2)]
        [TestCase(2, 1, 1)]
        [TestCase(2, 0, 0)]
        public void ClampDifficulty는_0에서_선택_가능_상한_사이로_보정한다(int difficulty, int maxSelectable, int expected)
        {
            //# 상한 maxSelectable 을 만들기 위해 stage=1, ClearedStage=1, 행 값=maxSelectable-1 (0 이면 미클리어 stage 2).
            int stage = maxSelectable == 0 ? 2 : 1;
            int clearedStage = maxSelectable == 0 ? 0 : 1;
            int clearedMax = System.Math.Max(0, maxSelectable - 1);

            Assert.AreEqual(expected, StageProgress.ClampDifficulty(difficulty, stage, clearedStage, clearedMax, 2));
        }

        [Test]
        public void 미클리어_스테이지는_행이_있어도_어떤_난이도든_Easy로_클램프된다()
        {
            Assert.AreEqual(0, StageProgress.ClampDifficulty(2, 4, 3, 2, 2));
        }

        //# --- MetaProfile 래퍼 ---

        [Test]
        public void GetMaxClearedDifficulty는_행이_없거나_리스트가_null이어도_0이고_조회는_부수효과가_없다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(0, p.GetMaxClearedDifficulty(3));
            Assert.AreEqual(0, p.DifficultyClears.Count);

            p.DifficultyClears = null;
            Assert.AreEqual(0, p.GetMaxClearedDifficulty(3));
            Assert.IsNull(p.DifficultyClears, "조회가 리스트를 만들지 않는다");
        }

        [Test]
        public void GetMaxClearedDifficulty는_null_엔트리를_건너뛰고_스테이지별로_독립이다()
        {
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(null);
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 1 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 4, MaxDifficulty = 2 });

            Assert.AreEqual(1, p.GetMaxClearedDifficulty(2));
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(4));
            Assert.AreEqual(0, p.GetMaxClearedDifficulty(3));
        }

        [Test]
        public void 프로필_MaxSelectable은_스테이지별로_독립이고_Clamp는_프로필을_수정하지_않는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 4, SelectedDifficulty = 2 };
            p.RecordDifficultyClear(2, 1);
            p.RecordDifficultyClear(3, 2);

            Assert.AreEqual(1, p.MaxSelectableDifficulty(1, 2), "행 없음 → Normal");
            Assert.AreEqual(2, p.MaxSelectableDifficulty(2, 2), "Normal 클리어 → Hard");
            Assert.AreEqual(2, p.MaxSelectableDifficulty(3, 2));
            Assert.AreEqual(0, p.MaxSelectableDifficulty(5, 2), "미클리어");
            Assert.AreEqual(1, p.ClampDifficulty(2, 1, 2));
            Assert.AreEqual(2, p.SelectedDifficulty);
            Assert.AreEqual(2, p.DifficultyClears.Count);
        }

        //# --- 기록 갱신 (§1.5) ---

        [Test]
        public void 행이_없을_때_Normal_승리는_새_행을_만든다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordDifficultyClear(3, 1);

            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(3, p.DifficultyClears[0].Stage);
            Assert.AreEqual(1, p.DifficultyClears[0].MaxDifficulty);
        }

        [Test]
        public void 더_높은_난이도_승리는_올리고_낮거나_같은_난이도_재클리어는_내리지_않는다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordDifficultyClear(1, 1);
            p.RecordDifficultyClear(1, 2);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(1), "Normal → Hard");

            p.RecordDifficultyClear(1, 1);
            p.RecordDifficultyClear(1, 2);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(1), "Hard 기록 후 Normal/Hard 재클리어");
            Assert.AreEqual(1, p.DifficultyClears.Count, "한 스테이지에 행은 하나");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Easy_승리나_음수_난이도는_행을_만들거나_바꾸지_않는다(int difficulty)
        {
            MetaProfile p = new MetaProfile();
            p.RecordDifficultyClear(2, 2);

            p.RecordDifficultyClear(1, difficulty);
            p.RecordDifficultyClear(2, difficulty);

            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(2));
            Assert.AreEqual(0, p.GetMaxClearedDifficulty(1));
        }

        [Test]
        public void 스테이지별_기록은_서로_독립이고_모든_행이_MaxDifficulty_1_이상이다()
        {
            MetaProfile p = new MetaProfile();
            int[,] sequence = { { 1, 0 }, { 1, 1 }, { 2, 2 }, { 1, 0 }, { 3, 1 }, { 2, 1 }, { 3, 0 } };

            for (int i = 0; i < sequence.GetLength(0); ++i)
            {
                p.RecordDifficultyClear(sequence[i, 0], sequence[i, 1]);
            }

            Assert.AreEqual(1, p.GetMaxClearedDifficulty(1));
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(2));
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(3));
            Assert.AreEqual(3, p.DifficultyClears.Count);
            foreach (DifficultyClearEntry entry in p.DifficultyClears)
            {
                Assert.GreaterOrEqual(entry.MaxDifficulty, 1);
            }
        }

        [Test]
        public void DifficultyClears가_null이어도_Normal_승리가_리스트를_만들어_기록한다()
        {
            MetaProfile p = new MetaProfile { DifficultyClears = null };

            Assert.DoesNotThrow(() => p.RecordDifficultyClear(1, 1));

            Assert.AreEqual(1, p.GetMaxClearedDifficulty(1));
        }

        [Test]
        public void 리스트에_null_엔트리가_섞여_있어도_기록은_예외없이_갱신된다()
        {
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(null);

            Assert.DoesNotThrow(() => p.RecordDifficultyClear(2, 1));

            Assert.AreEqual(1, p.GetMaxClearedDifficulty(2));
        }

        //# --- EndBattle 의 해금 배너 값(newlyUnlockedDifficulty) — BattleController §4.4 순서를 그대로 재현 ---
        //# BattleController.EndBattle 은 MonoBehaviour 라 직접 부를 수 없어, 같은 호출 순서(before → 진행 갱신 → after)를 순수 API 로 재현해 규칙을 고정한다.

        private static int PlayAndGetUnlocked(MetaProfile p, int stage, int effectiveDifficulty, bool win, int maxDifficulty = 2)
        {
            int before = p.MaxSelectableDifficulty(stage, maxDifficulty);
            if (win)
            {
                p.ClearedStage = StageProgress.ResolveClearedStage(p.ClearedStage, stage);
                p.RecordDifficultyClear(stage, effectiveDifficulty);
            }
            int after = p.MaxSelectableDifficulty(stage, maxDifficulty);
            return after > before ? after : 0;
        }

        [Test]
        public void 미클리어_스테이지를_Easy로_이기면_Normal_해금_배너_값_1이다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 0 };

            Assert.AreEqual(1, PlayAndGetUnlocked(p, 1, 0, true));
            Assert.AreEqual(1, p.ClearedStage);
            Assert.AreEqual(0, p.DifficultyClears.Count, "Easy 승리는 행을 만들지 않는다");
        }

        [Test]
        public void Normal_최초_클리어는_Hard_해금_배너_값_2다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 1 };

            Assert.AreEqual(2, PlayAndGetUnlocked(p, 1, 1, true));
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(1));
        }

        [Test]
        public void Hard_클리어와_이미_깬_난이도_재클리어와_패배는_배너_값_0이다()
        {
            MetaProfile hardCleared = new MetaProfile { ClearedStage = 1 };
            hardCleared.RecordDifficultyClear(1, 2);
            MetaProfile normalCleared = new MetaProfile { ClearedStage = 1 };
            normalCleared.RecordDifficultyClear(1, 1);
            MetaProfile easyAgain = new MetaProfile { ClearedStage = 1 };
            MetaProfile unclearedLoss = new MetaProfile { ClearedStage = 0 };

            Assert.AreEqual(0, PlayAndGetUnlocked(hardCleared, 1, 2, true), "Hard 클리어");
            Assert.AreEqual(0, PlayAndGetUnlocked(normalCleared, 1, 1, true), "이미 깬 Normal 재클리어");
            Assert.AreEqual(0, PlayAndGetUnlocked(easyAgain, 1, 0, true), "이미 Normal 이 열려 있는 스테이지의 Easy 재클리어");
            Assert.AreEqual(0, PlayAndGetUnlocked(unclearedLoss, 1, 0, false), "패배");
            Assert.AreEqual(0, unclearedLoss.ClearedStage);
        }

        [Test]
        public void 패배나_낮은_난이도_재클리어는_최고_난이도_기록을_바꾸지_않는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 1 };
            p.RecordDifficultyClear(1, 2);

            PlayAndGetUnlocked(p, 1, 1, false);
            PlayAndGetUnlocked(p, 1, 0, true);
            PlayAndGetUnlocked(p, 1, 1, true);

            Assert.AreEqual(2, p.GetMaxClearedDifficulty(1));
            Assert.AreEqual(1, p.DifficultyClears.Count);
        }

        [Test]
        public void 난이도_표_미로드_상한_0에서는_배너가_뜨지_않는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 0 };

            Assert.AreEqual(0, PlayAndGetUnlocked(p, 1, 0, true, 0));
        }

        //# --- D-6 기존 유저 ---

        [Test]
        public void 기존_클리어_스테이지는_행_없이도_Normal이_열리고_Hard는_잠겨_있다()
        {
            MetaProfile legacy = new MetaProfile { Version = 6, ClearedStage = 5 };

            for (int stage = 1; stage <= 5; ++stage)
            {
                Assert.AreEqual(1, legacy.MaxSelectableDifficulty(stage, 2), $"스테이지 {stage}");
            }
            Assert.AreEqual(0, legacy.MaxSelectableDifficulty(6, 2), "다음 스테이지는 Easy 만");
        }

        //# --- Normal/Hard 판 최단 기록 제외 (§1.6) — MetaProfile.RecordStageRun 범위 ---

        [Test]
        public void recordBest가_false인_승리는_Runs_Wins만_올리고_기존_최단을_유지한다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, true, 100f);

            p.RecordStageRun(1, true, 50f, false);

            StageRecordEntry entry = p.GetStageRecord(1);
            Assert.AreEqual(2, entry.Runs);
            Assert.AreEqual(2, entry.Wins);
            Assert.AreEqual(100f, entry.BestClearTime, 0.0001f);
        }

        [Test]
        public void recordBest가_false인_승리만_있으면_최단은_없음_상태로_남고_이후_Easy_승리가_최단이_된다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(2, true, 80f, false);
            Assert.AreEqual(-1f, p.GetStageRecord(2).BestClearTime);
            Assert.IsTrue(p.IsNewStageBest(2, true, 200f), "Normal/Hard 기록은 최단을 만들지 않으므로 Easy 첫 승리는 신기록");

            p.RecordStageRun(2, true, 200f);
            Assert.AreEqual(200f, p.GetStageRecord(2).BestClearTime, 0.0001f);
            Assert.AreEqual(2, p.GetStageRecord(2).Wins);
        }

        [Test]
        public void recordBest가_false인_패배는_Runs만_올린다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(1, false, 300f, false);

            StageRecordEntry entry = p.GetStageRecord(1);
            Assert.AreEqual(1, entry.Runs);
            Assert.AreEqual(0, entry.Wins);
            Assert.AreEqual(-1f, entry.BestClearTime);
        }

        [Test]
        public void recordBest_기본값은_true라_기존_호출부_동작이_그대로다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(1, true, 90f);
            p.RecordStageRun(1, true, 120f);
            p.RecordStageRun(1, true, 70f);

            Assert.AreEqual(70f, p.GetStageRecord(1).BestClearTime, 0.0001f);
        }

        [Test]
        public void IsNewStageBest는_패배면_항상_false이고_더_빠를_때만_true다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, true, 100f);

            Assert.IsFalse(p.IsNewStageBest(1, false, 1f));
            Assert.IsFalse(p.IsNewStageBest(1, true, 100f));
            Assert.IsTrue(p.IsNewStageBest(1, true, 99.9f));
        }

        [Test]
        public void 전체_BestClearTime은_RecordStageRun이_건드리지_않는다()
        {
            MetaProfile p = new MetaProfile { BestClearTime = 120f };

            p.RecordStageRun(1, true, 30f, false);
            p.RecordStageRun(1, true, 30f);

            Assert.AreEqual(120f, p.BestClearTime, 0.0001f, "프로필 전체 최단은 BattleController 소유(Easy 판에서만 갱신)");
        }
    }
}
