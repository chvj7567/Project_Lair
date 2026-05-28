using NUnit.Framework;
using Lair.Card;
using Lair.Character;
using Lair.Tests.Helpers;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# B3 영웅 액티브 오라 — POCO 검증 가능 항목.
    public class B3ActiveEffectTests
    {
        [Test]
        public void FearAura_OnAttached_FleeMode_ON_OnDetached_OFF()
        {
            GameObject go = new GameObject("hero");
            AutoCombatAI ai = go.AddComponent<AutoCombatAI>();
            FearAura aura = new FearAura(ai);

            aura.OnAttached(new FakeHealth());
            Assert.IsTrue(ai.FleeMode);
            aura.OnDetached(new FakeHealth());
            Assert.IsFalse(ai.FleeMode);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void WeakenAura_PowerScale_절반_적용_후_복원()
        {
            FakeAttacker atk = new FakeAttacker { PowerScale = 1f };
            WeakenAura aura = new WeakenAura(atk, 0.5f);

            aura.OnAttached(new FakeHealth());
            Assert.AreEqual(0.5f, atk.PowerScale, 0.001f);
            aura.OnDetached(new FakeHealth());
            Assert.AreEqual(1f, atk.PowerScale, 0.001f);
        }

        [Test]
        public void TimeStopAura_이동_공격_정지_후_복원()
        {
            FakeMover mover = new FakeMover { Speed = 5f };
            FakeAttacker atk = new FakeAttacker { Enabled = true };
            TimeStopAura aura = new TimeStopAura(mover, atk);

            aura.OnAttached(new FakeHealth());
            Assert.AreEqual(0f, mover.Speed, 0.001f);
            Assert.IsFalse(atk.Enabled);

            aura.OnDetached(new FakeHealth());
            Assert.AreEqual(5f, mover.Speed, 0.001f);
            Assert.IsTrue(atk.Enabled);
        }

        [Test]
        public void BleedAura_이동_중에만_1초마다_데미지()
        {
            FakeMover mover = new FakeMover();
            FakeHealth hp = new FakeHealth();
            hp.SetMax(100);
            BleedAura aura = new BleedAura(mover, 0.02f);
            aura.OnAttached(hp);

            //# 이동 중 — 0.6 + 0.6 = 1.2 → 1회 (Max 100 × 0.02 = 2)
            mover.IsMoving = true;
            aura.Tick(hp, 0.6f);
            Assert.AreEqual(0, hp.DamageCallCount);
            aura.Tick(hp, 0.6f);
            Assert.AreEqual(1, hp.DamageCallCount);
            Assert.AreEqual(2, hp.LastDamage);

            //# 정지 중 — 데미지 없음
            mover.IsMoving = false;
            aura.Tick(hp, 5f);
            Assert.AreEqual(1, hp.DamageCallCount);
        }
    }
}
