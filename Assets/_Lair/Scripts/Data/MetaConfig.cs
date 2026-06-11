using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 메타 진행 수치 단일 진실 — 기획서 docs/design/village-meta-hub.md 값으로 채움 (BalanceConfig 패턴).
    //# 에셋 생성/값 주입은 LairVillageBuilder 의 V1 메뉴 (Gate G4 — 기획서 §2.1/§3.2/§4/§5.2/§6).
    [CreateAssetMenu(menuName = "Lair/MetaConfig", fileName = "MetaConfig")]
    public class MetaConfig : ScriptableObject
    {
        [Header("소울 보상 (기획서 §2.1)")]
        public int WinBaseSouls = 100;             //# 승리 기본 보상
        public float WinTimeBonusPerSec = 0.5f;    //# 승리 시 잔여 1초당 보너스 (floor)
        public int LoseMaxSouls = 60;              //# 패배 시 영웅 HP 깎은 비율 × 이 값 (floor)

        [Header("영주 XP (기획서 §4.1)")]
        public int WinXp = 100;
        public int LoseXp = 40;
        public int XpPerLevelBase = 100;           //# Lv1→2 필요 XP
        public float XpGrowth = 1.25f;             //# 레벨당 필요 XP 증가율

        [Header("상점 / 영주 보상 / 도전과제 (기획서 §3.2 / §4.3 / §5.2)")]
        public List<ShopItemDef> ShopItems = new List<ShopItemDef>();
        public List<LordRewardDef> LordRewards = new List<LordRewardDef>();
        public List<AchievementDef> Achievements = new List<AchievementDef>();

        [Header("잠금 더미 슬롯 수 (기획서 §6)")]
        public int HeroLockedSlots = 3;            //# 영웅 선택 — Knight 1 + 잠금 3
        public int CodexLockedSlots = 2;           //# 도감 — 각 탭 말미 2개씩

        public ShopItemDef FindShopItem(string id)
        {
            foreach (ShopItemDef item in ShopItems)
            {
                if (item != null && item.Id == id)
                    return item;
            }
            return null;
        }
    }

    [Serializable]
    public class ShopItemDef
    {
        public string Id;                  //# 예: "MonsterHpUp" — MetaProfile.ShopLevels 키
        public string DisplayName;
        public string Description;         //# 효과 설명문 (셀 표기) — 기획서 §3.2 / §11.3 [추가 3]
        public EShopEffectKind EffectKind;
        public EMonsterStatKind StatKind;  //# EffectKind == MonsterStat 일 때만 사용
        public float PerLevelMul = 1f;     //# 레벨당 곱연산 배율 (SpawnerPeriod/Cooldown/SlowFactor 는 1 미만이 강화)
        public int MaxLevel = 5;
        public int BasePrice = 50;
        public float PriceGrowth = 1.6f;   //# 레벨당 가격 배율 (floor)
    }

    [Serializable]
    public class LordRewardDef
    {
        public int Level;
        public bool IsLockedDummy;         //# true 면 "??? — 추후 해금" 잠금 슬롯
        public int RewardSouls;            //# IsLockedDummy == false 일 때 지급
        public string DisplayName;
    }

    [Serializable]
    public class AchievementDef
    {
        public string Id;                  //# 예: "FirstWin" — MetaProfile.AchievedIds 키
        public string DisplayName;
        public string Description;
        public EAchievementCondition Condition;
        public float Threshold;            //# WinUnderSeconds=초 / TotalWins·TotalRuns=횟수 / SynergyTierReached=티어
        public int RewardSouls = 30;
    }
}
