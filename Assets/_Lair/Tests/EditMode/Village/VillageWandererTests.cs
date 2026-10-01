using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Village
{
    //# 마을 배회자 순수 함수 — 기획서 scene-2d-conversion §5.6 · §7.7 · §10.
    public class VillageWandererTests
    {
        [Test]
        public void 개수_1이면_제자리다()
        {
            Assert.AreEqual(0, VillageWanderer.NextWaypointIndex(0, 1, 5));
        }

        [Test]
        public void 다음_경유점은_현재_다음부터_랜덤_오프셋이다()
        {
            //# (현재 + 1 + 오프셋) mod 개수 — 오프셋 [0, 개수−2]
            Assert.AreEqual(1, VillageWanderer.NextWaypointIndex(0, 6, 0));
            Assert.AreEqual(5, VillageWanderer.NextWaypointIndex(0, 6, 4));
            Assert.AreEqual(0, VillageWanderer.NextWaypointIndex(5, 6, 0));
        }

        [Test]
        public void 개수_2면_항상_다른_점이다()
        {
            Assert.AreEqual(1, VillageWanderer.NextWaypointIndex(0, 2, 0));
            Assert.AreEqual(0, VillageWanderer.NextWaypointIndex(1, 2, 0));
        }

        [Test]
        public void StepToward는_속도_곱_dt만큼_전진한다()
        {
            (Vector2 pos, bool arrived) = VillageWanderer.StepToward(new Vector2(0f, 0f), new Vector2(100f, 0f), 26f, 0.5f);
            Assert.AreEqual(13f, pos.x, 1e-4f);
            Assert.IsFalse(arrived);
        }

        [Test]
        public void StepToward는_목표를_넘지_않는다()
        {
            (Vector2 pos, _) = VillageWanderer.StepToward(new Vector2(0f, 0f), new Vector2(3f, 4f), 100f, 1f);
            Assert.AreEqual(3f, pos.x, 1e-4f);
            Assert.AreEqual(4f, pos.y, 1e-4f);
        }

        [Test]
        public void 도착_판정은_1도트_미만이다()
        {
            Assert.IsTrue(VillageWanderer.StepToward(new Vector2(9.5f, 0f), new Vector2(10f, 0f), 26f, 0.1f).arrived);
            Assert.IsFalse(VillageWanderer.StepToward(new Vector2(8.9f, 0f), new Vector2(10f, 0f), 26f, 0.1f).arrived);
        }
    }
}
