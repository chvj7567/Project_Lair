using Lair.Battle;
using NUnit.Framework;

namespace Lair.Tests.Battle
{
    //# 카메라 흔들림 도트 양자화 — 기획서 scene-2d-conversion §7.10 · §10.
    public class BattleCameraQuantizeTests
    {
        [Test]
        public void QuantizeToDot_0_3은_14도트다()
        {
            Assert.AreEqual(14f / 48f, BattleCamera.QuantizeToDot(0.3f), 1e-6f);
        }

        [Test]
        public void QuantizeToDot_음수도_도트_배수다()
        {
            Assert.AreEqual(-14f / 48f, BattleCamera.QuantizeToDot(-0.3f), 1e-6f);
        }
    }
}
