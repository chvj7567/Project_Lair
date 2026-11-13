using System;

namespace Lair.Data
{
    //# 메타(소울·상점·영주·도전과제) JSON row 9종 (so-to-json-migration §3.4~§3.8). 파일명 = EData 값명. 선택적 관계는 별도 row, sentinel 없음.

    //# MetaConfig.json (단일 객체) — 핵심 엔티티: 소울 보상과 영주 XP 공식에 쓰이는 상수 묶음.
    [Serializable]
    public class MetaConfigRow
    {
        public int winBaseSouls;
        public float winTimeBonusPerSec;
        public int loseMaxSouls;
        public int winXp;
        public int loseXp;
        public int xpPerLevelBase;
        public float xpGrowth;
    }

    //# ShopItem.json — 핵심 엔티티: 상점에서 레벨을 올려 사는 영구 강화 항목 하나. 행 순서 = 상점 표시 순서. PK itemId.
    [Serializable]
    public class ShopItemRow
    {
        public string itemId;       //# MetaProfile.ShopLevels 세이브 키 — 글자 그대로 불변
        public int nameKey;         //# Strings_Ko
        public string effectKind;   //# EShopEffectKind 값명
        public int maxLevel;
        public int basePrice;
        public float priceGrowth;
        public string currency;     //# ECurrency 값명 = Currency.currencyId
    }

    //# ShopItem_Stat.json — ShopItem 의 관계. effectKind 가 MonsterStat 인 항목만. PK itemId.
    [Serializable]
    public class ShopItem_StatRow
    {
        public string itemId;
        public string statKind;     //# EMonsterStatKind 값명
    }

    //# ShopItem_Species.json — ShopItem 의 관계. 종족 항목(MonsterSpecies·MonsterAwaken·NestMastery·MonsterTrait)만. PK itemId.
    [Serializable]
    public class ShopItem_SpeciesRow
    {
        public string itemId;
        public string monster;      //# EMonster 기준 6종 값명
    }

    //# ShopItem_Mul.json — ShopItem 의 관계. 레벨당 곱 배율이 있는 항목(MonsterStat·SpawnerPeriod·MonsterSpecies)만. PK itemId.
    [Serializable]
    public class ShopItem_MulRow
    {
        public string itemId;
        public float perLevelMul;
    }

    //# ShopItem_Desc.json — ShopItem 의 관계. 설명 문구가 있는 항목만. PK itemId.
    [Serializable]
    public class ShopItem_DescRow
    {
        public string itemId;
        public int descKey;         //# Strings_Ko
    }

    //# LordLevel.json — 핵심 엔티티: 영주 보상 트랙에 표시되는 레벨 칸 하나. 행 순서 = level 오름차순. PK level.
    [Serializable]
    public class LordLevelRow
    {
        public int level;
    }

    //# LordLevel_Reward.json — LordLevel 의 관계. 보상이 있는 칸만(행 없음 = 잠금 칸). PK level.
    [Serializable]
    public class LordLevel_RewardRow
    {
        public int level;
        public int nameKey;         //# Strings_Ko
        public int rewardSouls;
    }

    //# Achievement.json — 핵심 엔티티: 고정 도전과제 하나. 행 순서 = 퀘스트 팝업 표시 순서. PK achievementId.
    [Serializable]
    public class AchievementRow
    {
        public string achievementId;    //# MetaProfile.AchievedIds 세이브 키 — 글자 그대로 불변
        public int nameKey;
        public int descKey;
        public string condition;        //# EAchievementCondition 값명
        public float threshold;
        public int rewardSouls;
    }

    //# 메타 JSON 9종을 한 번에 넘기는 묶음 — MetaConfig.Build 입력 (GrowthDataSet 선례).
    public class MetaDataSet
    {
        public MetaConfigRow Config;
        public ShopItemRow[] ShopItems;
        public ShopItem_StatRow[] Stats;
        public ShopItem_SpeciesRow[] Species;
        public ShopItem_MulRow[] Muls;
        public ShopItem_DescRow[] Descs;
        public LordLevelRow[] LordLevels;
        public LordLevel_RewardRow[] LordRewards;
        public AchievementRow[] Achievements;
    }
}
