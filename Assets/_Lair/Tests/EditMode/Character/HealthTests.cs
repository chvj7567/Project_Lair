using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.Character
{
    public class HealthTests
    {
        private static Health NewHealth(int max = 100)
        {
            //# Health 는 MonoBehaviour. 테스트에서는 ScriptableObject/MB 없이도 동작하도록
            //# 일반 클래스 형태의 내부 로직만 검증. (Unity 가 MB 인스턴스화 없이 new 호출은
            //# 권장하지 않지만 테스트 한정 허용.)
            Health h = new Health();
            h.SetMax(max);
            return h;
        }

        [Test]
        public void TakeDamage_Current_감소_및_OnChanged_발행()
        {
            Health h = NewHealth(100);
            int curCaptured = -1, maxCaptured = -1;
            h.OnChanged += (c, m) => { curCaptured = c; maxCaptured = m; };

            h.TakeDamage(30);

            Assert.AreEqual(70, h.Current);
            Assert.AreEqual(70, curCaptured);
            Assert.AreEqual(100, maxCaptured);
        }

        [Test]
        public void Current_0_도달_시_OnDied_1회만_발행()
        {
            Health h = NewHealth(50);
            int diedCount = 0;
            h.OnDied += () => diedCount++;

            h.TakeDamage(30);
            h.TakeDamage(30);   //# 누적 60 → 0 으로 클램프 + OnDied
            h.TakeDamage(30);   //# 사망 후, OnDied 추가 발행 X

            Assert.AreEqual(0, h.Current);
            Assert.IsFalse(h.IsAlive);
            Assert.AreEqual(1, diedCount);
        }

        [Test]
        public void 사망_후_TakeDamage_무시()
        {
            Health h = NewHealth(10);
            h.TakeDamage(10);
            int onChangedAfterDeath = 0;
            h.OnChanged += (_, _) => onChangedAfterDeath++;

            h.TakeDamage(5);

            Assert.AreEqual(0, h.Current);
            Assert.AreEqual(0, onChangedAfterDeath);
        }

        [Test]
        public void SetMax_resetCurrent_옵션()
        {
            Health h = NewHealth(100);
            h.TakeDamage(40);   //# Current=60

            h.SetMax(200, resetCurrent: false);
            Assert.AreEqual(60, h.Current);
            Assert.AreEqual(200, h.Max);

            h.SetMax(50, resetCurrent: true);
            Assert.AreEqual(50, h.Current);
            Assert.AreEqual(50, h.Max);
        }
    }
}
