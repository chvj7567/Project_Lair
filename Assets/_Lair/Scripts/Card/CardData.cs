using Lair.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lair.Card
{
    //# 카드 데이터 정의 — Effect 는 SerializeReference 로 polymorphic 직렬화.
    //# 카드 리뉴얼 v0.6 (2026-05-31) — 기존 _category 필드(int 0~3) → _axis(EBuildAxis) 마이그레이션 완료.
    [CreateAssetMenu(fileName = "Card_", menuName = "Lair/Card", order = 0)]
    public class CardData : ScriptableObject
    {
        [SerializeField] private ECardId _id;
        [FormerlySerializedAs("_category")]
        [SerializeField] private EBuildAxis _axis;
        [SerializeField] private string _displayName;
        [TextArea] [SerializeField] private string _description;
        //# 빌드 패널 아이콘 — LairCardPrefabBuilder 가 ECardId 이름 PNG 로 배정. 없으면 null.
        [SerializeField] private Sprite _icon;
        //# 3택1 팝업 카드 일러스트(3:4) — LairCardPrefabBuilder 가 CardArt/{ECardId}.png 로 배정. 없으면 null.
        [SerializeField] private Sprite _cardImage;
        [SerializeReference] private ICardEffect _effect;

        public ECardId Id => _id;
        public EBuildAxis Axis => _axis;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite CardImage => _cardImage;
        public ICardEffect Effect => _effect;
    }
}
