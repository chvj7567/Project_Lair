using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    public class MeleeAttackerTests
    {
        private static MeleeAttacker NewAttacker(float range = 1.5f, float cd = 1.0f, int power = 50)
        {
            MeleeAttacker a = new MeleeAttacker();
            a.Configure(range, cd, power);
            return a;
        }

        [Test]
        public void 사거리_밖_TryAttack_거부()
        {
            MeleeAttacker atk = NewAttacker(range: 1.0f);
            FakeHealth target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(5, 0, 0), now: 0f);

            Assert.IsFalse(hit);
            Assert.AreEqual(0, target.DamageCallCount);
        }

        [Test]
        public void 사거리_내_쿨_0_데미지_Power_만큼_적용()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            Assert.IsTrue(hit);
            Assert.AreEqual(1, target.DamageCallCount);
            Assert.AreEqual(50, target.LastDamage);
        }

        [Test]
        public void 쿨다운_중_재시도_거부()
        {
            MeleeAttacker atk = NewAttacker(cd: 1.0f);
            FakeHealth target = new FakeHealth();
            atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0f);

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0.5f);

            Assert.IsFalse(hit);
            Assert.AreEqual(1, target.DamageCallCount);
        }

        [Test]
        public void 쿨다운_경과_후_재공격_가능()
        {
            MeleeAttacker atk = NewAttacker(cd: 1.0f);
            FakeHealth target = new FakeHealth();
            atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0f);

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 1.5f);

            Assert.IsTrue(hit);
            Assert.AreEqual(2, target.DamageCallCount);
        }
    }
}
