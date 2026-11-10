using ChvjUnityInfra;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 풀링 스크롤뷰 폭 맞춤 산식(PoolingGridFit) - 자동 열 수, 셀 폭 [최소,최대] 범위, 좌우 여백 0.
    public class PoolingGridFitTests
    {
        private const float Gap = 6f;
        private const float MinW = 108f;
        private const float MaxW = 146f;

        [TestCase(720f, 6)]
        [TestCase(1000f, 8)]
        [TestCase(1280f, 11)]
        [TestCase(1920f, 16)]
        [TestCase(2400f, 21)]
        public void 해상도별_열_수와_셀_폭은_범위_안이고_좌우_여백이_0이다(float viewport, int expectedColumns)
        {
            PoolingGridFit.Calc(viewport, 0f, Gap, MinW, MaxW, 4, 0, 0, out int columns, out float width);

            Assert.AreEqual(expectedColumns, columns);
            Assert.GreaterOrEqual(width, MinW);
            Assert.LessOrEqual(width, MaxW);
            Assert.AreEqual(viewport, columns * width + (columns - 1) * Gap, 0.001f, "좌우 여백 0");
        }

        [Test]
        public void 폭이_최대_폭을_넘으면_열을_하나_늘린다()
        {
            //# 최소 폭 기준 2열이면 셀 폭 = (300-6)/2 = 147 > 최대 146 -> 3열(폭 96).
            PoolingGridFit.Calc(300f, 0f, Gap, MinW, MaxW, 1, 0, 0, out int columns, out float width);

            Assert.AreEqual(3, columns);
            Assert.AreEqual(96f, width, 0.001f);
        }

        [Test]
        public void 좁은_뷰포트는_열_수_하한을_지킨다()
        {
            PoolingGridFit.Calc(300f, 0f, Gap, MinW, MaxW, 4, 0, 0, out int columns, out float width);

            Assert.AreEqual(4, columns);
            Assert.AreEqual(300f, columns * width + (columns - 1) * Gap, 0.001f);
        }

        [Test]
        public void 열_수_상한이_있으면_넘지_않고_폭을_늘려_채운다()
        {
            PoolingGridFit.Calc(2400f, 0f, Gap, MinW, MaxW, 4, 8, 0, out int columns, out float width);

            Assert.AreEqual(8, columns);
            Assert.AreEqual(2400f, columns * width + (columns - 1) * Gap, 0.001f);
        }

        [Test]
        public void 열_수_고정이면_규칙을_무시하고_폭만_맞춘다()
        {
            PoolingGridFit.Calc(1000f, 20f, Gap, MinW, MaxW, 4, 0, 6, out int columns, out float width);

            Assert.AreEqual(6, columns);
            Assert.AreEqual(980f, columns * width + (columns - 1) * Gap, 0.001f);
        }
    }
}
