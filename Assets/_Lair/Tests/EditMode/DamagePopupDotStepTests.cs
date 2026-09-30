using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    //# DamagePopup 도트 연출 순수 함수 — 기획서 fx-2d-conversion §6.3/§10. 12fps 계단 + 4단 알파.
    public class DamagePopupDotStepTests
    {
        [Test]
        public void StepIndex_0이하는_0()
        {
            Assert.AreEqual(0, DamagePopup.StepIndex(0f));
            Assert.AreEqual(0, DamagePopup.StepIndex(-1f));
        }

        [Test]
        public void StepIndex_12fps_경계에서_다음_단계()
        {
            Assert.AreEqual(0, DamagePopup.StepIndex(0.05f));
            Assert.AreEqual(1, DamagePopup.StepIndex(1f / 12f));
            Assert.AreEqual(4, DamagePopup.StepIndex(4f / 12f));
            Assert.AreEqual(12, DamagePopup.StepIndex(1f));
        }

        [Test]
        public void StepIndex_기본수명_0점7초는_8단계()
        {
            Assert.AreEqual(8, DamagePopup.StepIndex(0.7f));
        }

        [Test]
        public void StepIndex_단조_비감소()
        {
            int prev = 0;
            for (int i = 0; i < 300; ++i)
            {
                int n = DamagePopup.StepIndex(i * 0.01f);
                Assert.GreaterOrEqual(n, prev);
                prev = n;
            }
        }

        [Test]
        public void RiseRatio_0단계는_0이고_7단계부터_1이다()
        {
            Assert.AreEqual(0f, DamagePopup.RiseRatio(0), 1e-6f);
            Assert.AreEqual(1f, DamagePopup.RiseRatio(7), 1e-6f);
            Assert.AreEqual(1f, DamagePopup.RiseRatio(8), 1e-6f);
            Assert.AreEqual(1f, DamagePopup.RiseRatio(1000), 1e-6f);
        }

        [Test]
        public void RiseRatio_음수단계는_0()
        {
            Assert.AreEqual(0f, DamagePopup.RiseRatio(-5), 1e-6f);
        }

        [Test]
        public void RiseRatio_7단계에서_부상거리는_1점2유닛()
        {
            Assert.AreEqual(1.2f, 1.2f * DamagePopup.RiseRatio(7), 1e-5f);
        }

        [Test]
        public void RiseRatio_0에서_7까지_단조_증가하고_ease_out이다()
        {
            float prev = DamagePopup.RiseRatio(0);
            float prevDelta = float.MaxValue;
            for (int n = 1; n <= 7; ++n)
            {
                float cur = DamagePopup.RiseRatio(n);
                float delta = cur - prev;
                Assert.Greater(delta, 0f, $"n={n} 증가 없음");
                Assert.LessOrEqual(delta, prevDelta + 1e-6f, $"n={n} 감속 아님");
                prev = cur;
                prevDelta = delta;
            }
        }

        [Test]
        public void StepAlpha_4단계까지는_1()
        {
            for (int n = 0; n <= 4; ++n)
            {
                Assert.AreEqual(1f, DamagePopup.StepAlpha(n), 1e-6f, $"n={n}");
            }
        }

        [Test]
        public void StepAlpha_5_6_7단계는_4단_계단값()
        {
            Assert.AreEqual(0.75f, DamagePopup.StepAlpha(5), 1e-6f);
            Assert.AreEqual(0.5f, DamagePopup.StepAlpha(6), 1e-6f);
            Assert.AreEqual(0.25f, DamagePopup.StepAlpha(7), 1e-6f);
        }

        [Test]
        public void StepAlpha_8단계_이상은_0()
        {
            Assert.AreEqual(0f, DamagePopup.StepAlpha(8), 1e-6f);
            Assert.AreEqual(0f, DamagePopup.StepAlpha(500), 1e-6f);
        }

        [Test]
        public void StepAlpha_음수단계는_1()
        {
            Assert.AreEqual(1f, DamagePopup.StepAlpha(-3), 1e-6f);
        }

        [Test]
        public void StepAlpha_전구간_단조_비증가이고_0에서_1범위()
        {
            float prev = 1f;
            for (int n = -2; n <= 12; ++n)
            {
                float a = DamagePopup.StepAlpha(n);
                Assert.LessOrEqual(a, prev + 1e-6f, $"n={n}");
                Assert.GreaterOrEqual(a, 0f);
                Assert.LessOrEqual(a, 1f);
                prev = a;
            }
        }

        [Test]
        public void 수명_끝_시각의_알파는_0이다()
        {
            Assert.AreEqual(0f, DamagePopup.StepAlpha(DamagePopup.StepIndex(0.7f)), 1e-6f);
        }
    }
}
