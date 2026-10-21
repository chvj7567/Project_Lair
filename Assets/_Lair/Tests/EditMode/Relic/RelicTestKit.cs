using System;
using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 본격 스위트 공용 — 출하 JSON 10종을 변경 가능한 리스트로 읽어 한 곳만 비틀어 DB 를 만든다 (위반 케이스·하한 케이스용).
    public class RelicDataSet
    {
        public List<RelicRow> Relics;
        public List<Relic_EffectRow> Effects;
        public List<Relic_Effect_ParamRow> Params;
        public List<Relic_DropRow> Drops;
        public List<RelicRarityRow> Rarities;
        public List<RelicStackRow> Stacks;
        public RelicConfigRow Config;
        public List<Stage_RelicRow> StageChances;
        public List<Difficulty_RelicRow> DifficultyChances;
        public List<Difficulty_Relic_RarityRow> DifficultyRarities;

        private static List<T> Load<T>(string name)
        {
            return new List<T>(JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(WorldMapTestSupport.JsonDir + name + ".json")));
        }

        public static RelicDataSet Shipped()
        {
            return new RelicDataSet
            {
                Relics = Load<RelicRow>("Relic"),
                Effects = Load<Relic_EffectRow>("Relic_Effect"),
                Params = Load<Relic_Effect_ParamRow>("Relic_Effect_Param"),
                Drops = Load<Relic_DropRow>("Relic_Drop"),
                Rarities = Load<RelicRarityRow>("RelicRarity"),
                Stacks = Load<RelicStackRow>("RelicStack"),
                Config = JsonUtility.FromJson<RelicConfigRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "RelicConfig.json")),
                StageChances = Load<Stage_RelicRow>("Stage_Relic"),
                DifficultyChances = Load<Difficulty_RelicRow>("Difficulty_Relic"),
                DifficultyRarities = Load<Difficulty_Relic_RarityRow>("Difficulty_Relic_Rarity"),
            };
        }

        //# 챕터 DB·Strings_Ko 교차 검증 포함 (출하 로더와 같은 조건).
        public RelicDatabase Build()
        {
            return RelicDatabase.Build(
                Relics.ToArray(), Effects.ToArray(), Params.ToArray(), Drops.ToArray(), Rarities.ToArray(), Stacks.ToArray(),
                Config, StageChances.ToArray(), DifficultyChances.ToArray(), DifficultyRarities.ToArray(),
                WorldMapTestSupport.FullDb(), id => WorldMapTestSupport.Text(id).Length > 0);
        }

        public RelicDatabase BuildWithoutCrossCheck()
        {
            return RelicDatabase.Build(
                Relics.ToArray(), Effects.ToArray(), Params.ToArray(), Drops.ToArray(), Rarities.ToArray(), Stacks.ToArray(),
                Config, StageChances.ToArray(), DifficultyChances.ToArray(), DifficultyRarities.ToArray());
        }

        public Relic_Effect_ParamRow Param(string relicId, int effectIndex, string key)
        {
            Relic_Effect_ParamRow row = Params.Find(p => p.relicId == relicId && p.effectIndex == effectIndex && p.paramKey == key);
            Assert.IsNotNull(row, $"파라미터 행 {relicId}/{effectIndex}/{key} 없음");
            return row;
        }

        public RelicRow Relic(string relicId)
        {
            RelicRow row = Relics.Find(r => r.relicId == relicId);
            Assert.IsNotNull(row, $"유물 행 {relicId} 없음");
            return row;
        }
    }

    //# 순서대로 값을 돌려주는 난수원 — 고갈되면 예외(예상보다 많이 굴렸음을 즉시 드러낸다). Calls 로 호출 횟수를 검증한다.
    public class ScriptedRng
    {
        private readonly Queue<float> _values;

        public int Calls { get; private set; }

        public ScriptedRng(params float[] values)
        {
            _values = new Queue<float>(values);
        }

        public float Next()
        {
            Calls++;
            if (_values.Count == 0)
                throw new InvalidOperationException("ScriptedRng 고갈 — 난수를 예상보다 많이 요청함");
            return _values.Dequeue();
        }
    }

    //# 유물 테스트 베이스 — 난이도 베이스(DifficultyTable.Current · CHText.StringProvider · 챕터 정적상태)에 RelicDatabase.Current 를 더해 격리한다.
    public abstract class RelicStaticStateTestBase : DifficultyStaticStateTestBase
    {
        private RelicDatabase _prevRelicDb;

        [SetUp]
        public void 유물_정적상태를_저장하고_초기화한다()
        {
            _prevRelicDb = RelicDatabase.Current;
            RelicDatabase.Current = null;
        }

        [TearDown]
        public void 유물_정적상태를_원복한다()
        {
            RelicDatabase.Current = _prevRelicDb;
        }

        protected static RelicDatabase Db()
        {
            return RelicTestSupport.ShippedDb();
        }

        protected static MetaProfile ProfileWith(params string[] relicIds)
        {
            MetaProfile profile = new MetaProfile();
            foreach (string id in relicIds)
            {
                profile.AddRelic(id);
            }
            return profile;
        }

        protected static void SetStack(MetaProfile profile, string relicId, int stack)
        {
            profile.Relics.Add(new RelicEntry { RelicId = relicId, Stack = stack });
        }

        //# Color → 8비트 비교 (부동소수 비교 오차 회피).
        protected static void AssertColor(byte r, byte g, byte b, Color actual, string message = null)
        {
            Color32 c = actual;
            Assert.AreEqual(r, c.r, message);
            Assert.AreEqual(g, c.g, message);
            Assert.AreEqual(b, c.b, message);
        }
    }
}
