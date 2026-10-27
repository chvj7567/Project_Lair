using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 공용 — 출하 JSON 5종을 변경 가능한 리스트로 읽어 한 곳만 비틀어 DB 를 만든다(위반 케이스용).
    public class FormationDataSet
    {
        public FormationConfigRow Config;
        public List<FormationMonsterRow> Monsters;
        public List<FormationSlotRow> Slots;
        public List<Chapter_FormationHintRow> Hints;
        public List<Card_SpeciesRow> CardSpecies;

        private static string Dir => WorldMapTestSupport.JsonDir;

        public static FormationDataSet Shipped()
        {
            return new FormationDataSet
            {
                Config = JsonUtility.FromJson<FormationConfigRow>(File.ReadAllText(Dir + "FormationConfig.json")),
                Monsters = new List<FormationMonsterRow>(JsonArrayUtility.FromJsonArray<FormationMonsterRow>(File.ReadAllText(Dir + "FormationMonster.json"))),
                Slots = new List<FormationSlotRow>(JsonArrayUtility.FromJsonArray<FormationSlotRow>(File.ReadAllText(Dir + "FormationSlot.json"))),
                Hints = new List<Chapter_FormationHintRow>(JsonArrayUtility.FromJsonArray<Chapter_FormationHintRow>(File.ReadAllText(Dir + "Chapter_FormationHint.json"))),
                CardSpecies = new List<Card_SpeciesRow>(JsonArrayUtility.FromJsonArray<Card_SpeciesRow>(File.ReadAllText(Dir + "Card_Species.json"))),
            };
        }

        //# 챕터 DB·Strings_Ko 교차 검증 포함 (출하 로더 조건).
        public FormationDatabase Build()
        {
            return FormationDatabase.Build(
                Config, Monsters.ToArray(), Slots.ToArray(), Hints.ToArray(),
                WorldMapTestSupport.FullDb().Chapters.Select(c => c.chapterId).ToList(),
                id => WorldMapTestSupport.Text(id).Length > 0);
        }

        public FormationDatabase BuildWithoutCrossCheck()
        {
            return FormationDatabase.Build(Config, Monsters.ToArray(), Slots.ToArray(), Hints.ToArray());
        }

        public FormationCardSpecies BuildCards(FormationDatabase db)
        {
            return FormationCardSpecies.Build(CardSpecies.ToArray(), db);
        }
    }

    public static class FormationTestKit
    {
        public static FormationDatabase ShippedDb()
        {
            return FormationDataSet.Shipped().Build();
        }

        public static FormationCardSpecies ShippedCards(FormationDatabase db)
        {
            return FormationDataSet.Shipped().BuildCards(db);
        }

        public static List<FormationSlotEntry> Rows(params EMonster[] monsters)
        {
            List<FormationSlotEntry> rows = new List<FormationSlotEntry>();
            for (int i = 0; i < monsters.Length; ++i)
            {
                rows.Add(new FormationSlotEntry { Slot = i + 1, Monster = monsters[i].ToString() });
            }
            return rows;
        }

        //# 기본 편성과 다른 유효 편성 (사신 2 + 저주술사 + 환령 2 + 역병귀 = 코스트 11, 종당 2).
        public static EMonster[] AlteredValid()
        {
            return new[] { EMonster.Reaper, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Phantom };
        }

        //# 코스트 14 (도깨비불 2 + 망령 + 사신 + 역병귀 + 환령) — 상한 13 초과, 종당 2 이내.
        public static EMonster[] Cost14()
        {
            return new[] { EMonster.Wisp, EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Plague, EMonster.Phantom };
        }

        public static HashSet<EMonster> Outputs(params EMonster[] monsters)
        {
            return new HashSet<EMonster>(monsters);
        }

        public static string[] Passives()
        {
            return Pool("passive");
        }

        public static string[] Actives()
        {
            return Pool("active");
        }

        private static string[] Pool(string key)
        {
            Newtonsoft.Json.Linq.JObject pools = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(WorldMapTestSupport.JsonDir + "card_pools.json"));
            return pools[key].Select(t => (string)t).ToArray();
        }

        public static ECardId Card(string name)
        {
            Assert.IsTrue(System.Enum.TryParse(name, out ECardId id), "ECardId 에 없는 카드 " + name);
            return id;
        }
    }
}
