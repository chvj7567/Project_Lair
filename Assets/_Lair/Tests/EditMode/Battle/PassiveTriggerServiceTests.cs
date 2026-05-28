using System.Collections.Generic;
using NUnit.Framework;
using Lair.Battle;
using Lair.Tests.Helpers;

namespace Lair.Tests.Battle
{
    //# HP% 임계점 9개 (90% ~ 10%) 통과 시 OnTriggered 발행 검증.
    public class PassiveTriggerServiceTests
    {
        [Test]
        public void HP_90퍼_통과_시_OnTriggered_0_발행()
        {
            FakeHealth hp = new FakeHealth();
            hp.SetMax(1000);
            PassiveTriggerService svc = new PassiveTriggerService(hp);
            List<int> fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(100);   //# 1000 → 900 (90%)

            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }

        [Test]
        public void 여러_임계점_동시_통과_시_순차_발동()
        {
            FakeHealth hp = new FakeHealth();
            hp.SetMax(1000);
            PassiveTriggerService svc = new PassiveTriggerService(hp);
            List<int> fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(500);   //# 1000 → 500 (50%) — 90/80/70/60/50% 통과

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, fired);
        }

        [Test]
        public void 한_임계점_1회만_발동()
        {
            FakeHealth hp = new FakeHealth();
            hp.SetMax(1000);
            PassiveTriggerService svc = new PassiveTriggerService(hp);
            List<int> fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(50);    //# 950
            hp.TakeDamage(50);    //# 900 — 90% 도달
            hp.TakeDamage(50);    //# 850 — 여전히 90% 이하지만 재발동 X

            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }

        [Test]
        public void 커스텀_임계점_주입_시_그_임계점으로만_발동()
        {
            FakeHealth hp = new FakeHealth();
            hp.SetMax(1000);
            //# 50% 단일 임계점만 주입
            PassiveTriggerService svc = new PassiveTriggerService(hp, new[] { 0.5f });
            List<int> fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(100);   //# 900 (90%) — 50% 미통과 → 발동 X
            Assert.AreEqual(0, fired.Count);

            hp.TakeDamage(450);   //# 450 (45%) — 50% 통과 → 발동
            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }
    }
}
