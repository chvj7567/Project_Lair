using NUnit.Framework;

namespace Lair.Tests
{
    public class PixelFxPlayerTests
    {
        private static readonly float[] Durations = { 0.1f, 0.2f, 0.1f };

        [Test]
        public void FrameAt_OneShot_AdvancesThenEnds()
        {
            Assert.AreEqual(0, PixelFxPlayer.FrameAt(Durations, 0.05f, false));
            Assert.AreEqual(1, PixelFxPlayer.FrameAt(Durations, 0.25f, false));
            Assert.AreEqual(2, PixelFxPlayer.FrameAt(Durations, 0.35f, false));
            Assert.AreEqual(-1, PixelFxPlayer.FrameAt(Durations, 0.40f, false));
        }

        [Test]
        public void FrameAt_Loop_Wraps()
        {
            Assert.AreEqual(0, PixelFxPlayer.FrameAt(Durations, 0.45f, true));
        }
    }
}
