using Lair.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode.Stage
{
    //# 도트 무대 좌표 매핑 — 기획서 scene-2d-conversion §1.2 · §7.1 · §10.
    public class DotStageMappingTests
    {
        private const float Tol = 0.01f;

        private static void AssertDot(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tol, "dx");
            Assert.AreEqual(expected.y, actual.y, Tol, "dy");
        }

        [Test]
        public void 주시점은_배경_중앙_도트다()
        {
            AssertDot(new Vector2(614f, 352f), DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, -1.60904f)));
        }

        [Test]
        public void BattleZone_중심은_614_366_38이다()
        {
            AssertDot(new Vector2(614f, 366.38f), DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, -2f)));
        }

        [Test]
        public void 구역_모서리_도트()
        {
            AssertDot(new Vector2(1094f, 182.53f), DotStageMapping.BattleGroundToDot(new Vector3(10f, 0f, 3f)));
            AssertDot(new Vector2(134f, 550.23f), DotStageMapping.BattleGroundToDot(new Vector3(-10f, 0f, -7f)));
        }

        [Test]
        public void 도트_지면_왕복_변환이_보존된다()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 w = new Vector3(-9f + i * 2.1f, 0f, -6.5f + i * 1.13f);
                Vector3 back = DotStageMapping.BattleDotToGround(DotStageMapping.BattleGroundToDot(w));
                Assert.AreEqual(w.x, back.x, 1e-4f);
                Assert.AreEqual(w.z, back.z, 1e-4f);
            }
        }

        [Test]
        public void HeroAnchor는_홀로그램_발밑이다()
        {
            AssertDot(new Vector2(240f, 221f), DotStageMapping.VillageGroundToDot(Vector3.zero));
        }

        [Test]
        public void 화면_중앙_도트의_지면점은_Z_6_5다()
        {
            Assert.AreEqual(6.50008f, DotStageMapping.VillageDotToGround(new Vector2(240f, 135f)).z, 1e-3f);
        }

        [Test]
        public void 배경_중앙_도트는_로컬_원점이다()
        {
            Assert.AreEqual(Vector2.zero, DotStageMapping.StageLocalFromDot(new Vector2(614f, 352f), DotStageMapping.BattleScreenCenterDot));
            Assert.AreEqual(Vector2.zero, DotStageMapping.StageLocalFromDot(new Vector2(240f, 135f), DotStageMapping.VillageScreenCenterDot));
        }

        [Test]
        public void 배틀_화로_좌상_로컬()
        {
            Vector2 local = DotStageMapping.StageLocalFromDot(new Vector2(193f, 205f), DotStageMapping.BattleScreenCenterDot);
            Assert.AreEqual(-8.7708f, local.x, 1e-3f);
            Assert.AreEqual(3.0625f, local.y, 1e-3f);
        }
    }
}
