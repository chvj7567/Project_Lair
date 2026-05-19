using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# PauseService 의 중첩 Pause/Resume + ForcePause 동작 검증.
    public class PauseServiceTests
    {
        [SetUp]
        public void Setup() { Time.timeScale = 1f; }

        [TearDown]
        public void TearDown() { Time.timeScale = 1f; }

        [Test]
        public void Pause_시_timeScale_0_Resume_시_1()
        {
            var ps = new PauseService();
            ps.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.IsTrue(ps.IsPaused);

            ps.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void 중첩_Pause_Resume_시_depth_관리()
        {
            var ps = new PauseService();
            ps.Pause();
            ps.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            ps.Resume();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f, "depth 2 → 1, 아직 pause 상태");
            Assert.IsTrue(ps.IsPaused);

            ps.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void ForcePause_즉시_정지_depth_무시()
        {
            var ps = new PauseService();
            ps.ForcePause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.IsTrue(ps.IsPaused);
        }
    }
}
