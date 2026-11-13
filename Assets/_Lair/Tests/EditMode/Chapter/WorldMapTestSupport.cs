using System.Collections.Generic;
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
        public static ChapterDatabase ShippedDb()
        {
            ChapterDatabase db = FullDb();
            StageProgress.ConfigureChapterCount(db.ChapterCount);
            return db;
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
                Load<HeroArchetype_GuardRow>("HeroArchetype_Guard"), Load<HeroArchetype_DebuffResistRow>("HeroArchetype_DebuffResist"),
                ShippedBalance().PassiveThresholds);
        }

        //# shipped BalanceConfig.json + MonsterStat.json 조립 — 로더와 같은 검증을 거친다 (so-to-json-migration §7.5).
        public static BalanceConfig ShippedBalance()
        {
            return BalanceConfig.Build(
                UnityEngine.JsonUtility.FromJson<BalanceConfigRow>(File.ReadAllText(JsonDir + "BalanceConfig.json")),
                Load<MonsterStatRow>("MonsterStat"));
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
