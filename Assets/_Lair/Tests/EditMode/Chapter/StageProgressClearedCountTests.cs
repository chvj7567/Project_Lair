using Lair.Battle;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 챕터 안 클리어 수 산식 최소 검증 (본격 경계 스위트는 test-engineer).
    public class StageProgressClearedCountTests
    {
        [TestCase(9, 5, 4, 0)]
        [TestCase(10, 5, 5, 0)]
        [TestCase(0, 0, 0, 0)]
        public void 클리어_스테이지별_챕터_안_클리어_수가_맞다(int cleared, int ch1, int ch2, int ch3)
        {
            Assert.AreEqual(ch1, StageProgress.ClearedCountInChapter(1, cleared));
            Assert.AreEqual(ch2, StageProgress.ClearedCountInChapter(2, cleared));
            Assert.AreEqual(ch3, StageProgress.ClearedCountInChapter(3, cleared));
        }

        [Test]
        public void 음수_클리어_스테이지는_0으로_하한_클램프된다()
        {
            Assert.AreEqual(0, StageProgress.ClearedCountInChapter(1, -1));
        }
    }
}
