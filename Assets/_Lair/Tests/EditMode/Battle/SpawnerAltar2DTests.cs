using Lair.Battle;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 스포너 제단 결정 흔들림 — 기획서 scene-2d-conversion §3.2 · §7.4 · §10.
    public class SpawnerAltar2DTests
    {
        [Test]
        public void CrystalBobDots_사인_최대면_2도트_아래()
        {
            //# sin(2.4t + φ) = 1 → floor(1.5 + 0.5) = 2
            float t = (Mathf.PI / 2f) / 2.4f;
            Assert.AreEqual(2, SpawnerAltar2D.CrystalBobDots(t, 0f));
        }

        [Test]
        public void CrystalBobDots_범위는_마이너스2부터_2()
        {
            for (int i = 0; i < 200; i++)
            {
                int v = SpawnerAltar2D.CrystalBobDots(i * 0.037f, i * 0.31f);
                Assert.IsTrue(v >= -2 && v <= 2, $"bob {v}");
            }
        }
    }
}
