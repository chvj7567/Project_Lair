using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    //# 도트 시트 프레임 계산 순수 함수 — 기획서 fx-2d-conversion §10. fps 12 / 프레임 = floor(경과 × fps).
    public class SpriteSheetFramesTests
    {
        private const float Fps = 12f;
        private const float Step = 1f / 12f;

        [Test]
        public void LoopFrame_시간0이면_프레임0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(0f, Fps, 16));
        }

        [Test]
        public void LoopFrame_정확히_프레임경계면_다음프레임이다()
        {
            //# 1/12 × 12 = 0.99999994 float 오차 — Epsilon 보정으로 한 칸 밀리지 않아야 한다.
            Assert.AreEqual(1, SpriteSheetFrames.LoopFrame(Step, Fps, 16));
            Assert.AreEqual(5, SpriteSheetFrames.LoopFrame(5f * Step, Fps, 16));
            Assert.AreEqual(15, SpriteSheetFrames.LoopFrame(15f * Step, Fps, 16));
        }

        [Test]
        public void LoopFrame_경계_직전은_이전프레임이다()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(0.05f, Fps, 16));
            Assert.AreEqual(1, SpriteSheetFrames.LoopFrame(0.12f, Fps, 16));
        }

        [Test]
        public void LoopFrame_프레임수만큼_지나면_0으로_wrap한다()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(16f / 12f, Fps, 16));
            Assert.AreEqual(1, SpriteSheetFrames.LoopFrame(17f / 12f, Fps, 16));
        }

        [Test]
        public void LoopFrame_큰_시간도_범위내로_접힌다()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(1000f, Fps, 16));
            Assert.AreEqual(6, SpriteSheetFrames.LoopFrame(1000.5f, Fps, 16));
        }

        [Test]
        public void LoopFrame_모든_시간에서_0이상_프레임수미만이다()
        {
            for (int i = 0; i < 2000; ++i)
            {
                int f = SpriteSheetFrames.LoopFrame(i * 0.0137f, Fps, 24);
                Assert.GreaterOrEqual(f, 0);
                Assert.Less(f, 24);
            }
        }

        [Test]
        public void LoopFrame_음수시간이면_프레임0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(-5f, Fps, 16));
        }

        [Test]
        public void LoopFrame_프레임수0이하_또는_fps0이하는_0을_돌려준다()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(1f, Fps, 0));
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(1f, Fps, -3));
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(1f, 0f, 16));
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(1f, -12f, 16));
        }

        [Test]
        public void LoopFrame_프레임1개면_항상_0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(0f, Fps, 1));
            Assert.AreEqual(0, SpriteSheetFrames.LoopFrame(3.7f, Fps, 1));
        }

        [Test]
        public void OnceFrame_시간0이면_프레임0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.OnceFrame(0f, Fps, 14));
        }

        [Test]
        public void OnceFrame_마지막프레임에서_고정된다()
        {
            Assert.AreEqual(13, SpriteSheetFrames.OnceFrame(13f * Step, Fps, 14));
            Assert.AreEqual(13, SpriteSheetFrames.OnceFrame(14f * Step, Fps, 14));
            Assert.AreEqual(13, SpriteSheetFrames.OnceFrame(999f, Fps, 14));
        }

        [Test]
        public void OnceFrame_음수시간이면_프레임0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.OnceFrame(-1f, Fps, 14));
        }

        [Test]
        public void OnceFrame_프레임수0이하면_0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.OnceFrame(1f, Fps, 0));
            Assert.AreEqual(0, SpriteSheetFrames.OnceFrame(1f, Fps, -1));
        }

        [Test]
        public void OnceFrame_단조_비감소이다()
        {
            int prev = 0;
            for (int i = 0; i < 500; ++i)
            {
                int f = SpriteSheetFrames.OnceFrame(i * 0.01f, Fps, 10);
                Assert.GreaterOrEqual(f, prev);
                prev = f;
            }
        }

        [Test]
        public void IsFinishedOnce_기획서_14프레임_경계()
        {
            Assert.IsFalse(SpriteSheetFrames.IsFinishedOnce(13f / 12f, Fps, 14));
            Assert.IsTrue(SpriteSheetFrames.IsFinishedOnce(14f / 12f, Fps, 14));
        }

        [Test]
        public void IsFinishedOnce_시간0과_음수는_종료아님()
        {
            Assert.IsFalse(SpriteSheetFrames.IsFinishedOnce(0f, Fps, 10));
            Assert.IsFalse(SpriteSheetFrames.IsFinishedOnce(-3f, Fps, 10));
        }

        [Test]
        public void IsFinishedOnce_큰시간은_종료()
        {
            Assert.IsTrue(SpriteSheetFrames.IsFinishedOnce(1000f, Fps, 10));
        }

        [Test]
        public void IsFinishedOnce_프레임수0이면_즉시_종료()
        {
            Assert.IsTrue(SpriteSheetFrames.IsFinishedOnce(0f, Fps, 0));
        }

        [Test]
        public void IsFinishedOnce_fps0이면_영원히_종료안됨()
        {
            Assert.IsFalse(SpriteSheetFrames.IsFinishedOnce(1000f, 0f, 10));
        }

        [Test]
        public void IsFinishedOnce_마지막프레임은_1프레임_분량_보이고_종료한다()
        {
            Assert.AreEqual(9, SpriteSheetFrames.OnceFrame(9f * Step, Fps, 10));
            Assert.IsFalse(SpriteSheetFrames.IsFinishedOnce(9f * Step, Fps, 10));
            Assert.IsTrue(SpriteSheetFrames.IsFinishedOnce(10f * Step, Fps, 10));
        }

        [Test]
        public void PhaseFrame_위상0은_프레임0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(0f, 24));
        }

        [Test]
        public void PhaseFrame_0점999는_마지막프레임()
        {
            Assert.AreEqual(23, SpriteSheetFrames.PhaseFrame(0.999f, 24));
        }

        [Test]
        public void PhaseFrame_위상1은_순환해서_0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(1f, 24));
        }

        [Test]
        public void PhaseFrame_1초과는_frac로_접는다()
        {
            Assert.AreEqual(6, SpriteSheetFrames.PhaseFrame(1.25f, 24));
            Assert.AreEqual(12, SpriteSheetFrames.PhaseFrame(2.5f, 24));
        }

        [Test]
        public void PhaseFrame_음수는_frac로_접는다()
        {
            Assert.AreEqual(18, SpriteSheetFrames.PhaseFrame(-0.25f, 24));
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(-1f, 24));
        }

        [Test]
        public void PhaseFrame_NaN_Infinity는_0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(float.NaN, 24));
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(float.PositiveInfinity, 24));
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(float.NegativeInfinity, 24));
        }

        [Test]
        public void PhaseFrame_프레임수0이하면_0()
        {
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(0.5f, 0));
            Assert.AreEqual(0, SpriteSheetFrames.PhaseFrame(0.5f, -2));
        }

        [Test]
        public void PhaseFrame_모든_입력에서_범위내이다()
        {
            for (int i = -300; i <= 300; ++i)
            {
                int f = SpriteSheetFrames.PhaseFrame(i * 0.0173f, 24);
                Assert.GreaterOrEqual(f, 0);
                Assert.Less(f, 24);
            }
        }

        [Test]
        public void 궤도_공전각도별_프레임은_기획서_표와_일치한다()
        {
            //# 기획서 §10 — 시트는 시계 방향이라 위상 반전(1 − frac). 0→0, 90→18, 180→12, 270→6.
            Assert.AreEqual(0, OrbitFrame(0f));
            Assert.AreEqual(18, OrbitFrame(90f));
            Assert.AreEqual(12, OrbitFrame(180f));
            Assert.AreEqual(6, OrbitFrame(270f));
            Assert.AreEqual(0, OrbitFrame(360f));
        }

        [Test]
        public void 궤도_음수각도도_범위내이며_양의_등가각과_같다()
        {
            int f = OrbitFrame(-90f);
            Assert.GreaterOrEqual(f, 0);
            Assert.Less(f, 24);
            Assert.AreEqual(OrbitFrame(270f), f);
        }

        private static int OrbitFrame(float angleDeg)
        {
            return SpriteSheetFrames.PhaseFrame(1f - OrbitingBladeRuntime.OrbitPhase(angleDeg), 24);
        }
    }
}
