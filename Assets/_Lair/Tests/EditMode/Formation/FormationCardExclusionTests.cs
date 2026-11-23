using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 카드 제외 판정은 편성과 독립이고 PickCounter 상한(IsCapped)에만 의존한다 (dungeon-formation-free §4·§7).
    public class FormationCardExclusionTests
    {
        private static CardPickCounter Picked(ECardId id, int times)
        {
            CardPickCounter counter = new CardPickCounter();
            for (int i = 0; i < times; ++i)
            {
                counter.RecordPick(id);
            }
            return counter;
        }

        [Test]
        public void 상한에_도달한_카드는_제외된다()
        {
            CardPickCounter counter = Picked(ECardId.WispHpBoost, CardPickCounter.Cap);
            Assert.IsTrue(BattleController.IsCardExcluded(counter, ECardId.WispHpBoost));
        }

        [Test]
        public void 상한_미만이거나_안_뽑은_카드는_제외되지_않는다()
        {
            CardPickCounter counter = Picked(ECardId.WispHpBoost, CardPickCounter.Cap - 1);
            Assert.IsFalse(BattleController.IsCardExcluded(counter, ECardId.WispHpBoost));
            Assert.IsFalse(BattleController.IsCardExcluded(counter, ECardId.WraithDamageBoost));
        }

        [Test]
        public void 편성_상태를_바꿔도_제외_판정은_변하지_않는다()
        {
            CardPickCounter counter = Picked(ECardId.WispHpBoost, CardPickCounter.Cap);
            MetaProfile profile = new MetaProfile();
            bool cappedBefore = BattleController.IsCardExcluded(counter, ECardId.WispHpBoost);
            bool openBefore = BattleController.IsCardExcluded(counter, ECardId.WraithDamageBoost);

            profile.HasCustomFormation = true;
            profile.Formation = FormationTestKit.Rows(FormationTestKit.Mismatched());
            Assert.AreEqual(cappedBefore, BattleController.IsCardExcluded(counter, ECardId.WispHpBoost));
            Assert.AreEqual(openBefore, BattleController.IsCardExcluded(counter, ECardId.WraithDamageBoost));

            profile.HasCustomFormation = false;
            profile.Formation.Clear();
            Assert.AreEqual(cappedBefore, BattleController.IsCardExcluded(counter, ECardId.WispHpBoost));
            Assert.AreEqual(openBefore, BattleController.IsCardExcluded(counter, ECardId.WraithDamageBoost));
        }
    }
}
