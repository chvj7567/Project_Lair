using System;
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
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

        //# 저장 행 → 편성. 정확히 6행이고 슬롯 1~6 을 한 번씩 덮고 종이 EMonster 값명일 때만, 아니면 null(= 기본 편성/무효).
        public static EMonster[] FromSaved(IReadOnlyList<FormationSlotEntry> saved)
        {
            if (saved == null || saved.Count != FormationDatabase.SlotCount)
                return null;
            EMonster[] formation = new EMonster[FormationDatabase.SlotCount];
            bool[] seen = new bool[FormationDatabase.SlotCount];
            for (int i = 0; i < saved.Count; ++i)
            {
                FormationSlotEntry entry = saved[i];
                if (entry == null || entry.Slot < 1 || entry.Slot > FormationDatabase.SlotCount || seen[entry.Slot - 1])
                    return null;
                if (FormationDatabase.TryParseMonster(entry.Monster, out EMonster monster) == false)
                    return null;
                seen[entry.Slot - 1] = true;
                formation[entry.Slot - 1] = monster;
            }
            return formation;
        }

        //# 편성 → 저장 행. 기본 편성과 같으면 0행(기본 편성 추적), 다르면 6행 (§3.1).
        public static List<FormationSlotEntry> ToSaved(IReadOnlyList<EMonster> formation, IReadOnlyList<EMonster> defaults)
        {
            List<FormationSlotEntry> rows = new List<FormationSlotEntry>();
            bool same = true;
            for (int i = 0; i < formation.Count; ++i)
            {
                if (formation[i] != defaults[i])
                {
                    same = false;
                    break;
                }
            }
            if (same)
                return rows;
            for (int i = 0; i < formation.Count; ++i)
            {
                rows.Add(new FormationSlotEntry { Slot = i + 1, Monster = formation[i].ToString() });
            }
            return rows;
        }

        //# 저장 행 정리 대상인가 — 0행이 아니면서 FromSaved 가 거부하는 모양(부분 행·중복·미지 종·null 행).
        public static bool IsMalformedSave(IReadOnlyList<FormationSlotEntry> saved)
        {
            return saved != null && saved.Count > 0 && FromSaved(saved) == null;
        }

        //# 전투 시작 EffectiveFormation (§6.3) — 프로필을 수정하지 않는다. 데이터 미로드(db null)면 null(= 씬 인스펙터 배치 그대로).
        //# fixedFormation: 외부(데일리 등)가 넘기는 고정 편성 — 6칸이면 저장 편성 대신 그대로 쓴다. costCapOverride/maxPerSpeciesOverride: 0 이면 데이터값.
        public static EMonster[] Resolve(
            FormationDatabase db, IReadOnlyList<FormationSlotEntry> saved, int clearedStage,
            EMonster[] fixedFormation = null, int costCapOverride = 0, int maxPerSpeciesOverride = 0, Action<string> warn = null)
        {
            if (db == null)
                return null;
            if (fixedFormation != null && fixedFormation.Length == FormationDatabase.SlotCount)
                return (EMonster[])fixedFormation.Clone();
            if (IsUnlocked(db.Config, clearedStage) == false || saved == null || saved.Count == 0)
                return db.DefaultFormation();

            EMonster[] candidate = FromSaved(saved);
            int costCap = costCapOverride > 0 ? costCapOverride : db.Config.costCap;
            int maxPerSpecies = maxPerSpeciesOverride > 0 ? maxPerSpeciesOverride : MaxPerSpecies(db.Config, clearedStage);
            string issue = candidate == null ? "저장 편성 모양이 올바르지 않음" : Describe(candidate, costCap, maxPerSpecies, db.CostOf);
            if (issue == null)
                return candidate;
            warn?.Invoke($"[Formation] 저장 편성이 무효({issue}) — 기본 편성으로 시작");
            return db.DefaultFormation();
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
