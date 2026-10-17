using Lair.Data;

namespace Lair.Card
{
    //# 시너지 진행도 산식(기획서 card-synergy-indicator §2) — View 밖 순수 계산. 임계 원본은 BuildSynergyService.
    public static class SynergyProgress
    {
        public const int TrackLength = 7;
        public const float DimOpacity = 0.55f;

        //# 활성 Tier — 임계(3/5/7) 이상 개수. 0~3.
        public static int ActiveTier(int count)
        {
            int tier = 0;
            if (count >= BuildSynergyService.Tier1Threshold)
                ++tier;
            if (count >= BuildSynergyService.Tier2Threshold)
                ++tier;
            if (count >= BuildSynergyService.Tier3Threshold)
                ++tier;
            return tier;
        }

        //# 다음 임계 — count 보다 큰 첫 임계. Tier3 도달이면 -1.
        public static int NextThreshold(int count)
        {
            if (count < BuildSynergyService.Tier1Threshold)
                return BuildSynergyService.Tier1Threshold;
            if (count < BuildSynergyService.Tier2Threshold)
                return BuildSynergyService.Tier2Threshold;
            if (count < BuildSynergyService.Tier3Threshold)
                return BuildSynergyService.Tier3Threshold;
            return -1;
        }

        //# 장수 텍스트 — "2/3" · "4/5 T1" · "9+ T3".
        public static string CountText(int count)
        {
            int tier = ActiveTier(count);
            int next = NextThreshold(count);
            if (next < 0)
                return $"{count}+ T{tier}";
            return tier > 0 ? $"{count}/{next} T{tier}" : $"{count}/{next}";
        }

        //# 행 불투명도 — 0장만 흐림.
        public static float Opacity(int count)
        {
            return count <= 0 ? DimOpacity : 1f;
        }

        //# 왼쪽 축 색띠 — Tier 1 이상일 때만.
        public static bool HasStrip(int count)
        {
            return ActiveTier(count) >= 1;
        }

        //# 트랙 채움 칸 수 = min(count, 7).
        public static int FilledCells(int count)
        {
            if (count <= 0)
                return 0;
            return count < TrackLength ? count : TrackLength;
        }

        //# 트랙 칸(0-base)이 Tier 칸(3·5·7번째)인지.
        public static bool IsTierCell(int index)
        {
            return index == BuildSynergyService.Tier1Threshold - 1
                || index == BuildSynergyService.Tier2Threshold - 1
                || index == BuildSynergyService.Tier3Threshold - 1;
        }

        //# 임계 새로 돌파 여부 — prev < T <= count 인 임계 존재.
        public static bool CrossedThreshold(int prev, int count)
        {
            return prev < BuildSynergyService.Tier1Threshold && count >= BuildSynergyService.Tier1Threshold
                || prev < BuildSynergyService.Tier2Threshold && count >= BuildSynergyService.Tier2Threshold
                || prev < BuildSynergyService.Tier3Threshold && count >= BuildSynergyService.Tier3Threshold;
        }
    }
}
