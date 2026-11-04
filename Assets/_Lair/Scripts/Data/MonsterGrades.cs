using System.Collections.Generic;

namespace Lair.Data
{
    //# EMonster 값 → 기준 몬스터(같은 종족 1등급)·등급 해석 (monster-grades §6). 데이터 로드 전에도 쓰도록 enum 순서로만 계산한다.
    //# EMonster 순서 = 기준 6종, 그 뒤 기준 6종 순 x 등급 2~5 (CommonEnum append-only). Race_Monster 로드 검증이 이 해석과 대조한다.
    public static class MonsterGrades
    {
        public const int MinGrade = 1;
        public const int MaxGrade = 5;
        private const int BaseCount = 6;
        private const int GradesPerBase = MaxGrade - MinGrade;

        //# 기준 몬스터 6종 — 종족 단위 순회(상점 행·도감·육성 데이터 검증·전투 배율 접기)는 Enum.GetValues 대신 이것을 돈다.
        public static readonly IReadOnlyList<EMonster> BaseMonsters = new[]
        {
            EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom,
        };

        public static bool IsBase(EMonster monster) => (int)monster >= 0 && (int)monster < BaseCount;

        //# 범위 밖 값(음수 sentinel 등)은 그대로 돌려준다.
        public static EMonster BaseOf(EMonster monster)
        {
            int value = (int)monster;
            if (value < BaseCount)
                return monster;
            return (EMonster)((value - BaseCount) / GradesPerBase);
        }

        public static int GradeOf(EMonster monster)
        {
            int value = (int)monster;
            if (value < BaseCount)
                return MinGrade;
            return (value - BaseCount) % GradesPerBase + MinGrade + 1;
        }

        //# 기준 몬스터 + 등급 → 그 등급 몬스터. grade 가 범위 밖이면 기준 몬스터.
        public static EMonster Of(EMonster baseMonster, int grade)
        {
            EMonster root = BaseOf(baseMonster);
            if (grade <= MinGrade || grade > MaxGrade)
                return root;
            return (EMonster)(BaseCount + (int)root * GradesPerBase + grade - MinGrade - 1);
        }

        //# 같은 종족(같은 기준 몬스터)인가 — 카드 대상·스포너 매칭·버프 대상 비교는 이 함수로 한다.
        public static bool SameSpecies(EMonster a, EMonster b) => BaseOf(a) == BaseOf(b);
    }
}
