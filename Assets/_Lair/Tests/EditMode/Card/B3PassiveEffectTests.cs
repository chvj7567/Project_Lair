using NUnit.Framework;
using Lair.Card;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    //# B3 패시브 효과 중 POCO 검증 가능한 항목 — HeroAttackDownAura 의 PowerScale 조작.
    //# 몬스터 순회 효과(강화/소환/교체)는 CharacterRegistry·CHMPool 의존이라 PlayMode/수동 검증.
    public class B3PassiveEffectTests
    {
        [Test]
        public void HeroAttackDownAura_OnAttached_PowerScale_0_75배()
        {
            FakeAttacker atk = new FakeAttacker { PowerScale = 1f };
            HeroAttackDownAura aura = new HeroAttackDownAura(atk, 0.75f);

            aura.OnAttached(new FakeHealth());

            Assert.AreEqual(0.75f, atk.PowerScale, 0.001f);
        }

        [Test]
        public void HeroAttackDownAura_중복_OnAttached_시_1회만_적용()
        {
            FakeAttacker atk = new FakeAttacker { PowerScale = 1f };
            HeroAttackDownAura aura = new HeroAttackDownAura(atk, 0.75f);

            aura.OnAttached(new FakeHealth());
            aura.OnAttached(new FakeHealth());   //# 같은 인스턴스 재호출 — _applied 가드

            Assert.AreEqual(0.75f, atk.PowerScale, 0.001f);
        }

        [Test]
        public void HeroAttackDownAura_Attacker가_null이면_무동작()
        {
            HeroAttackDownAura aura = new HeroAttackDownAura(null, 0.75f);
            Assert.DoesNotThrow(() => aura.OnAttached(new FakeHealth()));
            Assert.DoesNotThrow(() => aura.OnDetached(new FakeHealth()));
        }
    }
}
