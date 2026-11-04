using System;

namespace Lair.Data
{
    //# 던전 편성 JSON row (dungeon-formation §0.5). 순수 데이터 — 파일명 = EData 값명. 종족/몬스터/카드/재화는 Enum 값명 문자열.

    //# FormationConfig.json (단일 객체) — 핵심 엔티티: 편성 전역 규칙. 코스트·종당 최대는 2026-11-01 폐지.
    [Serializable]
    public class FormationConfigRow
    {
        public int unlockClearedStage;
    }

    //# Race.json — 핵심 엔티티: 종족 하나. PK race(ERace 값명), 이름·성격 문구는 필수 1:1 Strings_Ko id.
    [Serializable]
    public class RaceRow
    {
        public string race;
        public int nameKey;
        public int descKey;
    }

    //# Grade.json — 핵심 엔티티: 등급 하나(1~5). 접두어·HP·공격력 배율은 모든 등급이 가지는 필수 1:1 필드 (monster-grades §8).
    [Serializable]
    public class GradeRow
    {
        public int grade;
        public int prefixKey;
        public float hpMul;
        public float atkMul;
    }

    //# Race_Monster.json — Race 의 관계: 이 종족의 이 등급 몬스터. 복합키 race + grade, monster 는 전체에서 유일.
    [Serializable]
    public class Race_MonsterRow
    {
        public string race;
        public int grade;
        public string monster;
    }

    //# Grade_Promotion.json — Grade 의 선택적 관계: 이 등급으로 올리는 데 드는 재화 한 종. 복합키 grade + currency (2~5 등급만 행 존재).
    [Serializable]
    public class Grade_PromotionRow
    {
        public int grade;
        public string currency;
        public int amount;
    }

    //# FormationSlot.json — 핵심 엔티티: 둥지(스포너) 하나. PK slot(1~6, _spawners 인덱스+1). race 는 필수 1:1 직접 필드.
    [Serializable]
    public class FormationSlotRow
    {
        public int slot;
        public string race;
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
