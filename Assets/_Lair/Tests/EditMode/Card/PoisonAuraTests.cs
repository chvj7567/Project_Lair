using NUnit.Framework;
using Lair.Card;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    //# PoisonAura 의 1초 누적 틱 동작 + Attach/Detach 안전성 검증.
    public class PoisonAuraTests
    {
        [Test]
        public void Tick_1초마다_데미지_1회()
        {
            var hp = new FakeHealth();
            hp.SetMax(100);
            var aura = new PoisonAura(dps: 5f);
            aura.OnAttached(hp);

            //# 0.5초 → 데미지 없음
            aura.Tick(hp, 0.5f);
            Assert.AreEqual(0, hp.DamageCallCount);

            //# +0.6초 → 누적 1.1초, 1회 데미지
            aura.Tick(hp, 0.6f);
            Assert.AreEqual(1, hp.DamageCallCount);
            Assert.AreEqual(5, hp.LastDamage);

            //# +2초 → 누적 0.1+2=2.1, 2회 추가 데미지 (총 3회)
            aura.Tick(hp, 2.0f);
            Assert.AreEqual(3, hp.DamageCallCount);
        }

        [Test]
        public void OnAttached_시_초기화()
        {
            var hp = new FakeHealth();
            hp.SetMax(100);
            var aura = new PoisonAura(dps: 5f);
            aura.OnAttached(hp);
            aura.OnDetached(hp);

            //# Detach 후 Tick 호출은 무관 (호출되지 않을 것이지만 안전성)
            Assert.AreEqual(0, hp.DamageCallCount);
        }
    }
}
