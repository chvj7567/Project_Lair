using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using Lair.Meta;

namespace Lair.Village
{
    //# 도감 해금 규칙 순수 함수 (monster-codex §3). 저장 필드 없이 기존 프로필 값에서 파생한다.
    public static class CodexRules
    {
        //# 상점 강화 최대 Lv — EnhanceLevelVisual/MonsterEnhancementVisual 의 5 와 같은 값(표 길이 6).
        public const int MaxLevel = 5;

        public static int GradeCount => MonsterGrades.MaxGrade - MonsterGrades.MinGrade + 1;

        //# 티어 수 = 강화 Lv→티어 표에서 도출(기본 + T1~T3 = 4).
        public static int TierCount => TierOf(MaxLevel) + 1;

        public static int ClampLevel(int level) => level < 0 ? 0 : (level > MaxLevel ? MaxLevel : level);

        public static int TierOf(int level) => MonsterEnhancementVisual.TierOfLevel(ClampLevel(level));

        //# 티어 t 가 시작되는 가장 낮은 강화 Lv (T1=1, T2=3, T3=5).
        public static int MinLevelOfTier(int tier)
        {
            for (int level = 0; level <= MaxLevel; ++level)
            {
                if (TierOf(level) >= tier)
                    return level;
            }
            return MaxLevel;
        }

        //# 티어 t 에 속하는 가장 높은 강화 Lv (T0=0, T1=2, T2=4, T3=5).
        public static int MaxLevelOfTier(int tier)
        {
            int last = 0;
            for (int level = 0; level <= MaxLevel; ++level)
            {
                if (TierOf(level) == tier)
                {
                    last = level;
                }
            }
            return last;
        }

        //# open(race, g, t) = seen AND g <= G AND t <= T.
        public static bool IsOpen(bool seen, int ownedGrade, int level, int grade, int tier)
            => seen && grade <= ownedGrade && tier <= TierOf(level);

        public static int CellIndex(int grade, int tier) => (grade - MonsterGrades.MinGrade) * TierCount + tier;

        //# 칸 몸 크기(px) = 기준 px x 등급 sizeMul.
        public static float BodyPx(int basePx, float sizeMul) => basePx * sizeMul;

        //# 종족(기준 몬스터)의 열린 칸 인덱스 집합. profile/formation 이 없거나 종족을 못 찾으면 빈 집합.
        public static HashSet<int> OpenSetOf(MetaProfile profile, FormationDatabase formation, EMonster baseMonster)
        {
            HashSet<int> open = new HashSet<int>();
            if (profile == null || formation == null || formation.TryGetRace(baseMonster, out ERace race) == false)
                return open;
            bool seen = profile.SeenMonsters != null && profile.SeenMonsters.Contains(baseMonster.ToString());
            int owned = profile.GetRaceGrade(race);
            int level = profile.GetShopLevel("Enhance_" + baseMonster);
            for (int grade = MonsterGrades.MinGrade; grade <= MonsterGrades.MaxGrade; ++grade)
            {
                for (int tier = 0; tier < TierCount; ++tier)
                {
                    if (IsOpen(seen, owned, level, grade, tier))
                    {
                        open.Add(CellIndex(grade, tier));
                    }
                }
            }
            return open;
        }
    }

    //# NEW 배지용 세션 스냅샷 (monster-codex §6.2) — 앱 실행 동안 메모리(정적), 저장 안 함. 종족(기준 몬스터)별 "이미 본 열린 칸" 집합.
    public static class CodexSession
    {
        private static readonly Dictionary<EMonster, HashSet<int>> Seen = new Dictionary<EMonster, HashSet<int>>();
        private static bool _captured;

        public static bool Captured => _captured;

        //# 마을 첫 로드 때 한 번만(전투 복귀 때는 다시 잡지 않는다). 편성 데이터가 없으면 종족 판정이 불가라 잡지 않고 다음 기회로 미룬다.
        public static void EnsureCaptured(MetaProfile profile, FormationDatabase formation)
        {
            if (_captured || profile == null || formation == null)
                return;
            foreach (EMonster race in MonsterGrades.BaseMonsters)
            {
                Seen[race] = CodexRules.OpenSetOf(profile, formation, race);
            }
            _captured = true;
        }

        //# 스냅샷이 없으면 null (신규 칸 없음으로 취급).
        public static HashSet<int> Get(EMonster race) => Seen.TryGetValue(race, out HashSet<int> set) ? set : null;

        public static void Set(EMonster race, HashSet<int> open)
        {
            if (_captured == false)
                return;
            Seen[race] = new HashSet<int>(open);
        }

        public static void ResetForTest()
        {
            Seen.Clear();
            _captured = false;
        }
    }
}
