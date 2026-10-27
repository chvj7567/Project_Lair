using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 선택 둥지에 종을 넣으려 할 때의 판정 (dungeon-formation §7.4).
    public enum EFormationPlace
    {
        Ok,
        Same,        //# 이미 같은 종 — 무동작
        OverCost,    //# 코스트 상한 초과
        OverSpecies, //# 종당 최대 초과
    }

    //# 던전 편성 순수 규칙 (dungeon-formation §2.5, §6.3). Unity 비의존 정적 함수 — 상한·종당 최대·외부 고정 편성을 인자로 받는다(탑·데일리 훅, §10).
    public static class FormationRules
    {
        //# 편성 팝업이 열리는가 — ClearedStage >= unlockClearedStage.
        public static bool IsUnlocked(FormationConfigRow config, int clearedStage)
        {
            return clearedStage >= config.unlockClearedStage;
        }

        //# 현재 해금 단계의 종당 최대 — 1장 보스 처치(expandClearedStage) 후 확장.
        public static int MaxPerSpecies(FormationConfigRow config, int clearedStage)
        {
            return clearedStage >= config.expandClearedStage ? config.maxPerSpeciesExpanded : config.maxPerSpeciesBase;
        }

        public static int CostSum(IReadOnlyList<EMonster> formation, Func<EMonster, int> costOf)
        {
            int sum = 0;
            for (int i = 0; i < formation.Count; ++i)
            {
                sum += costOf(formation[i]);
            }
            return sum;
        }

        public static int CountOf(IReadOnlyList<EMonster> formation, EMonster monster)
        {
            int count = 0;
            for (int i = 0; i < formation.Count; ++i)
            {
                if (formation[i] == monster)
                {
                    ++count;
                }
            }
            return count;
        }

        //# 유효하지 않은 이유(진단 문구), 유효하면 null. costOf 가 0 이하를 돌려주면 알 수 없는 종.
        public static string Describe(IReadOnlyList<EMonster> formation, int costCap, int maxPerSpecies, Func<EMonster, int> costOf)
        {
            if (formation == null || formation.Count != FormationDatabase.SlotCount)
                return $"슬롯 수가 {FormationDatabase.SlotCount} 가 아님";
            for (int i = 0; i < formation.Count; ++i)
            {
                if (costOf(formation[i]) <= 0)
                    return $"슬롯 {i + 1} 의 종 {formation[i]} 을(를) 편성할 수 없음";
            }
            int sum = CostSum(formation, costOf);
            if (sum > costCap)
                return $"코스트 합 {sum} 이 상한 {costCap} 초과";
            foreach (EMonster monster in new HashSet<EMonster>(formation))
            {
                if (CountOf(formation, monster) > maxPerSpecies)
                    return $"{monster} 가 종당 최대 {maxPerSpecies} 초과";
            }
            return null;
        }

        public static bool IsValid(IReadOnlyList<EMonster> formation, int costCap, int maxPerSpecies, Func<EMonster, int> costOf)
        {
            return Describe(formation, costCap, maxPerSpecies, costOf) == null;
        }

        //# slotIndex(0~5) 둥지의 종을 monster 로 바꿀 수 있는가.
        public static EFormationPlace CanPlace(IReadOnlyList<EMonster> formation, int slotIndex, EMonster monster, int costCap, int maxPerSpecies, Func<EMonster, int> costOf)
        {
            if (formation[slotIndex] == monster)
                return EFormationPlace.Same;
            int newSum = CostSum(formation, costOf) - costOf(formation[slotIndex]) + costOf(monster);
            if (newSum > costCap)
                return EFormationPlace.OverCost;
            if (CountOf(formation, monster) + 1 > maxPerSpecies)
                return EFormationPlace.OverSpecies;
            return EFormationPlace.Ok;
        }

        //# 봉인할 둥지 인덱스 (§5.1-C) — species 를 출력하는 후보 중 슬롯 번호 최소, 없으면 코스트 최고(동률이면 슬롯 번호 최소). 후보가 없으면 -1.
        //# isCandidate: 이미 봉인된 둥지 등을 제외하는 필터(null 이면 전부 후보).
        public static int PickSealedSlot(IReadOnlyList<EMonster> outputs, EMonster species, Func<EMonster, int> costOf, Func<int, bool> isCandidate = null)
        {
            int best = -1;
            int bestCost = int.MinValue;
            for (int i = 0; i < outputs.Count; ++i)
            {
                if (isCandidate != null && isCandidate(i) == false)
                    continue;
                if (outputs[i] == species)
                    return i;
                int cost = costOf != null ? costOf(outputs[i]) : 0;
                if (cost > bestCost)
                {
                    bestCost = cost;
                    best = i;
                }
            }
            return best;
        }
    }
}
