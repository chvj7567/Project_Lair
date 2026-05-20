using NUnit.Framework;
using Lair.Card;
using Lair.Tests.Helpers;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# PoisonAura 의 영역판정 + 1초 누적 틱 동작 + Attach/Detach 안전성 검증.
    //# B2 재설계 후 Tick 은 Transform/CHPoolable 의존 — 코어 로직은 AccumulateDamageTicks 로 검증.
    public class PoisonAuraTests
    {
        [Test]
        public void 영역_안에서_1초마다_데미지_누적()
        {
            var aura = new PoisonAura(dps: 5f, radius: 1.25f);
            Vector3 disk = Vector3.zero;
            Vector3 inside = Vector3.zero;   //# 디스크 중심 — 영역 안

            //# 0.5초 → 누적 0.5, 틱 0
            Assert.AreEqual(0, aura.AccumulateDamageTicks(inside, disk, 0.5f));
            //# +0.6초 → 누적 1.1, 틱 1
            Assert.AreEqual(1, aura.AccumulateDamageTicks(inside, disk, 0.6f));
            //# +2.0초 → 누적 0.1+2=2.1, 틱 2
            Assert.AreEqual(2, aura.AccumulateDamageTicks(inside, disk, 2.0f));
        }

        [Test]
        public void 영역_경계_밖에서는_데미지_없음()
        {
            var aura = new PoisonAura(dps: 5f, radius: 1.25f);
            Vector3 disk = Vector3.zero;
            Vector3 outside = new Vector3(5f, 0f, 0f);   //# 반경 1.25 밖

            Assert.AreEqual(0, aura.AccumulateDamageTicks(outside, disk, 10f));
        }

        [Test]
        public void 영역_밖이면_accumulator_유지_재진입_시_이어서_누적()
        {
            var aura = new PoisonAura(dps: 5f, radius: 1.25f);
            Vector3 disk = Vector3.zero;
            Vector3 inside = Vector3.zero;
            Vector3 outside = new Vector3(5f, 0f, 0f);

            //# 안에서 0.8 누적
            Assert.AreEqual(0, aura.AccumulateDamageTicks(inside, disk, 0.8f));
            //# 밖으로 — 누적 동결, 틱 0
            Assert.AreEqual(0, aura.AccumulateDamageTicks(outside, disk, 5f));
            //# 다시 안 — 0.8 + 0.3 = 1.1, 틱 1
            Assert.AreEqual(1, aura.AccumulateDamageTicks(inside, disk, 0.3f));
        }

        [Test]
        public void OnAttached_OnDetached_안전성()
        {
            var hp = new FakeHealth();
            hp.SetMax(100);
            var aura = new PoisonAura(dps: 5f);

            //# FakeHealth 는 POCO — OnAttached 가 _heroTransform 못 잡아도 예외 없이 통과.
            Assert.DoesNotThrow(() => aura.OnAttached(hp));
            Assert.DoesNotThrow(() => aura.OnDetached(hp));
            Assert.AreEqual(0, hp.DamageCallCount);
        }
    }
}
