using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 누적 XP → 영주 레벨/진행률 계산 + 보상 트랙 자동 수령 (기획서 §4).
    //# 필요 XP = floor(XpPerLevelBase × XpGrowth^(Lv−1)) — 감산 루프, 상한 Lv99 가드.
    public static class LordLevelService
    {
        public const int MaxLevel = 99;

        public static int LevelFromXp(int xp, MetaConfig cfg)
        {
            if (cfg == null)
                return 1;

            int level = 1;
            int remain = xp;
            while (level < MaxLevel)
            {
                int need = NeedForLevel(level, cfg);
                if (remain < need)
                    break;
                remain -= need;
                level++;
            }
            return level;
        }

        //# 현재 레벨 구간 내 소모/필요 비율 0~1. 보상 트랙 만렙 도달 후엔 1 고정 (기획서 §10.7).
        public static float ProgressInLevel(int xp, MetaConfig cfg)
        {
            if (cfg == null)
                return 0f;

            //# 트랙 만렙 = LordRewards 최고 Level (하드코딩 금지). 트랙 미정의(0)면 Lv99 상한만 적용.
            int trackMax = TrackMaxLevel(cfg);

            int level = 1;
            int remain = xp;
            while (level < MaxLevel)
            {
                if (trackMax > 0 && level >= trackMax)
                    return 1f;
                int need = NeedForLevel(level, cfg);
                if (remain < need)
                    return need > 0 ? Mathf.Clamp01((float)remain / need) : 0f;
                remain -= need;
                level++;
            }
            return 1f;
        }

        //# 보상 트랙 만렙 — cfg.LordRewards 의 최고 Level. 트랙 미정의 시 0 (게이지 고정 비적용).
        public static int TrackMaxLevel(MetaConfig cfg)
        {
            if (cfg == null)
                return 0;

            int max = 0;
            foreach (LordRewardDef def in cfg.LordRewards)
            {
                if (def == null)
                    continue;
                if (def.Level > max)
                {
                    max = def.Level;
                }
            }
            return max;
        }

        //# 영주 보상 자동 수령 — LordRewardGrantedLevel+1 ~ 현재 레벨 구간 합산 지급, 멱등 (기획서 §4.4).
        //# 반환값 = 이번 정산 지급 소울 합 (ResultPopupArg.LordRewardSouls).
        public static int ClaimRewards(MetaProfile profile, MetaConfig cfg)
        {
            if (profile == null || cfg == null)
                return 0;

            int current = LevelFromXp(profile.LordXp, cfg);
            if (current <= profile.LordRewardGrantedLevel)
                return 0;

            int granted = 0;
            foreach (LordRewardDef def in cfg.LordRewards)
            {
                if (def == null || def.IsLockedDummy)
                    continue;
                if (def.Level <= profile.LordRewardGrantedLevel || def.Level > current)
                    continue;
                granted += def.RewardSouls;
            }
            profile.LordRewardGrantedLevel = current;
            profile.Souls += granted;
            return granted;
        }

        private static int NeedForLevel(int level, MetaConfig cfg)
            => Mathf.FloorToInt(cfg.XpPerLevelBase * Mathf.Pow(cfg.XpGrowth, level - 1));
    }
}
