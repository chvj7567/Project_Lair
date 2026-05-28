using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    public class BattleClockTests
    {
        [Test]
        public void Tick_누적되어_Elapsed_증가()
        {
            BattleClock clock = new BattleClock(10f);
            clock.Start();

            clock.Tick(0.5f);
            clock.Tick(0.5f);
            clock.Tick(1.0f);

            Assert.AreEqual(2.0f, clock.Elapsed, 0.0001f);
        }

        [Test]
        public void OnTick_매_Tick마다_발행()
        {
            BattleClock clock = new BattleClock(10f);
            clock.Start();
            int callCount = 0;
            clock.OnTick += _ => callCount++;

            clock.Tick(0.5f);
            clock.Tick(0.5f);
            clock.Tick(0.5f);

            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void OnTimeUp_Total_도달_시_1회만_발행()
        {
            BattleClock clock = new BattleClock(1.0f);
            clock.Start();
            int timeUpCount = 0;
            clock.OnTimeUp += () => timeUpCount++;

            clock.Tick(0.6f);
            clock.Tick(0.6f);  //# 누적 1.2, 초과
            clock.Tick(0.6f);  //# 이미 IsRunning false 이므로 무시

            Assert.AreEqual(1, timeUpCount);
            Assert.IsFalse(clock.IsRunning);
        }

        [Test]
        public void Stop_이후_Tick_무시()
        {
            BattleClock clock = new BattleClock(10f);
            clock.Start();
            clock.Tick(1.0f);
            clock.Stop();

            clock.Tick(5.0f);

            Assert.AreEqual(1.0f, clock.Elapsed, 0.0001f);
        }
    }
}
