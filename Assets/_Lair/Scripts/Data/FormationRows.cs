using System;

namespace Lair.Data
{
    //# 던전 편성 JSON row 5종 (dungeon-formation §3). 순수 데이터 — 파일명 = EData 값명. 종/카드는 Enum 값명 문자열.

    //# FormationConfig.json (단일 객체) — 핵심 엔티티: 편성 전역 규칙.
    [Serializable]
    public class FormationConfigRow
    {
        public int costCap;
        public int unlockClearedStage;
        public int maxPerSpeciesBase;
        public int maxPerSpeciesExpanded;
        public int expandClearedStage;
    }

    //# FormationMonster.json — 핵심 엔티티: 편성에 넣을 수 있는 몬스터 종 하나. PK monster(EMonster 값명).
    [Serializable]
    public class FormationMonsterRow
    {
        public string monster;
        public int cost;
    }

    //# FormationSlot.json — 핵심 엔티티: 둥지(스포너) 하나. PK slot(1~6, _spawners 인덱스+1). defaultMonster 는 필수 1:1 직접 필드.
    [Serializable]
    public class FormationSlotRow
    {
        public int slot;
        public string defaultMonster;
    }

    //# Card_Species.json — Card 의 선택적 관계(종 의존 카드만 행 존재). 복합키 cardId + monster. 의미는 any-of.
    [Serializable]
    public class Card_SpeciesRow
    {
        public string cardId;
        public string monster;
    }

    //# Chapter_FormationHint.json — Chapter 당 영웅 성향 문구 하나. PK chapterId, hintKey 는 Strings_Ko id.
    [Serializable]
    public class Chapter_FormationHintRow
    {
        public int chapterId;
        public int hintKey;
    }
}
