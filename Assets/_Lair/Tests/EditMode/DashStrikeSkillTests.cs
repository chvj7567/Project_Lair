using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class DashStrikeSkillTests
    {
        //# 테스트용 SO + 런타임 생성 (필드는 reflection 으로 주입).
        private static IHeroSkillRuntime MakeRuntime(out DashStrikeSkillData data)
        {
            data = ScriptableObject.CreateInstance<DashStrikeSkillData>();
            TestReflection.SetField(data, "_damage", 100);
            TestReflection.SetField(data, "_cooldown", 2f);
            TestReflection.SetField(data, "_dashLength", 6f);
            TestReflection.SetField(data, "_coneHalfAngle", 35f);
            TestReflection.SetField(data, "_knockbackStrength", 2f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_쿨다운_경과전엔_발동안함()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { CentroidResult = new Vector3(0, 0, 5) };
            rt.Tick(ctx, 1.0f);   //# cooldown 2 미만
            Assert.AreEqual(0, ctx.ConeCalls.Count);
        }

        [Test]
        public void Tick_쿨다운_경과시_부채꼴데미지_1회()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { CentroidResult = new Vector3(0, 0, 5) };
            rt.Tick(ctx, 2.0f);
            Assert.AreEqual(1, ctx.ConeCalls.Count);
            Assert.AreEqual(100, ctx.ConeCalls[0].Amount);
            Assert.AreEqual(6f, ctx.ConeCalls[0].Length, 0.001f);
            Assert.AreEqual(35f, ctx.ConeCalls[0].HalfAngleDeg, 0.001f);
            Assert.AreEqual(2f, ctx.ConeCalls[0].Knockback, 0.001f);
        }

        [Test]
        public void Tick_방향은_영웅에서_무게중심쪽()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext
            {
                HeroPosition = Vector3.zero,
                CentroidResult = new Vector3(0, 0, 5)
            };
            rt.Tick(ctx, 2.0f);
            Vector3 dir = ctx.ConeCalls[0].Dir.normalized;
            Assert.AreEqual(0f, dir.x, 0.01f);
            Assert.AreEqual(1f, dir.z, 0.01f);
        }

        [Test]
        public void Tick_몬스터없으면_발동보류()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            //# centroid == heroPosition → 방향 0 → 발동 안 함.
            FakeHeroSkillContext ctx = new FakeHeroSkillContext
            {
                HeroPosition = Vector3.zero,
                CentroidResult = Vector3.zero
            };
            rt.Tick(ctx, 2.0f);
            Assert.AreEqual(0, ctx.ConeCalls.Count);
        }

        [Test]
        public void Tick_발동후_쿨다운_재충전()
        {
            IHeroSkillRuntime rt = MakeRuntime(out _);
            FakeHeroSkillContext ctx = new FakeHeroSkillContext { CentroidResult = new Vector3(0, 0, 5) };
            rt.Tick(ctx, 2.0f);   //# 1회 발동
            rt.Tick(ctx, 1.0f);   //# 쿨다운 미경과
            Assert.AreEqual(1, ctx.ConeCalls.Count);
            rt.Tick(ctx, 2.0f);   //# 다시 경과
            Assert.AreEqual(2, ctx.ConeCalls.Count);
        }
    }
}
