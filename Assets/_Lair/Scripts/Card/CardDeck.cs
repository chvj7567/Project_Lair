using System;
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
        public IReadOnlyList<CardData> Draw(int n) => Draw(n, null);

        //# isExcluded(id) == true 인 카드는 후보에서 제외 (3픽 캡). null 이면 전체 후보.
        //# 제외 후 적격 카드가 n 미만이면 가능한 만큼만 반환 (기존 graceful fallback).
        public IReadOnlyList<CardData> Draw(int n, Func<Lair.Data.ECardId, bool> isExcluded)
        {
            List<CardData> pool = new List<CardData>();
            for (int i = 0; i < _all.Count; ++i)
            {
                if (isExcluded != null && isExcluded(_all[i].Id))
                    continue;
                pool.Add(_all[i]);
            }

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
