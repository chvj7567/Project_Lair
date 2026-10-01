using Lair.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode.Stage
{
    //# 배경이 화면을 덮도록 직교 카메라 크기를 맞추는 순수 함수 — 마을·로딩 와이드 화면 대응.
    public class DotStageCoverFitTests
    {
        private static readonly Vector2 Ref = new Vector2(480f, 270f);

        [TestCase(16f / 9f, 2.8125f)]
        [TestCase(2f, 2.5f)]
        [TestCase(21f / 9f, 2.142857f)]
        [TestCase(4f / 3f, 2.8125f)]
        public void 종횡비별_덮는_직교_크기(float aspect, float expected)
        {
            Assert.AreEqual(expected, DotStageCoverFit.OrthoSizeForCover(Ref, aspect, 48f), 1e-4f);
        }

        [Test]
        public void 종횡비가_0이하면_기준_크기다()
        {
            Assert.AreEqual(2.8125f, DotStageCoverFit.OrthoSizeForCover(Ref, 0f, 48f), 1e-4f);
        }

        [Test]
        public void 덮는_크기는_보이는_영역이_배경_안이다()
        {
            foreach (float aspect in new[] { 1.2f, 1.5f, 16f / 9f, 2f, 2.4f })
            {
                float size = DotStageCoverFit.OrthoSizeForCover(Ref, aspect, 48f);
                float w = 2f * size * aspect * 48f;
                float h = 2f * size * 48f;
                Assert.LessOrEqual(w, 480.01f, "w " + aspect);
                Assert.LessOrEqual(h, 270.01f, "h " + aspect);
            }
        }
    }
}
