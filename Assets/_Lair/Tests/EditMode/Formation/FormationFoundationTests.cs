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
    //# 던전 편성 슬라이스 A 핵심 로직 자체 테스트 — 데이터 로드, FormationRules, 마이그레이션/CopyFrom, 카드 제외, 씬 정합. (엣지 망라는 test-engineer)
    public class FormationFoundationTests
    {
        private static FormationDatabase ShippedDb()
        {
            string dir = WorldMapTestSupport.JsonDir;
            return FormationDatabase.Build(
                JsonUtility.FromJson<FormationConfigRow>(File.ReadAllText(dir + "FormationConfig.json")),
                JsonArrayUtility.FromJsonArray<FormationMonsterRow>(File.ReadAllText(dir + "FormationMonster.json")),
                JsonArrayUtility.FromJsonArray<FormationSlotRow>(File.ReadAllText(dir + "FormationSlot.json")),
                JsonArrayUtility.FromJsonArray<Chapter_FormationHintRow>(File.ReadAllText(dir + "Chapter_FormationHint.json")),
                WorldMapTestSupport.FullDb().Chapters.Select(c => c.chapterId).ToList(),
                id => WorldMapTestSupport.Text(id).Length > 0);
        }

        private static List<FormationSlotEntry> Rows(params EMonster[] monsters)
        {
            List<FormationSlotEntry> rows = new List<FormationSlotEntry>();
            for (int i = 0; i < monsters.Length; ++i)
            {
                rows.Add(new FormationSlotEntry { Slot = i + 1, Monster = monsters[i].ToString() });
            }
            return rows;
        }

        [Test]
        public void 출하_JSON_5종이_로드되고_기본_편성은_현행_배치다()
        {
            FormationDatabase db = ShippedDb();
            Assert.AreEqual(13, db.Config.costCap);
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom },
                db.DefaultFormation());
            Assert.AreEqual(13, FormationRules.CostSum(db.DefaultFormation(), db.CostOf));
            Assert.AreEqual(1408, db.HintKeyOf(1));

            Card_SpeciesRow[] cards = JsonArrayUtility.FromJsonArray<Card_SpeciesRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Card_Species.json"));
            Assert.AreEqual(16, cards.Length);
            Assert.DoesNotThrow(() => FormationCardSpecies.Build(cards, db));
        }

        [Test]
        public void 기본_편성이_상한을_넘기면_로드_오류다()
        {
            FormationDatabase ok = ShippedDb();
            FormationMonsterRow[] monsters = JsonArrayUtility.FromJsonArray<FormationMonsterRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "FormationMonster.json"));
            monsters[4].cost = 5;
            Assert.Throws<ChapterDataException>(() => FormationDatabase.Build(
                ok.Config, monsters,
                JsonArrayUtility.FromJsonArray<FormationSlotRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "FormationSlot.json")),
                JsonArrayUtility.FromJsonArray<Chapter_FormationHintRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Chapter_FormationHint.json"))));
        }

        [Test]
        public void 상한_경계와_종당_최대가_인자로_판정된다()
        {
            FormationDatabase db = ShippedDb();
            EMonster[] f = { EMonster.Reaper, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Phantom };
            Assert.IsTrue(FormationRules.IsValid(f, 13, 2, db.CostOf));
            Assert.IsFalse(FormationRules.IsValid(f, 10, 2, db.CostOf), "상한 인자가 쓰여야 한다");
            EMonster[] triple = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Phantom, EMonster.Hex, EMonster.Hex };
            Assert.IsFalse(FormationRules.IsValid(triple, 13, 2, db.CostOf));
            Assert.IsTrue(FormationRules.IsValid(triple, 13, 3, db.CostOf));
            Assert.AreEqual(EFormationPlace.OverCost, FormationRules.CanPlace(db.DefaultFormation(), 4, EMonster.Reaper, 13, 3, db.CostOf));
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(db.DefaultFormation(), 0, EMonster.Plague, 13, 2, db.CostOf));
        }

        [Test]
        public void EffectiveFormation_분기를_따른다()
        {
            FormationDatabase db = ShippedDb();
            EMonster[] defaults = db.DefaultFormation();
            List<FormationSlotEntry> valid = Rows(EMonster.Reaper, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Phantom);
            List<FormationSlotEntry> overCost = Rows(EMonster.Reaper, EMonster.Reaper, EMonster.Wisp, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);

            Assert.IsNull(FormationSave.Resolve(null, valid, 5), "데이터 실패 -> 씬 기본(null)");
            CollectionAssert.AreEqual(defaults, FormationSave.Resolve(db, new List<FormationSlotEntry>(), 5), "0행 -> 기본");
            CollectionAssert.AreEqual(defaults, FormationSave.Resolve(db, valid, 0), "잠김 -> 기본");
            CollectionAssert.AreEqual(Rows2Formation(valid), FormationSave.Resolve(db, valid, 1), "유효 6행 -> 저장");
            CollectionAssert.AreEqual(defaults, FormationSave.Resolve(db, overCost, 5), "무효 6행 -> 기본");
            EMonster[] fixedFormation = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague };
            CollectionAssert.AreEqual(fixedFormation, FormationSave.Resolve(db, valid, 5, fixedFormation), "외부 고정 편성 우선");
        }

        private static EMonster[] Rows2Formation(List<FormationSlotEntry> rows)
        {
            return FormationSave.FromSaved(rows);
        }

        [Test]
        public void 봉인_둥지는_봉인_종_최소_슬롯_없으면_코스트_최고_둥지다()
        {
            FormationDatabase db = ShippedDb();
            Assert.AreEqual(3, FormationRules.PickSealedSlot(db.DefaultFormation(), EMonster.Wraith, db.CostOf), "기본 편성 1-4 는 슬롯 4 (현행과 동일)");
            EMonster[] twoWraith = { EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom };
            Assert.AreEqual(0, FormationRules.PickSealedSlot(twoWraith, EMonster.Wraith, db.CostOf), "여러 곳이면 슬롯 번호 최소 1곳");
            EMonster[] noWraith = { EMonster.Hex, EMonster.Hex, EMonster.Plague, EMonster.Reaper, EMonster.Reaper, EMonster.Phantom };
            Assert.AreEqual(3, FormationRules.PickSealedSlot(noWraith, EMonster.Wraith, db.CostOf), "없으면 코스트 3 중 슬롯 최소");
            Assert.AreEqual(1, FormationRules.PickSealedSlot(noWraith, EMonster.Wraith, db.CostOf, i => i != 0 && i != 3 && i != 4), "후보 필터");
        }

        [Test]
        public void 카드_종_의존_제외는_현재_출력_종_기준_any_of다()
        {
            FormationDatabase db = ShippedDb();
            FormationCardSpecies table = FormationCardSpecies.Build(
                JsonArrayUtility.FromJsonArray<Card_SpeciesRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Card_Species.json")), db);
            HashSet<EMonster> all = new HashSet<EMonster>(db.DefaultFormation());
            foreach (ECardId card in System.Enum.GetValues(typeof(ECardId)))
            {
                Assert.IsTrue(table.IsAvailable(card, all.Contains), "기본 편성은 제외 0장: " + card);
            }

            HashSet<EMonster> noWisp = new HashSet<EMonster>(all);
            noWisp.Remove(EMonster.Wisp);
            Assert.IsFalse(table.IsAvailable(ECardId.WispHpBoost, noWisp.Contains));
            Assert.IsTrue(table.IsAvailable(ECardId.Berserk, noWisp.Contains), "수호자의 분노는 망령만 있어도 후보");
            noWisp.Remove(EMonster.Wraith);
            Assert.IsFalse(table.IsAvailable(ECardId.Berserk, noWisp.Contains));
            Assert.IsTrue(table.IsAvailable(ECardId.WallOfWisps, noWisp.Contains), "무의존 카드는 항상 후보");
        }

        [Test]
        public void 마이그레이션은_부분_행을_0행으로_정리하고_유효_행은_유지한다()
        {
            MetaProfile partial = new MetaProfile { Version = 8, Formation = Rows(EMonster.Wisp, EMonster.Reaper) };
            MetaProfileMigration.Apply(partial, (System.Func<int, string>)null);
            Assert.AreEqual(9, partial.Version);
            Assert.AreEqual(0, partial.Formation.Count);

            MetaProfile unknown = new MetaProfile { Formation = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom) };
            unknown.Formation[2].Monster = "Dragon";
            MetaProfileMigration.NormalizeFormation(unknown);
            Assert.AreEqual(0, unknown.Formation.Count);

            MetaProfile valid = new MetaProfile { Formation = Rows(EMonster.Reaper, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Phantom) };
            MetaProfileMigration.NormalizeFormation(valid);
            MetaProfileMigration.NormalizeFormation(valid);
            Assert.AreEqual(6, valid.Formation.Count, "멱등 + 유효 행 유지");
        }

        [Test]
        public void CopyFrom은_편성을_복사하고_null은_빈_리스트다()
        {
            MetaProfile src = new MetaProfile { Formation = Rows(EMonster.Reaper, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Phantom) };
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(src);
            Assert.AreEqual(6, dst.Formation.Count);
            Assert.AreNotSame(src.Formation[0], dst.Formation[0]);

            src.Formation = null;
            dst.CopyFrom(src);
            Assert.IsNotNull(dst.Formation);
            Assert.AreEqual(0, dst.Formation.Count);
        }

        [Test]
        public void 저장은_기본과_같으면_0행_다르면_6행이다()
        {
            FormationDatabase db = ShippedDb();
            Assert.AreEqual(0, FormationSave.ToSaved(db.DefaultFormation(), db.DefaultFormation()).Count);
            EMonster[] changed = db.DefaultFormation();
            changed[0] = EMonster.Plague;
            Assert.AreEqual(6, FormationSave.ToSaved(changed, db.DefaultFormation()).Count);
        }

        [Test]
        public void 씬_스포너_순서가_FormationSlot_기본_편성과_일치한다()
        {
            FormationDatabase db = ShippedDb();
            string scene = File.ReadAllText("Assets/_Lair/Scenes/Battle.unity");
            int arrayStart = scene.IndexOf("  _spawners:", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(arrayStart, 0);
            List<string> ids = new List<string>();
            foreach (string line in scene.Substring(arrayStart).Split('\n').Skip(1))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("- {fileID:") == false)
                    break;
                ids.Add(trimmed.Substring("- {fileID:".Length).Trim().TrimEnd('}').Trim());
            }
            Assert.AreEqual(FormationDatabase.SlotCount, ids.Count);
            EMonster[] defaults = db.DefaultFormation();
            for (int i = 0; i < ids.Count; ++i)
            {
                int block = scene.IndexOf("&" + ids[i] + "\n", System.StringComparison.Ordinal);
                if (block < 0)
                {
                    block = scene.IndexOf("&" + ids[i] + "\r\n", System.StringComparison.Ordinal);
                }
                Assert.GreaterOrEqual(block, 0, "스포너 블록 " + ids[i]);
                int typeAt = scene.IndexOf("_outputType:", block, System.StringComparison.Ordinal);
                int value = int.Parse(scene.Substring(typeAt + "_outputType:".Length, 3).Trim().Split('\n', '\r')[0]);
                Assert.AreEqual(defaults[i], (EMonster)value, "슬롯 " + (i + 1));
            }
        }
    }
}
