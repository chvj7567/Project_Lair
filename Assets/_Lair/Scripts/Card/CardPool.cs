using System.Collections.Generic;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 풀 — 한 슬라이스의 모든 카드 묶음. CHMResource 로 로드.
    [CreateAssetMenu(fileName = "CardPool_", menuName = "Lair/Card Pool")]
    public class CardPool : ScriptableObject
    {
        [SerializeField] private List<CardData> _cards = new();
        public IReadOnlyList<CardData> Cards => _cards;

        //# 런타임 소비처용 — Card.json 행이 없는 카드는 LogError 후 빠진다(so-to-json-migration §9 E5). 순서는 풀 그대로.
        public List<CardData> PlayableCards()
        {
            List<CardData> playable = new List<CardData>(_cards.Count);
            foreach (CardData card in _cards)
            {
                if (card != null && card.HasData)
                {
                    playable.Add(card);
                }
            }
            return playable;
        }
    }
}
