using Lair.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode.Stage
{
    //# 도트 무대 좌표 매핑 엣지·왕복·씬별 기대값 — 기획서 scene-2d-conversion §1.2 · §10 (기본 케이스는 DotStageMappingTests).
    public class DotStageMappingEdgeTests
    {
        private const float Tol = 0.01f;

        [Test]
        public void 영웅_진입점_도트는_278_366_38이다()
        {
            Vector2 d = DotStageMapping.BattleGroundToDot(new Vector3(-7f, 0f, -2f));
            Assert.AreEqual(278f, d.x, Tol);
            Assert.AreEqual(366.38f, d.y, Tol);
        }

        [Test]
        public void 높이_Y가_오르면_화면_위로_cos50_배율만큼_올라간다()
        {
            Vector2 ground = DotStageMapping.BattleGroundToDot(new Vector3(1f, 0f, 0f));
            Vector2 up = DotStageMapping.BattleGroundToDot(new Vector3(1f, 2f, 0f));
            Assert.AreEqual(ground.x, up.x, 1e-4f);
            Assert.AreEqual(ground.y - 2f * 48f * Mathf.Cos(50f * Mathf.Deg2Rad), up.y, 1e-3f);
        }

        [Test]
        public void Z가_커지면_화면_위로_올라간다()
        {
            Vector2 near = DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, -5f));
            Vector2 far = DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, 5f));
            Assert.Less(far.y, near.y);
        }

        [Test]
        public void 지면_Z_1u는_화면_36_77도트다()
        {
            Vector2 a = DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, 0f));
            Vector2 b = DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, 1f));
            Assert.AreEqual(36.77013f, a.y - b.y, 1e-3f);
        }

        [Test]
        public void 지면_X_1u는_화면_48도트다()
        {
            Vector2 a = DotStageMapping.BattleGroundToDot(new Vector3(0f, 0f, 0f));
            Vector2 b = DotStageMapping.BattleGroundToDot(new Vector3(1f, 0f, 0f));
            Assert.AreEqual(48f, b.x - a.x, 1e-4f);
        }

        [Test]
        public void 배틀_역변환_결과는_항상_지면_Y_0이다()
        {
            Assert.AreEqual(0f, DotStageMapping.BattleDotToGround(new Vector2(123f, 456f)).y);
        }

        [Test]
        public void 마을_왕복_변환이_보존된다()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 w = new Vector3(-5f + i * 1.1f, 0f, -3f + i * 0.9f);
                Vector3 back = DotStageMapping.VillageDotToGround(DotStageMapping.VillageGroundToDot(w));
                Assert.AreEqual(w.x, back.x, 1e-4f);
                Assert.AreEqual(w.z, back.z, 1e-4f);
            }
        }

        [Test]
        public void 마을_높이_Y는_화면_cos16_배율로_올라간다()
        {
            Vector2 ground = DotStageMapping.VillageGroundToDot(Vector3.zero);
            Vector2 up = DotStageMapping.VillageGroundToDot(new Vector3(0f, 1f, 0f));
            Assert.AreEqual(48f * Mathf.Cos(16f * Mathf.Deg2Rad), ground.y - up.y, 1e-3f);
        }

        [Test]
        public void 화면_중앙_상수는_기획서_값이다()
        {
            Assert.AreEqual(new Vector2(614f, 352f), DotStageMapping.BattleScreenCenterDot);
            Assert.AreEqual(new Vector2(240f, 135f), DotStageMapping.VillageScreenCenterDot);
            Assert.AreEqual(new Vector2(240f, 135f), DotStageMapping.LoadingScreenCenterDot);
            Assert.AreEqual(48f, DotStageMapping.Ppu);
            Assert.AreEqual(-1.60904f, DotStageMapping.BattleLookZ, 1e-6f);
        }

        [Test]
        public void StageLocalFromDot_화면_아래는_로컬_음수_y다()
        {
            Vector2 local = DotStageMapping.StageLocalFromDot(new Vector2(614f, 352f + 48f), DotStageMapping.BattleScreenCenterDot);
            Assert.AreEqual(0f, local.x, 1e-6f);
            Assert.AreEqual(-1f, local.y, 1e-6f);
        }

        [Test]
        public void StageLocalFromDot_오른쪽은_로컬_양수_x다()
        {
            Vector2 local = DotStageMapping.StageLocalFromDot(new Vector2(240f + 96f, 135f), DotStageMapping.LoadingScreenCenterDot);
            Assert.AreEqual(2f, local.x, 1e-6f);
            Assert.AreEqual(0f, local.y, 1e-6f);
        }

        [Test]
        public void 마을_배경_좌상단_도트는_로컬_좌상단_모서리다()
        {
            //# 480×270 배경의 (0,0) 도트 → (−5, 2.8125)
            Vector2 local = DotStageMapping.StageLocalFromDot(Vector2.zero, DotStageMapping.VillageScreenCenterDot);
            Assert.AreEqual(-5f, local.x, 1e-5f);
            Assert.AreEqual(2.8125f, local.y, 1e-5f);
        }
    }
}
