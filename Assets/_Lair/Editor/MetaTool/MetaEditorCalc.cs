using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Meta Editor 의 미리보기/검증 순수 계산 — 윈도우(IMGUI)와 분리해 테스트 가능 (Rule 02 §5).
    //# 가격 산식은 ShopService.PriceOf 단일 진실을 재사용 — 런타임과 desync 불가 구조.
    public static class MetaEditorCalc
    {
        //# 레벨별 구매 가격 행 (Lv0→1 … Lv(Max-1)→Max) = floor(BasePrice × PriceGrowth^Lv).
        public static int[] PriceRows(ShopItemDef def)
        {
            int count = Mathf.Max(0, def.MaxLevel);
            int[] rows = new int[count];
            for (int level = 0; level < count; ++level)
            {
                rows[level] = ShopService.PriceOf(def, level);
            }
            return rows;
        }

        //# 만렙까지 누적 구매 비용 (기획서 §3.5 "누적" 열).
        public static int CumulativeMaxCost(ShopItemDef def)
        {
            int total = 0;
            foreach (int price in PriceRows(def))
            {
                total += price;
            }
            return total;
        }

        //# 만렙 효과 배율 = PerLevelMul^MaxLevel (기획서 §3.2 "만렙 배율" 열).
        public static float MaxLevelMultiplier(ShopItemDef def)
            => Mathf.Pow(def.PerLevelMul, def.MaxLevel);

        //# 대시보드 — 전 품목 만렙 총비용 (회귀 테스트 11,849 기대값 갱신 참고용).
        public static int TotalShopMaxCost(IReadOnlyList<ShopItemDef> items)
        {
            int total = 0;
            foreach (ShopItemDef def in items)
            {
                if (def == null)
                    continue;
                total += CumulativeMaxCost(def);
            }
            return total;
        }

        //# 대시보드 — 도전과제 보상 소울 합 (회귀 테스트 880 기대값 갱신 참고용).
        public static int TotalAchievementSouls(IReadOnlyList<AchievementDef> items)
        {
            int total = 0;
            foreach (AchievementDef def in items)
            {
                if (def == null)
                    continue;
                total += def.RewardSouls;
            }
            return total;
        }

        //# 대시보드 — 영주 보상 소울 합, 잠금 더미 제외 (회귀 테스트 600 기대값 갱신 참고용).
        public static int TotalLordSouls(IReadOnlyList<LordRewardDef> items)
        {
            int total = 0;
            foreach (LordRewardDef def in items)
            {
                if (def == null)
                    continue;
                if (def.IsLockedDummy)
                    continue;
                total += def.RewardSouls;
            }
            return total;
        }

        //# 비공백 Id 중복 검출 — 공백/빈 Id 는 별도 "공백 경고" 대상이라 제외. 발견 순서 유지.
        public static List<string> FindDuplicateIds(IReadOnlyList<string> ids)
        {
            List<string> duplicates = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            foreach (string id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (seen.Add(id) == false && duplicates.Contains(id) == false)
                {
                    duplicates.Add(id);
                }
            }
            return duplicates;
        }

        //# Condition 별 Threshold 단위 안내 (기획서 §5.2 / CommonEnum EAchievementCondition 판정 의미).
        public static string ThresholdUnitHint(EAchievementCondition condition)
        {
            switch (condition)
            {
                case EAchievementCondition.FirstWin:
                    return "Threshold 무시됨 — 첫 승리 시 자동 달성";
                case EAchievementCondition.WinUnderSeconds:
                    return "Threshold 단위: 초 — 승리 && 사망 시각 < Threshold";
                case EAchievementCondition.TotalWins:
                    return "Threshold 단위: 횟수 — 누적 승수 >= Threshold";
                case EAchievementCondition.TotalRuns:
                    return "Threshold 단위: 횟수 — 누적 출격 >= Threshold";
                case EAchievementCondition.SynergyTierReached:
                    return "Threshold 단위: 티어 (1~3) — 한 런 최고 시너지 Tier >= Threshold";
                default:
                    return "";
            }
        }

        //# 영주 트랙 레벨 검증 — 중복 레벨 + 역순(앞 항목보다 낮거나 같은 레벨) 경고 메시지 목록.
        public static List<string> LordLevelWarnings(IReadOnlyList<LordRewardDef> rewards)
        {
            List<string> warnings = new List<string>();
            HashSet<int> seen = new HashSet<int>();
            int prevLevel = int.MinValue;
            for (int i = 0; i < rewards.Count; ++i)
            {
                LordRewardDef def = rewards[i];
                if (def == null)
                    continue;
                if (seen.Add(def.Level) == false)
                {
                    warnings.Add($"#{i + 1}: Lv {def.Level} 중복");
                }
                else if (def.Level < prevLevel)
                {
                    warnings.Add($"#{i + 1}: Lv {def.Level} 가 앞 항목(Lv {prevLevel})보다 낮음 — 역순");
                }
                prevLevel = def.Level;
            }
            return warnings;
        }
    }
}
