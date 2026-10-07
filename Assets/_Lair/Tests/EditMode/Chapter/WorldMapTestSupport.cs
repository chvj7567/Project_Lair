using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
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

        public static ChapterDatabase ShippedDb()
        {
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter"), Load<StageRow>("Stage"), Load<Stage_RuleRow>("Stage_Rule"),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param"), Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"),
                Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish"));
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
