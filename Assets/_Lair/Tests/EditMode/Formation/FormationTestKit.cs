using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;
using Lair.Card;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 공용 — 출하 JSON 8종을 변경 가능한 리스트로 읽어 한 곳만 비틀어 DB 를 만든다(위반 케이스용).
    public class FormationDataSet
    {
        public FormationConfigRow Config;
        public List<RaceRow> Races;
        public List<GradeRow> Grades;
        public List<Race_MonsterRow> RaceMonsters;
        public List<Grade_PromotionRow> Promotions;
        public List<FormationSlotRow> Slots;
        public List<Chapter_FormationHintRow> Hints;
        public List<Card_SpeciesRow> CardSpecies;

        private static string Dir => WorldMapTestSupport.JsonDir;

        public static FormationDataSet Shipped()
        {
            return new FormationDataSet
            {
                Config = JsonUtility.FromJson<FormationConfigRow>(File.ReadAllText(Dir + "FormationConfig.json")),
                Races = new List<RaceRow>(JsonArrayUtility.FromJsonArray<RaceRow>(File.ReadAllText(Dir + "Race.json"))),
                Grades = new List<GradeRow>(JsonArrayUtility.FromJsonArray<GradeRow>(File.ReadAllText(Dir + "Grade.json"))),
                RaceMonsters = new List<Race_MonsterRow>(JsonArrayUtility.FromJsonArray<Race_MonsterRow>(File.ReadAllText(Dir + "Race_Monster.json"))),
                Promotions = new List<Grade_PromotionRow>(JsonArrayUtility.FromJsonArray<Grade_PromotionRow>(File.ReadAllText(Dir + "Grade_Promotion.json"))),
                Slots = new List<FormationSlotRow>(JsonArrayUtility.FromJsonArray<FormationSlotRow>(File.ReadAllText(Dir + "FormationSlot.json"))),
                Hints = new List<Chapter_FormationHintRow>(JsonArrayUtility.FromJsonArray<Chapter_FormationHintRow>(File.ReadAllText(Dir + "Chapter_FormationHint.json"))),
                CardSpecies = new List<Card_SpeciesRow>(JsonArrayUtility.FromJsonArray<Card_SpeciesRow>(File.ReadAllText(Dir + "Card_Species.json"))),
            };
        }

        //# 챕터 DB·Strings_Ko 교차 검증 포함 (출하 로더 조건).
        public FormationDatabase Build()
        {
            return FormationDatabase.Build(
                Config, Races.ToArray(), Grades.ToArray(), RaceMonsters.ToArray(), Promotions.ToArray(), Slots.ToArray(), Hints.ToArray(),
                WorldMapTestSupport.FullDb().Chapters.Select(c => c.chapterId).ToList(),
                id => WorldMapTestSupport.Text(id).Length > 0);
        }

        public FormationDatabase BuildWithoutCrossCheck()
        {
            return FormationDatabase.Build(Config, Races.ToArray(), Grades.ToArray(), RaceMonsters.ToArray(), Promotions.ToArray(), Slots.ToArray(), Hints.ToArray());
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

        //# 출하 기본 편성(슬롯 1~6 = 이그니스·눅스·헥시아·레무르·미아르·판타스의 1등급).
        public static EMonster[] Default()
        {
            return new[] { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom };
        }

        //# 슬롯-종족이 어긋난 옛 편성(사신 2 + 저주술사 + 환령 2 + 역병귀).
        public static EMonster[] Mismatched()
        {
            return new[] { EMonster.Reaper, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Phantom };
        }

        public static HashSet<EMonster> Outputs(params EMonster[] monsters)
        {
            return new HashSet<EMonster>(monsters);
        }

        public static string[] Passives()
        {
            return Pool("CardPool_Passive");
        }

        public static string[] Actives()
        {
            return Pool("CardPool_Active");
        }

        //# 카드 풀 SO(순서 포함)가 출처 — 풀 JSON 은 없다(so-to-json-migration §3.10).
        private static string[] Pool(string assetName)
        {
            CardPool pool = UnityEditor.AssetDatabase.LoadAssetAtPath<CardPool>("Assets/_Lair/Art/Cards/" + assetName + ".asset");
            Assert.IsNotNull(pool, assetName + " 풀 에셋 없음");
            return pool.Cards.Select(c => c.Id.ToString()).ToArray();
        }

        public static ECardId Card(string name)
        {
            Assert.IsTrue(System.Enum.TryParse(name, out ECardId id), "ECardId 에 없는 카드 " + name);
            return id;
        }
    }
}
