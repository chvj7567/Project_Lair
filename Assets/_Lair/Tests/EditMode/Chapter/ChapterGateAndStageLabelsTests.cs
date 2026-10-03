using Lair.Battle;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# ChapterGate(출격 가능 챕터 단일 소유)와 StageLabels(챕터-인덱스 표기·위협도·잠금 안내) 경계 검증 (chapter-system §6.4).
    //# PlayableChapterCount 는 const 라 테스트에서 올릴 수 없다 — 값에 의존하지 않는 관계식으로 고정한다.
    public class ChapterGateAndStageLabelsTests : ChapterStaticStateTestBase
    {
        //# --- ChapterGate ---

        [Test]
        public void 출격_가능_스테이지_수는_출격_가능_챕터_수_곱하기_5다()
        {
            Assert.AreEqual(ChapterGate.PlayableChapterCount * StageProgress.StagesPerChapter, ChapterGate.PlayableStageCount);
        }

        [Test]
        public void 출격_가능_챕터_수는_1_이상이고_출시_챕터_수를_넘지_않는다()
        {
            Assert.GreaterOrEqual(ChapterGate.PlayableChapterCount, 1);
            Assert.LessOrEqual(ChapterGate.PlayableChapterCount, StageProgress.DefaultChapterCount);
            Assert.LessOrEqual(ChapterGate.PlayableStageCount, StageProgress.MaxStage);
        }

        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(0)]
        public void 챕터_번호가_1_미만이면_출격_불가다(int chapterId)
        {
            Assert.IsFalse(ChapterGate.IsChapterPlayable(chapterId));
        }

        [Test]
        public void 챕터_경계는_1과_PlayableChapterCount는_가능_그_다음과_큰_값은_불가다()
        {
            Assert.IsTrue(ChapterGate.IsChapterPlayable(1));
            Assert.IsTrue(ChapterGate.IsChapterPlayable(ChapterGate.PlayableChapterCount));
            Assert.IsFalse(ChapterGate.IsChapterPlayable(ChapterGate.PlayableChapterCount + 1));
            Assert.IsFalse(ChapterGate.IsChapterPlayable(99));
            Assert.IsFalse(ChapterGate.IsChapterPlayable(int.MaxValue));
        }

        [TestCase(int.MinValue)]
        [TestCase(-5)]
        [TestCase(0)]
        public void 스테이지_번호가_1_미만이면_출격_불가다(int linear)
        {
            Assert.IsFalse(ChapterGate.IsStagePlayable(linear));
        }

        [Test]
        public void 스테이지_경계는_1과_PlayableStageCount는_가능_그_다음과_큰_값은_불가다()
        {
            Assert.IsTrue(ChapterGate.IsStagePlayable(1));
            Assert.IsTrue(ChapterGate.IsStagePlayable(ChapterGate.PlayableStageCount));
            Assert.IsFalse(ChapterGate.IsStagePlayable(ChapterGate.PlayableStageCount + 1));
            Assert.IsFalse(ChapterGate.IsStagePlayable(int.MaxValue));
        }

        [Test]
        public void 스테이지_판정과_챕터_판정은_일관된다()
        {
            for (int linear = -2; linear <= 15; ++linear)
            {
                bool stagePlayable = ChapterGate.IsStagePlayable(linear);
                bool chapterPlayable = linear >= 1 && ChapterGate.IsChapterPlayable(StageProgress.ChapterOf(linear));

                Assert.AreEqual(chapterPlayable, stagePlayable, $"linear {linear}");
            }
        }

        [Test]
        public void 출격_가능_챕터_수는_챕터_수_설정과_독립이다()
        {
            StageProgress.ConfigureChapterCount(5);

            Assert.IsFalse(ChapterGate.IsChapterPlayable(ChapterGate.PlayableChapterCount + 1), "진행 저장 범위가 넓어져도 출격 게이트는 그대로");
        }

        //# --- StageLabels.Number ---

        [TestCase(1, "1-1")]
        [TestCase(2, "1-2")]
        [TestCase(4, "1-4")]
        [TestCase(5, "1-5")]
        [TestCase(6, "2-1")]
        [TestCase(7, "2-2")]
        [TestCase(10, "2-5")]
        [TestCase(11, "3-1")]
        [TestCase(15, "3-5")]
        public void 번호는_챕터_하이픈_인덱스다(int linear, string expected)
        {
            Assert.AreEqual(expected, StageLabels.Number(linear));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void 번호는_1_미만이면_1_1로_취급한다(int linear)
        {
            Assert.AreEqual("1-1", StageLabels.Number(linear));
        }

        //# --- StageLabels.Stars ---

        [TestCase(1, "★☆☆☆☆")]
        [TestCase(2, "★★☆☆☆")]
        [TestCase(3, "★★★☆☆")]
        [TestCase(4, "★★★★☆")]
        [TestCase(5, "★★★★★")]
        public void 위협도_별은_챕터_내_인덱스만큼_채운다(int index, string expected)
        {
            Assert.AreEqual(expected, StageLabels.Stars(index));
        }

        [TestCase(0, "☆☆☆☆☆")]
        [TestCase(-3, "☆☆☆☆☆")]
        [TestCase(6, "★★★★★")]
        [TestCase(10, "★★★★★")]
        [TestCase(int.MaxValue, "★★★★★")]
        public void 위협도_별은_범위_밖이면_0과_5로_clamp한다(int index, string expected)
        {
            Assert.AreEqual(expected, StageLabels.Stars(index));
        }

        [Test]
        public void 위협도_별은_항상_5글자다()
        {
            for (int index = -2; index <= 8; ++index)
            {
                Assert.AreEqual(5, StageLabels.Stars(index).Length, $"index {index}");
            }
        }

        [Test]
        public void 챕터2_스테이지의_별은_선형_번호가_아니라_챕터_내_인덱스_기준이다()
        {
            Assert.AreEqual("★☆☆☆☆", StageLabels.Stars(StageProgress.IndexOf(6)), "2-1");
            Assert.AreEqual("★★★★★", StageLabels.Stars(StageProgress.IndexOf(10)), "2-5");
        }

        //# --- StageLabels.UnlockHint ---

        [Test]
        public void 안내_id_상수는_1313_1314다()
        {
            Assert.AreEqual(1313, StageLabels.HintPreviousStageStringId);
            Assert.AreEqual(1314, StageLabels.HintChapterBossStringId);
        }

        [TestCase(2, "스테이지 1-1 클리어 필요")]
        [TestCase(3, "스테이지 1-2 클리어 필요")]
        [TestCase(5, "스테이지 1-4 클리어 필요")]
        [TestCase(7, "스테이지 2-1 클리어 필요")]
        [TestCase(10, "스테이지 2-4 클리어 필요")]
        [TestCase(12, "스테이지 3-1 클리어 필요")]
        public void 챕터_내_2번째_이후_스테이지는_직전_스테이지_클리어를_안내한다(int linear, string expected)
        {
            Assert.AreEqual(expected, StageLabels.UnlockHint(linear, WorldMapTestSupport.Text));
        }

        [TestCase(6, "1장 보스 클리어 필요")]
        [TestCase(11, "2장 보스 클리어 필요")]
        public void 챕터의_첫_스테이지는_직전_챕터_보스_클리어를_안내한다(int linear, string expected)
        {
            Assert.AreEqual(expected, StageLabels.UnlockHint(linear, WorldMapTestSupport.Text));
        }

        [Test]
        public void 문구_함수가_null이면_빈_문자열이다()
        {
            Assert.AreEqual(string.Empty, StageLabels.UnlockHint(3, null));
            Assert.AreEqual(string.Empty, StageLabels.UnlockHint(6, null));
        }

        [Test]
        public void 문구_함수가_id를_모르면_빈_포맷이라_빈_문자열이다()
        {
            Assert.AreEqual(string.Empty, StageLabels.UnlockHint(3, id => string.Empty));
        }

        [Test]
        public void 안내는_사용한_문구_id를_정확히_조회한다()
        {
            System.Collections.Generic.List<int> asked = new System.Collections.Generic.List<int>();

            StageLabels.UnlockHint(3, id =>
            {
                asked.Add(id);
                return "{0}-{1}";
            });
            StageLabels.UnlockHint(6, id =>
            {
                asked.Add(id);
                return "{0}";
            });

            CollectionAssert.AreEqual(new[] { 1313, 1314 }, asked);
        }
    }
}
