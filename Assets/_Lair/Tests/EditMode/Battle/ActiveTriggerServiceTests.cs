using System.Collections.Generic;
using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# 30초 단위 9개 임계점 (30/60/.../270초) 통과 시 OnTriggered 발행 검증.
    public class ActiveTriggerServiceTests
    {
        private static (BattleClock clock, ActiveTriggerService svc, List<int> fired) Setup()
        {
            var clock = new BattleClock(300f);
            clock.Start();
            var svc = new ActiveTriggerService(clock);
            var fired = new List<int>();
            svc.OnTriggered += idx => fired.Add(idx);
            return (clock, svc, fired);
        }

        [Test]
        public void Tick_30초_미만은_미발동()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(29.9f);
            Assert.AreEqual(0, fired.Count);
        }

        [Test]
        public void Tick_30초_도달_시_Index0_발동()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(30f);
            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }

        [Test]
        public void 한_임계점_1회만_발동()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(30f);
            clock.Tick(0.1f);
            Assert.AreEqual(1, fired.Count);
        }

        [Test]
        public void 큰_dt_로_여러_임계점_동시_통과_시_순차_발동()
        {
            var (clock, _, fired) = Setup();
            clock.Tick(95f);    //# 30, 60, 90 통과
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, fired);
        }

        [Test]
        public void Dispose_후_미발동()
        {
            var (clock, svc, fired) = Setup();
            svc.Dispose();
            clock.Tick(30f);
            Assert.AreEqual(0, fired.Count);
        }
    }
}
