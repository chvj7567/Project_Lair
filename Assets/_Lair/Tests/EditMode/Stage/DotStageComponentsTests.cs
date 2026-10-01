using Lair.Character;
using Lair.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode.Stage
{
    //# 도트 무대 공통 컴포넌트 순수 함수 — 기획서 scene-2d-conversion §7.2 · §7.3 · §7.5 · §10.
    public class DotStageComponentsTests
    {
        [Test]
        public void DotWave_사인_합()
        {
            float v = DotWave.Evaluate(0.8f, new[] { 0.07f, 0.04f }, new[] { 9f, 23f }, new[] { 2f, 1f }, 0.5f);
            float expected = 0.8f + 0.07f * Mathf.Sin(9f * 0.5f + 2f) + 0.04f * Mathf.Sin(23f * 0.5f + 1f);
            Assert.AreEqual(expected, v, 1e-5f);
        }

        [Test]
        public void DotWave_항이_없으면_기준값이다()
        {
            Assert.AreEqual(0.55f, DotWave.Evaluate(0.55f, null, null, null, 3f), 1e-6f);
        }

        [Test]
        public void DotOrbitRing_0번_점_위상0은_오른쪽_끝()
        {
            Assert.AreEqual(new Vector2Int(38, 0), DotOrbitRing.DotOffset(0, 16, 0f, 0.5f, 0f, 38f, 10f));
        }

        [Test]
        public void DotOrbitRing_반올림은_0_5_올림()
        {
            //# cos 0 · 2.5 = 2.5 → floor(2.5 + 0.5) = 3
            Assert.AreEqual(3, DotOrbitRing.DotOffset(0, 1, 0f, 0f, 0f, 2.5f, 0f).x);
        }

        [Test]
        public void SpriteSheetFx_시작오프셋이_첫_프레임을_정한다()
        {
            Assert.AreEqual(6, SpriteSheetFrames.StartFrame(0.5f, 12f, 24, true));
            Assert.AreEqual(0, SpriteSheetFrames.StartFrame(0f, 12f, 24, true));
        }

        [Test]
        public void SpriteSheetFx_루프_오프셋이_길이를_넘으면_감긴다()
        {
            Assert.AreEqual(2, SpriteSheetFrames.StartFrame(26f / 12f, 12f, 24, true));
        }
    }
}
