using System.Collections.Generic;
using NUnit.Framework;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# CardDeck 의 무작위 Draw — 중복 없음, 풀 부족 시 가능한 만큼, seed 재현성.
    public class CardDeckTests
    {
        private static List<CardData> NewPool(int count)
        {
            var list = new List<CardData>();
            for (int i = 0; i < count; ++i)
            {
                //# ECardId 의 정의된 값을 순환
                list.Add(FakeCardData.Create((ECardId)(i % System.Enum.GetValues(typeof(ECardId)).Length)));
            }
            return list;
        }

        [TearDown]
        public void TearDown()
        {
            //# ScriptableObject 정리
        }

        [Test]
        public void Draw_3_카드_3장_중복_없음()
        {
            var pool = NewPool(7);
            var deck = new CardDeck(pool, seed: 1234);

            var drawn = deck.Draw(3);

            Assert.AreEqual(3, drawn.Count);
            var set = new HashSet<CardData>(drawn);
            Assert.AreEqual(3, set.Count, "중복 카드 없음");
        }

        [Test]
        public void Draw_풀_부족_시_가능한_만큼()
        {
            var pool = NewPool(2);
            var deck = new CardDeck(pool, seed: 1234);

            var drawn = deck.Draw(3);

            Assert.AreEqual(2, drawn.Count);
        }

        [Test]
        public void Seed_고정_시_Reproducibility()
        {
            var pool = NewPool(7);
            var d1 = new CardDeck(pool, seed: 42);
            var d2 = new CardDeck(pool, seed: 42);

            var a = d1.Draw(3);
            var b = d2.Draw(3);

            for (int i = 0; i < 3; ++i)
                Assert.AreSame(a[i], b[i]);
        }
    }
}
