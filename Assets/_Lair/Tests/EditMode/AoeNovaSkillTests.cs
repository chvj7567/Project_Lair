using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class AoeNovaSkillTests
    {
        private static IHeroSkillRuntime MakeRuntime()
        {
            AoeNovaSkillData data = ScriptableObject.CreateInstance<AoeNovaSkillData>();
            TestReflection.SetField(data, "_damage", 80);
            TestReflection.SetField(data, "_cooldown", 4f);
            TestReflection.SetField(data, "_radius", 3f);
            TestReflection.SetField(data, "_knockbackStrength", 3f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_쿨다운_경과전엔_발동안함()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 5 };
            rt.Tick(ctx, 2f);
            Assert.AreEqual(0, ctx.RingCalls.Count);
        }

        [Test]
        public void Tick_쿨다운_경과시_디스크데미지_1회()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 5 };
            rt.Tick(ctx, 4f);
            Assert.AreEqual(1, ctx.RingCalls.Count);
            Assert.AreEqual(0f, ctx.RingCalls[0].Inner, 0.001f);     //# 디스크 = inner 0
            Assert.AreEqual(3f, ctx.RingCalls[0].Outer, 0.001f);
            Assert.AreEqual(80, ctx.RingCalls[0].Amount);
            Assert.AreEqual(3f, ctx.RingCalls[0].Knockback, 0.001f);
        }

        [Test]
        public void Tick_발동후_쿨다운_재충전()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { NextHitCount = 5 };
            rt.Tick(ctx, 4f);
            rt.Tick(ctx, 2f);
            Assert.AreEqual(1, ctx.RingCalls.Count);
            rt.Tick(ctx, 4f);
            Assert.AreEqual(2, ctx.RingCalls.Count);
        }
    }
}
