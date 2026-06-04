using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class OrbitingBladeSkillTests
    {
        private static IHeroSkillRuntime MakeRuntime()
        {
            OrbitingBladeSkillData data = ScriptableObject.CreateInstance<OrbitingBladeSkillData>();
            TestReflection.SetField(data, "_damage", 20);
            TestReflection.SetField(data, "_hitInterval", 0.3f);
            TestReflection.SetField(data, "_orbitRadius", 1.4f);
            TestReflection.SetField(data, "_bladeSphereRadius", 0.9f);
            TestReflection.SetField(data, "_bladeCount", 3);
            TestReflection.SetField(data, "_rotationSpeedDeg", 180f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_인터벌_미만이면_데미지없음()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 3 };
            rt.Tick(ctx, 0.2f);
            Assert.AreEqual(0, ctx.SpheresCalls.Count);
        }

        [Test]
        public void Tick_인터벌_도달시_구데미지_1회()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 3 };
            rt.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.SpheresCalls.Count);
            Assert.AreEqual(20, ctx.SpheresCalls[0].Amount);
            Assert.AreEqual(0.9f, ctx.SpheresCalls[0].Radius, 0.001f);
            Assert.AreEqual(3, ctx.SpheresCalls[0].Centers.Length, "구 중심 개수 = bladeCount");
            Assert.AreEqual(0f, ctx.SpheresCalls[0].Knockback, 0.001f);
        }

        [Test]
        public void Tick_구중심은_궤도반경상에_위치()
        {
            //# 영웅 원점, 각 구 중심은 궤도반경 1.4 거리 (XZ).
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { HeroPosition = Vector3.zero };
            rt.Tick(ctx, 0.3f);
            foreach (Vector3 c in ctx.SpheresCalls[0].Centers)
            {
                float dist = Mathf.Sqrt(c.x * c.x + c.z * c.z);
                Assert.AreEqual(1.4f, dist, 0.001f, "구 중심은 궤도반경 위");
            }
        }

        [Test]
        public void Tick_긴dt_여러인터벌_누적틱()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 1 };
            rt.Tick(ctx, 0.7f);   //# 0.3 인터벌 → 2회 (0.6 소비, 0.1 잔여)
            Assert.AreEqual(2, ctx.SpheresCalls.Count);
        }
    }
}
