using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    //# 고정 도전과제 판정 (spec §5.6 / 기획서 §5). 프로필은 이번 런 정산(TotalRuns/Wins 증가) "후" 상태로 전달.
    public static class AchievementService
    {
        //# 신규 달성 목록 반환 + 프로필에 보상 소울/달성 플래그 즉시 반영.
        public static List<AchievementDef> Evaluate(MetaProfile profile, RunSummary run, MetaConfig cfg)
        {
            List<AchievementDef> achieved = new List<AchievementDef>();
            if (profile == null || run == null || cfg == null)
                return achieved;

            foreach (AchievementDef def in cfg.Achievements)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    continue;
                if (profile.AchievedIds.Contains(def.Id))
                    continue;
                if (IsSatisfied(def, profile, run) == false)
                    continue;

                profile.AchievedIds.Add(def.Id);
                profile.Souls += def.RewardSouls;
                achieved.Add(def);
            }
            return achieved;
        }

        private static bool IsSatisfied(AchievementDef def, MetaProfile profile, RunSummary run)
        {
            switch (def.Condition)
            {
                case EAchievementCondition.FirstWin:
                    return profile.TotalWins >= 1;
                case EAchievementCondition.WinUnderSeconds:
                    return run.Result == BattleResult.Win && run.DeathTime < def.Threshold;
                case EAchievementCondition.TotalWins:
                    return profile.TotalWins >= def.Threshold;
                case EAchievementCondition.TotalRuns:
                    return profile.TotalRuns >= def.Threshold;
                case EAchievementCondition.SynergyTierReached:
                    return run.MaxSynergyTier >= def.Threshold;
                default:
                    return false;
            }
        }
    }
}
