using System;
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# 등급 이펙트 동시 상한 엣지(monster-grades §9.3, T10) - 오라 40 · 불꽃 기둥 24 · 후광 8, 종류별 독립, 과다 반납, Reset.
    //# 시드(GradeFxBudgetTests)의 후광 상한·오라 획득/반납과 중복하지 않는다. 정적 상태라 SetUp/TearDown 에서 Reset.
    public class GradeFxBudgetEdgeTests
    {
        [SetUp]
        public void SetUp() => GradeFxBudget.Reset();

        [TearDown]
        public void TearDown() => GradeFxBudget.Reset();

        private static int FillUntilRejected(Func<bool> acquire)
        {
            int n = 0;
            while (acquire() && n < 1000)
            {
                n++;
            }
            return n;
        }

        [Test]
        public void 상한_상수는_기획서_시작값과_같다()
        {
            Assert.AreEqual(40, GradeFxBudget.AuraCap);
            Assert.AreEqual(24, GradeFxBudget.TornadoCap);
            Assert.AreEqual(8, GradeFxBudget.SunburstCap);
        }

        [Test]
        public void 오라는_정확히_상한만큼만_획득된다()
        {
            Assert.AreEqual(40, FillUntilRejected(GradeFxBudget.TryAcquireAura));
            Assert.AreEqual(40, GradeFxBudget.AuraCountForTest);
        }

        [Test]
        public void 기둥은_정확히_상한만큼만_획득되고_반납하면_다시_허용한다()
        {
            Assert.AreEqual(24, FillUntilRejected(GradeFxBudget.TryAcquireTornado));
            Assert.IsFalse(GradeFxBudget.TryAcquireTornado());

            GradeFxBudget.ReleaseTornado();

            Assert.IsTrue(GradeFxBudget.TryAcquireTornado());
            Assert.IsFalse(GradeFxBudget.TryAcquireTornado());
        }

        [Test]
        public void 한_종류가_가득_차도_다른_종류는_영향받지_않는다()
        {
            FillUntilRejected(GradeFxBudget.TryAcquireAura);

            Assert.AreEqual(24, FillUntilRejected(GradeFxBudget.TryAcquireTornado));
            Assert.AreEqual(8, FillUntilRejected(GradeFxBudget.TryAcquireSunburst));
        }

        [Test]
        public void 획득_없이_반납해도_카운트는_음수가_되지_않고_상한이_늘지_않는다()
        {
            for (int i = 0; i < 5; i++)
            {
                GradeFxBudget.ReleaseAura();
                GradeFxBudget.ReleaseTornado();
                GradeFxBudget.ReleaseSunburst();
            }

            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
            Assert.AreEqual(40, FillUntilRejected(GradeFxBudget.TryAcquireAura), "과다 반납이 상한을 넘겨 허용하지 않음");
            Assert.AreEqual(24, FillUntilRejected(GradeFxBudget.TryAcquireTornado));
            Assert.AreEqual(8, FillUntilRejected(GradeFxBudget.TryAcquireSunburst));
        }

        [Test]
        public void Reset_은_세_카운터를_모두_비운다()
        {
            FillUntilRejected(GradeFxBudget.TryAcquireAura);
            FillUntilRejected(GradeFxBudget.TryAcquireTornado);
            FillUntilRejected(GradeFxBudget.TryAcquireSunburst);

            GradeFxBudget.Reset();

            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
            Assert.AreEqual(24, FillUntilRejected(GradeFxBudget.TryAcquireTornado));
            Assert.AreEqual(8, FillUntilRejected(GradeFxBudget.TryAcquireSunburst));
        }

        [Test]
        public void 정예_12마리가_기둥_2개씩_쓰면_13번째_몬스터는_기둥을_못_얻는다()
        {
            for (int monster = 0; monster < 12; monster++)
            {
                Assert.IsTrue(GradeFxBudget.TryAcquireTornado(), $"{monster}번째 기둥 1");
                Assert.IsTrue(GradeFxBudget.TryAcquireTornado(), $"{monster}번째 기둥 2");
            }

            Assert.IsFalse(GradeFxBudget.TryAcquireTornado(), "13번째 몬스터는 기둥 없이 시작(§9.3)");
        }
    }
}
