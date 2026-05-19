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
    }
}
