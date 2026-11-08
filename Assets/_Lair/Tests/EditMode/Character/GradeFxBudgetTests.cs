using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# 등급 이펙트 동시 상한(monster-grades §9.3) — 시드: gameplay-programmer. 정상(획득·반납) + 엣지(상한 초과 거부 후 반납하면 다시 허용).
    public class GradeFxBudgetTests
    {
        [SetUp]
        public void SetUp() => GradeFxBudget.Reset();

        [TearDown]
        public void TearDown() => GradeFxBudget.Reset();

        [Test]
        public void 오라를_획득하고_반납하면_카운트가_돌아온다()
        {
            Assert.IsTrue(GradeFxBudget.TryAcquireAura());
            Assert.AreEqual(1, GradeFxBudget.AuraCountForTest);
            GradeFxBudget.ReleaseAura();
            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
        }

        [Test]
        public void 상한을_넘으면_거부하고_반납하면_다시_허용한다()
        {
            for (int i = 0; i < GradeFxBudget.SunburstCap; i++)
            {
                Assert.IsTrue(GradeFxBudget.TryAcquireSunburst());
            }
            Assert.IsFalse(GradeFxBudget.TryAcquireSunburst(), "9번째는 후광 생략");
            GradeFxBudget.ReleaseSunburst();
            Assert.IsTrue(GradeFxBudget.TryAcquireSunburst());
        }
    }
}
