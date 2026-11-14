using System;

namespace Lair.Data
{
    //# 카드 JSON row 2종 (so-to-json-migration §3.10). 파일명 = EData 값명. 선택적 관계는 별도 row, sentinel 없음.

    //# Card.json — 핵심 엔티티: 카드 하나. 행 순서 = ECardId 정수값 순. PK cardId.
    [Serializable]
    public class CardRow
    {
        public string cardId;       //# ECardId 값명 — 세이브 문자열이라 글자 그대로 불변
        public string axis;         //# EBuildAxis 값명
        public int nameKey;         //# Strings_Ko
        public int descKey;         //# Strings_Ko
        public string effectKind;   //# ECardEffect 값명 (카드 id 와 다를 수 있다)
    }

    //# Card_Param.json — Card 의 관계: 카드 효과 수치 하나(효과마다 필드 구성이 달라 키-값). 복합키 cardId + paramKey.
    [Serializable]
    public class Card_ParamRow
    {
        public string cardId;
        public string paramKey;
        public float value;
    }

    //# 카드 JSON 입력 묶음 — CardDatabase.Build 입력 (MetaDataSet 선례).
    public class CardDataSet
    {
        public CardRow[] Cards;
        public Card_ParamRow[] Params;
    }
}
