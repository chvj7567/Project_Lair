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
    }
}
