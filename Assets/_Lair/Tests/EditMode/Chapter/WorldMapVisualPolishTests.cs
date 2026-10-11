using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 침입 지도 비주얼 폴리시 최소 검증(worldmap-popup-visual-polish §5·§4.1) — 팔레트 신규 조회 + UILoopMotion 곡선. 본격 스위트는 test-engineer.
    public class WorldMapVisualPolishTests
    {
        private const float Tolerance = 0.002f;

        [Test]
        public void 챕터1_열림_노드_틴트가_기획서_표_값과_같다()
        {
            Color tint = WorldMapChapterPalette.NodeOpenTint(1);

            Assert.AreEqual(0.503f, tint.r, Tolerance);
            Assert.AreEqual(0.605f, tint.g, Tolerance);
            Assert.AreEqual(0.753f, tint.b, Tolerance);
            Assert.AreEqual(1f, tint.a, Tolerance);
        }

        [Test]
        public void 챕터4_이상은_중립색_기반으로_글로우_알파와_실루엣_알파가_계산된다()
        {
            Assert.AreEqual(WorldMapChapterPalette.NodeGlowAlpha, WorldMapChapterPalette.NodeGlow(4).a, Tolerance);
            Assert.AreEqual(WorldMapChapterPalette.SilhouetteAlpha, WorldMapChapterPalette.Silhouette(4).a, Tolerance);
            Assert.AreEqual(0x0d / 255f * WorldMapChapterPalette.SilhouetteDarken, WorldMapChapterPalette.Silhouette(4).r, 0.0001f);
        }

        [Test]
        public void 루프_모션_곡선이_기획서_기준_시점_값을_낸다()
        {
            Assert.AreEqual(1f, UILoopMotion.DriftFactor(14f, 14f), 0.0001f);
            Assert.AreEqual(0f, UILoopMotion.DriftFactor(28f, 14f), 0.0001f);
            Assert.AreEqual(-90f, UILoopMotion.RotateAngle(8f, -11.25f), 0.0001f);
            Assert.AreEqual(0f, UILoopMotion.NudgeFactor(0.49f, 1f));
            Assert.AreEqual(1f, UILoopMotion.NudgeFactor(0.51f, 1f));
            Assert.AreEqual(0f, UILoopMotion.PulseFactor(0f, 2f), 0.0001f);
            Assert.AreEqual(1f, UILoopMotion.PulseFactor(1f, 2f), 0.0001f);
            Assert.AreEqual(0f, UILoopMotion.PulseFactor(2f, 2f), 0.0001f);
        }
    }
}
