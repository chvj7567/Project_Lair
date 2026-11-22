using System;

namespace Lair.Data
{
    //# EnhanceTier.json — 공통 상점 티어 단계. tier 는 1~3, 기본은 구매 행이 없다.
    [Serializable]
    public class EnhanceTierRow
    {
        public int tier;
        public int levelEquivalent;
        public int soulCost;
    }

    public sealed class EnhanceTierTable
    {
        public const int MaxTier = 3;
        public const int MaxStoredLevel = 5;

        //# 이전 저장값은 외형 티어로 읽고, 구매 결과는 각 티어에서 가능한 가장 높은 기존 저장값을 쓴다.
        private static readonly int[] TierByStoredLevel = { 0, 1, 1, 2, 2, 3 };
        private static readonly int[] StoredLevelByTier = { 0, 2, 4, 5 };
        private readonly int[] _soulCosts = new int[MaxTier + 1];

        public static int TierOfLevel(int level) => TierByStoredLevel[Math.Max(0, Math.Min(level, MaxStoredLevel))];

        public static int LevelEquivalent(int tier) => StoredLevelByTier[Math.Max(0, Math.Min(tier, MaxTier))];

        public bool TryGetStep(int tier, out int levelEquivalent, out int soulCost)
        {
            levelEquivalent = 0;
            soulCost = 0;
            if (tier < 1 || tier > MaxTier || _soulCosts[tier] == 0)
                return false;
            levelEquivalent = LevelEquivalent(tier);
            soulCost = _soulCosts[tier];
            return true;
        }

        public static EnhanceTierTable Build(EnhanceTierRow[] rows, Action<string> warn = null)
        {
            EnhanceTierTable result = new EnhanceTierTable();
            bool[] seen = new bool[MaxTier + 1];
            foreach (EnhanceTierRow row in rows ?? Array.Empty<EnhanceTierRow>())
            {
                if (row == null || row.tier < 1 || row.tier > MaxTier)
                {
                    warn?.Invoke("[EnhanceTier] T1~T3 범위 밖의 행");
                    continue;
                }
                if (seen[row.tier])
                {
                    result._soulCosts[row.tier] = 0;
                    continue;
                }
                seen[row.tier] = true;
                if (row.levelEquivalent == LevelEquivalent(row.tier) && row.soulCost > 0)
                {
                    result._soulCosts[row.tier] = row.soulCost;
                }
            }
            for (int tier = 1; tier <= MaxTier; ++tier)
            {
                if (result._soulCosts[tier] == 0)
                {
                    warn?.Invoke($"[EnhanceTier] T{tier} 단계 설정 누락, 중복 또는 값 오류 — 해당 구매 비활성");
                }
            }
            return result;
        }
    }
}
