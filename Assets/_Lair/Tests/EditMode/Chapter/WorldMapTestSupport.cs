using System.Collections.Generic;
using System.Linq;
using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 월드맵(슬라이스 c) 테스트 공용 — shipped JSON 기반 ChapterDatabase 와 실제 Strings_Ko 조회.
    public static class WorldMapTestSupport
    {
        public const string JsonDir = "Assets/_Lair/Data/Json/";

        private static Dictionary<int, string> _strings;

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        //# 실서비스와 같이 로드 직후 진행 범위(MaxStage)를 챕터 수에 맞춘다(ChapterDatabaseLoader 동작) — 호출 테스트는 정적 상태 격리(ChapterStaticStateTestBase)로 원복한다.
        //# 개방된 챕터(ChapterGate.PlayableChapterCount)까지만 담는다 — 챕터 5 는 게이트 상향(분할 d) 전까지 UI 테스트 범위 밖. (d) 에서 FullDb 기반으로 원복.
        public static ChapterDatabase ShippedDb()
        {
            ChapterDatabase db = PlayableDb();
            StageProgress.ConfigureChapterCount(db.ChapterCount);
            return db;
        }

        //# ConfigureChapterCount 를 건드리지 않는 개방 챕터 한정 DB.
        public static ChapterDatabase PlayableDb()
        {
            int max = ChapterGate.PlayableChapterCount;
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter").Where(r => r.chapterId <= max).ToArray(),
                Load<StageRow>("Stage").Where(r => r.chapterId <= max).ToArray(),
                Load<Stage_RuleRow>("Stage_Rule").Where(r => r.chapterId <= max).ToArray(),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param").Where(r => r.chapterId <= max).ToArray(),
                Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger").Where(r => r.chapterId <= max).ToArray(),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"),
                Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish"),
                Load<HeroArchetype_ChargeRow>("HeroArchetype_Charge"), Load<HeroArchetype_CleaveRow>("HeroArchetype_Cleave"),
                Load<HeroArchetype_GuardRow>("HeroArchetype_Guard"), Load<HeroArchetype_DebuffResistRow>("HeroArchetype_DebuffResist"));
        }

        //# shipped 데이터 전체 — ConfigureChapterCount 를 건드리지 않는다(정적 상태 불변).
        public static ChapterDatabase FullDb()
        {
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter"), Load<StageRow>("Stage"), Load<Stage_RuleRow>("Stage_Rule"),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param"), Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"),
                Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish"),
                Load<HeroArchetype_ChargeRow>("HeroArchetype_Charge"), Load<HeroArchetype_CleaveRow>("HeroArchetype_Cleave"),
                Load<HeroArchetype_GuardRow>("HeroArchetype_Guard"), Load<HeroArchetype_DebuffResistRow>("HeroArchetype_DebuffResist"));
        }

        public static IReadOnlyDictionary<int, string> Strings()
        {
            if (_strings == null)
            {
                Dictionary<int, string> table = new Dictionary<int, string>();
                foreach (StringEntry entry in Load<StringEntry>("Strings_Ko"))
                {
                    table[entry.id] = entry.text;
                }
                _strings = table;
            }
            return _strings;
        }

        //# 실제 Strings_Ko — id 누락이면 빈 문자열.
        public static string Text(int id)
        {
            return Strings().TryGetValue(id, out string text) ? text : string.Empty;
        }
    }
}
