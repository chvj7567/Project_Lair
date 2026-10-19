using System;

namespace Lair.Data
{
    //# 유물 2차 JSON row 클래스 (relics §3.2, Rule 02 §11-1). 순수 데이터 — JsonUtility 직렬화 대상.

    //# Relic.json — 핵심 엔티티: 유물 하나. rarity 는 모든 유물이 하나씩 갖는 필수 1:1 이라 직접 필드.
    [Serializable]
    public class RelicRow
    {
        public string relicId;    //# ERelic 값명 = 아이콘 파일명
        public string category;   //# ERelicCategory 값명
        public string rarity;     //# ERelicRarity 값명
        public int maxStack;
        public int nameKey;
        public int descKey;
    }

    //# Relic_Effect.json — 유물이 가진 효과들(복합키 relicId + effectIndex).
    [Serializable]
    public class Relic_EffectRow
    {
        public string relicId;
        public int effectIndex;
        public string effectKind;   //# ERelicEffectKind 값명
    }

    //# Relic_Effect_Param.json — 효과 수치 하나(키-값, Stage_Rule_Param 선례).
    [Serializable]
    public class Relic_Effect_ParamRow
    {
        public string relicId;
        public int effectIndex;
        public string paramKey;
        public float value;   //# 중첩 1 기준 값
    }

    //# Relic_Drop.json — 이 영웅이 이 유물을 드랍한다(선택적 관계, 행이 있는 조합만 존재).
    [Serializable]
    public class Relic_DropRow
    {
        public string relicId;
        public string heroId;   //# EHero 값명 (Chapter.heroId)
        public int weight;      //# 등급 안 상대 가중치 (>= 1)
    }

    //# RelicRarity.json — 등급 하나.
    [Serializable]
    public class RelicRarityRow
    {
        public string rarity;
        public int nameKey;
        public int baseWeight;
        public string colorHex;
    }

    //# RelicStack.json — 중첩 단계 하나의 효과 배율.
    [Serializable]
    public class RelicStackRow
    {
        public int stack;
        public float effectScale;
    }

    //# RelicConfig.json — 단일 객체(전역 드랍 규칙 수치).
    [Serializable]
    public class RelicConfigRow
    {
        public float lossChanceMul;
        public float pityPerMiss;
        public float pityMaxBonus;
        public float maxChance;
    }

    //# Stage_Relic.json — 스테이지 기본 드랍 확률(복합키 chapterId + stageIndex).
    [Serializable]
    public class Stage_RelicRow
    {
        public int chapterId;
        public int stageIndex;
        public float dropChance;
    }

    //# Difficulty_Relic.json — 난이도 드랍 확률 배율.
    [Serializable]
    public class Difficulty_RelicRow
    {
        public int difficulty;
        public float dropChanceMul;
    }

    //# Difficulty_Relic_Rarity.json — 난이도 x 등급 가중 배율(복합키 difficulty + rarity).
    [Serializable]
    public class Difficulty_Relic_RarityRow
    {
        public int difficulty;
        public string rarity;
        public float weightMul;
    }
}
