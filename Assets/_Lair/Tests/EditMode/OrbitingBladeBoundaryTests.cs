using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# [보강] OrbitingBladeRuntime 누적 틱 경계 회귀 (test-engineer).
    //# 기존 OrbitingBladeSkillTests(3) 는 정상/인터벌미만/긴dt 2회만 다룬다.
    //# 본 스위트는 0 dt / 정확 인터벌 경계 / 다중 인터벌 정수 카운트 / 잔여 누적의 경계만 보강한다.
    public class OrbitingBladeBoundaryTests
    {
        //# 인터벌 0.4 — 누적 틱 경계 회귀용 임의값(파라미터 정합은 OrbitingBladeSkillTests 커버). 데미지 적용 횟수만 검증.
        private static IHeroSkillRuntime MakeRuntime()
        {
            OrbitingBladeSkillData data = ScriptableObject.CreateInstance<OrbitingBladeSkillData>();
            TestReflection.SetField(data, "_damage", 15);
            TestReflection.SetField(data, "_hitInterval", 0.4f);
            TestReflection.SetField(data, "_orbitRadius", 1.4f);
            TestReflection.SetField(data, "_bladeSphereRadius", 0.9f);
            TestReflection.SetField(data, "_bladeCount", 3);
            TestReflection.SetField(data, "_rotationSpeedDeg", 180f);
            return data.CreateRuntime();
        }

        [Test]
        public void Tick_0dt_데미지없음()
        {
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext();
            rt.Tick(ctx, 0f);
            Assert.AreEqual(0, ctx.SpheresCalls.Count, "dt 0 이면 누적이 인터벌에 도달하지 못함");
        }

        [Test]
        public void Tick_정확히_인터벌이면_1틱()
        {
            //# 경계 — accum == hitInterval 정확히. while(accum >= interval) 이므로 1틱 발동.
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext();
            rt.Tick(ctx, 0.4f);
            Assert.AreEqual(1, ctx.SpheresCalls.Count);
        }

        [Test]
        public void Tick_매우_긴dt_정수_틱카운트()
        {
            //# 2.05 / 0.4 = 5.125 → 5틱(잔여 0.05). 여러 인터벌 누적이 정수로 떨어지는지.
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext();
            rt.Tick(ctx, 2.05f);
            Assert.AreEqual(5, ctx.SpheresCalls.Count);
        }

        [Test]
        public void Tick_여러번_나눠도_잔여누적되어_틱발동()
        {
            //# 잔여 누적 — 0.3 + 0.3 = 0.6 ≥ 0.4 → 두 번째 Tick 에서 1틱. 0.2 잔여.
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext();
            rt.Tick(ctx, 0.3f);
            Assert.AreEqual(0, ctx.SpheresCalls.Count, "0.3 < 0.4 — 첫 Tick 은 미발동");
            rt.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.SpheresCalls.Count, "0.3+0.3=0.6 ≥ 0.4 — 누적되어 1틱");
        }

        [Test]
        public void Tick_연속_정확인터벌_각1틱_누적없이()
        {
            //# 회귀 — 정확 인터벌 반복 시 잔여가 0 으로 떨어져 매 Tick 정확히 1틱.
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext();
            rt.Tick(ctx, 0.4f);
            rt.Tick(ctx, 0.4f);
            rt.Tick(ctx, 0.4f);
            Assert.AreEqual(3, ctx.SpheresCalls.Count, "정확 인터벌 3회 → 3틱(중복/누락 없음)");
        }

        [Test]
        public void OnDeactivate_인프라_미가용시_예외없음()
        {
            //# EditMode 는 CHMPool/CHMResource 미부팅 → UpdateBlades 가 early-return.
            //# 블레이드가 한 번도 스폰되지 않은 상태에서 OnDeactivate 가 null-safe 한지.
            IHeroSkillRuntime rt = MakeRuntime();
            FakeHeroSkillContext ctx = new FakeHeroSkillContext();
            rt.Tick(ctx, 0.4f);
            Assert.DoesNotThrow(() => rt.OnDeactivate());
            //# 멱등 — 두 번 호출해도 안전.
            Assert.DoesNotThrow(() => rt.OnDeactivate());
        }
    }
}
