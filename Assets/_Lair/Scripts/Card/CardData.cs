using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 데이터 정의 — Effect 는 SerializeReference 로 polymorphic 직렬화.
    [CreateAssetMenu(fileName = "Card_", menuName = "Lair/Card", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private ECardId _id;
        [SerializeField] private ECardCategory _category;
        [SerializeField] private string _displayName;
        [TextArea] [SerializeField] private string _description;
        //# 빌드 패널 아이콘 — LairCardPrefabBuilder 가 ECardId 이름 PNG 로 배정. 없으면 null.
        [SerializeField] private Sprite _icon;
        [SerializeReference] private ICardEffect _effect;

        public ECardId Id => _id;
        public ECardCategory Category => _category;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ICardEffect Effect => _effect;
    }
}
