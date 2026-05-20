using NUnit.Framework;
using Lair.Card;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    //# 영웅 IAttacker.Enabled 토글 + 백업/복원 검증.
    public class SilenceAuraTests
    {
        [Test]
        public void OnAttached_시_Enabled가_false로_변경()
        {
            var hero = new FakeHealth();
            hero.SetMax(100);
            var atk = new FakeAttacker { Enabled = true };
            var aura = new SilenceAura(atk);

            aura.OnAttached(hero);

            Assert.IsFalse(atk.Enabled);
        }

        [Test]
        public void OnDetached_시_Enabled_백업값으로_복원()
        {
            var hero = new FakeHealth();
            hero.SetMax(100);
            var atk = new FakeAttacker { Enabled = true };
            var aura = new SilenceAura(atk);

            aura.OnAttached(hero);
            aura.OnDetached(hero);

            Assert.IsTrue(atk.Enabled);
        }

        [Test]
        public void Attacker가_null이면_NRE_없이_무동작()
        {
            var hero = new FakeHealth();
            hero.SetMax(100);
            var aura = new SilenceAura(null);

            Assert.DoesNotThrow(() => aura.OnAttached(hero));
            Assert.DoesNotThrow(() => aura.OnDetached(hero));
        }
    }
}
