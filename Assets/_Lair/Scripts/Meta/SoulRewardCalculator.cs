using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    public struct SoulReward
    {
        public int Souls;
        public int Xp;
    }

    //# 런 결과 → 소울/XP 순수 계산 (spec §5.1). 수치는 MetaConfig — 기획서 §2.1 이 단일 진실.
    public static class SoulRewardCalculator
    {
        public static SoulReward Calculate(BattleResult result, float deathTime, float totalSeconds, float heroDamagedRatio, MetaConfig cfg)
        {
            if (cfg == null)
                return new SoulReward();

            if (result == BattleResult.Win)
            {
                float remain = Mathf.Max(0f, totalSeconds - deathTime);
                return new SoulReward
                {
                    Souls = cfg.WinBaseSouls + Mathf.FloorToInt(remain * cfg.WinTimeBonusPerSec),
                    Xp = cfg.WinXp,
                };
            }
            return new SoulReward
            {
                Souls = Mathf.FloorToInt(cfg.LoseMaxSouls * Mathf.Clamp01(heroDamagedRatio)),
                Xp = cfg.LoseXp,
            };
        }
    }
}
