using System.Collections.Generic;
using NUnit.Framework;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;

namespace Lair.Tests.Card
{
    //# CardDeck.Draw(n, isExcluded) — 제외 predicate 가 true 인 카드는 후보에서 빠진다.
    public class CardDeckCapExclusionTests
    {
        private static List<CardData> NewPool(params ECardId[] ids)
        {
            List<CardData> list = new List<CardData>();
            foreach (ECardId id in ids)
                list.Add(FakeCardData.Create(id));
            return list;
        }

        [Test]
        public void Draw_제외카드는_후보에_안나온다()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow, ECardId.TimeStop);
            CardDeck deck = new CardDeck(pool, seed: 99);

            IReadOnlyList<CardData> drawn = deck.Draw(3, id => id == ECardId.Frenzy);

            foreach (CardData c in drawn)
                Assert.AreNotEqual(ECardId.Frenzy, c.Id, "제외 카드 Frenzy 가 후보에 있으면 안 됨");
        }

        [Test]
        public void Draw_제외후_적격_3장미만이면_가능한_만큼()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow);
            CardDeck deck = new CardDeck(pool, seed: 99);

            //# 3장 중 2장 제외 → 적격 1장만
            IReadOnlyList<CardData> drawn = deck.Draw(3, id => id == ECardId.Frenzy || id == ECardId.Slow);

            Assert.AreEqual(1, drawn.Count);
            Assert.AreEqual(ECardId.WispHpBoost, drawn[0].Id);
        }

        [Test]
        public void Draw_predicate_null이면_기존동작_전체후보()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow);
            CardDeck deck = new CardDeck(pool, seed: 99);

            IReadOnlyList<CardData> drawn = deck.Draw(3, null);

            Assert.AreEqual(3, drawn.Count);
        }
    }
}
