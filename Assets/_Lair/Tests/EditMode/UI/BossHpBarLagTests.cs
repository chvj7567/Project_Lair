using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.UI
{
    //# 보스 바 피해 잔상 — 기획서 scene-2d-conversion §4.3.1 · §10.
    public class BossHpBarLagTests
    {
        [Test]
        public void 잔상은_0_7초_뒤_목표에_도달한다()
        {
            Assert.AreEqual(0.4f, BossHpBarView.EvaluateLag(0.8f, 0.4f, 0.7f, 0.7f), 1e-5f);
            Assert.AreEqual(0.4f, BossHpBarView.EvaluateLag(0.8f, 0.4f, 5f, 0.7f), 1e-5f);
        }

        [Test]
        public void 절반_시간에_중간값이다()
        {
            Assert.AreEqual(0.6f, BossHpBarView.EvaluateLag(0.8f, 0.4f, 0.35f, 0.7f), 1e-5f);
        }

        [Test]
        public void 회복이면_즉시_목표다()
        {
            Assert.AreEqual(0.9f, BossHpBarView.EvaluateLag(0.5f, 0.9f, 0f, 0.7f), 1e-5f);
        }

        [Test]
        public void 시간_0이면_시작값이다()
        {
            Assert.AreEqual(0.8f, BossHpBarView.EvaluateLag(0.8f, 0.4f, 0f, 0.7f), 1e-5f);
        }
    }
}
