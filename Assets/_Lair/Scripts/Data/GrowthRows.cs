using System;

namespace Lair.Data
{
    //# 몬스터 육성 재화 JSON row 10종 (monster-growth-currencies §6.2). 파일명 = EData 값명. Rule 02 §11-1 정규화 - sentinel 없음.

    //# Currency.json - 핵심 엔티티: 재화 하나.
    [Serializable]
    public class CurrencyRow
    {
        public string currencyId;   //# PK, ECurrency 값명
        public int nameKey;         //# Strings_Ko
        public string colorHex;     //# #RRGGBB
    }

    //# Currency_Exchange.json - 재화 A 를 B 로 바꾸는 비율 하나. 복합키 fromCurrency + toCurrency.
    [Serializable]
    public class Currency_ExchangeRow
    {
        public string fromCurrency;
        public string toCurrency;
        public int fromAmount;
        public int toAmount;
    }

    //# AwakenStat.json - 종족의 각성 스탯 한 줄. 복합키 monster + statIndex.
    [Serializable]
    public class AwakenStatRow
    {
        public string monster;      //# EMonster 값명
        public int statIndex;       //# 1부터 연속
        public string statKind;     //# EMonsterStatKind 값명
        public float perLevelMul;   //# 레벨당 곱 배율
    }

    //# NestMastery.json - 숙련 단계 하나의 효과 종류. PK level.
    [Serializable]
    public class NestMasteryRow
    {
        public int level;
        public string effectKind;   //# ENestMasteryEffect 값명
    }

    //# NestMastery_Param.json - 그 단계 효과 수치 하나. 복합키 level + paramKey.
    [Serializable]
    public class NestMastery_ParamRow
    {
        public int level;
        public string paramKey;
        public float value;
    }

    //# MonsterTrait.json - 종족 고유 특성 하나. PK monster.
    [Serializable]
    public class MonsterTraitRow
    {
        public string monster;      //# EMonster 값명
        public string trait;        //# EMonsterTrait 값명
        public int nameKey;
        public int descKey;
    }

    //# MonsterTrait_Param.json - 단계별 특성 수치 하나. 복합키 monster + tier + paramKey.
    [Serializable]
    public class MonsterTrait_ParamRow
    {
        public string monster;
        public int tier;
        public string paramKey;
        public float value;
    }

    //# Difficulty_Awaken.json - 난이도의 각성석 보상 (선택적 관계 - Easy 행 없음). PK difficulty.
    [Serializable]
    public class Difficulty_AwakenRow
    {
        public int difficulty;
        public int firstClear;
        public int repeatWin;
    }

    //# Difficulty_TowerCrystal.json - 탑 유물 등급 단계의 층당 결정. PK difficulty(= 탑 유물 등급 단계 0/1/2).
    [Serializable]
    public class Difficulty_TowerCrystalRow
    {
        public int difficulty;
        public int crystalPerFloor;
    }

    //# RelicRarity_Shard.json - 등급의 잔해량. PK rarity.
    [Serializable]
    public class RelicRarity_ShardRow
    {
        public string rarity;       //# ERelicRarity 값명
        public int shardAmount;
    }
}
