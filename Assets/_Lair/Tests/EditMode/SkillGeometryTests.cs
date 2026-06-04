using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class SkillGeometryTests
    {
        [Test]
        public void InRing_링밴드_안이면_true()
        {
            Vector3 center = Vector3.zero;
            //# 반경 2 점 — 밴드 [1.5, 2.5] 안.
            Assert.IsTrue(SkillGeometry.InRing(new Vector3(2f, 0f, 0f), center, 1.5f, 2.5f));
        }

        [Test]
        public void InRing_밴드_안쪽이면_false()
        {
            Assert.IsFalse(SkillGeometry.InRing(new Vector3(1f, 0f, 0f), Vector3.zero, 1.5f, 2.5f));
        }

        [Test]
        public void InRing_Y차이_무시_XZ만판정()
        {
            //# y=10 이어도 XZ 거리 2 면 밴드 안.
            Assert.IsTrue(SkillGeometry.InRing(new Vector3(2f, 10f, 0f), Vector3.zero, 1.5f, 2.5f));
        }

        //# ── InCone (radial 부채꼴) ───────────────────────────────────────

        [Test]
        public void InCone_전방_각도내면_true()
        {
            //# +Z 로 반경 5, 반각 35°. 정면 점 (0,0,3) 은 각도 0° → 안.
            Assert.IsTrue(SkillGeometry.InCone(new Vector3(0f, 0f, 3f), Vector3.zero, Vector3.forward, 5f, 35f));
        }

        [Test]
        public void InCone_각도초과면_false()
        {
            //# +Z 기준 점 (3,0,3) 은 각도 45° > 35° → 밖.
            Assert.IsFalse(SkillGeometry.InCone(new Vector3(3f, 0f, 3f), Vector3.zero, Vector3.forward, 5f, 35f));
        }

        [Test]
        public void InCone_길이초과면_false()
        {
            //# 정면이지만 반경 6 > 5 → 밖 (radial 거리 판정 — 축투영 아님).
            Assert.IsFalse(SkillGeometry.InCone(new Vector3(0f, 0f, 6f), Vector3.zero, Vector3.forward, 5f, 35f));
        }

        [Test]
        public void InCone_뒤쪽이면_false()
        {
            Assert.IsFalse(SkillGeometry.InCone(new Vector3(0f, 0f, -1f), Vector3.zero, Vector3.forward, 5f, 35f));
        }

        [Test]
        public void InCone_Y차이_무시_XZ만판정()
        {
            //# y=10 이어도 XZ 정면 거리 3 → 안.
            Assert.IsTrue(SkillGeometry.InCone(new Vector3(0f, 10f, 3f), Vector3.zero, Vector3.forward, 5f, 35f));
        }

        //# ── InSphere (XZ 거리) ───────────────────────────────────────────

        [Test]
        public void InSphere_반경내면_true()
        {
            Assert.IsTrue(SkillGeometry.InSphere(new Vector3(0.5f, 0f, 0.5f), Vector3.zero, 0.9f));
        }

        [Test]
        public void InSphere_반경밖이면_false()
        {
            Assert.IsFalse(SkillGeometry.InSphere(new Vector3(2f, 0f, 0f), Vector3.zero, 0.9f));
        }

        [Test]
        public void InSphere_경계면_포함()
        {
            //# 정확히 radius 거리 — <= 이므로 포함.
            Assert.IsTrue(SkillGeometry.InSphere(new Vector3(0.9f, 0f, 0f), Vector3.zero, 0.9f));
        }
    }
}
