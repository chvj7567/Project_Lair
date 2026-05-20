using NUnit.Framework;
using Lair.Card;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    //# 영웅 IMover.Speed 조작 + 백업/복원 검증.
    public class SlowAuraTests
    {
        [Test]
        public void OnAttached_시_Speed가_factor배로_감소()
        {
            var hero = new FakeHealth();
            hero.SetMax(100);
            var mover = new FakeMover { Speed = 5f };
            var aura = new SlowAura(mover, slowFactor: 0.6f);

            aura.OnAttached(hero);

            Assert.AreEqual(3f, mover.Speed, 0.001f);
        }

        [Test]
        public void OnDetached_시_Speed가_원래값으로_복원()
        {
            var hero = new FakeHealth();
            hero.SetMax(100);
            var mover = new FakeMover { Speed = 5f };
            var aura = new SlowAura(mover, slowFactor: 0.6f);

            aura.OnAttached(hero);
            aura.OnDetached(hero);

            Assert.AreEqual(5f, mover.Speed, 0.001f);
        }

        [Test]
        public void Mover가_null이면_NRE_없이_무동작()
        {
            var hero = new FakeHealth();
            hero.SetMax(100);
            var aura = new SlowAura(null, slowFactor: 0.6f);

            Assert.DoesNotThrow(() => aura.OnAttached(hero));
            Assert.DoesNotThrow(() => aura.OnDetached(hero));
        }
    }
}
