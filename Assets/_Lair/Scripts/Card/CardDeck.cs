using System.Collections.Generic;

namespace Lair.Card
{
    //# 카드 풀에서 무작위 n장 드로우. POCO — 런타임에 BattleController 가 보유.
    public class CardDeck
    {
        private readonly List<CardData> _all;
        private readonly System.Random _rng;

        public CardDeck(IEnumerable<CardData> cards, int seed = 0)
        {
            _all = new List<CardData>(cards);
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        //# 무작위 n장 (중복 X). 풀 부족 시 가능한 만큼.
        public IReadOnlyList<CardData> Draw(int n)
        {
            List<CardData> pool = new List<CardData>(_all);
            int actual = System.Math.Min(n, pool.Count);
            List<CardData> result = new List<CardData>(actual);
            for (int i = 0; i < actual; ++i)
            {
                int idx = _rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
