using System.Collections.Generic;
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    public class HeroSkillPhaseGateTests
    {
        private static HeroSkillPhaseGate Gate()
            => new HeroSkillPhaseGate(new[] { 0.9f, 0.6f, 0.3f });

        [Test]
        public void Poll_100퍼센트면_활성없음()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(1.0f, outv);
            Assert.AreEqual(0, outv.Count);
        }

        [Test]
        public void Poll_90퍼센트_도달시_인덱스0_활성()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.9f, outv);
            CollectionAssert.AreEqual(new[] { 0 }, outv);
        }

        [Test]
        public void Poll_같은페이즈_재폴링시_중복활성없음()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.9f, outv);
            g.Poll(0.85f, outv);   //# outv 는 호출마다 clear 됨
            Assert.AreEqual(0, outv.Count);
        }

        [Test]
        public void Poll_급격한_HP하락시_누락된_페이즈_모두_활성()
        {
            //# 90·60 을 건너뛰고 한 번에 25% 로 떨어져도 0,1,2 전부 활성.
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.25f, outv);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, outv);
        }

        [Test]
        public void Reset_후_다시_활성가능()
        {
            HeroSkillPhaseGate g = Gate();
            List<int> outv = new List<int>();
            g.Poll(0.2f, outv);
            g.Reset();
            g.Poll(0.9f, outv);
            CollectionAssert.AreEqual(new[] { 0 }, outv);
        }
    }
}
