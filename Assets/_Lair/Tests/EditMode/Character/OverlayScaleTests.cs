using NUnit.Framework;
using Lair.Character;
using Lair.Tests.Helpers;
using UnityEngine;

namespace Lair.Tests.Character
{
    //# B3 오버레이 배율 — Health.DamageTakenScale / MeleeAttacker.PowerScale 곱셈 적용 검증.
    public class OverlayScaleTests
    {
        [Test]
        public void Health_DamageTakenScale_데미지에_곱셈_적용()
        {
            var go = new GameObject("h");
            var h = go.AddComponent<Health>();
            h.SetMax(100);
            h.DamageTakenScale = 0.5f;
            h.TakeDamage(40);   //# 실제 20
            Assert.AreEqual(80, h.Current);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Health_Heal_Max_초과_불가()
        {
            var go = new GameObject("h");
            var h = go.AddComponent<Health>();
            h.SetMax(100);
            h.TakeDamage(30);   //# 70
            h.Heal(999);
            Assert.AreEqual(100, h.Current);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MeleeAttacker_PowerScale_데미지에_곱셈_적용()
        {
            var go = new GameObject("a");
            var atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(2f, 0f, 100);
            atk.PowerScale = 0.5f;
            var target = new FakeHealth();
            target.SetMax(1000);
            atk.TryAttack(target, Vector3.zero, Vector3.zero, 100f);
            Assert.AreEqual(50, target.LastDamage);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MeleeAttacker_OnHit_적중_시_발행()
        {
            var go = new GameObject("a");
            var atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(2f, 0f, 10);
            IHealth hit = null;
            atk.OnHit += t => hit = t;
            var target = new FakeHealth();
            target.SetMax(100);
            atk.TryAttack(target, Vector3.zero, Vector3.zero, 100f);
            Assert.AreSame(target, hit);
            Object.DestroyImmediate(go);
        }
    }
}
