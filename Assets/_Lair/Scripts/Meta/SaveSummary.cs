using Lair.Data;

namespace Lair.Meta
{
    //# 계정 충돌 비교 칸용 세이브 요약 — 영주 Lv + 소울. 프로필 전체를 UI 에 넘기지 않는다(Rule 02 §6).
    public class SaveSummary
    {
        public int LordLevel;
        public int Souls;

        //# 프로필 → 요약. profile 또는 config 가 없으면 null(비교 칸을 숨긴다).
        public static SaveSummary From(MetaProfile profile, MetaConfig config)
        {
            if (profile == null || config == null)
                return null;
            return new SaveSummary
            {
                LordLevel = LordLevelService.LevelFromXp(profile.LordXp, config),
                Souls = profile.Souls,
            };
        }

        //# "영주 Lv 7 · 1,240 소울"
        public string ToDisplayText()
        {
            return $"영주 Lv {LordLevel} · {Souls:N0} 소울";
        }
    }
}
