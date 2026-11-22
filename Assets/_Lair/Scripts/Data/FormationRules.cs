using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 던전 편성 순수 규칙 (dungeon-formation §0.3·§0.6·§0.7). Unity 비의존 정적 함수.
    //# ownedGradeOf: 종족 → 보유 최고 등급(1~5, MetaProfile.RaceGrades). null 이면 모든 종족 1등급.
    public static class FormationRules
    {
        //# 1-1 클리어 등 최소 진행 — ClearedStage >= unlockClearedStage.
        public static bool IsUnlocked(FormationConfigRow config, int clearedStage)
        {
            return clearedStage >= config.unlockClearedStage;
        }

        //# 고를 수 있는 몬스터가 2개 이상인 종족이 하나라도 있는가 (§0.3 노출 규칙 두 번째 조건).
        public static bool HasChoice(FormationDatabase db, Func<ERace, int> ownedGradeOf)
        {
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                ERace race = db.SlotRace(i);
                if (db.OwnedMonstersOf(race, OwnedGrade(race, ownedGradeOf)).Count >= 2)
                    return true;
            }
            return false;
        }

        //# "방어하기" 가 편성 팝업을 여는가 — 해금 진행 + 선택지 있는 종족 존재 (U-D A).
        public static bool ShouldShowPopup(FormationDatabase db, int clearedStage, Func<ERace, int> ownedGradeOf)
        {
            return db != null && IsUnlocked(db.Config, clearedStage);
        }

        //# 슬롯 index 에 monster 를 둘 수 있는가 — 보유 등급.
        public static bool IsAllowed(FormationDatabase db, int slotIndex, EMonster monster, Func<ERace, int> ownedGradeOf)
        {
            if (slotIndex < 0 || slotIndex >= FormationDatabase.SlotCount || db.TryGetRace(monster, out ERace race) == false)
                return false;
            return MonsterGrades.GradeOf(monster) <= OwnedGrade(race, ownedGradeOf);
        }

        //# 유효하지 않은 이유(진단 문구), 유효하면 null.
        public static string Describe(IReadOnlyList<EMonster> formation, FormationDatabase db, Func<ERace, int> ownedGradeOf)
        {
            if (formation == null || formation.Count != FormationDatabase.SlotCount)
                return $"슬롯 수가 {FormationDatabase.SlotCount} 가 아님";
            for (int i = 0; i < formation.Count; ++i)
            {
                if (IsAllowed(db, i, formation[i], ownedGradeOf) == false)
                    return $"슬롯 {i + 1} 의 {formation[i]} 은(는) {db.SlotRace(i)} 의 보유 몬스터가 아님";
            }
            return null;
        }

        //# 무효 슬롯을 그 종족의 보유 최고 등급 몬스터로 바꾼 복사본 (§0.6 읽는 순간 판정). repaired = 바꾼 슬롯 수.
        public static EMonster[] Repair(IReadOnlyList<EMonster> formation, FormationDatabase db, Func<ERace, int> ownedGradeOf, out int repaired)
        {
            EMonster[] fixedFormation = db.DefaultFormation(ownedGradeOf);
            repaired = 0;
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                if (formation != null && i < formation.Count && IsAllowed(db, i, formation[i], ownedGradeOf))
                {
                    fixedFormation[i] = formation[i];
                }
                else
                {
                    ++repaired;
                }
            }
            return fixedFormation;
        }

        //# 봉인할 둥지 인덱스 (§0.7) — species 와 같은 종족을 출력하는 후보 중 슬롯 번호 최소. 없으면 -1.
        //# isCandidate: 이미 봉인된 둥지 등을 제외하는 필터(null 이면 전부 후보).
        public static int PickSealedSlot(IReadOnlyList<EMonster> outputs, EMonster species, Func<int, bool> isCandidate = null)
        {
            for (int i = 0; i < outputs.Count; ++i)
            {
                if (isCandidate != null && isCandidate(i) == false)
                    continue;
                if (MonsterGrades.SameSpecies(outputs[i], species))
                    return i;
            }
            return -1;
        }

        //# 우선 대상 종, 해당 종의 원래 둥지, 그 외 채운 둥지 순으로 한 곳을 고른다.
        public static int PickSealedSlot(IReadOnlyList<EMonster?> outputs, EMonster species, FormationDatabase db, Func<int, bool> isCandidate = null)
        {
            int firstFilled = -1;
            int originalSlot = db != null && db.TryGetRace(species, out ERace race) ? db.SlotOf(race) : -1;
            bool originalAvailable = false;
            for (int i = 0; i < outputs.Count; ++i)
            {
                if (outputs[i].HasValue == false || (isCandidate != null && isCandidate(i) == false))
                    continue;
                if (MonsterGrades.SameSpecies(outputs[i].Value, species))
                    return i;
                if (firstFilled < 0)
                    firstFilled = i;
                if (i == originalSlot)
                    originalAvailable = true;
            }
            return originalAvailable ? originalSlot : firstFilled;
        }

        private static int OwnedGrade(ERace race, Func<ERace, int> ownedGradeOf)
        {
            return ownedGradeOf != null ? ownedGradeOf(race) : MonsterGrades.MinGrade;
        }
    }
}
