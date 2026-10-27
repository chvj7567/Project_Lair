using System;
using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    //# 편성 세이브 행(MetaProfile.Formation) 변환 + 전투 EffectiveFormation 결정 (dungeon-formation §3.1, §6.3). 데이터 규칙은 Lair.Data.FormationRules 소유 - 의존은 Meta -> Data 한 방향.
    public static class FormationSave
    {
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
            if (FormationRules.IsUnlocked(db.Config, clearedStage) == false || saved == null || saved.Count == 0)
                return db.DefaultFormation();

            EMonster[] candidate = FromSaved(saved);
            int costCap = costCapOverride > 0 ? costCapOverride : db.Config.costCap;
            int maxPerSpecies = maxPerSpeciesOverride > 0 ? maxPerSpeciesOverride : FormationRules.MaxPerSpecies(db.Config, clearedStage);
            string issue = candidate == null ? "저장 편성 모양이 올바르지 않음" : FormationRules.Describe(candidate, costCap, maxPerSpecies, db.CostOf);
            if (issue == null)
                return candidate;
            warn?.Invoke($"[Formation] 저장 편성이 무효({issue}) — 기본 편성으로 시작");
            return db.DefaultFormation();
        }

    }
}
