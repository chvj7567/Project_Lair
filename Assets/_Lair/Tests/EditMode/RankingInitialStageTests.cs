using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# RankingPopup 초기 탭 해석 스모크 — 탭 범위 밖(2챕터 6~10)은 전체 랭킹으로 안전하게 연다.
    public class RankingInitialStageTests
    {
        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(5, 5)]
        public void 탭_범위_안_스테이지는_그대로_연다(int initial, int expected)
        {
            Assert.AreEqual(expected, RankingPopup.ResolveInitialStage(initial, 5));
        }

        [TestCase(6)]
        [TestCase(10)]
        public void 탭_범위를_넘는_스테이지는_전체_랭킹으로_연다(int initial)
        {
            Assert.AreEqual(0, RankingPopup.ResolveInitialStage(initial, 5));
        }

        [Test]
        public void 탭_수가_늘어나면_그_범위까지_그대로_연다()
        {
            Assert.AreEqual(8, RankingPopup.ResolveInitialStage(8, 10));
            Assert.AreEqual(0, RankingPopup.ResolveInitialStage(11, 10));
        }

        [Test]
        public void 탭이_없으면_전체_랭킹으로_연다()
        {
            Assert.AreEqual(0, RankingPopup.ResolveInitialStage(3, 0));
        }
    }
}
