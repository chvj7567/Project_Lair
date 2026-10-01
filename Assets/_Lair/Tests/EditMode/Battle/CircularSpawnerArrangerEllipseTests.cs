using Lair.Battle;
using Lair.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 스포너 타원 배치 — 기획서 scene-2d-conversion §3.5 · §10.
    public class CircularSpawnerArrangerEllipseTests
    {
        private static readonly Vector3 Center = new Vector3(0f, 0f, -2f);
        private static readonly float[] Angles = { 0f, 60f, 120f, 180f, 240f, 300f };
        private static readonly Vector2[] Dots =
        {
            new Vector2(1013.36f, 366.38f), new Vector2(813.68f, 165.29f), new Vector2(414.32f, 165.29f),
            new Vector2(214.64f, 366.38f), new Vector2(414.32f, 567.47f), new Vector2(813.68f, 567.47f),
        };

        [Test]
        public void 반축이_같으면_원과_같다()
        {
            Vector3 e = CircularSpawnerArranger.PositionOnEllipse(Center, 5f, 5f, 37f);
            Vector3 c = CircularSpawnerArranger.PositionOnCircle(Center, 5f, 37f);
            Assert.AreEqual(c.x, e.x, 1e-5f);
            Assert.AreEqual(c.z, e.z, 1e-5f);
        }

        [Test]
        public void 각도_90도는_Z_반축_끝이다()
        {
            Vector3 p = CircularSpawnerArranger.PositionOnEllipse(Vector3.zero, 8.32f, 6.3149f, 90f);
            Assert.AreEqual(0f, p.x, 1e-4f);
            Assert.AreEqual(6.3149f, p.z, 1e-4f);
        }

        [Test]
        public void 타원_배치_스포너_6개_월드()
        {
            Vector3[] expected =
            {
                new Vector3(8.320f, 0f, -2f), new Vector3(4.160f, 0f, 3.469f), new Vector3(-4.160f, 0f, 3.469f),
                new Vector3(-8.320f, 0f, -2f), new Vector3(-4.160f, 0f, -7.469f), new Vector3(4.160f, 0f, -7.469f),
            };
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = CircularSpawnerArranger.PositionOnEllipse(Center, 8.32f, 6.3149f, Angles[i]);
                Assert.AreEqual(expected[i].x, p.x, 0.002f, "x" + i);
                Assert.AreEqual(expected[i].z, p.z, 0.002f, "z" + i);
            }
        }

        [Test]
        public void 타원_배치_스포너_6개_도트()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = CircularSpawnerArranger.PositionOnEllipse(Center, 8.32f, 6.3149f, Angles[i]);
                Vector2 dot = DotStageMapping.BattleGroundToDot(p);
                Assert.AreEqual(Dots[i].x, dot.x, 0.02f, "dx" + i);
                Assert.AreEqual(Dots[i].y, dot.y, 0.02f, "dy" + i);
            }
        }

        [Test]
        public void 제단_상자가_HUD와_겹치지_않는다()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 d = Dots[i];
                Assert.GreaterOrEqual(d.x - 22f, 16.67f, "left" + i);
                Assert.LessOrEqual(d.x + 22f, 1045.2f, "right" + i);
                Assert.GreaterOrEqual(d.y - 29f, 111.2f, "top" + i);
                Assert.LessOrEqual(d.y + 9f, 613.3f, "bottom" + i);
            }
        }

        [Test]
        public void 스포너가_투기장_타원_안이다()
        {
            for (int i = 0; i < 6; i++)
            {
                float nx = (Dots[i].x - 614f) / 624f;
                float ny = (Dots[i].y - 366f) / 258f;
                Assert.Less(nx * nx + ny * ny, 1f, "inside" + i);
            }
        }

        [Test]
        public void Wraith_제단과_영웅_진입점은_46도트_이상_떨어진다()
        {
            Vector2 entry = DotStageMapping.BattleGroundToDot(new Vector3(-7f, 0f, -2f));
            Assert.GreaterOrEqual(entry.x - Dots[3].x, 46f);
        }

        [Test]
        public void Wisp_제단은_펼친_오른쪽_열과_겹치지_않는다()
        {
            Assert.Less(Dots[0].x + 22f, 1045.2f);
        }
    }
}
