using NUnit.Framework;
using Lair.Battle;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class LetterboxFramerTests
    {
        private const float Aspect = 16f / 9f;

        [Test]
        public void ComputeViewportRect_정확히_16대9면_풀스크린()
        {
            //# 1280×720 = 16:9 → viewport 전체 (0,0,1,1).
            Rect rect = LetterboxFramer.ComputeViewportRect(1280, 720, Aspect);
            Assert.AreEqual(0f, rect.x, 1e-4f);
            Assert.AreEqual(0f, rect.y, 1e-4f);
            Assert.AreEqual(1f, rect.width, 1e-4f);
            Assert.AreEqual(1f, rect.height, 1e-4f);
        }

        [Test]
        public void ComputeViewportRect_가로가_더_길면_좌우_필러박스()
        {
            //# 2400×1080 (가로 긴 폰) → 좌우에 검은 띠, 박스는 가로 중앙.
            Rect rect = LetterboxFramer.ComputeViewportRect(2400, 1080, Aspect);
            //# 16:9 박스 폭 = 1080 * 16/9 = 1920 → 화면폭 2400 의 0.8.
            Assert.AreEqual(0.8f, rect.width, 1e-3f);
            Assert.AreEqual(1f, rect.height, 1e-4f);
            //# 중앙 정렬 → x = (1 - 0.8)/2 = 0.1.
            Assert.AreEqual(0.1f, rect.x, 1e-3f);
            Assert.AreEqual(0f, rect.y, 1e-4f);
        }

        [Test]
        public void ComputeViewportRect_세로가_더_길면_상하_레터박스()
        {
            //# 720×1280 (세로 긴) → 상하에 검은 띠, 박스는 세로 중앙.
            Rect rect = LetterboxFramer.ComputeViewportRect(720, 1280, Aspect);
            //# 16:9 박스 높이 = 720 * 9/16 = 405 → 화면높이 1280 의 0.3164.
            Assert.AreEqual(1f, rect.width, 1e-4f);
            Assert.AreEqual(405f / 1280f, rect.height, 1e-3f);
            Assert.AreEqual(0f, rect.x, 1e-4f);
            Assert.AreEqual((1f - 405f / 1280f) * 0.5f, rect.y, 1e-3f);
        }

        [Test]
        public void ComputeViewportRect_0크기_입력이면_풀스크린_폴백()
        {
            //# 엣지 — 화면 크기 0 (초기화 전 등) 이면 분모 0 회피해 안전한 전체 rect 반환.
            Rect rect = LetterboxFramer.ComputeViewportRect(0, 0, Aspect);
            Assert.AreEqual(0f, rect.x, 1e-4f);
            Assert.AreEqual(0f, rect.y, 1e-4f);
            Assert.AreEqual(1f, rect.width, 1e-4f);
            Assert.AreEqual(1f, rect.height, 1e-4f);
        }

        private const float MaxAspect = 21f / 9f;

        private static void AssertRect(Rect r, float x, float y, float w, float h)
        {
            Assert.AreEqual(x, r.x, 1e-4f);
            Assert.AreEqual(y, r.y, 1e-4f);
            Assert.AreEqual(w, r.width, 1e-4f);
            Assert.AreEqual(h, r.height, 1e-4f);
        }

        [TestCase(1920, 1080)]
        [TestCase(2400, 1080)]
        [TestCase(2520, 1080)]
        public void 클램프_범위_안_종횡비는_풀_rect(int w, int h)
        {
            //# 2520x1080 = 정확히 21:9 경계 포함.
            AssertRect(LetterboxFramer.ComputeViewportRect(w, h, Aspect, MaxAspect), 0f, 0f, 1f, 1f);
        }

        [Test]
        public void 클램프_최대_종횡비_초과면_좌우_띠()
        {
            //# 2640x1080 (22:9 초과) → 21:9 박스 폭 0.95455.
            AssertRect(LetterboxFramer.ComputeViewportRect(2640, 1080, Aspect, MaxAspect), 0.02273f, 0f, 0.95455f, 1f);
        }

        [Test]
        public void 클램프_16대10은_위아래_띠()
        {
            AssertRect(LetterboxFramer.ComputeViewportRect(1920, 1200, Aspect, MaxAspect), 0f, 0.05f, 1f, 0.9f);
        }

        [Test]
        public void 클램프_4대3은_위아래_띠()
        {
            AssertRect(LetterboxFramer.ComputeViewportRect(2048, 1536, Aspect, MaxAspect), 0f, 0.125f, 1f, 0.75f);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void 최대_종횡비_0이하면_3인자와_동일(float max)
        {
            //# 회귀 — 2400x1080 현행 필러박스 x 0.1, w 0.8.
            Rect r = LetterboxFramer.ComputeViewportRect(2400, 1080, Aspect, max);
            AssertRect(r, 0.1f, 0f, 0.8f, 1f);
            Rect legacy = LetterboxFramer.ComputeViewportRect(2400, 1080, Aspect);
            Assert.AreEqual(legacy, r);
        }
    }
}
