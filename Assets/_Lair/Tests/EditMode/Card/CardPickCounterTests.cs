using NUnit.Framework;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.Card
{
    //# CardPickCounter — 카드별 픽수 누적, 캡(3) 판정, 리셋.
    public class CardPickCounterTests
    {
        [Test]
        public void RecordPick_누적_GetCount_반영()
        {
            CardPickCounter c = new CardPickCounter();
            c.RecordPick(ECardId.WispHpBoost);
            c.RecordPick(ECardId.WispHpBoost);
            Assert.AreEqual(2, c.GetCount(ECardId.WispHpBoost));
            Assert.AreEqual(0, c.GetCount(ECardId.Frenzy));
        }

        [Test]
        public void IsCapped_3픽_도달시_true()
        {
            CardPickCounter c = new CardPickCounter();
            Assert.IsFalse(c.IsCapped(ECardId.Frenzy));
            c.RecordPick(ECardId.Frenzy);
            c.RecordPick(ECardId.Frenzy);
            Assert.IsFalse(c.IsCapped(ECardId.Frenzy), "2픽은 아직 미캡");
            c.RecordPick(ECardId.Frenzy);
            Assert.IsTrue(c.IsCapped(ECardId.Frenzy), "3픽 도달 시 캡");
        }

        [Test]
        public void Reset_모든_카운트_0()
        {
            CardPickCounter c = new CardPickCounter();
            c.RecordPick(ECardId.Slow);
            c.Reset();
            Assert.AreEqual(0, c.GetCount(ECardId.Slow));
            Assert.IsFalse(c.IsCapped(ECardId.Slow));
        }
    }
}
